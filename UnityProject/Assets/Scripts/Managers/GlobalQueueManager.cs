using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Functions;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using WebSocketSharp;
using LobbyPlayer = Unity.Services.Lobbies.Models.Player;

public class GlobalQueueManager : MonoBehaviour
{
    private const string QueueName = "colorhunt-global";
    private const int MaxPlayers = 2;

    [SerializeField] private float heartbeatIntervalSeconds = 15f;
    [SerializeField] private QueueUIManager queueUIManager;

    private Lobby currentLobby;
    private bool createdLobby;
    private bool canCancelSearch;
    private Coroutine heartbeatCoroutine;
    private ListenerRegistration activeGameListener;
    
    public async void JoinGlobalQueueButton()
    {
        queueUIManager.StartQueueUI();
        canCancelSearch = false;
        
        try
        {
            string firestoreDoc = await JoinGlobalQueueAndRegisterAsync();

            if (!firestoreDoc.IsNullOrEmpty())
            {
                Debug.Log("Player registered successfully. Now wait for hex code.");
                GameSession.CurrentMatch = new MatchState(firestoreDoc);
                GameSession.CurrentMatch.activeMatchSettings = await FirebaseUtil.GetMatchSettingsAsync();
                
                WaitForHexCodeAndLoadGame(firestoreDoc);
            }
            else
            {
                Debug.LogError("Server rejected registration. Abort game.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join global queue: {e}");
        }
        finally
        {
            canCancelSearch = true; 
        }
    }
    
    private async Task<string> JoinGlobalQueueAndRegisterAsync()
    {
        string unityPlayerId = GameSession.LocalPlayer.unityPlayerId;
        string firebasePlayerId = GameSession.LocalPlayer.firebasePlayerId;

        if (string.IsNullOrWhiteSpace(unityPlayerId))
        {
            throw new Exception("Missing Unity player ID.");
        }

        if (string.IsNullOrWhiteSpace(firebasePlayerId))
        {
            throw new Exception("Missing Firebase player ID.");
        }

        currentLobby = await QuickJoinOrCreateLobbyAsync(unityPlayerId);

        string gameId = currentLobby.Id;

        Debug.Log("PlayerID: " + unityPlayerId + " gameId: " + gameId);
        
        
        string returnedId = await RegisterPlayerForGameAsync(
            firebasePlayerId,
            gameId
        );
        return returnedId;
    }
    
    private async Task<Lobby> QuickJoinOrCreateLobbyAsync(string unityPlayerId)
    {
        LobbyPlayer lobbyPlayer = BuildLobbyPlayer(unityPlayerId);

        try
        {
            QuickJoinLobbyOptions quickJoinOptions = new QuickJoinLobbyOptions
            {
                Player = lobbyPlayer,
                Filter = new List<QueryFilter>
                {
                    new QueryFilter(
                        field: QueryFilter.FieldOptions.AvailableSlots,
                        op: QueryFilter.OpOptions.GT,
                        value: "0"),

                    new QueryFilter(
                        field: QueryFilter.FieldOptions.S1,
                        op: QueryFilter.OpOptions.EQ,
                        value: QueueName)
                }
            };

            createdLobby = false;

            return await LobbyService.Instance.QuickJoinLobbyAsync(quickJoinOptions);
        }
        catch (LobbyServiceException e)
        {
            if (e.Reason != LobbyExceptionReason.NoOpenLobbies &&
                e.Reason != LobbyExceptionReason.EntityNotFound)
            {
                throw;
            }

            CreateLobbyOptions createOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = lobbyPlayer,
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "queue",
                        new DataObject(
                            visibility: DataObject.VisibilityOptions.Public,
                            value: QueueName,
                            index: DataObject.IndexOptions.S1)
                    }
                }
            };

            createdLobby = true;

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(
                lobbyName: QueueName,
                maxPlayers: MaxPlayers,
                options: createOptions
            );

            heartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id));

            return lobby;
        }
    }
    
    private async Task<string> RegisterPlayerForGameAsync(string firebasePlayerId, string gameId)
    {
        FirebaseFunctions functions = FirebaseFunctions.DefaultInstance;

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "playerId", firebasePlayerId },
            { "gameId", gameId }
        };

        HttpsCallableResult result = await functions
            .GetHttpsCallable("registerPlayerForGame")
            .CallAsync(data);
        return ExtractFirestoreDoc(result.Data);
    }

    private LobbyPlayer BuildLobbyPlayer(string unityPlayerId)
    {
        return new LobbyPlayer(
            id: unityPlayerId,
            data: new Dictionary<string, PlayerDataObject>
            {
                {
                    "unityPlayerId",
                    new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Member,
                        value: unityPlayerId)
                }
            }
        );
    }
    
    private string ExtractFirestoreDoc(object functionData)
    {
        if (functionData is Dictionary<string, object> objectDict &&
            objectDict.TryGetValue("gameDoc", out object gameDoc))
        {
            return Convert.ToString(gameDoc);
        }
        
        Debug.LogError("Cloud Function response could not be extracted.");
        return "";
    }

    private IEnumerator HeartbeatLobbyCoroutine(string lobbyId)
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(heartbeatIntervalSeconds);

        while (true)
        {
            _ = LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }
    
    private void WaitForHexCodeAndLoadGame(string firestoreDoc)
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        activeGameListener?.Stop();

        activeGameListener = db.Collection("active-games")
            .Document(firestoreDoc)
            .Listen(snapshot =>
            {
                if (!snapshot.Exists)
                {
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
                    if (p1 == myId)
                    {
                        opponentId = p2;
                    }
                    else if (p2 == myId)
                    {
                        opponentId = p1;
                    }
                    else
                    {
                        Debug.LogWarning("Local player's ID was not found in either player1 or player2 slots yet.");
                        return; 
                    }
                }
                else
                {
                    return;
                }

                GameSession.CurrentMatch.hexCode = hexCode;
                GameSession.CurrentMatch.opponentIdentity = new PlayerIdentity(opponentId);
                
                activeGameListener.Stop();
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
            
            queueUIManager.DisplayMatchFound(fetchedName, async () => 
            {
                await CleanUpLobbyAsync();
                SceneManager.LoadScene("Game");
            });
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
    
    public async void CancelSearchButton()
    {
        try
        {
            if (queueUIManager != null)
            {
                queueUIManager.CancelMatchAndResetQueueUI();
            }

            while (!canCancelSearch)
            {
                await Task.Delay(500);
            }
            
            if (activeGameListener != null)
            {
                activeGameListener.Stop();
                activeGameListener = null;
            }

            if (heartbeatCoroutine != null)
            {
                StopCoroutine(heartbeatCoroutine);
                heartbeatCoroutine = null;
            }

            if (currentLobby != null)
            {
                _ = CleanUpLobbyAsync();
            }
            
            if (!GameSession.CurrentMatch.activeGameDocId.IsNullOrEmpty())
            {
                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    { "gameId", GameSession.CurrentMatch.activeGameDocId }
                };
                try
                {
                    FirebaseFunctions functions = FirebaseFunctions.DefaultInstance;
                    await functions.GetHttpsCallable("cancelWaitingGame").CallAsync(data);
                }
                catch (Exception e)
                {
                    Debug.LogError(e); 
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            GameSession.ClearMatch();
            if (queueUIManager != null) queueUIManager.BackToMenuScreen();
        }
    }
    
    private async void OnDestroy()
    {
        activeGameListener?.Stop();
        
        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
        }
        
        if (currentLobby != null)
        {
            _ = CleanUpLobbyAsync();
        }
    }
    
    private async Task CleanUpLobbyAsync()
    {
        if (currentLobby == null) return;

        try
        {
            if (createdLobby)
            {
                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                Debug.Log("Host deleted the matchmaking lobby.");
            }
            else
            {
                string unityPlayerId = GameSession.LocalPlayer.unityPlayerId;
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, unityPlayerId);
                Debug.Log("Client left the matchmaking lobby.");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to clean up lobby: {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            currentLobby = null;
        }
    }
}    