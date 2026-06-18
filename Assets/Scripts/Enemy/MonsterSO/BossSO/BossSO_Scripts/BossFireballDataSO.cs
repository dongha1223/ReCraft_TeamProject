using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "BossFireballData", menuName = "2D Roguelike/Boss/Fireball Data")]
    public class BossFireballDataSO : ScriptableObject
    {
        [Header("스폰")]
        [Tooltip("화면 위 스폰 Y 좌표 (월드 기준)")]
        public float SpawnY        = 12f;
        [Tooltip("낙하 속도 (유닛/초)")]
        public float FallSpeed     = 3f;
        [Tooltip("한 번에 떨어지는 화염구 수")]
        public int   BallCount     = 5;
        [Tooltip("화염구 사이 간격 (초)")]
        public float SpawnInterval = 1f;
        [Tooltip("플레이어 X 기준 랜덤 오프셋 범위 (±)")]
        public float SpawnXRange   = 0f;
        [Tooltip("이 Y 미만으로 내려가면 강제 제거 (바닥 미끄러짐 방지)")]
        public float DestroyBelowY = -8f;

        [Header("데미지")]
        [Tooltip("낙하 중 플레이어 직격 데미지")]
        public float HitDamage          = 40f;
        [Tooltip("잔해 틱 데미지")]
        public float DebrisDamage       = 3f;
        [Tooltip("잔해 지속 시간 (초)")]
        public float DebrisDuration     = 4f;
        [Tooltip("잔해 틱 간격 (초)")]
        public float DebrisTickInterval = 1f;

        [Header("잔해 충돌 판정 크기")]
        public float DebrisWidth  = 0.6f;
        public float DebrisHeight = 0.25f;

        [Header("패턴 쿨다운 (마지막 구 스폰 후 대기)")]
        public float PatternCooldown = 7f;

        [Header("스프라이트")]
        [Tooltip("낙하 애니메이션 (Fire_Ball_1 ~ 14)")]
        public Sprite[] FallSprites;
        [Tooltip("착지/잔해 애니메이션 (Fire_Ball_15 ~ 33)")]
        public Sprite[] ImpactSprites;

        [Header("프리팹")]
        public GameObject FireballPrefab;
    }
}
