using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MuralTransitionManager : MonoBehaviour
{
    [Header("Mask Transition UI")]
    [SerializeField] private GameObject transitionRoot;
    [SerializeField] private RawImage oldScreenImage;
    [SerializeField] private RectTransform circleMaskRoot;
    [SerializeField] private RawImage muralScreenImage;

    [Header("Canvas")]
    [SerializeField] private Canvas transitionCanvas;

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("벽화 상태 화면을 RenderTexture로 찍는 카메라입니다. targetTexture가 반드시 있어야 합니다.")]
    [SerializeField] private Camera muralCamera;

    [Header("Animation")]
    [SerializeField] private float transitionDuration = 1.2f;
    [SerializeField] private float finalDiameterMultiplier = 1.25f;
    [SerializeField] private Ease maskEase = Ease.OutCubic;
    [SerializeField] private bool useUnscaledTime = true;

    private Tween maskTween;
    private Texture2D oldScreenTexture;
    private bool completionInvoked;
    private bool isTransitioning;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (transitionCanvas == null && transitionRoot != null)
            transitionCanvas = transitionRoot.GetComponentInParent<Canvas>();

        SetTransitionRootActive(false);

        if (muralCamera != null)
            muralCamera.gameObject.SetActive(false);

        if (oldScreenImage != null)
            oldScreenImage.raycastTarget = false;

        if (muralScreenImage != null)
            muralScreenImage.raycastTarget = false;
    }

    private void OnDestroy()
    {
        maskTween?.Kill();
        CleanupCapturedTexture();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (transitionCanvas == null && transitionRoot != null)
            transitionCanvas = transitionRoot.GetComponentInParent<Canvas>();
    }
