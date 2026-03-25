using UnityEngine;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    [RequireComponent(typeof(UIDocument))]
    public class PlayerHPUI : MonoBehaviour
    {
        private PlayerStats             _playerStats;
        private PlayerSkill             _playerSkill;

        private VisualElement _hpBarFill;
        private Label         _hpLabel;
        private VisualElement _skillACooldown;
        private VisualElement _skillSCooldown;

        private void Start()
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                _playerStats = playerGO.GetComponent<PlayerStats>();
                _playerSkill = playerGO.GetComponent<PlayerSkill>();
            }

            var root = GetComponent<UIDocument>().rootVisualElement;
            _hpBarFill      = root.Q<VisualElement>("hp-bar-fill");
            _hpLabel        = root.Q<Label>("hp-label");
            _skillACooldown = root.Q<VisualElement>("skill-a-cooldown");
            _skillSCooldown = root.Q<VisualElement>("skill-s-cooldown");
        }

        private void Update()
        {
            UpdateHP();
            UpdateSkillCooldowns();
        }

        // ── HP 바 갱신 ────────────────────────────────────────────────
        private void UpdateHP()
        {
            if (_playerStats == null || _hpBarFill == null) return;

            float ratio = _playerStats.MaxHp > 0
                ? _playerStats.CurrentHp / _playerStats.MaxHp
                : 0f;

            _hpBarFill.style.width = Length.Percent(ratio * 100f);

            if (_hpLabel != null)
            {
                int cur = Mathf.CeilToInt(_playerStats.CurrentHp);
                int max = Mathf.CeilToInt(_playerStats.MaxHp);
                _hpLabel.text = $"{cur} / {max}";
            }
        }

        // ── 스킬 쿨타임 오버레이 갱신 ────────────────────────────────
        private void UpdateSkillCooldowns()
        {
            if (_playerSkill == null) return;

            if (_skillACooldown != null)
                _skillACooldown.style.height = Length.Percent(_playerSkill.Skill1CooldownRatio * 100f);

            if (_skillSCooldown != null)
                _skillSCooldown.style.height = Length.Percent(_playerSkill.Skill2CooldownRatio * 100f);
        }
    }
}
