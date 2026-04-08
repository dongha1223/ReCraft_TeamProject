using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    /// <summary>
    /// F키 상호작용의 유일한 중앙 처리기.
    /// 매 프레임 주변 IInteractable을 탐색해 가장 가까운 대상을 포커싱하고,
    /// F 단누름 / 길게 누름을 구분해 각 인터페이스 메서드를 호출한다.
    ///
    /// ※ 씬의 IInteractable 오브젝트는 반드시 Collider2D를 보유해야 탐색된다.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float _interactionRadius = 2f;

        private PlayerStatController _statController;
        private IInteractable        _focused;
        private float                _holdTimer;
        private bool                 _holdTriggered;

        // 물리 쿼리용 버퍼 및 필터
        private readonly Collider2D[] _overlapBuffer = new Collider2D[16];
        private ContactFilter2D _contactFilter;

        private void Awake()
        {
            _statController = GetComponent<PlayerStatController>();
            _contactFilter.useTriggers = true;
            _contactFilter.useLayerMask = false; // 모든 레이어
        }

        private void Update()
        {
            UpdateFocus();
            HandleInput();
        }

        // ── 포커스 갱신 ────────────────────────────────────────────────

        private void UpdateFocus()
        {
            IInteractable closest = FindClosest();

            if (closest == _focused) return;

            _focused?.OnUnfocused();
            _focused = closest;
            _focused?.OnFocused();

            // 포커스 대상이 바뀌면 홀드 상태 초기화
            _holdTimer     = 0f;
            _holdTriggered = false;
        }

        private IInteractable FindClosest()
        {
            int count = Physics2D.OverlapCircle(
                transform.position, _interactionRadius, _contactFilter, _overlapBuffer);

            IInteractable best    = null;
            float         minSqrDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (_overlapBuffer[i] == null) continue;
                if (_overlapBuffer[i].gameObject == gameObject) continue; // 자기 자신 제외

                var interactable = _overlapBuffer[i].GetComponent<IInteractable>();
                if (interactable == null || !interactable.CanInteract) continue;

                float dist = ((Vector2)transform.position - (Vector2)_overlapBuffer[i].transform.position).sqrMagnitude;

                if (dist < minSqrDist)
                {
                    minSqrDist = dist;
                    best       = interactable;
                }
            }

            return best;
        }

        // ── 입력 처리 ─────────────────────────────────────────────────

        private void HandleInput()
        {
            if (_focused == null) return;

            if (_focused is IHoldInteractable holdable)
                HandleHoldInput(holdable);
            else if (KeyBindingService.WasPressedThisFrame(KeyBindingService.Action.Interact))
                _focused.OnInteract(_statController);
        }

        /// <summary>
        /// 홀드 가능한 대상의 입력 처리.
        /// HoldDuration 전에 떼면 단누름, 이상이면 길게 누름으로 구분한다.
        /// </summary>
        private void HandleHoldInput(IHoldInteractable holdable)
        {
            // 매 프레임 KeyBindingService 중복 조회 방지 — 키 컨트롤 한 번만 획득
            var kb = Keyboard.current;
            if (kb == null) return;
            var ctrl = kb[KeyBindingService.Get(KeyBindingService.Action.Interact)];

            if (ctrl.wasPressedThisFrame)
            {
                _holdTimer     = 0f;
                _holdTriggered = false;
            }

            if (ctrl.isPressed && !_holdTriggered)
            {
                _holdTimer += Time.deltaTime;

                if (_holdTimer >= holdable.HoldDuration)
                {
                    _holdTriggered = true;
                    holdable.OnHoldInteract(_statController);
                }
            }

            if (ctrl.wasReleasedThisFrame)
            {
                if (!_holdTriggered)
                    holdable.OnInteract(_statController); // 짧게 눌렀다 뗌 → 단누름

                _holdTimer     = 0f;
                _holdTriggered = false;
            }
        }

        // ── 에디터 시각화 ─────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactionRadius);
        }
    }
}
