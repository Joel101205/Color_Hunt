using System;
using TMPro;
using UnityEngine;

namespace UI
{
    [CreateAssetMenu(fileName = "Typography", menuName = "Scriptable Objects/Typography")]
    public class Typography : ScriptableObject
    {
        [Header("Fonts")]
        public TMP_FontAsset titleFont;
        public TMP_FontAsset bodyFont;
        
        [Header("Font Sizes")]
        public float titleBodySize = 100f;
        public float primaryBodySize = 86f;
        public float secondaryBodySize = 58f;
        
        [Header("Styles")]
        public FontStyles boldStyle = FontStyles.Bold;
        public FontStyles normalStyle = FontStyles.Normal;
        
        public event Action OnTypographyChanged;

        private void OnValidate()
        {
            OnTypographyChanged?.Invoke();
        }
    }
}

