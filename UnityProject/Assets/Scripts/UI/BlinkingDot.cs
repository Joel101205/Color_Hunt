using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BlinkingDot : MonoBehaviour
{
    [Header("Blink Settings")] 
    public float blinkDelay = 2.0f;
    public float blinkInterval = 0.5f;

    private Image dotImage;

    private void Start()
    {
        dotImage = GetComponent<Image>();
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        dotImage.enabled = false;
        
        yield return new WaitForSecondsRealtime(blinkDelay);
        
        while (true)
        {
            dotImage.enabled = true;
            yield return new WaitForSecondsRealtime(blinkInterval);

            dotImage.enabled = false;
            yield return new WaitForSecondsRealtime(blinkInterval);
        }
    }

    private void OnDisable()
    {
        dotImage.enabled = true;
    }
}