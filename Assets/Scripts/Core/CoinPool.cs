using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace _2D_Roguelike
{
    public class CoinPool : MonoBehaviour
    {
        public static CoinPool Instance { get; private set; }

        [Header("풀 설정")]
        [SerializeField] private CoinFlyEffect _coinPrefab;
        [SerializeField] private int           _preWarmCount = 10;
        [SerializeField] private int           _maxSize      = 30;

        private ObjectPool<CoinFlyEffect> _pool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _pool = new ObjectPool<CoinFlyEffect>(
                createFunc:      ()   => Instantiate(_coinPrefab, GoldManager.Instance.transform),
                actionOnGet:     coin => coin.gameObject.SetActive(true),
                actionOnRelease: coin =>
                {
                    coin.transform.localScale = Vector3.one;
                    coin.gameObject.SetActive(false);
                },
                actionOnDestroy: coin => Destroy(coin.gameObject),
                collectionCheck: false,
                defaultCapacity: _preWarmCount,
                maxSize:         _maxSize
            );
        }

        private void Start()
        {
            PreWarm();
        }

        private void PreWarm()
        {
            var stack = new Stack<CoinFlyEffect>(_preWarmCount);
            for (int i = 0; i < _preWarmCount; i++)
                stack.Push(_pool.Get());
            while (stack.Count > 0)
                _pool.Release(stack.Pop());
        }

        public CoinFlyEffect Get()             => _pool.Get();
        public void          Release(CoinFlyEffect coin) => _pool.Release(coin);
    }
}
