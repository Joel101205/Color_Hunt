using System;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerProfileScript : MonoBehaviour
{
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text playerIdText;
    [SerializeField] private TMP_Text gamesPlayed;
    [SerializeField] private TMP_Text gamesWon;
    [SerializeField] private TMP_Text winRate;
    
    [SerializeField] private TMP_InputField newNameInputField;

    private void Start()
    {
        RefreshPlayerProfileAsync();
    }

    private async void RefreshPlayerProfileAsync()
    {
        try
        {
            string name = GameSession.LocalPlayer.userName;
            playerName.SetText(name);
            
            Dictionary<string, object> statistics =
                await FirebaseUtil.FetchPlayerStatistics(GameSession.LocalPlayer.firebasePlayerId);

            if (statistics != null)
            {
                GameSession.LocalPlayer.gamesPlayed = statistics["gamesPlayed"].ToString();
                GameSession.LocalPlayer.gamesWon = statistics["gamesWon"].ToString();
            }
            else
            {
                GameSession.LocalPlayer.gamesPlayed = "0";
                GameSession.LocalPlayer.gamesWon = "0";
            }
            
            gamesPlayed.SetText("GAMES PLAYED: " +GameSession.LocalPlayer.gamesPlayed);
            gamesWon.SetText("GAMES WON: " + GameSession.LocalPlayer.gamesWon);
            if (GameSession.LocalPlayer.gamesPlayed == "0")
            {
                winRate.SetText("WINRATE: 0%");
            }
            else 
            {
                float won = Convert.ToSingle(GameSession.LocalPlayer.gamesWon);
                float played = Convert.ToSingle(GameSession.LocalPlayer.gamesPlayed);
                float winPercentage = (won / played) * 100f;

                winRate.SetText("WINRATE: " + Mathf.RoundToInt(winPercentage) + "%");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to fetch player statistics: " + e);
        }
    }

    public void OnProfileOpened()
    {
        RefreshPlayerProfileAsync();
    }
    
    
    /*
    public async void UpdatePlayerName()
    {
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newNameInputField.text);
            playerName.SetText(AuthenticationService.Instance.PlayerName);

            DocumentReference doc = db.Collection("players").Document(firebaseAuth.CurrentUser.UserId);
            await doc.UpdateAsync("userName", newNameInputField.text);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }*/

    public void OnLogOutButtonClicked()
    {
        GameSession.ClearAll();
        FirebaseAuth.DefaultInstance.SignOut();
        AuthenticationService.Instance.SignOut();
        AuthenticationService.Instance.ClearSessionToken();
        SceneManager.LoadScene("LoginScene");
    }


}
