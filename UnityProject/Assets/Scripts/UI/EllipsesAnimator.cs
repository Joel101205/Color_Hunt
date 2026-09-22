using UnityEngine;
using TMPro;
using System.Collections;
using WebSocketSharp;

[RequireComponent(typeof(TMP_Text))]
public class EllipsisAnimator : MonoBehaviour
{
    [Tooltip("The text that appears before the periods")]
    public string baseText = "";
    
    [Tooltip("How fast the periods should appear in seconds")]
    public float delayBetweenPeriods = 1.0f;

    private TMP_Text textComponent;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if(baseText.IsNullOrEmpty()) baseText = textComponent.text;
        animationCoroutine = StartCoroutine(AnimatePeriods());
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    public void StopAnimation()
    {
        if (animationCoroutine != null) 
        {
            StopCoroutine(animationCoroutine);
        }
    }

    public void SetBaseText(string newText)
    {
        baseText = newText;
        if (gameObject.activeInHierarchy && enabled)
        {
            StopAnimation();
            animationCoroutine = StartCoroutine(AnimatePeriods());
        }
    }

    private IEnumerator AnimatePeriods()
    {
        int dotCount = 0;

        while (true)
        {
            string visible   = new string('.', dotCount);
            string invisible = new string('.', 3 - dotCount); 

            textComponent.text = baseText + visible + $"<color=#00000000>{invisible}</color>"; 

            dotCount = (dotCount % 3) + 1;

            yield return new WaitForSeconds(delayBetweenPeriods);
        }
    }
}