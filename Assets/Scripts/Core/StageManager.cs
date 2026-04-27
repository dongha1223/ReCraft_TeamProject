using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        // ── 스테이지 엔트리 ───────────────────────────────────────────────
        [System.Serializable]
        private struct StageEntry
        {
            [Tooltip("스테이지 데이터 SO")]
            public StageDataSO data;

            [Tooltip("씬 내 스테이지 루트 오브젝트")]
            public GameObject root;

            [Tooltip("플레이어 스폰 위치")]
            public Transform spawnPoint;
        }

        [Header("스테이지 목록 (배열 순서 = 게임 진행 순서)")]
        [SerializeField] private StageEntry[] _stages;

        [Header("플레이어")]
        [SerializeField] private Transform _playerTransform;

        public int  CurrentStage    { get; private set; } = 0;
        public bool AllEnemiesDead  => _aliveEnemyCount == 0;
        public int  AliveEnemyCount => _aliveEnemyCount;

        /// <summary>현재 스테이지의 StageDataSO. 없으면 null.</summary>
        public StageDataSO CurrentStageData =>
            (_stages != null && CurrentStage < _stages.Length) ? _stages[CurrentStage].data : null;

        private int _aliveEnemyCount;

        // 적이 전멸했을 때 발행 — SignpostController가 구독
        public event Action OnAllEnemiesDead;

        // ── 캐시 ─────────────────────────────────────────────────────────
        private PlayerStats          _playerStats;
        private FormSkillController  _formSkillController;
        private PlayerDash           _playerDash;
        private Rigidbody2D          _playerRb;
        private SignpostController[] _signpostCache; // 인덱스 = 스테이지 인덱스

        // GetAliveEnemyTransforms 재사용 버퍼
        private readonly List<Transform> _aliveEnemyBuffer = new List<Transform>();

        // ── 생명주기 ─────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            CachePlayerComponents();
            CacheSignposts();
        }

        private void Start()
        {
            ActivateStage(0);
        }

        // ── 캐싱 ─────────────────────────────────────────────────────────
        private void CachePlayerComponents()
        {
            if (_playerTransform == null) return;
            _playerStats         = _playerTransform.GetComponent<PlayerStats>();
            _formSkillController = _playerTransform.GetComponent<FormSkillController>();
            _playerDash          = _playerTransform.GetComponent<PlayerDash>();
            _playerRb            = _playerTransform.GetComponent<Rigidbody2D>();
        }

        private void CacheSignposts()
        {
            if (_stages == null) return;
            _signpostCache = new SignpostController[_stages.Length];
            for (int i = 0; i < _stages.Length; i++)
            {
                if (_stages[i].root != null)
                    _signpostCache[i] = _stages[i].root.GetComponentInChildren<SignpostController>(includeInactive: true);
            }
        }

        // ── 적 카운트 관리 ───────────────────────────────────────────────
        public void RegisterEnemy() => _aliveEnemyCount++;

        /// <summary>
        /// 현재 스테이지에서 살아있는(활성화된) 적의 Transform 목록을 반환한다.
        /// WarriorTagTech3Behaviour 등 적 위치를 순회해야 하는 스킬에서 사용.
        /// 내부 버퍼를 재사용하므로 반환된 리스트를 캐싱하지 말 것.
        /// </summary>
        public List<Transform> GetAliveEnemyTransforms()
        {
            _aliveEnemyBuffer.Clear();

            if (_stages == null || CurrentStage >= _stages.Length) return _aliveEnemyBuffer;
            var root = _stages[CurrentStage].root;
            if (root == null) return _aliveEnemyBuffer;

            foreach (var e in root.GetComponentsInChildren<EnemyStats>(includeInactive: false))
                _aliveEnemyBuffer.Add(e.transform);

            return _aliveEnemyBuffer;
        }

        public void OnEnemyDied()
        {
            _aliveEnemyCount = Mathf.Max(0, _aliveEnemyCount - 1);
            if (_aliveEnemyCount == 0)
                OnAllEnemiesDead?.Invoke();
        }

        // ── 스테이지 전환 ─────────────────────────────────────────────────
        public void TransitionToNextStage()
        {
            int next = CurrentStage + 1;
            if (next >= _stages.Length) return;
            StartCoroutine(DoTransition(next));
        }

        public void TriggerGameClear() => StartCoroutine(DoGameClear());

        public void RestartGame() => StartCoroutine(DoRestart());

        // ── 내부 코루틴 ───────────────────────────────────────────────────
        private IEnumerator DoTransition(int nextIndex)
        {
            if (FadeManager.Instance != null)
                yield return StartCoroutine(FadeManager.Instance.FadeOut());

            _stages[CurrentStage].root.SetActive(false);
            ActivateStage(nextIndex);

            if (FadeManager.Instance != null)
                yield return StartCoroutine(FadeManager.Instance.FadeIn());
        }

        private IEnumerator DoRestart()
        {
            if (FadeManager.Instance != null)
                yield return StartCoroutine(FadeManager.Instance.FadeOut());

            ResetPlayer();

            _stages[CurrentStage].root.SetActive(false);
            ActivateStage(0);

            if (FadeManager.Instance != null)
                yield return StartCoroutine(FadeManager.Instance.FadeIn());
        }

        private IEnumerator DoGameClear()
        {
            if (FadeManager.Instance != null)
                yield return StartCoroutine(FadeManager.Instance.FadeOut());
            FadeManager.Instance?.ShowGameClear(true);
            if (FadeManager.Instance != null)
                yield return StartCoroutine(FadeManager.Instance.FadeIn());

            for (int i = 10; i >= 0; i--)
            {
                FadeManager.Instance.SetCountdown(i);
                yield return new WaitForSeconds(1f);
            }

            yield return StartCoroutine(FadeManager.Instance.FadeOut());
            FadeManager.Instance.ShowGameClear(false);

            ResetPlayer();
            _stages[CurrentStage].root.SetActive(false);
            ActivateStage(0);

            yield return StartCoroutine(FadeManager.Instance.FadeIn());
        }

        // ── 핵심 스테이지 활성화 ──────────────────────────────────────────
        private void ActivateStage(int index)
        {
            CurrentStage     = index;
            _aliveEnemyCount = 0; // ★ SetActive 전 초기화 — EnemySpawner.OnEnable이 올바르게 카운트

            for (int i = 0; i < _stages.Length; i++)
            {
                if (_stages[i].root != null)
                    _stages[i].root.SetActive(i == index);
            }

            // 마지막 스테이지 여부를 캐시된 표지판에 자동 주입
            bool isLast = (index == _stages.Length - 1);
            if (_signpostCache != null && index < _signpostCache.Length && _signpostCache[index] != null)
                _signpostCache[index].SetIsLastStage(isLast);

            MovePlayerToSpawn(index);

            // SetActive로 발생한 모든 OnEnable이 완료된 다음 프레임에 검사
            // → EnemySpawner.OnEnable이 먼저 실행돼 카운트가 채워진 뒤 판단
            StartCoroutine(CheckAllEnemiesDeadNextFrame());
        }

        private IEnumerator CheckAllEnemiesDeadNextFrame()
        {
            yield return null;
            if (AllEnemiesDead)
                OnAllEnemiesDead?.Invoke();
        }

        private void ResetPlayer()
        {
            _playerStats?.FullRestore();
            _formSkillController?.ResetSkills();
            _playerDash?.ResetDash();
        }

        private void MovePlayerToSpawn(int stageIndex)
        {
            if (_playerTransform == null) return;
            if (_stages == null || stageIndex >= _stages.Length) return;
            if (_stages[stageIndex].spawnPoint == null) return;

            _playerTransform.position = _stages[stageIndex].spawnPoint.position;

            if (_playerRb != null) _playerRb.linearVelocity = Vector2.zero;
        }
    }
}
