using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    /// <summary>
    /// 표지판(Signpost) 인터랙션 컨트롤러
    /// - 현재 스테이지 적이 0마리이면 스프라이트 알파를 50→255로 변경
    /// - 플레이어가 접근하면 F키 프롬프트(FKeyPrompt 자식 오브젝트) 표시
    /// - F키 입력 시 다음 스테이지 전환 또는 GameClear 발동
    /// </summary>
    public class SignpostController : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private bool  _isLastStage      = false;
        [SerializeField] private float _interactionRange = 2f;

        [Header("참조 — 비워두면 자동 탐색")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private GameObject     _fKeyPrompt; // "FKeyPrompt" 자식 오브젝트

        private bool      _isActivated  = false;
        private bool      _isPlayerNear = false;
        private Transform _playerTransform;

        private const float AlphaInactive = 50f  / 255f;
        private const float AlphaActive   = 255f / 255f;

        // ── 생명주기 ─────────────────────────────────────────────────────
        private void Awake()
        {
            // 자동 탐색
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_fKeyPrompt == null)
            {
                var child = transform.Find("FKeyPrompt");
                if (child != null) _fKeyPrompt = child.gameObject;
            }
        }

        private void OnEnable()
        {
            // 항상 비활성 상태로 시작 — StageManager.CheckAllEnemiesDeadNextFrame이 다음 프레임에 이벤트를 발행
            _isActivated  = false;
            _isPlayerNear = false;
            UpdateAlpha();

            if (_fKeyPrompt != null) _fKeyPrompt.SetActive(false);

            if (StageManager.Instance != null)
                StageManager.Instance.OnAllEnemiesDead += HandleAllEnemiesDead;
        }

        private void OnDisable()
        {
            if (StageManager.Instance != null)
                StageManager.Instance.OnAllEnemiesDead -= HandleAllEnemiesDead;

            if (_fKeyPrompt != null) _fKeyPrompt.SetActive(false);
        }

        private void Start()
        {
            RefreshPlayerRef();

            // OnEnable 시점에 StageManager가 없었을 수 있으므로 Start에서 재확인
            if (StageManager.Instance != null)
            {
                // 중복 구독 방지 후 재구독
                StageManager.Instance.OnAllEnemiesDead -= HandleAllEnemiesDead;
                StageManager.Instance.OnAllEnemiesDead += HandleAllEnemiesDead;

                if (!_isActivated && StageManager.Instance.AllEnemiesDead)
                {
                    _isActivated = true;
                    UpdateAlpha();
                }
            }
        }

        private void Update()
        {
            if (!_isActivated) return;
            if (_playerTransform == null) { RefreshPlayerRef(); return; }

            float dist = Vector2.Distance(transform.position, _playerTransform.position);
            bool  near = dist <= _interactionRange;

            if (near != _isPlayerNear)
            {
                _isPlayerNear = near;
                if (_fKeyPrompt != null) _fKeyPrompt.SetActive(_isPlayerNear);
            }

            if (_isPlayerNear
                && Keyboard.current != null
                && Keyboard.current.fKey.wasPressedThisFrame)
            {
                OnInteract();
            }
        }

        // ── 내부 메서드 ───────────────────────────────────────────────────
        private void HandleAllEnemiesDead()
        {
            _isActivated = true;
            UpdateAlpha();
        }

        private void OnInteract()
        {
            if (StageManager.Instance == null) return;

            if (_fKeyPrompt != null) _fKeyPrompt.SetActive(false);

            if (_isLastStage)
                StageManager.Instance.TriggerGameClear();
            else
                StageManager.Instance.TransitionToNextStage();
        }

        private void UpdateAlpha()
        {
            if (_spriteRenderer == null) return;
            Color col = _spriteRenderer.color;
            col.a = _isActivated ? AlphaActive : AlphaInactive;
            _spriteRenderer.color = col;
        }

        private void RefreshPlayerRef()
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) _playerTransform = playerGo.transform;
        }

        // ── 에디터 시각화 ─────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }
    }
}
