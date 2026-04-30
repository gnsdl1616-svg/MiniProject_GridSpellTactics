[System.Serializable]
public class BattleCardDefinition
{
    public string cardId;
    public string displayName;

    public BattleCardActionType actionType = BattleCardActionType.None;
    public BattleTargetType targetType = BattleTargetType.None;

    public int energyCost = 0;

    public int moveDistance = 0;
    public int damageValue = 0;
    public int blockValue = 0;

    public bool goesToFieldPile = false;
}