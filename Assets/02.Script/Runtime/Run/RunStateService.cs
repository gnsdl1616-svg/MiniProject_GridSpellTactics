using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RunStateService : MonoBehaviour
{
    public static RunStateService Instance { get; private set; }

    [Header("Scene State")]
    [SerializeField] private RunStateType currentGameState = RunStateType.None;
    [SerializeField] private NextSceneTransitionData nextSceneTransition = new NextSceneTransitionData();

    [Header("Current Run")]
    [SerializeField] private RunData currentRun = new RunData();

    private readonly Dictionary<string, RoomProgressData> roomProgressLookup = new Dictionary<string, RoomProgressData>();
    private bool isRoomLookupDirty = true;

    public RunStateType CurrentGameState => currentGameState;
    public NextSceneTransitionData NextSceneTransition => nextSceneTransition;
    public RunData CurrentRun => currentRun;
    public bool HasActiveRun => currentRun != null && currentRun.isRunActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureRuntimeContainers();
    }

    private void EnsureRuntimeContainers()
    {
        if (nextSceneTransition == null)
        {
            nextSceneTransition = new NextSceneTransitionData();
        }

        if (currentRun == null)
        {
            currentRun = new RunData();
        }

        if (currentRun.player == null)
        {
            currentRun.player = new PlayerRunData();
        }

        if (currentRun.currentDeck == null)
        {
            currentRun.currentDeck = new List<DeckEntryRuntimeData>();
        }

        if (currentRun.currentRelicIds == null)
        {
            currentRun.currentRelicIds = new List<string>();
        }

        if (currentRun.roomProgressList == null)
        {
            currentRun.roomProgressList = new List<RoomProgressData>();
        }
    }

    public void ResetAllRuntimeState()
    {
        currentGameState = RunStateType.None;
        nextSceneTransition = new NextSceneTransitionData();
        currentRun = new RunData();
        EnsureRuntimeContainers();
        MarkRoomLookupDirty();
    }

    public void SetGameState(RunStateType gameState)
    {
        currentGameState = gameState;
    }

    public void SetCurrentGameState(RunStateType gameState)
    {
        currentGameState = gameState;
    }

    public void SetNextSceneTransition(string targetSceneName, RunSceneEnterReason enterReason)
    {
        SetNextSceneTransition(targetSceneName, ResolveTargetStateFromSceneName(targetSceneName), enterReason);
    }

    public void SetNextSceneTransition(string targetSceneName, RunStateType targetState, RunSceneEnterReason enterReason)
    {
        EnsureRuntimeContainers();
        nextSceneTransition.targetSceneName = targetSceneName;
        nextSceneTransition.targetGameState = targetState;
        nextSceneTransition.enterReason = enterReason;
        nextSceneTransition.hasPendingTransition = true;
    }

    public void ClearNextSceneTransition()
    {
        nextSceneTransition = new NextSceneTransitionData();
    }

    public void StartNewRun(int startMaxHp, int startCurrentHp, List<DeckEntryRuntimeData> startDeck, int startFloor = 1, string startRoomId = "")
    {
        currentRun = new RunData();
        EnsureRuntimeContainers();

        currentRun.isRunActive = true;
        currentRun.currentFloor = Mathf.Max(1, startFloor);
        currentRun.currentRoomId = startRoomId ?? string.Empty;

        currentRun.player.maxHp = Mathf.Max(1, startMaxHp);
        currentRun.player.currentHp = Mathf.Clamp(startCurrentHp, 0, currentRun.player.maxHp);
        currentRun.player.activeRelicBuffIds.Clear();

        currentRun.currentDeck = CloneDeckEntries(startDeck);
        currentRun.currentRelicIds.Clear();
        currentRun.temporaryCurrency = 0;
        currentRun.roomProgressList.Clear();

        currentRun.pendingBattleRequest = null;
        currentRun.lastBattleResult = null;
        currentRun.pendingReward = null;
        currentRun.returnPoint = null;

        MarkRoomLookupDirty();
    }

    public void EndCurrentRun()
    {
        currentRun = new RunData();
        EnsureRuntimeContainers();
        MarkRoomLookupDirty();
    }

    public void SetCurrentRoom(string roomId)
    {
        EnsureRuntimeContainers();

        if (string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }

        currentRun.currentRoomId = roomId;

        RoomProgressData roomData = GetOrCreateRoomProgress(roomId);
        if (roomData.roomState == RoomProgressState.Unvisited)
        {
            roomData.roomState = RoomProgressState.Visited;
        }
    }

    public RoomProgressData GetRoomProgress(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return null;
        }

        EnsureRoomLookup();
        roomProgressLookup.TryGetValue(roomId, out RoomProgressData roomData);
        return roomData;
    }

    public void UpsertRoomProgress(RoomProgressData roomData)
    {
        EnsureRuntimeContainers();

        if (roomData == null || string.IsNullOrWhiteSpace(roomData.roomId))
        {
            return;
        }

        EnsureRoomLookup();

        if (roomProgressLookup.TryGetValue(roomData.roomId, out RoomProgressData existing))
        {
            existing.roomType = roomData.roomType;
            existing.roomState = roomData.roomState;
            existing.connectedRoomIds = roomData.connectedRoomIds != null ? new List<string>(roomData.connectedRoomIds) : new List<string>();
            existing.isCleared = roomData.isCleared;
            existing.isLocked = roomData.isLocked;
        }
        else
        {
            currentRun.roomProgressList.Add(CloneRoomProgress(roomData));
            MarkRoomLookupDirty();
        }
    }

    public void SetRoomLocked(string roomId, bool isLocked)
    {
        RoomProgressData roomData = GetOrCreateRoomProgress(roomId);
        roomData.isLocked = isLocked;

        if (isLocked)
        {
            roomData.roomState = RoomProgressState.Locked;
        }
        else if (roomData.roomState == RoomProgressState.Locked)
        {
            roomData.roomState = RoomProgressState.Visited;
        }
    }

    public void SetRoomCombatCleared(string roomId)
    {
        RoomProgressData roomData = GetOrCreateRoomProgress(roomId);
        roomData.isCleared = true;
        roomData.isLocked = false;
        roomData.roomState = RoomProgressState.CombatCleared;
    }

    public void SetReturnPoint(string roomId, Vector3 worldPosition, string interactionObjectId = "")
    {
        EnsureRuntimeContainers();
        currentRun.returnPoint = new ReturnPointData
        {
            roomId = roomId ?? string.Empty,
            interactionWorldPosition = worldPosition,
            interactionObjectId = interactionObjectId ?? string.Empty
        };
    }

    public void ClearReturnPoint()
    {
        EnsureRuntimeContainers();
        currentRun.returnPoint = null;
    }

    public void PrepareBattleRequest(
        string roomId,
        string encounterId,
        string primaryEnemyPresetId,
        List<string> enemyPresetIds = null,
        List<string> buffSnapshot = null,
        bool isBossBattle = false,
        string battlefieldPresetId = "Battlefield_Default_6x4",
        int battleGridWidth = 6,
        int battleGridHeight = 4,
        GridPosition? playerStartGridPosition = null,
        GridPosition? enemyStartGridPosition = null)
    {
        EnsureRuntimeContainers();

        currentRun.pendingBattleRequest = new PendingBattleRequest
        {
            roomId = roomId ?? string.Empty,
            encounterId = encounterId ?? string.Empty,
            primaryEnemyPresetId = primaryEnemyPresetId ?? string.Empty,
            enemyPresetIds = enemyPresetIds != null ? new List<string>(enemyPresetIds) : new List<string>(),
            playerCurrentHpSnapshot = currentRun.player.currentHp,
            deckSnapshot = CloneDeckEntries(currentRun.currentDeck),
            relicSnapshot = CloneStringList(currentRun.currentRelicIds),
            buffSnapshot = buffSnapshot != null ? new List<string>(buffSnapshot) : new List<string>(),
            isBossBattle = isBossBattle,
            battlefieldPresetId = battlefieldPresetId,
            battleGridWidth = battleGridWidth,
            battleGridHeight = battleGridHeight,
            playerStartGridPosition = playerStartGridPosition ?? new GridPosition(0, 2),
            enemyStartGridPosition = enemyStartGridPosition ?? new GridPosition(battleGridWidth - 1, 2)
        };
    }

    public PendingBattleRequest GetPendingBattleRequest()
    {
        EnsureRuntimeContainers();
        return currentRun.pendingBattleRequest;
    }

    public void ClearPendingBattleRequest()
    {
        EnsureRuntimeContainers();
        currentRun.pendingBattleRequest = null;
    }

    public void ApplyBattleResult(BattleResultRuntimeData result)
    {
        if (result == null)
        {
            return;
        }

        EnsureRuntimeContainers();
        currentRun.lastBattleResult = CloneBattleResult(result);
        currentRun.player.currentHp = Mathf.Clamp(result.remainingHp, 0, currentRun.player.maxHp);

        if (result.outcome == BattleOutcome.Win)
        {
            SetRoomCombatCleared(result.clearedRoomId);
        }
        else if (result.outcome == BattleOutcome.Lose)
        {
            currentRun.isRunActive = false;
        }
    }

    public BattleResultRuntimeData GetBattleResult()
    {
        EnsureRuntimeContainers();
        return currentRun.lastBattleResult;
    }

    public void ClearBattleResult()
    {
        EnsureRuntimeContainers();
        currentRun.lastBattleResult = null;
    }

    public void CreatePendingRewardFromBattleResult(RewardSourceType sourceType = RewardSourceType.Battle)
    {
        EnsureRuntimeContainers();

        if (currentRun.lastBattleResult == null)
        {
            return;
        }

        BattleResultRuntimeData result = currentRun.lastBattleResult;
        currentRun.pendingReward = new PendingRewardData
        {
            sourceType = sourceType,
            sourceRoomId = result.clearedRoomId,
            autoHealAmount = result.autoHealAmount,
            candidateCardIds = result.rewardCardCandidateIds != null ? new List<string>(result.rewardCardCandidateIds) : new List<string>(),
            canRemoveCard = result.canRemoveCard,
            canUpgradeCard = result.canUpgradeCard,
            shouldOpenHubAfterResolve = result.rewardCardCandidateIds != null && result.rewardCardCandidateIds.Count > 0
        };
    }

    public PendingRewardData GetPendingReward()
    {
        EnsureRuntimeContainers();
        return currentRun.pendingReward;
    }

    public void ApplyPendingRewardAutoHeal()
    {
        EnsureRuntimeContainers();

        if (currentRun.pendingReward == null)
        {
            return;
        }

        currentRun.player.currentHp = Mathf.Clamp(currentRun.player.currentHp + currentRun.pendingReward.autoHealAmount, 0, currentRun.player.maxHp);
    }

    public bool TryAddCardToDeck(string cardId, int amount = 1)
    {
        EnsureRuntimeContainers();

        if (string.IsNullOrWhiteSpace(cardId) || amount <= 0)
        {
            return false;
        }

        for (int i = 0; i < currentRun.currentDeck.Count; i++)
        {
            if (currentRun.currentDeck[i].cardId == cardId)
            {
                currentRun.currentDeck[i].count += amount;
                return true;
            }
        }

        currentRun.currentDeck.Add(new DeckEntryRuntimeData
        {
            cardId = cardId,
            count = amount
        });

        return true;
    }

    public bool TryRemoveCardFromDeck(string cardId, int amount = 1)
    {
        EnsureRuntimeContainers();

        if (string.IsNullOrWhiteSpace(cardId) || amount <= 0)
        {
            return false;
        }

        for (int i = 0; i < currentRun.currentDeck.Count; i++)
        {
            if (currentRun.currentDeck[i].cardId == cardId)
            {
                currentRun.currentDeck[i].count -= amount;

                if (currentRun.currentDeck[i].count <= 0)
                {
                    currentRun.currentDeck.RemoveAt(i);
                }

                return true;
            }
        }

        return false;
    }

    public void ClearPendingReward()
    {
        EnsureRuntimeContainers();
        currentRun.pendingReward = null;
    }

    private void EnsureRoomLookup()
    {
        EnsureRuntimeContainers();

        if (!isRoomLookupDirty)
        {
            return;
        }

        roomProgressLookup.Clear();

        for (int i = 0; i < currentRun.roomProgressList.Count; i++)
        {
            RoomProgressData roomData = currentRun.roomProgressList[i];
            if (roomData == null || string.IsNullOrWhiteSpace(roomData.roomId))
            {
                continue;
            }

            roomProgressLookup[roomData.roomId] = roomData;
        }

        isRoomLookupDirty = false;
    }

    private void MarkRoomLookupDirty()
    {
        isRoomLookupDirty = true;
    }

    private RoomProgressData GetOrCreateRoomProgress(string roomId)
    {
        EnsureRoomLookup();

        if (roomProgressLookup.TryGetValue(roomId, out RoomProgressData roomData))
        {
            return roomData;
        }

        RoomProgressData newRoom = new RoomProgressData
        {
            roomId = roomId,
            roomType = string.Empty,
            roomState = RoomProgressState.Unvisited,
            connectedRoomIds = new List<string>(),
            isCleared = false,
            isLocked = false
        };

        currentRun.roomProgressList.Add(newRoom);
        MarkRoomLookupDirty();
        EnsureRoomLookup();
        return newRoom;
    }

    private RunStateType ResolveTargetStateFromSceneName(string sceneName)
    {
        if (RunFlowController.Instance == null || string.IsNullOrWhiteSpace(sceneName))
        {
            return RunStateType.None;
        }

        if (string.Equals(sceneName, RunFlowController.Instance.BootSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.Boot;
        }

        if (string.Equals(sceneName, RunFlowController.Instance.TitleSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.Title;
        }

        if (string.Equals(sceneName, RunFlowController.Instance.AdventureSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.AdventureMap;
        }

        if (string.Equals(sceneName, RunFlowController.Instance.BattleSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.Battle;
        }

        if (string.Equals(sceneName, RunFlowController.Instance.RewardSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.Reward;
        }

        if (string.Equals(sceneName, RunFlowController.Instance.DeckbuildingSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.DeckbuildingHub;
        }

        if (string.Equals(sceneName, RunFlowController.Instance.ResultSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.Result;
        }

        return RunStateType.None;
    }

    private List<DeckEntryRuntimeData> CloneDeckEntries(List<DeckEntryRuntimeData> source)
    {
        List<DeckEntryRuntimeData> cloned = new List<DeckEntryRuntimeData>();

        if (source == null)
        {
            return cloned;
        }

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] == null)
            {
                continue;
            }

            cloned.Add(new DeckEntryRuntimeData
            {
                cardId = source[i].cardId,
                count = source[i].count
            });
        }

        return cloned;
    }

    private List<string> CloneStringList(List<string> source)
    {
        return source != null ? new List<string>(source) : new List<string>();
    }

    private RoomProgressData CloneRoomProgress(RoomProgressData source)
    {
        if (source == null)
        {
            return null;
        }

        return new RoomProgressData
        {
            roomId = source.roomId,
            roomType = source.roomType,
            roomState = source.roomState,
            connectedRoomIds = source.connectedRoomIds != null ? new List<string>(source.connectedRoomIds) : new List<string>(),
            isCleared = source.isCleared,
            isLocked = source.isLocked
        };
    }

    private BattleResultRuntimeData CloneBattleResult(BattleResultRuntimeData source)
    {
        if (source == null)
        {
            return null;
        }

        return new BattleResultRuntimeData
        {
            outcome = source.outcome,
            clearedRoomId = source.clearedRoomId,
            remainingHp = source.remainingHp,
            autoHealAmount = source.autoHealAmount,
            rewardCardCandidateIds = source.rewardCardCandidateIds != null ? new List<string>(source.rewardCardCandidateIds) : new List<string>(),
            canRemoveCard = source.canRemoveCard,
            canUpgradeCard = source.canUpgradeCard
        };
    }
}
