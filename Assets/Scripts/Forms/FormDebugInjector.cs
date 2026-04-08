using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    /// <summary>
    /// [디버그 전용] 런타임에 폼을 강제 추가/슬롯 장착하는 테스트 도구.
    /// Phase 5(보상 시스템) 구현 전까지 사용. 완성 후 제거할 것.
    ///
    /// F1 : _formsToInject 전체를 인벤토리에 추가
    /// F2 : 슬롯 0에 _slot0Override 강제 장착
    /// F3 : 슬롯 1에 _slot1Override 강제 장착
    /// </summary>
    public class FormDebugInjector : MonoBehaviour
    {
        [Header("인벤토리에 추가할 폼 목록 (F1)")]
        [SerializeField] private FormDefinition[] _formsToInject;

        [Header("슬롯 직접 오버라이드")]
        [SerializeField] private FormDefinition _slot0Override;
        [SerializeField] private FormDefinition _slot1Override;

        private FormInventory _inventory;
        private FormManager   _formManager;

        private void Awake()
        {
            _inventory   = GetComponent<FormInventory>();
            _formManager = GetComponent<FormManager>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // F1: 목록 전체 인벤토리 추가
            if (kb.f1Key.wasPressedThisFrame)
            {
                if (_formsToInject == null) return;
                foreach (var form in _formsToInject)
                    _inventory.AddForm(form);
                Debug.Log("[FormDebugInjector] F1: 폼 목록 인벤토리 추가 완료");
            }

            // F2: 슬롯 0 강제 장착
            if (kb.f2Key.wasPressedThisFrame && _slot0Override != null)
            {
                _inventory.AddForm(_slot0Override);
                _inventory.EquipToSlot(_slot0Override, 0);
                Debug.Log($"[FormDebugInjector] F2: 슬롯 0 → {_slot0Override.DisplayName}");
            }

            // F3: 슬롯 1 강제 장착
            if (kb.f3Key.wasPressedThisFrame && _slot1Override != null)
            {
                _inventory.AddForm(_slot1Override);
                _inventory.EquipToSlot(_slot1Override, 1);
                Debug.Log($"[FormDebugInjector] F3: 슬롯 1 → {_slot1Override.DisplayName}");
            }
        }
    }
}