#endif

    /// <summary>
    /// 벽화 상호작용 시 호출.
    /// muralTransform: 원형 전환이 시작될 벽화 위치.
    /// onComplete: 실제 월드를 벽화 상태로 바꾸는 콜백.
    /// </summary>
    public void StartTransition(Transform muralTransform, Action onComplete = null)
    {
        if (isTransitioning)
            return;

        if (!CanPlayTransition(muralTransform))
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(TransitionRoutine(muralTransform, onComplete));
    }

    private IEnumerator TransitionRoutine(Transform muralTransform, Action onComplete)
    {
        isTransitioning = true;
        completionInvoked = false;

        maskTween?.Kill();
        CleanupCapturedTexture();

        SetTransitionRootActive(false);

        // 1. 기존 화면 캡처.
        yield return new WaitForEndOfFrame();
        oldScreenTexture = ScreenCapture.CaptureScreenshotAsTexture();

        // 2. 기존 화면을 UI로 덮음.
        PrepareOldScreenImage(oldScreenTexture);
        PrepareCircleMask(muralTransform);
        SetTransitionRootActive(true);

        if (circleMaskRoot != null)
            circleMaskRoot.gameObject.SetActive(false);

        // 3. 실제 월드를 벽화 상태로 변경.
        //    예: 기본 오브젝트 OFF, 벽화 오브젝트 ON.
        InvokeCompletion(onComplete);

        // 4. 벽화 상태 화면을 RenderTexture로 렌더링.
        PrepareMuralCamera();
        PrepareMuralScreenImage();

        // 5. 마스크 표시 시작.
        if (circleMaskRoot != null)
        {
            circleMaskRoot.sizeDelta = Vector2.zero;
            circleMaskRoot.gameObject.SetActive(true);
        }

        float finalDiameter = GetFinalMaskDiameter();

        bool tweenComplete = false;

        maskTween = circleMaskRoot
            .DOSizeDelta(new Vector2(finalDiameter, finalDiameter), Mathf.Max(0.01f, transitionDuration))
            .SetEase(maskEase)
            .SetUpdate(useUnscaledTime)
            .SetTarget(this)
            .OnComplete(() => tweenComplete = true);

        while (!tweenComplete)
            yield return null;

        // 6. 전환 UI 정리.
        StopTransitionVisuals();

        isTransitioning = false;
    }

    private bool CanPlayTransition(Transform muralTransform)
    {
        if (muralTransform == null)
        {
            Debug.LogWarning("벽화 전환 시작 위치 muralTransform이 없습니다.", this);
            return false;
        }

        if (transitionRoot == null)
        {
            Debug.LogWarning("MuralTransitionRoot가 연결되어 있지 않습니다.", this);
            return false;
        }

        if (oldScreenImage == null)
        {
            Debug.LogWarning("OldScreenImage가 연결되어 있지 않습니다.", this);
            return false;
        }

        if (circleMaskRoot == null)
        {
            Debug.LogWarning("CircleMaskRoot가 연결되어 있지 않습니다.", this);
            return false;
        }

        if (muralScreenImage == null)
        {
            Debug.LogWarning("MuralScreenImage가 연결되어 있지 않습니다.", this);
            return false;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("Main Camera가 연결되어 있지 않습니다.", this);
            return false;
        }

        if (muralCamera == null)
        {
            Debug.LogWarning("Mural Camera가 연결되어 있지 않습니다.", this);
            return false;
        }

        if (muralCamera.targetTexture == null)
        {
            Debug.LogWarning("Mural Camera에 targetTexture가 없습니다. RenderTexture를 연결해야 합니다.", this);
            return false;
        }

        if (transitionCanvas == null)
        {
            transitionCanvas = transitionRoot.GetComponentInParent<Canvas>();
        }

        if (transitionCanvas == null)
        {
            Debug.LogWarning("MuralTransitionCanvas를 찾을 수 없습니다.", this);
            return false;
        }

        return true;
    }

    private void PrepareOldScreenImage(Texture texture)
    {
        oldScreenImage.texture = texture;

        RectTransform rect = oldScreenImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void PrepareCircleMask(Transform muralTransform)
    {
        Vector2 maskPosition = WorldToMaskParentLocalPosition(muralTransform.position);

        circleMaskRoot.anchoredPosition = maskPosition;
        circleMaskRoot.sizeDelta = Vector2.zero;

        RectTransform muralRect = muralScreenImage.rectTransform;
        Vector2 canvasSize = GetMaskParentSize();

        muralRect.anchorMin = new Vector2(0.5f, 0.5f);
        muralRect.anchorMax = new Vector2(0.5f, 0.5f);
        muralRect.pivot = new Vector2(0.5f, 0.5f);

        // 전체 화면 크기의 벽화 화면을 마스크 안에 넣음.
        muralRect.sizeDelta = canvasSize;

        // 마스크가 중앙이 아닌 벽화 위치에서 시작하므로,
        // 자식 이미지는 반대 방향으로 밀어야 화면 정렬이 맞음.
        muralRect.anchoredPosition = -maskPosition;
    }

    private void PrepareMuralCamera()
    {
        SyncMuralCameraToMainCamera();

        muralCamera.gameObject.SetActive(true);

        // targetTexture에 현재 벽화 상태 월드를 렌더링.
        muralCamera.Render();
    }

    private void PrepareMuralScreenImage()
    {
        muralScreenImage.texture = muralCamera.targetTexture;
    }

    private Vector2 WorldToMaskParentLocalPosition(Vector3 worldPosition)
    {
        RectTransform parentRect = circleMaskRoot.parent as RectTransform;

        if (parentRect == null)
            parentRect = transitionRoot.transform as RectTransform;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCamera, worldPosition);

        Camera uiCamera = transitionCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : transitionCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPoint,
            uiCamera,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private Vector2 GetMaskParentSize()
    {
        RectTransform parentRect = circleMaskRoot.parent as RectTransform;

        if (parentRect != null)
            return parentRect.rect.size;

        RectTransform rootRect = transitionRoot.transform as RectTransform;

        if (rootRect != null)
            return rootRect.rect.size;

        return new Vector2(Screen.width, Screen.height);
    }

    private float GetFinalMaskDiameter()
    {
        Vector2 size = GetMaskParentSize();
        float diagonal = Mathf.Sqrt(size.x * size.x + size.y * size.y);
        return diagonal * Mathf.Max(1f, finalDiameterMultiplier);
    }

    private void SyncMuralCameraToMainCamera()
    {
        if (mainCamera == null || muralCamera == null)
            return;

        muralCamera.transform.SetPositionAndRotation(
            mainCamera.transform.position,
            mainCamera.transform.rotation
        );

        muralCamera.orthographic = mainCamera.orthographic;
        muralCamera.orthographicSize = mainCamera.orthographicSize;
        muralCamera.fieldOfView = mainCamera.fieldOfView;
        muralCamera.nearClipPlane = mainCamera.nearClipPlane;
        muralCamera.farClipPlane = mainCamera.farClipPlane;
        muralCamera.clearFlags = mainCamera.clearFlags;
        muralCamera.backgroundColor = mainCamera.backgroundColor;
        muralCamera.cullingMask = mainCamera.cullingMask;
    }

    private void StopTransitionVisuals()
    {
        maskTween?.Kill();
        maskTween = null;

        SetTransitionRootActive(false);

        if (muralCamera != null)
            muralCamera.gameObject.SetActive(false);

        if (oldScreenImage != null)
            oldScreenImage.texture = null;

        if (muralScreenImage != null)
            muralScreenImage.texture = null;

        CleanupCapturedTexture();
    }

    private void CleanupCapturedTexture()
    {
        if (oldScreenTexture == null)
            return;

        Destroy(oldScreenTexture);
        oldScreenTexture = null;
    }

    private void InvokeCompletion(Action onComplete)
    {
        if (completionInvoked)
            return;

        completionInvoked = true;
        onComplete?.Invoke();
    }

    private void SetTransitionRootActive(bool isActive)
    {
        if (transitionRoot != null)
            transitionRoot.SetActive(isActive);
    }
}