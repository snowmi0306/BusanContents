using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform2D : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private Vector2 moveOffset = new Vector2(0f, 2f);
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float waitTime = 0.2f;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 endPosition;
    private Vector2 targetPosition;
    private float waitTimer;

    public Vector2 Velocity { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    private void Start()
    {
        startPosition = rb.position;
        endPosition = startPosition + moveOffset;
        targetPosition = endPosition;
    }

    private void FixedUpdate()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            Velocity = Vector2.zero;
            return;
        }

        Vector2 currentPosition = rb.position;

        Vector2 nextPosition = Vector2.MoveTowards(
            currentPosition,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        Velocity = (nextPosition - currentPosition) / Time.fixedDeltaTime;

        rb.MovePosition(nextPosition);

        if (Vector2.Distance(nextPosition, targetPosition) <= 0.01f)
        {
            targetPosition = targetPosition == endPosition
                ? startPosition
                : endPosition;

            waitTimer = waitTime;
        }
    }
}