using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    [RequireComponent(typeof(UIDocument))]
    public class PlayerHPUI : MonoBehaviour
    {
        private PlayerStats             _playerStats;
        private FormSkillController     _formSkillController;
        private TagTokenBank            _tagTokenBank;
        private FormManager             _formManager;

        private VisualElement _hpBarFill;
        private VisualElement _portraitCircle;
        private Label         _hpLabel;
        private VisualElement _skillACooldown;
        private VisualElement _skillSCooldown;
        private Label         _enemyCountLabel;
        private VisualElement _tokenBarFill;
        private Label         _tokenCountLabel;
        private Label         _beliefCountLabel;
        private Label         _goldCountLabel;
        private VisualElement _goldIcon;
        private IPanel        _panel;

        public static Vector3 GoldIconWorldPos { get; private set; }

        private static readonly int _maxFilledCount = (int)(TagTokenBank.MaxGauge / TagTokenBank.GaugePerToken);

        private float _lastHp    = -1f;
        private float _lastMaxHp = -1f;
        private int   _lastEnemyCount = -1;

        private void Start()
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                _playerStats         = playerGO.GetComponent<PlayerStats>();
                _formSkillController = playerGO.GetComponent<FormSkillController>();
                _tagTokenBank        = playerGO.GetComponent<TagTokenBank>();
                _formManager         = playerGO.GetComponent<FormManager>();
            }

            var root = GetComponent<UIDocument>().rootVisualElement;
            _portraitCircle  = root.Q<VisualElement>("portrait-circle");
            _hpBarFill       = root.Q<VisualElement>("hp-bar-fill");
            _hpLabel         = root.Q<Label>("hp-label");
            _skillACooldown  = root.Q<VisualElement>("skill-a-cooldown");
            _skillSCooldown  = root.Q<VisualElement>("skill-s-cooldown");
            _enemyCountLabel  = root.Q<Label>("enemy-count");
            _tokenBarFill     = root.Q<VisualElement>("token-bar-fill");
            _tokenCountLabel  = root.Q<Label>("token-count-label");
            _beliefCountLabel = root.Q<Label>("belief-count");
            _goldCountLabel   = root.Q<Label>("gold-count");
            _goldIcon         = root.Q<VisualElement>("gold-icon");
            _panel            = root.panel;
            StartCoroutine(CacheGoldIconWorldPos());

            if (_formManager != null)
            {
                _formManager.OnFormSwapped += OnFormSwapped;
                RefreshPortrait(_formManager.Current);
            }

            if (_tagTokenBank != null)
            {
                _tagTokenBank.OnGaugeChanged += OnTokenGaugeChanged;
                OnTokenGaugeChanged(_tagTokenBank.TotalGauge);
            }

            var bm = BeliefManager.Instance;
            if (bm != null)
            {
                bm.OnBeliefChanged += UpdateBeliefCount;
                UpdateBeliefCount();
            }

            var gm = GoldManager.Instance;
            if (gm != null)
            {
                gm.OnGoldChanged += UpdateGoldCount;
                UpdateGoldCount();
            }
        }

        private void OnDestroy()
        {
            if (_formManager != null)
                _formManager.OnFormSwapped -= OnFormSwapped;

            if (_tagTokenBank != null)
                _tagTokenBank.OnGaugeChanged -= OnTokenGaugeChanged;

            var bm = BeliefManager.Instance;
            if (bm != null)
                bm.OnBeliefChanged -= UpdateBeliefCount;

            var gm = GoldManager.Instance;
            if (gm != null)
                gm.OnGoldChanged -= UpdateGoldCount;
        }

        private void Update()
        {
            UpdateHP();
            UpdateSkillCooldowns();
            UpdateEnemyCount();
        }

        // ── 초상화 갱신 ──────────────────────────────────────────────
        private void OnFormSwapped(FormDefinition _, FormDefinition next) => RefreshPortrait(next);

        private void RefreshPortrait(FormDefinition form)
        {
            if (_portraitCircle == null) return;
            var icon = form?.Icon;
            if (icon != null)
                _portraitCircle.style.backgroundImage = new StyleBackground(icon);
            else
                _portraitCircle.style.backgroundImage = StyleKeyword.None;
        }

        // ── HP 바 갱신 ────────────────────────────────────────────────
        private void UpdateHP()
        {
            if (_playerStats == null || _hpBarFill == null) return;

            float currentHp = _playerStats.CurrentHp;
            float maxHp     = _playerStats.MaxHp;
            if (currentHp == _lastHp && maxHp == _lastMaxHp) return;
            _lastHp    = currentHp;
            _lastMaxHp = maxHp;

            float ratio = maxHp > 0 ? currentHp / maxHp : 0f;
            _hpBarFill.style.width = Length.Percent(ratio * 100f);

            if (_hpLabel != null)
                _hpLabel.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
        }

        // ── 토큰 게이지 갱신 ─────────────────────────────────────────
        private void OnTokenGaugeChanged(float totalGauge)
        {
            if (_tokenBarFill == null || _tokenCountLabel == null) return;

            int   filled  = _tagTokenBank.FilledCount;
            float partial = filled >= _maxFilledCount
                ? 0f
                : (totalGauge % TagTokenBank.GaugePerToken) / TagTokenBank.GaugePerToken;

            _tokenBarFill.style.width = Length.Percent(partial * 100f);
            _tokenCountLabel.text     = $"({filled}/{_maxFilledCount})";
        }

        // ── 몬스터 카운터 갱신 ────────────────────────────────────────
        private void UpdateEnemyCount()
        {
            if (_enemyCountLabel == null || StageManager.Instance == null) return;
            int count = StageManager.Instance.AliveEnemyCount;
            if (count == _lastEnemyCount) return;
            _lastEnemyCount       = count;
            _enemyCountLabel.text = count.ToString();
        }

        // ── 신념 카운터 갱신 ──────────────────────────────────────────
        private void UpdateBeliefCount()
        {
            if (_beliefCountLabel == null) return;
            _beliefCountLabel.text = (BeliefManager.Instance?.TotalBelief ?? 0).ToString();
        }

        private IEnumerator CacheGoldIconWorldPos()
        {
            yield return null; // UIToolkit 레이아웃 완료 대기
            if (_goldIcon == null || Camera.main == null) yield break;

            Rect    bounds    = _goldIcon.worldBound;
            Vector2 panelCenter = bounds.center;
            Vector2 screenPos = new Vector2(panelCenter.x, Screen.height - panelCenter.y);
            float   depth     = Mathf.Abs(Camera.main.transform.position.z);
            var     worldPos  = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
            worldPos.z        = 0f;
            GoldIconWorldPos  = worldPos;
        }

        // ── 골드 카운터 갱신 ──────────────────────────────────────────
        private void UpdateGoldCount()
        {
            if (_goldCountLabel == null) return;
            _goldCountLabel.text = (GoldManager.Instance?.Gold ?? 0).ToString();
        }

        // ── 스킬 쿨타임 오버레이 갱신 ────────────────────────────────
        private void UpdateSkillCooldowns()
        {
            if (_formSkillController == null) return;

            if (_skillACooldown != null)
                _skillACooldown.style.height = Length.Percent(_formSkillController.Skill1CooldownRatio * 100f);

            if (_skillSCooldown != null)
                _skillSCooldown.style.height = Length.Percent(_formSkillController.Skill2CooldownRatio * 100f);
        }
    }
}
