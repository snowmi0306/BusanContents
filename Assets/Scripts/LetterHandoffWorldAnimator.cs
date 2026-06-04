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

    [Header("Rendering")]
    [SerializeField] private bool overrideRendererSorting = true;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 500;

    [Header("Visibility Layer")]
    [SerializeField] private bool overrideGameObjectLayer = true;
    [SerializeField] private string gameObjectLayerName = "Default";

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
        PrepareLetterObjectForAnimation(letterObject);
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
        letterObject.SetActive(false);

        GameObject effectObject = SpawnArrivalEffect(endPosition);
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

        PrepareVisualObjectForCutscene(effectObject);

        ParticleSystem particleSystem = effectObject.GetComponentInChildren<ParticleSystem>();
        if (particleSystem != null)
        {
            particleSystem.Play();
        }

        return effectObject;
    }

    private void PrepareLetterObjectForAnimation(GameObject letterObject)
    {
        if (letterObject == null)
            return;

        LetterIconPickup[] pickups = letterObject.GetComponentsInChildren<LetterIconPickup>(true);
        for (int i = 0; i < pickups.Length; i++)
        {
            pickups[i].enabled = false;
        }

        Collider2D[] colliders = letterObject.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        PrepareVisualObjectForCutscene(letterObject);
    }

    private void PrepareVisualObjectForCutscene(GameObject targetObject)
    {
        ApplyGameObjectLayer(targetObject);
        ApplyRendererSorting(targetObject);
    }

    private void ApplyGameObjectLayer(GameObject targetObject)
    {
        if (!overrideGameObjectLayer || targetObject == null || string.IsNullOrEmpty(gameObjectLayerName))
            return;

        int layer = LayerMask.NameToLayer(gameObjectLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"LetterHandoffWorldAnimator could not find layer '{gameObjectLayerName}'. Keeping the prefab layer.", this);
            return;
        }

        Transform[] childTransforms = targetObject.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < childTransforms.Length; i++)
        {
            childTransforms[i].gameObject.layer = layer;
        }
    }

    private void ApplyRendererSorting(GameObject targetObject)
    {
        if (!overrideRendererSorting || targetObject == null)
            return;

        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!string.IsNullOrEmpty(sortingLayerName))
            {
                renderers[i].sortingLayerName = sortingLayerName;
            }

            renderers[i].sortingOrder = sortingOrder;
        }
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
