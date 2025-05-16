# Audio Emotion Classification Model

Machine learning model for vocal emotion analysis.

## Model Description

Convolutional Neural Network (CNN) that classifies audio into 5 categories:
- positive
- sad  
- neutral
- angry
- other

## Repository Files

1. **training.py** - model training script
2. **test.py** - model testing script (PyTorch version)
3. **test_onnx.py** - model testing script (ONNX version)  
4. **export_to_onnx.py** - model export to ONNX format
5. **label_encoder.pkl** - emotion label encoder
6. **emotion_model.pth** - pretrained PyTorch model weights
7. **emotion_model.onnx** - ONNX format model

## Data Preparation

1. Download the dataset following instructions:  
   [Golos Dataset README](https://github.com/salute-developers/golos/blob/master/dusha/README.md)

2. Create required directories:
   - `train_data/` - for training data
   - `test_data/` - for test data

3. Organize files:
   - Training .parquet files → `train_data/`
   - Test .parquet files → `test_data/`
   - All WAV files → corresponding `wavs/` subdirectories

## Usage

### Model Training
```bash
python training.py
```

### Model Testing
```bash
# PyTorch version
python test.py

# ONNX version  
python test_onnx.py
```

### Model Export
```bash
python export_to_onnx.py
```

## Requirements

- Python 3.7+
- PyTorch
- TorchAudio  
- ONNX Runtime
- scikit-learn
- pandas
- tqdm

Install dependencies:
```bash
pip install -r requirements.txt
```

## Performance

Test set results:
- Overall accuracy: 71.23%
- Best performance on "neutral" class (F1-score: 0.80)
- Detailed metrics available in test_result.png

## Integration

The model can be fine-tuned with new data and integrated into various audio analysis systems. ONNX format enables cross-platform deployment.