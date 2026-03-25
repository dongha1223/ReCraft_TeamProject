using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// A/S 스킬 오브젝트 풀
    /// - SwordEnergyProjectile : A 스킬 투사체
    /// - RollingSlashVisual    : S 스킬 이펙트
    /// </summary>
    public class SkillObjectPool : MonoBehaviour
    {
        public static SkillObjectPool Instance { get; private set; }

        [SerializeField] private int _initialProjectileCount = 4;
        [SerializeField] private int _initialSlashVFXCount   = 5;

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
            var go = new GameObject("SwordEnergy_Pooled");
            go.transform.SetParent(transform);
            // RequireComponent 가 MeshFilter / MeshRenderer 를 자동으로 추가
            var p = go.AddComponent<SwordEnergyProjectile>();
            go.SetActive(false);
            return p;
        }

        /// <summary>풀에서 투사체를 꺼내 초기화 후 반환</summary>
        public SwordEnergyProjectile GetProjectile(bool facingRight, Vector2 pos)
        {
            var p = _projectilePool.Count > 0 ? _projectilePool.Dequeue() : CreateProjectile();
            p.transform.SetParent(null);
            p.transform.position = pos;
            p.gameObject.SetActive(true);
            p.Setup(facingRight);
            return p;
        }

        /// <summary>투사체를 비활성화하고 풀로 반환</summary>
        public void ReturnProjectile(SwordEnergyProjectile p)
        {
            p.gameObject.SetActive(false);
            p.transform.SetParent(transform);
            _projectilePool.Enqueue(p);
        }

        // ── RollingSlashVisual ────────────────────────────────────────────
        private RollingSlashVisual CreateSlashVFX()
        {
            var go = new GameObject("RollingSlash_Pooled");
            go.transform.SetParent(transform);
            // RequireComponent 가 MeshFilter / MeshRenderer 를 자동으로 추가
            var v = go.AddComponent<RollingSlashVisual>();
            go.SetActive(false);
            return v;
        }

        /// <summary>풀에서 슬래쉬 VFX를 꺼내 위치 설정 후 반환</summary>
        public RollingSlashVisual GetSlashVFX(Vector2 pos)
        {
            var v = _slashVFXPool.Count > 0 ? _slashVFXPool.Dequeue() : CreateSlashVFX();
            v.transform.SetParent(null);
            v.transform.position = pos;
            v.gameObject.SetActive(true);
            return v;
        }

        /// <summary>슬래쉬 VFX를 비활성화하고 풀로 반환</summary>
        public void ReturnSlashVFX(RollingSlashVisual v)
        {
            v.gameObject.SetActive(false);
            v.transform.SetParent(transform);
            _slashVFXPool.Enqueue(v);
        }
    }
}
