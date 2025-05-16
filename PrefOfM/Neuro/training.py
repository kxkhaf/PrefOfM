import torchaudio
import torchaudio.transforms as transforms
import torch
from torch import nn, optim
from torch.utils.data import DataLoader, Dataset, random_split, ConcatDataset
import pandas as pd
import os
import pickle
from sklearn.preprocessing import LabelEncoder
from tqdm import tqdm

# Устройство
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
print(f"Используется устройство: {device}")

# Датасет
class AudioDataset(Dataset):
    def __init__(self, metadata_file, audio_folder, label_encoder):
        self.metadata = pd.read_parquet(metadata_file)
        self.audio_folder = audio_folder
        self.transform = transforms.MelSpectrogram(sample_rate=16000, n_mels=64)
        self.metadata['annotator_emo'] = label_encoder.transform(self.metadata['annotator_emo'])

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
        except Exception as e:
            print(f"Ошибка при загрузке {audio_path}: {e}")
            mel_spec = torch.zeros((1, 64, 300))

        label = row['annotator_emo']
        return mel_spec, label

# Загрузка и объединение датасетов
def load_datasets(metadata_files, audio_folders, label_encoder_path="label_encoder.pkl"):
    if os.path.exists(label_encoder_path):
        with open(label_encoder_path, "rb") as f:
            label_encoder = pickle.load(f)
    else:
        label_encoder = LabelEncoder()
        combined_metadata = pd.concat([pd.read_parquet(file) for file in metadata_files])
        label_encoder.fit(combined_metadata['annotator_emo'])
        with open(label_encoder_path, "wb") as f:
            pickle.dump(label_encoder, f)

    datasets = [AudioDataset(meta, folder, label_encoder) for meta, folder in zip(metadata_files, audio_folders)]
    return ConcatDataset(datasets)

# Выравнивание батча
def collate_fn(batch):
    mel_specs, labels = zip(*batch)
    max_length = 300
    padded_specs = [torch.nn.functional.pad(spec, (0, max_length - spec.shape[-1])) for spec in mel_specs]
    return torch.stack(padded_specs).to(device), torch.tensor(labels, device=device)

# Улучшенная модель
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

# Тренировка
def train_model(metadata_files, audio_folders, model_path="emotion_model.pth", num_epochs=30, batch_size=32):
    dataset = load_datasets(metadata_files, audio_folders)
    train_size = int(0.8 * len(dataset))
    val_size = len(dataset) - train_size
    train_dataset, val_dataset = random_split(dataset, [train_size, val_size])

    train_loader = DataLoader(train_dataset, batch_size=batch_size, shuffle=True, collate_fn=collate_fn)
    val_loader = DataLoader(val_dataset, batch_size=batch_size, shuffle=False, collate_fn=collate_fn)

    num_classes = len(set(dataset.datasets[0].metadata['annotator_emo']))
    model = AnnotatorEmoClassifier(num_classes).to(device)
    criterion = nn.CrossEntropyLoss()
    optimizer = optim.AdamW(model.parameters(), lr=1e-4, weight_decay=1e-5)
    scheduler = optim.lr_scheduler.CosineAnnealingLR(optimizer, T_max=num_epochs)

    if os.path.exists(model_path):
        model.load_state_dict(torch.load(model_path, map_location=device))
        print(f"Загружена модель из {model_path}")

    for epoch in range(num_epochs):
        model.train()
        total_loss = 0
        correct = 0
        total = 0

        for mel_spec, label in tqdm(train_loader, desc=f"Эпоха {epoch + 1}/{num_epochs}"):
            optimizer.zero_grad()
            outputs = model(mel_spec)
            loss = criterion(outputs, label)
            loss.backward()
            optimizer.step()

            total_loss += loss.item()
            correct += (torch.argmax(outputs, dim=1) == label).sum().item()
            total += label.size(0)

        train_acc = correct / total * 100
        scheduler.step()

        model.eval()
        val_correct, val_total, val_loss = 0, 0, 0
        with torch.no_grad():
            for mel_spec, label in val_loader:
                outputs = model(mel_spec)
                loss = criterion(outputs, label)
                val_loss += loss.item()
                val_correct += (torch.argmax(outputs, dim=1) == label).sum().item()
                val_total += label.size(0)

        val_acc = val_correct / val_total * 100
        print(f"Эпоха {epoch+1}, Train Loss: {total_loss:.4f}, Train Acc: {train_acc:.2f}%, Val Acc: {val_acc:.2f}%")

        torch.save(model.state_dict(), model_path)

if __name__ == "__main__":
    metadata_files = ["train_data/train.parquet"]
    audio_folders = ["train_data/wavs"]
    train_model(metadata_files, audio_folders)