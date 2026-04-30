using UnityEngine;

public class BootSceneEntryPoint : BaseSceneEntryPoint
{
    [Header("Global Manager Prefabs (Optional)")]
    [SerializeField] private GameObject gameSceneManagerPrefab;
    [SerializeField] private GameObject runFlowControllerPrefab;
    [SerializeField] private GameObject runStateServicePrefab;
    [SerializeField] private GameObject soundManagerPrefab;
    [SerializeField] private GameObject saveManagerPrefab;

    [Header("Boot Options")]
    [SerializeField] private bool autoMoveToTitleOnStart = true;

    protected override void OnInitializeScene()
    {
        EnsureGlobalObject<GameSceneManager>(gameSceneManagerPrefab);
        EnsureGlobalObject<RunStateService>(runStateServicePrefab);
        EnsureGlobalObject<RunFlowController>(runFlowControllerPrefab);

        EnsureOptionalGlobalPrefab(soundManagerPrefab, "SoundManager");
        EnsureOptionalGlobalPrefab(saveManagerPrefab, "SaveManager");

        if (RunStateService.Instance != null)
        {
            RunStateService.Instance.SetCurrentGameState(RunStateType.Boot);
            RunStateService.Instance.ClearNextSceneTransition();
        }

        if (autoMoveToTitleOnStart && RunFlowController.Instance != null)
            RunFlowController.Instance.GoToTitle(RunSceneEnterReason.Bootstrap);
    }

    private void EnsureGlobalObject<T>(GameObject prefab) where T : Component
    {
        T existing = FindFirstObjectByType<T>();
        if (existing != null)
            return;

        if (prefab != null)
            Instantiate(prefab);
    }

    private void EnsureOptionalGlobalPrefab(GameObject prefab, string objectName)
    {
        if (prefab == null)
            return;

        if (GameObject.Find(objectName) != null)
            return;

        Instantiate(prefab);
    }
}
