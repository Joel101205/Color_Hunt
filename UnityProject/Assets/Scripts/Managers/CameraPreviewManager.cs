using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Threading.Tasks;

public class CameraPreviewManager : MonoBehaviour
{
    public event Action PhotoSubmitted;
    private WebCamTexture _webCamTexture;
    private Color _avgColor; 
    private Texture2D _capturedPhoto;
    private byte[] _capturedPhotoJpg;
    
    [SerializeField] private GameObject postGameScreen;
    [SerializeField] private GameObject gameScreen;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private GameObject retakeAndConfirmButtons;
    [SerializeField] private GameObject takePhotoButton;
    [SerializeField] private GameObject colorPicker;
    private DraggableColorPicker _draggablePickerComponent;

    public Texture2D capturedPhoto => _capturedPhoto;
    private bool _isSubmitting = false;
    
    void Start()
    {
        takePhotoButton.SetActive(true);
        retakeAndConfirmButtons.SetActive(false);
        colorPicker.SetActive(false);
        
        if (previewImage == null) previewImage = GetComponent<RawImage>();

        if (previewImage == null)
        {
            Debug.LogError("No RawImage assigned or found for camera preview.");
            return;
        }
        
        _draggablePickerComponent = colorPicker.GetComponentInChildren<DraggableColorPicker>(true);
        if (_draggablePickerComponent != null)
        {
            _draggablePickerComponent.OnColorPicked += HandleColorPicked;
        }
        else
        {
            Debug.LogWarning("DraggableColorPicker component not found on the colorPicker GameObject.");
        }

        StartCameraPreview();
    }

    private void HandleColorPicked(Color newColor)
    {
        _avgColor = newColor;
    }

    private void StartCameraPreview()
    {
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogError("No webcam/camera device found.");
            return;
        }

        WebCamDevice device = WebCamTexture.devices[0];

        _webCamTexture = new WebCamTexture(device.name);
        previewImage.texture = _webCamTexture;
        _webCamTexture.Play();

