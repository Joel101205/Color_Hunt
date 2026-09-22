using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class PixelPainter : MonoBehaviour
{
    [Header("UI Setup")]
    public RawImage displayImage;
    public Texture2D sourceTexture;

    [Header("Paint Settings")]
    public int blockSize = 24;
    public float animationDuration = 2.5f;
    public Ease easeType = Ease.InOutQuad;
    public bool randomOrder = false;

    private Texture2D paintedTexture;
    private int currentIndex = 0;

    private class PixelBlock
    {
        public int x, y, width, height;
        public Color[] colors;
    }
    private List<PixelBlock> validBlocks = new List<PixelBlock>();

    void Start()
    {
        paintedTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
        paintedTexture.filterMode = FilterMode.Point; 
        
        Color32[] clearColors = new Color32[paintedTexture.width * paintedTexture.height];
        paintedTexture.SetPixels32(clearColors);
        paintedTexture.Apply();

        displayImage.texture = paintedTexture;

        PrepareBlocks();

        AnimatePainting();
    }

    void PrepareBlocks()
    {
        for (int x = 0; x < sourceTexture.width; x += blockSize)
        {
            for (int y = 0; y < sourceTexture.height; y += blockSize)
            {
                int currentBlockWidth = Mathf.Min(blockSize, sourceTexture.width - x);
                int currentBlockHeight = Mathf.Min(blockSize, sourceTexture.height - y);

                Color[] pixelChunk = sourceTexture.GetPixels(x, y, currentBlockWidth, currentBlockHeight);

                bool hasColor = false;
                foreach (Color c in pixelChunk) { if (c.a > 0.1f) { hasColor = true; break; } }

                if (hasColor)
                {
                    validBlocks.Add(new PixelBlock { x = x, y = y, width = currentBlockWidth, height = currentBlockHeight, colors = pixelChunk });
                }
            }
        }

        if (randomOrder)
        {
            for (int i = 0; i < validBlocks.Count; i++)
            {
                PixelBlock temp = validBlocks[i];
                int randomIndex = Random.Range(i, validBlocks.Count);
                validBlocks[i] = validBlocks[randomIndex];
                validBlocks[randomIndex] = temp;
            }
        }
    }

    void AnimatePainting()
    {
        
        DOVirtual.Float(0, validBlocks.Count, animationDuration, (currentTweenValue) =>
        {
            int targetIndex = Mathf.FloorToInt(currentTweenValue);
            bool paintedAnythingThisFrame = false;

            while (currentIndex < targetIndex && currentIndex < validBlocks.Count)
            {
                PixelBlock block = validBlocks[currentIndex];
                paintedTexture.SetPixels(block.x, block.y, block.width, block.height, block.colors);
                
                currentIndex++;
                paintedAnythingThisFrame = true;
            }

            if (paintedAnythingThisFrame)
            {
                paintedTexture.Apply();
            }
        }).SetEase(easeType); 
    }
}