using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 태그 전환 HUD — 화면 좌측 하단에 현재 캐릭터 + 키 힌트 표시
    /// </summary>
    public class TagSwitchUI : MonoBehaviour
    {
        private TagSystem _tag;
        private float     _flashTimer;

        // GUI 스타일 (첫 OnGUI에서 빌드)
        private GUIStyle _nameStyle;
        private GUIStyle _hintStyle;
        private bool     _stylesReady;

        private void Awake()
        {
            _tag = FindFirstObjectByType<TagSystem>();
        }

        // TagSystem이 호출
        public void OnSwitchStart() => _flashTimer = 0.4f;
        public void OnSwitchEnd()   => _flashTimer = 0f;

        private void Update()
        {
            if (_flashTimer > 0f) _flashTimer -= Time.deltaTime;
        }

        private void OnGUI()
        {
            if (_tag == null) return;
            BuildStyles();

            bool  isMage = _tag.IsMage;
            float flash  = Mathf.Clamp01(_flashTimer / 0.4f);

            // ── 패널 ──────────────────────────────────────────────────
            float pw = 200f, ph = 60f;
            float px = 14f,  py = Screen.height - ph - 14f;

            Color panelBase = isMage
                ? Color.Lerp(new Color(0.08f, 0.08f, 0.22f, 0.85f), new Color(0.2f, 0.5f, 1f, 0.95f), flash)
                : Color.Lerp(new Color(0.14f, 0.10f, 0.06f, 0.85f), new Color(1f, 0.7f, 0.1f, 0.95f), flash);
            GUI.color = panelBase;
            GUI.Box(new Rect(px, py, pw, ph), GUIContent.none);
            GUI.color = Color.white;

            // ── 아이콘 + 이름 ─────────────────────────────────────────
            string icon  = isMage ? "✦" : "⚔";
            Color  tint  = isMage ? new Color(0.55f, 0.9f, 1f) : new Color(1f, 0.88f, 0.35f);
            GUI.color = tint;
            GUI.Label(new Rect(px + 8f, py + 6f, 36f, 44f), icon, _nameStyle);
            GUI.color = Color.white;
            GUI.Label(new Rect(px + 46f, py + 8f, 144f, 26f),
                isMage ? "마법사" : "전사", _nameStyle);

            // ── 힌트 ──────────────────────────────────────────────────
            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            GUI.Label(new Rect(px + 46f, py + 36f, 144f, 20f),
                isMage ? "[X] 마법타격 → 구체  [Z] 대시"
                       : "[X] 공격  [A] 검기  [S] 롤링",
                _hintStyle);
            GUI.Label(new Rect(px + 8f, py + 36f, 36f, 20f), "[Q]", _hintStyle);
            GUI.color = Color.white;
        }

        private void BuildStyles()
        {
            if (_stylesReady) return;
            _nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 18,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };
            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal   = { textColor = Color.white }
            };
            _stylesReady = true;
        }
    }
}
