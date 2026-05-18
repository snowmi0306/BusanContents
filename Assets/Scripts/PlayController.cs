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

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private bool isGrounded;
    private float currentStamina;
    private float lastGroundTime;
    private float defaultGravityScale;

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

    public void TakeDamage(int damage = 1)
    {
        if (invincibleTimer > 0)
        {
            Debug.Log("무적 상태");
            return;
        }

        currentHealth -= damage;
        invincibleTimer = invincibleDuration;

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
        invincibleTimer = invincibleDuration;
        transform.position = spawnPoint;
        rb.linearVelocity = Vector2.zero;

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
}
