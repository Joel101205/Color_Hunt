using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class AnimatedButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Animation Settings")]
    [SerializeField] private float pushScale = 0.9f;
    [SerializeField] private float pushDuration = 0.1f;
    [SerializeField] private float releaseDuration = 0.2f;
    
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOKill();

        transform.DOScale(originalScale * pushScale, pushDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOKill();
        
        transform.DOScale(originalScale, releaseDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .SetLink(gameObject);
    }
}