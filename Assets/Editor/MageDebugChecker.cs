using UnityEngine;
using UnityEditor;

namespace _2D_Roguelike
{
    public static class MageDebugChecker
    {
        [MenuItem("Tools/[Debug] Check Mage Setup")]
        public static void Check()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null) { Debug.LogError("Player 없음!"); return; }

            // SpriteRenderer
            var sr = player.GetComponent<SpriteRenderer>();
            Debug.Log($"[Debug] SpriteRenderer.sprite = {(sr?.sprite != null ? sr.sprite.name : "NULL")}");

            // Animator
            var anim = player.GetComponent<Animator>();
            Debug.Log($"[Debug] Animator.runtimeAnimatorController = {(anim?.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "NULL")}");

            // TagSystem
            var tag = player.GetComponent<_2D_Roguelike.TagSystem>();
            if (tag == null) { Debug.LogError("[Debug] TagSystem 없음!"); return; }

            var so = new SerializedObject(tag);
            var mageCtrl   = so.FindProperty("_mageController");
            var warriorCtrl = so.FindProperty("_warriorController");
            var mageSprite  = so.FindProperty("_mageIdleSprite");
            var warriorSprite = so.FindProperty("_warriorIdleSprite");

            Debug.Log($"[Debug] TagSystem._mageController = {(mageCtrl?.objectReferenceValue != null ? mageCtrl.objectReferenceValue.name : "NULL ← 문제!")}");
            Debug.Log($"[Debug] TagSystem._warriorController = {(warriorCtrl?.objectReferenceValue != null ? warriorCtrl.objectReferenceValue.name : "NULL ← 문제!")}");
            Debug.Log($"[Debug] TagSystem._mageIdleSprite = {(mageSprite?.objectReferenceValue != null ? mageSprite.objectReferenceValue.name : "NULL ← 문제!")}");
            Debug.Log($"[Debug] TagSystem._warriorIdleSprite = {(warriorSprite?.objectReferenceValue != null ? warriorSprite.objectReferenceValue.name : "NULL ← 문제!")}");

            // MageAttack
            var mageAtk = player.GetComponent<_2D_Roguelike.MageAttack>();
            Debug.Log($"[Debug] MageAttack.enabled = {(mageAtk != null ? mageAtk.enabled.ToString() : "컴포넌트 없음!")}");

            // AnimationClip 목록
            if (anim?.runtimeAnimatorController != null)
            {
                var clips = anim.runtimeAnimatorController.animationClips;
                Debug.Log($"[Debug] AnimationClips ({clips.Length}개):");
                foreach (var c in clips)
                    Debug.Log($"  - {c.name}");
            }

            Debug.Log("[Debug] ──── 확인 완료 ────");
        }
    }
}
