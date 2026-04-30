[System.Serializable]
public class ReservedActionData
{
    public BattleActorSide actorSide = BattleActorSide.None;

    public string sourceCardId;
    public BattleCardActionType actionType = BattleCardActionType.None;

    public GridPosition originGridPosition;
    public GridPosition targetGridPosition;

    public string targetActorId;

    public int moveDistance = 0;
    public int damageValue = 0;
    public int blockValue = 0;

    public int executionOrder = 0;
}