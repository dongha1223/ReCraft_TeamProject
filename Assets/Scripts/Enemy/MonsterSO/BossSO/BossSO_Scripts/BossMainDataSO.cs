using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "BossMainData", menuName = "2D Roguelike/Boss/Boss Main Data")]
    public class BossMainDataSO : ScriptableObject
    {
        [Header("페이즈 전환 조건")]
        [SerializeField] private int _phase2DestroyedCount = 2;

        [Header("패턴 쿨타임")]
        [SerializeField] private float _cooldownMin = 2f;
        [SerializeField] private float _cooldownMax = 4f;

        [Header("Phase 1 패턴 목록")]
        [SerializeField] private BossPattern[] _phase1Patterns;

        [Header("Phase 2 패턴 목록")]
        [SerializeField] private BossPattern[] _phase2Patterns;

        [Header("Phase 2 전환 연출")]
        [SerializeField] private string _phase2TransitionTrigger  = "";
        [SerializeField] private float  _phase2TransitionDuration = 1f;

        public int           Phase2DestroyedCount     => _phase2DestroyedCount;
        public float         CooldownMin              => _cooldownMin;
        public float         CooldownMax              => _cooldownMax;
        public BossPattern[] Phase1Patterns           => _phase1Patterns;
        public BossPattern[] Phase2Patterns           => _phase2Patterns;
        public string        Phase2TransitionTrigger  => _phase2TransitionTrigger;
        public float         Phase2TransitionDuration => _phase2TransitionDuration;
    }
}
