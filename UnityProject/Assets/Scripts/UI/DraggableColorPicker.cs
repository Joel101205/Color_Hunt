using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class DraggableColorPicker : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    [Header("References")]
    [SerializeField] private CameraPreviewManager cameraManager;
    [SerializeField] private RawImage targetRawImage;
    
    [Range(0.1f, 1f)] 
    [SerializeField] private float sampleRadiusMultiplier = 1f;

    public Color CurrentPickedColor { get; private set; }
    public event Action<Color> OnColorPicked;

    private RectTransform _rectTransform;
    private RectTransform _imageRect;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _imageRect = targetRawImage.GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdatePositionAndColor(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdatePositionAndColor(eventData);
    }

    private void UpdatePositionAndColor(PointerEventData eventData)
    {
        Texture2D photo = cameraManager.capturedPhoto;
        if (photo == null) return; 

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_imageRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
        {
            Rect rect = _imageRect.rect;

            float uiRadius = (_rectTransform.rect.width / 2f) * sampleRadiusMultiplier;
            float fullUiRadius = _rectTransform.rect.width / 2f;

            localPos.x = Mathf.Clamp(localPos.x, rect.xMin + fullUiRadius, rect.xMax - fullUiRadius);
            localPos.y = Mathf.Clamp(localPos.y, rect.yMin + fullUiRadius, rect.yMax - fullUiRadius);

            _rectTransform.position = _imageRect.TransformPoint(localPos);

            float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPos.x);
            float normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPos.y);

            int texCenterX = Mathf.Clamp(Mathf.RoundToInt(normalizedX * photo.width), 0, photo.width - 1);
            int texCenterY = Mathf.Clamp(Mathf.RoundToInt(normalizedY * photo.height), 0, photo.height - 1);

            float radiusRatio = uiRadius / rect.width; 
            int texRadius = Mathf.RoundToInt(radiusRatio * photo.width);

            CurrentPickedColor = GetCircularAverageColor(photo, texCenterX, texCenterY, texRadius);
            OnColorPicked?.Invoke(CurrentPickedColor);
        }
    }
    
    public void ResetToCenter()
    {
        if (_imageRect != null)
        {
            _rectTransform.position = _imageRect.TransformPoint(Vector2.zero);
        }
        
        Texture2D photo = cameraManager.capturedPhoto;
        if (photo != null)
        {
            float uiRadius = (_rectTransform.rect.width / 2f) * sampleRadiusMultiplier;
            int texRadius = Mathf.RoundToInt((uiRadius / _imageRect.rect.width) * photo.width);
            
            CurrentPickedColor = GetCircularAverageColor(photo, photo.width / 2, photo.height / 2, texRadius);
            OnColorPicked?.Invoke(CurrentPickedColor);
        }
    }

    private Color GetCircularAverageColor(Texture2D tex, int centerX, int centerY, int radius)
    {
        int startX = Mathf.Max(0, centerX - radius);
        int endX = Mathf.Min(tex.width - 1, centerX + radius);
        int startY = Mathf.Max(0, centerY - radius);
        int endY = Mathf.Min(tex.height - 1, centerY + radius);

        int blockWidth = endX - startX + 1;
        int blockHeight = endY - startY + 1;

        Color[] pixels = tex.GetPixels(startX, startY, blockWidth, blockHeight);

        float r = 0, g = 0, b = 0;
        int count = 0;
        int radiusSq = radius * radius;

        for (int y = 0; y < blockHeight; y++)
        {
            for (int x = 0; x < blockWidth; x++)
            {
                int absX = startX + x;
                int absY = startY + y;
                
                int dx = absX - centerX;
                int dy = absY - centerY;

                if (dx * dx + dy * dy <= radiusSq)
                {
                    int index = y * blockWidth + x;
                    if (pixels[index].a > 0.1f) 
                    {
                        r += pixels[index].r;
                        g += pixels[index].g;
                        b += pixels[index].b;
                        count++;
                    }
                }
            }
        }

        if (count == 0) return Color.clear;
        return new Color(r / count, g / count, b / count, 1f); 
    }

    
    private void OnDrawGizmosSelected()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) return;

        Gizmos.matrix = rt.localToWorldMatrix;
        
        Gizmos.color = Color.green;
        
        float radius = (rt.rect.width / 2f) * sampleRadiusMultiplier;

        Gizmos.DrawWireSphere(Vector3.zero, radius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(Vector3.zero, radius * 0.05f);
    }
}