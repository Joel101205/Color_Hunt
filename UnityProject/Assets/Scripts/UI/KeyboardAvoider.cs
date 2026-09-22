using UnityEngine;
using DG.Tweening;

public class KeyboardAvoider : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private float extraPadding = 450f;
    [SerializeField] private float animationDuration = 0.3f;
    
    private Vector3 originalPosition;
    private bool keyboardOpen = false;

    private void Start()
    {
        originalPosition = canvasRect.anchoredPosition3D;
    }

    private void Update()
    {
        if (TouchScreenKeyboard.visible)
        {
            if (!keyboardOpen)
            {
                keyboardOpen = true;
                
                float keyboardHeight = TouchScreenKeyboard.area.height;
                float canvasScaleFactor = canvasRect.GetComponentInParent<Canvas>().scaleFactor;
                float offset = (keyboardHeight / canvasScaleFactor) + extraPadding;

                Vector3 targetPos = originalPosition + new Vector3(0, offset, 0);

                canvasRect.DOKill();
                canvasRect.DOAnchorPos3D(targetPos, animationDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
            }
        }
        else if (keyboardOpen)
        {
            keyboardOpen = false;

            canvasRect.DOKill();
            canvasRect.DOAnchorPos3D(originalPosition, animationDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
        }
    }
}