using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

namespace _2D_Roguelike
{
    /// <summary>
    /// mage_sprites_96 기반 AnimationClip + AnimatorController 전체 재생성
    /// Tools > [96px] Rebuild Mage Animator
    /// </summary>
    public static class MageAnimatorSetup
    {
        private const string OUT_DIR     = "Assets/Animators/Mage";
        private const string SPRITE_ROOT = "Assets/mage_sprites_96/frames";
        private const float  FPS_IDLE    = 6f;
        private const float  FPS_RUN     = 8f;
        private const float  FPS_ATK     = 10f;
        private const float  FPS_FALL    = 8f;

        [MenuItem("Tools/[96px] Rebuild Mage Animator")]
        public static void Rebuild()
        {
            EnsureDir(OUT_DIR);

            // ── 클립 생성 ─────────────────────────────────────────────
            var idle  = MakeClip("mage_idle",    "idle",    FPS_IDLE, loop: true);
            var run   = MakeClip("mage_run",     "run",     FPS_RUN,  loop: true);
            var atk1  = MakeClip("mage_attack1", "attack1", FPS_ATK,  loop: false);
            var atk2  = MakeClip("mage_attack2", "attack2", FPS_ATK,  loop: false);
            var fall  = MakeClip("mage_fall",    "fall",    FPS_FALL, loop: true);

            // ── AnimatorController ────────────────────────────────────
            string ctrlPath = $"{OUT_DIR}/MageAnimator.controller";
            if (AssetDatabase.LoadAssetAtPath<Object>(ctrlPath) != null)
                AssetDatabase.DeleteAsset(ctrlPath);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Attack1",  AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Attack2",  AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Dash",     AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;

            var sIdle = sm.AddState("Idle");   sIdle.motion = idle;
            var sRun  = sm.AddState("Run");    sRun.motion  = run;
            var sAtk1 = sm.AddState("Attack1");sAtk1.motion = atk1;
            var sAtk2 = sm.AddState("Attack2");sAtk2.motion = atk2;
            var sFall = sm.AddState("Fall");   sFall.motion = fall;
            sm.defaultState = sIdle;

            // Idle <-> Run
            BoolTrans(sIdle, sRun,  "IsMoving", true);
            BoolTrans(sRun,  sIdle, "IsMoving", false);
            // AnyState -> 공격
            AnyTrig(sm, sAtk1, "Attack1");
            AnyTrig(sm, sAtk2, "Attack2");
            // 공격/낙하 -> Idle
            ExitTo(sAtk1, sIdle);
            ExitTo(sAtk2, sIdle);
            ExitTo(sFall, sIdle);

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MageAnimatorSetup-96] ✅ MageAnimator.controller 재생성 완료");
            EditorUtility.DisplayDialog("완료",
                "[96px] MageAnimator 재생성 완료!\n" +
                "Player > TagSystem > Mage Controller 필드를 확인해주세요.", "확인");
        }

        // ── 클립 생성 ─────────────────────────────────────────────────
        static AnimationClip MakeClip(string clipName, string action, float fps, bool loop)
        {
            string path = $"{OUT_DIR}/{clipName}.anim";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var clip = new AnimationClip { frameRate = fps };
            if (loop)
            {
                var s = AnimationUtility.GetAnimationClipSettings(clip);
                s.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, s);
            }

            var sprites = LoadSprites(action);
            if (sprites.Count > 0)
            {
                float dt = 1f / fps;
                var binding = new EditorCurveBinding
                {
                    type         = typeof(SpriteRenderer),
                    path         = "",
                    propertyName = "m_Sprite"
                };
                var keys = new ObjectReferenceKeyframe[sprites.Count + 1];
                for (int i = 0; i < sprites.Count; i++)
                    keys[i] = new ObjectReferenceKeyframe { time = i * dt, value = sprites[i] };
                keys[sprites.Count] = new ObjectReferenceKeyframe
                {
                    time  = sprites.Count * dt,
                    value = sprites[loop ? 0 : sprites.Count - 1]
                };
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            }

            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        // ── 스프라이트 로드 ───────────────────────────────────────────
        static List<Sprite> LoadSprites(string action)
        {
            string folder = $"{SPRITE_ROOT}/{action}";
            var    guids  = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            var    paths  = new List<string>();
            foreach (var g in guids) paths.Add(AssetDatabase.GUIDToAssetPath(g));
            paths.Sort();

            var result = new List<Sprite>();
            foreach (var p in paths)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (sp != null) result.Add(sp);
            }
            if (result.Count == 0)
                Debug.LogWarning($"[MageAnimatorSetup-96] '{folder}' 스프라이트 없음");
            return result;
        }

        // ── 트랜지션 헬퍼 ─────────────────────────────────────────────
        static void BoolTrans(AnimatorState from, AnimatorState to, string param, bool val)
        {
            var t = from.AddTransition(to);
            t.duration = 0; t.hasExitTime = false;
            t.AddCondition(val ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
        }

        static void AnyTrig(AnimatorStateMachine sm, AnimatorState to, string trig)
        {
            var t = sm.AddAnyStateTransition(to);
            t.duration = 0; t.hasExitTime = false; t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0, trig);
        }

        static void ExitTo(AnimatorState from, AnimatorState to)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = true; t.exitTime = 1f; t.duration = 0;
        }

        static void EnsureDir(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
