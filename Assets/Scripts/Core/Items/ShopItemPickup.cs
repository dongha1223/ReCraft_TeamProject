using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 상점 전용 아이템 픽업. F 단누름 시 골드를 소모하고 아이템을 획득한다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class ShopItemPickup : MonoBehaviour, IInteractable
    {
        private ItemDefinition _definition;
        private SpriteRenderer _spriteRenderer;

        private static readonly Color _highlightColor = new Color(1f, 0.88f, 0.25f);

        public bool CanInteract => _definition != null;

        public void Init(ItemDefinition definition)
        {
            _definition = definition;
            if (_definition?.icon != null)
                _spriteRenderer.sprite = _definition.icon;
        }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void OnFocused()   => _spriteRenderer.color = _highlightColor;
        public void OnUnfocused() => _spriteRenderer.color = Color.white;

        public void OnInteract(PlayerStatController statController)
        {
            if (_definition == null) return;
            if (statController.InventoryService.IsFull) return;

            if (GoldManager.Instance == null || !GoldManager.Instance.TrySpendGold(_definition.price))
            {
                // 골드 부족 — 추후 UI 피드백 추가 가능
                return;
            }

            var instance = new ItemInstance(_definition);
            statController.InventoryService.Add(instance);
            statController.EquipmentService.Equip(instance);

            gameObject.SetActive(false);
        }
    }
}
