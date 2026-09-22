using UnityEngine;

public class MatchSettings
{
    public int PlayTimeSeconds;
    public float ColorCutoff;
    public float ColorCutoffPow;

    public MatchSettings(int playTimeSeconds, float colorCutoff, float colorCutoffPow)
    {
        PlayTimeSeconds = playTimeSeconds;
        ColorCutoff = colorCutoff;
        ColorCutoffPow = colorCutoffPow;
    }

    public MatchSettings()
    {
        PlayTimeSeconds = 180;
        ColorCutoff = 0.65f;
        ColorCutoffPow = 1.5f;
    }
}
