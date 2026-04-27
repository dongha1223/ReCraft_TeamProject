using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 스킬 커스텀 실행 로직의 추상 기반 클래스.
    /// TagTechniqueBehaviour와 동일한 패턴.
    ///
    /// 주의:
    ///   ScriptableObject는 StartCoroutine 불가.
    ///   실제 실행은 FormSkillController(MonoBehaviour)가 담당.
    ///   이 클래스는 로직 정의만 한다.
    ///
    /// 구현 예시:
    ///   SwordEnergySkillBehaviour  — 검기 발산 (상·하 투사체 2발)
    ///   RollingSlashBehaviour      — 롤링 슬래쉬 (이동 + 판정 3회)
    /// </summary>
    public abstract class SkillBehaviour : ScriptableObject
    {
        /// <summary>
        /// 스킬 실행 로직.
        /// FormSkillController가 StartCoroutine으로 호출한다.
        /// </summary>
        public abstract IEnumerator Execute(SkillContext ctx);
    }
}
