using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 교체기 커스텀 실행 로직의 추상 기반 클래스.
    /// 이 클래스를 상속해서 직업별 고유 교체기 로직을 구현한다.
    ///
    /// 사용법:
    ///   1. TagTechniqueBehaviour를 상속한 구체 클래스 작성
    ///   2. [CreateAssetMenu]로 에셋 생성
    ///   3. TagTechniqueDefinition.Behaviour 슬롯에 연결
    ///
    /// 주의:
    ///   ScriptableObject는 StartCoroutine 불가.
    ///   실제 실행은 TagTechniqueExecutor(MonoBehaviour)가 담당.
    ///   이 클래스는 로직 정의만 한다.
    ///
    /// ※ Duration 설정 권고:
    ///   Behaviour는 TagTechniqueDefinition.Duration 경과 후
    ///   폼 체인지와 무관하게 독립 실행된다 (비블로킹).
    ///
    ///   스프라이트 프레임 재생 / 오브젝트 이동 / 카메라 연출 등
    ///   "눈에 보이는 시간이 있는 연출"을 포함하는 경우,
    ///   Definition.Duration을 연출 총 시간에 맞춰 설정할 것.
    ///   Duration &lt; 연출 시간이면 폼 체인지가 연출 도중 발생한다.
    ///
    /// 예시 구체 클래스:
    ///   DashAttackBehaviour      — 전방 돌진 후 범위 공격 (Duration = 돌진 시간)
    ///   JumpSlamBehaviour        — 점프 후 낙하 내려찍기 (Duration = 낙하 완료 시간)
    ///   WarriorTagTech3Behaviour — 전체 범위 슬래시 체인 (Duration = 원하는 폼 체인지 타이밍)
    /// </summary>
    public abstract class TagTechniqueBehaviour : ScriptableObject
    {
        /// <summary>
        /// 교체기 실행 로직.
        /// TagTechniqueExecutor가 StartCoroutine으로 호출한다.
        /// </summary>
        public abstract IEnumerator Execute(TagTechniqueContext ctx);
    }
}
