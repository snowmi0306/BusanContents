using Spine;
using Spine.Unity;
using UnityEngine;

public class PlayController : MonoBehaviour
{
    private const int BaseTrack = 0;
    private const int HitTrack = 1;

    [Header("Move")]
    public float moveSpeed = 5f;

    [Header("Jump / Glide")]
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
    private float currentStamina;
    private int currentHealth;
    private float invincibleTimer;
    private string currentAnimationName;
    private Vector3 spawnPoint;
    private Vector3 stageStartPoint;

    private void Start()
    {
        Application.targetFrameRate = 60;

        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();

        if (rb == null)
        {
            Debug.LogError("PlayController needs a Rigidbody2D on the player root.");
            enabled = false;
            return;
        }

        defaultGravityScale = rb.gravityScale;
        currentStamina = maxStamina;
        currentHealth = maxHealth;
        spawnPoint = transform.position;
        stageStartPoint = transform.position;

        AssignGroundCheckIfNeeded();
        AssignSkeletonAnimationIfNeeded();
    }

    private void Update()
    {
        TickInvincibleTimer();

        float moveInput = Input.GetAxis("Horizontal");
        isGrounded = CheckGrounded();

        Move(moveInput);
        JumpIfPossible();
        UpdateGlide();
        UpdateSpine(moveInput);
    }

    private void TickInvincibleTimer()
    {
        if (invincibleTimer > 0)
        {
            invincibleTimer -= Time.deltaTime;
        }
    }

    private void Move(float moveInput)
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void JumpIfPossible()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private void UpdateGlide()
    {
        isGliding = Input.GetKey(KeyCode.Space) && !isGrounded && rb.linearVelocity.y < 0 && currentStamina > 0;

        if (isGliding)
        {
            rb.gravityScale = glideGravity;
            currentStamina = Mathf.Max(0, currentStamina - glideStaminaCost * Time.deltaTime);
            return;
        }

        rb.gravityScale = defaultGravityScale;

        if (isGrounded)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
        }
    }

    private void AssignGroundCheckIfNeeded()
    {
        if (IsValidGroundCheck())
        {
            return;
        }

        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child != transform && child.name == "GroundCheck")
            {
                groundCheck = child;
                return;
            }
        }
    }

    private bool IsValidGroundCheck()
    {
        return groundCheck != null && groundCheck.IsChildOf(transform);
    }

    private bool CheckGrounded()
    {
        Vector2 checkPosition = GetGroundCheckPosition();
        return Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer) != null;
    }

    private Vector2 GetGroundCheckPosition()
    {
        if (IsValidGroundCheck())
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

    private void AssignSkeletonAnimationIfNeeded()
    {
        if (skeletonAnimation != null)
        {
            return;
        }

        foreach (SkeletonAnimation candidate in GetComponentsInChildren<SkeletonAnimation>(true))
        {
            if (candidate.gameObject.name == spineObjectName)
            {
                skeletonAnimation = candidate;
                return;
            }
        }

        skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);
    }

    private void UpdateSpine(float moveInput)
    {
        if (skeletonAnimation == null)
        {
            return;
        }

        FlipSpine(moveInput);

        if (isGliding)
        {
            SetSpineAnimation(glideAnimationName, true);
        }
        else if (!isGrounded)
        {
            SetSpineAnimation(jumpAnimationName, false);
        }
        else if (Mathf.Abs(moveInput) > walkInputThreshold)
        {
            SetSpineAnimation(walkAnimationName, true);
        }
        else
        {
            SetSpineAnimation(idleAnimationName, true);
        }
    }

    private void FlipSpine(float moveInput)
    {
        if (Mathf.Abs(moveInput) <= walkInputThreshold)
        {
            return;
        }

        skeletonAnimation.Skeleton.ScaleX = moveInput < 0 ? -1f : 1f;
    }

    private void SetSpineAnimation(string animationName, bool loop)
    {
        if (string.IsNullOrEmpty(animationName) || currentAnimationName == animationName || !HasSpineAnimation(animationName))
        {
            return;
        }

        skeletonAnimation.AnimationState.SetAnimation(BaseTrack, animationName, loop);
        currentAnimationName = animationName;
    }

    private bool HasSpineAnimation(string animationName)
    {
        if (skeletonAnimation == null || skeletonAnimation.SkeletonDataAsset == null)
        {
            return false;
        }

        SkeletonData skeletonData = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false);
        bool hasAnimation = skeletonData != null && skeletonData.FindAnimation(animationName) != null;

        if (!hasAnimation)
        {
            Debug.LogWarning($"Spine animation not found: {animationName}");
        }

        return hasAnimation;
    }

    private void PlayHitAnimation()
    {
        if (skeletonAnimation == null || string.IsNullOrEmpty(hitAnimationName) || !HasSpineAnimation(hitAnimationName))
        {
            return;
        }

        TrackEntry hitEntry = skeletonAnimation.AnimationState.SetAnimation(HitTrack, hitAnimationName, false);
        hitEntry.Complete += _ => skeletonAnimation.AnimationState.ClearTrack(HitTrack);
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
        Respawn(spawnPoint);
        Debug.Log("체크포인트에서 리스폰! 위치: " + spawnPoint);
    }

    /// <summary>
    /// 스테이지 처음으로 리스폰 (체력 = 0)
    /// </summary>
    public void RespawnAtStageStart()
    {
        currentHealth = maxHealth;
        spawnPoint = stageStartPoint;
        Respawn(stageStartPoint);
        Debug.Log("게임 오버! 스테이지 처음으로 리스폰: " + stageStartPoint);
    }

    private void Respawn(Vector3 respawnPoint)
    {
        transform.position = respawnPoint;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = defaultGravityScale;
        invincibleTimer = invincibleDuration;
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
