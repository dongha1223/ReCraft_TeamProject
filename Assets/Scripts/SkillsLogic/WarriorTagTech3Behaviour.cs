using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 전사 3단계 태그 기술 — 전체 범위 슬래시 체인.
    ///
    /// 동작 흐름:
    ///   1. 현재 스테이지의 생존 적 수를 확인한다.
    ///      - 2명 이상 → _multiEnemyRepeatCount 회 반복
    ///      - 1명       → _singleEnemyRepeatCount 회 반복
    ///   2. 각 반복마다:
    ///      a. 모든 적 위치에 슬래시 이펙트 체인(slash0~3)을 겹쳐 재생한다.
    ///         (각 애니메이션 종료 _overlapOffset 초 전에 다음 애니메이션 시작)
    ///      b. 체인 재생 중 _damageInterval 초마다 모든 적에게 데미지를 적용한다.
    ///
    /// Inspector 설정:
    ///   _slashPrefabs  : SkillEffectActor가 부착된 슬래시 이펙트 프리팹 4종 (slash0~3 순서)
    ///   _damageSpec    : 데미지 판정에 사용할 AreaSkillSpec (적 위치 중심, Radius 작게)
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Tag Technique Behaviour/Warrior Tag 3",
                     fileName = "WarriorTagTech3Behaviour")]
    public class WarriorTagTech3Behaviour : TagTechniqueBehaviour
    {
        [Header("슬래시 이펙트 프리팹 (slash0 → slash3 순서로 연결)")]
        [SerializeField] private GameObject[] _slashPrefabs;

        [Header("겹침 설정")]
        [Tooltip("다음 슬래시 애니메이션을 현재 애니메이션 종료 몇 초 전에 시작할지")]
        [SerializeField] private float _overlapOffset = 0.5f;

        [Header("데미지 설정")]
        [Tooltip("각 틱에서 사용할 AreaSkillSpec. Radius를 작게 설정해 개별 적을 타격한다.")]
        [SerializeField] private AreaSkillSpec _damageSpec;
        [Tooltip("데미지 판정 간격(초)")]
        [SerializeField] private float _damageInterval = 0.4f;

        [Header("반복 횟수")]
        [Tooltip("스테이지에 적이 2명 이상일 때 슬래시 체인 반복 횟수")]
        [SerializeField] private int _multiEnemyRepeatCount  = 3;
        [Tooltip("스테이지에 적이 1명일 때 슬래시 체인 반복 횟수")]
        [SerializeField] private int _singleEnemyRepeatCount = 10;

        // ── 진입점 ────────────────────────────────────────────────────

        public override IEnumerator Execute(TagTechniqueContext ctx)
        {
            var stageManager = StageManager.Instance;
            if (stageManager == null) yield break;

            var enemies = stageManager.GetAliveEnemyTransforms();
            if (enemies.Count == 0) yield break;

            int repeatCount = enemies.Count > 1
                ? _multiEnemyRepeatCount
                : _singleEnemyRepeatCount;

            // TagTechniqueExecutor(MonoBehaviour)를 통해 데미지 틱 코루틴 병렬 실행
            var executor = ctx.PlayerTransform.GetComponent<TagTechniqueExecutor>();

            for (int rep = 0; rep < repeatCount; rep++)
            {
                // 매 반복마다 생존 적 목록 갱신 (중간에 처치될 수 있음)
                enemies = stageManager.GetAliveEnemyTransforms();
                if (enemies.Count == 0) break;

                float chainDuration = CalculateChainDuration();

                // 데미지 틱을 별도 코루틴으로 병렬 실행
                if (executor != null && _damageSpec != null)
                    executor.StartCoroutine(DamageTickRoutine(ctx, enemies, chainDuration));

                // 슬래시 비주얼 체인을 이 코루틴에서 순서대로 실행
                yield return SlashChainRoutine(enemies);
            }
        }

        // ── 비주얼 체인 ───────────────────────────────────────────────

        /// <summary>
        /// slash0~3을 순서대로 스폰하되, 각 애니메이션 종료 _overlapOffset 초 전에
        /// 다음 애니메이션을 시작해 자연스럽게 겹쳐 보이게 한다.
        /// </summary>
        private IEnumerator SlashChainRoutine(List<Transform> enemies)
        {
            if (_slashPrefabs == null || _slashPrefabs.Length == 0) yield break;

            for (int i = 0; i < _slashPrefabs.Length; i++)
            {
                if (_slashPrefabs[i] == null) continue;

                // 현재 슬래시를 살아있는 모든 적 위치에 스폰
                SpawnAtEnemies(i, enemies);

                float duration = GetPrefabDuration(i);

                if (i < _slashPrefabs.Length - 1)
                {
                    // 마지막 이전 슬래시: 종료 _overlapOffset 전에 다음 슬래시 시작
                    float waitTime = Mathf.Max(0f, duration - _overlapOffset);
                    if (waitTime > 0f)
                        yield return new WaitForSeconds(waitTime);
                }
                else
                {
                    // 마지막 슬래시: 애니메이션이 완전히 끝날 때까지 대기
                    if (duration > 0f)
                        yield return new WaitForSeconds(duration);
                }
            }
        }

        // ── 데미지 틱 ─────────────────────────────────────────────────

        /// <summary>
        /// chainDuration 동안 _damageInterval 간격으로 데미지를 적용한다.
        /// SlashChainRoutine과 병렬로 실행된다.
        /// </summary>
        private IEnumerator DamageTickRoutine(
            TagTechniqueContext ctx,
            List<Transform> enemies,
            float chainDuration)
        {
            float elapsed = 0f;

            while (elapsed < chainDuration)
            {
                ApplyDamageToEnemies(ctx, enemies);
                yield return new WaitForSeconds(_damageInterval);
                elapsed += _damageInterval;
            }
        }

        // ── 내부 유틸 ─────────────────────────────────────────────────

        private void SpawnAtEnemies(int prefabIndex, List<Transform> enemies)
        {
            var prefab = _slashPrefabs[prefabIndex];
            if (prefab == null) return;

            foreach (var t in enemies)
            {
                if (t == null || !t.gameObject.activeSelf) continue;
                Instantiate(prefab, t.position, Quaternion.identity);
            }
        }

        private void ApplyDamageToEnemies(TagTechniqueContext ctx, List<Transform> enemies)
        {
            if (ctx.AreaExecutor == null) return;

            foreach (var t in enemies)
            {
                if (t == null || !t.gameObject.activeSelf) continue;
                // 각 적의 위치를 중심으로 범위 판정 실행
                ctx.AreaExecutor.Execute(_damageSpec, t.position, Vector2.zero);
            }
        }

        /// <summary>
        /// 슬래시 체인 1회의 총 재생 시간을 계산한다.
        ///   = Σ(slash0~N-1 의 duration - overlapOffset) + slashN의 duration
        /// </summary>
        private float CalculateChainDuration()
        {
            if (_slashPrefabs == null || _slashPrefabs.Length == 0) return 0f;

            float total = 0f;
            for (int i = 0; i < _slashPrefabs.Length - 1; i++)
                total += Mathf.Max(0f, GetPrefabDuration(i) - _overlapOffset);

            total += GetPrefabDuration(_slashPrefabs.Length - 1);
            return total;
        }

        /// <summary>
        /// 프리팹에 부착된 SkillEffectActor의 TotalDuration을 반환한다.
        /// 컴포넌트가 없으면 0f 반환.
        /// </summary>
        private float GetPrefabDuration(int index)
        {
            if (_slashPrefabs == null || index >= _slashPrefabs.Length) return 0f;
            var prefab = _slashPrefabs[index];
            if (prefab == null) return 0f;
            var actor = prefab.GetComponent<SkillEffectActor>();
            return actor != null ? actor.TotalDuration : 0f;
        }
    }
}
