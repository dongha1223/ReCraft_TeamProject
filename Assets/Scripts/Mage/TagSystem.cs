using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    /// <summary>
    /// 스컬 스타일 태그 시스템 (96px 버전 v2)
    /// [Q 키] 전사 ↔ 마법사 전환
    ///
    /// 핵심 수정:
    ///   - 전환 시 해당 캐릭터의 첫 스프라이트를 Resources.Load or
    ///     직접 경로로 즉시 SpriteRenderer에 적용 → 스프라이트 즉시 교체
    ///   - Animator.Play("Idle", 0, 0f) 로 강제 첫 프레임부터 재생
    /// </summary>
    public class TagSystem : MonoBehaviour
    {
        public enum CharacterType { Warrior, Mage }

        [Header("현재 캐릭터")]
        [SerializeField] private CharacterType _currentCharacter = CharacterType.Warrior;

        [Header("전사 컨트롤러")]
        [SerializeField] private RuntimeAnimatorController _warriorController;

        [Header("마법사 컨트롤러")]
        [SerializeField] private RuntimeAnimatorController _mageController;

        [Header("마법사 첫 스프라이트 (idle_01)")]
        [SerializeField] private Sprite _mageIdleSprite;

        [Header("전사 첫 스프라이트 (idle 첫 프레임)")]
        [SerializeField] private Sprite _warriorIdleSprite;

                        [Header("전사 콜라이더")]
        [SerializeField] private Vector2 _warriorColliderSize   = new Vector2(0.15f, 0.31f);
        [SerializeField] private Vector2 _warriorColliderOffset = new Vector2(-0.03f, -0.04f);

        [Header("마법사 콜라이더 (scale=1 기준, 전사의 3배)")]
        [SerializeField] private Vector2 _mageColliderSize   = new Vector2(0.45f, 0.93f);
        [SerializeField] private Vector2 _mageColliderOffset = new Vector2(-0.09f, -0.12f);

[Header("전사 스케일")]
        [SerializeField] private Vector3 _warriorScale = new Vector3(3f, 3f, 3f);

        [Header("마법사 스케일")]
        [SerializeField] private Vector3 _mageScale = new Vector3(1f, 1f, 1f);

[Header("전환 쿨다운 (초)")]
        [SerializeField] private float _tagCooldown = 0.5f;

        // ── 컴포넌트 ──────────────────────────────────────────────────
        private Animator       _animator;
        private SpriteRenderer _sr;
        private PlayerAttack   _warriorAttack;
        private PlayerSkill    _warriorSkill;
        private MageAttack     _mageAttack;
        private TagSwitchUI    _tagUI;

        private bool _isSwitching;

        public CharacterType Current  => _currentCharacter;
        public bool IsMage    => _currentCharacter == CharacterType.Mage;
        public bool IsWarrior => _currentCharacter == CharacterType.Warrior;

        private void Awake()
        {
            _animator      = GetComponent<Animator>();
            _sr            = GetComponent<SpriteRenderer>();
            _warriorAttack = GetComponent<PlayerAttack>();
            _warriorSkill  = GetComponent<PlayerSkill>();
            _mageAttack    = GetComponent<MageAttack>();
            _tagUI         = FindFirstObjectByType<TagSwitchUI>();
        }

        private void Start() => ApplyCharacter(_currentCharacter, instant: true);

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || _isSwitching) return;
            if (kb.qKey.wasPressedThisFrame)
                StartCoroutine(SwitchRoutine());
        }

        // ── 전환 코루틴 ───────────────────────────────────────────────
        private IEnumerator SwitchRoutine()
        {
            _isSwitching = true;
            _tagUI?.OnSwitchStart();

            yield return StartCoroutine(Flash());

            _currentCharacter = IsMage ? CharacterType.Warrior : CharacterType.Mage;
            ApplyCharacter(_currentCharacter, instant: false);

            _tagUI?.OnSwitchEnd();
            yield return new WaitForSeconds(_tagCooldown);
            _isSwitching = false;
        }

        // ── 캐릭터 적용 ───────────────────────────────────────────────
private void ApplyCharacter(CharacterType type, bool instant)
        {
            bool mage = (type == CharacterType.Mage);

            // 1. 공격/스킬 컴포넌트 전환
            if (_warriorAttack != null) _warriorAttack.enabled = !mage;
            if (_warriorSkill  != null) _warriorSkill.enabled  = !mage;
            if (_mageAttack    != null) _mageAttack.enabled    =  mage;

            // 2. 스케일 전환
            transform.localScale = mage ? _mageScale : _warriorScale;

            // 3. BoxCollider2D 보정
            //    전사(scale=3): size=(0.15, 0.31), offset=(-0.03, -0.04)
            //    마법사(scale=1): 실제 월드 크기 유지를 위해 3배 확대
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                if (mage)
                {
                    // 마법사: scale=1 이므로 콜라이더를 3배로
                    col.size   = _mageColliderSize;
                    col.offset = _mageColliderOffset;
                }
                else
                {
                    // 전사: 원래 값 복구
                    col.size   = _warriorColliderSize;
                    col.offset = _warriorColliderOffset;
                }
            }

            // 4. 스프라이트 즉시 교체
            if (_sr != null)
            {
                var firstSprite = mage ? _mageIdleSprite : _warriorIdleSprite;
                if (firstSprite != null)
                    _sr.sprite = firstSprite;

                _sr.color = Color.white;
            }

            // 5. Animator 컨트롤러 교체 + Idle 강제 재생
            if (_animator != null)
            {
                var ctrl = mage ? _mageController : _warriorController;
                if (ctrl != null)
                {
                    _animator.runtimeAnimatorController = ctrl;
                    _animator.Play("Idle", 0, 0f);
                    _animator.Update(0f);
                }
            }
        }

        // ── 깜빡임 ────────────────────────────────────────────────────
        private IEnumerator Flash()
        {
            if (_sr == null) yield break;
            for (int i = 0; i < 4; i++)
            {
                _sr.color = new Color(1f, 1f, 1f, 0.1f);
                yield return new WaitForSeconds(0.04f);
                _sr.color = Color.white;
                yield return new WaitForSeconds(0.04f);
            }
        }
    }
}
