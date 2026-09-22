using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(RectTransform))]
public class CanvasZoomer : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float minZoom = 1f;
    public float maxZoom = 10f;
    [SerializeField] private float zoomDuration = 0.5f;

    [Header("PC Settings")]
    public float mouseZoomSpeed = 0.005f;

    [Header("Mobile Settings")]
    public float touchZoomSpeed = 0.005f;
    [SerializeField] private float maxTouchZoomStepPerFrame = 0.35f;

    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;

    private RectTransform contentRect;
    private RectTransform viewportRect;
    private Canvas rootCanvas;
    private InputAction scrollAction;
    private float currentScrollY;

    private Coroutine zoomCoroutine;
    private bool pinchInProgress;
    private bool scrollRectWasEnabledBeforePinch = true;
    private float ignoreMouseWheelUntil;

    private Camera UiCamera
    {
        get
        {
            if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
        }
    }

    private void Awake()
    {
        contentRect = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();

        if (scrollRect == null)
        {
            scrollRect = GetComponentInParent<ScrollRect>();
        }

        if (scrollRect == null)
        {
            Debug.LogError($"{nameof(CanvasZoomer)} needs a ScrollRect reference.", this);
            enabled = false;
            return;
        }

        if (scrollRect.content == null)
        {
            scrollRect.content = contentRect;
        }
        else if (scrollRect.content != contentRect)
        {
            Debug.LogWarning($"{nameof(CanvasZoomer)} is on '{name}', but the ScrollRect content is '{scrollRect.content.name}'. Put this script on the ScrollRect content object or assign the same RectTransform as ScrollRect.content.", this);
        }

        viewportRect = scrollRect.viewport != null
            ? scrollRect.viewport
            : scrollRect.GetComponent<RectTransform>();

        scrollAction = new InputAction("Canvas Zoom", binding: "<Mouse>/scroll");
        scrollAction.performed += ctx => currentScrollY = ctx.ReadValue<Vector2>().y;
        scrollAction.canceled += _ => currentScrollY = 0f;
    }

    private void OnEnable()
    {
        scrollAction?.Enable();
    }

    private void OnDisable()
    {
        scrollAction?.Disable();

        if (pinchInProgress && scrollRect != null)
        {
            scrollRect.enabled = scrollRectWasEnabledBeforePinch;
        }

        pinchInProgress = false;
    }

    private void OnDestroy()
    {
        scrollAction?.Dispose();
    }

    private void Update()
    {
        HandleTouchPinch();
        HandleMouseWheel();
    }

    private void LateUpdate()
    {
        ClampContentToBounds();
    }

    public void ZoomAndCenterOnRect(RectTransform targetRect, float targetZoom)
    {
        if (targetRect == null)
        {
            return;
        }

        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
        }

        zoomCoroutine = StartCoroutine(AnimateZoomAndCenter(targetRect, targetZoom, zoomDuration));
    }

    private IEnumerator AnimateZoomAndCenter(RectTransform targetRect, float targetZoom, float duration)
    {
        Vector3 startScale = contentRect.localScale;
        Vector3 startPosition = contentRect.position;

        float clampedZoom = Mathf.Clamp(targetZoom, GetDynamicMinZoom(), maxZoom);
        Vector3 targetScale = new Vector3(clampedZoom, clampedZoom, 1f);

        contentRect.localScale = targetScale;
        Canvas.ForceUpdateCanvases();

        Vector3 viewportCenter = GetWorldCenter(viewportRect);
        Vector3 targetCenter = GetWorldCenter(targetRect);
        contentRect.position += viewportCenter - targetCenter;
        ClampContentToBounds();

        Vector3 endPosition = contentRect.position;

        contentRect.localScale = startScale;
        contentRect.position = startPosition;

        if (duration <= 0f)
        {
            contentRect.localScale = targetScale;
            contentRect.position = endPosition;
            ClampContentToBounds();
            zoomCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            contentRect.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            contentRect.position = Vector3.LerpUnclamped(startPosition, endPosition, t);

            yield return null;
        }

        contentRect.localScale = targetScale;
        contentRect.position = endPosition;
        ClampContentToBounds();
        zoomCoroutine = null;
    }

    private void HandleMouseWheel()
    {
        if (Mathf.Abs(currentScrollY) <= 0.1f)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        float scrollY = currentScrollY;
        currentScrollY = 0f;

        if (Time.unscaledTime < ignoreMouseWheelUntil || !ScreenPointIsInsideViewport(mousePosition))
        {
            return;
        }

        ApplyZoomDeltaAtScreenPoint(scrollY * mouseZoomSpeed, mousePosition);
    }

    private void HandleTouchPinch()
    {
        int activeTouchCount = GetActiveTouchCount(out TouchControl touch0, out TouchControl touch1);

        if (activeTouchCount >= 2)
        {
            BeginPinchIfNeeded();

            Vector2 touch0Position = touch0.position.ReadValue();
            Vector2 touch1Position = touch1.position.ReadValue();
            Vector2 midpoint = (touch0Position + touch1Position) * 0.5f;

            if (!ScreenPointIsInsideViewport(midpoint))
            {
                return;
            }

            TouchPhase phase0 = touch0.phase.ReadValue();
            TouchPhase phase1 = touch1.phase.ReadValue();

            if (phase0 == TouchPhase.Began || phase1 == TouchPhase.Began)
            {
                return;
            }

            Vector2 previousTouch0Position = touch0Position - touch0.delta.ReadValue();
            Vector2 previousTouch1Position = touch1Position - touch1.delta.ReadValue();

            float previousDistance = Vector2.Distance(previousTouch0Position, previousTouch1Position);
            float currentDistance = Vector2.Distance(touch0Position, touch1Position);

            if (previousDistance <= 0.01f)
            {
                return;
            }

            float zoomDelta = (currentDistance - previousDistance) * touchZoomSpeed;
            zoomDelta = Mathf.Clamp(zoomDelta, -maxTouchZoomStepPerFrame, maxTouchZoomStepPerFrame);
            ApplyZoomDeltaAtScreenPoint(zoomDelta, midpoint);
            return;
        }

        if (pinchInProgress)
        {
            EndPinch();
        }
    }

    private void BeginPinchIfNeeded()
    {
        if (pinchInProgress)
        {
            SetScrollRectEnabled(false);
            return;
        }

        pinchInProgress = true;
        scrollRectWasEnabledBeforePinch = scrollRect.enabled;
        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;
        SetScrollRectEnabled(false);
    }

    private void EndPinch()
    {
        pinchInProgress = false;
        ignoreMouseWheelUntil = Time.unscaledTime + 0.05f;

        if (scrollRect == null)
        {
            return;
        }

        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;
        SetScrollRectEnabled(scrollRectWasEnabledBeforePinch);
    }

    private void SetScrollRectEnabled(bool value)
    {
        if (scrollRect != null && scrollRect.enabled != value)
        {
            scrollRect.enabled = value;
        }
    }

    private int GetActiveTouchCount(out TouchControl firstTouch, out TouchControl secondTouch)
    {
        firstTouch = null;
        secondTouch = null;

        if (Touchscreen.current == null)
        {
            return 0;
        }

        int activeCount = 0;
        var touches = Touchscreen.current.touches;

        for (int i = 0; i < touches.Count; i++)
        {
            TouchControl touch = touches[i];
            TouchPhase phase = touch.phase.ReadValue();

            bool isActive = touch.press.isPressed
                && phase != TouchPhase.None
                && phase != TouchPhase.Ended
                && phase != TouchPhase.Canceled;

            if (!isActive)
            {
                continue;
            }

            if (activeCount == 0)
            {
                firstTouch = touch;
            }
            else if (activeCount == 1)
            {
                secondTouch = touch;
            }

            activeCount++;
        }

        return activeCount;
    }

    private void ApplyZoomDeltaAtScreenPoint(float zoomDelta, Vector2 screenPoint)
    {
        float currentScale = contentRect.localScale.x;
        float targetScale = Mathf.Clamp(currentScale * Mathf.Exp(zoomDelta), GetDynamicMinZoom(), maxZoom);

        if (Mathf.Approximately(currentScale, targetScale))
        {
            return;
        }

        SetZoomAtScreenPoint(targetScale, screenPoint);
    }

    private void SetZoomAtScreenPoint(float targetScale, Vector2 screenPoint)
    {
        Camera uiCamera = UiCamera;

        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(contentRect, screenPoint, uiCamera, out Vector3 worldPointBeforeZoom))
        {
            return;
        }

        Vector3 localPoint = contentRect.InverseTransformPoint(worldPointBeforeZoom);

        contentRect.localScale = new Vector3(targetScale, targetScale, 1f);
        Canvas.ForceUpdateCanvases();

        Vector3 worldPointAfterZoom = contentRect.TransformPoint(localPoint);
        contentRect.position += worldPointBeforeZoom - worldPointAfterZoom;

        ClampContentToBounds();
    }

    private float GetDynamicMinZoom()
    {
        if (viewportRect == null || contentRect == null)
        {
            return minZoom;
        }

        float contentWidth = contentRect.rect.width;
        float contentHeight = contentRect.rect.height;

        if (contentWidth <= 0f || contentHeight <= 0f)
        {
            return minZoom;
        }

        float minScaleX = viewportRect.rect.width / contentWidth;
        float minScaleY = viewportRect.rect.height / contentHeight;
        return Mathf.Min(maxZoom, Mathf.Max(minZoom, minScaleX, minScaleY));
    }

    private void ClampContentToBounds()
    {
        if (contentRect == null || viewportRect == null)
        {
            return;
        }

        GetWorldMinMax(contentRect, out Vector3 contentMin, out Vector3 contentMax);
        GetWorldMinMax(viewportRect, out Vector3 viewportMin, out Vector3 viewportMax);

        Vector3 shift = Vector3.zero;
        shift.x = GetAxisShift(contentMin.x, contentMax.x, viewportMin.x, viewportMax.x);
        shift.y = GetAxisShift(contentMin.y, contentMax.y, viewportMin.y, viewportMax.y);

        if (shift.sqrMagnitude > 0.000001f)
        {
            contentRect.position += shift;
        }
    }

    private static float GetAxisShift(float contentMin, float contentMax, float viewportMin, float viewportMax)
    {
        float contentSize = contentMax - contentMin;
        float viewportSize = viewportMax - viewportMin;

        if (contentSize <= viewportSize)
        {
            float contentCenter = (contentMin + contentMax) * 0.5f;
            float viewportCenter = (viewportMin + viewportMax) * 0.5f;
            return viewportCenter - contentCenter;
        }

        if (contentMin > viewportMin)
        {
            return viewportMin - contentMin;
        }

        if (contentMax < viewportMax)
        {
            return viewportMax - contentMax;
        }

        return 0f;
    }

    private bool ScreenPointIsInsideViewport(Vector2 screenPoint)
    {
        return viewportRect != null
            && RectTransformUtility.RectangleContainsScreenPoint(viewportRect, screenPoint, UiCamera);
    }

    private static Vector3 GetWorldCenter(RectTransform rectTransform)
    {
        GetWorldMinMax(rectTransform, out Vector3 min, out Vector3 max);
        return (min + max) * 0.5f;
    }

    private static void GetWorldMinMax(RectTransform rectTransform, out Vector3 min, out Vector3 max)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        min = corners[0];
        max = corners[0];

        for (int i = 1; i < corners.Length; i++)
        {
            min = Vector3.Min(min, corners[i]);
            max = Vector3.Max(max, corners[i]);
        }
    }

    private void OnValidate()
    {
        minZoom = Mathf.Max(0.01f, minZoom);
        maxZoom = Mathf.Max(minZoom, maxZoom);
        mouseZoomSpeed = Mathf.Max(0f, mouseZoomSpeed);
        touchZoomSpeed = Mathf.Max(0f, touchZoomSpeed);
        maxTouchZoomStepPerFrame = Mathf.Max(0.01f, maxTouchZoomStepPerFrame);
        zoomDuration = Mathf.Max(0f, zoomDuration);
    }
}
