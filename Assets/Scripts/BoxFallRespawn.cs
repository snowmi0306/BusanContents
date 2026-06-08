using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BoxFallRespawn : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || other.GetComponent<FallZone>() == null)
            return;

        RespawnAtInitialPosition();
    }

    private void RespawnAtInitialPosition()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.position = initialPosition;
        rb.rotation = initialRotation.eulerAngles.z;

        transform.SetPositionAndRotation(initialPosition, initialRotation);
        Physics2D.SyncTransforms();
    }
}
