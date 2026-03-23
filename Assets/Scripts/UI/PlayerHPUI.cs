using UnityEngine;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    [RequireComponent(typeof(UIDocument))]
    public class PlayerHPUI : MonoBehaviour
    {
        private PlayerStats _playerStats;
        private VisualElement _hpBarFill;
        private Label _hpLabel;

        private void Start()
        {
            // 플레이어 참조
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
                _playerStats = playerGO.GetComponent<PlayerStats>();

            // UI 요소 참조
            var root = GetComponent<UIDocument>().rootVisualElement;
            _hpBarFill = root.Q<VisualElement>("hp-bar-fill");
            _hpLabel   = root.Q<Label>("hp-label");
        }

        private void Update()
        {
            if (_playerStats == null || _hpBarFill == null) return;

            float ratio = _playerStats.MaxHp > 0
                ? _playerStats.CurrentHp / _playerStats.MaxHp
                : 0f;

            // HP 바 너비 갱신
            _hpBarFill.style.width = Length.Percent(ratio * 100f);

            // HP 텍스트 갱신
            int cur = Mathf.CeilToInt(_playerStats.CurrentHp);
            int max = Mathf.CeilToInt(_playerStats.MaxHp);
            _hpLabel.text = $"{cur} / {max}";

            // HP 비율에 따라 바 색상 변경
            if (ratio > 0.5f)
                _hpBarFill.style.backgroundColor = new StyleColor(new Color(0.82f, 0.2f, 0.2f));
            else if (ratio > 0.25f)
                _hpBarFill.style.backgroundColor = new StyleColor(new Color(0.9f, 0.55f, 0.1f));
            else
                _hpBarFill.style.backgroundColor = new StyleColor(new Color(0.95f, 0.15f, 0.15f));
        }
    }
}
