using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Battle의 전체 흐름을 관리하는 컨트롤러.
/// 
/// 이번 버전의 업데이트:
/// - 적 계획은 Planning 진입 시 자동 생성되어 항상 먼저 보임
/// - 이동 카드 2회 이상 예약 시 '마지막 예약 이동 목표 위치'를 기준으로 다음 이동 범위를 계산 가능
/// - Wait는 카드가 아닌 예약 전용 행동
/// - 연출 컨트롤러가 실행 순서를 직접 재생할 수 있도록 Execute / Resolve 단계를 분리
/// </summary>
public class BattleFlowController : MonoBehaviour
{
    [Header("Battle Defaults")]
    [SerializeField] private int defaultStartEnergy = 3;
    [SerializeField] private int defaultEnemyMaxHp = 10;
    [SerializeField] private int openingHandCount = 4;

    [Header("Turn Loop Options")]
    [SerializeField] private bool refillHandToOpeningCountEachTurn = true;
    [SerializeField] private bool reshuffleDiscardWhenDrawEmpty = true;
    [SerializeField] private bool discardRemainingHandOnTurnEnd = false;

    [Header("Wait Reservation")]
    [SerializeField] private int maxWaitReservationsPerTurn = 2;

    [Header("Links")]
    [SerializeField] private BattleExecutionResolver battleExecutionResolver;

    [Header("Runtime (Debug View)")]
    [SerializeField] private BattleRuntimeState runtimeState = new BattleRuntimeState();

    [Header("Debug")]
    [SerializeField] private bool logPhaseChange = true;
    [SerializeField] private bool logDeckLoop = true;

    private BattleCardLibrary battleCardLibrary;

    public BattleRuntimeState RuntimeState => runtimeState;
    public int MaxWaitReservationsPerTurn => maxWaitReservationsPerTurn;
    public BattleExecutionResolver ExecutionResolver => battleExecutionResolver;
    public BattleCardLibrary CardLibrary => battleCardLibrary;

    public void InitializeBattle(BattleRuntimeState state, BattleCardLibrary cardLibrary)
    {
        runtimeState = state ?? new BattleRuntimeState();
        battleCardLibrary = cardLibrary;

        if (battleExecutionResolver == null)
        {
            battleExecutionResolver = GetComponent<BattleExecutionResolver>();
        }

        EnsureRuntimeContainers();
        InitializeBattleDefaults();
        DrawOpeningHand();
        EnterPlanningPhase();
    }

    public void EnterPlanningPhase()
    {
        runtimeState.currentPhase = BattleFlowPhase.Planning;
        EnsureEnemyPlanPreview();
        LogPhase("Planning");
    }

    public void EnterExecutePhase()
    {
        runtimeState.currentPhase = BattleFlowPhase.Execute;
        LogPhase("Execute");
    }

    public void EnterResolvePhase()
    {
        runtimeState.currentPhase = BattleFlowPhase.Resolve;
        LogPhase("Resolve");
    }

    public bool TryReservePlayerCard(string cardId, GridPosition targetGridPosition, string targetActorId = "")
    {
        if (runtimeState == null || runtimeState.currentPhase != BattleFlowPhase.Planning)
        {
            return false;
        }

        if (runtimeState.deckRuntime == null || runtimeState.deckRuntime.handCardIds == null)
        {
            return false;
        }

        int handIndex = runtimeState.deckRuntime.handCardIds.IndexOf(cardId);
        if (handIndex < 0)
        {
            Debug.LogWarning($"[BattleFlowController] 손패에 카드가 없습니다. cardId={cardId}", this);
            return false;
        }

        if (battleCardLibrary == null || !battleCardLibrary.TryGetCardDefinition(cardId, out BattleCardDefinition cardDefinition))
        {
            Debug.LogWarning($"[BattleFlowController] 카드 정의를 찾지 못했습니다. cardId={cardId}", this);
            return false;
        }

        if (runtimeState.currentEnergy < cardDefinition.energyCost)
        {
            Debug.LogWarning($"[BattleFlowController] 에너지가 부족합니다. current={runtimeState.currentEnergy}, need={cardDefinition.energyCost}", this);
            return false;
        }

        runtimeState.currentEnergy -= cardDefinition.energyCost;
        runtimeState.deckRuntime.handCardIds.RemoveAt(handIndex);

        GridPosition resolvedTarget = ResolvePlayerActionTarget(cardDefinition, targetGridPosition);
        ReservedActionData reservedAction = new ReservedActionData
        {
            actorSide = BattleActorSide.Player,
            sourceCardId = cardDefinition.cardId,
            actionType = cardDefinition.actionType,
            originGridPosition = GetPlannedPlayerPositionBeforeNewReservation(),
            targetGridPosition = resolvedTarget,
            targetActorId = ResolvePlayerActionTargetActorId(cardDefinition, targetActorId),
            moveDistance = cardDefinition.moveDistance,
            damageValue = cardDefinition.damageValue,
            blockValue = cardDefinition.blockValue,
            executionOrder = runtimeState.playerReservedActions.Count
        };

        runtimeState.playerReservedActions.Add(reservedAction);
        return true;
    }

