using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Functions;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private LocationTrackerManager gpsTracker;
    [SerializeField] private GameUIManager uiManager;
    [SerializeField] private CameraPreviewManager cameraManager;


    [SerializeField] private float opponentWaitTimeoutSeconds = 10f;

    private float timeRemaining;
    private float totalPlayTime;
    
    private DateTime timeAppLostFocus;
    private bool isAppFocused = true;

    private bool gameLocked;
    private bool canContinue = false;
    private bool isSubmittingResult = false;
    private ListenerRegistration matchResultListener;
    
    private void Start()
    {
        GameSession.OwnResult = new GameResult(GameSession.LocalPlayer.firebasePlayerId);

        totalPlayTime = GameSession.CurrentMatch.activeMatchSettings != null ? GameSession.CurrentMatch.activeMatchSettings.PlayTimeSeconds : 180;
        timeRemaining = totalPlayTime;
        
        uiManager.SetupInGameHUD(GameSession.CurrentMatch.opponentIdentity.userName, GameSession.CurrentMatch.hexCode);
        uiManager.UpdateHUD(timeRemaining, 0f);

        if (cameraManager != null) cameraManager.PhotoSubmitted += HandlePhotoSubmitted;

        StartCoroutine(gpsTracker.InitializeGps());
    }

    private void Update()
    {
        if (gameLocked) return;

        timeRemaining -= Time.deltaTime;
        
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            LockGameState();
            cameraManager.ForceSubmitBlackPicture();
        }

        uiManager.UpdateHUD(timeRemaining, gpsTracker.GetCurrentDistanceMeters());
    }

    private void OnApplicationPause(bool isPaused)
    {
        HandleAppFocusChange(!isPaused);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        HandleAppFocusChange(hasFocus);
    }

    private void HandleAppFocusChange(bool hasFocus)
    {
        if (gameLocked) return;

        if (hasFocus && !isAppFocused)
        {
            isAppFocused = true;
            if (timeAppLostFocus != default)
            {
                float secondsAway = (float)(DateTime.UtcNow - timeAppLostFocus).TotalSeconds;
                if (secondsAway > 0)
                {
                    timeRemaining -= secondsAway;
                }
            }
        }
        else if (!hasFocus && isAppFocused)
        {
            isAppFocused = false;
            timeAppLostFocus = DateTime.UtcNow;
        }
    }

    private void OnDestroy()
    {
        matchResultListener?.Stop();
        if (cameraManager != null) cameraManager.PhotoSubmitted -= HandlePhotoSubmitted;
    }

    private void LockGameState()
    {
        gameLocked = true;
        
        float finalDistance = gpsTracker.GetCurrentDistanceMeters();
        
        GameSession.OwnResult.distanceMetres = Mathf.RoundToInt(finalDistance);
        GameSession.OwnResult.timeUsedMilliseconds = Mathf.RoundToInt((totalPlayTime - timeRemaining) * 1000f);

        uiManager.UpdateHUD(timeRemaining, finalDistance);
    }

    private async void HandlePhotoSubmitted()
    {
        if (isSubmittingResult) return;
        isSubmittingResult = true;

        if (!gameLocked) 
        {
            LockGameState();
        }
        
        uiManager.ShowPostGameWaitingState(GameSession.CurrentMatch.hexCode, cameraManager.capturedPhoto, GameSession.OwnResult);

        await CommitResultToFirebase();
    }

    private async Task CommitResultToFirebase()
    {
        var data = new Dictionary<string, object>
        {
            { "playerId", GameSession.LocalPlayer.firebasePlayerId },
            { "gameId", GameSession.CurrentMatch.activeGameDocId },
            { "accuracy", GameSession.OwnResult.colorAccuracyPercent },
            { "distance",  GameSession.OwnResult.distanceMetres},
            { "time", GameSession.OwnResult.timeUsedMilliseconds },
            { "image", GameSession.OwnResult.imageStoragePath}
        };

        try
        {
            HttpsCallableResult result = await FirebaseFunctions.DefaultInstance.GetHttpsCallable("commitGameResult").CallAsync(data);
            
            if (result.Data is Dictionary<string, object> objectDict && objectDict.TryGetValue("score", out object score))
            {
                GameSession.OwnResult.scorePoints = Convert.ToSingle(score);
                uiManager.UpdatePoints(GameSession.OwnResult.scorePoints);
            }

            ListenForGameResult();
            
            StartCoroutine(WaitAndUnlockContinue());
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to upload game data: {e.Message}");
            canContinue = true;
        }
    }

    private IEnumerator WaitAndUnlockContinue()
    {
        yield return new WaitForSeconds(opponentWaitTimeoutSeconds);
        
        if (!canContinue)
        {
            canContinue = true;
            Debug.Log("Opponent wait timeout reached, unlocking continue button");
        }
    }

    public void OnContinueButtonClick()
    {
        if(!canContinue) return;
        GameSession.ClearMatch();
        SceneManager.LoadScene("MainMenu");
    }
    
    private void ListenForGameResult()
    {
        matchResultListener?.Stop();
        
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        
        DocumentReference docRef = db.Collection("finished-games").Document(GameSession.CurrentMatch.activeGameDocId);

        matchResultListener = docRef.Listen(snapshot =>
        {
            if (!snapshot.Exists) return;

            string myId = GameSession.LocalPlayer.firebasePlayerId;
            
            string opponentId = "";
            string prefix = "";

            if (snapshot.TryGetValue("player1", out string p1) && snapshot.TryGetValue("player2", out string p2))
            {
                if (p1 == myId)
                {
                    opponentId = p2;
                    prefix = "player2_";
                }
                else if (p2 == myId)
                {
                    opponentId = p1;
                    prefix = "player1_";
                }
                else
                {
                    Debug.LogWarning("Local player's ID was not found in either player1 or player2 slots.");
                    return;
                }
            }
            else
            {
                return; 
            }

            try 
            {
                if (snapshot.TryGetValue(prefix + "score", out object scoreObj) &&
                    snapshot.TryGetValue(prefix + "colorAccuracy", out object accObj) &&
                    snapshot.TryGetValue(prefix + "distance", out object distObj) &&
                    snapshot.TryGetValue(prefix + "time", out object timeObj) &&
                    snapshot.TryGetValue(prefix + "image", out object imgObj))
                {
                    GameResult opponentResult = new GameResult(opponentId)
                    {
                        scorePoints = Convert.ToSingle(scoreObj),
                        colorAccuracyPercent = Convert.ToSingle(accObj),
                        distanceMetres = Convert.ToInt32(distObj),
                        timeUsedMilliseconds = Convert.ToInt32(timeObj),
                        imageStoragePath = imgObj.ToString()
                    };

                    GameSession.OpponentResult = opponentResult;

                    uiManager.UpdateOpponentStats(opponentResult, GameSession.CurrentMatch.opponentIdentity.userName);
                    
                    FetchAndDisplayOpponentImage(opponentResult.imageStoragePath);

                    matchResultListener.Stop();
                    matchResultListener = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse opponent data: {e.Message}");
            }
        });
    }
    
    private async void FetchAndDisplayOpponentImage(string imagePath)
    {
        try
        {
            byte[] imageBytes = await FirebaseUtil.DownloadImageAsync(imagePath);

            if (imageBytes == null || imageBytes.Length <= 0) return;

            var opponentTexture = new Texture2D(2, 2);
            opponentTexture.LoadImage(imageBytes); // LoadImage resizes the Texture automatically
            uiManager.SetOpponentImage(opponentTexture);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load opponent image: {e.Message}");
        }
        finally
        {
            canContinue = true;
        }
    }
    
    public void OnRematchButtonClicked()
    {
        string opponentId = GameSession.CurrentMatch?.opponentIdentity?.firebasePlayerId;

        if (string.IsNullOrEmpty(opponentId))
        {
            Debug.LogError("Cannot rematch: Opponent ID is missing.");
            return;
        }

        GameSession.PendingRematchOpponentId = opponentId;
        GameSession.ClearMatch();
        SceneManager.LoadScene("MainMenu"); 
    }

    
}