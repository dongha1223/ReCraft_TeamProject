using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    public class PlayerSkill : MonoBehaviour
    {
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.aKey.wasPressedThisFrame)
                UseSkill1();

            if (keyboard.sKey.wasPressedThisFrame)
                UseSkill2();
        }

        private void UseSkill1()
        {
            // TODO: 스킬 1 구현
            Debug.Log("[PlayerSkill] Skill 1 used (A)");
        }

        private void UseSkill2()
        {
            // TODO: 스킬 2 구현
            Debug.Log("[PlayerSkill] Skill 2 used (S)");
        }
    }
}
