using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginScript : MonoBehaviour
{
    private enum AuthMode
    {
        None,
        SignUp,
        SignIn
    }

    private AuthMode currentAuthMode = AuthMode.None;


    [SerializeField] private GameObject authScreen;
    [SerializeField] private GameObject loginScreen;
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private TMP_Text authModeText;
    [SerializeField] private TMP_Text switchToOtherAuthMethodText;

    [SerializeField] private GameObject authErrorCard;
    [SerializeField] private GameObject signUpErrorMessage;
    [SerializeField] private GameObject signInErrorMessage;

    [SerializeField] private TMP_Text loadingMessage;

    private FirebaseAuth firebaseAuth;
    private FirebaseFirestore db;

    async void Awake()
    {
        try
        {
            await UnityServices.InitializeAsync();

            //AuthenticationService.Instance.ClearSessionToken();
            Debug.Log("Unity Initialized");

            await InitializeFirebase();

            SetupEvents();

            await SignInCachedUserAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    async Task InitializeFirebase()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (status != DependencyStatus.Available)
        {
            Debug.LogError($"Firebase dependencies unavailable: {status}");
            return;
        }

        firebaseAuth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        Debug.Log("Firebase initialized");
    }

    void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += async () =>
        {
            string name = await AuthenticationService.Instance.GetPlayerNameAsync();
            Debug.Log($"Access Token: {AuthenticationService.Instance.AccessToken}");
            Debug.Log($"PlayerId: {AuthenticationService.Instance.PlayerId}");
            Debug.Log($"PlayerName: {name}");

            Dictionary<string, object> statistics =
                await FirebaseUtil.FetchPlayerStatistics(firebaseAuth.CurrentUser.UserId);

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
            
        };

        AuthenticationService.Instance.SignInFailed += (error) => { Debug.LogError(error); };

        AuthenticationService.Instance.SignedOut += () => { Debug.Log("Player signed out."); };

        AuthenticationService.Instance.Expired += () =>
        {
            Debug.Log("Player session could not be refreshed and expired.");
        };
    }

    private void ClearInputFields()
    {
        usernameInputField.text = "";
        emailInputField.text = "";
        passwordInputField.text = "";
    }

    public void HideAuthScreen()
    {
        currentAuthMode = AuthMode.None;
        authScreen.SetActive(false);
        HideAuthErrors();
        ClearInputFields();
    }

    private void HideAuthErrors()
    {
        if (authErrorCard != null)
            authErrorCard.SetActive(false);

        if (signUpErrorMessage != null)
            signUpErrorMessage.SetActive(false);

        if (signInErrorMessage != null)
            signInErrorMessage.SetActive(false);
    }

    public void ShowSignUpForm()
    {
        currentAuthMode = AuthMode.SignUp;
        authScreen.SetActive(true);
        usernameInputField.gameObject.SetActive(true);
        authModeText.text = "Sign Up";
        switchToOtherAuthMethodText.text = "Sign in instead";

        HideAuthErrors();
        ClearInputFields();
    }

    public void ShowSignInForm()
    {
        currentAuthMode = AuthMode.SignIn;
        authScreen.SetActive(true);
        usernameInputField.gameObject.SetActive(false);
        authModeText.text = "Sign In";
        switchToOtherAuthMethodText.text = "Sign up instead";

        HideAuthErrors();
        ClearInputFields();
    }

    public void SwitchAuthMethod()
    {
        switch (currentAuthMode)
        {
            case AuthMode.SignUp:
                ShowSignInForm();
                break;

            case AuthMode.SignIn:
                ShowSignUpForm();
                break;

            default:
                Debug.LogWarning("No auth mode selected.");
                break;
        }
    }

private void ShowAuthError()
    {
        if (authErrorCard != null)
            authErrorCard.SetActive(true);

        if (signUpErrorMessage != null)
            signUpErrorMessage.SetActive(currentAuthMode == AuthMode.SignUp);

        if (signInErrorMessage != null)
            signInErrorMessage.SetActive(currentAuthMode == AuthMode.SignIn);
    }
    
    public void SubmitAuth()
    {
        HideAuthErrors();
        switch (currentAuthMode)
        {
            case AuthMode.SignUp:
                _ = SignUpWithUsernamePasswordAsync();
                break;

            case AuthMode.SignIn:
                _ = SignInWithUsernamePasswordAsync();
                break;

            default:
                Debug.LogWarning("No auth mode selected.");
                break;
        }
    }

    public void SignInAnonymous()
    {
        _ = SignInAnonymouslyAsync();
    }
    
    async Task SaveUserToFirestore(string firebaseId, string unityPlayerId, string userName, string userEmail)
    {
        var data = new Dictionary<String, object>
        {
            { "firebaseUid", firebaseId },
            { "unityPlayerId", unityPlayerId },
            { "userName", userName },
            { "userEmail", userEmail},
            { "createdAt", Timestamp.GetCurrentTimestamp() }
        };

        await db.Collection("players")
            .Document(firebaseId)
            .SetAsync(data);
        
        
    }
    
    async Task SignInCachedUserAsync()
    {
        if (!AuthenticationService.Instance.SessionTokenExists)
        {
            return;
            // session token does not exist, player needs to log in via username and password
        }

        try
        {
            loginScreen.SetActive(false);
            if (!AuthenticationService.Instance.SessionTokenExists)
            {
                Debug.Log("No Unity cached session found");
                return;
            }
            
            if (firebaseAuth.CurrentUser == null)
            {
                Debug.LogWarning("Unity cache exists, but Firebase cache is empty");
                AuthenticationService.Instance.SignOut(true); // Wipes Unity session token
                return;
            }
            
            
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Unity session restored successfully. PlayerId: {AuthenticationService.Instance.PlayerId}");

            string cachedFirebaseUid = firebaseAuth.CurrentUser.UserId;
        
            GameSession.LocalPlayer = new PlayerIdentity(cachedFirebaseUid, AuthenticationService.Instance.PlayerId);
        
            GameSession.LocalPlayer.userName = await FirebaseUtil.GetUserNameAsync(cachedFirebaseUid);
        
            Debug.Log($"Firebase session restored successfully: {cachedFirebaseUid}");

            await LoadMainMenu();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
        }
    }

    async Task LoadMainMenu()
    {
        authScreen.SetActive(false);
        loadingMessage.gameObject.SetActive(true);

        await FirebaseUtil.FetchPlayerInventory(GameSession.LocalPlayer.firebasePlayerId);
        
        await Task.Delay(1000);
        
        SceneManager.LoadScene("MainMenu");
    }

    public async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Unity Anonymous SignIn was successful.");

            var firebaseUser = await firebaseAuth.SignInAnonymouslyAsync();
            Debug.Log($"Firebase Anonymous ID: {firebaseUser.User.UserId}");
            
            string guestName = "Guest_" + UnityEngine.Random.Range(1000, 9999);
            
            await SaveUserToFirestore(
                firebaseUser.User.UserId,
                AuthenticationService.Instance.PlayerId,
                guestName,
                "anonymous user"
            );
            
            Debug.Log($"Anonymous user saved to Firestore as {guestName}");
            
            GameSession.LocalPlayer = new PlayerIdentity(firebaseUser.User.UserId, AuthenticationService.Instance.PlayerId);

            await LoadMainMenu();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public async Task SignUpWithUsernamePasswordAsync()
    {
        bool unityCreated = false;
        FirebaseUser firebaseUser = null;

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(usernameInputField.text, passwordInputField.text);
            
            unityCreated = true;

            Debug.Log("Unity signup successful");

            var authResult = await firebaseAuth.CreateUserWithEmailAndPasswordAsync(emailInputField.text, passwordInputField.text);

            firebaseUser = authResult.User;

            Debug.Log("Firebase signup successful");

            await SaveUserToFirestore(firebaseUser.UserId, AuthenticationService.Instance.PlayerId, usernameInputField.text, emailInputField.text);

            Debug.Log("Firestore save complete");

            GameSession.LocalPlayer = new PlayerIdentity(firebaseUser.UserId, AuthenticationService.Instance.PlayerId);
            GameSession.LocalPlayer.userName = usernameInputField.text;

            await LoadMainMenu();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            await RollbackSignup(unityCreated, firebaseUser);
            
            ShowAuthError();
        }
    }

    private async Task RollbackSignup(bool unityCreated, FirebaseUser firebaseUser)
    {
        try
        {
            if (firebaseUser != null)
            {
                await firebaseUser.DeleteAsync();
                Debug.Log("Firebase account rolled back");
            }

            if (unityCreated)
            {
                await AuthenticationService.Instance.DeleteAccountAsync();
                Debug.Log("Unity Account rolled back");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
    
    public async Task SignInWithUsernamePasswordAsync()
    {
        try
        {
            var firebaseUser = await firebaseAuth.SignInWithEmailAndPasswordAsync(emailInputField.text, passwordInputField.text);
            Debug.Log($"Firebase UID: {firebaseUser.User.UserId}");

            string userName = await FirebaseUtil.GetUserNameAsync(firebaseUser.User.UserId);
        
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(userName, passwordInputField.text);
            Debug.Log("Unity SignIn is successful.");
        
            GameSession.LocalPlayer = new PlayerIdentity(firebaseUser.User.UserId, AuthenticationService.Instance.PlayerId);
            GameSession.LocalPlayer.userName = userName;
            
            Debug.Log("SignIn is successful.");
            
            await LoadMainMenu();
        } 
        catch (AuthenticationException ex) 
        {
            Debug.LogException(ex);
            ShowAuthError();
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
            ShowAuthError();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            ShowAuthError();
        }
    }
}