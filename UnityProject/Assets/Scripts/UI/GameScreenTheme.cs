using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;


namespace UI
{
    [ExecuteAlways]
    public class GameScreenTheme : MonoBehaviour
    {
        [Header("Design System")]
        [SerializeField] private ColorPalette colorPalette;
        [SerializeField] private Typography typography;

        [Header("Backgrounds / Images")]
        [SerializeField] private Image[] gameScreenBackground;
        [SerializeField] private Image[] cardScreenBackground;
        [SerializeField] private Image[] buttonBackground;

        [Header("Texts")]
        [SerializeField] private TMP_Text[] titleTexts;
        [SerializeField] private TMP_Text[] primaryTexts;
        [SerializeField] private TMP_Text[] secondaryTexts;
        
        private void OnEnable()
        {
            if (typography != null && colorPalette != null)
            {
                typography.OnTypographyChanged += ApplyTheme;
                colorPalette.OnColorChanged += ApplyTheme;
            }

            ApplyTheme();
        }

        private void OnDisable()
        {
            if (typography != null && colorPalette != null)
            {
                typography.OnTypographyChanged -= ApplyTheme;
                colorPalette.OnColorChanged -= ApplyTheme;
            }
        }

        private void OnValidate()
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            ApplyColors();
            ApplyFonts();
        }

        private void ApplyColors()
        {
            if (colorPalette == null)
                return;
            
            ApplyImageColor(gameScreenBackground, colorPalette.backgroundColor);
            ApplyImageColor(cardScreenBackground, colorPalette.backgroundCard);
            ApplyImageColor(buttonBackground, colorPalette.accentPrimary);
            
            ApplyTextColor(titleTexts, colorPalette.textPrimary);
            ApplyTextColor(primaryTexts, colorPalette.textPrimary);
            ApplyTextColor(secondaryTexts, colorPalette.textSecondary);
        }

        private void ApplyFonts()
        {
            if (typography == null)
                return;
            
            ApplyTextGroup(titleTexts, typography.titleFont, typography.titleBodySize, typography.boldStyle);
            ApplyTextGroup(primaryTexts, typography.bodyFont, typography.primaryBodySize, typography.normalStyle);
            ApplyTextGroup(secondaryTexts, typography.bodyFont, typography.secondaryBodySize, typography.normalStyle);
        }
        
        private void ApplyTextGroup(TMP_Text[] texts, TMP_FontAsset font, float size, FontStyles style)
        {
            if (texts == null)
                return;

            foreach (var text in texts)
            {
                if (text == null)
                    continue;

                if (font != null)
                    text.font = font;

                text.fontSize = size;
                text.fontStyle = style;
            }
        }
        
        private void ApplyTextColor(TMP_Text[] texts, Color color)
        {
            if (texts == null)
                return;

            foreach (var text in texts)
            {
                if (text == null)
                    continue;

                text.color = color;
            }
        }
        
        private void ApplyImageColor(Image[] cards, Color color)
        {
            if (cards == null)
                return;

            foreach (var card in cards)
            {
                if (card == null)
                    continue;

                card.color = color;
            }
        }
        
    }
}