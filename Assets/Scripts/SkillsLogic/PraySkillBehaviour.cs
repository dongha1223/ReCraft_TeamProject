using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// Pray 스킬.
    ///
    /// 흐름:
    ///   1. Pray 애니메이션 트리거 + SFX + 이동 잠금
    ///   2. 플레이어에 반짝이 이펙트, 플레이어 위에 이펙트 스폰
    ///   3. _prayDuration 동안 대기 (애니메이션 재생 시간에 맞춰 설정)
    ///   4. 이동 잠금 해제
    ///   5. _lifeStealDuration 동안 흡혈: 플레이어가 가한 피해의 _lifeStealRatio 만큼 회복
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Behaviour/Pray", fileName = "PraySkillBehaviour")]
    public class PraySkillBehaviour : SkillBehaviour
    {
        [Header("애니메이션")]
        [Tooltip("플레이어 애니메이터의 Pray 트리거 이름")]
        [SerializeField] private string    _prayAnimTrigger = "Pray";
        [Tooltip("Pray 애니메이션 총 길이 (초). 애니메이션 클립 길이와 맞출 것.")]
        [SerializeField] private float     _prayDuration    = 1.2f;

        [Header("이펙트")]
        [Tooltip("Pray 시작 시 플레이어 위치에 스폰되는 반짝이 이펙트 프리팹")]
        [SerializeField] private GameObject _sparkleEffectPrefab;
        [Tooltip("sparkle 이펙트 자동 소멸 시간 (초, 0 = 소멸 안 함)")]
        [SerializeField] private float      _sparkleLifetime     = 1.2f;

        [Tooltip("플레이어 위에 스폰되는 이펙트 프리팹")]
        [SerializeField] private GameObject _aboveEffectPrefab;
        [Tooltip("위 이펙트의 Y 오프셋 (플레이어 위치 기준)")]
        [SerializeField] private float      _aboveEffectYOffset  = 2.5f;
        [Tooltip("위 이펙트 자동 소멸 시간 (초, 0 = 소멸 안 함)")]
        [SerializeField] private float      _aboveEffectLifetime = 1.5f;

        [Header("흡혈 버프")]
        [Tooltip("Pray 종료 후 흡혈이 유지되는 시간 (초)")]
        [SerializeField] private float _lifeStealDuration = 5f;
        [Tooltip("가한 피해 대비 회복 비율 (0.1 = 10%)")]
        [SerializeField] private float _lifeStealRatio    = 0.1f;

        [Header("사운드")]
        [SerializeField] private AudioClip _praySfx;

        public override IEnumerator Execute(SkillContext ctx)
        {
            ctx.SetMovementLock?.Invoke(true);

            // ── 1. Pray 애니메이션 + SFX ─────────────────────────────────────
            SafeAnimTrigger(ctx.Animator, _prayAnimTrigger);
            SfxManager.Instance?.PlayOneShot(_praySfx, ctx.PlayerTransform.position);

            // ── 2. 이펙트 스폰 ───────────────────────────────────────────────
            SpawnEffects(ctx.PlayerTransform.position);

            // ── 3. Pray 애니메이션 재생 대기 ─────────────────────────────────
            yield return new WaitForSeconds(_prayDuration);

            // ── 4. 이동 잠금 해제 (흡혈 구간은 자유롭게 이동) ──────────────
            ctx.SetMovementLock?.Invoke(false);

            // ── 5. 흡혈 버프 ─────────────────────────────────────────────────
            var playerStats = ctx.PlayerTransform.GetComponent<PlayerStats>();
            if (playerStats != null)
                yield return LifeStealRoutine(playerStats);
        }

        private IEnumerator LifeStealRoutine(PlayerStats stats)
        {
            void OnHit(float damage) => stats.Heal(damage * _lifeStealRatio);

            PlayerCombatEvents.OnDamageDealt += OnHit;
            yield return new WaitForSeconds(_lifeStealDuration);
            PlayerCombatEvents.OnDamageDealt -= OnHit;
        }

        private void SpawnEffects(Vector3 playerPos)
        {
            if (_sparkleEffectPrefab != null)
            {
                var go = Instantiate(_sparkleEffectPrefab, playerPos, Quaternion.identity);
                if (_sparkleLifetime > 0f)
                    Destroy(go, _sparkleLifetime);
            }

            if (_aboveEffectPrefab != null)
            {
                Vector3 abovePos = playerPos + new Vector3(0f, _aboveEffectYOffset, 0f);
                var go = Instantiate(_aboveEffectPrefab, abovePos, Quaternion.identity);
                if (_aboveEffectLifetime > 0f)
                    Destroy(go, _aboveEffectLifetime);
            }
        }

        private static void SafeAnimTrigger(Animator anim, string triggerName)
        {
            if (anim == null || string.IsNullOrEmpty(triggerName)) return;
            int hash = Animator.StringToHash(triggerName);
            foreach (var p in anim.parameters)
                if (p.nameHash == hash) { anim.SetTrigger(hash); return; }
        }
    }
}
