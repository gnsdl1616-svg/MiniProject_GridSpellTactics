using System.Collections.Generic;
using UnityEngine;

public class TitleSceneEntryPoint : BaseSceneEntryPoint
{
    [Header("New Run Defaults")]
    [SerializeField] private int defaultStartFloor = 1;
    [SerializeField] private int defaultPlayerMaxHp = 30;
    [SerializeField] private int defaultPlayerStartHp = 30;
    [SerializeField] private string defaultStartRoomId = "CombatRoom_01";
    [SerializeField] private List<DeckEntryRuntimeData> defaultStartDeck = new List<DeckEntryRuntimeData>();

    protected override void OnInitializeScene()
    {
        if (RunStateService.Instance != null)
        {
            RunStateService.Instance.SetCurrentGameState(RunStateType.Title);
            RunStateService.Instance.ClearPendingBattleRequest();
            RunStateService.Instance.ClearBattleResult();
            RunStateService.Instance.ClearPendingReward();
            RunStateService.Instance.ClearReturnPoint();
        }
    }

    public void OnClickNewRun()
    {
        if (!ValidateCoreManagers())
            return;

        RunStateService.Instance.ResetAllRuntimeState();
        RunStateService.Instance.StartNewRun(
            defaultPlayerMaxHp,
            defaultPlayerStartHp,
            defaultStartDeck,
            defaultStartFloor,
            defaultStartRoomId);
        RunStateService.Instance.SetCurrentGameState(RunStateType.Title);

        RunFlowController.Instance.GoToAdventure(RunSceneEnterReason.NewRun);
    }

    public void OnClickContinue()
    {
        if (!ValidateCoreManagers())
            return;

        if (!RunStateService.Instance.HasActiveRun)
        {
            Debug.LogWarning("[TitleSceneEntryPoint] Continue 가능한 활성 런 데이터가 없습니다.");
            return;
        }

        RunFlowController.Instance.GoToAdventure(RunSceneEnterReason.ContinueRun);
    }

    public void OnClickSettings()
    {
        Debug.Log("[TitleSceneEntryPoint] Settings는 다음 단계에서 연결합니다.");
    }

    private bool ValidateCoreManagers()
    {
        if (RunStateService.Instance == null)
        {
            Debug.LogWarning("[TitleSceneEntryPoint] RunStateService가 없습니다.");
            return false;
        }

        if (RunFlowController.Instance == null)
        {
            Debug.LogWarning("[TitleSceneEntryPoint] RunFlowController가 없습니다.");
            return false;
        }

        return true;
    }
}
