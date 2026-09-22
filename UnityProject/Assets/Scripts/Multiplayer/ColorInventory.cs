using UnityEngine;
using System.Collections.Generic;
using System;

public class ColorInventory
{
    private Dictionary<string, int> inventoryMap = new Dictionary<string, int>();
    public event Action OnInventoryChanged;

    public void InitializeColorInventory()
    {
        inventoryMap = new Dictionary<string, int>()
        {
            { "#FFFFFF", 0 }, // White
            { "#000000", 0 }, // Black
            { "#0000FF", 0 }, // Blue
            { "#00FFFF", 0 }, // Cyan
            { "#00FF00", 0 }, // Green
            { "#FFA500", 0 }, // Orange
            { "#FFFF00", 0 }, // Yellow
            { "#FF0000", 0 }, // Red
            { "#FF00FF", 0 }, // Magenta
            { "#8000FF", 0 }  // Purple
        };
    }


    public int GetAvailableAmount(string hexColor)
    {
        return inventoryMap.GetValueOrDefault(hexColor.ToUpper(), 0);
    }

    public bool TryUsePixel(string hexColor)
    {
        hexColor = hexColor.ToUpper();
        if (!inventoryMap.TryGetValue(hexColor, out int amount) || amount <= 0) return false;
        
        inventoryMap[hexColor]--;
        OnInventoryChanged?.Invoke();
        return true;
    }
    
    public void AddPixelAmount(string hexColor, int amountToAdd)
    {
        hexColor = hexColor.ToUpper();
        if (inventoryMap.ContainsKey(hexColor))
        {
            inventoryMap[hexColor] += amountToAdd;
            OnInventoryChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"Could not add {amountToAdd} to {hexColor}. Color not in dictionary.");
        }
    }
    
    public void SetPixelAmount(string hexColor, int amountToSet)
    {
        hexColor = hexColor.ToUpper();
        if (inventoryMap.ContainsKey(hexColor))
        {
            inventoryMap[hexColor] = amountToSet;
            OnInventoryChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"Could not set {hexColor} to {amountToSet}. Color not in dictionary.");
        }
    }



    public static string ColorToHex(Color color)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(color).ToUpper();
    }

    public static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color parsedColor))
        {
            parsedColor.a = 1f; 
            return parsedColor;
        }
        Debug.LogError("Failed to parse hex color: " + hex);
        return Color.white;
    }
}