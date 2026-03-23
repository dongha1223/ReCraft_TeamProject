using UnityEngine;

namespace _2D_Roguelike
{
    public class EnemyStats : MonoBehaviour
    {
        [SerializeField] private float _maxHp = 50f;

        private float _currentHp;
        private Animator _animator;
        private EnemyController _controller;

        private static readonly int AnimDie = Animator.StringToHash("Die");

        public bool IsDead => _currentHp <= 0f;

        private void Awake()
        {
            _currentHp = _maxHp;
            _animator = GetComponent<Animator>();
            _controller = GetComponent<EnemyController>();
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            _currentHp = Mathf.Max(0f, _currentHp - amount);
            Debug.Log($"[EnemyStats] {name} HP: {_currentHp}/{_maxHp}");

            if (IsDead)
                OnDead();
        }

        private void OnDead()
        {
            Debug.Log($"[EnemyStats] {name} died.");
            if (_controller != null) _controller.enabled = false;
            _animator?.SetTrigger(AnimDie);

            // 사망 애니메이션 후 풀로 반환
            StartCoroutine(ReturnToPoolAfterDelay(1.5f));
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (EnemyPool.Instance != null)
                EnemyPool.Instance.Return(gameObject);
            else
                Destroy(gameObject);
        }

        // 풀에서 꺼낼 때 상태 초기화
        public void ResetStats()
        {
            _currentHp = _maxHp;
        }
    }
}
