using System;
using UnityEngine;

namespace _2D_Roguelike
{
    public class BeliefManager : MonoBehaviour
    {
        public static BeliefManager Instance { get; private set; }

        private const string _saveKey = "Belief";

        public event Action OnBeliefChanged;

        [SerializeField] private int _totalBelief;

        private int _monsterStageClearCount;
        private StageManager _stageManager;

        public int TotalBelief => _totalBelief;

        /// <summary>ExpBonus 배율 읽기용 — 플레이어 StatService 참조 (런타임에 캐싱)</summary>
        private StatService _playerStatService;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _totalBelief = PlayerPrefs.GetInt(_saveKey, 0);
        }

        private void Start()
        {
            _stageManager = StageManager.Instance;
            if (_stageManager == null) return;
            _stageManager.OnNormalStageCleared += HandleNormalStageCleared;
            _stageManager.OnRunRestarted       += HandleRunRestarted;
        }

        private void OnDestroy()
        {
            if (_stageManager == null) return;
            _stageManager.OnNormalStageCleared -= HandleNormalStageCleared;
            _stageManager.OnRunRestarted       -= HandleRunRestarted;
        }

        private void OnValidate()
        {
            _totalBelief = Mathf.Max(0, _totalBelief);
            if (Application.isPlaying)
                OnBeliefChanged?.Invoke();
        }

        private void HandleNormalStageCleared()
        {
            _monsterStageClearCount++;
            AddBelief(_monsterStageClearCount);
        }

        private void HandleRunRestarted()
        {
            _monsterStageClearCount = 0;
        }

        /// <summary>
        /// 플레이어 StatService를 등록한다.
        /// PlayerStatController.Start()에서 호출하거나, 첫 AddBelief 시 자동 탐색한다.
        /// </summary>
        public void RegisterPlayerStatService(StatService statService)
        {
            _playerStatService = statService;
        }

        public void AddBelief(int amount)
        {
            if (amount <= 0) return;

            // ExpBonus 배율 적용 (기본 1.0)
            if (_playerStatService == null)
            {
                // 미등록 시 플레이어에서 자동 탐색 (1회)
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    _playerStatService = player.GetComponent<PlayerStatController>()?.StatService;
            }

            float expMult = _playerStatService != null
                ? _playerStatService.GetFinalValue(StatType.ExpBonus)
                : 1f;

            int finalAmount = Mathf.RoundToInt(amount * Mathf.Max(expMult, 0.01f));
            _totalBelief += finalAmount;
            Save();
        }

        public bool TrySpendBelief(int amount)
        {
            if (amount <= 0 || _totalBelief < amount) return false;
            _totalBelief -= amount;
            Save();
            return true;
        }

        private void Save()
        {
            PlayerPrefs.SetInt(_saveKey, _totalBelief);
            PlayerPrefs.Save();
            OnBeliefChanged?.Invoke();
        }
    }
}
