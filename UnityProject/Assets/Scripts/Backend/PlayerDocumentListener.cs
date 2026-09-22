using System;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

public class PlayerDocumentListener : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FriendChallengeManager friendChallengeManager;
    
    private ListenerRegistration playerDocListener;
    private string latestDetectedChallengeId = "";

    private void Start()
    {
        StartListeningToPlayerDocument();
    }

    private void StartListeningToPlayerDocument()
    {
        string myId = GameSession.LocalPlayer?.firebasePlayerId;
        
        if (string.IsNullOrEmpty(myId))
        {
            Debug.LogError("Cannot listen to player doc: firebasePlayerId is null or empty.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference myPlayerDoc = db.Collection("players").Document(myId);

        StopListening();

        playerDocListener = myPlayerDoc.Listen(snapshot =>
        {
            if (!snapshot.Exists) return;

            if (snapshot.TryGetValue("waitForFriendGame", out string activeChallengeGameId))
            {
                latestDetectedChallengeId = activeChallengeGameId;

                if (string.IsNullOrEmpty(activeChallengeGameId))
                {
                    friendChallengeManager?.HideChallengePopup();
                    return; 
                }

                db.Collection("active-games").Document(activeChallengeGameId).GetSnapshotAsync().ContinueWithOnMainThread(async task =>
                {
                    if (task.IsFaulted || task.IsCanceled) return;

                    DocumentSnapshot gameSnap = task.Result;
                    if (gameSnap.Exists)
                    {
                        if (gameSnap.TryGetValue("player1", out string player1Id))
                        {
                            if (player1Id == myId) return;

                            if (latestDetectedChallengeId != activeChallengeGameId) return;

                            string challengerName = await FirebaseUtil.GetUserNameAsync(player1Id);

                            friendChallengeManager?.ReceiveChallengeInvitation(activeChallengeGameId, challengerName, player1Id);
                        }
                    }
                });
            }
        });
    }

    private void StopListening()
    {
        if (playerDocListener != null)
        {
            playerDocListener.Stop();
            playerDocListener = null;
        }
    }

    private void OnDestroy()
    {
        StopListening();
    }
}