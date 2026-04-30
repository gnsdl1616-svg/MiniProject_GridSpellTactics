using System.Collections.Generic;

[System.Serializable]
public class BattleRuntimeState
{
    public BattleFlowPhase currentPhase = BattleFlowPhase.None;
    public int currentTurnIndex = 1;

    public int currentEnergy = 0;
    public int maxEnergy = 3;

    public BattleActorRuntime playerActor = new BattleActorRuntime();
    public BattleActorRuntime enemyActor = new BattleActorRuntime();

    public List<ReservedActionData> playerReservedActions = new List<ReservedActionData>();
    public List<ReservedActionData> enemyReservedActions = new List<ReservedActionData>();

    public BattleDeckRuntimeState deckRuntime = new BattleDeckRuntimeState();

    public BattleOutcome outcome = BattleOutcome.None;
}