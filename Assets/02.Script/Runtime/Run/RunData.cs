using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunData
{
    [Header("Run State")]
    public bool isRunActive = false;
    public int currentFloor = 1;
    public string currentRoomId = string.Empty;

    [Header("Player Run Data")]
    public PlayerRunData player = new PlayerRunData();

    [Header("Deck / Relic / Currency")]
    public List<DeckEntryRuntimeData> currentDeck = new List<DeckEntryRuntimeData>();
    public List<string> currentRelicIds = new List<string>();
    public int temporaryCurrency = 0;

    [Header("Map Progress")]
    public List<RoomProgressData> roomProgressList = new List<RoomProgressData>();

    [Header("Pending / Result")]
    public PendingBattleRequest pendingBattleRequest = null;
    public BattleResultRuntimeData lastBattleResult = null;
    public PendingRewardData pendingReward = null;
    public ReturnPointData returnPoint = null;
}

[Serializable]
public class PlayerRunData
{
    public int currentHp = 0;
    public int maxHp = 0;
    public List<string> activeRelicBuffIds = new List<string>();
}

[Serializable]
public class DeckEntryRuntimeData
{
    public string cardId;
    public int count = 1;
}

[Serializable]
public class RoomProgressData
{
    public string roomId;
    public string roomType;
    public RoomProgressState roomState = RoomProgressState.Unvisited;
    public List<string> connectedRoomIds = new List<string>();
    public bool isCleared = false;
    public bool isLocked = false;
}

[Serializable]
public class PendingBattleRequest
{
    public string roomId;
    public string encounterId;
    public string primaryEnemyPresetId;
    public List<string> enemyPresetIds = new List<string>();
    public int playerCurrentHpSnapshot;
    public List<DeckEntryRuntimeData> deckSnapshot = new List<DeckEntryRuntimeData>();
    public List<string> relicSnapshot = new List<string>();
    public List<string> buffSnapshot = new List<string>();
    public bool isBossBattle = false;

    [Header("Battlefield")]
    public string battlefieldPresetId = "Battlefield_Default_6x5";
    public GridPosition playerStartGridPosition = new GridPosition(0, 2);
    public GridPosition enemyStartGridPosition = new GridPosition(5, 2);
    public int battleGridWidth = 6;
    public int battleGridHeight = 5;
}

[Serializable]
public class BattleResultRuntimeData
{
    public BattleOutcome outcome = BattleOutcome.None;
    public string clearedRoomId = string.Empty;
    public int remainingHp = 0;

    [Header("Reward Seed / Candidates")]
    public int autoHealAmount = 0;
    public List<string> rewardCardCandidateIds = new List<string>();
    public bool canRemoveCard = false;
    public bool canUpgradeCard = false;
}

[Serializable]
public class PendingRewardData
{
    public RewardSourceType sourceType = RewardSourceType.None;
    public string sourceRoomId = string.Empty;
    public int autoHealAmount = 0;
    public List<string> candidateCardIds = new List<string>();
    public bool canRemoveCard = false;
    public bool canUpgradeCard = false;
    public bool shouldOpenHubAfterResolve = false;
}

[Serializable]
public class ReturnPointData
{
    public string roomId = string.Empty;
    public Vector3 interactionWorldPosition = Vector3.zero;
    public string interactionObjectId = string.Empty;
}

[Serializable]
public class NextSceneTransitionData
{
    public string targetSceneName = string.Empty;
    public RunStateType targetGameState = RunStateType.None;
    public RunSceneEnterReason enterReason = RunSceneEnterReason.Unknown;
    public bool hasPendingTransition = false;
}
