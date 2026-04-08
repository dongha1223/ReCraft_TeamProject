using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    /// <summary>
    /// Q키 입력을 받아 태그 교체 흐름을 제어한다.
    ///
    /// 흐름:
    ///   Q키 → 토큰 확인 → ConsumeAll() → 레벨 결정
    ///   → TagTechniqueExecutor.Execute() (공격 + 무적 + 후딜)
    ///   → FormManager.SwapSlots() (폼 교체)
    ///   → ApplyTempBuff() (버프, 있으면)
    /// </summary>
    public class TagController : MonoBehaviour
    {
        private TagTokenBank         _tokenBank;
        private FormManager          _formManager;
        private TagTechniqueExecutor _executor;

        private bool _isBusy;

        private void Awake()
        {
            _tokenBank   = GetComponent<TagTokenBank>();
            _formManager = GetComponent<FormManager>();
            _executor    = GetComponent<TagTechniqueExecutor>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || _isBusy) return;
            if (kb.qKey.wasPressedThisFrame)
                TryTag();
        }

        private void TryTag()
        {
            if (!_tokenBank.HasAny)
            {
                Debug.Log("[TagController] 토큰 부족 — 교체 불가");
                return;
            }
            if (_formManager.Standby == null)
            {
                Debug.Log("[TagController] 대기 슬롯 비어있음 — 교체 불가");
                return;
            }

            StartCoroutine(TagRoutine());
        }

        private IEnumerator TagRoutine()
        {
            _isBusy = true;

            // 1. 토큰 전량 소비 → 레벨 결정 (1~3)
            int level = _tokenBank.ConsumeAll();

            // 2. 진입 폼의 해당 레벨 교체기 가져오기
            var techniques = _formManager.Standby.TagTechniques;
            if (techniques == null || techniques.Length < level || techniques[level - 1] == null)
            {
                Debug.LogWarning($"[TagController] 진입 폼 '{_formManager.Standby.DisplayName}'에 " +
                                 $"{level}단계 TagTechniqueDefinition 없음");
                _isBusy = false;
                yield break;
            }

            var definition = techniques[level - 1];
            Debug.Log($"[TagController] {level}단계 교체기 발동: {definition.name}");

            // 3. 교체기 실행 (공격 + 무적 + 후딜 전부 여기서 보장)
            yield return StartCoroutine(_executor.Execute(definition));

            // 4. 폼 교체
            _formManager.SwapSlots();

            // 5. 버프 적용 (있으면 — 폼 교체 완료 후 별도 코루틴)
            if (definition.HasBuff)
                StartCoroutine(_executor.ApplyTempBuff(definition));

            _isBusy = false;
        }
    }
}
