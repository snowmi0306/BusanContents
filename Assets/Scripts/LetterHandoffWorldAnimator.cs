using System;
using System.Collections;
using UnityEngine;

public class LetterHandoffWorldAnimator : MonoBehaviour
{
    [Header("Letter")]
    [SerializeField] private GameObject letterPrefab;

    [Header("Arrival Effect")]
    [SerializeField] private GameObject arrivalEffectPrefab;
    [SerializeField] private float arrivalEffectLifetime = 1f;
    [SerializeField] private Vector3 arrivalEffectOffset = Vector3.zero;

    [Header("Position Offset")]
    [SerializeField] private Vector3 playerStartOffset = new Vector3(0.35f, 0.75f, 0f);
    [SerializeField] private Vector3 npcEndOffset = new Vector3(-0.35f, 0.85f, 0f);

    [Header("Arc Motion")]
    [SerializeField] private float arcHeight = 1.2f;
    [SerializeField] private float moveDuration = 0.8f;

    [Header("Motion Visual")]
    [SerializeField] private float endScaleMultiplier = 0.8f;

    private Coroutine routine;

    public void Play(Transform playerTransform, Transform npcTransform, Action onComplete)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(PlayRoutine(playerTransform, npcTransform, onComplete));
    }

    private IEnumerator PlayRoutine(Transform playerTransform, Transform npcTransform, Action onComplete)
    {
        if (letterPrefab == null || playerTransform == null || npcTransform == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector3 startPosition = playerTransform.position + playerStartOffset;
        Vector3 endPosition = npcTransform.position + npcEndOffset;
        Vector3 controlPosition = (startPosition + endPosition) * 0.5f + Vector3.up * arcHeight;

        GameObject letterObject = Instantiate(letterPrefab, startPosition, Quaternion.identity);
        SpriteRenderer letterRenderer = letterObject.GetComponentInChildren<SpriteRenderer>();

        Vector3 startScale = letterObject.transform.localScale;
        Vector3 endScale = startScale * endScaleMultiplier;

        SetAlpha(letterRenderer, 1f);

        float time = 0f;

        while (time < moveDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / moveDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 position = GetQuadraticBezierPoint(
                startPosition,
                controlPosition,
                endPosition,
                smoothT
            );

            letterObject.transform.position = position;
            letterObject.transform.localScale = Vector3.Lerp(startScale, endScale, smoothT);

            yield return null;
        }

        letterObject.transform.position = endPosition;
        letterObject.transform.localScale = endScale;

        // 도착 순간 편지 비활성화
        letterObject.SetActive(false);

        // 도착 위치에 이펙트 출력
        GameObject effectObject = SpawnArrivalEffect(endPosition);

        // 이펙트를 1초 정도 보여줌
        float waitTime = Mathf.Max(0f, arrivalEffectLifetime);

        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        if (effectObject != null)
        {
            Destroy(effectObject);
        }

        Destroy(letterObject);

        routine = null;
        onComplete?.Invoke();
    }

    private GameObject SpawnArrivalEffect(Vector3 endPosition)
    {
        if (arrivalEffectPrefab == null)
            return null;

        Vector3 effectPosition = endPosition + arrivalEffectOffset;

        GameObject effectObject = Instantiate(
            arrivalEffectPrefab,
            effectPosition,
            Quaternion.identity
        );

        ParticleSystem particleSystem = effectObject.GetComponentInChildren<ParticleSystem>();
        if (particleSystem != null)
        {
            particleSystem.Play();
        }

        return effectObject;
    }

    private Vector3 GetQuadraticBezierPoint(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float oneMinusT = 1f - t;

        return oneMinusT * oneMinusT * start
            + 2f * oneMinusT * t * control
            + t * t * end;
    }

    private void SetAlpha(SpriteRenderer spriteRenderer, float alpha)
    {
        if (spriteRenderer == null)
            return;

        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }
}
