using System;
using UnityEngine;

public enum DialogueSpeaker
{
    Dandi,
    NPC
}

[Serializable]
public class DialogueLine
{
    public DialogueSpeaker speaker;
    public string speakerName;

    [TextArea(2, 4)]
    public string text;
}
