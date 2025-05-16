import torchaudio
import torchaudio.transforms as transforms
import torch
import torch.nn as nn
import pandas as pd
import os
import pickle
from torch.utils.data import DataLoader, Dataset
from sklearn.metrics import accuracy_score, classification_report, confusion_matrix
import matplotlib.pyplot as plt
import numpy as np
from tqdm import tqdm

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


# Модель
class AnnotatorEmoClassifier(nn.Module):
    def __init__(self, num_classes):
        super().__init__()
        self.conv = nn.Sequential(
            nn.Conv2d(1, 32, kernel_size=3, padding=1),
            nn.BatchNorm2d(32),
            nn.ReLU(),
            nn.MaxPool2d(2),
            nn.Conv2d(32, 64, kernel_size=3, padding=1),
            nn.BatchNorm2d(64),
            nn.ReLU(),
            nn.MaxPool2d(2),
            nn.Conv2d(64, 128, kernel_size=3, padding=1),
            nn.BatchNorm2d(128),
            nn.ReLU(),
            nn.AdaptiveAvgPool2d((1, 1))
        )
        self.fc = nn.Sequential(
            nn.Linear(128, 64),
            nn.ReLU(),
            nn.Dropout(0.3),
            nn.Linear(64, num_classes)
        )

    def forward(self, x):
        x = self.conv(x)
        x = x.view(x.size(0), -1)
        return self.fc(x)


# Функция для обработки батчей
def collate_fn(batch):
    mel_specs, labels = zip(*batch)
    max_length = 300
    padded_specs = [torch.nn.functional.pad(spec, (0, max_length - spec.shape[-1])) for spec in mel_specs]
    return torch.stack(padded_specs).to(device), torch.tensor(labels, device=device)


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


# Основная функция оценки
def evaluate_model(metadata_file, audio_folder, model_path="emotion_model.pth", batch_size=32):
    # Загрузка датасета
    dataset = AudioDataset(metadata_file, audio_folder)
    data_loader = DataLoader(dataset, batch_size=batch_size, shuffle=False, collate_fn=collate_fn)

    # Инициализация модели
    num_classes = len(dataset.label_encoder.classes_)
    model = AnnotatorEmoClassifier(num_classes).to(device)

    # Безопасная загрузка модели
    try:
        model.load_state_dict(torch.load(model_path, map_location=device, weights_only=True))
        print(f"Модель успешно загружена из {model_path}")
    except Exception as e:
        print(f"Ошибка при загрузке модели: {e}")
        return

    model.eval()

    all_preds = []
    all_labels = []

    print("\nНачало оценки модели...")
    with torch.no_grad():
        for mel_specs, labels in tqdm(data_loader, desc="Обработка батчей"):
            outputs = model(mel_specs)
            preds = torch.argmax(outputs, dim=1)
            all_preds.extend(preds.cpu().numpy())
            all_labels.extend(labels.cpu().numpy())

    # Декодируем метки
    pred_classes = dataset.label_encoder.inverse_transform(all_preds)
    true_classes = dataset.label_encoder.inverse_transform(all_labels)

    # Выводим результаты
    print("\nРезультаты оценки:")
    print(f"Точность (accuracy): {accuracy_score(true_classes, pred_classes):.4f}")
    print("\nОтчет по классификации:")
    print(classification_report(true_classes, pred_classes, target_names=dataset.label_encoder.classes_))

    # Матрица ошибок
    cm = confusion_matrix(true_classes, pred_classes, labels=dataset.label_encoder.classes_)
    plot_confusion_matrix(cm, dataset.label_encoder.classes_)


if __name__ == "__main__":
    metadata_file = "test_data/test.parquet"  # Путь к метаданным тестовых данных
    audio_folder = "test_data/wavs"  # Путь к аудиофайлам
    model_path = "emotion_model.pth"  # Путь к сохраненной модели

    evaluate_model(metadata_file, audio_folder, model_path)