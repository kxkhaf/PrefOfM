namespace MusicService.Services.Interfaces;

public interface IEmotionPredictor
{
    public async Task<string> PredictEmotionAsync(IFormFile audioFile)
    {
        throw new NotImplementedException();
    }
}