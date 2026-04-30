using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Battle 씬 진입점.
/// 
/// 이번 단계에서 강화된 역할:
/// - Execute Turn 이후 승패가 나면 실제 Reward / Result로 연결
/// - 여러 턴 지속 루프의 상태를 로그로 확인
/// - UI 가이드는 아직 구현하지 않고, 현재는 데이터와 흐름만 검증
/// </summary>
public class BattleSceneEntryPoint : BaseSceneEntryPoint
{
    [Header("Battle Components")]
    [SerializeField] private BattleCardLibrary battleCardLibrary;
    [SerializeField] private BattleFlowController battleFlowController;
    [SerializeField] private BattleExecutionResolver battleExecutionResolver;
    [SerializeField] private BattleActionPresentationController battleActionPresentationController;

    [Header("Runtime Spawn Rules")]
    [SerializeField] private bool randomizePlayerStartOnBattleEnter = true;

    [Header("Battle Runtime (Debug View)")]
    [SerializeField] private BattleRuntimeState battleRuntimeState = new BattleRuntimeState();

    [Header("Fallback Grid / Preset")]
    [SerializeField] private string fallbackBattlefieldPresetId = "Battlefield_Default_6x5";
    [SerializeField] private int fallbackGridWidth = 6;
    [SerializeField] private int fallbackGridHeight = 5;
    [SerializeField] private GridPosition fallbackPlayerStartPosition = new GridPosition(0, 2);
    [SerializeField] private GridPosition fallbackEnemyStartPosition = new GridPosition(5, 2);

    [Header("Fallback Battle Stats")]
    [SerializeField] private int fallbackEnemyMaxHp = 10;
    [SerializeField] private int fallbackEnemyCurrentHp = 10;

    [Header("Battle End Options")]
    [SerializeField] private bool autoTransitionOnBattleEnd = true;
    [SerializeField] private int defaultRewardHealAmount = 3;

    [Header("Debug")]
    [SerializeField] private bool logOnSceneEnter = true;

    private PendingBattleRequest cachedRequest;
    private bool hasHandledBattleEnd = false;

    public BattleRuntimeState RuntimeState => battleRuntimeState;

    protected override void OnInitializeScene()
    {
        if (RunStateService.Instance != null)
        {
            RunStateService.Instance.SetCurrentGameState(RunStateType.Battle);
            cachedRequest = RunStateService.Instance.GetPendingBattleRequest();
        }

        if (cachedRequest == null)
        {
            cachedRequest = CreateFallbackRequest();
        }

        EnsureDependencies();
        InitializeRuntimeStateFromRequest(cachedRequest);
        hasHandledBattleEnd = false;

        if (battleFlowController != null)
        {
            battleFlowController.InitializeBattle(battleRuntimeState, battleCardLibrary);
        }

        if (battleActionPresentationController != null)
        {
            battleActionPresentationController.InitializePresentation(battleRuntimeState);
        }

        if (logOnSceneEnter)
        {
            Debug.Log(BuildBattleRequestDebugText());
            Debug.Log(BuildBattleFlowDebugText());
        }
    }

    private void Update()
    {
        if (autoTransitionOnBattleEnd)
        {
            TryHandleBattleEndTransition();
        }
    }

    private void EnsureDependencies()
    {
        if (battleCardLibrary == null)
        {
            battleCardLibrary = FindFirstObjectByType<BattleCardLibrary>();
        }

        if (battleFlowController == null)
        {
            battleFlowController = FindFirstObjectByType<BattleFlowController>();
        }

        if (battleExecutionResolver == null)
        {
            battleExecutionResolver = FindFirstObjectByType<BattleExecutionResolver>();
        }

        if (battleActionPresentationController == null)
        {
            battleActionPresentationController = FindFirstObjectByType<BattleActionPresentationController>();
        }
    }

