using UnityEngine;
using UnityEngine.UI;

public class UIImageFrameAnimator : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 12f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool playOnEnable = false;

    private float timer;
    private int currentFrame;
    private bool isPlaying;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        ResetToFirstFrame();
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        if (targetImage == null || frames == null || frames.Length == 0)
            return;

        timer += Time.unscaledDeltaTime;

        float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);

        while (timer >= frameDuration)
        {
            timer -= frameDuration;
            currentFrame++;

            if (currentFrame >= frames.Length)
            {
                if (loop)
                {
                    currentFrame = 0;
                }
                else
                {
                    currentFrame = frames.Length - 1;
                    isPlaying = false;
                }
            }

            targetImage.sprite = frames[currentFrame];
        }
    }

    public void Play()
    {
        if (targetImage == null || frames == null || frames.Length == 0)
            return;

        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void StopAndReset()
    {
        isPlaying = false;
        ResetToFirstFrame();
    }

    public void ResetToFirstFrame()
    {
        timer = 0f;
        currentFrame = 0;

        if (targetImage != null && frames != null && frames.Length > 0)
            targetImage.sprite = frames[0];
    }
}