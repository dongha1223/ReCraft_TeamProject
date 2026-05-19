using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 스킬 실행 시 SkillBehaviour에 전달되는 플레이어 컴포넌트 묶음.
    /// ScriptableObject(SkillBehaviour)가 MonoBehaviour 없이
    /// 플레이어 데이터에 접근할 수 있도록 한다.
    /// </summary>
    public class SkillContext
    {
        public Transform             PlayerTransform  { get; }
        public Rigidbody2D           PlayerRb         { get; }
        public Animator              Animator         { get; }
        public PlayerStatController  StatController   { get; }
        public OnHitStatusRegistry   OnHitRegistry    { get; }
        public AreaSkillExecutor     AreaExecutor     { get; }
        public SkillDefinition       Definition       { get; }

        /// <summary>이동 잠금 콜백. true = 잠금, false = 해제. 차지 스킬 전용.</summary>
        public System.Action<bool>   SetMovementLock  { get; }

        /// <summary>현재 바라보는 방향 (localScale.x 부호 기반)</summary>
        public Vector2 FacingDirection =>
            PlayerTransform.localScale.x >= 0f ? Vector2.right : Vector2.left;

        public SkillContext(
            Transform            playerTransform,
            Rigidbody2D          playerRb,
            Animator             animator,
            PlayerStatController statController,
            OnHitStatusRegistry  onHitRegistry,
            AreaSkillExecutor    areaExecutor,
            SkillDefinition      definition,
            System.Action<bool>  setMovementLock = null)
        {
            PlayerTransform = playerTransform;
            PlayerRb        = playerRb;
            Animator        = animator;
            StatController  = statController;
            OnHitRegistry   = onHitRegistry;
            AreaExecutor    = areaExecutor;
            Definition      = definition;
            SetMovementLock = setMovementLock;
        }
    }
}
