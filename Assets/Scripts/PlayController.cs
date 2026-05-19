using Spine;
using Spine.Unity;
using UnityEngine;

public class PlayController : MonoBehaviour
{
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

    [Header("Damage Knockback")]
    public Vector2 damageKnockbackForce = new Vector2(6f, 5f);


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
    private bool isGrounded;
    private float currentStamina;
    private float lastGroundTime;
    private float defaultGravityScale;
    private string currentBaseAnimationName;

    private int currentHealth;
    private float invincibleTimer = 0f;
    private Vector3 spawnPoint;
    private Vector3 stageStartPoint;

    void Start()
    {
        Application.targetFrameRate = 60;

        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        defaultGravityScale = rb.gravityScale;
        AssignGroundCheckIfNeeded();
        AssignSkeletonAnimationIfNeeded();

        currentStamina = maxStamina;
        currentHealth = maxHealth;
        spawnPoint = transform.position;
        stageStartPoint = transform.position;
    }

    void Update()
    {
        if (invincibleTimer > 0)
        {
            invincibleTimer -= Time.deltaTime;
        }

        float moveInput = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        FlipSpine(moveInput);

        isGrounded = CheckGrounded();

        if (isGrounded)
        {
            lastGroundTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        bool isGliding = Input.GetKey(KeyCode.Space) && !isGrounded && rb.linearVelocity.y < 0 && currentStamina > 0;

        if (isGliding)
        {
            rb.gravityScale = glideGravity;

            currentStamina -= glideStaminaCost * Time.deltaTime;
            currentStamina = Mathf.Max(0, currentStamina);

            Debug.Log(currentStamina.ToString("F2"));
        }
        else
        {
            rb.gravityScale = defaultGravityScale;

            if (isGrounded)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);

                //Debug.Log("Stamina : " + currentStamina.ToString("F2"));
            }
        }

        UpdateSpineAnimation(moveInput, isGliding);
    }

    private void AssignGroundCheckIfNeeded()
    {
        if (groundCheck != null && groundCheck.IsChildOf(transform))
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

    private void AssignSkeletonAnimationIfNeeded()
    {
        if (skeletonAnimation != null)
        {
            return;
        }

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == spineObjectName && child.TryGetComponent(out SkeletonAnimation foundSkeletonAnimation))
            {
                skeletonAnimation = foundSkeletonAnimation;
                return;
            }
        }

        skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);
    }

    private bool CheckGrounded()
    {
        Vector2 checkPosition = GetGroundCheckPosition();
        return Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer) != null;
    }

    private Vector2 GetGroundCheckPosition()
    {
        if (groundCheck != null && groundCheck.IsChildOf(transform))
        {
            return groundCheck.position;
        }

        if (playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            return new Vector2(bounds.center.x, bounds.min.y - groundCheckRadius * 0.5f);
        }

        return transform.position;
    }

    private void FlipSpine(float moveInput)
    {
        if (skeletonAnimation == null || Mathf.Abs(moveInput) <= walkInputThreshold)
        {
            return;
        }

        skeletonAnimation.Skeleton.ScaleX = moveInput < 0 ? -1f : 1f;
    }

    private void UpdateSpineAnimation(float moveInput, bool isGliding)
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

        skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
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

        TrackEntry hitEntry = skeletonAnimation.AnimationState.SetAnimation(1, hitAnimationName, false);
        hitEntry.Complete += delegate
        {
            skeletonAnimation.AnimationState.ClearTrack(1);
        };
    }

    public void ApplyKnockback(Vector2 hitPoint)
    {
        if (rb == null)
        {
            return;
        }

        float directionX = transform.position.x >= hitPoint.x ? 1f : -1f;
        Vector2 knockback = new Vector2(directionX * damageKnockbackForce.x, damageKnockbackForce.y);
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockback, ForceMode2D.Impulse);
    }


    public void TakeDamage(int damage = 1, bool shouldRespawn = true)
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
        else if (shouldRespawn)
        {
            RespawnAtCheckpoint();
        }
    }

    /// <summary>
    /// 체크포인트에서 리스폰 (체력 > 0)
    /// </summary>
    public void RespawnAtCheckpoint()
    {
        invincibleTimer = invincibleDuration;
        transform.position = spawnPoint;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = defaultGravityScale;

        Debug.Log("체크포인트에서 리스폰! 위치: " + spawnPoint);
    }

    /// <summary>
    /// 스테이지 처음으로 리스폰 (체력 = 0)
    /// </summary>
    public void RespawnAtStageStart()
    {
        currentHealth = maxHealth;
        invincibleTimer = invincibleDuration;
        transform.position = stageStartPoint;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = defaultGravityScale;
        spawnPoint = stageStartPoint;

        Debug.Log("게임 오버! 스테이지 처음으로 리스폰: " + stageStartPoint);
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
        return currentStamina / maxStamina;
    }

    public void RecoverStamina(float amount)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
        Debug.Log("Stamina : " + currentStamina.ToString("F2"));
    }

    public bool TryHeal(int amount)
    {
        if (currentHealth >= maxHealth)
            return false;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        return true;
    }
}
