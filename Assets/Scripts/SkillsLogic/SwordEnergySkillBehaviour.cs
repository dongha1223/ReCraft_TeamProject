using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 검기 발산 스킬 실행 로직.
    /// Project 창 우클릭 → Create → Game/Skill Behaviour/Sword Energy
    /// SkillDefinition.Behaviour 슬롯에 연결해서 사용.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Behaviour/Sword Energy", fileName = "SwordEnergySkillBehaviour")]
    public class SwordEnergySkillBehaviour : SkillBehaviour
    {
        [Header("검기 발산")]
        [Tooltip("상·하현달 수직 오프셋 (두 발의 Y 간격 절반)")]
        [SerializeField] private float _verticalOffset = 0.25f;

        [Tooltip("검기 발산 고유 상태이상 (아이템 무관 고정 효과)")]
        [SerializeField] private StatusEffectSpec[] _innateEffects;

        public override IEnumerator Execute(SkillContext ctx)
        {
            bool    facingLeft  = ctx.PlayerTransform.localScale.x < 0f;
            Vector2 dir         = facingLeft ? Vector2.left : Vector2.right;

            StatusEffectSpec[] statusSpecs = MergeSpecs(
                _innateEffects,
                ctx.OnHitRegistry?.GetSpecsFor(OnHitTarget.Skill1));

            SpawnCrescent(ctx, dir,  _verticalOffset, statusSpecs);
            SpawnCrescent(ctx, dir, -_verticalOffset, statusSpecs);

            yield break;
        }

        private static void SpawnCrescent(SkillContext ctx, Vector2 dir, float yOffset,
                                          StatusEffectSpec[] statusEffects)
        {
            if (SkillObjectPool.Instance == null)
            {
                Debug.LogError("[SwordEnergySkillBehaviour] SkillObjectPool이 없습니다.");
                return;
            }

            float finalDamage = ctx.StatController != null
                ? ctx.StatController.StatService.GetFinalValue(ctx.Definition.DamageStatType)
                : ctx.Definition.BaseDamage;

            Vector2 pos = (Vector2)ctx.PlayerTransform.position + new Vector2(0f, yOffset);
            var p = SkillObjectPool.Instance.GetProjectile(pos);
            if (p == null) return;

            p.Launch(dir, finalDamage, statusEffects, ctx.Definition.DamageType);
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
