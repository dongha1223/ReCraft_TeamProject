using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 런 중 보유한 모든 폼 목록 관리.
    /// 폼 추가 및 슬롯 장착을 담당.
    /// </summary>
    public class FormInventory : MonoBehaviour
    {
        [Header("런 시작 시 기본 보유 폼")]
        [SerializeField] private FormDefinition _startingFormSlot0;
        [SerializeField] private FormDefinition _startingFormSlot1;

        private FormManager _formManager;

        /// <summary>현재 보유 중인 모든 폼</summary>
        public IReadOnlyList<FormDefinition> OwnedForms => _ownedForms;
        private readonly List<FormDefinition> _ownedForms = new();

        private void Awake()
        {
            _formManager = GetComponent<FormManager>();
        }

        private void Start()
        {
            // 시작 폼 등록 및 슬롯 장착
            if (_startingFormSlot0 != null)
            {
                _ownedForms.Add(_startingFormSlot0);
                _formManager.SetSlot(0, _startingFormSlot0);
            }

            if (_startingFormSlot1 != null)
            {
                _ownedForms.Add(_startingFormSlot1);
                _formManager.SetSlot(1, _startingFormSlot1);
            }
        }

        // ── 폼 추가 ──────────────────────────────────────────────────
        /// <summary>보유 목록에 폼 추가 (보상 드랍 시 호출)</summary>
        public void AddForm(FormDefinition form)
        {
            if (form == null) return;
            if (_ownedForms.Contains(form))
            {
                Debug.LogWarning($"[FormInventory] 이미 보유 중인 폼: {form.DisplayName}");
                return;
            }

            _ownedForms.Add(form);
            Debug.Log($"[FormInventory] 폼 획득: {form.DisplayName}");
        }

        // ── 슬롯 장착 ────────────────────────────────────────────────
        /// <summary>보유 목록의 폼을 특정 슬롯에 장착</summary>
        public void EquipToSlot(FormDefinition form, int slotIndex)
        {
            if (!_ownedForms.Contains(form))
            {
                Debug.LogWarning($"[FormInventory] 보유하지 않은 폼은 장착 불가: {form?.DisplayName}");
                return;
            }

            _formManager.SetSlot(slotIndex, form);
        }
    }
}
