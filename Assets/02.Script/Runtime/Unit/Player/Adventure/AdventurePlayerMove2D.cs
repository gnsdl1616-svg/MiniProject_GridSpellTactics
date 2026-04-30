using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// AdventureMap 테스트용 2D 플레이어 이동 스크립트.
/// 
/// 특징:
/// - New Input System의 InputActionAsset을 직접 읽습니다.
/// - Rigidbody2D가 있으면 MovePosition으로 이동합니다.
/// - Rigidbody2D가 없으면 Transform 이동으로 동작합니다.
/// 
/// 사용 전제:
/// - Input Actions Asset에 이동용 Vector2 액션이 있어야 합니다.
/// - 예시:
///   Action Map = Player
///   Action Name = Move   (권장)
///   Action Type = Value
///   Control Type = Vector2
///   Composite = 2D Vector (WASD)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AdventurePlayerMove2D : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private Rigidbody2D rigidbody2D;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";

    private InputAction moveAction;
    private Vector2 inputVector;

    private void Reset()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        CacheMoveAction();
    }

    private void OnEnable()
    {
        CacheMoveAction();

        if (moveAction != null)
        {
            moveAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.Disable();
        }
    }

    private void Update()
    {
        if (moveAction == null)
        {
            inputVector = Vector2.zero;
            return;
        }

        inputVector = moveAction.ReadValue<Vector2>();

        // 대각선 이동 속도가 더 빨라지는 것을 막기 위해 정규화합니다.
        if (inputVector.sqrMagnitude > 1f)
        {
            inputVector.Normalize();
        }
    }

    private void FixedUpdate()
    {
        Vector2 moveDelta = inputVector * moveSpeed * Time.fixedDeltaTime;

        if (rigidbody2D != null)
        {
            rigidbody2D.MovePosition(rigidbody2D.position + moveDelta);
        }
        else
        {
            transform.position += (Vector3)moveDelta;
        }
    }

    private void CacheMoveAction()
    {
        moveAction = null;

        if (inputActions == null)
        {
            Debug.LogWarning("[AdventurePlayerMove2D] InputActionAsset이 연결되지 않았습니다.", this);
            return;
        }

        InputActionMap actionMap = inputActions.FindActionMap(actionMapName, false);
        if (actionMap == null)
        {
            Debug.LogWarning($"[AdventurePlayerMove2D] ActionMap을 찾지 못했습니다: {actionMapName}", this);
            return;
        }

        moveAction = actionMap.FindAction(moveActionName, false);
        if (moveAction == null)
        {
            Debug.LogWarning($"[AdventurePlayerMove2D] Move Action을 찾지 못했습니다: {moveActionName}", this);
        }
    }
}