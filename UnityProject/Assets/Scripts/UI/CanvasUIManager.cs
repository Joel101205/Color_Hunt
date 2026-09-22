using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CanvasUIManager : MonoBehaviour
{
    [Header("Canvas Settings")]
    public RawImage canvasRawImage;
    public int canvasWidth = 50;
    public int canvasHeight = 50;
    private Texture2D boardTexture; 
    
    [SerializeField] private RectTransform selectorRect; 
    [SerializeField] private GameObject placePixelButton;
    [SerializeField] private CanvasZoomer canvasZoomer;
    [SerializeField] private float autoZoomLevel = 12f;
    [SerializeField] private CanvasNetworkManager networkManager;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text backButtonText;

    private bool canGoBack = true;
    
    public static Color selectedColor { get; set; } = Color.white;

    private int currentSelectedX = -1;
    private int currentSelectedY = -1;
    
    [Header("Interaction Settings")]
    public float dragThreshold = 15f; 

    private Vector2 pointerDownPosition;
    private void Start()
    {
        if (backButtonText != null) backButtonText.text = "Back";
        
        InitializeCanvas();
        AttachClickProxy();
        
        if (selectorRect != null) selectorRect.gameObject.SetActive(false);
        if (placePixelButton != null) placePixelButton.SetActive(false);
        
        SetNetworkBusy(false);
    }
    
    public void LoadCanvasFromBytes(byte[] imageData)
    {
        boardTexture.LoadImage(imageData);
        boardTexture.filterMode = FilterMode.Point; 
        boardTexture.Apply();
        
        canvasWidth = boardTexture.width;
        canvasHeight = boardTexture.height;
    }
    
    public void SetNetworkBusy(bool isBusy)
    {
        canGoBack = !isBusy;
        if (backButton != null)
        {
            backButton.interactable = !isBusy;
        }
    }

    private void AttachClickProxy()
    {
        RawImageClickProxy proxy = canvasRawImage.gameObject.AddComponent<RawImageClickProxy>();

        proxy.OnPointerDownAction = (PointerEventData data) => 
        {
            pointerDownPosition = data.position;
        };

        proxy.OnPointerUpAction = (PointerEventData data) => 
        {
            float distanceMoved = Vector2.Distance(pointerDownPosition, data.position);
            
            if (distanceMoved < dragThreshold)
            {
                OnCanvasClicked(data);
            }
        };
    }

    public void OnBackButtonClicked()
    {
        backButtonText.text = "Loading...";
        if (canGoBack) SceneManager.LoadScene("MainMenu");
    }

    private void InitializeCanvas()
    {
        boardTexture = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, false);
        boardTexture.filterMode = FilterMode.Point; 
        canvasRawImage.texture = boardTexture;

        Color[] defaultPixels = new Color[canvasWidth * canvasHeight];
        for (int i = 0; i < defaultPixels.Length; i++)
        {
            defaultPixels[i] = Color.white;
        }
        boardTexture.SetPixels(defaultPixels);
        boardTexture.Apply();
    }

    public void UpdateSinglePixel(int x, int y, Color newColor)
    {
        boardTexture.SetPixel(x, y, newColor);
        boardTexture.Apply(); 
    }

    public void OnCanvasClicked(PointerEventData eventData)
    {
        Vector2 localClickPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRawImage.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localClickPosition
        );

        Rect rect = canvasRawImage.rectTransform.rect;
        float normalizedX = (localClickPosition.x - rect.x) / rect.width;
        float normalizedY = (localClickPosition.y - rect.y) / rect.height;

        int pixelX = Mathf.FloorToInt(normalizedX * canvasWidth);
        int pixelY = Mathf.FloorToInt(normalizedY * canvasHeight);

        if (pixelX >= 0 && pixelX < canvasWidth && 
            pixelY >= 0 && pixelY < canvasHeight)
        {
            SelectPixel(pixelX, pixelY);
        }
    }

    private void SelectPixel(int x, int y)
    {
        currentSelectedX = x;
        currentSelectedY = y;
        
        selectorRect.gameObject.SetActive(true);
        placePixelButton.SetActive(true);

        Rect rect = canvasRawImage.rectTransform.rect;
        float pixelWidthUI = rect.width / canvasWidth;
        float pixelHeightUI = rect.height / canvasHeight;

        selectorRect.sizeDelta = new Vector2(pixelWidthUI, pixelHeightUI);

        float posX = (x * pixelWidthUI) + (pixelWidthUI / 2f);
        float posY = (y * pixelHeightUI) + (pixelHeightUI / 2f);
        
        selectorRect.anchoredPosition = new Vector2(posX, posY);
        
        if (canvasZoomer != null)
        {
            canvasZoomer.ZoomAndCenterOnRect(selectorRect, autoZoomLevel);
        }
        
    }

    public async void ConfirmPixelPlacement()
    {
        if (currentSelectedX != -1 && currentSelectedY != -1)
        {
            if (GameSession.LocalPlayer == null)
            {
                Debug.LogError("No local player found in GameSession!");
                return;
            }

            if (GameSession.LocalPlayer.colorInventory.TryUsePixel(ColorInventory.ColorToHex(selectedColor)))
            {
                Color previousColor = boardTexture.GetPixel(currentSelectedX, currentSelectedY);

                UpdateSinglePixel(currentSelectedX, currentSelectedY, selectedColor);
            
                selectorRect.gameObject.SetActive(false);
                placePixelButton.SetActive(false);
            
                if (networkManager != null)
                {
                    bool success = false;
                    try
                    {
                        success = await networkManager.UploadPixel(currentSelectedX, currentSelectedY, selectedColor);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }

                    // if upload failed, revert pixel on local canvas texture
                    if (!success)
                    {
                        UpdateSinglePixel(currentSelectedX, currentSelectedY, previousColor);
                        Debug.Log("Upload failed. Pixel reverted to previous color locally.");
                    }
                }
                
                currentSelectedX = -1;
                currentSelectedY = -1;
            }
            else
            {
                Debug.LogWarning("Not enough pixels of that color left!");
                //TODO: visualization of not having enough pixels
            }
        }
    }
    //Setter to simplify development
    public void UpdateDragThreshold(float newThreshold)
    {
        dragThreshold = newThreshold;
    }

    public void UpdateAutoZoomLevel(float newZoomLevel)
    {
        autoZoomLevel = newZoomLevel;
    }
    
}

public class RawImageClickProxy : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public System.Action<PointerEventData> OnPointerDownAction;
    public System.Action<PointerEventData> OnPointerUpAction;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnPointerDownAction?.Invoke(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnPointerUpAction?.Invoke(eventData);
    }
}