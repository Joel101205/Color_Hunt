using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine.UI;
using UnityEngine;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;

public class FriendsManager : MonoBehaviour
{

    [SerializeField] private TMP_InputField addFriendsInputField;
    [SerializeField] private GameObject FriendView;
    [SerializeField] private GameObject friendPrefab;
    [SerializeField] private FriendChallengeManager challengeManager;

    [SerializeField] private Button addFriendButton;
    [SerializeField] private Button addButton;

    private IReadOnlyList<Relationship> friendList;
    private IReadOnlyList<Relationship> incomingFriendRequestsList;

    private IFriendsService fs;

    async void Start()
    {
        await Init();
    }

    async Task Init()
    {
        Debug.Log($"Services: {UnityServices.State}");
        Debug.Log($"Signed In: {AuthenticationService.Instance.IsSignedIn}");

        fs = UnityServices.Instance.GetFriendsService();
        await fs.InitializeAsync();

        Debug.Log("successfully retrieved friendsService Instance");

        friendList = fs.Friends;
        incomingFriendRequestsList = fs.IncomingFriendRequests;

        await acceptFriends();

        printFriends();
    }

    public void ShowAddFriendObjects()
    {
        if (addFriendsInputField != null) addFriendsInputField.gameObject.SetActive(true);
        if (addFriendButton != null) addFriendButton.gameObject.SetActive(false);
        if (addButton != null) addButton.gameObject.SetActive(true);
    }
    
    public void HideAddFriendObjects()
    {
        if (addFriendsInputField != null) addFriendsInputField.gameObject.SetActive(false);
        if (addFriendButton != null) addFriendButton.gameObject.SetActive(true);
        if (addButton != null) addButton.gameObject.SetActive(false);
    }

    async Task OnAddFriendsButtonClicked()
    {
        try
        {
            //await fs.AddFriendByNameAsync(addFriendsInputField.text);

            string playerId = await FirebaseUtil.GetUnityIdByUsernameAsync(addFriendsInputField.text);
            Debug.Log($"friend's unityid: {playerId}");
            await fs.AddFriendAsync(playerId);
            Debug.Log($"send friend request to {addFriendsInputField.text}");

        }
        catch (Exception e)
        { 
            Debug.LogError(e);
        }
    }

    async Task acceptFriends()
    {
        incomingFriendRequestsList = fs.IncomingFriendRequests;
        
        foreach (Relationship relationship in incomingFriendRequestsList)
        {
            await fs.AddFriendAsync(relationship.Member.Id);
            Debug.Log($"added {relationship.Member.Id} to friends list");
        }
    }

    public async void showFriendView()
    {
        if (fs == null) return;
        
        foreach (Transform child in FriendView.transform)
        {
            Destroy(child.gameObject);
        }

        friendList = fs.Friends;
        
        foreach (Relationship relationship in friendList)
        {
            
            GameObject friendGameObject = Instantiate(friendPrefab, FriendView.transform);

            TMP_Text friendNameText = friendGameObject.transform.Find(
                "VerticalLayoutGroup/FriendInfoLine1/FriendName"
                ).GetComponent<TMP_Text>();
            
            TMP_Text gamesPlayedCount = friendGameObject.transform.Find(
                "VerticalLayoutGroup/FriendInfoLine1/FriendGamePlayedCount"
            ).GetComponent<TMP_Text>();
            
            TMP_Text gamesWonCount = friendGameObject.transform.Find(
                "VerticalLayoutGroup/FriendInfoLine1/FriendGameWonCount"
            ).GetComponent<TMP_Text>();
            
            var friendFirebaseId = "";
            try
            {
                friendFirebaseId = await FirebaseUtil.GetFirebaseIdByUnityIdAsync(relationship.Member.Id);
                var friendName = await FirebaseUtil.GetUserNameAsync(friendFirebaseId);
                Dictionary<string, object> statistics = await FirebaseUtil.FetchPlayerStatistics(friendFirebaseId);
                friendNameText.SetText(friendName);

                if (statistics != null)
                {
                    gamesPlayedCount.SetText("GAMES PLAYED: " + statistics["gamesPlayed"]);
                    gamesWonCount.SetText("GAMES WON: " + statistics["gamesWon"]);
                }
                else
                {
                    gamesPlayedCount.SetText("GAMES PLAYED: 0");
                    gamesWonCount.SetText("GAMES WON: 0");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to fetch friend's id or name: " + e.Message);
            }
            
            Transform buttonTransform = friendGameObject.transform.Find("VerticalLayoutGroup/FriendInfoLine3/ButtonsHorizontalGroup/ChallengeFriendButton");
            
            if (buttonTransform != null)
            {
                Button challengeButton = buttonTransform.GetComponent<Button>();
                
                challengeButton.onClick.RemoveAllListeners();
                challengeButton.onClick.AddListener(() => 
                {
                    OnChallengeButtonClicked(friendFirebaseId);
                });
            }
            else
            {
                Debug.LogWarning("Challenge Button not found on prefab.");
            }
            
            Transform removeButtonTransform = friendGameObject.transform.Find("VerticalLayoutGroup/FriendInfoLine3/ButtonsHorizontalGroup/RemoveFriendButton");
            
            if (removeButtonTransform != null)
            {
                Button removeButton = removeButtonTransform.GetComponent<Button>();
                
                string friendUnityId = relationship.Member.Id; 
                
                removeButton.onClick.RemoveAllListeners();
                removeButton.onClick.AddListener(() => 
                {
                    _ = RemoveFriendAsync(friendUnityId);
                });
            }
            else
            {
                Debug.LogWarning("Remove Button not found on prefab. Check your hierarchy names.");
            }
        }
    }
    
    private async Task RemoveFriendAsync(string playerId)
    {
        try
        {
            Debug.Log($"Attempting to remove friend with ID: {playerId}");
            await fs.DeleteFriendAsync(playerId);
            Debug.Log($"Successfully removed friend with ID: {playerId}");
            
            reloadFriendsList();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to remove friend: {e.Message}");
        }
    }
    
    private async void OnChallengeButtonClicked(string friendFirebaseId)
    {
        try
        {
            if (challengeManager == null) 
            {
                return;
            }
            
            if (!string.IsNullOrEmpty(friendFirebaseId))
            {
                Debug.Log($"Initiating challenge against Firebase friend ID: {friendFirebaseId}");
                challengeManager.SendChallengeToFriend(friendFirebaseId);
            }
            else
            {
                Debug.LogError("Challenge aborted. Could not resolve Firebase Player ID for this friend.");
                //TODO: visual for failed to challenge
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
    }

    void printFriends()
    {
        Debug.Log($"Number of friends {friendList.Count}");
        
        foreach (Relationship relationship in friendList)
        {
            Debug.Log($"Friend with ID {relationship.Member.Id}");
        }
    }

    public void addFriendsWrapper()
    {
        
        _ = OnAddFriendsButtonClicked();
    }

    public void reloadFriendsList()
    {
        _ = acceptFriends();
        showFriendView();
    }

    public async Task addFriendWithId(String playerId)
    {
        try
        {
            await fs.AddFriendAsync(playerId);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        
    }
}
