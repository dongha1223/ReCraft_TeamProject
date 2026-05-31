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
        [Tooltip("StageManager가 런타임에 자동 주입 — 직접 수정 불필요")]
        [SerializeField] private bool _isLastStage = false;

        [Header("참조 — 비워두면 자동 탐색")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private GameObject     _fKeyPrompt;

        private bool _isActivated = false;

        private const float AlphaInactive = 50f  / 255f;
        private const float AlphaActive   = 255f / 255f;

        // ── 외부 주입 ─────────────────────────────────────────────────

        /// <summary>StageManager가 스테이지 활성화 시 자동 주입 — Inspector 값을 덮어씀</summary>
        public void SetIsLastStage(bool isLast) => _isLastStage = isLast;

        /// <summary>스테이지 종류 정보 수신 — 현재는 구독 방식이 통일되어 처리 불필요</summary>
        public void SetIsBossStage(bool isBoss) { }

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
#if UNITY_EDITOR
            Debug.Log($"[Signpost] OnInteract called. isLastStage={_isLastStage}, StageManager={(StageManager.Instance != null ? "OK" : "NULL")}");
#endif
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

            RefreshSubscription();
        }

        private void OnDisable()
        {
            UnsubscribeAll();
            if (_fKeyPrompt != null) _fKeyPrompt.SetActive(false);
        }

        private void Start()
        {
            // OnEnable 시점에 StageManager가 없었을 수 있으므로 재구독
            RefreshSubscription();

            // 이미 전멸 상태로 시작하는 스테이지 대응 (적 0마리 스테이지)
            if (!_isActivated && StageManager.Instance != null && StageManager.Instance.AllEnemiesDead)
            {
                _isActivated = true;
                UpdateAlpha();
            }
        }

        // ── 내부 ─────────────────────────────────────────────────────

        private void RefreshSubscription()
        {
            UnsubscribeAll();

            // 보스/미드보스/일반 스테이지 모두 OnAllEnemiesDead로 판정.
            // 현재 씬의 모든 보스(BossMainController, BossArcherController, MidBossController)는
            // EnemyStats 또는 BossMainController.HandleBossDeath()를 통해
            // StageManager.OnEnemyDied()를 호출하므로 이 단일 경로로 처리 가능.
            if (StageManager.Instance != null)
                StageManager.Instance.OnAllEnemiesDead += HandleClearConditionMet;
        }

        private void UnsubscribeAll()
        {
            BossController.OnBossDead -= HandleClearConditionMet; // 안전망 유지
            if (StageManager.Instance != null)
                StageManager.Instance.OnAllEnemiesDead -= HandleClearConditionMet;
        }

        private void HandleClearConditionMet()
        {
#if UNITY_EDITOR
            Debug.Log($"[Signpost] 클리어 조건 충족 — '{gameObject.scene.name}'");
#endif
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
