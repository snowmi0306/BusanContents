using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TitleMenu : MonoBehaviour
{
    private const string ButtonClickSfx = "sfx_ui_click";
    private const float ButtonClickStartOffset = 0.3f;
    private const float ButtonActionDelay = 0.1f;

    [SerializeField] private string startSceneName = "StoryCutScene";
    private bool buttonActionStarted;

    public void StartGame()
    {
        if (buttonActionStarted)
            return;

        buttonActionStarted = true;
        AudioManager.PlaySfx(ButtonClickSfx, 1f, ButtonClickStartOffset);
        StartCoroutine(LoadStartSceneAfterClick());
    }

    public void QuitGame()
    {
        if (buttonActionStarted)
            return;

        buttonActionStarted = true;
        AudioManager.PlaySfx(ButtonClickSfx, 1f, ButtonClickStartOffset);
        StartCoroutine(QuitAfterClick());
    }

    private IEnumerator LoadStartSceneAfterClick()
    {
        yield return new WaitForSecondsRealtime(ButtonActionDelay);
        SceneManager.LoadScene(startSceneName);
    }

    private IEnumerator QuitAfterClick()
    {
        yield return new WaitForSecondsRealtime(ButtonActionDelay);
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
