using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// Phase 2 플랫폼의 오벨리스크.
    /// IInteractable을 구현해 F키 상호작용으로 플랫폼을 순환 이동한다.
    /// 이동 순서: 1 → 2 → 3 → 4 → 1 (현재 플랫폼 기준 다음 인덱스)
    /// 쿨타임 5초 동안 재상호작용 불가.
    /// </summary>
    public class ObeliskTeleporter : MonoBehaviour, IInteractable
    {
        [Header("설정")]
        [Tooltip("이 오벨리스크가 속한 플랫폼 인덱스")]
        [SerializeField] private int             _platformIndex;
        [Tooltip("PlatformManager 참조")]
        [SerializeField] private PlatformManager _platformManager;
        [Tooltip("순간이동 쿨타임 (초)")]
        [SerializeField] private float           _cooldown = 5f;

        [Header("이펙트")]
        [Tooltip("순간이동 이펙트 프리팹 (출발·도착 위치에 생성)")]
        [SerializeField] private GameObject _teleportEffectPrefab;

        private bool  _isOnCooldown;
        private float _cooldownTimer;

        // ── IInteractable ─────────────────────────────────────────────────

        /// <summary>이 플랫폼 위에 있고 쿨타임이 아닐 때만 상호작용 가능</summary>
        public bool CanInteract
            => !_isOnCooldown
               && _platformManager != null
               && _platformManager.CurrentPlatformIndex == _platformIndex;

        public void OnFocused()   { /* TODO: 상호작용 가능 UI 표시 */ }
        public void OnUnfocused() { /* TODO: 상호작용 가능 UI 숨김 */ }

        public void OnInteract(PlayerStatController statController)
        {
            if (!CanInteract) return;

            int nextIndex = (_platformIndex + 1) % _platformManager.PlatformCount;

            // 출발 이펙트
            SpawnEffect(transform.position);

            // 플레이어 이동
            _platformManager.TeleportPlayerToPlatform(nextIndex);

            // 도착 이펙트
            var dest = _platformManager.GetObelisk(nextIndex);
            if (dest != null) SpawnEffect(dest.position);

            // 쿨타임 시작
            StartCoroutine(CooldownCoroutine());
        }

        // ── 쿨타임 ────────────────────────────────────────────────────────

        private IEnumerator CooldownCoroutine()
        {
            _isOnCooldown = true;
            _cooldownTimer = _cooldown;

            while (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
                yield return null;
            }

            _isOnCooldown  = false;
            _cooldownTimer = 0f;
        }

        /// <summary>쿨타임 남은 시간 (UI 표시 등에 사용)</summary>
        public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);
        public bool  IsOnCooldown      => _isOnCooldown;

        // ── 유틸 ─────────────────────────────────────────────────────────

        private void SpawnEffect(Vector3 pos)
        {
            if (_teleportEffectPrefab != null)
                Instantiate(_teleportEffectPrefab, pos, Quaternion.identity);
        }
    }
}
