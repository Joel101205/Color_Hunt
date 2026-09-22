using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GalleryItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private RawImage image;
    [SerializeField] private Image backgroundImage;

    public void Initialize(MatchHistoryEntry matchData)
    {
        if (image != null) 
        {
            image.texture = matchData.myResult.localPhotoTexture;
            
            image.enabled = (image.texture != null);
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = ColorInventory.HexToColor(matchData.hexCode);
        }

        if (scoreText != null) 
        {
            scoreText.text = $"{matchData.myResult.scorePoints}";
        }
    }
}