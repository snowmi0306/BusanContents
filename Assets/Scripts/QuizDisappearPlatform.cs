using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class QuizDisappearPlatform : MonoBehaviour
{
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float disappearDelay = 3f;

    private Collider2D platformCollider;
    private SpriteRenderer spriteRenderer;
    private bool started;
    private Coroutine countdownCoroutine;

    private void Awake()
    {
        platformCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (countdownText != null)
        {
            countdownText.text = "";
            countdownText.gameObject.SetActive(false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (started)
            return;

        PlayController player = collision.collider.GetComponentInParent<PlayController>();
        if (player == null)
            return;

        started = true;
        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        PlayController player = collision.collider.GetComponentInParent<PlayController>();
        if (player == null)
            return;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        started = false;

        if (countdownText != null)
        {
            countdownText.text = "";
            countdownText.gameObject.SetActive(false);
        }
    }

    private IEnumerator CountdownRoutine()
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            if (countdownText != null)
                countdownText.text = i.ToString();

            yield return new WaitForSeconds(1f);
        }

        yield return new WaitForSeconds(0.2f);

        if (platformCollider != null)
            platformCollider.enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }
}   