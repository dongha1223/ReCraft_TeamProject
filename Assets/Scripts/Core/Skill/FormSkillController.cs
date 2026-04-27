using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 현재 폼의 A/S 스킬을 실행하는 컨트롤러.
    /// PlayerSkill.cs를 대체한다.
    ///
    /// A키 → FormDefinition.Skills[0]
    /// S키 → FormDefinition.Skills[1]
    ///
    /// 폼 교체 시 FormManager.OnFormSwapped 이벤트를 구독해 스킬 스탯을 갱신한다.
    /// </summary>
    public class FormSkillController : MonoBehaviour
    {
        private FormManager          _formManager;
        private Rigidbody2D          _rb;
        private Animator             _anim;
        private PlayerStatController _statController;
        private OnHitStatusRegistry  _onHitRegistry;
        private AreaSkillExecutor    _areaExecutor;

        private readonly float[] _cooldownTimers = new float[2];
        private readonly bool[]  _isBusy         = new bool[2];

        /// <summary>S스킬(롤링 슬래쉬) 실행 중 여부. PlayerController가 이동 입력 차단에 사용.</summary>
        public bool IsRolling => _isBusy[1];

        /// <summary>0 = 사용 가능, 1 = 방금 사용. UI 냉각 오버레이용.</summary>
        public float Skill1CooldownRatio => GetCooldownRatio(0);
        public float Skill2CooldownRatio => GetCooldownRatio(1);

        private void Awake()
        {
            _formManager    = GetComponent<FormManager>();
            _rb             = GetComponent<Rigidbody2D>();
            _anim           = GetComponent<Animator>();
            _statController = GetComponent<PlayerStatController>();
            _onHitRegistry  = GetComponent<OnHitStatusRegistry>();
            _areaExecutor   = GetComponent<AreaSkillExecutor>();
        }

        private void Start()
        {
            _formManager.OnFormSwapped += OnFormSwapped;
            RegisterSkillStats(_formManager.Current);
        }

        private void OnDestroy()
        {
            if (_formManager != null)
                _formManager.OnFormSwapped -= OnFormSwapped;
        }

        private void OnFormSwapped(FormDefinition prev, FormDefinition next)
        {
            RegisterSkillStats(next);
        }

        /// <summary>폼의 스킬 BaseDamage를 StatService에 기본값으로 등록한다.</summary>
        private void RegisterSkillStats(FormDefinition form)
        {
            if (form?.Skills == null || _statController == null) return;
            foreach (var skill in form.Skills)
            {
                if (skill == null) continue;
                _statController.StatService.SetBaseValue(skill.DamageStatType, skill.BaseDamage);
            }
        }

        private void Update()
        {
            if (UIState.IsBlockingInput) return;

            // 쿨다운 감소
            for (int i = 0; i < 2; i++)
            {
                if (_cooldownTimers[i] > 0f)
                    _cooldownTimers[i] -= Time.deltaTime;
            }

            if (KeyBindingService.WasPressedThisFrame(KeyBindingService.Action.Skill1) && !_isBusy[0])
                TryUseSkill(0);

            // S스킬은 실행 중(_isBusy[1] = IsRolling) 재사용 방지
            if (KeyBindingService.WasPressedThisFrame(KeyBindingService.Action.Skill2) && !_isBusy[1])
                TryUseSkill(1);
        }

        private void TryUseSkill(int slotIndex)
        {
            var form = _formManager?.Current;
            if (form?.Skills == null || slotIndex >= form.Skills.Length) return;

            var def = form.Skills[slotIndex];
            if (def == null) return;
            if (_cooldownTimers[slotIndex] > 0f) return;

            StartCoroutine(UseSkillRoutine(slotIndex, def));
        }

        private IEnumerator UseSkillRoutine(int slotIndex, SkillDefinition def)
        {
            _isBusy[slotIndex] = true;

            var ctx = new SkillContext(transform, _rb, _anim, _statController, _onHitRegistry, _areaExecutor, def);

            switch (def.Type)
            {
                case SkillType.Area:
                    yield return StartCoroutine(ExecuteAreaPhases(ctx));
                    break;
                case SkillType.Custom:
                    if (def.Behaviour != null)
                        yield return StartCoroutine(def.Behaviour.Execute(ctx));
                    break;
            }

            _cooldownTimers[slotIndex] = def.Cooldown;
            _isBusy[slotIndex]         = false;
        }

        private IEnumerator ExecuteAreaPhases(SkillContext ctx)
        {
            var phases = ctx.Definition.AreaPhases;
            var delays = ctx.Definition.PhaseDelays;
            if (phases == null || phases.Length == 0) yield break;

            Vector2 forward = ctx.FacingDirection;
            Vector2 origin  = ctx.PlayerTransform.position;

            for (int i = 0; i < phases.Length; i++)
            {
                if (phases[i] == null) continue;
                _areaExecutor?.Execute(phases[i], origin, forward);
                float delay = (delays != null && i < delays.Length) ? delays[i] : 0f;
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }
        }

        private float GetCooldownRatio(int slotIndex)
        {
            var form = _formManager?.Current;
            if (form?.Skills == null || slotIndex >= form.Skills.Length) return 0f;
            var def = form.Skills[slotIndex];
            if (def == null || def.Cooldown <= 0f) return 0f;
            return Mathf.Clamp01(_cooldownTimers[slotIndex] / def.Cooldown);
        }

        /// <summary>스테이지 재시작 시 상태 전체 초기화. StageManager에서 호출.</summary>
        public void ResetSkills()
        {
            StopAllCoroutines();
            for (int i = 0; i < 2; i++)
            {
                _cooldownTimers[i] = 0f;
                _isBusy[i]         = false;
            }
            transform.rotation = Quaternion.identity;
            if (_rb != null)
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }
    }
}
