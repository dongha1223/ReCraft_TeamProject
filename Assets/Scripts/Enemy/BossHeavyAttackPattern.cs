using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _2D_Roguelike
{
    /// <summary>
    /// FirePillar와 ArmSlam 두 패턴을 단일 풀 슬롯으로 묶는 디스패처.
    /// BossPattern.patternName = "HeavyAttack" 으로 등록하면 BossMainController가 호출한다.
    ///
    /// 효과:
    ///   - 두 패턴이 같은 쿨타임 슬롯을 공유하므로 연속 실행 불가
    ///   - 실행마다 둘 중 하나를 50:50으로 선택
    /// </summary>
    public class BossHeavyAttackPattern : BossCustomPatternBase
    {
        public override string PatternId => "HeavyAttack";

        [Header("하위 패턴 (같은 오브젝트에 있는 컴포넌트 연결)")]
        [SerializeField] private BossFirePillarPattern _firePillarPattern;
        [SerializeField] private BossArmSlamPattern    _armSlamPattern;

        private BossCustomPatternBase _currentSub;

        // ── BossCustomPatternBase ──────────────────────────────────────────

        protected override IEnumerator ExecuteRoutine()
        {
            _currentSub = PickSubPattern();
            if (_currentSub == null) yield break;

            // 선택된 패턴을 실행하고 완료까지 대기
            yield return _currentSub.BeginExecute();

            _currentSub = null;
        }

        protected override void OnCancel()
        {
            // HeavyAttack 취소 시 현재 실행 중인 서브패턴도 함께 중단
            _currentSub?.Cancel();
            _currentSub = null;
        }

        // ── 선택 로직 ────────────────────────────────────────────────────

        private BossCustomPatternBase PickSubPattern()
        {
            bool firePillarAvail = _firePillarPattern != null;
            bool armSlamAvail    = _armSlamPattern    != null;

            if (firePillarAvail && armSlamAvail)
            {
                if (Random.value < 0.5f) return _firePillarPattern;
                return _armSlamPattern;
            }

            if (firePillarAvail) return _firePillarPattern;
            if (armSlamAvail)    return _armSlamPattern;

            Debug.LogWarning("[HeavyAttack] FirePillar, ArmSlam 둘 다 연결되지 않음");
            return null;
        }
    }
}
