using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    /// <summary>
    /// 키 바인딩 상태를 전역 관리하고 PlayerPrefs에 저장한다.
    /// 플레이어 스크립트는 Keyboard.current.xKey 등 대신 이 클래스를 사용할 것.
    /// </summary>
    public static class KeyBindingService
    {
        public enum Action
        {
            MoveUp    = 0,
            MoveDown  = 1,
            MoveLeft  = 2,
            MoveRight = 3,
            Inventory = 4,
            Interact  = 5,
            Attack    = 6,
            Jump      = 7,
            Dash      = 8,
            Skill1    = 9,
            Skill2    = 10,
            Essence   = 11,
            Swap      = 12,
            Count     = 13
        }

        // 기본 키 설정 (컨트롤 UI에 표시된 값과 일치)
        private static readonly Key[] Defaults =
        {
            Key.UpArrow,    // MoveUp
            Key.DownArrow,  // MoveDown
            Key.LeftArrow,  // MoveLeft
            Key.RightArrow, // MoveRight
            Key.Tab,        // Inventory
            Key.F,          // Interact
            Key.X,          // Attack
            Key.Space,      // Jump
            Key.Z,          // Dash
            Key.A,          // Skill1
            Key.S,          // Skill2
            Key.D,          // Essence
            Key.C,          // Swap
        };

        private static readonly Key[] _bindings = new Key[(int)Action.Count];
        private static bool _directionalDash;

        private const string PrefPrefix  = "KeyBind_";
        private const string PrefDirDash = "KeyBind_DirDash";

        static KeyBindingService() => Load();

        // ── 읽기 ──────────────────────────────────────────────────────

        public static Key  Get(Action action)  => _bindings[(int)action];
        public static bool DirectionalDash     => _directionalDash;

        // ── 쓰기 ──────────────────────────────────────────────────────

        public static void Set(Action action, Key key)
        {
            _bindings[(int)action] = key;
            PlayerPrefs.SetInt(PrefPrefix + (int)action, (int)key);
            PlayerPrefs.Save();
        }

        public static void SetDirectionalDash(bool value)
        {
            _directionalDash = value;
            PlayerPrefs.SetInt(PrefDirDash, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ResetToDefaults()
        {
            System.Array.Copy(Defaults, _bindings, Defaults.Length);
            _directionalDash = false;
            for (int i = 0; i < Defaults.Length; i++)
                PlayerPrefs.SetInt(PrefPrefix + i, (int)Defaults[i]);
            PlayerPrefs.SetInt(PrefDirDash, 0);
            PlayerPrefs.Save();
        }

        // ── 키 상태 조회 ──────────────────────────────────────────────

        public static bool IsPressed(Action action)
        {
            var kb = Keyboard.current;
            return kb != null && kb[_bindings[(int)action]].isPressed;
        }

        public static bool WasPressedThisFrame(Action action)
        {
            var kb = Keyboard.current;
            return kb != null && kb[_bindings[(int)action]].wasPressedThisFrame;
        }

        public static bool WasReleasedThisFrame(Action action)
        {
            var kb = Keyboard.current;
            return kb != null && kb[_bindings[(int)action]].wasReleasedThisFrame;
        }

        // ── 표시 문자열 변환 ──────────────────────────────────────────

        public static string ToDisplayString(Key key) => key switch
        {
            Key.UpArrow      => "↑",
            Key.DownArrow    => "↓",
            Key.LeftArrow    => "←",
            Key.RightArrow   => "→",
            Key.Space        => "SPACE",
            Key.Tab          => "TAB",
            Key.Enter        => "ENTER",
            Key.Escape       => "ESC",
            Key.LeftShift    => "LSHIFT",
            Key.RightShift   => "RSHIFT",
            Key.LeftCtrl     => "LCTRL",
            Key.RightCtrl    => "RCTRL",
            Key.LeftAlt      => "LALT",
            Key.RightAlt     => "RALT",
            Key.Backspace    => "BACK",
            Key.Delete       => "DEL",
            Key.Insert       => "INS",
            Key.Home         => "HOME",
            Key.End          => "END",
            Key.PageUp       => "PG UP",
            Key.PageDown     => "PG DN",
            Key.NumpadEnter  => "N.ENTER",
            Key.NumpadPlus   => "N.+",
            Key.NumpadMinus  => "N.-",
            Key.NumpadMultiply => "N.*",
            Key.NumpadDivide => "N./",
            Key.Numpad0      => "NUM 0",
            Key.Numpad1      => "NUM 1",
            Key.Numpad2      => "NUM 2",
            Key.Numpad3      => "NUM 3",
            Key.Numpad4      => "NUM 4",
            Key.Numpad5      => "NUM 5",
            Key.Numpad6      => "NUM 6",
            Key.Numpad7      => "NUM 7",
            Key.Numpad8      => "NUM 8",
            Key.Numpad9      => "NUM 9",
            Key.F1  => "F1",  Key.F2  => "F2",  Key.F3  => "F3",
            Key.F4  => "F4",  Key.F5  => "F5",  Key.F6  => "F6",
            Key.F7  => "F7",  Key.F8  => "F8",  Key.F9  => "F9",
            Key.F10 => "F10", Key.F11 => "F11", Key.F12 => "F12",
            _                => key.ToString().ToUpper()
        };

        // ── 로드 ──────────────────────────────────────────────────────

        private static void Load()
        {
            for (int i = 0; i < Defaults.Length; i++)
                _bindings[i] = (Key)PlayerPrefs.GetInt(PrefPrefix + i, (int)Defaults[i]);
            _directionalDash = PlayerPrefs.GetInt(PrefDirDash, 0) == 1;
        }
    }
}