        StartCoroutine(ApplyRotationWhenReady());
    }
    
    private IEnumerator ApplyRotationWhenReady()
    {
        while (_webCamTexture.width < 100) yield return null;

        int rotationAngle = -_webCamTexture.videoRotationAngle;
        previewImage.rectTransform.localEulerAngles = new Vector3(0, 0, rotationAngle);
    }
    
    public void TakePhoto()
    {
        if (_webCamTexture == null || !_webCamTexture.isPlaying) return;
        if (_webCamTexture.width < 100 || _webCamTexture.height < 100) return;
        
        Texture2D rawPhoto = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGB24, false);
        rawPhoto.SetPixels(_webCamTexture.GetPixels());
        rawPhoto.Apply();
        
        int rotationAngle = _webCamTexture.videoRotationAngle;
        _capturedPhoto = RotateTexture(rawPhoto, 360 - rotationAngle);
        
        previewImage.rectTransform.localEulerAngles = Vector3.zero;
        previewImage.rectTransform.localScale = Vector3.one;
        previewImage.texture = _capturedPhoto;
        
        _webCamTexture.Pause();
        
        takePhotoButton.SetActive(false);
        retakeAndConfirmButtons.SetActive(true);
        colorPicker.SetActive(true);

        if (_draggablePickerComponent != null)
        {
            _draggablePickerComponent.ResetToCenter();
        }
    }

    public void RetakePhoto()
    {
        if (_isSubmitting) return;
        retakeAndConfirmButtons.SetActive(false);
        takePhotoButton.SetActive(true);
        colorPicker.SetActive(false);
        _webCamTexture.Play();
        previewImage.texture = _webCamTexture;
        StartCoroutine(ApplyRotationWhenReady());
    }

    public async void SubmitPhoto()
    {
        if (_capturedPhoto == null || _isSubmitting) return;

        if (_draggablePickerComponent != null)
        {
            _avgColor = _draggablePickerComponent.CurrentPickedColor;
        }

        if (_avgColor.a < 0.1f || _avgColor == Color.black || _avgColor == Color.clear)
        {
            _avgColor = _capturedPhoto.GetPixel(_capturedPhoto.width / 2, _capturedPhoto.height / 2);
            Debug.LogWarning("Color picker failed to supply a color, sampling center pixel instead");
        }

        _isSubmitting = true;
        retakeAndConfirmButtons.SetActive(false);
        colorPicker.SetActive(false);
        GameSession.OwnResult.colorAccuracyPercent = GetColorMatchPercentage(_avgColor, GameSession.CurrentMatch.hexCode);
        await ProcessAndUploadPhoto();
    }

    public async void ForceSubmitBlackPicture()
    {
        if (_isSubmitting) return;
        _isSubmitting = true;

        if (_webCamTexture != null) _webCamTexture.Stop();

        _capturedPhoto = new Texture2D(100, 100, TextureFormat.RGB24, false);
        Color[] blackPixels = new Color[10000];
        for (int i = 0; i < blackPixels.Length; i++) blackPixels[i] = Color.black;
        _capturedPhoto.SetPixels(blackPixels);
        _capturedPhoto.Apply();

        _avgColor = Color.black;
        GameSession.OwnResult.colorAccuracyPercent = 0f;
        await ProcessAndUploadPhoto();
    }

    private async Task ProcessAndUploadPhoto()
    {
        GameSession.OwnResult.localPhotoTexture = _capturedPhoto;
        GameSession.OwnResult.submittedColor = _avgColor;
        _capturedPhotoJpg = _capturedPhoto.EncodeToJPG(75);
        
        try 
        {
            await FirebaseUtil.UploadImage(GameSession.LocalPlayer.firebasePlayerId, GameSession.CurrentMatch.activeGameDocId, _capturedPhotoJpg);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to upload image: {e.Message}");
            _isSubmitting = false; // Allow retry if upload fails
            return; 
        }
        
        if (_webCamTexture != null) _webCamTexture.Stop();
        
        gameScreen.SetActive(false);
        postGameScreen.SetActive(true);
        PhotoSubmitted?.Invoke();
    }
    
    private static float GetColorMatchPercentage(Color avgColor, string hexCode)
    {
        if (string.IsNullOrWhiteSpace(hexCode))
        {
            Debug.LogError("Hex code is empty. Defaulting to 0% match.");
            return 0f;
        }
        
        if (!hexCode.StartsWith("#"))
        {
            hexCode = "#" + hexCode;
        }
        
        if (!ColorUtility.TryParseHtmlString(hexCode, out Color targetColor))
        {
            Debug.LogError($"Invalid hex code: {hexCode}. Defaulting to 0% match.");
            return 0f;
        }

        float rDiff = avgColor.r - targetColor.r;
        float gDiff = avgColor.g - targetColor.g;
        float bDiff = avgColor.b - targetColor.b;

        float distance = Mathf.Sqrt(
            (rDiff * rDiff) +
            (gDiff * gDiff) +
            (bDiff * bDiff)
        );

        float normalizedDistance;
        if (GameSession.CurrentMatch.activeMatchSettings != null)
        {
            normalizedDistance = Mathf.Clamp01(distance / GameSession.CurrentMatch.activeMatchSettings.ColorCutoff);
        }
        else
        {
            normalizedDistance = Mathf.Clamp01(distance / 1.1f);
        }

        float accuracy;
        if (GameSession.CurrentMatch.activeMatchSettings != null)
        {
            accuracy = Mathf.Pow(1f - normalizedDistance, GameSession.CurrentMatch.activeMatchSettings.ColorCutoffPow);
        }
        else
        {
            accuracy = Mathf.Pow(1f - normalizedDistance, 1.1f);
        }

        return Mathf.Round(accuracy * 100f);
    }
    
    private void OnDestroy()
    {
        if (_draggablePickerComponent != null)
        {
            _draggablePickerComponent.OnColorPicked -= HandleColorPicked;
        }

        if (_webCamTexture != null)
        {
            _webCamTexture.Stop(); 
            Destroy(_webCamTexture);
            _webCamTexture = null;
        }
    }
    
    private Texture2D RotateTexture(Texture2D source, int rotationAngle)
    {
        rotationAngle = ((rotationAngle % 360) + 360) % 360;

        if (rotationAngle == 90) return RotateTexture90Clockwise(source);
        if (rotationAngle == 180) return RotateTexture180(source);
        if (rotationAngle == 270) return RotateTexture90CounterClockwise(source);

        return source;
    }

    private Texture2D RotateTexture90Clockwise(Texture2D source)
    {
        int sourceWidth = source.width;
        int sourceHeight = source.height;
        Texture2D rotated = new Texture2D(sourceHeight, sourceWidth, source.format, false);
        for (int y = 0; y < sourceHeight; y++)
        {
            for (int x = 0; x < sourceWidth; x++)
            {
                rotated.SetPixel(sourceHeight - y - 1, x, source.GetPixel(x, y));
            }
        }
        rotated.Apply();
        return rotated;
    }

    private Texture2D RotateTexture90CounterClockwise(Texture2D source)
    {
        int sourceWidth = source.width;
        int sourceHeight = source.height;
        Texture2D rotated = new Texture2D(sourceHeight, sourceWidth, source.format, false);
        for (int y = 0; y < sourceHeight; y++)
        {
            for (int x = 0; x < sourceWidth; x++)
            {
                rotated.SetPixel(y, sourceWidth - x - 1, source.GetPixel(x, y));
            }
        }
        rotated.Apply();
        return rotated;
    }

    private Texture2D RotateTexture180(Texture2D source)
    {
        int sourceWidth = source.width;
        int sourceHeight = source.height;
        Texture2D rotated = new Texture2D(sourceWidth, sourceHeight, source.format, false);
        for (int y = 0; y < sourceHeight; y++)
        {
            for (int x = 0; x < sourceWidth; x++)
            {
                rotated.SetPixel(sourceWidth - x - 1, sourceHeight - y - 1, source.GetPixel(x, y));
            }
        }
        rotated.Apply();
        return rotated;
    }
}