    public bool TryReserveWaitAction()
    {
        if (runtimeState == null || runtimeState.currentPhase != BattleFlowPhase.Planning)
        {
            return false;
        }

        if (GetCurrentWaitReservationCount() >= maxWaitReservationsPerTurn)
        {
            return false;
        }

        GridPosition latestPlannedPosition = GetPlannedPlayerPositionBeforeNewReservation();
        ReservedActionData waitAction = new ReservedActionData
        {
            actorSide = BattleActorSide.Player,
            sourceCardId = string.Empty,
            actionType = BattleCardActionType.Wait,
            originGridPosition = latestPlannedPosition,
            targetGridPosition = latestPlannedPosition,
            targetActorId = string.Empty,
            moveDistance = 0,
            damageValue = 0,
            blockValue = 0,
            executionOrder = runtimeState.playerReservedActions.Count
        };

        runtimeState.playerReservedActions.Add(waitAction);
        return true;
    }

    public void BuildDefaultEnemyPlan()
    {
        EnsureEnemyPlanPreview(true);
    }

    public void PrepareExecutePhaseIfNeeded()
    {
        if (runtimeState == null)
        {
            return;
        }

        if (battleExecutionResolver == null)
        {
            battleExecutionResolver = GetComponent<BattleExecutionResolver>();
        }

        if (runtimeState.enemyReservedActions == null || runtimeState.enemyReservedActions.Count == 0)
        {
            EnsureEnemyPlanPreview(true);
        }

        EnterExecutePhase();
    }

    public void ExecuteCurrentTurn()
    {
        if (runtimeState == null)
        {
            return;
        }

        if (battleExecutionResolver == null)
        {
            Debug.LogWarning("[BattleFlowController] BattleExecutionResolver가 연결되지 않았습니다.", this);
            return;
        }

        PrepareExecutePhaseIfNeeded();
        battleExecutionResolver.ExecuteTurn(runtimeState, battleCardLibrary);
        CompleteTurnAfterExecution();
    }

    public void CompleteTurnAfterExecution()
    {
        if (runtimeState == null)
        {
            return;
        }

        EnterResolvePhase();
        ResolveAfterExecution();
    }

    public int GetCurrentWaitReservationCount()
    {
        if (runtimeState == null || runtimeState.playerReservedActions == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < runtimeState.playerReservedActions.Count; i++)
        {
            if (runtimeState.playerReservedActions[i] != null && runtimeState.playerReservedActions[i].actionType == BattleCardActionType.Wait)
            {
                count += 1;
            }
        }

        return count;
    }

    public GridPosition GetPlannedPlayerPositionBeforeNewReservation()
    {
        if (runtimeState == null || runtimeState.playerReservedActions == null || runtimeState.playerReservedActions.Count == 0)
        {
            return runtimeState.playerActor.currentGridPosition;
        }

        GridPosition latest = runtimeState.playerActor.currentGridPosition;
        for (int i = 0; i < runtimeState.playerReservedActions.Count; i++)
        {
            ReservedActionData action = runtimeState.playerReservedActions[i];
            if (action == null || action.actorSide != BattleActorSide.Player)
            {
                continue;
            }

            if (action.actionType == BattleCardActionType.Move)
            {
                latest = action.targetGridPosition;
            }
        }

        return latest;
    }

