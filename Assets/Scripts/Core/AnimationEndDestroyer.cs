using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// Animator 애니메이션이 1회 재생 완료되면 GameObject를 삭제한다.
    /// Loop Time이 꺼진 클립 전용. SpriteAnimator 기반 이펙트(DashEFX, JumpEFX 등)에 사용.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AnimationEndDestroyer : MonoBehaviour
    {
        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private IEnumerator Start()
        {
            // 첫 프레임은 Animator가 초기화 중이므로 한 프레임 대기
            yield return null;

            while (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
                yield return null;

            Destroy(gameObject);
        }
    }
}
