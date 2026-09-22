using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Functions;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FriendChallengeManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject challengePopupPanel;
    [SerializeField] private TMP_Text challengePopupMessageText;
    [SerializeField] private QueueUIManager queueUIManager;
    
    private string HostedGameId { get; set; } = "";
    private ListenerRegistration activeGameListener;
    
    private string pendingGameId = "";
    private string pendingChallengerName = "";

    private void Start()
    {
        if (challengePopupPanel != null)
        {
            challengePopupPanel.SetActive(false);
        }

        if (!string.IsNullOrEmpty(GameSession.PendingRematchOpponentId))
        {
            string targetId = GameSession.PendingRematchOpponentId;
            
            GameSession.PendingRematchOpponentId = ""; 
            
            Debug.Log("Pending rematch found! Initiating challenge sequence...");
            
            SendChallengeToFriend(targetId);
        }
    }
    
    public async void SendChallengeToFriend(string friendId)
    {
        try
        {
            string myFirebaseId = GameSession.LocalPlayer.firebasePlayerId;
            if (string.IsNullOrWhiteSpace(myFirebaseId) || string.IsNullOrWhiteSpace(friendId))
            {
                Debug.LogError("Cannot challenge friend: Missing Player ID(s).");
                return;
            }
            
            string friendName = await FirebaseUtil.GetUserNameAsync(friendId);
            queueUIManager?.StartFriendQueueUI(true, friendName);
            
            FirebaseFunctions functions = FirebaseFunctions.DefaultInstance;
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "playerId", myFirebaseId },
                { "friendId", friendId }
            };

            HttpsCallableResult result = await functions.GetHttpsCallable("registerFriendGame").CallAsync(data);
            string gameId = ExtractGameId(result.Data);

            if (!string.IsNullOrEmpty(gameId))
            {
                HostedGameId = gameId;
                InitializeFriendMatchSession(gameId);
            }
            else
            {
                queueUIManager?.BackToMenuScreen();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error registering friend game challenge: {e}");
            queueUIManager?.BackToMenuScreen();
        }
    }

    public async void OnCancelChallengeButtonClick()
    {
        try
        {
            if (string.IsNullOrEmpty(HostedGameId)) return;
            queueUIManager?.CancelMatchAndResetQueueUI();
            activeGameListener?.Stop();
            activeGameListener = null;

            await ResolveChallengeCloudFunction(HostedGameId, shouldJoin: false);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error cancelling sent friend challenge: {e}");
        }
        finally
        {
            HostedGameId = "";
            GameSession.ClearMatch();
            queueUIManager?.BackToMenuScreen();
        }
    }
    
    public void ReceiveChallengeInvitation(string incomingGameId, string challengerName, string challengerId)
    {
        pendingGameId = incomingGameId;
        pendingChallengerName = challengerName; 

        if (challengePopupPanel != null)
        {
            if (challengePopupMessageText != null)
            {
                if (!string.IsNullOrEmpty(GameSession.LastOpponentId) && challengerId == GameSession.LastOpponentId)
                {
                    challengePopupMessageText.text = $"{challengerName} challenges you to a Rematch!";
                }
                else
                {
                    challengePopupMessageText.text = $"{challengerName} has challenged you to a Duel!";
                }
            }
            challengePopupPanel.SetActive(true);
        }
    }

    public void HideChallengePopup()
    {
        pendingGameId = "";
        pendingChallengerName = "";
        if (challengePopupPanel != null)
        {
            challengePopupPanel.SetActive(false);
        }
    }
    
    public async void AcceptChallengeButton()
    {
        try
        {
            if (challengePopupPanel != null) challengePopupPanel.SetActive(false);
            if (string.IsNullOrEmpty(pendingGameId)) return;
            
            string gameIdToJoin = pendingGameId; 
            
            queueUIManager?.StartFriendQueueUI(false, pendingChallengerName);
            
            await ResolveChallengeCloudFunction(gameIdToJoin, shouldJoin: true);
            
            InitializeFriendMatchSession(gameIdToJoin);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error trying to accept friend challenge: {e}");
            queueUIManager?.BackToMenuScreen();
        }
    }

    public async void RejectChallengeButton()
    {
        try
        {
            if (challengePopupPanel != null) challengePopupPanel.SetActive(false);
            if (string.IsNullOrEmpty(pendingGameId)) return;
            await ResolveChallengeCloudFunction(pendingGameId, shouldJoin: false);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error rejecting friend challenge: {e}");
        }
        finally
        {
            pendingGameId = "";
            pendingChallengerName = "";
        }
    }

    private async Task ResolveChallengeCloudFunction(string gameId, bool shouldJoin)
    {
        FirebaseFunctions functions = FirebaseFunctions.DefaultInstance;
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "gameId", gameId },
            { "join", shouldJoin }
        };

        await functions.GetHttpsCallable("joinFriendGame").CallAsync(data);
    }

    private async void InitializeFriendMatchSession(string gameId)
    {
        GameSession.CurrentMatch = new MatchState(gameId);
        GameSession.CurrentMatch.activeMatchSettings = await FirebaseUtil.GetMatchSettingsAsync();

        WaitForHexCodeAndLoadGame(gameId);
    }

    private void WaitForHexCodeAndLoadGame(string firestoreDoc)
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        activeGameListener?.Stop();

        activeGameListener = db.Collection("active-games").Document(firestoreDoc).Listen(snapshot =>
        {
            if (!snapshot.Exists) 
            {
                activeGameListener?.Stop();
                activeGameListener = null;
                HostedGameId = "";
                GameSession.ClearMatch();

                Debug.Log("Challenge document was deleted. Opponent rejected the match.");

                queueUIManager?.DisplayChallengeRejected(() =>
                {
                    queueUIManager?.BackToMenuScreen();
                });
                
                return;
            }

            if (!snapshot.TryGetValue("hexCode", out string hexCode) || string.IsNullOrWhiteSpace(hexCode))
            {
                return;
            }

            string myId = GameSession.LocalPlayer.firebasePlayerId;
            string opponentId = "";

            if (snapshot.TryGetValue("player1", out string p1) && snapshot.TryGetValue("player2", out string p2))
            {
                if (p1 == myId) opponentId = p2;
                else if (p2 == myId) opponentId = p1;
                else return;
            }
            else
            {
                return;
            }

            GameSession.CurrentMatch.hexCode = hexCode;
            GameSession.CurrentMatch.opponentIdentity = new PlayerIdentity(opponentId);

            activeGameListener?.Stop();
            activeGameListener = null;

            StartGame();
        });
    }

    private async void StartGame()
    {
        try
        {
            string fetchedName = await FirebaseUtil.GetUserNameAsync(GameSession.CurrentMatch.opponentIdentity.firebasePlayerId);
            GameSession.CurrentMatch.opponentIdentity.userName = fetchedName;
            string fetchedUnityId = await FirebaseUtil.GetUnityIdByUsernameAsync(fetchedName);
            GameSession.CurrentMatch.opponentIdentity.unityPlayerId = fetchedUnityId;

            queueUIManager?.DisplayFriendMatchFound(() =>
            {
                SceneManager.LoadScene("Game");
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed loading friend match setup: {e}");
        }
    }

    private string ExtractGameId(object functionData)
    {
        if (functionData is Dictionary<string, object> objectDict &&
            objectDict.TryGetValue("gameId", out object gameId))
        {
            return Convert.ToString(gameId);
        }
        return "";
    }

    private void OnDestroy()
    {
        activeGameListener?.Stop();
    }
}