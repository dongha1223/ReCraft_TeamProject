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
    /// 예시 구체 클래스:
    ///   DashAttackBehaviour  — 전방 돌진 후 범위 공격
    ///   JumpSlamBehaviour    — 점프 후 낙하 내려찍기
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
