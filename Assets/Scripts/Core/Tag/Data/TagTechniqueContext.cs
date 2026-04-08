using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 교체기 실행 시 Behaviour에 전달되는 플레이어 컴포넌트 묶음.
    /// ScriptableObject(TagTechniqueBehaviour)가 MonoBehaviour 없이
    /// 플레이어 데이터에 접근할 수 있도록 한다.
    /// </summary>
    public class TagTechniqueContext
    {
        public Transform             PlayerTransform { get; }
        public Rigidbody2D           PlayerRb        { get; }
        public AreaSkillExecutor     AreaExecutor    { get; }
        public InvincibilityHandler  Invincibility   { get; }
        public PlayerStatController  StatController  { get; }
        public TagTechniqueDefinition Definition     { get; }

        /// <summary>현재 바라보는 방향 (localScale.x 부호 기반)</summary>
        public Vector2 FacingDirection =>
            PlayerTransform.localScale.x >= 0f ? Vector2.right : Vector2.left;

        public TagTechniqueContext(
            Transform            playerTransform,
            Rigidbody2D          playerRb,
            AreaSkillExecutor    areaExecutor,
            InvincibilityHandler invincibility,
            PlayerStatController statController,
            TagTechniqueDefinition definition)
        {
            PlayerTransform = playerTransform;
            PlayerRb        = playerRb;
            AreaExecutor    = areaExecutor;
            Invincibility   = invincibility;
            StatController  = statController;
            Definition      = definition;
        }
    }
}
