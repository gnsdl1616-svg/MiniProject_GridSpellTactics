using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 예약된 행동을 실제 런타임 상태에 반영하는 실행기입니다.
/// 
/// 이번 버전의 목적:
/// - 최근 4줄 전투 로그를 UI에서 바로 읽을 수 있게 유지
/// - Wait는 카드 순환에 전혀 영향을 주지 않음
/// - 실행 순서를 플레이어 > 적 번갈아 처리할 수 있도록 시퀀스 제공
/// </summary>
public class BattleExecutionResolver : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool logExecution = true;
    [SerializeField] private int maxRecentLogCount = 4;

    private readonly List<string> recentLogs = new List<string>();
    public IReadOnlyList<string> RecentLogs => recentLogs;

    public void ExecuteTurn(BattleRuntimeState runtimeState, BattleCardLibrary battleCardLibrary)
    {
        if (runtimeState == null)
        {
            Debug.LogWarning("[BattleExecutionResolver] ExecuteTurn 실패: runtimeState가 null입니다.", this);
            return;
        }

        List<ReservedActionData> sequence = BuildAlternatingExecutionSequence(runtimeState.playerReservedActions, runtimeState.enemyReservedActions);
        for (int i = 0; i < sequence.Count; i++)
        {
            ExecuteSingleAction(runtimeState, sequence[i], battleCardLibrary);
        }
    }

    public List<ReservedActionData> BuildAlternatingExecutionSequence(List<ReservedActionData> playerActions, List<ReservedActionData> enemyActions)
    {
        List<ReservedActionData> sequence = new List<ReservedActionData>();

        List<ReservedActionData> playerSorted = playerActions != null ? new List<ReservedActionData>(playerActions) : new List<ReservedActionData>();
        List<ReservedActionData> enemySorted = enemyActions != null ? new List<ReservedActionData>(enemyActions) : new List<ReservedActionData>();

        playerSorted.Sort((a, b) => a.executionOrder.CompareTo(b.executionOrder));
        enemySorted.Sort((a, b) => a.executionOrder.CompareTo(b.executionOrder));

        int maxCount = Mathf.Max(playerSorted.Count, enemySorted.Count);
        for (int i = 0; i < maxCount; i++)
        {
            if (i < playerSorted.Count)
            {
                sequence.Add(playerSorted[i]);
            }

            if (i < enemySorted.Count)
            {
                sequence.Add(enemySorted[i]);
            }
        }

        return sequence;
    }

    public string GetRecentLogsText()
    {
        if (recentLogs.Count <= 0)
        {
            return "BattleLog\n-";
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("BattleLog");
        for (int i = 0; i < recentLogs.Count; i++)
        {
            sb.Append("- ").Append(recentLogs[i]);
            if (i < recentLogs.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    public void ClearRecentLogs()
    {
        recentLogs.Clear();
    }

    public void ExecuteSingleAction(BattleRuntimeState runtimeState, ReservedActionData action, BattleCardLibrary battleCardLibrary)
    {
        if (action == null)
        {
            return;
        }

        BattleActorRuntime actor = GetActor(runtimeState, action.actorSide);
        if (actor == null || actor.isDead)
        {
            return;
        }

        switch (action.actionType)
        {
            case BattleCardActionType.Move:
                actor.currentGridPosition = action.targetGridPosition;
                AddLog($"Move -> {actor.actorId} to ({actor.currentGridPosition.x}, {actor.currentGridPosition.y})");
                break;

            case BattleCardActionType.Attack:
                BattleActorRuntime target = ResolveAttackTarget(runtimeState, action);
                if (target != null && !target.isDead)
                {
                    ApplyDamage(target, action.damageValue);
                    AddLog($"Attack -> {actor.actorId} dealt {action.damageValue} to {target.actorId} (HP={target.currentHp}, Block={target.currentBlock})");
                }
                break;

            case BattleCardActionType.Defense:
                actor.currentBlock += Mathf.Max(0, action.blockValue);
                AddLog($"Defense -> {actor.actorId} gained Block {action.blockValue} (CurrentBlock={actor.currentBlock})");
                break;

            case BattleCardActionType.Wait:
                AddLog($"Wait -> {actor.actorId}");
                break;

            case BattleCardActionType.EndTurn:
                AddLog($"EndTurn -> {actor.actorId}");
                break;
        }

        if (action.actorSide == BattleActorSide.Player && !string.IsNullOrWhiteSpace(action.sourceCardId))
        {
            MoveUsedCardToNextPile(runtimeState, action.sourceCardId, battleCardLibrary);
        }

        runtimeState.playerActor.isDead = runtimeState.playerActor.currentHp <= 0;
        runtimeState.enemyActor.isDead = runtimeState.enemyActor.currentHp <= 0;
    }

    private BattleActorRuntime ResolveAttackTarget(BattleRuntimeState runtimeState, ReservedActionData action)
    {
        if (!string.IsNullOrWhiteSpace(action.targetActorId))
        {
            if (runtimeState.playerActor != null && runtimeState.playerActor.actorId == action.targetActorId)
            {
                return runtimeState.playerActor;
            }

            if (runtimeState.enemyActor != null && runtimeState.enemyActor.actorId == action.targetActorId)
            {
                return runtimeState.enemyActor;
            }
        }

        return action.actorSide == BattleActorSide.Player ? runtimeState.enemyActor : runtimeState.playerActor;
    }

    private void ApplyDamage(BattleActorRuntime target, int damage)
    {
        int remainingDamage = Mathf.Max(0, damage);

        if (target.currentBlock > 0)
        {
            int blockUsed = Mathf.Min(target.currentBlock, remainingDamage);
            target.currentBlock -= blockUsed;
            remainingDamage -= blockUsed;
        }

        if (remainingDamage > 0)
        {
            target.currentHp = Mathf.Max(0, target.currentHp - remainingDamage);
        }
    }

    private void MoveUsedCardToNextPile(BattleRuntimeState runtimeState, string cardId, BattleCardLibrary battleCardLibrary)
    {
        if (runtimeState.deckRuntime == null || string.IsNullOrWhiteSpace(cardId))
        {
            return;
        }

        bool goesToFieldPile = false;
        if (battleCardLibrary != null && battleCardLibrary.TryGetCardDefinition(cardId, out BattleCardDefinition definition))
        {
            goesToFieldPile = definition.goesToFieldPile;
        }

        if (goesToFieldPile)
        {
            runtimeState.deckRuntime.fieldPileCardIds.Add(cardId);
        }
        else
        {
            runtimeState.deckRuntime.discardPileCardIds.Add(cardId);
        }
    }

    private BattleActorRuntime GetActor(BattleRuntimeState runtimeState, BattleActorSide side)
    {
        switch (side)
        {
            case BattleActorSide.Player:
                return runtimeState.playerActor;
            case BattleActorSide.Enemy:
                return runtimeState.enemyActor;
            default:
                return null;
        }
    }

    private void AddLog(string message)
    {
        recentLogs.Add(message);
        while (recentLogs.Count > Mathf.Max(1, maxRecentLogCount))
        {
            recentLogs.RemoveAt(0);
        }

        if (!logExecution)
        {
            return;
        }

        Debug.Log($"[BattleExecutionResolver] {message}", this);
    }
}
