using System;
using UnityEngine;

public class PlayerLetterInventory : MonoBehaviour
{
    [SerializeField, Min(0)] private int currentLetterCount = 0;

    public event Action<int> OnLetterCountChanged;

    private void Awake()
    {
        currentLetterCount = Mathf.Max(0, currentLetterCount);
        NotifyLetterCountChanged();
    }

    public void AddLetter(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int previousLetterCount = currentLetterCount;
        currentLetterCount = Mathf.Max(0, currentLetterCount + amount);

        if (previousLetterCount != currentLetterCount)
        {
            NotifyLetterCountChanged();
        }
    }

    public bool ConsumeLetters(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (currentLetterCount < amount)
        {
            return false;
        }

        int previousLetterCount = currentLetterCount;
        currentLetterCount = Mathf.Max(0, currentLetterCount - amount);

        if (previousLetterCount != currentLetterCount)
        {
            NotifyLetterCountChanged();
        }

        return true;
    }

    public int GetCurrentLetterCount()
    {
        return currentLetterCount;
    }

    private void NotifyLetterCountChanged()
    {
        OnLetterCountChanged?.Invoke(currentLetterCount);
    }
}
