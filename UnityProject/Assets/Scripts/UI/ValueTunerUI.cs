using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;


public class ValueTunerUI : MonoBehaviour
{
    [SerializeField] private Slider dragThresholdSlider;
    [SerializeField] private Slider autZoomLevelSlider;
    [SerializeField] private Slider minZoomSlider;
    [SerializeField] private Slider maxZoomSlider;
    [SerializeField] private Slider zoomSpeedSlider;
    [SerializeField] private Slider InertiaStopThresholdSlider;
    [SerializeField] private Slider zoomDurationSlider;
    [SerializeField] private TextMeshProUGUI values;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        updateValueText();
    }

    public void updateValueText()
    {
        values.text = "DTS. " + dragThresholdSlider.value + "\nAZL." + autZoomLevelSlider.value + "\nMI." +
                      minZoomSlider.value + "\nMA." + maxZoomSlider.value + "\nZS." +
                      zoomSpeedSlider.value + "\nIST."+ InertiaStopThresholdSlider.value+"\nZD."+ zoomDurationSlider.value;
    }
}
