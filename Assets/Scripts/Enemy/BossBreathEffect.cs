using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 보스 Idle 시 숨쉬는 듯한 미세 스케일 파동 효과.
    /// 부착한 Transform의 localScale에 Sin 파를 적용한다.
    /// flipX 대신 localScale.x 부호로 방향을 관리하는 프로젝트 규칙을 유지한다.
    /// </summary>
    public class BossBreathEffect : MonoBehaviour
    {
        [Header("데이터 SO")]
        [SerializeField] private BossBreathEffectDataSO _data;

        [SerializeField] private float _breathSpeed  = 1.2f;  // 초당 사이클 수
        [SerializeField] private float _breathAmount = 0.03f; // 최대 스케일 변화량 (±3%)

        private Vector3 _baseSize; // 절댓값 기준 스케일 (방향 부호 제외)
        private bool    _isDead;

        private void Awake()
        {
            if (_data != null)
            {
                _breathSpeed  = _data.BreathSpeed;
                _breathAmount = _data.BreathAmount;
            }
            Vector3 s = transform.localScale;
            _baseSize = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }

        private void OnEnable()
        {
            BossMainController.OnBossDead += HandleDead;
            _isDead = false;
        }

        private void OnDisable()
        {
            BossMainController.OnBossDead -= HandleDead;
        }

        private void Update()
        {
            if (_isDead) return;

            float breath = 1f + Mathf.Sin(Time.time * _breathSpeed * Mathf.PI * 2f) * _breathAmount;

            // localScale.x 부호를 읽어 flip 방향 보존
            float dirX = Mathf.Sign(transform.localScale.x);
            if (dirX == 0f) dirX = 1f;

            transform.localScale = new Vector3(
                dirX * _baseSize.x * breath,
                _baseSize.y * breath,
                _baseSize.z
            );
        }

        private void HandleDead()
        {
            _isDead = true;

            // 죽으면 스케일을 원래 크기로 즉시 복원
            float dirX = Mathf.Sign(transform.localScale.x);
            if (dirX == 0f) dirX = 1f;

            transform.localScale = new Vector3(
                dirX * _baseSize.x,
                _baseSize.y,
                _baseSize.z
            );
        }
    }
}
