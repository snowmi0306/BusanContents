using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageGoal : MonoBehaviour
{
    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "Stage2";

    [Header("Transition")]
    [SerializeField] private float transitionDelay = 1.5f;
    [SerializeField] private GameObject transitionPanel;
    [SerializeField] private bool disablePlayerControl = true;

    private bool isCleared;

    private void Awake()
    {
        if (transitionPanel != null)
        {
            transitionPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCleared)
            return;

        if (!other.CompareTag("Player"))
            return;

        isCleared = true;

        if (disablePlayerControl)
        {
            PlayController player = other.GetComponent<PlayController>();
            if (player != null)
            {
                player.enabled = false;
            }

            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        if (transitionPanel != null)
        {
            transitionPanel.SetActive(true);
        }

        StartCoroutine(GoToNextSceneAfterDelay());
    }

    private IEnumerator GoToNextSceneAfterDelay()
    {
        yield return new WaitForSeconds(transitionDelay);

        SceneManager.LoadScene(nextSceneName);
    }
}