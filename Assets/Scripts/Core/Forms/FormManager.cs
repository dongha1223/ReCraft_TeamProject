using System;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 2슬롯 폼 관리 + 비주얼 교체 실행.
    /// 슬롯 0 = 현재 활성 폼, 슬롯 1 = 대기 폼.
    /// Phase 1: 토큰 없이 Q키로 단순 교체 (TokenBank 연결은 Phase 2에서 추가).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class FormManager : MonoBehaviour
    {
        [Header("장착 슬롯 (0 = 현재, 1 = 대기)")]
        [SerializeField] private FormDefinition[] _equippedForms = new FormDefinition[2];

        private Animator        _anim;
        private SpriteRenderer  _sr;
        private BoxCollider2D   _col;
        private PlayerStatController _statController;

        /// <summary>현재 활성 폼</summary>
        public FormDefinition Current  => _equippedForms[0];

        /// <summary>대기 폼</summary>
        public FormDefinition Standby  => _equippedForms[1];

        /// <summary>폼 교체 완료 시 발생 (이전 폼, 새 폼)</summary>
        public event Action<FormDefinition, FormDefinition> OnFormSwapped;

        private void Awake()
        {
            _anim           = GetComponent<Animator>();
            _sr             = GetComponent<SpriteRenderer>();
            _col            = GetComponent<BoxCollider2D>();
            _statController = GetComponent<PlayerStatController>();
        }

        private void Start()
        {
            // 시작 폼 즉시 적용
            if (_equippedForms[0] != null)
                ApplyVisual(_equippedForms[0]);
        }

        // ── 슬롯 교체 ────────────────────────────────────────────────
        /// <summary>슬롯 0↔1 교체 후 비주얼 적용</summary>
        public void SwapSlots()
        {
            if (_equippedForms[0] == null || _equippedForms[1] == null) return;

            var prev = _equippedForms[0];

            (_equippedForms[0], _equippedForms[1]) = (_equippedForms[1], _equippedForms[0]);

            ApplyVisual(_equippedForms[0]);
            ApplyStats(_equippedForms[0]);

            OnFormSwapped?.Invoke(prev, _equippedForms[0]);
        }

        // ── 슬롯에 폼 직접 설정 ──────────────────────────────────────
        /// <summary>특정 슬롯에 폼을 장착 (FormInventory에서 호출)</summary>
        public void SetSlot(int slotIndex, FormDefinition form)
        {
            if (slotIndex < 0 || slotIndex >= _equippedForms.Length) return;
            _equippedForms[slotIndex] = form;

            // 현재 슬롯(0)을 바꿨으면 즉시 비주얼 반영
            if (slotIndex == 0 && form != null)
            {
                ApplyVisual(form);
                ApplyStats(form);
            }
        }

        // ── 비주얼 적용 ──────────────────────────────────────────────
        /// <summary>Animator / Sprite / Collider / Scale 교체</summary>
        public void ApplyVisual(FormDefinition form)
        {
            if (form == null) return;

            // 1. 방향 부호 보존하면서 스케일 교체
            float dirSign = transform.localScale.x < 0f ? -1f : 1f;
            Vector3 scale = form.BaseScale;
            scale.x = Mathf.Abs(scale.x) * dirSign;
            transform.localScale = scale;

            // 2. 콜라이더
            _col.size   = form.ColliderSize;
            _col.offset = form.ColliderOffset;

            // 3. 스프라이트
            if (form.IdleSprite != null)
                _sr.sprite = form.IdleSprite;

            // 4. 애니메이터 컨트롤러 교체 + Idle 강제 재생
            if (form.AnimatorController != null)
            {
                _anim.runtimeAnimatorController = form.AnimatorController;
                _anim.Play("Idle", 0, 0f);
                _anim.Update(0f);
            }
        }

        // ── 스탯 적용 ─────────────────────────────────────────────────
        /// <summary>폼 기본 스탯을 StatService에 반영 (Phase 4에서 실제 연동)</summary>
        private void ApplyStats(FormDefinition form)
        {
            if (_statController == null || form == null) return;
            _statController.ApplyFormStats(form);
        }
    }
}
