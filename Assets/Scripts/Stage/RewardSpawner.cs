using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 스테이지 클리어(적 전멸) 시 포탈 옆에 보상 아이템을 스폰한다.
    /// SignpostController와 같이 OnAllEnemiesDead 이벤트를 구독한다.
    /// </summary>
    public class RewardSpawner : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private ItemDatabaseSO _itemDatabase;
        [SerializeField] private StageDataSO    _stageData;

        [Header("스폰 설정")]
        [SerializeField] private GameObject  _itemPickupPrefab;

        [Tooltip("비워두면 '##--REWARD_SPAWNS--##' 자식에서 자동 탐색")]
        [SerializeField] private Transform[] _spawnPoints;

        private bool _spawned = false;

        // ── 생명주기 ─────────────────────────────────────────────────

        private void OnEnable()
        {
            _spawned = false;

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
            if (StageManager.Instance == null) return;

            StageManager.Instance.OnAllEnemiesDead -= HandleAllEnemiesDead;
            StageManager.Instance.OnAllEnemiesDead += HandleAllEnemiesDead;

            // 이미 전멸 상태인 스테이지 대응 (Start/Shop 등)
            if (!_spawned && StageManager.Instance.AllEnemiesDead)
                HandleAllEnemiesDead();
        }

        // ── 내부 ─────────────────────────────────────────────────────

        private void HandleAllEnemiesDead()
        {
            if (_spawned) return;
            _spawned = true;
            SpawnRewards();
        }

        private Transform[] ResolveSpawnPoints()
        {
            // null이 아닌 요소가 하나라도 있으면 그대로 사용
            if (_spawnPoints != null)
            {
                foreach (var sp in _spawnPoints)
                    if (sp != null) return _spawnPoints;
            }

            // 전부 null이거나 비어있으면 ##--REWARD_SPAWNS--## 자식에서 자동 탐색
            Transform container = transform.Find("##--REWARD_SPAWNS--##");
            if (container == null) return null;

            var points = new Transform[container.childCount];
            for (int i = 0; i < container.childCount; i++)
                points[i] = container.GetChild(i);
            return points;
        }

        private void SpawnRewards()
        {
            if (_stageData == null || _itemDatabase == null || _itemPickupPrefab == null) return;

            Transform[] spawnPoints = ResolveSpawnPoints();
            if (spawnPoints == null || spawnPoints.Length == 0) return;

            int count = Mathf.Min(_stageData.reward.itemChoiceCount, spawnPoints.Length);
            if (count <= 0) return;

            List<ItemDefinition> drops = DropSystem.RollDrops(_itemDatabase, _stageData.mapTheme, count);

            for (int i = 0; i < drops.Count; i++)
            {
                if (spawnPoints[i] == null) continue;

                GameObject go = Instantiate(_itemPickupPrefab, spawnPoints[i].position, Quaternion.identity);
                var pickup = go.GetComponent<ItemPickup>();
                pickup?.Init(drops[i]);
            }
        }
    }
}
