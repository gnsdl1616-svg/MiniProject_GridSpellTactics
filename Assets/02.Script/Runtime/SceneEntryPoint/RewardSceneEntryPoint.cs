using System.Text;
using UnityEngine;

public class RewardSceneEntryPoint : BaseSceneEntryPoint
{
    [Header("Debug")]
    [SerializeField] private bool logRewardInfoOnEnter = true;

    private PendingRewardData cachedReward;

    protected override void OnInitializeScene()
    {
        if (RunStateService.Instance != null)
        {
            RunStateService.Instance.SetCurrentGameState(RunStateType.Reward);
            cachedReward = RunStateService.Instance.GetPendingReward();

            // Reward 씬 진입 시 자동 회복 먼저 반영
            RunStateService.Instance.ApplyPendingRewardAutoHeal();
        }

        if (logRewardInfoOnEnter)
        {
            Debug.Log(BuildRewardDebugText());
        }
    }

    public void SelectRewardCardByIndex(int index)
    {
        if (RunStateService.Instance == null || RunFlowController.Instance == null)
        {
            return;
        }

        if (cachedReward == null || cachedReward.candidateCardIds == null)
        {
            Debug.LogWarning("[RewardSceneEntryPoint] candidateCardIds가 없습니다.");
            return;
        }

        if (index < 0 || index >= cachedReward.candidateCardIds.Count)
        {
            Debug.LogWarning("[RewardSceneEntryPoint] 보상 카드 index가 범위를 벗어났습니다.");
            return;
        }

        string selectedCardId = cachedReward.candidateCardIds[index];
        bool added = RunStateService.Instance.TryAddCardToDeck(selectedCardId, 1);

        if (!added)
        {
            Debug.LogWarning("[RewardSceneEntryPoint] 카드 추가에 실패했습니다.");
            return;
        }

        bool shouldOpenHub = cachedReward.shouldOpenHubAfterResolve;

        RunStateService.Instance.ClearPendingReward();

        if (shouldOpenHub)
        {
            RunFlowController.Instance.GoToDeckbuilding(RunSceneEnterReason.RewardResolved);
        }
        else
        {
            RunFlowController.Instance.GoToAdventure(RunSceneEnterReason.ContinueRun);
        }
    }

    public void SkipRewardAndReturn()
    {
        if (RunStateService.Instance == null || RunFlowController.Instance == null)
        {
            return;
        }

        RunStateService.Instance.ClearPendingReward();
        RunFlowController.Instance.GoToAdventure(RunSceneEnterReason.ContinueRun);
    }

    public string BuildRewardDebugText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[RewardSceneEntryPoint] PendingReward 확인");

        if (cachedReward == null)
        {
            sb.AppendLine("- reward = null");
            return sb.ToString();
        }

        sb.AppendLine($"- sourceType = {cachedReward.sourceType}");
        sb.AppendLine($"- sourceRoomId = {cachedReward.sourceRoomId}");
        sb.AppendLine($"- autoHealAmount = {cachedReward.autoHealAmount}");
        sb.AppendLine($"- canRemoveCard = {cachedReward.canRemoveCard}");
        sb.AppendLine($"- canUpgradeCard = {cachedReward.canUpgradeCard}");
        sb.AppendLine($"- shouldOpenHubAfterResolve = {cachedReward.shouldOpenHubAfterResolve}");

        if (cachedReward.candidateCardIds != null)
        {
            for (int i = 0; i < cachedReward.candidateCardIds.Count; i++)
            {
                sb.AppendLine($"- candidate[{i}] = {cachedReward.candidateCardIds[i]}");
            }
        }

        return sb.ToString();
    }
}