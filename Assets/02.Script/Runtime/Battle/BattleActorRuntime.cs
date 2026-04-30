[System.Serializable]
public class BattleActorRuntime
{
    public BattleActorSide side = BattleActorSide.None;
    public string actorId;

    public GridPosition currentGridPosition;

    public int currentHp = 0;
    public int maxHp = 0;

    public int currentBlock = 0;

    public bool isDead = false;
}