    private void ResolveAfterExecution()
    {
        EvaluateOutcome();

        if (runtimeState.outcome != BattleOutcome.None)
        {
            runtimeState.currentPhase = BattleFlowPhase.End;
            LogPhase($"End ({runtimeState.outcome})");
            return;
        }

        runtimeState.playerReservedActions.Clear();
        runtimeState.enemyReservedActions.Clear();

        runtimeState.playerActor.currentBlock = 0;
        runtimeState.enemyActor.currentBlock = 0;

        if (discardRemainingHandOnTurnEnd)
        {
            DiscardRemainingHandToDiscardPile();
        }

        runtimeState.currentTurnIndex += 1;
        runtimeState.currentEnergy = runtimeState.maxEnergy;

        if (refillHandToOpeningCountEachTurn)
        {
            DrawToOpeningHandCount();
        }

        EnterPlanningPhase();
    }

    private void EvaluateOutcome()
    {
        runtimeState.playerActor.isDead = runtimeState.playerActor.currentHp <= 0;
        runtimeState.enemyActor.isDead = runtimeState.enemyActor.currentHp <= 0;

        if (runtimeState.enemyActor.isDead)
        {
            runtimeState.outcome = BattleOutcome.Win;
        }
        else if (runtimeState.playerActor.isDead)
        {
            runtimeState.outcome = BattleOutcome.Lose;
        }
        else
        {
            runtimeState.outcome = BattleOutcome.None;
        }
    }

    private void EnsureRuntimeContainers()
    {
        if (runtimeState.playerActor == null)
        {
            runtimeState.playerActor = new BattleActorRuntime();
        }

        if (runtimeState.enemyActor == null)
        {
            runtimeState.enemyActor = new BattleActorRuntime();
        }

        if (runtimeState.playerReservedActions == null)
        {
            runtimeState.playerReservedActions = new List<ReservedActionData>();
        }

        if (runtimeState.enemyReservedActions == null)
        {
            runtimeState.enemyReservedActions = new List<ReservedActionData>();
        }

        if (runtimeState.deckRuntime == null)
        {
            runtimeState.deckRuntime = new BattleDeckRuntimeState();
        }

        if (runtimeState.deckRuntime.drawPileCardIds == null)
        {
            runtimeState.deckRuntime.drawPileCardIds = new List<string>();
        }

        if (runtimeState.deckRuntime.handCardIds == null)
        {
            runtimeState.deckRuntime.handCardIds = new List<string>();
        }

        if (runtimeState.deckRuntime.discardPileCardIds == null)
        {
            runtimeState.deckRuntime.discardPileCardIds = new List<string>();
        }

        if (runtimeState.deckRuntime.fieldPileCardIds == null)
        {
            runtimeState.deckRuntime.fieldPileCardIds = new List<string>();
        }
    }

    private void InitializeBattleDefaults()
    {
        if (runtimeState.maxEnergy <= 0)
        {
            runtimeState.maxEnergy = defaultStartEnergy;
        }

        runtimeState.currentEnergy = runtimeState.maxEnergy;
        runtimeState.currentTurnIndex = Mathf.Max(1, runtimeState.currentTurnIndex);
        runtimeState.outcome = BattleOutcome.None;

        if (runtimeState.enemyActor.maxHp <= 0)
        {
            runtimeState.enemyActor.maxHp = defaultEnemyMaxHp;
        }

        if (runtimeState.enemyActor.currentHp <= 0)
        {
            runtimeState.enemyActor.currentHp = runtimeState.enemyActor.maxHp;
        }
    }

    private void DrawOpeningHand()
    {
        DrawToOpeningHandCount();
    }

    private void DrawToOpeningHandCount()
    {
        while (runtimeState.deckRuntime.handCardIds.Count < openingHandCount)
        {
            if (!TryDrawOneCard())
            {
                break;
            }
        }
    }

