using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleActionPresentationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleFlowController battleFlowController;
    [SerializeField] private BattleGridViewBinder battleGridViewBinder;

    [Header("Spawn Parents")]
    [SerializeField] private RectTransform playerObjectLayer;
    [SerializeField] private RectTransform enemyObjectLayer;

    [Header("Prefabs")]
    [SerializeField] private BattleUnitView playerViewPrefab;
    [SerializeField] private BattleUnitView enemyViewPrefab;

    [Header("Spawned Views")]
    [SerializeField] private BattleUnitView spawnedPlayerView;
    [SerializeField] private BattleUnitView spawnedEnemyView;

    [Header("Presentation Timing")]
    [SerializeField] private float initializeSnapDelay = 0.2f;
    [SerializeField] private float movePresentationDelay = 0.32f;
    [SerializeField] private float attackLeadDelay = 0.20f;
    [SerializeField] private float attackResolveDelay = 0.28f;
    [SerializeField] private float guardDelay = 0.24f;
    [SerializeField] private float waitDelay = 0.18f;
    [SerializeField] private float deathAnimationDelay = 1.50f;
    [SerializeField] private float betweenActionDelay = 0.20f;
    [SerializeField] private float beforeResolveDelay = 0.30f;

    private bool isInitialized;
    private bool isPlayingSequence;
    private int previousPlayerHp;
    private int previousEnemyHp;
    private int previousPlayerBlock;
    private int previousEnemyBlock;
    private GridPosition previousPlayerGrid;
    private BattleOutcome previousOutcome;

    public bool IsPlayingSequence => isPlayingSequence;

    public void InitializePresentation(BattleRuntimeState runtimeState)
    {
        ResolveReferences();
        SpawnViewsIfNeeded();
        StartCoroutine(CoInitializePresentation(runtimeState));
    }

    public void PlayExecuteTurnPresentation()
    {
        if (isPlayingSequence || battleFlowController == null || battleFlowController.RuntimeState == null)
        {
            return;
        }

        StartCoroutine(CoPlayExecuteTurnPresentation());
    }

    private IEnumerator CoInitializePresentation(BattleRuntimeState runtimeState)
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (battleGridViewBinder != null)
        {
            battleGridViewBinder.RefreshGridCache();
        }

        if (spawnedPlayerView != null)
        {
            spawnedPlayerView.ShowImmediate();
            if (initializeSnapDelay > 0f)
            {
                yield return new WaitForSeconds(initializeSnapDelay);
            }
            battleGridViewBinder.SnapPlayerToGrid(spawnedPlayerView, runtimeState.playerActor.currentGridPosition);
        }

        if (spawnedEnemyView != null)
        {
            spawnedEnemyView.ShowImmediate();
            spawnedEnemyView.SnapTo(Vector2.zero);
        }

        SyncSnapshots(runtimeState);
        isInitialized = true;
    }

    private IEnumerator CoPlayExecuteTurnPresentation()
    {
        isPlayingSequence = true;

        BattleRuntimeState runtimeState = battleFlowController.RuntimeState;
        BattleExecutionResolver executionResolver = battleFlowController.ExecutionResolver;
        BattleCardLibrary cardLibrary = battleFlowController.CardLibrary;

        if (runtimeState == null || executionResolver == null)
        {
            isPlayingSequence = false;
            yield break;
        }

        battleFlowController.PrepareExecutePhaseIfNeeded();

        List<ReservedActionData> sequence = executionResolver.BuildAlternatingExecutionSequence(
            runtimeState.playerReservedActions,
            runtimeState.enemyReservedActions);

        for (int i = 0; i < sequence.Count; i++)
        {
            ReservedActionData action = sequence[i];
            if (action == null)
            {
                continue;
            }

            switch (action.actionType)
            {
                case BattleCardActionType.Move:
                    yield return CoPresentMove(runtimeState, executionResolver, cardLibrary, action);
                    break;

                case BattleCardActionType.Attack:
                    yield return CoPresentAttack(runtimeState, executionResolver, cardLibrary, action);
                    break;

                case BattleCardActionType.Defense:
                    yield return CoPresentDefense(runtimeState, executionResolver, cardLibrary, action);
                    break;

                case BattleCardActionType.Wait:
                    executionResolver.ExecuteSingleAction(runtimeState, action, cardLibrary);
                    yield return WaitIfPositive(waitDelay);
                    SyncSnapshots(runtimeState);
                    break;

                default:
                    executionResolver.ExecuteSingleAction(runtimeState, action, cardLibrary);
                    yield return null;
                    SyncSnapshots(runtimeState);
                    break;
            }

            if (i < sequence.Count - 1)
            {
                yield return WaitIfPositive(betweenActionDelay);
            }
        }

        yield return WaitIfPositive(beforeResolveDelay);
        battleFlowController.CompleteTurnAfterExecution();

        if (runtimeState.outcome == BattleOutcome.Win && spawnedEnemyView != null)
        {
            spawnedEnemyView.PlayDeath();
            if (deathAnimationDelay > 0f)
            {
                yield return new WaitForSeconds(deathAnimationDelay);
            }
        }
        else if (runtimeState.outcome == BattleOutcome.Lose && spawnedPlayerView != null)
        {
            spawnedPlayerView.PlayDeath();
            if (deathAnimationDelay > 0f)
            {
                yield return new WaitForSeconds(deathAnimationDelay);
            }
        }

        SyncSnapshots(runtimeState);
        isPlayingSequence = false;
    }

    private IEnumerator CoPresentMove(BattleRuntimeState runtimeState, BattleExecutionResolver executionResolver, BattleCardLibrary cardLibrary, ReservedActionData action)
    {
        executionResolver.ExecuteSingleAction(runtimeState, action, cardLibrary);

        if (action.actorSide == BattleActorSide.Player && spawnedPlayerView != null)
        {
            spawnedPlayerView.PlayMove();
            battleGridViewBinder.MovePlayerToGrid(spawnedPlayerView, runtimeState.playerActor.currentGridPosition);
        }

        yield return new WaitForSeconds(movePresentationDelay);
        SyncSnapshots(runtimeState);
    }

    private IEnumerator CoPresentAttack(BattleRuntimeState runtimeState, BattleExecutionResolver executionResolver, BattleCardLibrary cardLibrary, ReservedActionData action)
    {
        BattleUnitView actorView = action.actorSide == BattleActorSide.Player ? spawnedPlayerView : spawnedEnemyView;
        BattleUnitView targetView = action.actorSide == BattleActorSide.Player ? spawnedEnemyView : spawnedPlayerView;

        if (actorView != null)
        {
            actorView.PlayAttack();
        }

        if (attackLeadDelay > 0f)
        {
            yield return new WaitForSeconds(attackLeadDelay);
        }

        executionResolver.ExecuteSingleAction(runtimeState, action, cardLibrary);

        if (targetView != null)
        {
            targetView.PlayHit();
        }

        if (attackResolveDelay > 0f)
        {
            yield return new WaitForSeconds(attackResolveDelay);
        }

        SyncSnapshots(runtimeState);
    }

    private IEnumerator CoPresentDefense(BattleRuntimeState runtimeState, BattleExecutionResolver executionResolver, BattleCardLibrary cardLibrary, ReservedActionData action)
    {
        executionResolver.ExecuteSingleAction(runtimeState, action, cardLibrary);

        BattleUnitView actorView = action.actorSide == BattleActorSide.Player ? spawnedPlayerView : spawnedEnemyView;
        if (actorView != null)
        {
            actorView.PlayGuard();
        }

        if (guardDelay > 0f)
        {
            yield return new WaitForSeconds(guardDelay);
        }

        SyncSnapshots(runtimeState);
    }

    private void Update()
    {
        if (!isInitialized || isPlayingSequence || battleFlowController == null || battleFlowController.RuntimeState == null)
        {
            return;
        }

        BattleRuntimeState state = battleFlowController.RuntimeState;

        SyncSnapshots(state);
    }

    private void SpawnViewsIfNeeded()
    {
        if (spawnedPlayerView == null && playerViewPrefab != null && playerObjectLayer != null)
        {
            spawnedPlayerView = Instantiate(playerViewPrefab, playerObjectLayer);
            spawnedPlayerView.gameObject.name = "PlayerObject";
        }

        if (spawnedEnemyView == null && enemyViewPrefab != null && enemyObjectLayer != null)
        {
            spawnedEnemyView = Instantiate(enemyViewPrefab, enemyObjectLayer);
            spawnedEnemyView.gameObject.name = "EnemyObject";
        }
    }

    private void ResolveReferences()
    {
        if (battleFlowController == null)
        {
            battleFlowController = FindFirstObjectByType<BattleFlowController>();
        }

        if (battleGridViewBinder == null)
        {
            battleGridViewBinder = FindFirstObjectByType<BattleGridViewBinder>();
        }
    }

    private IEnumerator WaitIfPositive(float seconds)
    {
        if (seconds > 0f)
        {
            yield return new WaitForSeconds(seconds);
        }
    }

    private void SyncSnapshots(BattleRuntimeState state)
    {
        previousPlayerHp = state.playerActor.currentHp;
        previousEnemyHp = state.enemyActor.currentHp;
        previousPlayerBlock = state.playerActor.currentBlock;
        previousEnemyBlock = state.enemyActor.currentBlock;
        previousPlayerGrid = state.playerActor.currentGridPosition;
        previousOutcome = state.outcome;
    }
}
