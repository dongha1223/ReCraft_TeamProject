using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// Physics2D 쿼리 결과(Collider2D[])에서 유효한 IDamageable 목록을 추출하는 정적 유틸리티.
    ///
    /// 처리하는 문제:
    ///   - 보스처럼 콜라이더가 여러 개인 대상의 중복 적중 방지
    ///   - 이미 사망한 대상 제외
    ///   - LoS(시야) 차단 옵션 지원
    /// </summary>
    public static class TargetCollector2D
    {
        /// <summary>
        /// Collider2D 배열에서 유효한 IDamageable을 중복 없이 수집한다.
        /// </summary>
        /// <param name="colliders">Physics2D 쿼리 결과</param>
        /// <param name="requireLoS">true면 origin → 대상 사이의 시야를 체크한다</param>
        /// <param name="origin">시야 체크 기준 위치</param>
        /// <param name="obstacleLayer">시야를 막는 레이어 (requireLoS = true일 때만 사용)</param>
        public static List<IDamageable> CollectUnique(
            Collider2D[] colliders,
            bool         requireLoS    = false,
            Vector2      origin        = default,
            LayerMask    obstacleLayer = default)
        {
            var seen    = new HashSet<IDamageable>();
            var results = new List<IDamageable>(colliders.Length);

            foreach (var col in colliders)
            {
                if (col == null) continue;

                // 콜라이더 루트에서 IDamageable 탐색 (히트박스가 자식에 붙어 있는 경우 대응)
                var dmg = col.GetComponentInParent<IDamageable>();
                if (dmg == null) continue;

                // 이미 처리한 대상 또는 사망한 대상 제외
                if (!seen.Add(dmg)) continue;
                if (dmg.IsDead) continue;

                // 시야 차단 체크
                if (requireLoS)
                {
                    Vector2 targetPos = col.transform.position;
                    if (Physics2D.Linecast(origin, targetPos, obstacleLayer).collider != null)
                        continue;
                }

                results.Add(dmg);
            }

            return results;
        }
    }
}
