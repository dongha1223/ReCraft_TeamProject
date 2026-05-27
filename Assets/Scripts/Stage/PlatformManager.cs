using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// Phase 2 전용 플랫폼 4개를 관리한다.
    /// 플레이어가 플랫폼에 올라서면 Trigger로 감지해 현재 플랫폼 인덱스를 추적한다.
    /// BossPatternExecutor가 패턴 실행 시 스폰 위치를 요청하면 플랫폼 기준 좌표를 반환한다.
    /// </summary>
    public class PlatformManager : MonoBehaviour
    {
        [System.Serializable]
        public class PlatformData
        {
            [Tooltip("플랫폼 루트 오브젝트 (비활성 → 활성 전환에 사용)")]
            public GameObject platformRoot;
            [Tooltip("플레이어 순간이동 착지 위치")]
            public Transform  teleportTarget;
            [Tooltip("오벨리스크 Transform (ObeliskTeleporter 컴포넌트 포함)")]
            public Transform  obelisk;
            [Tooltip("검은색 하수인 스폰 위치 배열 (패턴 A용)")]
            public Transform[] blackMinionSpawnPoints;
            [Tooltip("하얀색 하수인 스폰 위치 배열 (패턴 B용)")]
            public Transform[] whiteMinionSpawnPoints;
        }

        [Header("플랫폼 설정 (인덱스 0~3)")]
        [SerializeField] private PlatformData[] _platforms;

        [Header("플레이어 감지")]
        [Tooltip("플레이어 레이어마스크 (플랫폼 위 플레이어 Trigger 감지용)")]
        [SerializeField] private LayerMask _playerLayer;

        private int _currentPlatformIndex = 0;

        public int   CurrentPlatformIndex => _currentPlatformIndex;
        public int   PlatformCount        => _platforms != null ? _platforms.Length : 0;

        // ── 활성화 / 비활성화 ─────────────────────────────────────────────

        /// <summary>Phase 2 시작 시 4개 플랫폼을 모두 활성화한다.</summary>
        public void ActivatePlatforms()
        {
            if (_platforms == null) return;
            foreach (var p in _platforms)
                p.platformRoot?.SetActive(true);
        }

        /// <summary>보스 사망 또는 Phase 종료 시 플랫폼을 비활성화한다.</summary>
        public void DisablePlatforms()
        {
            if (_platforms == null) return;
            foreach (var p in _platforms)
                p.platformRoot?.SetActive(false);
        }

        // ── 플레이어 이동 ─────────────────────────────────────────────────

        /// <summary>플레이어를 지정 인덱스 플랫폼의 착지 위치로 순간이동시킨다.</summary>
        public void TeleportPlayerToPlatform(int platformIndex)
        {
            if (!IsValidIndex(platformIndex)) return;

            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO == null) return;

            var target = _platforms[platformIndex].teleportTarget;
            if (target == null) return;

            playerGO.transform.position = target.position;
            _currentPlatformIndex = platformIndex;
        }

        // ── 스폰 위치 제공 ────────────────────────────────────────────────

        /// <summary>지정 플랫폼의 검은색 하수인 스폰 위치 배열을 반환한다 (패턴 A용).</summary>
        public Transform[] GetBlackMinionSpawnPoints(int platformIndex)
        {
            if (!IsValidIndex(platformIndex)) return System.Array.Empty<Transform>();
            return _platforms[platformIndex].blackMinionSpawnPoints ?? System.Array.Empty<Transform>();
        }

        /// <summary>지정 플랫폼의 하얀색 하수인 스폰 위치 배열을 반환한다 (패턴 B용).</summary>
        public Transform[] GetWhiteMinionSpawnPoints(int platformIndex)
        {
            if (!IsValidIndex(platformIndex)) return System.Array.Empty<Transform>();
            return _platforms[platformIndex].whiteMinionSpawnPoints ?? System.Array.Empty<Transform>();
        }

        /// <summary>지정 플랫폼의 오벨리스크 Transform을 반환한다.</summary>
        public Transform GetObelisk(int platformIndex)
        {
            if (!IsValidIndex(platformIndex)) return null;
            return _platforms[platformIndex].obelisk;
        }

        // ── 현재 플랫폼 갱신 (PlatformTrigger에서 호출) ──────────────────

        /// <summary>플레이어가 올라선 플랫폼 인덱스를 갱신한다.</summary>
        public void SetCurrentPlatform(int index)
        {
            if (IsValidIndex(index))
                _currentPlatformIndex = index;
        }

        // ── 유틸 ─────────────────────────────────────────────────────────

        private bool IsValidIndex(int index)
            => _platforms != null && index >= 0 && index < _platforms.Length;

        // ── Gizmos ────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_platforms == null) return;
            for (int i = 0; i < _platforms.Length; i++)
            {
                var p = _platforms[i];
                if (p.teleportTarget == null) continue;
                Gizmos.color = (i == _currentPlatformIndex)
                    ? new Color(0f, 1f, 0.5f, 0.8f)
                    : new Color(0.5f, 0.5f, 1f, 0.5f);
                Gizmos.DrawSphere(p.teleportTarget.position, 0.2f);
                UnityEditor.Handles.Label(p.teleportTarget.position + Vector3.up * 0.4f, $"P{i}");
            }
        }
#endif
    }
}
