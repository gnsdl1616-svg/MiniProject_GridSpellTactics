using System.Collections.Generic;

[System.Serializable]
public class BattleDeckRuntimeState
{
    public List<string> drawPileCardIds = new List<string>();
    public List<string> handCardIds = new List<string>();
    public List<string> discardPileCardIds = new List<string>();
    public List<string> fieldPileCardIds = new List<string>();
}