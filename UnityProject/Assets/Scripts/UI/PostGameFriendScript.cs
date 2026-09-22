using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Friends;
using UnityEngine;
using UnityEngine.UI;

public class PostGameFriendScript : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject addFriendButtonObject;

    private void Start()
    {
        EvaluateFriendStatusAsync();
    }

    private void EvaluateFriendStatusAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized) return;

        try
        {
            IFriendsService friendsService = UnityServices.Instance.GetFriendsService();
            
            string opponentUnityId = GameSession.CurrentMatch?.opponentIdentity?.unityPlayerId;

            if (string.IsNullOrEmpty(opponentUnityId))
            {
                Debug.LogWarning("Opponent Unity ID is missing, hiding friend button.");
                if (addFriendButtonObject != null) addFriendButtonObject.SetActive(false);
                return;
            }

            bool isAlreadyFriend = friendsService.Friends.Any(friend => friend.Member.Id == opponentUnityId);

            if (isAlreadyFriend)
            {
                if (addFriendButtonObject != null) addFriendButtonObject.SetActive(false);
            }
            else
            {
                if (addFriendButtonObject != null) addFriendButtonObject.SetActive(true);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to evaluate friend status: {e}");
        }
    }

    public async void OnAddFriendButtonClicked()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized) return;
            if (addFriendButtonObject != null) addFriendButtonObject.SetActive(false);

            var opponentUnityId = GameSession.CurrentMatch.opponentIdentity.unityPlayerId;
            
            await UnityServices.Instance.GetFriendsService().AddFriendAsync(opponentUnityId);
            
            Debug.Log($"Friend request sent to {opponentUnityId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error adding friend: {e}");
            
            if (addFriendButtonObject != null) addFriendButtonObject.SetActive(true);
        }
    }
}