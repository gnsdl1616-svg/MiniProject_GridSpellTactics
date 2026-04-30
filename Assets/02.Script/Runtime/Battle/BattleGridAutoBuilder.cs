using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Battle UI용 Grid 셀을 자동 생성하는 보조 스크립트입니다.
/// 
/// 목적:
/// - 6x4(또는 지정한 크기) Grid를 씬 진입 시 자동 생성
/// - BattleUIView가 수동 연결 없이 gridCells 리스트를 받을 수 있게 준비
/// - 랜덤 일반 타일 이미지 1~2종 섞기 지원
/// </summary>
public class BattleGridAutoBuilder : MonoBehaviour
{
    [System.Serializable]
    public class GeneratedCell
    {
        public Button button;
        public Image background;
        public TMP_Text coordinateText;
    }

    [Header("Grid Root")]
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private Button cellTemplateButton;
    [SerializeField] private int gridWidth = 6;
    [SerializeField] private int gridHeight = 4;

    [Header("Tile Visuals")]
    [SerializeField] private List<Sprite> groundTileSprites = new List<Sprite>();
    [SerializeField] private bool randomizeTilesOnBuild = true;

    [Header("Generated Cells")]
    [SerializeField] private List<Button> generatedButtons = new List<Button>();

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public IReadOnlyList<Button> GeneratedButtons => generatedButtons;

    private void Awake()
    {
        BuildGrid();
    }

    [ContextMenu("Build Grid")]
    public void BuildGrid()
    {
        if (gridRoot == null || cellTemplateButton == null)
        {
            Debug.LogWarning("[BattleGridAutoBuilder] gridRoot 또는 cellTemplateButton이 비어 있습니다.", this);
            return;
        }

        ClearGrid();
        cellTemplateButton.gameObject.SetActive(false);

        int total = Mathf.Max(1, gridWidth * gridHeight);
        generatedButtons = new List<Button>(total);

        for (int i = 0; i < total; i++)
        {
            Button clone = Instantiate(cellTemplateButton, gridRoot);
            clone.gameObject.name = $"GridCell_{i:00}";
            clone.gameObject.SetActive(true);

            Image bg = clone.GetComponent<Image>();
            if (bg != null && groundTileSprites != null && groundTileSprites.Count > 0 && randomizeTilesOnBuild)
            {
                int randomIndex = Random.Range(0, groundTileSprites.Count);
                bg.sprite = groundTileSprites[randomIndex];
                bg.type = Image.Type.Sliced;
            }

            generatedButtons.Add(clone);
        }
    }

    public List<Button> GetGeneratedButtonsCopy()
    {
        return new List<Button>(generatedButtons);
    }

    private void ClearGrid()
    {
        generatedButtons.Clear();

        List<Transform> toDelete = new List<Transform>();
        for (int i = 0; i < gridRoot.childCount; i++)
        {
            Transform child = gridRoot.GetChild(i);
            if (cellTemplateButton != null && child == cellTemplateButton.transform)
            {
                continue;
            }
            toDelete.Add(child);
        }

        for (int i = 0; i < toDelete.Count; i++)
        {
            DestroyImmediate(toDelete[i].gameObject);
        }
    }
}
