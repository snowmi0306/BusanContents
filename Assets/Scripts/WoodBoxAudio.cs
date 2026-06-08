using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class WoodBoxAudio : MonoBehaviour
{
    private const string Stage2SceneName = "Stage2";
    private const string Stage3SceneName = "Stage3";

    [Header("Clips")]
    [SerializeField] private string pushSfxName = "sfx_woodbox_push";
    [SerializeField] private string dropSfxName = "sfx_woodbox_drop";

    [Header("Push")]
    [SerializeField] private float minPushSpeed = 0.08f;
    [SerializeField] private float pushCooldown = 0.35f;
    [SerializeField, Range(0f, 1f)] private float pushVolume = 0.75f;

    [Header("Drop")]
    [SerializeField] private float minDropSpeed = 1.2f;
    [SerializeField] private float dropCooldown = 0.15f;

    private Rigidbody2D rb;
    private float lastVerticalVelocity;
    private float nextPushTime;
    private float nextDropTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            lastVerticalVelocity = rb.linearVelocity.y;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!ShouldPlayInCurrentScene() || collision == null || Time.time < nextDropTime)
            return;

        if (lastVerticalVelocity > -minDropSpeed || !HasUpwardContact(collision))
            return;

        AudioManager.PlaySfx(dropSfxName, 1f, 0.5f);
        nextDropTime = Time.time + dropCooldown;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!ShouldPlayInCurrentScene() || collision == null || Time.time < nextPushTime)
            return;

        if (rb == null || Mathf.Abs(rb.linearVelocity.x) < minPushSpeed)
            return;

        if (!IsPlayerSideContact(collision))
            return;

        AudioManager.PlaySfx(pushSfxName, pushVolume);
        nextPushTime = Time.time + pushCooldown;
    }

    private static bool ShouldPlayInCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName == Stage2SceneName || sceneName == Stage3SceneName;
    }

    private static bool HasUpwardContact(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.2f)
                return true;
        }

        return false;
    }

    private static bool IsPlayerSideContact(Collision2D collision)
    {
        if (collision.gameObject.GetComponentInParent<PlayController>() == null)
            return false;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.x) > 0.3f)
                return true;
        }

        return false;
    }
}
