using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// A/S 스킬 오브젝트 풀.
    /// - SwordEnergyProjectile: 프리팹 기반 풀링 (_projectilePrefab Inspector 할당 필수)
    /// - RollingSlashVisual   : 프리팹 기반 풀링 (_slashVFXPrefab Inspector 할당 필수)
    /// </summary>
    public class SkillObjectPool : MonoBehaviour
    {
        public static SkillObjectPool Instance { get; private set; }

        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private GameObject _slashVFXPrefab;
        [SerializeField] private int        _initialProjectileCount = 4;
        [SerializeField] private int        _initialSlashVFXCount   = 5;

        private readonly Queue<SwordEnergyProjectile> _projectilePool = new Queue<SwordEnergyProjectile>();
        private readonly Queue<RollingSlashVisual>     _slashVFXPool   = new Queue<RollingSlashVisual>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            for (int i = 0; i < _initialProjectileCount; i++)
                _projectilePool.Enqueue(CreateProjectile());

            for (int i = 0; i < _initialSlashVFXCount; i++)
                _slashVFXPool.Enqueue(CreateSlashVFX());
        }

        // ── SwordEnergyProjectile ─────────────────────────────────────────
        private SwordEnergyProjectile CreateProjectile()
        {
            if (_projectilePrefab == null)
            {
                Debug.LogError("[SkillObjectPool] _projectilePrefab이 할당되지 않았습니다.");
                return null;
            }
            var go = Instantiate(_projectilePrefab, transform);
            go.SetActive(false);
            return go.GetComponent<SwordEnergyProjectile>();
        }

        public SwordEnergyProjectile GetProjectile(Vector2 pos)
        {
            var p = (_projectilePool.Count > 0) ? _projectilePool.Dequeue() : CreateProjectile();
            if (p == null) return null;

            p.transform.SetParent(null);
            p.transform.position = pos;
            p.gameObject.SetActive(true);
            return p;
        }

        public void ReturnProjectile(SwordEnergyProjectile p)
        {
            p.gameObject.SetActive(false);
            p.transform.SetParent(transform);
            _projectilePool.Enqueue(p);
        }

        // ── RollingSlashVisual ────────────────────────────────────────────
        private RollingSlashVisual CreateSlashVFX()
        {
            if (_slashVFXPrefab == null)
            {
                Debug.LogError("[SkillObjectPool] _slashVFXPrefab이 할당되지 않았습니다.");
                return null;
            }
            var go = Instantiate(_slashVFXPrefab, transform);
            go.SetActive(false);
            return go.GetComponent<RollingSlashVisual>();
        }

        public RollingSlashVisual GetSlashVFX(Vector2 pos)
        {
            var v = (_slashVFXPool.Count > 0) ? _slashVFXPool.Dequeue() : CreateSlashVFX();
            if (v == null) return null;

            v.transform.SetParent(null);
            v.transform.position = pos;
            v.gameObject.SetActive(true);
            return v;
        }

        public void ReturnSlashVFX(RollingSlashVisual v)
        {
            v.gameObject.SetActive(false);
            v.transform.SetParent(transform);
            _slashVFXPool.Enqueue(v);
        }
    }
}
