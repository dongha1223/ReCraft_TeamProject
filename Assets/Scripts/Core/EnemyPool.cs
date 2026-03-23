using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    public class EnemyPool : MonoBehaviour
    {
        public static EnemyPool Instance { get; private set; }

        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private int _initialPoolSize = 5;

        private readonly Queue<GameObject> _pool = new Queue<GameObject>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            // 초기 풀 생성
            for (int i = 0; i < _initialPoolSize; i++)
                _pool.Enqueue(CreateInstance());
        }

        private GameObject CreateInstance()
        {
            var obj = Instantiate(_enemyPrefab, transform);
            obj.SetActive(false);
            return obj;
        }

        // 풀에서 적을 꺼내 지정 위치에 활성화
        public GameObject Get(Vector3 position)
        {
            var obj = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();
            obj.transform.position = position;
            obj.SetActive(true);
            return obj;
        }

        // 적을 비활성화하고 풀로 반환
        public void Return(GameObject obj)
        {
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
    }
}
