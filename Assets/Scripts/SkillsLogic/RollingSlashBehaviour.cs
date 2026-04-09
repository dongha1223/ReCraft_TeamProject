using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 롤링 슬래쉬 스킬 실행 로직.
    /// Project 창 우클릭 → Create → Game/Skill Behaviour/Rolling Slash
    /// SkillDefinition.Behaviour 슬롯에 연결해서 사용.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Behaviour/Rolling Slash", fileName = "RollingSlashBehaviour")]
    public class RollingSlashBehaviour : SkillBehaviour
    {
        [Header("롤링 슬래쉬")]
        [Tooltip("1회 구르기당 전진 거리")]
        [SerializeField] private float    _rollDistance   = 1.1f;
        [Tooltip("1회 구르기 소요 시간 (초)")]
        [SerializeField] private float    _rollTime       = 0.22f;
        [SerializeField] private float    _knockbackForce = 6f;
        [Tooltip("가로 타원 크기 (width > height)")]
        [SerializeField] private Vector2  _ovalSize       = new Vector2(2.6f, 1.0f);
        [SerializeField] private LayerMask _enemyLayer;

        [Tooltip("롤링 슬래쉬 고유 상태이상 (아이템 무관 고정 효과)")]
        [SerializeField] private StatusEffectSpec[] _innateEffects;

        private static readonly int AnimRollingSlash = Animator.StringToHash("RollingSlash");

        public override IEnumerator Execute(SkillContext ctx)
        {
            float rollDirSign = ctx.PlayerTransform.localScale.x < 0f ? -1f : 1f;
            float moveSpeed   = _rollDistance / _rollTime;

            SafeAnimTrigger(ctx.Animator, AnimRollingSlash);

            var alreadyHit = new HashSet<Collider2D>();

            for (int roll = 0; roll < 3; roll++)
            {
                yield return SingleRoll(ctx, moveSpeed, rollDirSign);

                Vector2 center = ctx.PlayerTransform.position;
                SpawnSlashVFX(center, rollDirSign);
                ApplyOvalHit(ctx, center, alreadyHit);

                yield return new WaitForSeconds(0.04f);
            }

            // 완전 종료: 수평 속도 멈추고 정자세 확정
            if (ctx.PlayerRb != null)
                ctx.PlayerRb.linearVelocity = new Vector2(0f, ctx.PlayerRb.linearVelocity.y);
            ctx.PlayerTransform.rotation = Quaternion.identity;
        }

        /// <summary>1회 구르기: rollTime 동안 전진 + Z축 360° 회전 후 정자세 스냅</summary>
        private IEnumerator SingleRoll(SkillContext ctx, float moveSpeed, float rollDirSign)
        {
            float elapsed   = 0f;
            float totalTime = _rollTime;
            // 앞구르기 = 진행 방향 반대 회전 (자연스러운 텀블링)
            float rotDir = -rollDirSign;

            while (elapsed < totalTime)
            {
                elapsed += Time.deltaTime;

                if (ctx.PlayerRb != null)
                    ctx.PlayerRb.linearVelocity = new Vector2(
                        moveSpeed * rollDirSign,
                        ctx.PlayerRb.linearVelocity.y);

                float t      = Mathf.Clamp01(elapsed / totalTime);
                float zAngle = rotDir * 360f * t;
                ctx.PlayerTransform.rotation = Quaternion.Euler(0f, 0f, zAngle);

                yield return null;
            }

            // 1회 구르기 끝 → 반드시 정자세(0°)로 스냅
            ctx.PlayerTransform.rotation = Quaternion.identity;
            if (ctx.PlayerRb != null)
                ctx.PlayerRb.linearVelocity = new Vector2(0f, ctx.PlayerRb.linearVelocity.y);
        }

        private void SpawnSlashVFX(Vector2 pos, float rollDirSign)
        {
            if (SkillObjectPool.Instance == null) return;
            var v = SkillObjectPool.Instance.GetSlashVFX(pos);
            v?.Initialize(_ovalSize, rollDirSign);
        }

        private void ApplyOvalHit(SkillContext ctx, Vector2 center, HashSet<Collider2D> alreadyHit)
        {
            float finalDamage = ctx.StatController != null
                ? ctx.StatController.StatService.GetFinalValue(ctx.Definition.DamageStatType)
                : ctx.Definition.BaseDamage;

            StatusEffectSpec[] statusEffects = MergeSpecs(
                _innateEffects,
                ctx.OnHitRegistry?.GetSpecsFor(OnHitTarget.Skill2));

            var hitInfo = new HitInfo
            {
                Damage         = finalDamage,
                DamageType     = ctx.Definition.DamageType,
                SourcePosition = ctx.PlayerTransform.position,
                KnockbackForce = _knockbackForce,
                StatusEffects  = statusEffects
            };

            // 1차: LayerMask
            if (_enemyLayer.value != 0)
            {
                Collider2D[] hits = Physics2D.OverlapCapsuleAll(
                    center, _ovalSize, CapsuleDirection2D.Horizontal, 0f, _enemyLayer);
                foreach (var col in hits)
                {
                    if (alreadyHit.Contains(col)) continue;
                    var damageable = col.GetComponent<IDamageable>();
                    if (damageable == null) continue;
                    alreadyHit.Add(col);
                    damageable.TakeDamage(hitInfo);
                }
            }

            // 2차: "Enemy" 태그 폴백
            Collider2D[] all = Physics2D.OverlapCapsuleAll(
                center, _ovalSize, CapsuleDirection2D.Horizontal, 0f);
            foreach (var col in all)
            {
                if (alreadyHit.Contains(col)) continue;
                if (!col.CompareTag("Enemy")) continue;
                var damageable = col.GetComponent<IDamageable>();
                if (damageable == null) continue;
                alreadyHit.Add(col);
                damageable.TakeDamage(hitInfo);
            }
        }

        private static void SafeAnimTrigger(Animator anim, int hash)
        {
            if (anim == null) return;
            foreach (var p in anim.parameters)
                if (p.nameHash == hash) { anim.SetTrigger(hash); return; }
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
