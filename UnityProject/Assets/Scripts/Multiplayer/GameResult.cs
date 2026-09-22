using UnityEngine;

public class GameResult
{
    public string firebasePlayerId { get; set; }
    public int distanceMetres { get; set; }
    public int timeUsedMilliseconds { get; set; }
    public float colorAccuracyPercent { get; set; }
    public float scorePoints { get; set; }
    public Color submittedColor { get; set; }
    public string imageStoragePath { get; set; }

    public Texture2D localPhotoTexture { get; set; }

    public GameResult(string firebasePlayerId)
    {
        this.firebasePlayerId = firebasePlayerId;
    }
}
