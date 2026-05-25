using System;
using UnityEngine;

public class PlayerLetterInventory : MonoBehaviour
{
    [SerializeField, Min(0)] private int currentLetterCount = 0;
    [SerializeField, Min(0)] private int requiredLetterCount = 3;

    public event Action<int, int> OnLetterCountChanged;

    private void Awake()
    {
        currentLetterCount = Mathf.Max(0, currentLetterCount);
        requiredLetterCount = Mathf.Max(0, requiredLetterCount);
        NotifyLetterCountChanged();
    }

    public void AddLetter(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int previous = currentLetterCount;
        currentLetterCount = Mathf.Max(0, currentLetterCount + amount);

        if (previous != currentLetterCount)
        {
            NotifyLetterCountChanged();
        }
    }

    public bool HasRequiredLetters()
    {
        return currentLetterCount >= requiredLetterCount;
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

        int previous = currentLetterCount;
        currentLetterCount = Mathf.Max(0, currentLetterCount - amount);

        if (previous != currentLetterCount)
        {
            NotifyLetterCountChanged();
        }

        return true;
    }

    public int GetCurrentLetterCount()
    {
        return currentLetterCount;
    }

    public int GetRequiredLetterCount()
    {
        return requiredLetterCount;
    }

    private void NotifyLetterCountChanged()
    {
        OnLetterCountChanged?.Invoke(currentLetterCount, requiredLetterCount);
    }
}
