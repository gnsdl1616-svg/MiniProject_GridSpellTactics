using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투에서 사용하는 카드 정의를 관리하는 라이브러리입니다.
/// 현재 단계에서는 ScriptableObject 없이 코드/Inspector 기반으로만 관리합니다.
/// </summary>
public class BattleCardLibrary : MonoBehaviour
{
    [SerializeField] private List<BattleCardDefinition> cardDefinitions = new List<BattleCardDefinition>();

    private readonly Dictionary<string, BattleCardDefinition> cardLookup = new Dictionary<string, BattleCardDefinition>();
    private bool isLookupDirty = true;

    private void Awake()
    {
        EnsureDefaultCardsIfEmpty();
        RebuildLookup();
    }

    public void EnsureDefaultCardsIfEmpty()
    {
        if (cardDefinitions != null && cardDefinitions.Count > 0)
        {
            return;
        }

        cardDefinitions = new List<BattleCardDefinition>
        {
            new BattleCardDefinition
            {
                cardId = "Move_1",
                displayName = "Move 1",
                actionType = BattleCardActionType.Move,
                targetType = BattleTargetType.TileSelect,
                energyCost = 1,
                moveDistance = 1,
                damageValue = 0,
                blockValue = 0,
                goesToFieldPile = false
            },
            new BattleCardDefinition
            {
                cardId = "Attack_3",
                displayName = "Attack 3",
                actionType = BattleCardActionType.Attack,
                targetType = BattleTargetType.EnemySelect,
                energyCost = 1,
                moveDistance = 0,
                damageValue = 3,
                blockValue = 0,
                goesToFieldPile = false
            },
            new BattleCardDefinition
            {
                cardId = "Guard_3",
                displayName = "Guard 3",
                actionType = BattleCardActionType.Defense,
                targetType = BattleTargetType.Self,
                energyCost = 1,
                moveDistance = 0,
                damageValue = 0,
                blockValue = 3,
                goesToFieldPile = false
            },
            new BattleCardDefinition
            {
                cardId = "Dash_2",
                displayName = "Dash 2",
                actionType = BattleCardActionType.Move,
                targetType = BattleTargetType.TileSelect,
                energyCost = 1,
                moveDistance = 2,
                damageValue = 0,
                blockValue = 0,
                goesToFieldPile = false
            }
        };

        isLookupDirty = true;
    }

    public List<BattleCardDefinition> GetAllDefinitions()
    {
        EnsureLookup();
        return cardDefinitions;
    }

    public bool TryGetCardDefinition(string cardId, out BattleCardDefinition definition)
    {
        EnsureLookup();
        return cardLookup.TryGetValue(cardId, out definition);
    }

    public BattleCardDefinition GetCardDefinition(string cardId)
    {
        EnsureLookup();
        cardLookup.TryGetValue(cardId, out BattleCardDefinition definition);
        return definition;
    }

    private void EnsureLookup()
    {
        if (!isLookupDirty)
        {
            return;
        }

        RebuildLookup();
    }

    private void RebuildLookup()
    {
        cardLookup.Clear();

        if (cardDefinitions == null)
        {
            cardDefinitions = new List<BattleCardDefinition>();
        }

        for (int i = 0; i < cardDefinitions.Count; i++)
        {
            BattleCardDefinition definition = cardDefinitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.cardId))
            {
                continue;
            }

            cardLookup[definition.cardId] = definition;
        }

        isLookupDirty = false;
    }
}