    private bool TryDrawOneCard()
    {
        if (runtimeState.deckRuntime.drawPileCardIds.Count <= 0)
        {
            if (!ReshuffleDiscardIntoDrawPile())
            {
                return false;
            }
        }

        if (runtimeState.deckRuntime.drawPileCardIds.Count <= 0)
        {
            return false;
        }

        string cardId = runtimeState.deckRuntime.drawPileCardIds[0];
        runtimeState.deckRuntime.drawPileCardIds.RemoveAt(0);
        runtimeState.deckRuntime.handCardIds.Add(cardId);
        LogDeck($"Draw -> {cardId} (Hand={runtimeState.deckRuntime.handCardIds.Count}, DrawPile={runtimeState.deckRuntime.drawPileCardIds.Count})");
        return true;
    }

    private bool ReshuffleDiscardIntoDrawPile()
    {
        if (!reshuffleDiscardWhenDrawEmpty || runtimeState.deckRuntime.discardPileCardIds.Count <= 0)
        {
            return false;
        }

        List<string> discard = runtimeState.deckRuntime.discardPileCardIds;
        for (int i = discard.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            string temp = discard[i];
            discard[i] = discard[randomIndex];
            discard[randomIndex] = temp;
        }

        runtimeState.deckRuntime.drawPileCardIds.AddRange(discard);
        runtimeState.deckRuntime.discardPileCardIds.Clear();
        LogDeck($"Reshuffle -> DrawPile={runtimeState.deckRuntime.drawPileCardIds.Count}, Discard=0");
        return true;
    }

    private void DiscardRemainingHandToDiscardPile()
    {
        if (runtimeState.deckRuntime.handCardIds.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < runtimeState.deckRuntime.handCardIds.Count; i++)
        {
            runtimeState.deckRuntime.discardPileCardIds.Add(runtimeState.deckRuntime.handCardIds[i]);
        }

        LogDeck($"Discard Remaining Hand -> {runtimeState.deckRuntime.handCardIds.Count} cards");
        runtimeState.deckRuntime.handCardIds.Clear();
    }

    private void EnsureEnemyPlanPreview(bool forceRebuild = false)
    {
        if (runtimeState == null || runtimeState.enemyActor == null || runtimeState.enemyActor.isDead)
        {
            return;
        }

        if (!forceRebuild && runtimeState.enemyReservedActions != null && runtimeState.enemyReservedActions.Count > 0)
        {
            return;
        }

        runtimeState.enemyReservedActions.Clear();

        ReservedActionData enemyAction = new ReservedActionData
        {
            actorSide = BattleActorSide.Enemy,
            sourceCardId = "Enemy_Attack_2",
            actionType = BattleCardActionType.Attack,
            originGridPosition = runtimeState.enemyActor.currentGridPosition,
            targetGridPosition = runtimeState.playerActor.currentGridPosition,
            targetActorId = runtimeState.playerActor.actorId,
            moveDistance = 0,
            damageValue = 2,
            blockValue = 0,
            executionOrder = 0
        };

        runtimeState.enemyReservedActions.Add(enemyAction);
    }

    private GridPosition ResolvePlayerActionTarget(BattleCardDefinition definition, GridPosition inputTarget)
    {
        switch (definition.actionType)
        {
            case BattleCardActionType.Move:
                return inputTarget;
            case BattleCardActionType.Attack:
                return runtimeState.enemyActor.currentGridPosition;
            case BattleCardActionType.Defense:
                return GetPlannedPlayerPositionBeforeNewReservation();
            default:
                return inputTarget;
        }
    }

    private string ResolvePlayerActionTargetActorId(BattleCardDefinition definition, string inputTargetActorId)
    {
        switch (definition.actionType)
        {
            case BattleCardActionType.Attack:
                return !string.IsNullOrWhiteSpace(inputTargetActorId)
                    ? inputTargetActorId
                    : runtimeState.enemyActor.actorId;
            default:
                return inputTargetActorId;
        }
    }

    private void LogPhase(string phaseName)
    {
        if (!logPhaseChange)
        {
            return;
        }

        Debug.Log($"[BattleFlowController] - Phase = {phaseName}", this);
    }

    private void LogDeck(string message)
    {
        if (!logDeckLoop)
        {
            return;
        }

        Debug.Log($"[BattleFlowController] {message}", this);
    }
}
