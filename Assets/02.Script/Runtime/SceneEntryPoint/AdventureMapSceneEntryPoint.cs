using System.Collections.Generic;
using UnityEngine;

public class AdventureMapSceneEntryPoint : BaseSceneEntryPoint
{
    [Header("Scene Defaults")]
    [SerializeField] private string defaultRoomId = "CombatRoom_01";
    [SerializeField] private string defaultRoomType = "Combat";
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool applySavedReturnPointOnEnter = true;

    protected override void OnInitializeScene()
    {
        if (RunStateService.Instance != null)
        {
            RunStateService.Instance.SetCurrentGameState(RunStateType.AdventureMap);
            RegisterCurrentRoom(defaultRoomId, defaultRoomType, null);
        }

        if (applySavedReturnPointOnEnter)
            ApplySavedReturnPointToPlayer();
    }

    public void RegisterCurrentRoom(string roomId, string roomType, List<string> connectedRoomIds = null)
    {
        if (RunStateService.Instance == null || string.IsNullOrWhiteSpace(roomId))
            return;

        RunStateService.Instance.SetCurrentRoom(roomId);
        RoomProgressData existing = RunStateService.Instance.GetRoomProgress(roomId);

        RoomProgressData roomData = new RoomProgressData
        {
            roomId = roomId,
            roomType = roomType ?? string.Empty,
            roomState = existing != null ? existing.roomState : RoomProgressState.Visited,
            connectedRoomIds = connectedRoomIds != null ? new List<string>(connectedRoomIds) : new List<string>(),
            isCleared = existing != null && existing.isCleared,
            isLocked = existing != null && existing.isLocked
        };

        RunStateService.Instance.UpsertRoomProgress(roomData);
    }

    public void ApplySavedReturnPointToPlayer()
    {
        if (RunStateService.Instance == null || playerTransform == null)
            return;

        ReturnPointData returnPoint = RunStateService.Instance.CurrentRun.returnPoint;
        if (returnPoint == null)
            return;

        playerTransform.position = returnPoint.interactionWorldPosition;
    }

    public void SaveReturnPointFromPlayer(string roomId, string interactionObjectId = "")
    {
        if (RunStateService.Instance == null || playerTransform == null)
            return;

        RunStateService.Instance.SetReturnPoint(roomId, playerTransform.position, interactionObjectId);
    }

    public void RequestBattleFromInteraction(
        string roomId,
        string encounterId,
        string primaryEnemyPresetId,
        Vector3 interactionWorldPosition,
        string interactionObjectId = "",
        bool isBossBattle = false)
    {
        if (RunStateService.Instance == null || RunFlowController.Instance == null)
            return;

        RunStateService.Instance.SetCurrentRoom(roomId);
        RunStateService.Instance.SetReturnPoint(roomId, interactionWorldPosition, interactionObjectId);
        RunStateService.Instance.SetRoomLocked(roomId, true);
        RunStateService.Instance.PrepareBattleRequest(
            roomId,
            encounterId,
            primaryEnemyPresetId,
            null,
            null,
            isBossBattle);

        RunFlowController.Instance.GoToBattle(RunSceneEnterReason.NodeCombatSelected);
    }

    public void OpenDeckbuildingHubFromMap()
    {
        if (RunFlowController.Instance == null)
            return;

        if (playerTransform != null && RunStateService.Instance != null)
        {
            string currentRoomId = RunStateService.Instance.CurrentRun != null ? RunStateService.Instance.CurrentRun.currentRoomId : string.Empty;
            RunStateService.Instance.SetReturnPoint(currentRoomId, playerTransform.position, "DeckbuildingHubOpen");
        }

        RunFlowController.Instance.GoToDeckbuilding(RunSceneEnterReason.RoomInteraction);
    }
}
