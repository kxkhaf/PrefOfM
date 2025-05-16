using System.Numerics;
using System.Text.Json;
using MathNet.Numerics.IntegralTransforms;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using MusicService.Services.Interfaces;
using NAudio.Wave;

namespace MusicService.Services.Implementation;

public class EmotionPredictorService : IEmotionPredictor, IDisposable
{
    private readonly InferenceSession _session;
    private readonly Dictionary<int, string> _labelMap;
    private const int SampleRate = 16000;
    private const int NMels = 64;
    private const int MaxLength = 300;
    private const int FftSize = 512;
    private const int HopLength = FftSize / 2;
    private readonly float[] _melBasis;

    public EmotionPredictorService(string modelPath, string labelPath)
    {
        _session = new InferenceSession(modelPath);
        _labelMap = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(labelPath))!
            .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            
        _melBasis = CreateMelFilterBank();
    }

    private float[] CreateMelFilterBank()
    {
        int nFft = FftSize;
        double fMin = 0.0;
        double fMax = SampleRate / 2.0;
        int nMels = NMels;

        var melBasis = new float[nMels * (nFft / 2 + 1)];
        var fftFreqs = GenerateFftFrequencies(nFft);
        var melFreqs = Linspace(ToMelScale(fMin), ToMelScale(fMax), nMels + 2);
        var binFreqs = melFreqs.Select(FromMelScale).ToArray();

        for (int i = 0; i < nMels; i++)
        {
            int start = (int)Math.Floor(binFreqs[i] / (SampleRate / (double)nFft));
            int center = (int)Math.Floor(binFreqs[i + 1] / (SampleRate / (double)nFft));
            int end = (int)Math.Floor(binFreqs[i + 2] / (SampleRate / (double)nFft));

            for (int j = start; j < center; j++)
            {
                if (j >= 0 && j < nFft / 2 + 1)
                {
                    melBasis[i * (nFft / 2 + 1) + j] = (float)((j - start) / (double)(center - start));
                }
            }

            for (int j = center; j < end; j++)
            {
                if (j >= 0 && j < nFft / 2 + 1)
                {
                    melBasis[i * (nFft / 2 + 1) + j] = (float)((end - j) / (double)(end - center));
                }
            }
        }

        return melBasis;
    }

    private double[] GenerateFftFrequencies(int nFft)
    {
        return Enumerable.Range(0, nFft / 2 + 1)
            .Select(k => k * (SampleRate / (double)nFft))
            .ToArray();
    }

    private double[] Linspace(double start, double stop, int num)
    {
        double[] result = new double[num];
        double step = (stop - start) / (num - 1);
        for (int i = 0; i < num; i++)
        {
            result[i] = start + i * step;
        }
        return result;
    }

    private double ToMelScale(double freq)
    {
        return 1127.0 * Math.Log(1.0 + freq / 700.0);
    }

    private double FromMelScale(double mel)
    {
        return 700.0 * (Math.Exp(mel / 1127.0) - 1.0);
    }

    public async Task<string> PredictEmotionAsync(IFormFile audioFile)
    {
        using var stream = new MemoryStream();
        await audioFile.CopyToAsync(stream);
        stream.Position = 0;

        var melSpec = await ComputeLogMelSpectrogramAsync(stream);
        var inputTensor = new DenseTensor<float>(melSpec, [1, 1, NMels, MaxLength]);
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", inputTensor)
        };

        using var results = _session.Run(inputs);
        var output = results.First().AsEnumerable<float>().ToArray();
        int predictedLabelIdx = ArgMax(output);

        return _labelMap.GetValueOrDefault(predictedLabelIdx, "Unknown");
    }

    private int ArgMax(float[] array)
    {
        int maxIndex = 0;
        float maxValue = array[0];
        for (int i = 1; i < array.Length; i++)
        {
            if (array[i] > maxValue)
            {
                maxValue = array[i];
                maxIndex = i;
            }
        }
        return maxIndex;
    }

    private async Task<float[]> ComputeLogMelSpectrogramAsync(Stream audioStream)
    {
        var waveform = await LoadAndResampleAudioAsync(audioStream);
        var spectrogram = ComputeSpectrogram(waveform);
        var melSpec = ApplyMelFilters(spectrogram);

        for (int i = 0; i < melSpec.Length; i++)
        {
            melSpec[i] = (float)Math.Log(Math.Max(melSpec[i], 1e-9));
        }

        return melSpec;
    }

    private async Task<float[]> LoadAndResampleAudioAsync(Stream audioStream)
    {
        await using var reader = new WaveFileReader(audioStream);
        
        if (reader.WaveFormat.SampleRate == SampleRate && reader.WaveFormat.Channels == 1)
        {
            return await ConvertToFloatArrayAsync(reader);
        }

        ISampleProvider sampleProvider = reader.ToSampleProvider();
        if (reader.WaveFormat.Channels > 1)
        {
            sampleProvider = new StereoToMonoSampleProvider(sampleProvider);
        }
        
        if (reader.WaveFormat.SampleRate != SampleRate)
        {
            sampleProvider = new WdlResamplingSampleProvider(sampleProvider, SampleRate);
        }
        
        var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
        List<float> allSamples = new List<float>();
        int samplesRead;
        while ((samplesRead = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < samplesRead; i++)
            {
                allSamples.Add(buffer[i]);
            }
        }

        return allSamples.ToArray();
    }

    private async Task<float[]> ConvertToFloatArrayAsync(WaveFileReader reader)
    {
        var buffer = new byte[reader.Length];
        await reader.ReadExactlyAsync(buffer);
        var floatArray = new float[buffer.Length / 2];
        for (int i = 0; i < floatArray.Length; i++)
        {
            floatArray[i] = BitConverter.ToInt16(buffer, i * 2) / 32768f;
        }
        return floatArray;
    }

    private float[] ComputeSpectrogram(float[] waveform)
    {
        int nFrames = Math.Min((waveform.Length - FftSize) / HopLength + 1, MaxLength);
        var spectrogram = new float[(FftSize / 2 + 1) * nFrames];

        for (int i = 0; i < nFrames; i++)
        {
            var frame = new Complex[FftSize];
            for (int j = 0; j < FftSize; j++)
            {
                int index = i * HopLength + j;
                frame[j] = index < waveform.Length ? new Complex(waveform[index], 0) : Complex.Zero;
            }

            ApplyHannWindow(frame);
            Fourier.Forward(frame, FourierOptions.NoScaling);
                
            for (int j = 0; j < FftSize / 2 + 1; j++)
            {
                spectrogram[i * (FftSize / 2 + 1) + j] = (float)(frame[j].Magnitude * frame[j].Magnitude);
            }
        }

        return spectrogram;
    }

    private void ApplyHannWindow(Complex[] frame)
    {
        for (int i = 0; i < frame.Length; i++)
        {
            double window = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (frame.Length - 1)));
            frame[i] *= window;
        }
    }

    private float[] ApplyMelFilters(float[] spectrogram)
    {
        int nFftBins = FftSize / 2 + 1;
        int nFrames = spectrogram.Length / nFftBins;
        var melSpec = new float[NMels * nFrames];

        for (int t = 0; t < nFrames; t++)
        {
            for (int m = 0; m < NMels; m++)
            {
                float sum = 0;
                for (int k = 0; k < nFftBins; k++)
                {
                    sum += spectrogram[t * nFftBins + k] * _melBasis[m * nFftBins + k];
                }
                melSpec[t * NMels + m] = sum;
            }
        }

        return melSpec;
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}

