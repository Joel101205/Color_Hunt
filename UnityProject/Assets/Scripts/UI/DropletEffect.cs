using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class DropletEffect : MonoBehaviour
{
    private static readonly int DropX = Shader.PropertyToID("_DropX");
    private static readonly int DropY = Shader.PropertyToID("_DropY");
    private static readonly int DropColor = Shader.PropertyToID("_DropColor");
    private static readonly int Progress = Shader.PropertyToID("_Progress");
    private static readonly int AspectRatio = Shader.PropertyToID("_AspectRatio");
    [SerializeField] private float xLandingPosition = 0.5f;
    [SerializeField] private float yLandingPosition = 0.5f;
    [SerializeField] private AnimationCurve fallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Animation Settings")]
    [SerializeField] private float fallDuration = 1.2f; 
    [SerializeField] private float splashDuration = 0.6f;
    
    private Image uiImage;
    private Material instancedMaterial;
    private Sequence currentTween;

    private void Start()
    {
        uiImage = GetComponent<Image>();
        
        instancedMaterial = new Material(uiImage.material);
        uiImage.material = instancedMaterial;
    }

    public void PlaySplash(Color dropletColor)
    {
        instancedMaterial.SetFloat(DropX, xLandingPosition);
        instancedMaterial.SetFloat(DropY, yLandingPosition);
        instancedMaterial.SetColor(DropColor, dropletColor);
        
        float width = uiImage.rectTransform.rect.width;
        float height = uiImage.rectTransform.rect.height;
        float aspectRatio = height > 0 ? width / height : 1f; 
    
        instancedMaterial.SetFloat(AspectRatio, aspectRatio);
        
        instancedMaterial.SetFloat(Progress, 0f);
        currentTween?.Kill();
        
        currentTween = DOTween.Sequence();
        
        currentTween.Append(instancedMaterial.DOFloat(0.5f, Progress, fallDuration)
            .SetEase(fallCurve));

        currentTween.Append(instancedMaterial.DOFloat(1f, Progress, splashDuration)
            .SetEase(Ease.OutQuad));
        
    }


    private void OnDestroy()
    {
        if (instancedMaterial != null)
        {
            Destroy(instancedMaterial);
        }
    }
}