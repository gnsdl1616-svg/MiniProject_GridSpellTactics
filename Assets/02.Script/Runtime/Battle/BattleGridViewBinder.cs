using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Grid 셀의 UI 좌표와 Battle Runtime GridPosition을 연결합니다.
/// PlayerObject는 Grid 위를 이동하고, Enemy는 Grid 밖 Enemy Zone에 고정하는 구조를 전제로 합니다.
/// </summary>
public class BattleGridViewBinder : MonoBehaviour
{
    [SerializeField] private BattleGridAutoBuilder battleGridAutoBuilder;
    [SerializeField] private RectTransform playerObjectLayer;
    [SerializeField] private int gridWidth = 6;
    [SerializeField] private int gridHeight = 4;

    private List<Button> cachedButtons = new List<Button>();

    private void Awake()
    {
        ResolveReferences();
        RefreshGridCache();
    }

    public void RefreshGridCache()
    {
        ResolveReferences();

        if (battleGridAutoBuilder == null)
        {
            return;
        }

        gridWidth = battleGridAutoBuilder.GridWidth;
        gridHeight = battleGridAutoBuilder.GridHeight;
        cachedButtons = battleGridAutoBuilder.GetGeneratedButtonsCopy();
    }

    public bool TryGetAnchoredPosition(GridPosition gridPosition, out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        if (!IsWithinBounds(gridPosition))
        {
            return false;
        }

        if (cachedButtons == null || cachedButtons.Count <= 0)
        {
            RefreshGridCache();
        }

        int index = GridPositionToIndex(gridPosition);
        if (index < 0 || index >= cachedButtons.Count)
        {
            return false;
        }

        Button button = cachedButtons[index];
        if (button == null || playerObjectLayer == null)
        {
            return false;
        }

        RectTransform cellRect = button.GetComponent<RectTransform>();
        if (cellRect == null)
        {
            return false;
        }

        Vector3 worldCenter = cellRect.TransformPoint(cellRect.rect.center);
        Vector3 localPoint = playerObjectLayer.InverseTransformPoint(worldCenter);
        anchoredPosition = new Vector2(localPoint.x, localPoint.y);
        return true;
    }

    public void SnapPlayerToGrid(BattleUnitView playerView, GridPosition gridPosition)
    {
        if (playerView == null)
        {
            return;
        }

        if (TryGetAnchoredPosition(gridPosition, out Vector2 pos))
        {
            playerView.SnapTo(pos);
        }
    }

    public void MovePlayerToGrid(BattleUnitView playerView, GridPosition gridPosition)
    {
        if (playerView == null)
        {
            return;
        }

        if (TryGetAnchoredPosition(gridPosition, out Vector2 pos))
        {
            playerView.MoveTo(pos);
        }
    }

    private bool IsWithinBounds(GridPosition pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }

    private int GridPositionToIndex(GridPosition pos)
    {
        int topToBottomRow = (gridHeight - 1) - pos.y;
        return topToBottomRow * gridWidth + pos.x;
    }

    private void ResolveReferences()
    {
        if (battleGridAutoBuilder == null)
        {
            battleGridAutoBuilder = FindFirstObjectByType<BattleGridAutoBuilder>();
        }
    }
}
