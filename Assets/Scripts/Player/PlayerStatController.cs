using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 플레이어에 붙는 스탯/아이템/각인 시스템의 단일 진입점.
    /// 모든 서비스를 생성·소유하며, 장착 변경 이벤트를 받아 효과를 자동 재계산한다.
    ///
    /// 실행 순서 -100: 다른 Player 컴포넌트의 Awake보다 반드시 먼저 초기화된다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerStatController : MonoBehaviour
    {
        // ── 공개 서비스 ───────────────────────────────────────────────
        public StatService              StatService              { get; private set; }
        public InventoryService         InventoryService         { get; private set; }
        public EquipmentService         EquipmentService         { get; private set; }
        public InscriptionService       InscriptionService       { get; private set; }
        public LoadoutEffectCoordinator LoadoutEffectCoordinator { get; private set; }

        // ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            // 1. 스탯 서비스
            StatService = new StatService();

            // 2. 효과 실행기 등록
            var registry = new EffectExecutorRegistry();
            registry.Register<StatModifierEffectDefinition>(new StatModifierEffectExecutor());
            registry.Register<StatusOnHitEffectDefinition>(new StatusOnHitEffectExecutor());
            // 새 효과 타입이 생기면 여기에 Register 한 줄 추가

            // 3. 나머지 서비스 생성
            var effectService   = new EffectService(registry);
            InventoryService    = new InventoryService();
            EquipmentService    = new EquipmentService();
            InscriptionService  = new InscriptionService();
            var tierResolver    = new InscriptionTierResolver();
            var onHitRegistry   = GetComponent<OnHitStatusRegistry>();

            LoadoutEffectCoordinator = new LoadoutEffectCoordinator(
                effectService, InscriptionService, tierResolver, StatService, onHitRegistry);

            // 4. 장착 변경 시 자동 재계산
            EquipmentService.OnItemEquipped   += OnEquipmentChanged;
            EquipmentService.OnItemUnequipped += OnEquipmentChanged;
        }

        private void OnDestroy()
        {
            EquipmentService.OnItemEquipped   -= OnEquipmentChanged;
            EquipmentService.OnItemUnequipped -= OnEquipmentChanged;
        }

        private void OnEquipmentChanged(ItemInstance _)
        {
            LoadoutEffectCoordinator.Rebuild(EquipmentService.EquippedItems);
        }
    }
}
