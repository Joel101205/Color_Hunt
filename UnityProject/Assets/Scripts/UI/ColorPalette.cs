using System;
using UnityEngine;

namespace  UI
{
    [CreateAssetMenu(fileName = "ColorPalette", menuName = "Scriptable Objects/ColorPalette")]
    public class ColorPalette : ScriptableObject
    {
        [Header("Background Colors")]
        public Color backgroundColor = new Color(0.10f, 0.10f, 0.18f);
        public Color backgroundCard = new Color32(0x1E, 0x1E, 0x3A, 0xFF);
        
        [Header("Accent Colors")]
        public Color accentPrimary = new Color32(0xFF, 0x6B, 0x35, 0xFF);
        public Color accentSecondary = new Color32(0xFF, 0x9F, 0x1C, 0xFF);
        public Color accentDanger = new Color(0.75f, 0.0f, 0.0f);
        public Color accentSuccess = new Color(0.0f, 0.75f, 0.0f);
        
        [Header("Text Colors")]
        public Color textPrimary = Color.white;
        public Color textSecondary = new Color(1, 1, 1, 0.6f);
        
        public event Action OnColorChanged;

        private void OnValidate()
        {
            OnColorChanged?.Invoke();
        }
    }  
}