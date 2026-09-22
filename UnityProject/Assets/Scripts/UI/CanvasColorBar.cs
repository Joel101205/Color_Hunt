using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class CanvasColorBar : MonoBehaviour
{
    [SerializeField] private GameObject selectorRect;
    private Button[] colorButtons;
    private TMP_Text[] amountOfColorDisplay;
    private int selectedIndex = 0;
    
    void Start()
    {
        colorButtons = GetComponentsInChildren<Button>();
        amountOfColorDisplay = new TMP_Text[colorButtons.Length];

        for (int i = 0; i < colorButtons.Length; i++)
        {
            int index = i;
            colorButtons[i].onClick.AddListener(()=> ColorButtonPress(index));
            amountOfColorDisplay[i] = colorButtons[i].GetComponentInChildren<TextMeshProUGUI>();
        }

        UpdateAllDisplays();
    }
    
    private void OnEnable()
    {
        if (GameSession.LocalPlayer != null && GameSession.LocalPlayer.colorInventory != null)
        {
            GameSession.LocalPlayer.colorInventory.OnInventoryChanged += UpdateAllDisplays;
        }
    }

    private void OnDisable()
    {
        if (GameSession.LocalPlayer != null && GameSession.LocalPlayer.colorInventory != null)
        {
            GameSession.LocalPlayer.colorInventory.OnInventoryChanged -= UpdateAllDisplays;
        }
    }

    private void ColorButtonPress(int index)
    {
        selectedIndex = index;
        CanvasUIManager.selectedColor = colorButtons[index].image.color;
        float buttonX = colorButtons[index].GetComponent<RectTransform>().anchoredPosition.x;
        selectorRect.GetComponent<RectTransform>().anchoredPosition = new Vector2(buttonX, 0);
    }

    private void UpdateAllDisplays()
    {
        for (int i = 0; i < colorButtons.Length; i++)
        {
            Color buttonColor = colorButtons[i].image.color;
            int currentAmount = GameSession.LocalPlayer.colorInventory.GetAvailableAmount(ColorInventory.ColorToHex(buttonColor));
            amountOfColorDisplay[i].text = currentAmount.ToString();
        }
    }
}