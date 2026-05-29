using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "BossFirePillarData", menuName = "2D Roguelike/Boss/Boss Fire Pillar Data")]
    public class BossFirePillarDataSO : ScriptableObject
    {
        [Header("패턴 설정")]
        [SerializeField] private float _spawnY        = -3f;
        [SerializeField] private int   _pillarCount   = 10;
        [SerializeField] private float _spawnInterval = 0.3f;
        [SerializeField] private float _minSpacing    = 1.5f;

        [Header("불기둥 크기")]
        [SerializeField] private float _pillarWidth   = 1f;
        [SerializeField] private float _pillarHeight  = 1f;

        [Header("눈 빛나기")]
        [SerializeField] private Color _glowColor = Color.red;

        [Header("불기둥 프리팹")]
        [SerializeField] private GameObject _pillarPrefab;

        [Header("스프라이트")]
        [SerializeField] private Sprite   _craterSprite;
        [SerializeField] private Sprite[] _fireSprites;

        public float      SpawnY        => _spawnY;
        public int        PillarCount   => _pillarCount;
        public float      SpawnInterval => _spawnInterval;
        public float      MinSpacing    => _minSpacing;
        public float      PillarWidth   => _pillarWidth;
        public float      PillarHeight  => _pillarHeight;
        public Color      GlowColor     => _glowColor;
        public GameObject PillarPrefab  => _pillarPrefab;
        public Sprite     CraterSprite  => _craterSprite;
        public Sprite[]   FireSprites   => _fireSprites;
    }
}
