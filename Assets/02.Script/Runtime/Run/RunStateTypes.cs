using UnityEngine;

public enum RunStateType
{
    None,
    Boot,
    Title,
    AdventureMap,
    Battle,
    Reward,
    DeckbuildingHub,
    Result
}

public enum RunSceneEnterReason
{
    Unknown,
    Bootstrap,
    NewRun,
    ContinueRun,
    RoomInteraction,
    NodeCombatSelected,
    BattleWon,
    BattleLost,
    RewardResolved,
    HubClosed,
    ReturnToTitle
}

public enum RoomProgressState
{
    Unvisited,
    Visited,
    CombatCleared,
    Locked
}

public enum BattleOutcome
{
    None,
    Win,
    Lose
}

public enum RewardSourceType
{
    None,
    Battle,
    Event,
    Shop,
    Elite,
    Boss
}

public enum BattleFlowPhase
{
    None,
    Planning,
    EnemyPlan,
    Execute,
    Resolve,
    End
}

public enum BattleCardActionType
{
    None,
    Move,
    Attack,
    Defense,
    Utility,
    Wait,
    EndTurn
}

public enum BattleTargetType
{
    None,
    Self,
    TileSelect,
    EnemySelect
}

public enum BattleActorSide
{
    None,
    Player,
    Enemy
}
