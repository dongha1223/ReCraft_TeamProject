using UnityEngine;

namespace _2D_Roguelike
{
    public class GoldChest : MonoBehaviour, IInteractable
    {
        [Header("보상")]
        [SerializeField] private int _goldAmount = 50;
        [SerializeField] private int _coinCount  = 5;

        private Animator _animator;
        private bool     _opened;

        private static readonly int AnimOpen = Animator.StringToHash("Open");

        public bool CanInteract => !_opened;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            _opened = false;
            if (_animator != null)
            {
                _animator.Rebind();
                _animator.Update(0f);
            }
        }

        public void Init(int goldAmount, int coinCount = 5)
        {
            _goldAmount = goldAmount;
            _coinCount  = coinCount;
        }

        public void OnFocused()   { }
        public void OnUnfocused() { }

        public void OnInteract(PlayerStatController statController)
        {
            if (_opened) return;
            _opened = true;
            _animator.SetTrigger(AnimOpen);
            SpawnCoinEffects();
        }

        private void SpawnCoinEffects()
        {
            var pool = CoinPool.Instance;
            if (pool == null || _goldAmount <= 0) return;

            int     count       = Mathf.Min(_coinCount, _goldAmount);
            int     goldPerCoin = _goldAmount / count;
            int     remainder   = _goldAmount % count;
            Vector3 spawnPos    = transform.position;
            Vector3 target      = PlayerHPUI.GoldIconWorldPos;

            for (int i = 0; i < count; i++)
            {
                int coinGold = goldPerCoin + (i == count - 1 ? remainder : 0);
                pool.Get().Play(spawnPos, target, coinGold);
            }
        }
    }
}
