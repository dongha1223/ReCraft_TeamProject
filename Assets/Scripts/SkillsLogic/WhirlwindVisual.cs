using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 회전베기 VFX 컴포넌트.
    /// - SkillObjectPool 기반 풀링: lifetime 경과 후 자동 반환
    /// - 프리팹에 Animator + SpriteRenderer를 미리 구성해 둘 것
    /// </summary>
    public class WhirlwindVisual : MonoBehaviour
    {
        private Animator _anim;

        private void Awake()
        {
            _anim = GetComponent<Animator>();
        }

        /// <summary>
        /// 이펙트 시작.
        /// </summary>
        /// <param name="radius">회전베기 범위 반지름. 오브젝트 스케일을 지름(radius * 2)으로 설정한다.</param>
        /// <param name="dirSign">바라보는 방향 부호 (양수 = 오른쪽, 음수 = 왼쪽).</param>
        /// <param name="lifetime">표시 지속 시간 (초). 이후 풀로 반환.</param>
        public void Initialize(float radius, float dirSign, float lifetime)
        {
            float diameter = radius;
            transform.localScale = new Vector3(
                diameter * (dirSign >= 0f ? 1f : -1f),
                diameter,
                1f);

            // 풀에서 재사용될 때 애니메이션을 처음부터 재생
            if (_anim != null)
                _anim.Play(0, 0, 0f);

            StartCoroutine(WaitAndReturn(lifetime));
        }

        private IEnumerator WaitAndReturn(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);

            if (SkillObjectPool.Instance != null)
                SkillObjectPool.Instance.ReturnWhirlwindVFX(this);
            else
                Destroy(gameObject);
        }
    }
}
