using UnityEngine;
using UnityEditor;
using System.IO;

namespace _2D_Roguelike
{
    /// <summary>
    /// 스토리 NPC 5명의 DialogueData SO 생성 + 대사 일괄 입력.
    /// 메뉴: Tools > 2D Roguelike > Setup Story NPC Dialogues
    /// 이미 존재하는 SO는 대사만 덮어씌운다 (Effect 연결은 유지).
    /// </summary>
    public static class NpcDialogueSetup
    {
        private const string _soPath = "Assets/Scripts/Core/Items/NpcSO";
        private const string _effectPath = "Assets/Scripts/Core/Items/Data/NPC_Effects";

        [MenuItem("Tools/2D Roguelike/Setup Story NPC Dialogues")]
        public static void Setup()
        {
            SetupOren();
            SetupVeros();
            SetupPainter();
            SetupGareth();
            SetupLucius();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[NpcDialogueSetup] 5개 스토리 NPC 대사 설정 완료!");
            EditorUtility.DisplayDialog("완료", "5개 스토리 NPC 대사 설정 완료!", "확인");
        }

        // ── 오렌 (음유시인) ──────────────────────────────────────────
        private static void SetupOren()
        {
            var d = LoadOrCreate("Bard_Dialogue");
            var so = new SerializedObject(d);

            SetString(so, "_npcName", "음유시인 오렌");

            SetStringArray(so, "_lines", new[]
            {
                "\"오, 손님이네! 화염 지대의 영웅 카엘의 노래를 들어보겠소?\"",
                "\"한번 들어 보시오, 내가 직접 지은 거라오~!\""
            });

            so.FindProperty("_hasChoice").boolValue = true;
            SetString(so, "_choiceConfirmLabel", "선행");
            SetString(so, "_choiceNoLabel", "악행");

            // 선행 루트 반응 (.....틀린 부분이 있어.)
            SetStringArray(so, "_yesLines", new[]
            {
                "\"틀린 부분이요? 나는 생존한 병사들에게 직접 들은 내용으로 지은 거 다만..\"",
                "\"..마을?... 지나가기만 했소?\"",
                "\"......알겠소, 고치리다.\"",
                "\"..그렇다면, 당신이 카엘이오?\""
            });

            // 악행 루트 반응 (..듣기 싫어.)
            SetStringArray(so, "_noLines", new[]
            {
                "\"왜요? 훌륭한 영웅담인데ㅡ\"",
                "\"그 날 생존한 병사들에게 직접 들은 내용인..ㅡ\"",
                "\".............\"\n\"…당신이, 그 카엘이오?\""
            });

            // 재대화
            SetStringArray(so, "_yesRetalkLines", new[]
            {
                "\"시간 있으면, 언제든 오시오, 새 노래를 들려드리죠~!\""
            });
            SetStringArray(so, "_noRetalkLines", new[]
            {
                "\"앞으로 당신 같은 사람에겐 노래는 없소!!\""
            });

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(d);
            Debug.Log("[NpcDialogueSetup] 오렌 완료");
        }

        // ── 베로스 (노예 상인) ───────────────────────────────────────
        private static void SetupVeros()
        {
            var d = LoadOrCreate("SlaveTrader_Dialogue");
            var so = new SerializedObject(d);

            SetString(so, "_npcName", "노예 상인 베로스");

            SetStringArray(so, "_lines", new[]
            {
                "\"3번 라인 빨리 움직여!\"",
                "노예 A : \"발이 타고 있어요...;;\"",
                "\"그건 네 문제지! 빨리 건너면 안 타잖아!\""
            });

            so.FindProperty("_hasChoice").boolValue = true;
            SetString(so, "_choiceConfirmLabel", "선행");
            SetString(so, "_choiceNoLabel", "악행");

            // 선행 루트 (노예 해방)
            SetStringArray(so, "_yesLines", new[]
            {
                "\"당신, 뭐야? 여긴 내 사유 구역이야!\"",
                "\"다 계약된 녀석들이라고!\"",
                "\"이..이건 재산 침해야! 고소할 거야!\"",
                "노예 A : \"정말 감사합니다!, 저흰 이제 어디로 가야 합니까?\""
            });

            // 악행 루트 (재료 낚아채기)
            SetStringArray(so, "_noLines", new[]
            {
                "\"뭐, 뭐야 당신! 그거 이리 안 내놔?!\"",
                "\"가, 가져가! 하지만 나한테 손대면 당국에 신고한다고!\"",
                "노예 A : \"저, 저희는… 어떻게 되는 건가요…?\""
            });

            // 재대화
            SetStringArray(so, "_yesRetalkLines", new[]
            {
                "\"제발 날 내버려둬.. 저 녀석들은 더 이상 건들지 않을 거니까...!\"",
                "노예 A : \"덕분에 살았습니다. 우리 모두가 이 은혜를 잊지 않을 거에요..!\""
            });
            SetStringArray(so, "_noRetalkLines", new[]
            {
                "\"더 이상 보기 싫어!, 저리 가!!!!!\"",
                "노예 A : \"(원망스러움이 깃든 채 곁눈질 하고 있다)\""
            });

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(d);
            Debug.Log("[NpcDialogueSetup] 베로스 완료");
        }

