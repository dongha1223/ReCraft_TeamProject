using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 표지판(Signpost) 상호작용 컨트롤러.
    /// - 현재 스테이지 적이 0마리이면 스프라이트 알파를 50→255로 변경
    /// - F키 입력 및 근접 감지는 PlayerInteractor가 담당
    /// - 이 클래스는 "활성 조건 관리"와 "실제 스테이지 전환 실행"만 책임진다
    /// </summary>
    public class SignpostController : MonoBehaviour, IInteractable
    {
        [Header("설정")]
        [SerializeField] private bool _isLastStage = false;

        [Header("참조 — 비워두면 자동 탐색")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private GameObject     _fKeyPrompt;

        private bool _isActivated = false;

        private const float AlphaInactive = 50f  / 255f;
        private const float AlphaActive   = 255f / 255f;

        // ── IInteractable ─────────────────────────────────────────────

        /// <summary>적이 모두 죽어야 상호작용 가능</summary>
        public bool CanInteract => _isActivated;

        public void OnFocused()
        {
            if (_fKeyPrompt != null) _fKeyPrompt.SetActive(true);
        }

        public void OnUnfocused()
        {
            if (_fKeyPrompt != null) _fKeyPrompt.SetActive(false);
        }

        public void OnInteract(PlayerStatController statController)
        {
            Debug.Log($"[Signpost] OnInteract called. isLastStage={_isLastStage}, StageManager={(StageManager.Instance != null ? "OK" : "NULL")}");
            if (StageManager.Instance == null) return;
            if (_fKeyPrompt != null) _fKeyPrompt.SetActive(false);

            if (_isLastStage)
                StageManager.Instance.TriggerGameClear();
            else
                StageManager.Instance.TransitionToNextStage();
        }

        // ── 생명주기 ─────────────────────────────────────────────────

        private void Awake()
        {
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
            _isActivated = false;
            UpdateAlpha();

            if (_fKeyPrompt != null) _fKeyPrompt.SetActive(false);

            if (StageManager.Instance != null)
            {
                StageManager.Instance.OnAllEnemiesDead -= HandleAllEnemiesDead;
                StageManager.Instance.OnAllEnemiesDead += HandleAllEnemiesDead;
            }
        }

        private void OnDisable()
        {
            if (StageManager.Instance != null)
                StageManager.Instance.OnAllEnemiesDead -= HandleAllEnemiesDead;

            if (_fKeyPrompt != null) _fKeyPrompt.SetActive(false);
        }

        private void Start()
        {
            if (StageManager.Instance == null) return;

            // OnEnable 시점에 StageManager가 없었을 수 있으므로 재구독
            StageManager.Instance.OnAllEnemiesDead -= HandleAllEnemiesDead;
            StageManager.Instance.OnAllEnemiesDead += HandleAllEnemiesDead;

            // 이미 전멸 상태로 시작하는 스테이지 대응
            if (!_isActivated && StageManager.Instance.AllEnemiesDead)
            {
                _isActivated = true;
                UpdateAlpha();
            }
        }

        // ── 내부 ─────────────────────────────────────────────────────

        private void HandleAllEnemiesDead()
        {
            Debug.Log($"[Signpost] HandleAllEnemiesDead — activating signpost on '{gameObject.scene.name}'");
            _isActivated = true;
            UpdateAlpha();
        }

        private void UpdateAlpha()
        {
            if (_spriteRenderer == null) return;
            Color col = _spriteRenderer.color;
            col.a = _isActivated ? AlphaActive : AlphaInactive;
            _spriteRenderer.color = col;
        }
    }
}
