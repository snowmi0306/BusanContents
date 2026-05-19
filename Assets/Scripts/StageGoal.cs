using UnityEngine;
using UnityEngine.SceneManagement;

public class StageGoal : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "EndingScene";

    private bool isCleared;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCleared)
            return;

        if (!other.CompareTag("Player"))
            return;

        isCleared = true;
        SceneManager.LoadScene(nextSceneName);
    }
}