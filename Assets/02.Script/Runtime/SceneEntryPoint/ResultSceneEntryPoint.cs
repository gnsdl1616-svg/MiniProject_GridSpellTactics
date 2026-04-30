using UnityEngine;

public class ResultSceneEntryPoint : BaseSceneEntryPoint
{
    [Header("Debug")]
    [SerializeField] private bool logResultOnEnter = true;

    protected override void OnInitializeScene()
    {
        if (RunStateService.Instance != null)
        {
            RunStateService.Instance.SetCurrentGameState(RunStateType.Result);
        }

        if (logResultOnEnter)
        {
            Debug.Log("[ResultSceneEntryPoint] 패배 결과 씬 진입");
        }
    }

    public void ConfirmResultAndGoToTitle()
    {
        if (RunStateService.Instance != null)
        {
            RunStateService.Instance.EndCurrentRun();
        }

        if (RunFlowController.Instance != null)
        {
            RunFlowController.Instance.GoToTitle(RunSceneEnterReason.ReturnToTitle);
        }
    }
}