using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _2D_Roguelike
{
    /// <summary>
    /// 보스의 모든 공격 패턴 코루틴을 구현하는 실행 엔진.
    /// BossController가 StartPhase1Loop / StartPhase2Loop / StopAll을 호출한다.
    /// 생성된 투사체·하수인·장판은 추적 리스트에 등록되어 StopAll 시 일괄 정리된다.
    /// </summary>
    public class BossPatternExecutor : MonoBehaviour
    {
        // ── Phase 1 공통 ──────────────────────────────────────────────────
        [Header("Phase 1 — 패턴 간격")]
        [SerializeField] private float _phase1CooldownMin = 2f;
        [SerializeField] private float _phase1CooldownMax = 4f;

        // ── 패턴 1: 조준 투사체 ───────────────────────────────────────────
        [Header("패턴 1 — 조준 투사체")]
        [Tooltip("플레이어 위치 조준선 전조 이펙트 프리팹")]
        [SerializeField] private GameObject _aimIndicatorPrefab;
        [Tooltip("투사체 프리팹 (BossProjectile 컴포넌트 포함)")]
        [SerializeField] private GameObject _projectilePrefab;
        [Tooltip("보스에서 투사체가 발사되는 스폰 포인트 배열 (4개 권장)")]
        [SerializeField] private Transform[] _projectileSpawnPoints;
        [Tooltip("조준선 간 간격 (초)")]
        [SerializeField] private float _aimInterval       = 0.6f;
        [Tooltip("조준선 표시 지속 시간 (초)")]
        [SerializeField] private float _aimDuration       = 1.5f;
        [Tooltip("투사체 데미지")]
        [SerializeField] private float _projectileDamage  = 18f;
        [Tooltip("투사체 넉백")]
        [SerializeField] private float _projectileKnockback = 4f;
        [Tooltip("조준선 간 발사 딜레이 (초)")]
        [SerializeField] private float _fireInterval      = 0.3f;

        // ── 패턴 2 공통: 하수인 ──────────────────────────────────────────
        [Header("패턴 2 — 하수인 공통")]
        [Tooltip("검은색 하수인 프리팹 (BlackMinion 컴포넌트 포함)")]
        [SerializeField] private GameObject _blackMinionPrefab;
        [Tooltip("하얀색 하수인 프리팹 (WhiteMinion 컴포넌트 포함)")]
        [SerializeField] private GameObject _whiteMinionPrefab;

        // ── 패턴 2-1: 검은 하수인 수직 ───────────────────────────────────
        [Header("패턴 2-1 — 검은 하수인 수직")]
        [Tooltip("수직 라인 스폰 기준 X 좌표 (보스 X + 이 값)")]
        [SerializeField] private float _pattern21SpawnOffsetX  = 3f;
        [Tooltip("수직 라인 최하단 Y (월드 좌표)")]
        [SerializeField] private float _pattern21BottomY       = -2f;
        [Tooltip("하수인 간 세로 간격 (m)")]
        [SerializeField] private float _pattern21VerticalStep  = 2f;

        // ── 패턴 2-2: 하얀 하수인 대각 ───────────────────────────────────
        [Header("패턴 2-2 — 하얀 하수인 대각")]
        [Tooltip("플레이어 기준 오프셋 배열 (4개). 대각 구조 예: (+3,+3),(+3,-3),(-3,-3),(-3,+3)")]
        [SerializeField] private Vector2[] _pattern22Offsets = new Vector2[]
        {
            new Vector2( 3f,  3f),
            new Vector2( 3f, -1f),
            new Vector2(-3f, -1f),
            new Vector2(-3f,  3f),
        };

        // ── 패턴 2-3: 하얀 하수인 수직 ───────────────────────────────────
        [Header("패턴 2-3 — 하얀 하수인 수직")]
        [Tooltip("플레이어 기준 오프셋 배열 (4개). 수직 라인 예: (+4,3),(+4,1),(+4,-1),(+4,-3)")]
        [SerializeField] private Vector2[] _pattern23Offsets = new Vector2[]
        {
            new Vector2(4f,  3f),
            new Vector2(4f,  1f),
            new Vector2(4f, -1f),
            new Vector2(4f, -3f),
        };

        // ── Phase 2 공통 ──────────────────────────────────────────────────
        [Header("Phase 2 — 패턴 간격")]
        [SerializeField] private float _phase2CooldownMin = 2.5f;
        [SerializeField] private float _phase2CooldownMax = 4f;

        // ── Phase 2: 강화 투사체 장판 ─────────────────────────────────────
        [Header("Phase 2 — 장판 스펙")]
        [Tooltip("Phase 2 투사체 착지 시 생성할 장판 AreaSkillSpec SO")]
        [SerializeField] private AreaSkillSpec _phase2ZoneSpec;

        // ── Phase 2 패턴 A: 검은 하수인 3플랫폼 ─────────────────────────
        [Header("Phase 2 패턴 A")]
        [Tooltip("true면 플레이어 플랫폼도 포함해 3개 선정. false면 플레이어 플랫폼은 제외.")]
        [SerializeField] private bool _patternA_IncludePlayerPlatform = false;

        // ── Phase 2 패턴 C: 반복 레이저 ──────────────────────────────────
        [Header("Phase 2 패턴 C")]
        [Tooltip("패턴 C 반복 횟수")]
        [SerializeField] private int   _patternC_RepeatCount  = 4;
        [Tooltip("반복 간 딜레이 (초) — 이 시간 안에 플레이어가 플랫폼 이동 가능")]
        [SerializeField] private float _patternC_RepeatDelay  = 3f;

        [Header("데이터 SO")]
        [SerializeField] private BossPatternDataSO _data;

        // ── 내부 상태 ─────────────────────────────────────────────────────
        private Transform         _player;
        private PlatformManager   _platformManager;
        private Coroutine         _loopHandle;

        // 활성 오브젝트 추적 — StopAll 시 일괄 Destroy
        private readonly List<GameObject> _activeProjectiles  = new();
        private readonly List<GameObject> _activeMinions      = new();
        private readonly List<GameObject> _activeDamageZones  = new();

        // ── 생명주기 ──────────────────────────────────────────────────────

        private void Awake()
        {
            _platformManager = FindFirstObjectByType<PlatformManager>();
            if (_data != null)
            {
                _phase1CooldownMin              = _data.Phase1CooldownMin;
                _phase1CooldownMax              = _data.Phase1CooldownMax;
                if (_data.AimIndicatorPrefab != null) _aimIndicatorPrefab = _data.AimIndicatorPrefab;
                if (_data.ProjectilePrefab   != null) _projectilePrefab   = _data.ProjectilePrefab;
                _aimInterval                    = _data.AimInterval;
                _aimDuration                    = _data.AimDuration;
                _projectileDamage               = _data.ProjectileDamage;
                _projectileKnockback            = _data.ProjectileKnockback;
                _fireInterval                   = _data.FireInterval;
                if (_data.BlackMinionPrefab  != null) _blackMinionPrefab  = _data.BlackMinionPrefab;
                if (_data.WhiteMinionPrefab  != null) _whiteMinionPrefab  = _data.WhiteMinionPrefab;
                _pattern21SpawnOffsetX          = _data.Pattern21SpawnOffsetX;
                _pattern21BottomY               = _data.Pattern21BottomY;
                _pattern21VerticalStep          = _data.Pattern21VerticalStep;
                if (_data.Pattern22Offsets   != null) _pattern22Offsets   = _data.Pattern22Offsets;
                if (_data.Pattern23Offsets   != null) _pattern23Offsets   = _data.Pattern23Offsets;
                _phase2CooldownMin              = _data.Phase2CooldownMin;
                _phase2CooldownMax              = _data.Phase2CooldownMax;
                if (_data.Phase2ZoneSpec     != null) _phase2ZoneSpec     = _data.Phase2ZoneSpec;
                _patternA_IncludePlayerPlatform = _data.PatternA_IncludePlayerPlatform;
                _patternC_RepeatCount           = _data.PatternC_RepeatCount;
                _patternC_RepeatDelay           = _data.PatternC_RepeatDelay;
            }
        }

        private void Start()
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null) _player = playerGO.transform;
        }

        // ── 루프 진입점 ───────────────────────────────────────────────────

        public void StartPhase1Loop()
        {
            StopLoop();
            _loopHandle = StartCoroutine(Phase1Loop());
        }

        public void StartPhase2Loop()
        {
            StopLoop();
            _loopHandle = StartCoroutine(Phase2Loop());
        }

        public void StopAll()
        {
            StopLoop();
            CleanupTrackedObjects();
        }

        // ── Phase 1 루프 ──────────────────────────────────────────────────

        private IEnumerator Phase1Loop()
        {
            int lastPattern = -1;

            while (true)
            {
                yield return new WaitForSeconds(Random.Range(_phase1CooldownMin, _phase1CooldownMax));

                // 0~3 중 직전과 다른 패턴 선택
                int idx = SelectPattern(4, lastPattern);
                lastPattern = idx;

                IEnumerator phase1Pattern;
                switch (idx)
                {
                    case 0:  phase1Pattern = Pattern1_AimProjectile(phase2Mode: false); break;
                    case 1:  phase1Pattern = Pattern2_1_BlackMinionsVertical();         break;
                    case 2:  phase1Pattern = Pattern2_2_WhiteMinionsDiagonal();         break;
                    default: phase1Pattern = Pattern2_3_WhiteMinionsVertical();         break;
                }
                yield return StartCoroutine(phase1Pattern);
            }
        }

        // ── Phase 2 루프 ──────────────────────────────────────────────────

        private IEnumerator Phase2Loop()
        {
            int lastPattern = -1;

            while (true)
            {
                yield return new WaitForSeconds(Random.Range(_phase2CooldownMin, _phase2CooldownMax));

                int idx = SelectPattern(4, lastPattern);
                lastPattern = idx;

                IEnumerator phase2Pattern;
                switch (idx)
                {
                    case 0:  phase2Pattern = Pattern1_AimProjectile(phase2Mode: true);      break;
                    case 1:  phase2Pattern = Phase2_PatternA_BlackMinions3Platforms();      break;
                    case 2:  phase2Pattern = Phase2_PatternB_WhiteMinionsAllPlatforms();    break;
                    default: phase2Pattern = Phase2_PatternC_BlackMinionsRepeat();          break;
                }
                yield return StartCoroutine(phase2Pattern);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  패턴 1 — 조준 투사체
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 플레이어 위치에 전조 이펙트를 순차 출력하며 좌표를 기록한 뒤,
        /// 기록된 좌표를 향해 투사체를 순서대로 발사한다.
        /// phase2Mode = true 이면 투사체가 착지 시 장판을 생성한다.
        /// </summary>
        private IEnumerator Pattern1_AimProjectile(bool phase2Mode)
        {
            if (_projectileSpawnPoints == null || _projectileSpawnPoints.Length == 0) yield break;

            int count = _projectileSpawnPoints.Length;
            var storedPositions = new Vector3[count];

            // ① 조준선 전조 이펙트 순차 표시 + 위치 저장
            //    (이펙트 출력 순간의 플레이어 위치를 기록 — 발사 이후 플레이어 이동 무관)
            for (int i = 0; i < count; i++)
            {
                storedPositions[i] = _player != null ? _player.position : transform.position;

                if (_aimIndicatorPrefab != null)
                {
                    var indicator = SpawnTracked(_aimIndicatorPrefab, storedPositions[i], Quaternion.identity, _activeDamageZones);
                    // 전조 이펙트는 aimDuration 후 스스로 파괴되거나 수동 제거
                    Destroy(indicator, _aimDuration);
                }

                if (i < count - 1)
                    yield return new WaitForSeconds(_aimInterval);
            }

            yield return new WaitForSeconds(_aimDuration);

            // ② 기록된 좌표를 향해 순서대로 투사체 발사
            for (int i = 0; i < count; i++)
            {
                if (_projectilePrefab == null) continue;

                var spawnPoint = _projectileSpawnPoints[i % _projectileSpawnPoints.Length];
                var go         = SpawnTracked(_projectilePrefab, spawnPoint.position, Quaternion.identity, _activeProjectiles);
                var proj       = go.GetComponent<BossProjectile>();
                if (proj != null)
                {
                    proj.SetPhase2Mode(phase2Mode);
                    proj.OnZoneSpawned += zone => _activeDamageZones.Add(zone);
                    proj.Setup(storedPositions[i], new HitInfo
                    {
                        Damage         = _projectileDamage,
                        DamageType     = DamageType.Magic,
                        SourcePosition = spawnPoint.position,
                        KnockbackForce = _projectileKnockback,
                    });
                }

                if (i < count - 1)
                    yield return new WaitForSeconds(_fireInterval);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  패턴 2-1 — 검은 하수인 수직 라인 4마리
        // ══════════════════════════════════════════════════════════════════

        private IEnumerator Pattern2_1_BlackMinionsVertical()
        {
            if (_blackMinionPrefab == null) yield break;

            float spawnX = transform.position.x + _pattern21SpawnOffsetX;
            int   facing = _pattern21SpawnOffsetX >= 0 ? -1 : 1; // 보스 방향으로 바라봄

            for (int i = 0; i < 4; i++)
            {
                float  spawnY = _pattern21BottomY + i * _pattern21VerticalStep;
                var    pos    = new Vector3(spawnX, spawnY, 0f);
                SpawnMinion(_blackMinionPrefab, pos, facing);
            }

            yield return null;
        }

        // ══════════════════════════════════════════════════════════════════
        //  패턴 2-2 — 하얀 하수인 대각 4마리
        // ══════════════════════════════════════════════════════════════════

        private IEnumerator Pattern2_2_WhiteMinionsDiagonal()
        {
            if (_whiteMinionPrefab == null || _player == null) yield break;
            if (_pattern22Offsets == null || _pattern22Offsets.Length < 4) yield break;

            // 오른쪽이 위/아래인지 랜덤으로 결정 (대각선 방향 두 가지)
            bool rightIsUpper = Random.value > 0.5f;

            for (int i = 0; i < 4; i++)
            {
                var offset = _pattern22Offsets[i];
                // 오른쪽이 아래인 경우 Y 부호 반전
                if (!rightIsUpper) offset.y = -offset.y;

                var pos    = (Vector2)_player.position + offset;
                int facing = offset.x >= 0 ? -1 : 1; // 플레이어 방향으로 바라봄
                SpawnMinion(_whiteMinionPrefab, pos, facing);
            }

            yield return null;
        }

        // ══════════════════════════════════════════════════════════════════
        //  패턴 2-3 — 하얀 하수인 수직 라인 4마리
        // ══════════════════════════════════════════════════════════════════

        private IEnumerator Pattern2_3_WhiteMinionsVertical()
        {
            if (_whiteMinionPrefab == null || _player == null) yield break;
            if (_pattern23Offsets == null || _pattern23Offsets.Length < 4) yield break;

            for (int i = 0; i < 4; i++)
            {
                var offset = _pattern23Offsets[i];
                var pos    = (Vector2)_player.position + offset;
                int facing = offset.x >= 0 ? -1 : 1;
                SpawnMinion(_whiteMinionPrefab, pos, facing);
            }

            yield return null;
        }

        // ══════════════════════════════════════════════════════════════════
        //  Phase 2 패턴 A — 3개 플랫폼 오른쪽에 검은 하수인 2마리씩
        // ══════════════════════════════════════════════════════════════════

        private IEnumerator Phase2_PatternA_BlackMinions3Platforms()
        {
            if (_blackMinionPrefab == null || _platformManager == null) yield break;

            int playerPlatform = _platformManager.CurrentPlatformIndex;
            int count          = _platformManager.PlatformCount;

            // 플랫폼 인덱스 후보 선정 (4개 중 3개)
            var candidates = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (!_patternA_IncludePlayerPlatform && i == playerPlatform) continue;
                candidates.Add(i);
            }

            // 후보가 3개 초과면 3개만 무작위 선택
            while (candidates.Count > 3)
                candidates.RemoveAt(Random.Range(0, candidates.Count));

            foreach (int platformIdx in candidates)
            {
                var spawnPoints = _platformManager.GetBlackMinionSpawnPoints(platformIdx);
                foreach (var sp in spawnPoints)
                {
                    if (sp == null) continue;
                    // 플랫폼 오른쪽에서 왼쪽(플랫폼 안쪽)으로 바라봄
                    SpawnMinion(_blackMinionPrefab, sp.position, facingDir: -1);
                }
            }

            yield return null;
        }

        // ══════════════════════════════════════════════════════════════════
        //  Phase 2 패턴 B — 4개 플랫폼 주변에 하얀 하수인 2마리씩
        // ══════════════════════════════════════════════════════════════════

        private IEnumerator Phase2_PatternB_WhiteMinionsAllPlatforms()
        {
            if (_whiteMinionPrefab == null || _platformManager == null) yield break;

            for (int i = 0; i < _platformManager.PlatformCount; i++)
            {
                var spawnPoints = _platformManager.GetWhiteMinionSpawnPoints(i);
                foreach (var sp in spawnPoints)
                {
                    if (sp == null) continue;
                    // 플랫폼 중심을 향하는 방향으로 돌진하도록 방향 설정
                    int facing = sp.position.x > 0f ? -1 : 1;
                    SpawnMinion(_whiteMinionPrefab, sp.position, facing);
                }
            }

            yield return null;
        }

        // ══════════════════════════════════════════════════════════════════
        //  Phase 2 패턴 C — 플레이어 플랫폼에 검은 하수인 2마리 × 4회 반복
        // ══════════════════════════════════════════════════════════════════

        private IEnumerator Phase2_PatternC_BlackMinionsRepeat()
        {
            if (_blackMinionPrefab == null || _platformManager == null) yield break;

            for (int rep = 0; rep < _patternC_RepeatCount; rep++)
            {
                // 매 반복마다 플레이어의 현재 플랫폼을 재확인 (이동했을 수 있음)
                int playerPlatform = _platformManager.CurrentPlatformIndex;
                var spawnPoints    = _platformManager.GetBlackMinionSpawnPoints(playerPlatform);

                foreach (var sp in spawnPoints)
                {
                    if (sp == null) continue;
                    SpawnMinion(_blackMinionPrefab, sp.position, facingDir: -1);
                }

                if (rep < _patternC_RepeatCount - 1)
                    yield return new WaitForSeconds(_patternC_RepeatDelay);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  유틸 메서드
        // ══════════════════════════════════════════════════════════════════

        private void SpawnMinion(GameObject prefab, Vector3 position, int facingDir)
        {
            var go     = SpawnTracked(prefab, position, Quaternion.identity, _activeMinions);
            var minion = go.GetComponent<MinionBase>();
            minion?.InitializeMinion(facingDir);
        }

        /// <summary>오브젝트를 생성하고 추적 리스트에 등록한다.</summary>
        private GameObject SpawnTracked(GameObject prefab, Vector3 position, Quaternion rotation, List<GameObject> list)
        {
            var go = Instantiate(prefab, position, rotation);
            list.Add(go);
            return go;
        }

        private void StopLoop()
        {
            if (_loopHandle == null) return;
            StopCoroutine(_loopHandle);
            _loopHandle = null;
        }

        private void CleanupTrackedObjects()
        {
            DestroyList(_activeProjectiles);
            DestroyMinionList(_activeMinions);
            DestroyList(_activeDamageZones);
        }

        private void DestroyList(List<GameObject> list)
        {
            foreach (var go in list)
                if (go != null) Destroy(go);
            list.Clear();
        }

        private void DestroyMinionList(List<GameObject> list)
        {
            foreach (var go in list)
            {
                if (go == null) continue;
                var minion = go.GetComponent<MinionBase>();
                if (minion != null)
                    minion.ForceCleanup();
                else
                    Destroy(go);
            }
            list.Clear();
        }

        /// <summary>totalCount 개 중 lastIdx와 다른 인덱스를 반환한다.</summary>
        private static int SelectPattern(int totalCount, int lastIdx)
        {
            if (totalCount <= 1) return 0;
            int idx;
            int tries = 0;
            do { idx = Random.Range(0, totalCount); tries++; }
            while (idx == lastIdx && tries < 10);
            return idx;
        }

        // ── Gizmos ────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_projectileSpawnPoints != null)
            {
                Gizmos.color = Color.cyan;
                foreach (var sp in _projectileSpawnPoints)
                    if (sp != null) Gizmos.DrawWireSphere(sp.position, 0.15f);
            }

            // 패턴 2-3 오프셋 미리보기
            if (_pattern23Offsets != null && _player != null)
            {
                Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.5f);
                foreach (var off in _pattern23Offsets)
                    Gizmos.DrawSphere((Vector2)_player.position + off, 0.2f);
            }
        }
#endif
    }
}
