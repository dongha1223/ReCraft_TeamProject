using UnityEngine;
using UnityEngine.Pool;

namespace _2D_Roguelike
{
    /// <summary>
    /// 피격이펙트 오브젝트 풀 관리 싱글턴.
    /// 씬에 하나의 GameObject에 부착해 사용.
    /// </summary>
    public class HitEffectSpawner : MonoBehaviour
    {
        public static HitEffectSpawner Instance { get; private set; }

        [SerializeField] private HitEffectActor _prefab;
        [SerializeField] private int            _defaultPoolSize = 10;
        [SerializeField] private int            _maxPoolSize     = 20;

        private ObjectPool<HitEffectActor> _pool;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _pool = new ObjectPool<HitEffectActor>(
                createFunc:      () => Instantiate(_prefab),
                actionOnGet:     e  => e.OnGetFromPool(),
                actionOnRelease: e  => e.OnReturnToPool(),
                actionOnDestroy: e  => Destroy(e.gameObject),
                collectionCheck: false,
                defaultCapacity: _defaultPoolSize,
                maxSize:         _maxPoolSize
            );
        }

        public void Spawn(Vector3 worldPos)
        {
            var effect = _pool.Get();
            effect.transform.position = worldPos;
            effect.Play(Return);
        }

        private void Return(HitEffectActor effect) => _pool.Release(effect);
    }
}
