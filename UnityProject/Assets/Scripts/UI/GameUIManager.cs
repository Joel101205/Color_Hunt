using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("In-Game HUD")]
        [SerializeField] private TMP_Text playingAgainstText;
        [SerializeField] private TMP_Text timeLeftText;
        [SerializeField] private TMP_Text distanceToStartText;
        [SerializeField] private Image lookingForColorImage;
        [SerializeField] private GameObject rematchButton;

        [Header("Post-Game Stats UI")] 
        [SerializeField] private GameObject colorWonCard;
        [SerializeField] private TMP_Text colorWonCardText;
        [SerializeField] private DropletEffect matchResultDroplet;
        [SerializeField] private Image colorImage;
        [SerializeField] private Image submittedColor;
        [SerializeField] private TMP_Text hexCodeText;
        [SerializeField] private TMP_Text matchResultText;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private RawImage playerImage;
        [SerializeField] private TMP_Text timeStatsText;
        [SerializeField] private TMP_Text distanceStatsText;
        [SerializeField] private TMP_Text accuracyStatsText;
        [SerializeField] private TMP_Text pointsStatsText;
        
        [Header("Opponent Stats UI")]
        [SerializeField] private GameObject opponentStatsRoot;
        [SerializeField] private TMP_Text opponentNameText;
        [SerializeField] private RawImage opponentImage;
        [SerializeField] private TMP_Text opponentTimeStatsText;
        [SerializeField] private TMP_Text opponentDistanceStatsText;
        [SerializeField] private TMP_Text opponentAccuracyStatsText;
        [SerializeField] private TMP_Text opponentPointsStatsText;

        public void SetupInGameHUD(string enemyName, string hexCode)
        {
            colorWonCard.SetActive(false);
            rematchButton.SetActive(false);
            playingAgainstText.text = (string.IsNullOrWhiteSpace(enemyName) ? "???????" : enemyName);

            if (ColorUtility.TryParseHtmlString(hexCode.StartsWith("#") ? hexCode : "#" + hexCode, out Color color))
            {
                lookingForColorImage.color = color;
            }
        }

        public void UpdateHUD(float timeRemainingSeconds, float distanceMeters)
        {
            int totalSeconds = Mathf.CeilToInt(timeRemainingSeconds);
            timeLeftText.text = $"Time: {totalSeconds / 60:00}:{totalSeconds % 60:00}";
            distanceToStartText.text = $"Distance: {Mathf.RoundToInt(distanceMeters):000}m";
        }
        

        public void ShowPostGameWaitingState(string hexCode, Texture2D capturedPhoto, GameResult result)
        {
            if (ColorUtility.TryParseHtmlString(hexCode.StartsWith("#") ? hexCode : "#" + hexCode, out Color color))
            {
                colorImage.color = color;
            }

            submittedColor.color = result.submittedColor;
            matchResultText.text = "Waiting for Opponent...";
            opponentStatsRoot.SetActive(false);
            
            hexCodeText.text = (hexCode.StartsWith("#") ? hexCode : "#" + hexCode).ToUpperInvariant();
            playerNameText.text = "You";
            distanceStatsText.text = $"{result.distanceMetres}m";
            accuracyStatsText.text = $"{Mathf.RoundToInt(result.colorAccuracyPercent)}%";
            pointsStatsText.text = "?? Points";

            if (playerImage != null && capturedPhoto != null)
            {
                playerImage.texture = capturedPhoto;
            }

            int totalSecs = Mathf.CeilToInt(result.timeUsedMilliseconds / 1000f);
            timeStatsText.text = $"{totalSecs / 60:00}:{totalSecs % 60:00}";
        }

        public void UpdatePoints(float points)
        {
            pointsStatsText.text = $"{points} Points";
        }
        
        public void UpdateOpponentStats(GameResult result, string opponentName)
        {
            rematchButton.SetActive(true);
            opponentStatsRoot.SetActive(true);
            opponentAccuracyStatsText.text = $"{Mathf.RoundToInt(result.colorAccuracyPercent)}%";
            opponentDistanceStatsText.text = $"{result.distanceMetres}m";
            opponentNameText.text = opponentName;
            int totalSecs = Mathf.CeilToInt(result.timeUsedMilliseconds / 1000f);
            opponentTimeStatsText.text = $"{totalSecs / 60:00}:{totalSecs % 60:00}";
            opponentPointsStatsText.text = $"{result.scorePoints} Points";
            colorWonCard.SetActive(true);
            if (GameSession.OwnResult.scorePoints > result.scorePoints)
            {
                matchResultText.text = "You Win!";
                colorWonCardText.text = "Added 3 new Pixels to your Inventory!";
                matchResultDroplet.PlaySplash(Color.darkGreen);
            }
            else if (GameSession.OwnResult.scorePoints < result.scorePoints)
            {
                matchResultText.text = "You Lose!";
                colorWonCardText.text = "Added 1 new Pixel to your Inventory!";
                matchResultDroplet.PlaySplash(Color.firebrick);
            }
            else
            {
                matchResultText.text = "Draw!";
                colorWonCardText.text = "Added 1 new Pixels to your Inventory!";
                matchResultDroplet.PlaySplash(Color.gray2);
            }
        }
        
        public void SetOpponentImage(Texture2D texture)
        {
            opponentImage.texture = texture;
        }
    }
}