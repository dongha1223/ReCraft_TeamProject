using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 스테이지 클리어 시 아이템 드랍 테이블을 구성하고 결과를 반환하는 정적 유틸리티.
    /// - dropThemes가 비어있는 아이템은 모든 테마에서 드랍 가능 (공용 아이템)
    /// - dropThemes에 현재 테마가 포함된 아이템만 드랍 풀에 진입
    /// - baseDropWeight 기반 가중치 랜덤, 중복 없이 count개 반환
    /// </summary>
    public static class DropSystem
    {
        /// <summary>
        /// 드랍 아이템 목록을 반환한다.
        /// </summary>
        /// <param name="database">전체 아이템 DB</param>
        /// <param name="theme">현재 스테이지 테마 (Start/Shop/Boss면 None 전달)</param>
        /// <param name="count">선택지로 보여줄 아이템 수</param>
        /// <returns>가중치 랜덤으로 선정된 아이템 목록 (count 이하)</returns>
        public static List<ItemDefinition> RollDrops(ItemDatabaseSO database, MapTheme theme, int count)
        {
            if (database == null || database.items == null || count <= 0)
                return new List<ItemDefinition>();

            // 1. 드랍 풀 필터링
            var pool = BuildPool(database.items, theme);

            // 2. 풀이 부족하면 count 조정
            int pickCount = Mathf.Min(count, pool.Count);
            var result = new List<ItemDefinition>(pickCount);

            // 3. 가중치 랜덤, 중복 없이 선택
            // 선택된 항목을 맨 뒤와 swap 후 Count 감소 → O(1) 제거 (RemoveAt O(n) 회피)
            int remaining = pool.Count;
            for (int i = 0; i < pickCount; i++)
            {
                int index = WeightedRandom(pool, remaining);
                result.Add(pool[index].item);

                remaining--;
                pool[index] = pool[remaining]; // 선택된 슬롯에 마지막 항목 덮어쓰기
            }

            return result;
        }

        // ── 내부 ──────────────────────────────────────────────────────────────

        private struct WeightedEntry
        {
            public ItemDefinition item;
            public float weight;
        }

        private static List<WeightedEntry> BuildPool(ItemDefinition[] allItems, MapTheme theme)
        {
            var pool = new List<WeightedEntry>(allItems.Length);

            foreach (var item in allItems)
            {
                if (item == null || item.baseDropWeight <= 0f) continue;

                // dropThemes가 null/비어있으면 공용 아이템
                // None만 들어있어도 공용, 현재 테마와 일치하는 항목이 있으면 themeMatch
                bool isCommon    = item.dropThemes == null || item.dropThemes.Length == 0;
                bool themeMatch  = false;

                if (!isCommon)
                {
                    isCommon = true; // None만 있는지 검사 시작
                    foreach (var t in item.dropThemes)
                    {
                        if (t == theme)        themeMatch = true;
                        if (t != MapTheme.None) isCommon  = false;
                    }
                }

                if (isCommon || themeMatch)
                    pool.Add(new WeightedEntry { item = item, weight = item.baseDropWeight });
            }

            return pool;
        }

        private static int WeightedRandom(List<WeightedEntry> pool, int count)
        {
            float total = 0f;
            for (int i = 0; i < count; i++) total += pool[i].weight;

            float roll       = Random.Range(0f, total);
            float cumulative = 0f;

            for (int i = 0; i < count; i++)
            {
                cumulative += pool[i].weight;
                if (roll <= cumulative) return i;
            }

            return count - 1; // 부동소수점 오차 보정
        }
    }
}
