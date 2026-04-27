using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 애니메이션 트리거 + 선딜 + 범위 판정 순서로 실행하는 범용 SkillBehaviour.
    /// 자기 주변 폭발처럼 "애니 → 대기 → 범위 적용" 패턴의 스킬에 재사용 가능.
    ///
    /// 사용 예:
    ///   마법사 S스킬 — SkillS 트리거 → preDelay → 자기 주변 Circle 폭발
    ///   단순 범위기  — 트리거 이름과 AreaSkillSpec[]만 바꿔 에셋 복제로 확장
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Behaviour/Animated Area", fileName = "AnimatedAreaSkillBehaviour")]
    public class AnimatedAreaSkillBehaviour : SkillBehaviour
    {
        [Header("애니메이션")]
        [Tooltip("Animator 트리거 이름. 파라미터가 없으면 무시됨.")]
        [SerializeField] private string _animTrigger = "SkillS";
        [Tooltip("트리거 후 실제 판정까지 대기 시간(초). 애니메이션 선딜과 맞춘다.")]
        [SerializeField] private float  _preDelay    = 0.2f;

        [Header("범위 판정 (순차 실행)")]
        [Tooltip("순서대로 실행할 AreaSkillSpec 목록.")]
        [SerializeField] private AreaSkillSpec[] _phases;
        [Tooltip("각 Phase 실행 후 다음 Phase까지 대기 시간(초). 부족하면 0으로 처리.")]
        [SerializeField] private float[]         _phaseDelays;

        // ── 실행 진입점 ───────────────────────────────────────────────
        public override IEnumerator Execute(SkillContext ctx)
        {
            // 1. 애니메이션 트리거
            SafeAnimTrigger(ctx.Animator, _animTrigger);

            // 2. 선딜 (애니메이션 선모션)
            if (_preDelay > 0f)
                yield return new WaitForSeconds(_preDelay);

            // 3. 범위 판정 순차 실행
            if (_phases == null || _phases.Length == 0) yield break;

            Vector2 forward = ctx.FacingDirection;
            Vector2 origin  = ctx.PlayerTransform.position;

            for (int i = 0; i < _phases.Length; i++)
            {
                if (_phases[i] == null) continue;

                ctx.AreaExecutor?.Execute(_phases[i], origin, forward);

                float delay = (_phaseDelays != null && i < _phaseDelays.Length)
                    ? _phaseDelays[i]
                    : 0f;

                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }
        }

        // ── 유틸 ─────────────────────────────────────────────────────
        private static void SafeAnimTrigger(Animator anim, string triggerName)
        {
            if (anim == null || string.IsNullOrEmpty(triggerName)) return;
            int hash = Animator.StringToHash(triggerName);
            foreach (var p in anim.parameters)
                if (p.nameHash == hash) { anim.SetTrigger(hash); return; }
        }
    }
}
