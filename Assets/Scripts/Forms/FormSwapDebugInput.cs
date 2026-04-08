using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    /// <summary>
    /// [디버그 전용] Phase 1 테스트용 Q키 교체 입력.
    /// Phase 2에서 TagController로 대체 후 제거할 것.
    /// </summary>
    public class FormSwapDebugInput : MonoBehaviour
    {
        private FormManager _formManager;

        private void Awake()
        {
            _formManager = GetComponent<FormManager>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.qKey.wasPressedThisFrame)
                _formManager.SwapSlots();
        }
    }
}
