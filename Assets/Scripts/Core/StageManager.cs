using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [SerializeField] private GameObject[] _stageRoots;   // Inspector 할당
        [SerializeField] private Transform[]  _spawnPoints;  // Inspector 할당
        [SerializeField] private Transform    _playerTransform;

        public int  CurrentStage    { get; private set; } = 0;
        public bool AllEnemiesDead  => _aliveEnemyCount == 0;
        public int  AliveEnemyCount => _aliveEnemyCount;

        private int _aliveEnemyCount;

        // 적이 전멸했을 때 발행 — SignpostController가 구독
        public event Action OnAllEnemiesDead;

        // ── 생명주기 ─────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ActivateStage(0);
        }

        // ── 적 카운트 관리 ───────────────────────────────────────────────
        public void RegisterEnemy() => _aliveEnemyCount++;

        /// <summary>
        /// 현재 스테이지에서 살아있는(활성화된) 적의 Transform 목록을 반환한다.
        /// WarriorTagTech3Behaviour 등 적 위치를 순회해야 하는 스킬에서 사용.
        /// </summary>
        public List<Transform> GetAliveEnemyTransforms()
        {
            var result = new List<Transform>();

            if (_stageRoots == null || CurrentStage >= _stageRoots.Length) return result;
            var root = _stageRoots[CurrentStage];
            if (root == null) return result;

            foreach (var e in root.GetComponentsInChildren<EnemyStats>(includeInactive: false))
                result.Add(e.transform);

            return result;
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
            if (next >= _stageRoots.Length) return;
            StartCoroutine(DoTransition(next));
        }

        public void TriggerGameClear() => StartCoroutine(DoGameClear());

        public void RestartGame() => StartCoroutine(DoRestart());

        // ── 내부 코루틴 ───────────────────────────────────────────────────
        private IEnumerator DoTransition(int nextIndex)
        {
            if (FadeManager.Instance != null)
                yield return StartCoroutine(FadeManager.Instance.FadeOut());

            _stageRoots[CurrentStage].SetActive(false);
            ActivateStage(nextIndex);

            if (FadeManager.Instance != null)
                yield return StartCoroutine(FadeManager.Instance.FadeIn());
        }

        private IEnumerator DoRestart()
        {
            if (FadeManager.Instance != null)
                yield return StartCoroutine(FadeManager.Instance.FadeOut());

            ResetPlayer();

            _stageRoots[CurrentStage].SetActive(false);
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
            _stageRoots[CurrentStage].SetActive(false);
            ActivateStage(0);

            yield return StartCoroutine(FadeManager.Instance.FadeIn());
        }

        // ── 핵심 스테이지 활성화 ──────────────────────────────────────────
        private void ActivateStage(int index)
        {
            CurrentStage     = index;
            _aliveEnemyCount = 0; // ★ SetActive 전 초기화 — EnemySpawner.OnEnable이 올바르게 카운트

            for (int i = 0; i < _stageRoots.Length; i++)
            {
                if (_stageRoots[i] != null)
                    _stageRoots[i].SetActive(i == index);
            }

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
            if (_playerTransform == null) return;
            _playerTransform.GetComponent<PlayerStats>()?.FullRestore();
            _playerTransform.GetComponent<FormSkillController>()?.ResetSkills();
            _playerTransform.GetComponent<PlayerDash>()?.ResetDash();
        }

        private void MovePlayerToSpawn(int stageIndex)
        {
            if (_playerTransform == null) return;
            if (_spawnPoints == null || stageIndex >= _spawnPoints.Length) return;
            if (_spawnPoints[stageIndex] == null) return;

            _playerTransform.position = _spawnPoints[stageIndex].position;

            var rb = _playerTransform.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }
}
