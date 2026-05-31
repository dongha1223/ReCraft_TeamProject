using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 스테이지 클리어(적 전멸) 시 골드 상자를 스폰한다.
    /// 스폰된 상자는 이 오브젝트의 자식으로 생성되므로
    /// 스테이지 루트가 비활성화되면 함께 사라진다.
    /// </summary>
    public class GoldChestSpawner : MonoBehaviour
    {
        [Header("프리팹")]
        [SerializeField] private GameObject _goldChestPrefab;

        [Header("스폰 설정")]
        [Tooltip("비워두면 이 오브젝트 위치에 스폰")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private int _goldAmount = 30;

        private bool       _spawned;
        private GameObject _spawnedChest;

        private void OnEnable()
        {
            _spawned = false;

            // 재활성화 시 상자를 숨겨두고 적 전멸 이벤트 재구독
            if (_spawnedChest != null)
                _spawnedChest.SetActive(false);

            if (StageManager.Instance != null)
            {
                StageManager.Instance.OnAllEnemiesDead -= HandleAllEnemiesDead;
                StageManager.Instance.OnAllEnemiesDead += HandleAllEnemiesDead;
            }
        }

        private void OnDisable()
        {
            if (StageManager.Instance != null)
                StageManager.Instance.OnAllEnemiesDead -= HandleAllEnemiesDead;
        }

        private void Start()
        {
            // 최초 1회만 생성, 이후 SetActive로 재사용
            if (_goldChestPrefab != null)
            {
                Vector3 pos   = _spawnPoint != null ? _spawnPoint.position : transform.position;
                _spawnedChest = Instantiate(_goldChestPrefab, pos, Quaternion.identity, transform);
                _spawnedChest.SetActive(false);
            }

            if (StageManager.Instance == null) return;
            StageManager.Instance.OnAllEnemiesDead -= HandleAllEnemiesDead;
            StageManager.Instance.OnAllEnemiesDead += HandleAllEnemiesDead;

            if (!_spawned && StageManager.Instance.AllEnemiesDead)
                HandleAllEnemiesDead();
        }

        private void HandleAllEnemiesDead()
        {
            if (_spawned) return;
            _spawned = true;
            ShowChest();
        }

        private void ShowChest()
        {
            if (_spawnedChest == null) return;
            _spawnedChest.GetComponent<GoldChest>()?.Init(_goldAmount);
            _spawnedChest.SetActive(true);
        }
    }
}
