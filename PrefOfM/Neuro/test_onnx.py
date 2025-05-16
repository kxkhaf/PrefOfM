import torchaudio
import torchaudio.transforms as transforms
import torch
import pandas as pd
import os
import pickle
from torch.utils.data import Dataset
from sklearn.metrics import accuracy_score, classification_report, confusion_matrix
import matplotlib.pyplot as plt
import numpy as np
from tqdm import tqdm
import onnxruntime as ort

# Устройство
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
print(f"Используется устройство: {device}")

# Датасет
class AudioDataset(Dataset):
    def __init__(self, metadata_file, audio_folder, label_encoder_path="label_encoder.pkl"):
        self.metadata = pd.read_parquet(metadata_file)
        self.audio_folder = audio_folder
        self.transform = transforms.MelSpectrogram(sample_rate=16000, n_mels=64)
        self.label_encoder_path = label_encoder_path

        with open(self.label_encoder_path, "rb") as f:
            self.label_encoder = pickle.load(f)

        self.metadata['annotator_emo'] = self.label_encoder.transform(self.metadata['annotator_emo'])
        self.success_count = 0
        self.fail_count = 0

    def __len__(self):
        return len(self.metadata)

    def __getitem__(self, idx):
        row = self.metadata.iloc[idx]
        audio_path = os.path.join(self.audio_folder, f"{row['hash_id']}.wav")

        try:
            waveform, sample_rate = torchaudio.load(audio_path, normalize=True)
            waveform = torchaudio.transforms.Resample(orig_freq=sample_rate, new_freq=16000)(waveform)
            mel_spec = self.transform(waveform)
            mel_spec = torch.log(mel_spec + 1e-9)
            self.success_count += 1
        except Exception as e:
            print(f"Ошибка при загрузке {audio_path}: {e}")
            mel_spec = torch.zeros((1, 64, 300))
            self.fail_count += 1

        total = self.success_count + self.fail_count
        if total % 100 == 0:
            success_rate = (self.success_count / total) * 100
            print(f"Прогресс загрузки аудио: {success_rate:.2f}% ({self.success_count}/{total} успешных)")

        return mel_spec, row['annotator_emo']

# Функция для обработки батчей
def collate_fn_onnx(batch):
    mel_specs, labels = zip(*batch)
    max_length = 300
    padded_specs = [torch.nn.functional.pad(spec, (0, max_length - spec.shape[-1])) for spec in mel_specs]
    padded_specs_np = torch.stack(padded_specs).numpy()
    labels_np = np.array(labels)
    return padded_specs_np, labels_np

# Визуализация матрицы ошибок
def plot_confusion_matrix(cm, classes):
    fig, ax = plt.subplots(figsize=(10, 8))
    im = ax.imshow(cm, interpolation='nearest', cmap=plt.cm.Blues)
    plt.colorbar(im)

    ax.set(xticks=np.arange(cm.shape[1]),
           yticks=np.arange(cm.shape[0]),
           xticklabels=classes, yticklabels=classes,
           title='Confusion matrix',
           ylabel='True label',
           xlabel='Predicted label')

    plt.setp(ax.get_xticklabels(), rotation=45, ha="right", rotation_mode="anchor")

    fmt = 'd'
    thresh = cm.max() / 2.
    for i in range(cm.shape[0]):
        for j in range(cm.shape[1]):
            ax.text(j, i, format(cm[i, j], fmt),
                    ha="center", va="center",
                    color="white" if cm[i, j] > thresh else "black")
    fig.tight_layout()
    plt.show()

# Основная функция оценки для ONNX
def evaluate_onnx_model(metadata_file, audio_folder, onnx_path="emotion_model.onnx", batch_size=32):
    # Загрузка датасета
    dataset = AudioDataset(metadata_file, audio_folder)
    data_loader = torch.utils.data.DataLoader(dataset, batch_size=batch_size, shuffle=False, collate_fn=collate_fn_onnx)

    # Инициализация ONNX Runtime
    ort_session = ort.InferenceSession(onnx_path, providers=['CUDAExecutionProvider' if device.type == 'cuda' else 'CPUExecutionProvider'])
    print(f"ONNX модель успешно загружена из {onnx_path}")

    all_preds = []
    all_labels = []

    print("\nНачало оценки ONNX модели...")
    for mel_specs_np, labels_np in tqdm(data_loader, desc="Обработка батчей"):
        # Выполнение предсказания
        outputs = ort_session.run(None, {'input': mel_specs_np})[0]
        preds = np.argmax(outputs, axis=1)
        all_preds.extend(preds)
        all_labels.extend(labels_np)

    # Декодируем метки
    pred_classes = dataset.label_encoder.inverse_transform(all_preds)
    true_classes = dataset.label_encoder.inverse_transform(all_labels)

    # Выводим результаты
    print("\nРезультаты оценки ONNX модели:")
    print(f"Точность (accuracy): {accuracy_score(true_classes, pred_classes):.4f}")
    print("\nОтчет по классификации:")
    print(classification_report(true_classes, pred_classes, target_names=dataset.label_encoder.classes_))

    # Матрица ошибок
    cm = confusion_matrix(true_classes, pred_classes, labels=dataset.label_encoder.classes_)
    plot_confusion_matrix(cm, dataset.label_encoder.classes_)

if __name__ == "__main__":
    metadata_file = "test_data/test.parquet"  # Путь к метаданным тестовых данных
    audio_folder = "test_data/wavs"  # Путь к аудиофайлам
    onnx_path = "emotion_model.onnx"  # Путь к ONNX модели

    evaluate_onnx_model(metadata_file, audio_folder, onnx_path)