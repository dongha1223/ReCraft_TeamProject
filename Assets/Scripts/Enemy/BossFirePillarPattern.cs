using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 보스 불기둥 패턴.
    /// BossPattern.patternName = "FirePillar" 로 등록하면 BossMainController 가 실행한다.
    ///
    /// 동작 순서:
    ///   1) Boss_Head SpriteRenderer 를 붉게 물들임 (눈 빛나기)
    ///   2) 투명벽 범위 안에 중복되지 않는 X 위치 생성
    ///   3) _spawnInterval 간격으로 불기둥 순서 소환 (FirePillarActor)
    ///      - 각 기둥은 3초 경고(크레이터 페이드인) 후 불꽃 애니메이션 재생
    ///   4) 모든 기둥이 완료될 때까지 대기
    ///   5) Boss_Head 색상 복원
    /// </summary>
    public class BossFirePillarPattern : BossCustomPatternBase
    {
        public override string PatternId => "FirePillar";

        [Header("데이터 SO")]
        [SerializeField] private BossFirePillarDataSO _data;

        [Header("참조")]
        [Tooltip("Boss_Head SpriteRenderer — 눈 붉게 빛내기용")]
        [SerializeField] private SpriteRenderer _headRenderer;

        [Tooltip("왼쪽 투명벽 Transform — X 범위 계산용")]
        [SerializeField] private Transform _wallLeft;

        [Tooltip("오른쪽 투명벽 Transform — X 범위 계산용")]
        [SerializeField] private Transform _wallRight;

        [Tooltip("불기둥이 솟아오르는 바닥 Y 좌표 (월드 기준)")]
        [SerializeField] private float _spawnY = -3f;

        [Header("불기둥 스프라이트")]
        [Tooltip("경고(크레이터) 단계에 표시할 스프라이트 (보스 불기등_1 권장)")]
        [SerializeField] private Sprite _craterSprite;

        [Tooltip("불꽃 애니메이션 프레임 배열 (보스 불기등_2~_36)")]
        [SerializeField] private Sprite[] _fireSprites;

        [Tooltip("불기둥 Actor 프리팹 (FirePillarActor 컴포넌트 포함)")]
        [SerializeField] private GameObject _pillarPrefab;

        [Header("패턴 설정")]
        [Tooltip("동시에 관리할 불기둥 최대 수 (= 풀 크기)")]
        [SerializeField] private int _pillarCount = 10;

        [Tooltip("각 불기둥 소환 간격 (초)")]
        [SerializeField] private float _spawnInterval = 0.3f;

        [Tooltip("인접 불기둥 간 최소 거리 (유닛)")]
        [SerializeField] private float _minSpacing = 1.5f;

        [Header("불기둥 크기")]
        [Tooltip("불기둥 폭 (X 스케일)")]
        [SerializeField] private float _pillarWidth  = 1f;

        [Tooltip("불기둥 높이 (Y 스케일)")]
        [SerializeField] private float _pillarHeight = 1f;

        [Header("눈 빛나기")]
        [SerializeField] private Color _glowColor = Color.red;

        // ── 내부 ─────────────────────────────────────────────────────────────

        private List<FirePillarActor> _pool;
        private Color                 _originalHeadColor;

        // ── 생명주기 ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_data != null)
            {
                _spawnY        = _data.SpawnY;
                _pillarCount   = _data.PillarCount;
                _spawnInterval = _data.SpawnInterval;
                _minSpacing    = _data.MinSpacing;
                _pillarWidth   = _data.PillarWidth;
                _pillarHeight  = _data.PillarHeight;
                _glowColor     = _data.GlowColor;
                if (_data.PillarPrefab  != null) _pillarPrefab  = _data.PillarPrefab;
                if (_data.CraterSprite  != null) _craterSprite  = _data.CraterSprite;
                if (_data.FireSprites   != null && _data.FireSprites.Length > 0) _fireSprites = _data.FireSprites;
            }
            // Cancel() 호출 시 _originalHeadColor가 default(Color(0,0,0,0))로 남아
            // 머리가 투명해지는 버그 방지 — 패턴 실행 전에 미리 캐시
            if (_headRenderer != null)
                _originalHeadColor = _headRenderer.color;

            BuildPool();
        }

        private void BuildPool()
        {
            if (_pillarPrefab == null) return;

            _pool = new List<FirePillarActor>(_pillarCount);
            var scale = new Vector3(_pillarWidth, _pillarHeight, 1f);
            for (int i = 0; i < _pillarCount; i++)
            {
                var go    = Instantiate(_pillarPrefab, transform);
                go.transform.localScale = scale;
                go.SetActive(false);
                var actor = go.GetComponent<FirePillarActor>();
                actor.Setup(_craterSprite, _fireSprites);
                _pool.Add(actor);
            }
        }

        // ── BossCustomPatternBase ─────────────────────────────────────────────

        protected override IEnumerator ExecuteRoutine()
        {
            if (_pool == null || _pool.Count == 0)
            {
                Debug.LogWarning("[FirePillar] 풀이 비어있음 — _pillarPrefab 확인 필요");
                yield break;
            }

            // ① 눈 붉게
            if (_headRenderer != null)
            {
                _originalHeadColor  = _headRenderer.color;
                _headRenderer.color = _glowColor;
            }

            // ② 비중복 위치 생성 후 순서대로 소환
            var positions = GeneratePositions();
            int poolIdx   = 0;
            foreach (var pos in positions)
            {
                if (poolIdx >= _pool.Count) break;
                _pool[poolIdx++].Launch(pos);
                yield return StartCoroutine(PauseableWait(_spawnInterval));
            }

            // ③ 전부 완료 대기
            while (!AllDone()) yield return null;

            // ④ 눈 색 복원
            if (_headRenderer != null)
                _headRenderer.color = _originalHeadColor;
        }

        protected override void OnCancel()
        {
            if (_headRenderer != null)
                _headRenderer.color = _originalHeadColor;

            if (_pool == null) return;
            foreach (var actor in _pool)
                if (actor != null && actor.gameObject.activeSelf)
                    actor.gameObject.SetActive(false);
        }

        // ── 내부 유틸 ─────────────────────────────────────────────────────────

        private List<Vector3> GeneratePositions()
        {
            float leftX  = _wallLeft  != null ? _wallLeft.position.x  : -10f;
            float rightX = _wallRight != null ? _wallRight.position.x :  10f;

            var result = new List<Vector3>(_pillarCount);
            const int maxAttempts = 200;

            for (int i = 0; i < _pillarCount; i++)
            {
                float x;
                bool  valid;
                int   attempts = 0;

                do
                {
                    x     = Random.Range(leftX, rightX);
                    valid = true;
                    for (int j = 0; j < result.Count; j++)
                    {
                        if (Mathf.Abs(result[j].x - x) < _minSpacing) { valid = false; break; }
                    }
                    attempts++;
                }
                while (!valid && attempts < maxAttempts);

                if (valid)
                    result.Add(new Vector3(x, _spawnY, 0f));
            }

            return result;
        }

        private bool AllDone()
        {
            if (_pool == null) return true;
            foreach (var actor in _pool)
                if (actor != null && actor.gameObject.activeSelf) return false;
            return true;
        }
    }
}
