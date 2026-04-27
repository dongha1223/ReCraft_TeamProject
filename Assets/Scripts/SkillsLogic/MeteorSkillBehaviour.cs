using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 메테오 스킬 실행 로직.
    /// Project 창 우클릭 → Create → Game/Skill Behaviour/Meteor
    ///
    /// 흐름:
    ///   애니메이션 트리거
    ///   → meteorCount 개수만큼 순차적으로 SingleMeteor 실행
    ///       → 메테오 프리팹이 spawnPos에서 landingPos까지 대각선 이동
    ///       → 착지 시 즉발 폭발(AreaSkillExecutor) + 불 장판(AreaZoneActor) 스폰
    ///   → 각 메테오 사이 meteorInterval 대기
    ///
    /// MidBossSlam 패턴과 동일하게 모든 로직이 하나의 ScriptableObject 안에 완결됨.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Behaviour/Meteor", fileName = "MeteorSkillBehaviour")]
    public class MeteorSkillBehaviour : SkillBehaviour
    {
        [Header("애니메이션")]
        [Tooltip("MageAnimator의 트리거 이름")]
        [SerializeField] private string _animTrigger = "SkillA";

        [Header("메테오 패턴")]
        [Tooltip("메테오 개수")]
        [SerializeField] private int   _meteorCount    = 3;
        [Tooltip("플레이어 기준 첫 낙하 지점까지 전방 거리")]
        [SerializeField] private float _startOffset    = 2f;
        [Tooltip("낙하 지점 간 간격")]
        [SerializeField] private float _meteorStep     = 1.5f;
        [Tooltip("메테오 하나가 낙하에 걸리는 시간(초)")]
        [SerializeField] private float _fallDuration   = 0.4f;
        [Tooltip("다음 메테오 시작까지 대기 시간(초)")]
        [SerializeField] private float _meteorInterval = 0.2f;

        [Header("스폰 위치 (대각선 각도 결정)")]
        [Tooltip("착지 지점보다 위로 얼마나 띄울지 (Y 오프셋)")]
        [SerializeField] private float _spawnHeight  = 5f;
        [Tooltip("착지 지점보다 뒤로 얼마나 띄울지 (X 오프셋, 방향 자동 반전)")]
        [SerializeField] private float _spawnXOffset = 3f;

        [Header("착지 판정")]
        [Tooltip("착지 순간 즉발 폭발. Circle 또는 Box 권장.")]
        [SerializeField] private AreaSkillSpec _explosionSpec;
        [Tooltip("착지 후 생성될 불 장판. ZoneDuration > 0 필수.")]
        [SerializeField] private AreaSkillSpec _fireZoneSpec;

        [Header("비주얼 프리팹")]
        [Tooltip("낙하 중 표시될 메테오 프리팹 (SpriteRenderer 포함). null이면 시각 없음.")]
        [SerializeField] private GameObject _meteorPrefab;
        [Tooltip("착지 후 장판 프리팹 (SpriteRenderer + AreaZoneActor). null이면 장판 없음.")]
        [SerializeField] private GameObject _fireZonePrefab;

        // ── 실행 진입점 ───────────────────────────────────────────────
        public override IEnumerator Execute(SkillContext ctx)
        {
            SafeAnimTrigger(ctx.Animator, _animTrigger);

            float dir = ctx.FacingDirection.x; // 1 or -1

            for (int i = 0; i < _meteorCount; i++)
            {
                // 착지 지점: 플레이어 전방으로 startOffset + step * i
                float   landX      = dir * (_startOffset + _meteorStep * i);
                Vector2 landingPos = (Vector2)ctx.PlayerTransform.position + new Vector2(landX, 0f);

                // 스폰 지점: 착지 지점 기준 위+뒤 (대각선)
                // 전방 반대 방향으로 X 오프셋 → 앞→뒤 방향으로 떨어지는 연출
                Vector2 spawnPos = landingPos + new Vector2(-dir * _spawnXOffset, _spawnHeight);

                yield return SingleMeteor(ctx, spawnPos, landingPos);

                if (_meteorInterval > 0f)
                    yield return new WaitForSeconds(_meteorInterval);
            }
        }

        // ── 메테오 1개 처리 ───────────────────────────────────────────
        /// <summary>
        /// 메테오 1개를 스폰해 낙하시키고, 착지 시 폭발과 장판을 생성한다.
        /// MidBossSlam의 "이펙트 스폰 → Execute → WaitForSeconds" 패턴을
        /// fallDuration 길이의 이동 루프로 확장한 형태.
        /// </summary>
        private IEnumerator SingleMeteor(SkillContext ctx, Vector2 spawnPos, Vector2 landingPos)
        {
            // 메테오 오브젝트 스폰
            GameObject meteorObj = _meteorPrefab != null
                ? Instantiate(_meteorPrefab, spawnPos, Quaternion.identity)
                : null;

            // 대각선 낙하 이동 (MeteorActor 없이 직접 루프)
            float elapsed = 0f;
            while (elapsed < _fallDuration)
            {
                elapsed += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fallDuration);

                if (meteorObj != null)
                    meteorObj.transform.position = Vector2.Lerp(spawnPos, landingPos, t);

                yield return null;
            }

            // 착지: 메테오 오브젝트 제거
            if (meteorObj != null)
                Destroy(meteorObj);

            // 즉발 폭발 판정
            if (_explosionSpec != null)
                ctx.AreaExecutor?.Execute(_explosionSpec, landingPos, Vector2.down);

            // 불 장판 스폰 (AreaZoneActor가 수명 + 틱 데미지 자동 처리)
            if (_fireZonePrefab != null && _fireZoneSpec != null)
            {
                var zoneObj = Instantiate(_fireZonePrefab, landingPos, Quaternion.identity);
                zoneObj.GetComponent<AreaZoneActor>()?.Initialize(ctx.PlayerTransform, _fireZoneSpec);
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
