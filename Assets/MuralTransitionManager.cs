using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class MuralTransitionManager : MonoBehaviour
{
    private static readonly int CenterPosId = Shader.PropertyToID("_CenterPos");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");

    [Header("전환 연출 UI")]
    [Tooltip("셰이더가 들어간 RawImage입니다. 화면 전체를 덮도록 배치해주세요.")]
    public RawImage transitionRawImage;

    [Tooltip("RawImage에 들어있는 매테리얼입니다. 비워두면 RawImage.material을 자동으로 사용합니다.")]
    public Material maskMaterial;

    [Header("카메라 세팅")]
    public Camera mainCamera;

    [Tooltip("RenderTexture를 쓰는 별도 벽화 카메라입니다. 기본 전환에서는 켜지지 않도록 둡니다.")]
    public Camera muralCamera;

    [Header("연출 설정")]
    public float transitionDuration = 1.5f; // 원이 퍼져나가는 시간
    [SerializeField] private bool playShaderTransition = false;
    [SerializeField] private float maxRadius = 1.5f;
    [SerializeField] private bool swapBeforeTransitionEffect = true;
    [SerializeField] private bool useMuralCameraTexture = false;

    private Material runtimeMaskMaterial;
    private Texture originalTransitionTexture;
    private Tween radiusTween;
    private bool completionInvoked;

    private void Awake()
    {
        InitializeRuntimeMaterial();
        SetTransitionImageActive(false);
    }

    private void OnDestroy()
    {
        radiusTween?.Kill();

        if (runtimeMaskMaterial != null)
        {
            Destroy(runtimeMaskMaterial);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (transitionRawImage != null && maskMaterial == null)
        {
            maskMaterial = transitionRawImage.material;
        }
    }
#endif

    // 단디가 벽화 앞에서 상호작용할 때 이 함수를 호출해주세요.
    // 매개변수 muralTransform에는 상호작용하는 '해당 벽화의 Transform'을 넘겨줍니다.
    // Action onComplete를 통해 연출이 다 끝난 시점을 상호작용 스크립트에 알려줍니다.
    public void StartTransition(Transform muralTransform, Action onComplete = null)
    {
        if (!CanPlayTransition(muralTransform))
        {
            onComplete?.Invoke();
            return;
        }

        if (!playShaderTransition)
        {
            StopTransitionVisuals();
            onComplete?.Invoke();
            return;
        }

        InitializeRuntimeMaterial();

        if (runtimeMaskMaterial == null || !runtimeMaskMaterial.HasProperty(CenterPosId) || !runtimeMaskMaterial.HasProperty(RadiusId))
        {
            Debug.LogWarning("벽화 전환 매테리얼에 _CenterPos 또는 _Radius 프로퍼티가 없어 연출을 건너뜁니다.", this);
            StopTransitionVisuals();
            onComplete?.Invoke();
            return;
        }

        radiusTween?.Kill();

        completionInvoked = false;

        if (useMuralCameraTexture && muralCamera != null)
        {
            transitionRawImage.texture = originalTransitionTexture;
            SyncMuralCameraToMainCamera();
            muralCamera.gameObject.SetActive(true);
        }
        else
        {
            transitionRawImage.texture = Texture2D.whiteTexture;

            if (muralCamera != null)
            {
                muralCamera.gameObject.SetActive(false);
            }
        }

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(muralTransform.position);
        Vector2 normalizedPos = new Vector2(
            Mathf.Clamp01(viewportPos.x),
            Mathf.Clamp01(viewportPos.y)
        );

        runtimeMaskMaterial.SetVector(CenterPosId, new Vector4(normalizedPos.x, normalizedPos.y, 0f, 0f));
        runtimeMaskMaterial.SetFloat(RadiusId, 0f);

        // 실제 벽화 상태는 먼저 바꿔 둡니다. 그래야 RenderTexture/보조 카메라가 잠깐 보이며
        // 이상한 색 화면이 뜬 뒤 바뀌는 느낌 없이, 메인 카메라 화면 기준으로 바로 전환됩니다.
        if (swapBeforeTransitionEffect)
        {
            InvokeCompletion(onComplete);
        }

        SetTransitionImageActive(true);

        radiusTween = DOTween.To(
                () => runtimeMaskMaterial.GetFloat(RadiusId),
                radius => runtimeMaskMaterial.SetFloat(RadiusId, radius),
                maxRadius,
                Mathf.Max(0.01f, transitionDuration)
            )
            .SetEase(Ease.OutBack)
            .SetTarget(this)
            .OnComplete(() => CompleteTransition(onComplete));
    }

    private void InitializeRuntimeMaterial()
    {
        if (transitionRawImage == null || runtimeMaskMaterial != null)
        {
            return;
        }

        originalTransitionTexture = transitionRawImage.texture;

        Material sourceMaterial = maskMaterial != null ? maskMaterial : transitionRawImage.material;
        if (sourceMaterial == null)
        {
            return;
        }

        runtimeMaskMaterial = Instantiate(sourceMaterial);
        runtimeMaskMaterial.name = sourceMaterial.name + " (Runtime)";
        transitionRawImage.material = runtimeMaskMaterial;
        maskMaterial = runtimeMaskMaterial;
    }

    private bool CanPlayTransition(Transform muralTransform)
    {
        if (muralTransform == null)
        {
            Debug.LogWarning("벽화 전환을 시작할 Transform이 없습니다.", this);
            return false;
        }

        if (transitionRawImage == null)
        {
            Debug.LogWarning("벽화 전환 RawImage가 연결되어 있지 않아 연출을 건너뜁니다.", this);
            return false;
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("벽화 전환 Main Camera가 연결되어 있지 않아 연출을 건너뜁니다.", this);
            return false;
        }

        return true;
    }

    private void CompleteTransition(Action onComplete)
    {
        InvokeCompletion(onComplete);
        StopTransitionVisuals();

        radiusTween = null;
    }

    private void StopTransitionVisuals()
    {
        SetTransitionImageActive(false);

        if (muralCamera != null)
        {
            muralCamera.gameObject.SetActive(false);
        }
    }

    private void InvokeCompletion(Action onComplete)
    {
        if (completionInvoked)
        {
            return;
        }

        completionInvoked = true;
        onComplete?.Invoke();
    }

    private void SyncMuralCameraToMainCamera()
    {
        if (mainCamera == null || muralCamera == null)
        {
            return;
        }

        muralCamera.transform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);
        muralCamera.orthographic = mainCamera.orthographic;
        muralCamera.orthographicSize = mainCamera.orthographicSize;
        muralCamera.fieldOfView = mainCamera.fieldOfView;
    }

    private void SetTransitionImageActive(bool isActive)
    {
        if (transitionRawImage == null)
        {
            return;
        }

        transitionRawImage.gameObject.SetActive(isActive);
    }
}