public class StereoToMonoSampleProvider(ISampleProvider source) : ISampleProvider
{
    public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 1);

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesNeeded = count * 2;
        var tempBuffer = new float[samplesNeeded];
        int samplesRead = source.Read(tempBuffer, 0, samplesNeeded);
            
        for (int i = 0; i < samplesRead / 2; i++)
        {
            buffer[offset + i] = (tempBuffer[i * 2] + tempBuffer[i * 2 + 1]) * 0.5f;
        }

        return samplesRead / 2;
    }
}

public class WdlResamplingSampleProvider(ISampleProvider source, int targetSampleRate) : ISampleProvider
{
    private readonly float[] _sourceBuffer = new float[1024];

    public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(targetSampleRate, source.WaveFormat.Channels);

    public int Read(float[] buffer, int offset, int count)
    {
        int sourceSamplesNeeded = (int)Math.Ceiling((double)count * source.WaveFormat.SampleRate / targetSampleRate);
        sourceSamplesNeeded = Math.Min(sourceSamplesNeeded, _sourceBuffer.Length);
            
        int sourceSamplesRead = source.Read(_sourceBuffer, 0, sourceSamplesNeeded);
        if (sourceSamplesRead == 0) return 0;
            
        for (int i = 0; i < count; i++)
        {
            double srcPos = (double)i * sourceSamplesRead / count;
            int srcIndex = (int)srcPos;
            float frac = (float)(srcPos - srcIndex);
                
            if (srcIndex + 1 < sourceSamplesRead)
            {
                buffer[offset + i] = _sourceBuffer[srcIndex] * (1 - frac) + _sourceBuffer[srcIndex + 1] * frac;
            }
            else
            {
                buffer[offset + i] = _sourceBuffer[srcIndex];
            }
        }

        return count;
    }
}