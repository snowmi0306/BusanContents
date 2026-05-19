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
        {
            activeEffect.SetActive(false);
        }
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

        Vector3 checkpointPosition = respawnPoint != null
            ? respawnPoint.position
            : transform.position;

        player.SetCheckpoint(checkpointPosition);
        activated = true;

        if (activeEffect != null)
        {
            StartCoroutine(PlayActiveEffect());
        }

        Debug.Log("체크포인트 저장 완료");
    }

    private IEnumerator PlayActiveEffect()
    {
        activeEffect.SetActive(true);

        Transform effectTransform = activeEffect.transform;
        SpriteRenderer sr = activeEffect.GetComponent<SpriteRenderer>();

        Vector3 endPos = effectTransform.localPosition;
        Vector3 startPos = endPos + Vector3.down * effectMoveDistance;

        effectTransform.localPosition = startPos;

        if (sr != null)
        {
            Color color = sr.color;
            color.a = 0f;
            sr.color = color;
        }

        float time = 0f;

        while (time < effectDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / effectDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            effectTransform.localPosition = Vector3.Lerp(startPos, endPos, smoothT);

            if (sr != null)
            {
                Color color = sr.color;
                color.a = Mathf.Lerp(0f, 1f, smoothT);
                sr.color = color;
            }

            yield return null;
        }

        effectTransform.localPosition = endPos;

        if (sr != null)
        {
            Color color = sr.color;
            color.a = 1f;
            sr.color = color;
        }
    }
}