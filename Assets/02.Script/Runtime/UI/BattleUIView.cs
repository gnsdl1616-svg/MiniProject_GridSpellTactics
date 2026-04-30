using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIView : MonoBehaviour
{
    [System.Serializable]
    public class CardSlotUI
    {
        public Button button;
        public TMP_Text nameText;
        public TMP_Text costText;
        public TMP_Text detailText;
        public Image disabledOverlay;
    }

    [System.Serializable]
    public class GridCellUI
    {
        public Button button;
        public Image background;
        public TMP_Text coordinateText;
    }

    [Header("References")]
    [SerializeField] private BattleFlowController battleFlowController;
    [SerializeField] private BattleCardLibrary battleCardLibrary;
    [SerializeField] private BattleExecutionResolver battleExecutionResolver;
    [SerializeField] private BattleGridAutoBuilder battleGridAutoBuilder;
    [SerializeField] private BattleSceneEntryPoint battleSceneEntryPoint;

    [Header("Top Bar")]
    [SerializeField] private TMP_Text playerHpText;
    [SerializeField] private Slider playerHpSlider;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private Slider energySlider;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private Slider enemyHpSlider;
    [SerializeField] private TMP_Text enemyNameText;
    [SerializeField] private TMP_Text playerBlockText;
    [SerializeField] private TMP_Text enemyBlockText;

    [Header("Reservation Panels")]
    [SerializeField] private TMP_Text playerReservationText;
    [SerializeField] private TMP_Text enemyReservationText;
    [SerializeField] private TMP_Text enemyIntentText;

    [Header("Bottom Area")]
    [SerializeField] private TMP_Text drawPileCountText;
    [SerializeField] private TMP_Text discardPileCountText;
    [SerializeField] private TMP_Text battleLogText;
    [SerializeField] private TMP_Text waitButtonText;
    [SerializeField] private TMP_Text executeButtonText;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text gridModeText;

    [Header("Hand UI")]
    [SerializeField] private List<CardSlotUI> handSlots = new List<CardSlotUI>();

    [Header("Grid UI")]
    [SerializeField] private List<GridCellUI> gridCells = new List<GridCellUI>();
    [SerializeField] private int gridWidth = 6;
    [SerializeField] private int gridHeight = 4;
    [SerializeField] private Color normalCellColor = Color.white;
    [SerializeField] private Color selectableCellColor = new Color(0.75f, 0.95f, 0.75f, 1f);
    [SerializeField] private Color selectedCellColor = new Color(0.55f, 0.85f, 1f, 1f);
    [SerializeField] private Color playerCellColor = new Color(0.75f, 0.9f, 1f, 1f);
    [SerializeField] private Color enemyPreviewCellColor = new Color(1f, 0.93f, 0.6f, 1f);

    [Header("Buttons")]
    [SerializeField] private Button waitButton;
    [SerializeField] private Button buildEnemyPlanButton;
    [SerializeField] private Button executeTurnButton;
    [SerializeField] private Button refreshButton;

    private string pendingMoveCardId;
    private BattleCardDefinition pendingMoveDefinition;
    private readonly HashSet<int> selectableGridCellIndices = new HashSet<int>();

    private void Awake()
    {
        ResolveReferences();
        TryAutoBindGeneratedGrid();
        BindStaticButtons();
        BindHandButtons();
        BindGridButtons();
    }

    private void OnEnable()
    {
        RefreshView();
    }

    private void Update()
    {
        RefreshView();
    }

    private void ResolveReferences()
    {
        if (battleFlowController == null)
        {
            battleFlowController = FindFirstObjectByType<BattleFlowController>();
        }

        if (battleCardLibrary == null)
        {
            battleCardLibrary = FindFirstObjectByType<BattleCardLibrary>();
        }

        if (battleExecutionResolver == null)
        {
            battleExecutionResolver = FindFirstObjectByType<BattleExecutionResolver>();
        }

        if (battleGridAutoBuilder == null)
        {
            battleGridAutoBuilder = FindFirstObjectByType<BattleGridAutoBuilder>();
        }

        if (battleSceneEntryPoint == null)
        {
            battleSceneEntryPoint = FindFirstObjectByType<BattleSceneEntryPoint>();
        }
    }

    private void TryAutoBindGeneratedGrid()
    {
        if (battleGridAutoBuilder == null)
        {
            return;
        }

        gridWidth = battleGridAutoBuilder.GridWidth;
        gridHeight = battleGridAutoBuilder.GridHeight;

        List<Button> buttons = battleGridAutoBuilder.GetGeneratedButtonsCopy();
        if (buttons == null || buttons.Count == 0)
        {
            return;
        }

        gridCells = new List<GridCellUI>();
        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];
            GridCellUI ui = new GridCellUI
            {
                button = button,
                background = button != null ? button.GetComponent<Image>() : null,
                coordinateText = button != null ? button.GetComponentInChildren<TMP_Text>() : null
            };
            gridCells.Add(ui);
        }
    }

    private void BindStaticButtons()
    {
        if (waitButton != null)
        {
            waitButton.onClick.RemoveAllListeners();
            waitButton.onClick.AddListener(OnClickWait);
        }

        if (buildEnemyPlanButton != null)
        {
            buildEnemyPlanButton.onClick.RemoveAllListeners();
            buildEnemyPlanButton.onClick.AddListener(OnClickBuildEnemyPlan);
        }

        if (executeTurnButton != null)
        {
            executeTurnButton.onClick.RemoveAllListeners();
            executeTurnButton.onClick.AddListener(OnClickExecuteTurn);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(RefreshView);
        }
    }

    private void BindHandButtons()
    {
        for (int i = 0; i < handSlots.Count; i++)
        {
            int capturedIndex = i;
            if (handSlots[i] != null && handSlots[i].button != null)
            {
                handSlots[i].button.onClick.RemoveAllListeners();
                handSlots[i].button.onClick.AddListener(() => OnClickHandCard(capturedIndex));
            }
        }
    }

    private void BindGridButtons()
    {
        for (int i = 0; i < gridCells.Count; i++)
        {
            int capturedIndex = i;
            if (gridCells[i] != null && gridCells[i].button != null)
            {
                gridCells[i].button.onClick.RemoveAllListeners();
                gridCells[i].button.onClick.AddListener(() => OnClickGridCell(capturedIndex));
            }
        }
    }

    public void RefreshView()
    {
        if (battleFlowController == null || battleFlowController.RuntimeState == null)
        {
            return;
        }

        BattleRuntimeState state = battleFlowController.RuntimeState;

        SetHp(playerHpText, playerHpSlider, "HP", state.playerActor.currentHp, state.playerActor.maxHp);
        SetEnergy(energyText, energySlider, state.currentEnergy, state.maxEnergy);
        SetText(turnText, $"Turn {state.currentTurnIndex}");
        SetHp(enemyHpText, enemyHpSlider, "HP", state.enemyActor.currentHp, state.enemyActor.maxHp);
        SetText(enemyNameText, string.IsNullOrWhiteSpace(state.enemyActor.actorId) ? "Enemy" : state.enemyActor.actorId);
        SetText(playerBlockText, $"Guard {state.playerActor.currentBlock}");
        SetText(enemyBlockText, $"Guard {state.enemyActor.currentBlock}");

        SetText(playerReservationText, BuildReservationText("Player Reservation", state.playerReservedActions));
        SetText(enemyReservationText, BuildReservationText("Enemy Reservation", state.enemyReservedActions));
        SetText(enemyIntentText, BuildEnemyIntentText(state));

        SetText(drawPileCountText, $"Draw\nPile\n({GetCount(state.deckRuntime != null ? state.deckRuntime.drawPileCardIds : null)})");
        SetText(discardPileCountText, $"Discard\nPile\n({GetCount(state.deckRuntime != null ? state.deckRuntime.discardPileCardIds : null)})");
        SetText(waitButtonText, $"Wait\n({battleFlowController.GetCurrentWaitReservationCount()}/{battleFlowController.MaxWaitReservationsPerTurn})");
        SetText(executeButtonText, "Execute\nTurn");
        SetText(summaryText, BuildSummaryText(state));
        SetText(gridModeText, BuildGridModeText(state));
        SetText(battleLogText, battleExecutionResolver != null ? battleExecutionResolver.GetRecentLogsText() : "BattleLog\n-");

        RefreshHandSlots(state);
        RefreshButtons(state);
        RefreshGrid(state);
    }

    private void RefreshHandSlots(BattleRuntimeState state)
    {
        List<string> hand = state.deckRuntime != null ? state.deckRuntime.handCardIds : null;

        for (int i = 0; i < handSlots.Count; i++)
        {
            CardSlotUI slot = handSlots[i];
            if (slot == null)
            {
                continue;
            }

            bool hasCard = hand != null && i < hand.Count;
            bool canUseCard = hasCard && state.currentPhase == BattleFlowPhase.Planning && state.outcome == BattleOutcome.None;

            if (slot.button != null)
            {
                slot.button.interactable = canUseCard;
            }

            if (slot.disabledOverlay != null)
            {
                slot.disabledOverlay.gameObject.SetActive(!canUseCard && hasCard);
            }

            if (!hasCard)
            {
                SetText(slot.nameText, string.Empty);
                SetText(slot.costText, string.Empty);
                SetText(slot.detailText, string.Empty);
                continue;
            }

            string cardId = hand[i];
            BattleCardDefinition definition = battleCardLibrary != null ? battleCardLibrary.GetCardDefinition(cardId) : null;
            if (definition == null)
            {
                SetText(slot.nameText, cardId);
                SetText(slot.costText, string.Empty);
                SetText(slot.detailText, string.Empty);
                continue;
            }

            SetText(slot.nameText, definition.displayName);
            SetText(slot.costText, $"Cost {definition.energyCost}");
            SetText(slot.detailText, BuildCardDetailText(definition));

            bool canPay = state.currentEnergy >= definition.energyCost;
            if (slot.button != null)
            {
                slot.button.interactable = canUseCard && canPay;
            }

            if (slot.disabledOverlay != null)
            {
                slot.disabledOverlay.gameObject.SetActive(hasCard && (!canUseCard || !canPay));
            }
        }
    }

    private void RefreshButtons(BattleRuntimeState state)
    {
        bool canPlan = state.currentPhase == BattleFlowPhase.Planning && state.outcome == BattleOutcome.None;
        bool canExecute = (state.playerReservedActions != null && state.playerReservedActions.Count > 0) && state.outcome == BattleOutcome.None;

        if (waitButton != null)
        {
            waitButton.interactable = canPlan && battleFlowController.GetCurrentWaitReservationCount() < battleFlowController.MaxWaitReservationsPerTurn;
        }

        if (buildEnemyPlanButton != null)
        {
            buildEnemyPlanButton.interactable = canPlan;
        }

        if (executeTurnButton != null)
        {
            executeTurnButton.interactable = canExecute;
        }
    }

    private void RefreshGrid(BattleRuntimeState state)
    {
        EnsureSelectableGridIndices(state);

        for (int i = 0; i < gridCells.Count; i++)
        {
            GridCellUI cell = gridCells[i];
            if (cell == null)
            {
                continue;
            }

            GridPosition gridPos = GridIndexToPosition(i);
            bool isPlayerCell = SameGrid(gridPos, state.playerActor.currentGridPosition);
            bool isSelectable = selectableGridCellIndices.Contains(i);
            bool isSelectedTarget = pendingMoveDefinition != null && isSelectable;
            bool isEnemyPreviewTarget = IsEnemyPreviewCell(state, gridPos);

            if (cell.coordinateText != null)
            {
                cell.coordinateText.text = string.Empty;
            }

            if (cell.background != null)
            {
                Color color = normalCellColor;
                if (isSelectable)
                {
                    color = selectableCellColor;
                }
                if (isSelectedTarget)
                {
                    color = selectableCellColor;
                }
                if (isEnemyPreviewTarget)
                {
                    color = enemyPreviewCellColor;
                }
                if (isPlayerCell)
                {
                    color = playerCellColor;
                }

                cell.background.color = color;
            }

            if (cell.button != null)
            {
                cell.button.interactable = isSelectable;
            }
        }
    }

    public void OnClickHandCard(int handIndex)
    {
        if (battleFlowController == null || battleFlowController.RuntimeState == null)
        {
            return;
        }

        BattleRuntimeState state = battleFlowController.RuntimeState;
        if (state.deckRuntime == null || state.deckRuntime.handCardIds == null || handIndex < 0 || handIndex >= state.deckRuntime.handCardIds.Count)
        {
            return;
        }

        string cardId = state.deckRuntime.handCardIds[handIndex];
        if (battleCardLibrary == null || !battleCardLibrary.TryGetCardDefinition(cardId, out BattleCardDefinition definition))
        {
            return;
        }

        if (definition.actionType == BattleCardActionType.Move)
        {
            pendingMoveCardId = cardId;
            pendingMoveDefinition = definition;
            RefreshView();
            return;
        }

        GridPosition targetGrid = battleFlowController.GetPlannedPlayerPositionBeforeNewReservation();
        string targetActorId = string.Empty;

        switch (definition.actionType)
        {
            case BattleCardActionType.Attack:
                targetGrid = state.enemyActor.currentGridPosition;
                targetActorId = state.enemyActor.actorId;
                break;
            case BattleCardActionType.Defense:
                targetGrid = battleFlowController.GetPlannedPlayerPositionBeforeNewReservation();
                break;
        }

        battleFlowController.TryReservePlayerCard(cardId, targetGrid, targetActorId);
        ClearPendingMoveSelection();
        RefreshView();
    }

    public void OnClickGridCell(int cellIndex)
    {
        if (battleFlowController == null || battleFlowController.RuntimeState == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(pendingMoveCardId) || pendingMoveDefinition == null)
        {
            return;
        }

        if (!selectableGridCellIndices.Contains(cellIndex))
        {
            return;
        }

        GridPosition targetGrid = GridIndexToPosition(cellIndex);
        bool reserved = battleFlowController.TryReservePlayerCard(pendingMoveCardId, targetGrid);

        if (reserved)
        {
            ClearPendingMoveSelection();
            RefreshView();
        }
    }

    public void OnClickWait()
    {
        if (battleFlowController == null)
        {
            return;
        }

        battleFlowController.TryReserveWaitAction();
        ClearPendingMoveSelection();
        RefreshView();
    }

    public void OnClickBuildEnemyPlan()
    {
        if (battleFlowController == null)
        {
            return;
        }

        battleFlowController.BuildDefaultEnemyPlan();
        RefreshView();
    }

    public void OnClickExecuteTurn()
    {
        ClearPendingMoveSelection();

        if (battleSceneEntryPoint != null)
        {
            battleSceneEntryPoint.DebugExecuteTurn();
        }
        else if (battleFlowController != null)
        {
            battleFlowController.ExecuteCurrentTurn();
        }

        RefreshView();
    }

    private void EnsureSelectableGridIndices(BattleRuntimeState state)
    {
        selectableGridCellIndices.Clear();

        if (string.IsNullOrWhiteSpace(pendingMoveCardId) || pendingMoveDefinition == null)
        {
            return;
        }

        GridPosition origin = battleFlowController.GetPlannedPlayerPositionBeforeNewReservation();
        int maxDistance = Mathf.Max(1, pendingMoveDefinition.moveDistance);

        for (int i = 0; i < gridCells.Count; i++)
        {
            GridPosition gridPos = GridIndexToPosition(i);

            if (!IsGridWithinBounds(gridPos))
            {
                continue;
            }

            int distance = Mathf.Abs(gridPos.x - origin.x) + Mathf.Abs(gridPos.y - origin.y);
            if (distance <= 0 || distance > maxDistance)
            {
                continue;
            }

            selectableGridCellIndices.Add(i);
        }
    }

    private bool IsGridWithinBounds(GridPosition pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }

    private GridPosition GridIndexToPosition(int index)
    {
        int x = index % gridWidth;
        int topToBottomRow = index / gridWidth;
        int y = (gridHeight - 1) - topToBottomRow;
        return new GridPosition(x, y);
    }

    private bool SameGrid(GridPosition a, GridPosition b)
    {
        return a.x == b.x && a.y == b.y;
    }

    private bool IsEnemyPreviewCell(BattleRuntimeState state, GridPosition gridPos)
    {
        if (state.enemyReservedActions == null || state.enemyReservedActions.Count <= 0)
        {
            return false;
        }

        ReservedActionData action = state.enemyReservedActions[0];
        return SameGrid(gridPos, action.targetGridPosition);
    }

    private void ClearPendingMoveSelection()
    {
        pendingMoveCardId = null;
        pendingMoveDefinition = null;
        selectableGridCellIndices.Clear();
    }

    private string BuildEnemyIntentText(BattleRuntimeState state)
    {
        if (state.enemyReservedActions == null || state.enemyReservedActions.Count <= 0)
        {
            return "Enemy Intent\nNone";
        }

        ReservedActionData action = state.enemyReservedActions[0];
        return $"Enemy Intent\n{action.actionType} / {action.damageValue}";
    }

    private string BuildSummaryText(BattleRuntimeState state)
    {
        return
            $"Player Pos : ({state.playerActor.currentGridPosition.x}, {state.playerActor.currentGridPosition.y})\n" +
            $"Enemy Zone : {state.enemyActor.actorId}\n" +
            $"Player Reserved : {(state.playerReservedActions != null ? state.playerReservedActions.Count : 0)}\n" +
            $"Enemy Reserved : {(state.enemyReservedActions != null ? state.enemyReservedActions.Count : 0)}\n" +
            $"Outcome : {state.outcome}";
    }

    private string BuildGridModeText(BattleRuntimeState state)
    {
        if (!string.IsNullOrWhiteSpace(pendingMoveCardId) && pendingMoveDefinition != null)
        {
            return $"Grid Mode : {pendingMoveDefinition.displayName} target select";
        }

        GridPosition planned = battleFlowController.GetPlannedPlayerPositionBeforeNewReservation();
        return $"Grid Mode : View\nPlanned ({planned.x}, {planned.y})";
    }

    private string BuildCardDetailText(BattleCardDefinition definition)
    {
        switch (definition.actionType)
        {
            case BattleCardActionType.Move:
                return $"Move {definition.moveDistance}";
            case BattleCardActionType.Attack:
                return $"Atk {definition.damageValue}";
            case BattleCardActionType.Defense:
                return $"Block {definition.blockValue}";
            case BattleCardActionType.Wait:
                return "Wait";
            default:
                return definition.actionType.ToString();
        }
    }

    private int GetCount(List<string> list)
    {
        return list != null ? list.Count : 0;
    }

    private void SetHp(TMP_Text textField, Slider slider, string label, int current, int max)
    {
        if (textField != null)
        {
            textField.text = $"{label} : {current}/{max}";
        }

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = Mathf.Max(1, max);
            slider.value = Mathf.Clamp(current, 0, Mathf.Max(1, max));
        }
    }

    private void SetEnergy(TMP_Text textField, Slider slider, int current, int max)
    {
        if (textField != null)
        {
            textField.text = $"ENERGY : {current}/{max}";
        }

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = Mathf.Max(1, max);
            slider.value = Mathf.Clamp(current, 0, Mathf.Max(1, max));
        }
    }

    private string BuildReservationText(string title, List<ReservedActionData> actions)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(title);

        if (actions == null || actions.Count == 0)
        {
            sb.Append("-");
            return sb.ToString();
        }

        for (int i = 0; i < actions.Count; i++)
        {
            ReservedActionData action = actions[i];
            if (action == null)
            {
                continue;
            }

            sb.Append(i + 1).Append(". ").Append(action.actionType);

            if (action.actionType == BattleCardActionType.Move)
            {
                sb.Append(" -> (").Append(action.targetGridPosition.x).Append(", ").Append(action.targetGridPosition.y).Append(")");
            }
            else if (!string.IsNullOrWhiteSpace(action.targetActorId))
            {
                sb.Append(" -> ").Append(action.targetActorId);
            }

            if (i < actions.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private void SetText(TMP_Text textField, string value)
    {
        if (textField != null)
        {
            textField.text = value;
        }
    }
}
