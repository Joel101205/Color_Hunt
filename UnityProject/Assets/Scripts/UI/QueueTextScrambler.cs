using UnityEngine;
using TMPro;
using System.Collections;
using System.Text;

[RequireComponent(typeof(TMP_Text))]
public class QueueTextScrambler : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Characters used to generate the random scramble effect.")]
    public string validCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
    
    [Tooltip("How fast the characters change (lower = faster)")]
    public float scrambleInterval = 0.05f;

    [Tooltip("The length of the random text string during the queue")]
    public int queueTextLength = 10;

    [Header("Transition Settings")]
    [Tooltip("How fast characters are deleted")]
    public float deletionSpeed = 0.04f;

    [Tooltip("How fast the opponents name is typed out")]
    public float typingSpeed = 0.06f;
    
    [Tooltip("How long the pause in between deletion and typing should be")]
    public float deletionPause = 0.15f;

    private TMP_Text textComponent;
    private Coroutine scrambleCoroutine;
    private Coroutine displayOpponentUsernameCoroutine;
    private bool isMatchFound = false;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        StartScrambling();
    }

    private void OnDisable()
    {
        StopScrambling();
        StopOpponentUsernameAnimation();
    }

    public void StartScrambling()
    {
        isMatchFound = false;
        
        if (scrambleCoroutine != null)
        {
            StopCoroutine(scrambleCoroutine);
        }
        
        scrambleCoroutine = StartCoroutine(ScrambleRoutine());
    }

    public void StopScrambling()
    {
        if (scrambleCoroutine != null)
        {
            StopCoroutine(scrambleCoroutine);
        }
    }

    public void StopOpponentUsernameAnimation()
    {
        if (displayOpponentUsernameCoroutine != null)
        {
            StopCoroutine(displayOpponentUsernameCoroutine);
        }
    }
    
    
    public void SetMatchFound(string opponentUsername)
    {
        isMatchFound = true;
        if (scrambleCoroutine != null)
        {
            StopCoroutine(scrambleCoroutine);
        }

        if (displayOpponentUsernameCoroutine != null)
        {
            StopCoroutine(displayOpponentUsernameCoroutine);
        }
        
        displayOpponentUsernameCoroutine = StartCoroutine(DisplayOpponentUsername(opponentUsername));
    }

    private IEnumerator DisplayOpponentUsername(string opponentUsername)
    {
        
        string currentText = textComponent.text;
        while (currentText.Length > 0)
        {
            currentText = currentText.Substring(0, currentText.Length - 1);
            textComponent.text = currentText;
            
            yield return new WaitForSeconds(deletionSpeed);
        }

        yield return new WaitForSeconds(deletionPause);

        foreach (var character in opponentUsername)
        {
            textComponent.text += character;
            
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private IEnumerator ScrambleRoutine()
    {
        StringBuilder stringBuilder = new StringBuilder();

        while (!isMatchFound)
        {
            stringBuilder.Clear();
            
            for (int i = 0; i < queueTextLength; i++)
            {
                int randomIndex = Random.Range(0, validCharacters.Length);
                stringBuilder.Append(validCharacters[randomIndex]);
            }

            textComponent.text = stringBuilder.ToString();

            yield return new WaitForSeconds(scrambleInterval);
        }
    }
}