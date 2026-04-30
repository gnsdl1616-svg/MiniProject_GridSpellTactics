using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// AdventureMap에서 상호작용을 담당하는 플레이어 입력 스크립트.
/// 
/// 특징:
/// - New Input System의 InputActionAsset을 읽을 수 있습니다.
/// - 아직 Interact 액션을 만들지 않았다면 fallbackKey로도 테스트할 수 있습니다.
/// 
/// 권장 액션 설정:
/// Action Map = Player
/// Action Name = Interact
/// Action Type = Button
/// Binding = Keyboard / E
/// </summary>
public class AdventurePlayerInteractionController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string interactionActionName = "Interact";

    [Header("Fallback")]
    [SerializeField] private KeyCode fallbackKey = KeyCode.E;
    [SerializeField] private bool allowFallbackKey = true;

    private InputAction interactAction;
    private IAdventureInteractable currentInteractable;
    private Collider2D currentInteractableCollider;

    private void Awake()
    {
        CacheInteractAction();
    }

    private void OnEnable()
    {
        CacheInteractAction();

        if (interactAction != null)
        {
            interactAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.Disable();
        }
    }

    private void Update()
    {
        if (currentInteractable == null)
        {
            return;
        }

        bool interacted = false;

        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            interacted = true;
        }
        else if (allowFallbackKey && Input.GetKeyDown(fallbackKey))
        {
            interacted = true;
        }

        if (interacted)
        {
            currentInteractable.Interact(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IAdventureInteractable interactable = other.GetComponent<IAdventureInteractable>();
        if (interactable == null)
        {
            return;
        }

        currentInteractable = interactable;
        currentInteractableCollider = other;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != currentInteractableCollider)
        {
            return;
        }

        currentInteractable = null;
        currentInteractableCollider = null;
    }

    private void CacheInteractAction()
    {
        interactAction = null;

        if (inputActions == null)
        {
            return;
        }

        InputActionMap actionMap = inputActions.FindActionMap(actionMapName, false);
        if (actionMap == null)
        {
            Debug.LogWarning($"[AdventurePlayerInteractionController] ActionMap을 찾지 못했습니다: {actionMapName}", this);
            return;
        }

        interactAction = actionMap.FindAction(interactionActionName, false);
        if (interactAction == null)
        {
            Debug.LogWarning($"[AdventurePlayerInteractionController] Interact Action을 찾지 못했습니다: {interactionActionName}", this);
        }
    }
}