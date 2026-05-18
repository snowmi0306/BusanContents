using Spine;
using Spine.Unity;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayController : MonoBehaviour
{
    private const int BaseAnimationTrack = 0;
    private const int HitAnimationTrack = 1;
    private const string GroundCheckObjectName = "GroundCheck";

    [Header("Move")]
    public float moveSpeed = 5f;

    [Header("Jump")]
    public float jumpForce = 5f;
    public float glideGravity = 0.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float glideStaminaCost = 15f;
    public float staminaRegenRate = 25f;

    [Header("Health")]
    public int maxHealth = 3;
    public float invincibleDuration = 1f;

    [Header("Spine Animation")]
    public string spineObjectName = "Spine GameObject (dandi)";
    public SkeletonAnimation skeletonAnimation;
    public string idleAnimationName = "dandi_idle";
    public string walkAnimationName = "dandi_walk";
    public string jumpAnimationName = "dandi_Jump";
    public string glideAnimationName = "dandi_Glide";
    public string hitAnimationName = "dandi_hit";
    public float walkInputThreshold = 0.01f;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private float defaultGravityScale;

    private bool isGrounded;
    private bool isGliding;
    private float moveInput;
    private string currentBaseAnimationName;

    private float currentStamina;
    private int currentHealth;
    private float invincibleTimer;
    private Vector3 spawnPoint;
    private Vector3 stageStartPoint;

    private void Awake()
    {
        CacheComponents();
        CacheSceneReferences();
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        InitializeState();
    }

    private void Update()
    {
        UpdateInvincibilityTimer();
        ReadInput();
        UpdateGroundedState();
        MoveHorizontally();
        TryJump();
        UpdateGlideAndStamina();
        UpdateSpineVisual();
    }

    private void CacheComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        defaultGravityScale = rb.gravityScale;
    }

    private void CacheSceneReferences()
    {
        AssignGroundCheckIfNeeded();
        AssignSkeletonAnimationIfNeeded();
    }

    private void InitializeState()
    {
        currentStamina = maxStamina;
        currentHealth = maxHealth;
        spawnPoint = transform.position;
        stageStartPoint = transform.position;
    }

    private void UpdateInvincibilityTimer()
    {
        if (invincibleTimer <= 0)
        {
            return;
        }

        invincibleTimer -= Time.deltaTime;
    }

    private void ReadInput()
    {
        moveInput = Input.GetAxis("Horizontal");
    }

    private void UpdateGroundedState()
    {
        isGrounded = CheckGrounded();
    }

    private void MoveHorizontally()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void TryJump()
    {
        if (!Input.GetKeyDown(KeyCode.Space) || !isGrounded)
        {
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void UpdateGlideAndStamina()
    {
        isGliding = CanGlide();

        if (isGliding)
        {
            rb.gravityScale = glideGravity;
            ConsumeStamina(glideStaminaCost * Time.deltaTime);
            return;
        }

        rb.gravityScale = defaultGravityScale;

        if (isGrounded)
        {
            RecoverStamina(staminaRegenRate * Time.deltaTime);
        }
    }

    private bool CanGlide()
    {
        return Input.GetKey(KeyCode.Space) && !isGrounded && rb.linearVelocity.y < 0 && currentStamina > 0;
    }

    private void ConsumeStamina(float amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
    }

    private void AssignGroundCheckIfNeeded()
    {
        if (IsValidGroundCheck(groundCheck))
        {
            return;
        }

        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child != transform && child.name == GroundCheckObjectName)
            {
                groundCheck = child;
                return;
            }
        }
    }

    private bool IsValidGroundCheck(Transform checkTransform)
    {
        return checkTransform != null && checkTransform.IsChildOf(transform);
    }

    private void AssignSkeletonAnimationIfNeeded()
    {
        if (skeletonAnimation != null)
        {
            return;
        }

        skeletonAnimation = FindNamedSkeletonAnimation();

        if (skeletonAnimation == null)
        {
            skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);
        }
    }

    private SkeletonAnimation FindNamedSkeletonAnimation()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == spineObjectName && child.TryGetComponent(out SkeletonAnimation foundSkeletonAnimation))
            {
                return foundSkeletonAnimation;
            }
        }

        return null;
    }

    private bool CheckGrounded()
    {
        return Physics2D.OverlapCircle(GetGroundCheckPosition(), groundCheckRadius, groundLayer) != null;
    }

    private Vector2 GetGroundCheckPosition()
    {
        if (IsValidGroundCheck(groundCheck))
        {
            return groundCheck.position;
        }

        if (playerCollider == null)
        {
            return transform.position;
        }

        Bounds bounds = playerCollider.bounds;
        return new Vector2(bounds.center.x, bounds.min.y - groundCheckRadius * 0.5f);
    }

    private void UpdateSpineVisual()
    {
        FlipSpine(moveInput);
        UpdateSpineAnimation();
    }

    private void FlipSpine(float horizontalInput)
    {
        if (skeletonAnimation == null || Mathf.Abs(horizontalInput) <= walkInputThreshold)
        {
            return;
        }

        skeletonAnimation.Skeleton.ScaleX = horizontalInput < 0 ? -1f : 1f;
    }

    private void UpdateSpineAnimation()
    {
        if (isGliding)
        {
            SetBaseSpineAnimation(glideAnimationName, true);
            return;
        }

        if (!isGrounded)
        {
            SetBaseSpineAnimation(jumpAnimationName, false);
            return;
        }

        if (Mathf.Abs(moveInput) > walkInputThreshold)
        {
            SetBaseSpineAnimation(walkAnimationName, true);
            return;
        }

        SetBaseSpineAnimation(idleAnimationName, true);
    }

    private void SetBaseSpineAnimation(string animationName, bool loop)
    {
        if (skeletonAnimation == null || string.IsNullOrEmpty(animationName) || currentBaseAnimationName == animationName)
        {
            return;
        }

        if (!HasSpineAnimation(animationName))
        {
            Debug.LogWarning($"Spine animation not found: {animationName}");
            return;
        }

        skeletonAnimation.AnimationState.SetAnimation(BaseAnimationTrack, animationName, loop);
        currentBaseAnimationName = animationName;
    }

    private bool HasSpineAnimation(string animationName)
    {
        if (skeletonAnimation == null || skeletonAnimation.SkeletonDataAsset == null || string.IsNullOrEmpty(animationName))
        {
            return false;
        }

        SkeletonData skeletonData = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false);
        return skeletonData != null && skeletonData.FindAnimation(animationName) != null;
    }

    private void PlayHitAnimation()
    {
        if (skeletonAnimation == null || !HasSpineAnimation(hitAnimationName))
        {
            return;
        }

        TrackEntry hitEntry = skeletonAnimation.AnimationState.SetAnimation(HitAnimationTrack, hitAnimationName, false);
        hitEntry.Complete += delegate
        {
            skeletonAnimation.AnimationState.ClearTrack(HitAnimationTrack);
        };
    }

    public void TakeDamage(int damage = 1)
    {
        if (invincibleTimer > 0)
        {
            Debug.Log("무적 상태");
            return;
        }

        currentHealth -= damage;
        invincibleTimer = invincibleDuration;
        PlayHitAnimation();

        Debug.Log($"데미지! 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            RespawnAtStageStart();
        }
        else
        {
            RespawnAtCheckpoint();
        }
    }

    /// <summary>
    /// 체크포인트에서 리스폰 (체력 > 0)
    /// </summary>
    public void RespawnAtCheckpoint()
    {
        MoveToSpawnPoint(spawnPoint);

        Debug.Log("체크포인트에서 리스폰! 위치: " + spawnPoint);
    }

    /// <summary>
    /// 스테이지 처음으로 리스폰 (체력 = 0)
    /// </summary>
    public void RespawnAtStageStart()
    {
        currentHealth = maxHealth;
        spawnPoint = stageStartPoint;
        MoveToSpawnPoint(stageStartPoint);

        Debug.Log("게임 오버! 스테이지 처음으로 리스폰: " + stageStartPoint);
    }

    private void MoveToSpawnPoint(Vector3 targetPosition)
    {
        invincibleTimer = invincibleDuration;
        transform.position = targetPosition;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = defaultGravityScale;
        isGliding = false;
    }

    public void SetCheckpoint(Vector3 checkpointPos)
    {
        spawnPoint = checkpointPos;
        Debug.Log("체크포인트 저장: " + checkpointPos);
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public bool IsInvincible()
    {
        return invincibleTimer > 0;
    }

    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    public float GetStaminaPercent()
    {
        if (maxStamina <= 0)
        {
            return 0;
        }

        return currentStamina / maxStamina;
    }

    public void RecoverStamina(float amount)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
    }
}
