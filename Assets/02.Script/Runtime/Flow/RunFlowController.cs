using UnityEngine;

[DisallowMultipleComponent]
public class RunFlowController : MonoBehaviour
{
    public static RunFlowController Instance { get; private set; }

    [Header("Scene Flow")]
    [SerializeField] private string bootSceneName = "Boot";
    [SerializeField] private string titleSceneName = "Title";
    [SerializeField] private string adventureSceneName = "AdventureMap_2D";
    [SerializeField] private string battleSceneName = "Battle_Encounter";
    [SerializeField] private string rewardSceneName = "Reward_Choice";
    [SerializeField] private string deckbuildingSceneName = "Deckbuilding_Hub";
    [SerializeField] private string resultSceneName = "Result";

    [Header("Debug")]
    [SerializeField] private bool enableDebugInput = false;
    [SerializeField] private KeyCode debugTitleSceneKey = KeyCode.F1;
    [SerializeField] private KeyCode debugAdventureSceneKey = KeyCode.F2;
    [SerializeField] private KeyCode debugBattleSceneKey = KeyCode.F3;
    [SerializeField] private KeyCode debugDeckbuildingSceneKey = KeyCode.F4;

    public string BootSceneName => bootSceneName;
    public string TitleSceneName => titleSceneName;
    public string AdventureSceneName => adventureSceneName;
    public string BattleSceneName => battleSceneName;
    public string RewardSceneName => rewardSceneName;
    public string DeckbuildingSceneName => deckbuildingSceneName;
    public string ResultSceneName => resultSceneName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        DontDestroyOnLoad(gameObject);
        EnsureRuntimeDependencies();
    }

    private void Update()
    {
        if (!Application.isPlaying || !enableDebugInput)
        {
            return;
        }

        if (Input.GetKeyDown(debugTitleSceneKey))
        {
            GoToTitle();
        }
        else if (Input.GetKeyDown(debugAdventureSceneKey))
        {
            GoToAdventure();
        }
        else if (Input.GetKeyDown(debugBattleSceneKey))
        {
            GoToBattle();
        }
        else if (Input.GetKeyDown(debugDeckbuildingSceneKey))
        {
            GoToDeckbuilding();
        }
    }

    public bool GoToBoot() => GoToBoot(RunSceneEnterReason.Bootstrap);
    public bool GoToBoot(RunSceneEnterReason reason) => LoadSceneByName(bootSceneName, reason);

    public bool GoToTitle() => GoToTitle(RunSceneEnterReason.ReturnToTitle);
    public bool GoToTitle(RunSceneEnterReason reason) => LoadSceneByName(titleSceneName, reason);

    public bool GoToAdventure() => GoToAdventure(RunSceneEnterReason.ContinueRun);
    public bool GoToAdventure(RunSceneEnterReason reason) => LoadSceneByName(adventureSceneName, reason);

    public bool GoToBattle() => GoToBattle(RunSceneEnterReason.NodeCombatSelected);
    public bool GoToBattle(RunSceneEnterReason reason) => LoadSceneByName(battleSceneName, reason);

    public bool GoToReward() => GoToReward(RunSceneEnterReason.BattleWon);
    public bool GoToReward(RunSceneEnterReason reason) => LoadSceneByName(rewardSceneName, reason);

    public bool GoToDeckbuilding() => GoToDeckbuilding(RunSceneEnterReason.RewardResolved);
    public bool GoToDeckbuilding(RunSceneEnterReason reason) => LoadSceneByName(deckbuildingSceneName, reason);

    public bool GoToResult() => GoToResult(RunSceneEnterReason.BattleLost);
    public bool GoToResult(RunSceneEnterReason reason) => LoadSceneByName(resultSceneName, reason);

    public bool LoadSceneByName(string sceneName) => LoadSceneByName(sceneName, RunSceneEnterReason.Unknown);

    public bool LoadSceneByName(string sceneName, RunSceneEnterReason reason)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[RunFlowController] sceneName이 비어 있어 씬 전환을 중단합니다.");
            return false;
        }

        if (GameSceneManager.Instance == null)
        {
            Debug.LogWarning("[RunFlowController] GameSceneManager 인스턴스가 없어 씬 전환을 중단합니다.");
            return false;
        }

        RunStateService runStateService = RunStateService.Instance;
        if (runStateService != null)
        {
            RunStateType targetGameState = ResolveTargetGameState(sceneName);
            if (targetGameState != RunStateType.None)
            {
                runStateService.SetGameState(targetGameState);
            }

            runStateService.SetNextSceneTransition(sceneName, reason);
        }

        GameSceneManager.Instance.LoadSceneByName(sceneName);
        return true;
    }

    private void EnsureRuntimeDependencies()
    {
        if (GameSceneManager.Instance == null)
        {
            GameObject managerObject = new GameObject("GameSceneManager");
            managerObject.AddComponent<GameSceneManager>();
        }

        if (RunStateService.Instance == null)
        {
            GameObject runStateObject = new GameObject("RunStateService");
            runStateObject.AddComponent<RunStateService>();
        }
    }

    private RunStateType ResolveTargetGameState(string sceneName)
    {
        if (string.Equals(sceneName, bootSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.Boot;
        }

        if (string.Equals(sceneName, titleSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.Title;
        }

        if (string.Equals(sceneName, adventureSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.AdventureMap;
        }

        if (string.Equals(sceneName, battleSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.Battle;
        }

        if (string.Equals(sceneName, rewardSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.Reward;
        }

        if (string.Equals(sceneName, deckbuildingSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.DeckbuildingHub;
        }

        if (string.Equals(sceneName, resultSceneName, System.StringComparison.Ordinal))
        {
            return RunStateType.Result;
        }

        return RunStateType.None;
    }
}