        // ── 화가 (신비로운 화가) ─────────────────────────────────────
        private static void SetupPainter()
        {
            var d = LoadOrCreate("Painter_Dialogue");
            var so = new SerializedObject(d);

            SetString(so, "_npcName", "신비로운 화가");

            SetStringArray(so, "_lines", new[]
            {
                "\"잠깐, 당신을 그려줄게요. 잠깐 서 있어줄래요?\"",
                "\"그래요, 등만 봐도 알 것 같아요. 당신이 무엇인지 말이죠.\""
            });

            so.FindProperty("_hasChoice").boolValue = true;
            SetString(so, "_choiceConfirmLabel", "선행");
            SetString(so, "_choiceNoLabel", "악행");

            // 선행 루트 (..어떻게 말이지?)
            SetStringArray(so, "_yesLines", new[]
            {
                "\"그냥 서 있으면 돼요. 평소처럼.\"",
                "\"무거워 보여, 전부...\"",
                "\"나중의 당신이거나. 아니면 원래의 당신이거나.\"",
                "\"이제 가도 좋아요, 오래 서 있었잖아.\""
            });

            // 악행 루트 (필요 없어.)
            SetStringArray(so, "_noLines", new[]
            {
                "\"다 그리고 나서 판단해요.\"",
                "\"........다 그려내고 싶었는데…\""
            });

            // 재대화
            SetStringArray(so, "_yesRetalkLines", new[]
            {
                "\"또 왔네요, 얼굴은 아직 비어 있어 보여요..\"",
                "\"언제쯤 채워지려나..\""
            });
            SetStringArray(so, "_noRetalkLines", new[]
            {
                "\"...이제, 당신 같은 건 다시 못봐...\""
            });

            // Effect 연결
            var effectSo = new SerializedObject(d);
            var noProp = effectSo.FindProperty("_noEffects");
            if (noProp != null && noProp.arraySize == 0)
            {
                var painterEffect = AssetDatabase.LoadAssetAtPath<StatModifierEffectDefinition>(
                    $"{_effectPath}/Painter/Painter_No_AttackPower.asset");
                if (painterEffect != null)
                {
                    noProp.arraySize = 1;
                    noProp.GetArrayElementAtIndex(0).objectReferenceValue = painterEffect;
                }
            }

            so.ApplyModifiedProperties();
            effectSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(d);
            Debug.Log("[NpcDialogueSetup] 화가 완료");
        }

