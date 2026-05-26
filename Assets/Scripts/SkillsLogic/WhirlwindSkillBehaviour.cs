using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 회전베기(Whirlwind) 스킬.
    /// A키를 누르는 시간에 따라 3단계로 타격 횟수와 범위가 증가한다.
    ///
    /// 흐름:
    ///   1. A키 누름 → 이동 잠금 + Charging bool true + 차지 SFX
    ///   2. A키 뗌 or 2.5초 경과 → Charging false, 단계 결정
    ///   3. 회전베기 트리거 + SFX + WhirlwindVisual VFX 스폰
    ///   4. 단계별 원형 히트 반복
    ///   5. 이동 잠금 해제
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Behaviour/Whirlwind", fileName = "WhirlwindSkillBehaviour")]
    public class WhirlwindSkillBehaviour : SkillBehaviour
    {
        [System.Serializable]
        public class Stage
        {
            [Tooltip("이 단계가 활성화되는 최소 차지 시간 (초). 오름차순으로 설정할 것.")]
            public float chargeThreshold;
            [Tooltip("회전베기 타격 횟수")]
            public int   hitCount    = 3;
            [Tooltip("회전베기 범위 반지름")]
            public float attackRadius = 2.5f;
            [Tooltip("전체 시전 시간 (초). 타격 간격 = 시전시간 / 타격횟수")]
            public float castDuration = 0.6f;
        }

        [Header("차지")]
        [Tooltip("최대 차지 시간 (초)")]
        [SerializeField] private float     _maxChargeTime     = 2.5f;
        [Tooltip("차지 중 true로 유지할 애니메이터 Bool 파라미터 이름")]
        [SerializeField] private string    _chargeAnimBool    = "Charging";
        [SerializeField] private AudioClip _chargeClip;

        [Header("회전베기")]
        [Tooltip("회전베기 시전 시 재생할 애니메이터 트리거 이름")]
        [SerializeField] private string    _whirlwindAnimTrigger = "Whirlwind";
        [SerializeField] private AudioClip _whirlwindClip;
        [SerializeField] private float     _knockbackForce       = 5f;

        [Header("단계 설정 (chargeThreshold 오름차순)")]
        [SerializeField] private Stage[] _stages = new Stage[]
        {
            new Stage { chargeThreshold = 0f,    hitCount = 3, attackRadius = 2.5f, castDuration = 0.6f },
            new Stage { chargeThreshold = 0.83f, hitCount = 5, attackRadius = 3.5f, castDuration = 0.9f },
            new Stage { chargeThreshold = 1.67f, hitCount = 8, attackRadius = 5.0f, castDuration = 1.2f },
        };

        [Header("히트 판정")]
        [SerializeField] private LayerMask _enemyLayer;

        [Header("고유 상태이상")]
        [SerializeField] private StatusEffectSpec[] _innateEffects;

        public override bool IsChargeSkill => true;

        public override IEnumerator Execute(SkillContext ctx)
        {
            ctx.SetMovementLock?.Invoke(true);

            // ── 1. 차지 ─────────────────────────────────────────────────────
            SafeAnimBool(ctx.Animator, _chargeAnimBool, true);
            SfxManager.Instance?.PlayOneShot(_chargeClip, ctx.PlayerTransform.position);

            float chargeTime = 0f;
            while (chargeTime < _maxChargeTime)
            {
                if (!KeyBindingService.IsPressed(KeyBindingService.Action.Skill1))
                    break;
                chargeTime += Time.deltaTime;
                yield return null;
            }
            chargeTime = Mathf.Min(chargeTime, _maxChargeTime);

            SafeAnimBool(ctx.Animator, _chargeAnimBool, false);

            // ── 2. 단계 결정 ─────────────────────────────────────────────────
            Stage stage = GetStage(chargeTime);

            // ── 3. 회전베기 실행 ─────────────────────────────────────────────
            SafeAnimTrigger(ctx.Animator, _whirlwindAnimTrigger);
            SfxManager.Instance?.PlayOneShot(_whirlwindClip, ctx.PlayerTransform.position);

            SpawnWhirlwindVFX(
                (Vector2)ctx.PlayerTransform.position,
                stage.attackRadius,
                ctx.FacingDirection.x,
                stage.castDuration);

            float hitInterval = stage.castDuration / Mathf.Max(1, stage.hitCount);
            for (int i = 0; i < stage.hitCount; i++)
            {
                ApplyCircleHit(ctx, stage);
                yield return new WaitForSeconds(hitInterval);
            }

            // ── 5. 종료 ─────────────────────────────────────────────────────
            if (ctx.PlayerRb != null)
                ctx.PlayerRb.linearVelocity = new Vector2(0f, ctx.PlayerRb.linearVelocity.y);

            ctx.SetMovementLock?.Invoke(false);
        }

        // ── 단계 선택 ────────────────────────────────────────────────────────

        private Stage GetStage(float chargeTime)
        {
            Stage result = _stages[0];
            for (int i = 1; i < _stages.Length; i++)
            {
                if (chargeTime >= _stages[i].chargeThreshold)
                    result = _stages[i];
                else
                    break;
            }
            return result;
        }

        // ── VFX ──────────────────────────────────────────────────────────────

        private static void SpawnWhirlwindVFX(Vector2 pos, float radius, float dirSign, float lifetime)
        {
            if (SkillObjectPool.Instance == null) return;
            var v = SkillObjectPool.Instance.GetWhirlwindVFX(pos);
            v?.Initialize(radius, dirSign, lifetime);
        }

        // ── 히트 판정 ────────────────────────────────────────────────────────

        private void ApplyCircleHit(SkillContext ctx, Stage stage)
        {
            Vector2 center = ctx.PlayerTransform.position;

            float finalDamage = ctx.StatController != null
                ? ctx.StatController.StatService.GetFinalValue(ctx.Definition.DamageStatType)
                : ctx.Definition.BaseDamage;

            StatusEffectSpec[] effects = MergeSpecs(
                _innateEffects,
                ctx.OnHitRegistry?.GetSpecsFor(OnHitTarget.Skill1));

            var hitInfo = new HitInfo
            {
                AttackId       = AttackIdGenerator.Next(),
                Damage         = finalDamage,
                DamageType     = ctx.Definition.DamageType,
                SourcePosition = center,
                KnockbackForce = _knockbackForce,
                StatusEffects  = effects,
            };

            // 한 타격 내 중복 히트 방지
            var alreadyHit = new HashSet<Collider2D>();

            if (_enemyLayer.value != 0)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(center, stage.attackRadius, _enemyLayer);
                foreach (var col in hits)
                {
                    if (!alreadyHit.Add(col)) continue;
                    var dmg = col.GetComponent<IDamageable>();
                    if (dmg == null) continue;
                    dmg.TakeDamage(hitInfo);
                    PlayerCombatEvents.InvokeDamageDealt(finalDamage);
                }
            }

            // "Enemy" 태그 폴백
            Collider2D[] allCols = Physics2D.OverlapCircleAll(center, stage.attackRadius);
            foreach (var col in allCols)
            {
                if (!col.CompareTag("Enemy")) continue;
                if (!alreadyHit.Add(col)) continue;
                var dmg = col.GetComponent<IDamageable>();
                if (dmg == null) continue;
                dmg.TakeDamage(hitInfo);
                PlayerCombatEvents.InvokeDamageDealt(finalDamage);
            }
        }

        // ── 공통 유틸리티 ────────────────────────────────────────────────────

        private static void SafeAnimTrigger(Animator anim, string triggerName)
        {
            if (anim == null || string.IsNullOrEmpty(triggerName)) return;
            int hash = Animator.StringToHash(triggerName);
            foreach (var p in anim.parameters)
                if (p.nameHash == hash) { anim.SetTrigger(hash); return; }
        }

        private static void SafeAnimBool(Animator anim, string paramName, bool value)
        {
            if (anim == null || string.IsNullOrEmpty(paramName)) return;
            int hash = Animator.StringToHash(paramName);
            foreach (var p in anim.parameters)
                if (p.nameHash == hash) { anim.SetBool(hash, value); return; }
        }

        private static StatusEffectSpec[] MergeSpecs(StatusEffectSpec[] innate, StatusEffectSpec[] fromRegistry)
        {
            bool hasInnate   = innate       != null && innate.Length       > 0;
            bool hasRegistry = fromRegistry != null && fromRegistry.Length > 0;

            if (!hasInnate && !hasRegistry) return null;
            if (!hasInnate)   return fromRegistry;
            if (!hasRegistry) return innate;

            var merged = new StatusEffectSpec[innate.Length + fromRegistry.Length];
            innate.CopyTo(merged, 0);
            fromRegistry.CopyTo(merged, innate.Length);
            return merged;
        }
    }
}
