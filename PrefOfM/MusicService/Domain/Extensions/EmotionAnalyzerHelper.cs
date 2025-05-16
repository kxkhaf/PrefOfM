using MusicService.Domain.Enums;

namespace MusicService.Domain.Extensions;

public static class EmotionAnalyzer
{
    public static Emotion GetRandomEmotion(this Emotion emotion)
    {
        var availableEmotions = Enum.GetValues(typeof(Emotion))
            .Cast<Emotion>()
            .Where(e => e != Emotion.Other)
            .ToList();
        int randomIndex = new Random().Next(availableEmotions.Count);
        return availableEmotions[randomIndex];
    }
}