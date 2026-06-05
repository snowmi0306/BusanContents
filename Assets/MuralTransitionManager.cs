using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MuralTransitionManager : MonoBehaviour
{
    private static readonly int DistortionMainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int DistortionCenterPosId = Shader.PropertyToID("_CenterPos");
    private static readonly int DistortionRadiusId = Shader.PropertyToID("_Radius");
    private static readonly int DistortionWidthId = Shader.PropertyToID("_DistortionWidth");
    private static readonly int DistortionStrengthId = Shader.PropertyToID("_DistortionStrength");
    private static readonly int DistortionAspectRatioId = Shader.PropertyToID("_AspectRatio");
    private static readonly int DistortionAlphaId = Shader.PropertyToID("_Alpha");

    [Header("Mask Transition UI")]
    [SerializeField] private GameObject transitionRoot;
    [SerializeField] private RawImage oldScreenImage;
    [SerializeField] private RectTransform circleMaskRoot;
    [SerializeField] private RawImage muralScreenImage;

    [Header("Distortion UI")]
    [Tooltip("OldScreenImage 위, CircleMaskRoot 아래에 배치할 왜곡 전용 RawImage입니다.")]
    [SerializeField] private RawImage distortionImage;

    [Tooltip("DistortionUIShader로 만든 Material입니다. 런타임에서 복제해서 사용합니다.")]
    [SerializeField] private Material distortionMaterial;

    [Header("Ring FX")]
    [Tooltip("원 테두리 그래픽의 부모 RectTransform입니다. CircleMaskRoot와 같은 위치/크기로 움직입니다.")]
    [SerializeField] private RectTransform ringFxRoot;

    [Tooltip("RingFX에 붙은 CanvasGroup입니다. 없으면 자동으로 찾습니다.")]
    [SerializeField] private CanvasGroup ringCanvasGroup;

    [Tooltip("mural_circle_0~4를 표시할 Image입니다.")]
    [SerializeField] private Image ringImage;

    [Tooltip("mural_circle_0, mural_circle_1, mural_circle_2, mural_circle_3, mural_circle_4 순서로 넣으세요.")]
    [SerializeField] private Sprite[] ringFrames;

    [SerializeField] private float ringFramesPerSecond = 12f;
    [SerializeField] private float ringSizePadding = 80f;
    [SerializeField, Range(0f, 1f)] private float ringMaxAlpha = 1f;
    [SerializeField] private bool rotateRing = true;
    [SerializeField] private float ringRotationDegrees = 180f;

    [Header("Canvas")]
    [SerializeField] private Canvas transitionCanvas;

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("벽화 상태 화면을 RenderTexture로 찍는 카메라입니다. targetTexture가 반드시 있어야 합니다.")]
    [SerializeField] private Camera muralCamera;

    [Header("Animation")]
    [Tooltip("마지막으로 원이 크게 퍼지는 시간입니다.")]
    [SerializeField] private float transitionDuration = 1.2f;

    [Tooltip("최종 원 지름 배율입니다. 화면 대각선보다 크게 잡아야 화면 전체가 덮입니다.")]
    [SerializeField] private float finalDiameterMultiplier = 1.25f;

    [SerializeField] private bool useUnscaledTime = true;

    [Header("Wonder Pulse")]
    [Tooltip("처음 살짝 커지는 시간입니다.")]
    [SerializeField] private float pulseOutDuration = 0.25f;

    [Tooltip("작게 다시 줄어드는 시간입니다.")]
    [SerializeField] private float pulseInDuration = 0.15f;

    [Tooltip("첫 번째로 커질 크기 비율입니다. 전체 지름 기준입니다.")]
    [SerializeField, Range(0.01f, 1f)] private float pulseOutSizeRatio = 0.25f;

    [Tooltip("다시 줄어들 크기 비율입니다. 전체 지름 기준입니다.")]
    [SerializeField, Range(0.01f, 1f)] private float pulseInSizeRatio = 0.06f;

    [SerializeField] private Ease pulseOutEase = Ease.OutQuad;
    [SerializeField] private Ease pulseInEase = Ease.InOutSine;
    [SerializeField] private Ease finalExpandEase = Ease.OutCubic;

    [Header("Distortion Settings")]
    [Tooltip("왜곡 링 두께입니다. Shader Graph의 _DistortionWidth에 들어갑니다.")]
    [SerializeField] private float distortionWidth = 0.08f;

    [Tooltip("최대 왜곡 강도입니다. 너무 강하면 화면이 과하게 찌그러집니다.")]
    [SerializeField] private float maxDistortionStrength = 0.025f;

    [Tooltip("왜곡 오버레이 알파입니다.")]
    [SerializeField, Range(0f, 1f)] private float distortionAlpha = 1f;

    private Tween transitionTween;
    private Tween ringRotationTween;
    private Texture2D oldScreenTexture;
    private Material runtimeDistortionMaterial;

    private bool completionInvoked;
    private bool isTransitioning;

    private bool ringAnimating;
    private float ringFrameTimer;
    private int ringFrameIndex;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (transitionCanvas == null && transitionRoot != null)
            transitionCanvas = transitionRoot.GetComponentInParent<Canvas>();

        InitializeDistortionMaterial();

        if (ringCanvasGroup == null && ringFxRoot != null)
            ringCanvasGroup = ringFxRoot.GetComponent<CanvasGroup>();

        SetTransitionRootActive(false);
        SetDistortionImageActive(false);
        SetRingActive(false);

        if (muralCamera != null)
            muralCamera.gameObject.SetActive(false);

        if (oldScreenImage != null)
            oldScreenImage.raycastTarget = false;

        if (muralScreenImage != null)
            muralScreenImage.raycastTarget = false;

        if (distortionImage != null)
            distortionImage.raycastTarget = false;

        if (ringImage != null)
            ringImage.raycastTarget = false;

        ResetDistortionValues();
        ResetRingVisual();
    }

    private void Update()
    {
        UpdateRingFrameAnimation();
    }

    private void OnDestroy()
    {
        transitionTween?.Kill();
        ringRotationTween?.Kill();

        CleanupCapturedTexture();

        if (runtimeDistortionMaterial != null)
        {
            Destroy(runtimeDistortionMaterial);
            runtimeDistortionMaterial = null;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (transitionCanvas == null && transitionRoot != null)
            transitionCanvas = transitionRoot.GetComponentInParent<Canvas>();

        if (distortionImage != null && distortionMaterial == null)
            distortionMaterial = distortionImage.material;

        if (ringCanvasGroup == null && ringFxRoot != null)
            ringCanvasGroup = ringFxRoot.GetComponent<CanvasGroup>();
    }
#endif

    /// <summary>
    /// 벽화 상호작용 시 호출.
    /// muralTransform: 원형 전환이 시작될 벽화 위치.
    /// onComplete: 실제 월드를 벽화 상태/현실 상태로 바꾸는 콜백.
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

        transitionTween?.Kill();
        ringRotationTween?.Kill();

        CleanupCapturedTexture();
        ResetDistortionValues();
        ResetRingVisual();

        SetTransitionRootActive(false);
        SetDistortionImageActive(false);
        SetRingActive(false);

        // 1. 현재 화면 캡처
        yield return new WaitForEndOfFrame();
        oldScreenTexture = ScreenCapture.CaptureScreenshotAsTexture();

        // 2. 기존 화면을 UI로 덮기
        PrepareOldScreenImage(oldScreenTexture);
        PrepareCircleMask(muralTransform);

        Vector2 normalizedCenter = GetViewportCenterPosition(muralTransform);
        float aspectRatio = GetTransitionAspectRatio();
        PrepareDistortionImage(oldScreenTexture, normalizedCenter, aspectRatio);

        SetTransitionRootActive(true);

        if (circleMaskRoot != null)
        {
            circleMaskRoot.sizeDelta = Vector2.zero;
            circleMaskRoot.gameObject.SetActive(false);
        }

        // 3. 실제 월드 상태 변경
        InvokeCompletion(onComplete);

        // 4. 변경된 월드를 보조 카메라로 RenderTexture에 렌더링
        PrepareMuralCamera();
        PrepareMuralScreenImage();

        // 5. 마스크/링 표시 시작
        if (circleMaskRoot != null)
        {
            circleMaskRoot.sizeDelta = Vector2.zero;
            circleMaskRoot.gameObject.SetActive(true);
        }

        if (ringFxRoot != null)
            SetRingActive(true);

        float finalDiameter = GetFinalMaskDiameter();

        Sequence sequence = BuildTransitionSequence(finalDiameter);
        transitionTween = sequence;

        yield return sequence.WaitForCompletion();

        StopTransitionVisuals();

        isTransitioning = false;
    }

    private Sequence BuildTransitionSequence(float finalDiameter)
    {
        float step1Diameter = finalDiameter * pulseOutSizeRatio;
        float step2Diameter = finalDiameter * pulseInSizeRatio;

        float step1Radius = ConvertDiameterToShaderRadius(step1Diameter);
        float step2Radius = ConvertDiameterToShaderRadius(step2Diameter);
        float finalRadius = ConvertDiameterToShaderRadius(finalDiameter);

        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(useUnscaledTime).SetTarget(this);

        StartRingFrameAnimation();

        if (rotateRing && ringFxRoot != null)
        {
            ringRotationTween = ringFxRoot
                .DORotate(
                    new Vector3(0f, 0f, ringRotationDegrees),
                    pulseOutDuration + pulseInDuration + transitionDuration,
                    RotateMode.FastBeyond360
                )
                .SetEase(Ease.Linear)
                .SetUpdate(useUnscaledTime)
                .SetTarget(this);
        }

        // 1단계: 살짝 커짐
        float outDuration = Mathf.Max(0.01f, pulseOutDuration);

        sequence.Append(
            circleMaskRoot
                .DOSizeDelta(new Vector2(step1Diameter, step1Diameter), outDuration)
                .SetEase(pulseOutEase)
        );

        JoinRingSize(sequence, step1Diameter, outDuration, pulseOutEase);
        JoinRingAlpha(sequence, ringMaxAlpha, outDuration);

        if (runtimeDistortionMaterial != null)
        {
            sequence.Join(DOSetDistortionRadius(step1Radius, outDuration).SetEase(pulseOutEase));
            sequence.Join(DOSetDistortionStrength(maxDistortionStrength * 0.35f, outDuration).SetEase(Ease.OutCubic));
            sequence.Join(DOSetDistortionAlpha(distortionAlpha, outDuration).SetEase(Ease.OutCubic));
        }

        // 2단계: 다시 줄어듦
        float inDuration = Mathf.Max(0.01f, pulseInDuration);

        sequence.Append(
            circleMaskRoot
                .DOSizeDelta(new Vector2(step2Diameter, step2Diameter), inDuration)
                .SetEase(pulseInEase)
        );

        JoinRingSize(sequence, step2Diameter, inDuration, pulseInEase);

        if (runtimeDistortionMaterial != null)
        {
            sequence.Join(DOSetDistortionRadius(step2Radius, inDuration).SetEase(pulseInEase));
            sequence.Join(DOSetDistortionStrength(maxDistortionStrength, inDuration).SetEase(Ease.OutCubic));
        }

        // 3단계: 확 커짐
        float finalDuration = Mathf.Max(0.01f, transitionDuration);

        sequence.Append(
            circleMaskRoot
                .DOSizeDelta(new Vector2(finalDiameter, finalDiameter), finalDuration)
                .SetEase(finalExpandEase)
        );

        JoinRingSize(sequence, finalDiameter, finalDuration, finalExpandEase);
        JoinRingAlpha(sequence, 0f, finalDuration);

        if (runtimeDistortionMaterial != null)
        {
            sequence.Join(DOSetDistortionRadius(finalRadius, finalDuration).SetEase(finalExpandEase));
            sequence.Join(DOSetDistortionStrength(0f, finalDuration * 0.65f).SetEase(Ease.OutCubic));
            sequence.Join(DOSetDistortionAlpha(0f, finalDuration).SetEase(Ease.InCubic));
        }

        return sequence;
    }

    private void InitializeDistortionMaterial()
    {
        if (distortionImage == null || runtimeDistortionMaterial != null)
            return;

        Material sourceMaterial = distortionMaterial != null ? distortionMaterial : distortionImage.material;

        if (sourceMaterial == null)
            return;

        runtimeDistortionMaterial = Instantiate(sourceMaterial);
        runtimeDistortionMaterial.name = sourceMaterial.name + " (Runtime)";
        distortionImage.material = runtimeDistortionMaterial;
        distortionMaterial = runtimeDistortionMaterial;
    }

    private void PrepareOldScreenImage(Texture texture)
    {
        if (oldScreenImage == null)
            return;

        oldScreenImage.texture = texture;
        StretchRawImage(oldScreenImage);
    }

    private void PrepareDistortionImage(Texture screenTexture, Vector2 normalizedCenter, float aspectRatio)
    {
        if (distortionImage == null || runtimeDistortionMaterial == null)
            return;

        StretchRawImage(distortionImage);

        distortionImage.texture = screenTexture;
        distortionImage.gameObject.SetActive(true);

        SetDistortionTexture(DistortionMainTexId, screenTexture);
        SetDistortionVector(DistortionCenterPosId, new Vector4(normalizedCenter.x, normalizedCenter.y, 0f, 0f));
        SetDistortionFloat(DistortionRadiusId, 0f);
        SetDistortionFloat(DistortionWidthId, distortionWidth);
        SetDistortionFloat(DistortionStrengthId, 0f);
        SetDistortionFloat(DistortionAspectRatioId, aspectRatio);
        SetDistortionFloat(DistortionAlphaId, distortionAlpha);
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
        muralRect.sizeDelta = canvasSize;
        muralRect.anchoredPosition = -maskPosition;

        PrepareRing(maskPosition);
    }

    private void PrepareRing(Vector2 maskPosition)
    {
        if (ringFxRoot == null)
            return;

        ringFxRoot.gameObject.SetActive(true);
        ringFxRoot.anchoredPosition = maskPosition;
        ringFxRoot.sizeDelta = Vector2.zero;
        ringFxRoot.localRotation = Quaternion.identity;

        if (ringCanvasGroup == null)
            ringCanvasGroup = ringFxRoot.GetComponent<CanvasGroup>();

        if (ringCanvasGroup != null)
        {
            ringCanvasGroup.alpha = 0f;
            ringCanvasGroup.interactable = false;
            ringCanvasGroup.blocksRaycasts = false;
        }

        SetRingFrame(0);
        ringFrameTimer = 0f;
        ringFrameIndex = 0;
    }

    private void PrepareMuralCamera()
    {
        if (muralCamera == null)
            return;

        SyncMuralCameraToMainCamera();

        muralCamera.gameObject.SetActive(true);
        muralCamera.Render();
    }

    private void PrepareMuralScreenImage()
    {
        if (muralScreenImage == null || muralCamera == null)
            return;

        muralScreenImage.texture = muralCamera.targetTexture;
    }

    private void StartRingFrameAnimation()
    {
        if (ringImage == null || ringFrames == null || ringFrames.Length == 0)
            return;

        ringAnimating = true;
        ringFrameTimer = 0f;
        ringFrameIndex = 0;
        SetRingFrame(ringFrameIndex);
    }

    private void StopRingFrameAnimation()
    {
        ringAnimating = false;
        ringFrameTimer = 0f;
        ringFrameIndex = 0;
        SetRingFrame(0);
    }

    private void UpdateRingFrameAnimation()
    {
        if (!ringAnimating)
            return;

        if (ringImage == null || ringFrames == null || ringFrames.Length == 0)
            return;

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(1f, ringFramesPerSecond);

        ringFrameTimer += deltaTime;

        while (ringFrameTimer >= frameDuration)
        {
            ringFrameTimer -= frameDuration;
            ringFrameIndex++;

            if (ringFrameIndex >= ringFrames.Length)
                ringFrameIndex = 0;

            SetRingFrame(ringFrameIndex);
        }
    }

    private void SetRingFrame(int index)
    {
        if (ringImage == null || ringFrames == null || ringFrames.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, ringFrames.Length - 1);

        if (ringFrames[index] != null)
            ringImage.sprite = ringFrames[index];
    }

    private void JoinRingSize(Sequence sequence, float diameter, float duration, Ease ease)
    {
        if (sequence == null || ringFxRoot == null)
            return;

        float paddedDiameter = diameter + ringSizePadding;

        sequence.Join(
            ringFxRoot
                .DOSizeDelta(new Vector2(paddedDiameter, paddedDiameter), Mathf.Max(0.01f, duration))
                .SetEase(ease)
        );
    }

    private void JoinRingAlpha(Sequence sequence, float targetAlpha, float duration)
    {
        if (sequence == null || ringCanvasGroup == null)
            return;

        sequence.Join(
            ringCanvasGroup
                .DOFade(targetAlpha, Mathf.Max(0.01f, duration))
                .SetEase(Ease.OutCubic)
        );
    }

    private Tween DOSetDistortionRadius(float targetValue, float duration)
    {
        return DOTween.To(
            () => GetDistortionFloat(DistortionRadiusId),
            value => SetDistortionFloat(DistortionRadiusId, value),
            targetValue,
            duration
        );
    }

    private Tween DOSetDistortionStrength(float targetValue, float duration)
    {
        return DOTween.To(
            () => GetDistortionFloat(DistortionStrengthId),
            value => SetDistortionFloat(DistortionStrengthId, value),
            targetValue,
            duration
        );
    }

    private Tween DOSetDistortionAlpha(float targetValue, float duration)
    {
        return DOTween.To(
            () => GetDistortionFloat(DistortionAlphaId),
            value => SetDistortionFloat(DistortionAlphaId, value),
            targetValue,
            duration
        );
    }

    private float GetDistortionFloat(int propertyId)
    {
        if (runtimeDistortionMaterial == null || !runtimeDistortionMaterial.HasProperty(propertyId))
            return 0f;

        return runtimeDistortionMaterial.GetFloat(propertyId);
    }

    private void SetDistortionFloat(int propertyId, float value)
    {
        if (runtimeDistortionMaterial == null || !runtimeDistortionMaterial.HasProperty(propertyId))
            return;

        runtimeDistortionMaterial.SetFloat(propertyId, value);
    }

    private void SetDistortionVector(int propertyId, Vector4 value)
    {
        if (runtimeDistortionMaterial == null || !runtimeDistortionMaterial.HasProperty(propertyId))
            return;

        runtimeDistortionMaterial.SetVector(propertyId, value);
    }

    private void SetDistortionTexture(int propertyId, Texture texture)
    {
        if (runtimeDistortionMaterial == null || !runtimeDistortionMaterial.HasProperty(propertyId))
            return;

        runtimeDistortionMaterial.SetTexture(propertyId, texture);
    }

    private void ResetDistortionValues()
    {
        if (runtimeDistortionMaterial == null)
            return;

        SetDistortionFloat(DistortionRadiusId, 0f);
        SetDistortionFloat(DistortionStrengthId, 0f);
        SetDistortionFloat(DistortionAlphaId, 0f);
    }

    private void ResetRingVisual()
    {
        StopRingFrameAnimation();

        ringRotationTween?.Kill();
        ringRotationTween = null;

        if (ringFxRoot != null)
        {
            ringFxRoot.sizeDelta = Vector2.zero;
            ringFxRoot.localRotation = Quaternion.identity;
            ringFxRoot.gameObject.SetActive(false);
        }

        if (ringCanvasGroup != null)
            ringCanvasGroup.alpha = 0f;
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
            mainCamera = Camera.main;

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
            transitionCanvas = transitionRoot.GetComponentInParent<Canvas>();

        if (transitionCanvas == null)
        {
            Debug.LogWarning("MuralTransitionCanvas를 찾을 수 없습니다.", this);
            return false;
        }

        if (distortionImage != null && runtimeDistortionMaterial == null)
            InitializeDistortionMaterial();

        if (distortionImage != null && runtimeDistortionMaterial == null)
            Debug.LogWarning("DistortionImage에 사용할 Material이 없어 왜곡 연출은 생략됩니다.", this);

        if (ringFxRoot == null || ringImage == null)
            Debug.LogWarning("RingFX 또는 RingImage가 연결되어 있지 않아 원 테두리 그래픽 연출은 생략됩니다.", this);

        return true;
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

    private Vector2 GetViewportCenterPosition(Transform muralTransform)
    {
        Vector3 viewport = mainCamera.WorldToViewportPoint(muralTransform.position);

        return new Vector2(
            Mathf.Clamp01(viewport.x),
            Mathf.Clamp01(viewport.y)
        );
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

    private float GetTransitionAspectRatio()
    {
        Vector2 size = GetMaskParentSize();

        if (size.y > 0.01f)
            return size.x / size.y;

        if (mainCamera != null && mainCamera.aspect > 0f)
            return mainCamera.aspect;

        if (Screen.height > 0)
            return (float)Screen.width / Screen.height;

        return 16f / 9f;
    }

    private float GetFinalMaskDiameter()
    {
        Vector2 size = GetMaskParentSize();
        float diagonal = Mathf.Sqrt(size.x * size.x + size.y * size.y);
        return diagonal * Mathf.Max(1f, finalDiameterMultiplier);
    }

    private float ConvertDiameterToShaderRadius(float diameter)
    {
        Vector2 size = GetMaskParentSize();
        float height = size.y > 0.01f ? size.y : Screen.height;

        if (height <= 0.01f)
            height = 1080f;

        return diameter / height * 0.5f;
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
        transitionTween?.Kill();
        transitionTween = null;

        ringRotationTween?.Kill();
        ringRotationTween = null;

        SetTransitionRootActive(false);
        SetDistortionImageActive(false);

        if (muralCamera != null)
            muralCamera.gameObject.SetActive(false);

        if (oldScreenImage != null)
            oldScreenImage.texture = null;

        if (muralScreenImage != null)
            muralScreenImage.texture = null;

        if (distortionImage != null)
            distortionImage.texture = null;

        CleanupCapturedTexture();
        ResetDistortionValues();
        ResetRingVisual();
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

    private void SetDistortionImageActive(bool isActive)
    {
        if (distortionImage != null)
            distortionImage.gameObject.SetActive(isActive);
    }

    private void SetRingActive(bool isActive)
    {
        if (ringFxRoot != null)
            ringFxRoot.gameObject.SetActive(isActive);
    }

    private static void StretchRawImage(RawImage rawImage)
    {
        if (rawImage == null)
            return;

        RectTransform rect = rawImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}