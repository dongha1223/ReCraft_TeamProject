using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 교체기 실행 전담 MonoBehaviour.
    ///
    /// 역할:
    ///   - ScriptableObject(TagTechniqueBehaviour)를 대신해 StartCoroutine 실행
    ///   - Behaviour가 없으면 DefaultPhases(AreaSkillSpec[]) 순차 실행
    ///   - 무적 부여, 후딜 보장
    ///   - 버프 임시 적용/해제
    ///
    /// 호출 순서 (TagController):
    ///   1. Execute(definition)  — 공격 + 무적 + 후딜
    ///   2. FormManager.SwapSlots()
    ///   3. ApplyTempBuff(definition) — 필요 시
    /// </summary>
    public class TagTechniqueExecutor : MonoBehaviour
    {
        private AreaSkillExecutor    _areaExecutor;
        private InvincibilityHandler _invincibility;
        private PlayerStatController _statController;

        private void Awake()
        {
            _areaExecutor   = GetComponent<AreaSkillExecutor>();
            _invincibility  = GetComponent<InvincibilityHandler>();
            _statController = GetComponent<PlayerStatController>();
        }

        // ── 공개 실행 진입점 ──────────────────────────────────────────
        /// <summary>
        /// 교체기를 실행한다.
        /// definition.Duration 시간이 보장된 후 반환한다.
        /// TagController에서 StartCoroutine으로 호출.
        /// </summary>
        public IEnumerator Execute(TagTechniqueDefinition definition)
        {
            float startTime = Time.time;

            // 1. 무적 부여
            _invincibility?.SetInvincible(definition.InvincibleDuration);

            // 2. 컨텍스트 생성
            var ctx = new TagTechniqueContext(
                transform,
                GetComponent<Rigidbody2D>(),
                _areaExecutor,
                _invincibility,
                _statController,
                definition);

            // 3. Behaviour 있으면 위임, 없으면 기본 페이즈 실행
            if (definition.Behaviour != null)
                yield return StartCoroutine(definition.Behaviour.Execute(ctx));
            else
                yield return StartCoroutine(ExecuteDefaultPhases(ctx));

            // 4. Duration까지 남은 시간 후딜 처리
            float remaining = definition.Duration - (Time.time - startTime);
            if (remaining > 0f)
                yield return new WaitForSeconds(remaining);
        }

        /// <summary>
        /// 버프를 임시 적용한다.
        /// SwapSlots() 이후 TagController에서 호출. 별도 코루틴으로 실행됨.
        /// </summary>
        public IEnumerator ApplyTempBuff(TagTechniqueDefinition definition)
        {
            if (!definition.HasBuff || _statController == null) yield break;

            // sourceId에 Time.time을 포함해 중복 적용 구분
            string sourceId = $"tag_buff_{definition.name}_{Time.time:F3}";

            _statController.StatService.AddModifier(
                sourceId,
                definition.BuffStat,
                definition.BuffOperation,
                definition.BuffValue);

            Debug.Log($"[TagTechniqueExecutor] 버프 적용: {definition.BuffStat} " +
                      $"{definition.BuffOperation} {definition.BuffValue} ({definition.BuffDuration}초)");

            yield return new WaitForSeconds(definition.BuffDuration);

            _statController.StatService.RemoveModifiersFromSource(sourceId, definition.BuffStat);

            Debug.Log($"[TagTechniqueExecutor] 버프 해제: {definition.BuffStat}");
        }

        // ── 기본 페이즈 실행 ──────────────────────────────────────────
        private IEnumerator ExecuteDefaultPhases(TagTechniqueContext ctx)
        {
            var phases = ctx.Definition.DefaultPhases;
            var delays = ctx.Definition.PhaseDelays;

            if (phases == null || phases.Length == 0) yield break;

            Vector2 forward = ctx.FacingDirection;
            Vector2 origin  = ctx.PlayerTransform.position;

            for (int i = 0; i < phases.Length; i++)
            {
                if (phases[i] == null) continue;

                ctx.AreaExecutor?.Execute(phases[i], origin, forward);

                float delay = (delays != null && i < delays.Length) ? delays[i] : 0f;
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }
        }
    }
}
