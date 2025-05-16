import torch
from torch import nn
import pickle

# Устройство
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

# Загрузка LabelEncoder
with open("label_encoder.pkl", "rb") as f:
    label_encoder = pickle.load(f)

# Определение модели
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

# Загрузка обученной модели
num_classes = len(label_encoder.classes_)
model = AnnotatorEmoClassifier(num_classes).to(device)
model.load_state_dict(torch.load("emotion_model.pth", map_location=device))
model.eval()

# Размерность: (batch_size, channels, height, width) = (1, 1, 64, 300)
dummy_input = torch.randn(1, 1, 64, 300).to(device)

# Экспорт модели в ONNX
torch.onnx.export(
    model,                     # модель
    dummy_input,               # пример входных данных
    "emotion_model.onnx",      # имя выходного файла
    export_params=True,        # сохранять обученные параметры
    opset_version=11,          # версия ONNX
    do_constant_folding=True,  # оптимизация констант
    input_names=['input'],     # имя входного тензора
    output_names=['output'],   # имя выходного тензора
    dynamic_axes={
        'input': {0: 'batch_size'},  # динамическая размерность батча
        'output': {0: 'batch_size'}
    }
)

print("Модель успешно экспортирована в ONNX формат: emotion_model.onnx")