    private void InitializeRuntimeStateFromRequest(PendingBattleRequest request)
    {
        battleRuntimeState = new BattleRuntimeState();
        battleRuntimeState.currentPhase = BattleFlowPhase.None;
        battleRuntimeState.currentTurnIndex = 1;
        battleRuntimeState.maxEnergy = 3;
        battleRuntimeState.currentEnergy = 3;
        battleRuntimeState.outcome = BattleOutcome.None;

        battleRuntimeState.playerActor = new BattleActorRuntime
        {
            side = BattleActorSide.Player,
            actorId = "Player",
            currentGridPosition = ResolvePlayerStartGridPositionForBattle(request),
            currentHp = request.playerCurrentHpSnapshot > 0 ? request.playerCurrentHpSnapshot : 10,
            maxHp = ResolvePlayerMaxHp(request),
            currentBlock = 0,
            isDead = false
        };

        battleRuntimeState.enemyActor = new BattleActorRuntime
        {
            side = BattleActorSide.Enemy,
            actorId = string.IsNullOrWhiteSpace(request.primaryEnemyPresetId) ? "Enemy_Default" : request.primaryEnemyPresetId,
            currentGridPosition = ResolveEnemyStartGridPosition(request),
            currentHp = fallbackEnemyCurrentHp,
            maxHp = fallbackEnemyMaxHp,
            currentBlock = 0,
            isDead = false
        };

        battleRuntimeState.playerReservedActions = new List<ReservedActionData>();
        battleRuntimeState.enemyReservedActions = new List<ReservedActionData>();
        battleRuntimeState.deckRuntime = new BattleDeckRuntimeState
        {
            drawPileCardIds = new List<string>(),
            handCardIds = new List<string>(),
            discardPileCardIds = new List<string>(),
            fieldPileCardIds = new List<string>()
        };

        if (request.deckSnapshot != null)
        {
            for (int i = 0; i < request.deckSnapshot.Count; i++)
            {
                DeckEntryRuntimeData entry = request.deckSnapshot[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.cardId) || entry.count <= 0)
                {
                    continue;
                }

                for (int j = 0; j < entry.count; j++)
                {
                    battleRuntimeState.deckRuntime.drawPileCardIds.Add(entry.cardId);
                }
            }
        }
    }

    public void DebugReserveMoveCard()
    {
        if (battleFlowController == null)
        {
            return;
        }

        GridPosition target = new GridPosition(battleRuntimeState.playerActor.currentGridPosition.x + 1, battleRuntimeState.playerActor.currentGridPosition.y);
        bool reserved = battleFlowController.TryReservePlayerCard("Move_1", target);

        if (reserved)
        {
            Debug.Log(BuildBattleFlowDebugText());
        }
    }

    public void DebugReserveAttackCard()
    {
        if (battleFlowController == null)
        {
            return;
        }

        bool reserved = battleFlowController.TryReservePlayerCard("Attack_3", battleRuntimeState.enemyActor.currentGridPosition, battleRuntimeState.enemyActor.actorId);

        if (reserved)
        {
            Debug.Log(BuildBattleFlowDebugText());
        }
    }

    public void DebugBuildEnemyPlan()
    {
        if (battleFlowController == null)
        {
            return;
        }

        battleFlowController.BuildDefaultEnemyPlan();
        Debug.Log(BuildBattleFlowDebugText());
    }

    public void DebugExecuteTurn()
    {
        if (battleFlowController == null)
        {
            return;
        }

        if (battleActionPresentationController != null)
        {
            battleActionPresentationController.PlayExecuteTurnPresentation();
        }
        else
        {
            battleFlowController.ExecuteCurrentTurn();
        }

        Debug.Log(BuildBattleFlowDebugText());

        if (autoTransitionOnBattleEnd)
        {
            TryHandleBattleEndTransition();
        }
    }

    public void DebugBattleWin()
    {
        HandleBattleWinTransition();
    }

    public void DebugBattleLose()
    {
        HandleBattleLoseTransition();
    }

    private void TryHandleBattleEndTransition()
    {
        if (hasHandledBattleEnd || battleFlowController == null || battleFlowController.RuntimeState == null)
        {
            return;
        }

        if (battleActionPresentationController != null && battleActionPresentationController.IsPlayingSequence)
        {
            return;
        }

        if (battleFlowController.RuntimeState.outcome == BattleOutcome.Win)
        {
            HandleBattleWinTransition();
        }
        else if (battleFlowController.RuntimeState.outcome == BattleOutcome.Lose)
        {
            HandleBattleLoseTransition();
        }
    }

    private void HandleBattleWinTransition()
    {
        if (hasHandledBattleEnd || RunStateService.Instance == null || RunFlowController.Instance == null)
        {
            return;
        }

        hasHandledBattleEnd = true;

        BattleResultRuntimeData result = new BattleResultRuntimeData
        {
            outcome = BattleOutcome.Win,
            clearedRoomId = cachedRequest != null ? cachedRequest.roomId : string.Empty,
            remainingHp = battleRuntimeState.playerActor.currentHp,
            autoHealAmount = defaultRewardHealAmount,
            rewardCardCandidateIds = new List<string> { "Move_1", "Attack_3", "Guard_3" },
            canRemoveCard = false,
            canUpgradeCard = false
        };

        RunStateService.Instance.ApplyBattleResult(result);
        RunStateService.Instance.CreatePendingRewardFromBattleResult(RewardSourceType.Battle);
        RunFlowController.Instance.GoToReward(RunSceneEnterReason.BattleWon);
    }

    private void HandleBattleLoseTransition()
    {
        if (hasHandledBattleEnd || RunStateService.Instance == null || RunFlowController.Instance == null)
        {
            return;
        }

        hasHandledBattleEnd = true;

        BattleResultRuntimeData result = new BattleResultRuntimeData
        {
            outcome = BattleOutcome.Lose,
            clearedRoomId = cachedRequest != null ? cachedRequest.roomId : string.Empty,
            remainingHp = 0,
            autoHealAmount = 0,
            rewardCardCandidateIds = new List<string>(),
            canRemoveCard = false,
            canUpgradeCard = false
        };

        RunStateService.Instance.ApplyBattleResult(result);
        RunFlowController.Instance.GoToResult(RunSceneEnterReason.BattleLost);
    }

    private PendingBattleRequest CreateFallbackRequest()
    {
        PendingBattleRequest fallback = new PendingBattleRequest
        {
            roomId = "DebugRoom_01",
            encounterId = "DebugEncounter_01",
            primaryEnemyPresetId = "Enemy_Test",
            battlefieldPresetId = fallbackBattlefieldPresetId,
            playerCurrentHpSnapshot = 10,
            playerStartGridPosition = fallbackPlayerStartPosition,
            enemyStartGridPosition = fallbackEnemyStartPosition,
            battleGridWidth = fallbackGridWidth,
            battleGridHeight = fallbackGridHeight,
            deckSnapshot = new List<DeckEntryRuntimeData>
            {
                new DeckEntryRuntimeData { cardId = "Move_1", count = 1 },
                new DeckEntryRuntimeData { cardId = "Attack_3", count = 1 },
                new DeckEntryRuntimeData { cardId = "Guard_3", count = 1 },
                new DeckEntryRuntimeData { cardId = "Dash_2", count = 1 }
            }
        };

        return fallback;
    }

    private GridPosition ResolvePlayerStartGridPositionForBattle(PendingBattleRequest request)
    {
        int width = request.battleGridWidth > 0 ? request.battleGridWidth : fallbackGridWidth;
        int height = request.battleGridHeight > 0 ? request.battleGridHeight : fallbackGridHeight;

        if (randomizePlayerStartOnBattleEnter)
        {
            return new GridPosition(Random.Range(0, Mathf.Max(1, width)), Random.Range(0, Mathf.Max(1, height)));
        }

        if (request.playerStartGridPosition.x == 0 && request.playerStartGridPosition.y == 0)
        {
            return fallbackPlayerStartPosition;
        }

        return request.playerStartGridPosition;
    }

    private GridPosition ResolveEnemyStartGridPosition(PendingBattleRequest request)
    {
        if (request.enemyStartGridPosition.x == 0 && request.enemyStartGridPosition.y == 0)
        {
            return fallbackEnemyStartPosition;
        }

        return request.enemyStartGridPosition;
    }

    private int ResolvePlayerMaxHp(PendingBattleRequest request)
    {
        if (RunStateService.Instance != null && RunStateService.Instance.CurrentRun != null)
        {
            return Mathf.Max(1, RunStateService.Instance.CurrentRun.player.maxHp);
        }

        return Mathf.Max(1, request.playerCurrentHpSnapshot);
    }

    public string BuildBattleRequestDebugText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[BattleSceneEntryPoint] Battle 초기화 확인");
        sb.AppendLine($"- roomId = {cachedRequest.roomId}");
        sb.AppendLine($"- encounterId = {cachedRequest.encounterId}");
        sb.AppendLine($"- primaryEnemyPresetId = {cachedRequest.primaryEnemyPresetId}");
        sb.AppendLine($"- battlefieldPresetId = {cachedRequest.battlefieldPresetId}");
        sb.AppendLine($"- gridSize = {cachedRequest.battleGridWidth} x {cachedRequest.battleGridHeight}");
        sb.AppendLine($"- playerStart = ({ResolvePlayerStartGridPositionForBattle(cachedRequest).x}, {ResolvePlayerStartGridPositionForBattle(cachedRequest).y})");
        sb.AppendLine($"- enemyStart = ({ResolveEnemyStartGridPosition(cachedRequest).x}, {ResolveEnemyStartGridPosition(cachedRequest).y})");
        return sb.ToString();
    }

    public string BuildBattleFlowDebugText()
    {
        BattleRuntimeState state = battleFlowController != null ? battleFlowController.RuntimeState : battleRuntimeState;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[BattleFlowController]");
        sb.AppendLine($"- Phase = {state.currentPhase}");
        sb.AppendLine($"- Turn = {state.currentTurnIndex}");
        sb.AppendLine($"- Energy = {state.currentEnergy}/{state.maxEnergy}");
        sb.AppendLine($"- Player Pos = ({state.playerActor.currentGridPosition.x}, {state.playerActor.currentGridPosition.y})");
        sb.AppendLine($"- Enemy Pos = ({state.enemyActor.currentGridPosition.x}, {state.enemyActor.currentGridPosition.y})");
        sb.AppendLine($"- Player HP = {state.playerActor.currentHp}/{state.playerActor.maxHp}");
        sb.AppendLine($"- Enemy HP = {state.enemyActor.currentHp}/{state.enemyActor.maxHp}");
        sb.AppendLine($"- HandCount = {state.deckRuntime.handCardIds.Count}");
        sb.AppendLine($"- DrawPileCount = {state.deckRuntime.drawPileCardIds.Count}");
        sb.AppendLine($"- DiscardCount = {state.deckRuntime.discardPileCardIds.Count}");
        sb.AppendLine($"- FieldCount = {state.deckRuntime.fieldPileCardIds.Count}");
        sb.AppendLine($"- PlayerReserved = {state.playerReservedActions.Count}");
        sb.AppendLine($"- EnemyReserved = {state.enemyReservedActions.Count}");
        sb.AppendLine($"- PlayerBlock = {state.playerActor.currentBlock}");
        sb.AppendLine($"- EnemyBlock = {state.enemyActor.currentBlock}");
        sb.AppendLine($"- Outcome = {state.outcome}");
        return sb.ToString();
    }
}
