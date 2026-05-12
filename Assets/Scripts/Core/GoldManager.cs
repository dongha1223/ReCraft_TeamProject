using System;
using UnityEngine;

namespace _2D_Roguelike
{
    public class GoldManager : MonoBehaviour
    {
        public static GoldManager Instance { get; private set; }

        public event Action OnGoldChanged;

        private int _gold;
        private StageManager _stageManager;

        public int Gold => _gold;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _stageManager = StageManager.Instance;
            if (_stageManager == null) return;
            _stageManager.OnRunRestarted += ResetGold;
        }

        private void OnDestroy()
        {
            if (_stageManager != null)
                _stageManager.OnRunRestarted -= ResetGold;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            _gold += amount;
            OnGoldChanged?.Invoke();
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0 || _gold < amount) return false;
            _gold -= amount;
            OnGoldChanged?.Invoke();
            return true;
        }

        private void ResetGold()
        {
            _gold = 0;
            OnGoldChanged?.Invoke();
        }
    }
}
