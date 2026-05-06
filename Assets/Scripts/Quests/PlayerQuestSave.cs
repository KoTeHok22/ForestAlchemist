using System;
using System.Collections.Generic;

[Serializable]
public sealed class PlayerQuestSave
{
    public List<string> boardQuestIds = new List<string>();
    public List<string> activeQuestIds = new List<string>();
}
