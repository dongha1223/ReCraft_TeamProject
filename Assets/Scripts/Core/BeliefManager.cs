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

        public void AddBelief(int amount)
        {
            if (amount <= 0) return;
            _totalBelief += amount;
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
