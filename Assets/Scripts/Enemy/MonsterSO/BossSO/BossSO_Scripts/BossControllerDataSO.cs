using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "BossControllerData", menuName = "2D Roguelike/Boss/Boss Controller Data")]
    public class BossControllerDataSO : ScriptableObject
    {
        [Header("보스 정보")]
        [SerializeField] private string _bossName = "";
        [SerializeField] private string _bossRace = "";

        [Header("Phase 2 — 연출")]
        [SerializeField] private GameObject _transitionObeliskPrefab;
        [SerializeField] private float      _survivalRadius       = 3f;
        [SerializeField] private float      _screenShakeDuration  = 0.6f;
        [SerializeField] private float      _screenShakeIntensity = 0.25f;
        [SerializeField] private float      _terrainChangDelay    = 4f;

        [Header("Phase 2 — 플레이어 제한")]
        [SerializeField] private int _phase2MaxDashes = 1;

        [Header("카메라")]
        [SerializeField] private float _bossOrthoSize            = 10f;
        [SerializeField] private float _cameraTransitionDuration = 1.5f;

        public string     BossName                 => _bossName;
        public string     BossRace                 => _bossRace;
        public GameObject TransitionObeliskPrefab  => _transitionObeliskPrefab;
        public float      SurvivalRadius           => _survivalRadius;
        public float      ScreenShakeDuration      => _screenShakeDuration;
        public float      ScreenShakeIntensity     => _screenShakeIntensity;
        public float      TerrainChangDelay        => _terrainChangDelay;
        public int        Phase2MaxDashes          => _phase2MaxDashes;
        public float      BossOrthoSize            => _bossOrthoSize;
        public float      CameraTransitionDuration => _cameraTransitionDuration;
    }
}
