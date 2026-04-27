using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 씬에 배치되는 아이템 픽업 오브젝트.
    ///
    /// F 단누름  → 수집 (인벤토리 추가 + 즉시 장착)
    /// F 길게누름 → 분해 (추후 재화 지급 연동)
    ///
    /// ※ 이 컴포넌트가 붙는 GameObject에는 반드시 Collider2D가 있어야
    ///   PlayerInteractor의 범위 탐색에 감지된다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ItemPickup : MonoBehaviour, IHoldInteractable
    {
        [SerializeField] private ItemDefinition _definition;
        [SerializeField] private float          _holdDuration = 1.5f;

        private SpriteRenderer _spriteRenderer;

        // ── IHoldInteractable ─────────────────────────────────────────
        public bool  CanInteract  => _definition != null;
        public float HoldDuration => _holdDuration;

        public void OnFocused()
        {
            _spriteRenderer.color = new Color(1f, 0.88f, 0.25f); // 노란 하이라이트
        }

        public void OnUnfocused()
        {
            _spriteRenderer.color = Color.white;
        }

        public void OnInteract(PlayerStatController statController)
        {
            // F 단누름: 수집 후 즉시 장착
            var instance = new ItemInstance(_definition);
            statController.InventoryService.Add(instance);
            statController.EquipmentService.Equip(instance);

            gameObject.SetActive(false);
        }

        public void OnHoldInteract(PlayerStatController statController)
        {
            // F 길게 누름: 분해 (추후 재화 지급 로직 추가)
            Debug.Log($"[ItemPickup] '{_definition?.displayName}' 분해됨");
            gameObject.SetActive(false);
        }

        // ── 생명주기 ─────────────────────────────────────────────────
        /// <summary>런타임 스폰 시 아이템 정의를 주입한다.</summary>
        public void Init(ItemDefinition definition)
        {
            _definition = definition;
            if (_definition != null && _definition.icon != null)
                _spriteRenderer.sprite = _definition.icon;
        }


        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_definition != null && _definition.icon != null)
                _spriteRenderer.sprite = _definition.icon;
        }
    }
}
