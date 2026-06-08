using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MuralInteractTrigger : MonoBehaviour
{
    public enum WorldTargetMode
    {
        ToMuralWorld,
        ToDefaultWorld,
        Toggle
    }

    [Header("Transition Manager")]
    [Tooltip("원형 마스크/페이드 등 벽화 전환 연출을 담당하는 매니저입니다. 비워두면 즉시 전환합니다.")]
    [SerializeField] private MuralTransitionManager transitionManager;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject interactHint;

    [Tooltip("이 벽화를 상호작용했을 때 전환될 목표 세계입니다. 현실→벽화 벽화는 ToMuralWorld, 벽화→현실 벽화는 ToDefaultWorld로 설정하세요.")]
    [SerializeField] private WorldTargetMode targetMode = WorldTargetMode.ToMuralWorld;

    [Tooltip("전환 도중 플레이어 속도를 0으로 만들어 끼임/튐을 줄입니다.")]
    [SerializeField] private bool stabilizePlayerDuringSwap = true;

    [Header("Initial State")]
    [Tooltip("씬 시작 시 기본/벽화 배경과 오브젝트 그룹의 초기 상태를 이 스크립트가 세팅할지 여부입니다. 여러 벽화에 모두 켜면 서로 상태를 덮어쓸 수 있으니 보통 한 곳에서만 켜거나, 씬에서 직접 초기 상태를 세팅하세요.")]
    [SerializeField] private bool initializeWorldStateOnAwake = false;

    [Tooltip("초기 상태를 설정할 때 벽화 세계로 시작할지 여부입니다.")]
    [SerializeField] private bool startInMuralWorld = false;

    [Header("Background Toggle")]
    [SerializeField] private GameObject defaultBackground;
    [SerializeField] private GameObject muralBackground;

    [Header("Object Toggle")]
    [Tooltip("현실 세계에서 활성화될 발판/장애물/맵 오브젝트 그룹입니다.")]
    [SerializeField] private GameObject defaultObjectGroup;

    [Tooltip("벽화 세계에서 활성화될 발판/장애물/맵 오브젝트 그룹입니다.")]
    [SerializeField] private GameObject muralObjectGroup;

    [Header("Checkpoint")]
    [Tooltip("전환이 끝났을 때 플레이어의 체크포인트를 갱신합니다.")]
    [SerializeField] private bool setCheckpointOnTransition = true;

    [Tooltip("비워두면 이 벽화 오브젝트 위치가 세이브 포인트로 저장됩니다.")]
    [SerializeField] private Transform checkpointRespawnPoint;

    [Header("Interact Hint Transparency")]
    [SerializeField, Range(0f, 1f)] private float defaultInteractHintAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float disabledInteractHintAlpha = 0.5f;

    [Header("Events")]
    [SerializeField] private UnityEvent onTransitionCompleted;

    private PlayController currentPlayer;
    private Rigidbody2D currentPlayerRigidbody;
    private bool isPlayerInside;
    private bool isTransitioning;

    private void Awake()
    {
        SetHintActive(interactHint, false);
        SetHintTransparency(interactHint, defaultInteractHintAlpha);

        if (initializeWorldStateOnAwake)
        {
            ApplyWorldState(startInMuralWorld);
        }
    }

    private void Update()
    {
        if (!isPlayerInside || currentPlayer == null || isTransitioning)
            return;

        if (!Input.GetKeyDown(interactKey))
            return;

        StartWorldTransition();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!TryRegisterPlayer(other))
            return;

        SetHintActive(interactHint, true);
        SetHintTransparency(interactHint, isTransitioning ? disabledInteractHintAlpha : defaultInteractHintAlpha);
        AudioManager.PlaySfx("sfx_mural_ready");
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isPlayerInside && currentPlayer != null)
            return;

        if (!TryRegisterPlayer(other))
            return;
    }

    private bool TryRegisterPlayer(Collider2D other)
    {
        if (other == null || !other.CompareTag("Player"))
            return false;

        PlayController player = other.GetComponentInParent<PlayController>();
        if (player == null)
            return false;

        currentPlayer = player;
        currentPlayerRigidbody = other.GetComponentInParent<Rigidbody2D>();
        isPlayerInside = true;

        if (!isTransitioning)
        {
            SetHintActive(interactHint, true);
            SetHintTransparency(interactHint, defaultInteractHintAlpha);
        }

        return true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null || !other.CompareTag("Player"))
            return;

        PlayController exitingPlayer = other.GetComponentInParent<PlayController>();

        if (currentPlayer != null && exitingPlayer != currentPlayer)
            return;

        isPlayerInside = false;
        currentPlayer = null;
        currentPlayerRigidbody = null;

        SetHintActive(interactHint, false);
        SetHintTransparency(interactHint, defaultInteractHintAlpha);
    }

    private void StartWorldTransition()
    {
        if (isTransitioning)
            return;

        bool targetMuralActive = GetTargetMuralActive();

        // 이미 목표 상태라면 중복 전환하지 않습니다.
        if (IsAlreadyInTargetState(targetMuralActive))
        {
            Debug.Log("이미 목표 세계 상태입니다.");
            return;
        }

        AudioManager.PlaySfx(targetMuralActive ? "sfx_mural_enter" : "sfx_mural_exit");

        isTransitioning = true;
        SetHintActive(interactHint, false);
        SetHintTransparency(interactHint, defaultInteractHintAlpha);

        StabilizeCurrentPlayerForWorldSwap();

        if (transitionManager != null)
        {
            transitionManager.StartTransition(transform, () => CompleteWorldTransition(targetMuralActive));
        }
        else
        {
            CompleteWorldTransition(targetMuralActive);
        }
    }

    private bool GetTargetMuralActive()
    {
        switch (targetMode)
        {
            case WorldTargetMode.ToMuralWorld:
                return true;

            case WorldTargetMode.ToDefaultWorld:
                return false;

            case WorldTargetMode.Toggle:
                return !IsMuralWorldCurrentlyActive();

            default:
                return true;
        }
    }

    private bool IsMuralWorldCurrentlyActive()
    {
        if (muralBackground != null)
            return muralBackground.activeSelf;

        if (muralObjectGroup != null)
            return muralObjectGroup.activeSelf;

        if (defaultBackground != null)
            return !defaultBackground.activeSelf;

        if (defaultObjectGroup != null)
            return !defaultObjectGroup.activeSelf;

        return false;
    }

    private bool IsAlreadyInTargetState(bool targetMuralActive)
    {
        if (muralBackground != null)
            return muralBackground.activeSelf == targetMuralActive;

        if (muralObjectGroup != null)
            return muralObjectGroup.activeSelf == targetMuralActive;

        return false;
    }

    private void CompleteWorldTransition(bool muralActive)
    {
        StabilizeCurrentPlayerForWorldSwap();

        ApplyWorldState(muralActive);
        AudioManager.PlayCurrentSceneBgm(muralActive);

        Physics2D.SyncTransforms();

        StabilizeCurrentPlayerForWorldSwap();
        UpdateMuralCheckpoint(muralActive);

        onTransitionCompleted?.Invoke();

        isTransitioning = false;

        if (isPlayerInside)
        {
            SetHintActive(interactHint, true);
            SetHintTransparency(interactHint, defaultInteractHintAlpha);
        }

        Debug.Log(muralActive ? "벽화 세계로 전환 완료" : "현실 세계로 복귀 완료");
    }

    private void ApplyWorldState(bool muralActive)
    {
        SetObjectPairActive(defaultBackground, muralBackground, muralActive);
        SetObjectPairActive(defaultObjectGroup, muralObjectGroup, muralActive);
    }

    private void StabilizeCurrentPlayerForWorldSwap()
    {
        if (!stabilizePlayerDuringSwap)
            return;

        if (currentPlayerRigidbody == null && currentPlayer != null)
            currentPlayerRigidbody = currentPlayer.GetComponentInParent<Rigidbody2D>();

        if (currentPlayerRigidbody == null)
            return;

        currentPlayerRigidbody.linearVelocity = Vector2.zero;
        currentPlayerRigidbody.angularVelocity = 0f;
    }

    private void UpdateMuralCheckpoint(bool muralActive)
    {
        if (!setCheckpointOnTransition || currentPlayer == null)
            return;

        if (!muralActive)
        {
            currentPlayer.RestoreCheckpointBeforeTemporary();
            return;
        }

        Vector3 checkpointPosition = checkpointRespawnPoint != null
            ? checkpointRespawnPoint.position
            : transform.position;

        currentPlayer.SetTemporaryCheckpoint(checkpointPosition);
        Debug.Log("벽화 전환 세이브 포인트 저장: " + checkpointPosition);
    }

    private static void SetObjectPairActive(GameObject defaultObject, GameObject muralObject, bool muralActive)
    {
        if (defaultObject != null)
            defaultObject.SetActive(!muralActive);

        if (muralObject != null)
            muralObject.SetActive(muralActive);
    }

    private static void SetHintActive(GameObject target, bool active)
    {
        if (target == null)
            return;

        target.SetActive(active);
    }

    private static void SetHintTransparency(GameObject target, float alpha)
    {
        if (target == null)
            return;

        alpha = Mathf.Clamp01(alpha);

        Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        SpriteRenderer[] spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}
