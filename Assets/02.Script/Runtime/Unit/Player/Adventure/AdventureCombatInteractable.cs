using UnityEngine;

/// <summary>
/// AdventureMap에서 몬스터 오브젝트에 붙이는 전투 상호작용 스크립트입니다.
/// 
/// 현재 목적:
/// - CombatRoom_01 안의 Monster_Test_01 한 개와 상호작용
/// - PendingBattleRequest 생성
/// - Battle 씬으로 이동
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AdventureCombatInteractable : MonoBehaviour, IAdventureInteractable
{
    [Header("Battle Request Data")]
    [SerializeField] private string roomId = "CombatRoom_01";
    [SerializeField] private string encounterId = "Encounter_Test_01";
    [SerializeField] private string primaryEnemyPresetId = "Enemy_Test";
    [SerializeField] private string interactionObjectId = "Monster_Test_01";
    [SerializeField] private bool isBossBattle = false;

    [Header("Optional Reference")]
    [SerializeField] private AdventureMapSceneEntryPoint adventureMapSceneEntryPoint;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    public void Interact(AdventurePlayerInteractionController interactor)
    {
        if (adventureMapSceneEntryPoint == null)
        {
            adventureMapSceneEntryPoint = FindFirstObjectByType<AdventureMapSceneEntryPoint>();
        }

        if (adventureMapSceneEntryPoint == null)
        {
            Debug.LogWarning("[AdventureCombatInteractable] AdventureMapSceneEntryPoint를 찾지 못했습니다.");
            return;
        }

        adventureMapSceneEntryPoint.RequestBattleFromInteraction(
            roomId,
            encounterId,
            primaryEnemyPresetId,
            transform.position,
            interactionObjectId,
            isBossBattle);
    }
}
