using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.UI;

public class QueueUIManager : MonoBehaviour
{
    [SerializeField] private QueueTextScrambler opponentNameScrambler;
    [SerializeField] private EllipsisAnimator findingMatchAnimator;
    [SerializeField] private TMP_Text findingMatchText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject mainMenuScreen;
    [SerializeField] private GameObject queueScreen;
    [SerializeField] private GameObject friendScreen;
    [SerializeField] private Button cancelChallengeButton;

    private TMP_Text cancelChallengeButtonText;
    private TMP_Text cancelButtonText;
    private Coroutine matchFoundRoutine;

    private void Awake()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(false);

        if (cancelButton != null)
        {
            cancelButtonText = cancelButton.GetComponentInChildren<TMP_Text>();
            if(cancelButtonText != null) cancelButtonText.text = "Cancel Search";
        }

        if (cancelChallengeButton != null)
        {
            cancelChallengeButtonText = cancelChallengeButton.GetComponentInChildren<TMP_Text>();
            if (cancelChallengeButtonText != null) cancelChallengeButtonText.text = "Cancel Challenge";
        }
    }
    
    public void CancelMatchAndResetQueueUI()
    {
        if (cancelButton != null) cancelButton.interactable = false;
        if (cancelButtonText != null) cancelButtonText.text = "Cancelling...";

        if (cancelChallengeButton != null) cancelChallengeButton.interactable = false;
        if (cancelChallengeButtonText != null) cancelChallengeButtonText.text = "Cancelling...";
        
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (findingMatchText != null) findingMatchText.text = "Finding match";
        if (findingMatchAnimator != null) findingMatchAnimator.enabled = true;

        if (opponentNameScrambler != null)
        {
            opponentNameScrambler.GetComponent<TMP_Text>().text = "";
            opponentNameScrambler.enabled = false; 
        }
    }

    public void BackToMenuScreen()
    {
        if (cancelButton != null) cancelButton.interactable = true;
        if (cancelChallengeButton != null) cancelChallengeButton.interactable = true;
        
        if (queueScreen != null) queueScreen.SetActive(false);
        if (mainMenuScreen != null) mainMenuScreen.SetActive(true);
    }

    public void StartQueueUI()
    {
        if (mainMenuScreen != null) mainMenuScreen.SetActive(false);
        if (friendScreen != null) friendScreen.SetActive(false);
        if (queueScreen != null) queueScreen.SetActive(true);

        if (cancelButton != null) cancelButton.gameObject.SetActive(true);
        if (cancelChallengeButton != null) cancelChallengeButton.gameObject.SetActive(false);
        if (cancelButtonText != null) cancelButtonText.text = "Cancel Search";
        
        if (opponentNameScrambler != null) opponentNameScrambler.enabled = true;
    }

    public void DisplayMatchFound(string opponentName, Action onSequenceComplete)
    {
        matchFoundRoutine = StartCoroutine(MatchFoundSequence(opponentName, onSequenceComplete));
    }

    private IEnumerator MatchFoundSequence(string opponentName, Action onSequenceComplete)
    {
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);
        
        if (findingMatchAnimator != null) findingMatchAnimator.StopAnimation();
        if (findingMatchText != null) findingMatchText.text = "Match found!";
        
        if (opponentNameScrambler != null)
        {
            opponentNameScrambler.enabled = true;
            opponentNameScrambler.SetMatchFound(opponentName);
        }
        
        yield return new WaitForSeconds(1.25f);
        
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            for (int i = 3; i > 0; i--)
            {
                countdownText.text = $"Starting in {i}...";
                yield return new WaitForSeconds(1f);
            }
            countdownText.text = "Loading...";
        }

        onSequenceComplete?.Invoke();
    }
    
    public void StartFriendQueueUI(bool isHost, string opponentName)
    {
        if (friendScreen != null) friendScreen.SetActive(false);
        if (mainMenuScreen != null) mainMenuScreen.SetActive(false);
        if (queueScreen != null) queueScreen.SetActive(true);

        if (cancelButton != null) cancelButton.gameObject.SetActive(false);
        if (cancelChallengeButton != null) cancelChallengeButton.gameObject.SetActive(isHost);
        if (cancelChallengeButtonText != null) cancelChallengeButtonText.text = "Cancel Challenge";

        if (findingMatchAnimator != null) 
        {
            findingMatchAnimator.enabled = true;
            findingMatchAnimator.SetBaseText(isHost ? "Challenge sent. Waiting for Opponent" : "Connecting to match");
        }

        if (opponentNameScrambler != null)
        {
            opponentNameScrambler.enabled = true;
            opponentNameScrambler.SetMatchFound(opponentName);
        }
    }

    public void DisplayFriendMatchFound(Action onSequenceComplete)
    {
        matchFoundRoutine = StartCoroutine(FriendMatchFoundSequence(onSequenceComplete));
    }

    private IEnumerator FriendMatchFoundSequence(Action onSequenceComplete)
    {
        if (cancelChallengeButton != null) cancelChallengeButton.gameObject.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false); 
        
        if (findingMatchAnimator != null) findingMatchAnimator.StopAnimation();
        if (findingMatchText != null) findingMatchText.text = "Challenge Accepted!";
        
        yield return new WaitForSeconds(1.25f);
        
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            for (int i = 3; i > 0; i--)
            {
                countdownText.text = $"Starting in {i}...";
                yield return new WaitForSeconds(1f);
            }
            countdownText.text = "Loading...";
        }

        onSequenceComplete?.Invoke();
    }
    
    public void DisplayChallengeRejected(Action onSequenceComplete)
    {
        StartCoroutine(ChallengeRejectedSequence(onSequenceComplete));
    }

    private IEnumerator ChallengeRejectedSequence(Action onSequenceComplete)
    {
        if (cancelChallengeButton != null) cancelChallengeButton.gameObject.SetActive(false);
        if (findingMatchAnimator != null) findingMatchAnimator.StopAnimation();
        if (findingMatchText != null) findingMatchText.text = "Opponent rejected the challenge.";
        
        if (opponentNameScrambler != null)
        {
            opponentNameScrambler.StopScrambling();
            opponentNameScrambler.GetComponent<TMP_Text>().text = "";
        }
        
        yield return new WaitForSeconds(2f);
        onSequenceComplete?.Invoke();
    }
}