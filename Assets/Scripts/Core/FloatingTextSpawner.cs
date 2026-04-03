using UnityEngine;
using UnityEngine.Pool;

namespace _2D_Roguelike
{
    public enum FloatingTextType
    {
        Damage,       // 빨강
        Heal,         // 초록
        StatusEffect  // 보라 (향후 확장용)
    }

    /// <summary>
    /// FloatingText 오브젝트 풀 관리 싱글턴
    /// 씬에 하나의 GameObject에 부착해 사용
    /// </summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        public static FloatingTextSpawner Instance { get; private set; }

        [SerializeField] private FloatingText _prefab;
        [SerializeField] private int          _defaultPoolSize = 15;
        [SerializeField] private int          _maxPoolSize     = 30;

        private ObjectPool<FloatingText> _pool;

        // ── 타입별 색상 테이블 ────────────────────────────────────────
        private static readonly Color ColorDamage      = new Color(1.00f, 0.20f, 0.20f);
        private static readonly Color ColorHeal        = new Color(0.20f, 1.00f, 0.30f);
        private static readonly Color ColorStatusEffect = new Color(0.80f, 0.30f, 1.00f);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _pool = new ObjectPool<FloatingText>(
                createFunc:      () => Instantiate(_prefab),
                actionOnGet:     t  => t.OnGetFromPool(),
                actionOnRelease: t  => t.OnReturnToPool(),
                actionOnDestroy: t  => Destroy(t.gameObject),
                collectionCheck: false,
                defaultCapacity: _defaultPoolSize,
                maxSize:         _maxPoolSize
            );
        }

        /// <summary>
        /// 지정 위치에 플로팅 텍스트 생성
        /// </summary>
        /// <param name="worldPos">월드 스폰 위치</param>
        /// <param name="text">표시할 문자열</param>
        /// <param name="type">텍스트 타입 (색상 결정)</param>
        public void Spawn(Vector3 worldPos, string text, FloatingTextType type)
        {
            var ft = _pool.Get();
            ft.transform.position = worldPos;
            ft.Init(text, ResolveColor(type), Return);
        }

        /// <summary>FloatingText에서 수명 만료 시 콜백으로 호출</summary>
        private void Return(FloatingText ft) => _pool.Release(ft);

        private static Color ResolveColor(FloatingTextType type) => type switch
        {
            FloatingTextType.Heal         => ColorHeal,
            FloatingTextType.StatusEffect => ColorStatusEffect,
            _                             => ColorDamage,
        };
    }
}
