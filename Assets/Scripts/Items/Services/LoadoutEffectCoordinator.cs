using System.Collections.Generic;

namespace _2D_Roguelike
{
    /// <summary>
    /// 장착 변경 시 아이템 효과와 각인 효과를 전체 재빌드하는 오케스트레이터.
    ///
    /// 재빌드 순서:
    ///   1. 기존 적용 중인 효과 전부 제거
    ///   2. 장착 아이템 고유 효과 적용
    ///   3. 각인 카운트 재계산
    ///   4. 활성화된 각인 단계 효과 적용
    /// </summary>
    public class LoadoutEffectCoordinator
    {
        private readonly EffectService           _effectService;
        private readonly InscriptionService      _inscriptionService;
        private readonly InscriptionTierResolver _tierResolver;
        private readonly StatService             _statService;
        private readonly OnHitStatusRegistry     _onHitRegistry;

        private readonly List<AppliedEffectHandle> _activeItemEffects        = new();
        private readonly List<AppliedEffectHandle> _activeInscriptionEffects = new();

        public LoadoutEffectCoordinator(
            EffectService            effectService,
            InscriptionService       inscriptionService,
            InscriptionTierResolver  tierResolver,
            StatService              statService,
            OnHitStatusRegistry      onHitRegistry = null)
        {
            _effectService      = effectService;
            _inscriptionService = inscriptionService;
            _tierResolver       = tierResolver;
            _statService        = statService;
            _onHitRegistry      = onHitRegistry;
        }

        /// <summary>
        /// 장착 목록이 바뀔 때마다 호출한다.
        /// 기존 효과를 전부 지우고 현재 장착 상태 기준으로 다시 계산한다.
        /// </summary>
        public void Rebuild(IReadOnlyList<ItemInstance> equippedItems)
        {
            ClearAll();
            ApplyItemEffects(equippedItems);

            _inscriptionService.RebuildFromEquipped(equippedItems);

            // 장착 아이템에 등장하는 각인 종류만 추출해서 처리
            var inscriptions = CollectInscriptions(equippedItems);
            ApplyInscriptionEffects(inscriptions);
        }

        // ── private ──────────────────────────────────────────────────

        /// <summary>EffectContext 생성 헬퍼 — OnHitRegistry를 항상 포함한다.</summary>
        private EffectContext CreateContext(string sourceId)
            => new EffectContext(sourceId, _statService, _onHitRegistry);

        private void ApplyItemEffects(IReadOnlyList<ItemInstance> equippedItems)
        {
            foreach (var item in equippedItems)
            {
                if (item.Definition.effects == null) continue;

                var ctx = CreateContext(item.InstanceId);
                foreach (var effect in item.Definition.effects)
                {
                    if (effect == null) continue;
                    var handle = _effectService.Apply(ctx, effect);
                    _activeItemEffects.Add(handle);
                }
            }
        }

        private void ApplyInscriptionEffects(HashSet<InscriptionDefinition> inscriptions)
        {
            foreach (var inscription in inscriptions)
            {
                int count       = _inscriptionService.GetCount(inscription);
                var activeTiers = _tierResolver.GetActiveTiers(inscription, count);

                foreach (var tier in activeTiers)
                {
                    if (tier.effects == null) continue;

                    // 각인 단계별 고유 sourceId로 독립 추적
                    string sourceId = $"inscription-{inscription.inscriptionId}-tier-{tier.requiredCount}";
                    var ctx = CreateContext(sourceId);

                    foreach (var effect in tier.effects)
                    {
                        if (effect == null) continue;
                        var handle = _effectService.Apply(ctx, effect);
                        _activeInscriptionEffects.Add(handle);
                    }
                }
            }
        }

        private void ClearAll()
        {
            foreach (var handle in _activeItemEffects)
            {
                var ctx = CreateContext(handle.SourceId);
                _effectService.Remove(ctx, handle);
            }
            _activeItemEffects.Clear();

            foreach (var handle in _activeInscriptionEffects)
            {
                var ctx = CreateContext(handle.SourceId);
                _effectService.Remove(ctx, handle);
            }
            _activeInscriptionEffects.Clear();
        }

        /// <summary>장착 아이템에서 등장하는 각인 종류를 중복 없이 추출</summary>
        private static HashSet<InscriptionDefinition> CollectInscriptions(
            IReadOnlyList<ItemInstance> equippedItems)
        {
            var result = new HashSet<InscriptionDefinition>();
            foreach (var item in equippedItems)
            {
                if (item.Definition.inscriptions == null) continue;
                foreach (var entry in item.Definition.inscriptions)
                {
                    if (entry.inscription != null)
                        result.Add(entry.inscription);
                }
            }
            return result;
        }
    }
}