        // ── 가레스 (선대 비질란테) ────────────────────────────────────
        private static void SetupGareth()
        {
            var d = LoadOrCreate("VeteranVigilante_Dialogue");
            var so = new SerializedObject(d);

            SetString(so, "_npcName", "선대 비질란테 가레스");

            SetStringArray(so, "_lines", new[]
            {
                "\"...왔군. 여기까지 올 줄은 몰랐어.\"",
                "\"받거라, 나는 이제 못 쓰겠어.\""
            });

            so.FindProperty("_hasChoice").boolValue = true;
            SetString(so, "_choiceConfirmLabel", "선행");
            SetString(so, "_choiceNoLabel", "악행");

            // 선행 루트 (그 말, 기억해요?)
            SetStringArray(so, "_yesLines", new[]
            {
                "\"…무슨.. 말을..?\"",
                "카엘 : \"분노는 연료라고요. 쓰면 앞으로 간다고.\"",
                "\"…그런 말을 했었나.\"",
                "카엘 : \"그 덕에 제가 여기까지 왔어요.\"",
                "\"…그렇다면...\""
            });

            // 악행 루트 (한심하군.)
            SetStringArray(so, "_noLines", new[]
            {
                "\"…그래. 한심하지.\"",
                "\"카엘, 그 '분노'라는 것이 어디로 가는지 알고 있나?\"",
                "카엘 : \"..당연히 그 악마 자식 아닙니까?\"",
                "\"…네 생각대로면 좋으련만...\""
            });

            // 재대화
            SetStringArray(so, "_yesRetalkLines", new[]
            {
                "\"..길을 잃지는 않은 것 같군.\"",
                "\"정말 다행이야.\""
            });
            SetStringArray(so, "_noRetalkLines", new[]
            {
                "\"...아직 그 방향인가...... 그럼, 더 할 말 없네...\""
            });

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(d);
            Debug.Log("[NpcDialogueSetup] 가레스 완료");
        }

        // ── 루시우스 (신성기사) ──────────────────────────────────────
        private static void SetupLucius()
        {
            var d = LoadOrCreate("HolyKnight_Dialogue");
            var so = new SerializedObject(d);

            SetString(so, "_npcName", "신성기사 루시우스");

            SetStringArray(so, "_lines", new[]
            {
                "\"왔군요. 카엘.\"",
                "\"전 당신에 대해 알고 있죠. 그리고 그날 일도.\"",
                "\"...저는 심판하러 온 게 아니에요. 들을 준비가 되어 있어요.\""
            });

            so.FindProperty("_hasChoice").boolValue = true;
            SetString(so, "_choiceConfirmLabel", "선행");
            SetString(so, "_choiceNoLabel", "악행");

            // 선행 루트 (...들어 줄 수 있나?)
            SetStringArray(so, "_yesLines", new[]
            {
                "\"…앉으시죠.\"",
                "카엘 : \"...그날 밤, 나는 부관을 잃었고 이성을 잃었어. 항복한 자를 베고, 마을을 불태웠어.\"",
                "카엘 : \"멈추지....않았어.\"",
                "\"…계속 하세요.\"",
                "카엘 : \"그 악마를 잡으러 온 건 속죄가 아니야...\"",
                "카엘 : \"멈추는 순간… 그것들이 보이니까.\"",
                "카엘 : \"그래서...계속 걷는 거야...\"",
                "\"…알겠어요. 그것으로 충분해요.\""
            });

            // 악행 루트 (그건 무용담일 뿐이야.)
            SetStringArray(so, "_noLines", new[]
            {
                "\"…그렇게 믿고 있군요.\"",
                "카엘 : \"아니, 사실이야....믿고 있는 게 아니라고!\"",
                "\"그게 진실이라 생각 하는 건가요?\"",
                "\"베른과 그의 딸들도 그렇게 생각할까요?\"",
                "\"….고해는… 죽은 자를 위한 게 아니에요… 남은 자를… 위한 거죠…\""
            });

            // 재대화
            SetStringArray(so, "_yesRetalkLines", new[]
            {
                "\"다시 오셨군요.\"",
                "\"..고해성사는 한 번으로 끝나지 않죠.\"",
                "\"여유 있다면, 언제든지 와요. 들어드릴 테니.\""
            });
            SetStringArray(so, "_noRetalkLines", new[]
            {
                "(쓰러진 채)",
                "\"…아직도, 인정하지 않는군요.\""
            });

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(d);
            Debug.Log("[NpcDialogueSetup] 루시우스 완료");
        }

        // ── 유틸 ─────────────────────────────────────────────────────

        private static DialogueData LoadOrCreate(string fileName)
        {
            string path = $"{_soPath}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (existing != null) return existing;

            // 새로 생성 (화가처럼 SO가 없을 경우)
            var newSo = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(newSo, path);
            Debug.Log($"[NpcDialogueSetup] 새 SO 생성: {path}");
            return newSo;
        }

        private static void SetString(SerializedObject so, string prop, string value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.stringValue = value;
        }

        private static void SetStringArray(SerializedObject so, string prop, string[] values)
        {
            var p = so.FindProperty(prop);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }
}
