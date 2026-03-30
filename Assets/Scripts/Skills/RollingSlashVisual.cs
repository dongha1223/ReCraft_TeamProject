using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 롤링 슬레쉬 참격 이펙트
    /// - 프리팹에 미리 구성된 ParticleSystem을 재생
    /// - SkillObjectPool 기반 풀링: 재생 완료 후 풀 반환
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class RollingSlashVisual : MonoBehaviour
    {
        private ParticleSystem _ps;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
        }

        /// <summary>
        /// 이펙트 시작. ovalSize로 ParticleSystem Shape 반경을 조정한 뒤 재생.
        /// </summary>
        public void Initialize(Vector2 ovalSize, float dirSign)
        {
            // Shape 반경을 타원 가로 절반 크기에 맞춤
            var shape = _ps.shape;
            shape.radius = ovalSize.x * 0.5f;

            // dirSign에 따라 X 방향 반전 (필요 시 스케일 조정)
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (dirSign >= 0f ? 1f : -1f);
            transform.localScale = s;

            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _ps.Play();

            StartCoroutine(WaitAndReturn());
        }

        private IEnumerator WaitAndReturn()
        {
            // ParticleSystem이 완전히 끝날 때까지 대기
            yield return new WaitUntil(() => _ps == null || !_ps.IsAlive(true));

            if (SkillObjectPool.Instance != null)
                SkillObjectPool.Instance.ReturnSlashVFX(this);
            else
                Destroy(gameObject);
        }
    }
}
