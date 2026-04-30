using System.Text;
using UnityEngine;

public class DeckbuildingHubSceneEntryPoint : BaseSceneEntryPoint
{
    [Header("Debug")]
    [SerializeField] private bool logDeckOnEnter = true;

    protected override void OnInitializeScene()
    {
        if (RunStateService.Instance != null)
        {
            RunStateService.Instance.SetCurrentGameState(RunStateType.DeckbuildingHub);
        }

        if (logDeckOnEnter)
        {
            Debug.Log(BuildDeckDebugText());
        }
    }

    public void RemoveCardById(string cardId)
    {
        if (RunStateService.Instance == null)
        {
            return;
        }

        bool removed = RunStateService.Instance.TryRemoveCardFromDeck(cardId, 1);

        Debug.Log(removed
            ? $"[DeckbuildingHubSceneEntryPoint] 카드 제거 성공: {cardId}"
            : $"[DeckbuildingHubSceneEntryPoint] 카드 제거 실패: {cardId}");
    }

    public void CloseHubAndReturnToAdventure()
    {
        if (RunFlowController.Instance == null)
        {
            return;
        }

        RunFlowController.Instance.GoToAdventure(RunSceneEnterReason.HubClosed);
    }

    public string BuildDeckDebugText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[DeckbuildingHubSceneEntryPoint] 현재 덱 확인");

        if (RunStateService.Instance == null || RunStateService.Instance.CurrentRun == null)
        {
            sb.AppendLine("- run data = null");
            return sb.ToString();
        }

        if (RunStateService.Instance.CurrentRun.currentDeck == null ||
            RunStateService.Instance.CurrentRun.currentDeck.Count == 0)
        {
            sb.AppendLine("- 현재 덱이 비어 있습니다.");
            return sb.ToString();
        }

        for (int i = 0; i < RunStateService.Instance.CurrentRun.currentDeck.Count; i++)
        {
            var entry = RunStateService.Instance.CurrentRun.currentDeck[i];
            sb.AppendLine($"- {entry.cardId} x {entry.count}");
        }

        return sb.ToString();
    }
}