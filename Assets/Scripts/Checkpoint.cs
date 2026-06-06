using System.Collections;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint")]
    [SerializeField] private Transform respawnPoint;

    [Header("Effect")]
    [SerializeField] private GameObject activeEffect;
    [SerializeField] private float effectMoveDistance = 0.25f;
    [SerializeField] private float effectDuration = 0.35f;

    private bool activated;

    private void Awake()
    {
        if (activeEffect != null)
            activeEffect.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated)
            return;

        if (!other.CompareTag("Player"))
            return;

        PlayController player = other.GetComponentInParent<PlayController>();
        if (player == null)
            return;

        Vector3 checkpointPosition = respawnPoint != null ? respawnPoint.position : transform.position;

        player.SetCheckpoint(checkpointPosition);
        activated = true;
        AudioManager.PlaySfx("sfx_checkpoint_mailbox");

        if (activeEffect != null)
            StartCoroutine(PlayActiveEffect());

        Debug.Log("Checkpoint saved.");
    }

    private IEnumerator PlayActiveEffect()
    {
        activeEffect.SetActive(true);
        Transform effectTransform = activeEffect.transform;
        SpriteRenderer spriteRenderer = activeEffect.GetComponent<SpriteRenderer>();

        Vector3 endPosition = effectTransform.localPosition;
        Vector3 startPosition = endPosition + Vector3.down * effectMoveDistance;
        effectTransform.localPosition = startPosition;

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0f;
            spriteRenderer.color = color;
        }

        float time = 0f;
        while (time < effectDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / effectDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            effectTransform.localPosition = Vector3.Lerp(startPosition, endPosition, smoothT);

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(0f, 1f, smoothT);
                spriteRenderer.color = color;
            }

            yield return null;
        }

        effectTransform.localPosition = endPosition;

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
    }
}
