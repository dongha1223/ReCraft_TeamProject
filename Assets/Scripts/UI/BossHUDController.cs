using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    public class BossHUDController : MonoBehaviour
    {
        private const float SlideShowY    = 10f;
        private const float SlideHideY    = -200f;
        private const float SlideDuration = 0.55f;

        private VisualElement _wrapper;
        private VisualElement _fill;

        private BossStats    _bossStats;
        private BossPartHP[] _bossParts;
        private float        _totalMaxHp;
        private float        _currentY;
        private float        _lastHpRatio = -1f;
        private bool         _bossActive;

        private Coroutine _slideCoroutine;

        private void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _wrapper  = root.Q("boss-hud-wrapper");
            _fill     = root.Q("boss-hp-bar-fill");

            _currentY              = SlideHideY;
            _wrapper.style.top     = SlideHideY;
        }

        private void OnEnable()
        {
            BossMainController.OnBossEngaged += HandleBossEngaged;
            BossMainController.OnBossDead    += HandleBossDead;
            BossController.OnBossEngaged     += HandleBossEngaged;
            BossController.OnBossDead        += HandleBossDead;
        }

        private void OnDisable()
        {
            BossMainController.OnBossEngaged -= HandleBossEngaged;
            BossMainController.OnBossDead    -= HandleBossDead;
            BossController.OnBossEngaged     -= HandleBossEngaged;
            BossController.OnBossDead        -= HandleBossDead;
        }

        private void HandleBossEngaged()
        {
            CacheBossHpSource();
            _lastHpRatio = -1f;
            _bossActive  = true;
            PlaySlide(SlideShowY);
        }

        private void CacheBossHpSource()
        {
            // BossController 보스 (BossStats 사용)
            var stats = FindFirstObjectByType<BossStats>();
            if (stats != null)
            {
                _bossStats  = stats;
                _bossParts  = null;
                _totalMaxHp = stats.MaxHp;
                return;
            }

            // BossMainController 보스 (파츠 합산 HP)
            _bossStats = null;
            var bmc = FindFirstObjectByType<BossMainController>();
            _bossParts  = bmc != null ? bmc.GetComponentsInChildren<BossPartHP>(true) : null;
            _totalMaxHp = 0f;
            if (_bossParts != null)
                foreach (var p in _bossParts) _totalMaxHp += p.GetMaxHP();
        }

        private void HandleBossDead()
        {
            PlaySlide(SlideHideY, clearOnDone: true);
        }

        private void Update()
        {
            if (!_bossActive) return;

            float ratio = GetHpRatio();
            if (Mathf.Approximately(ratio, _lastHpRatio)) return;

            _lastHpRatio      = ratio;
            _fill.style.width = new StyleLength(new Length(ratio * 100f, LengthUnit.Percent));
        }

        private float GetHpRatio()
        {
            if (_bossStats != null) return _bossStats.HpRatio;

            if (_bossParts != null && _totalMaxHp > 0f)
            {
                float cur = 0f;
                foreach (var p in _bossParts) cur += p.GetCurrentHP();
                return cur / _totalMaxHp;
            }

            return 1f;
        }

        private void PlaySlide(float targetY, bool clearOnDone = false)
        {
            if (_slideCoroutine != null)
                StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlideCoroutine(targetY, clearOnDone));
        }

        private IEnumerator SlideCoroutine(float targetY, bool clearOnDone)
        {
            float startY  = _currentY;
            float elapsed = 0f;

            while (elapsed < SlideDuration)
            {
                elapsed   += Time.deltaTime;
                _currentY  = Mathf.Lerp(startY, targetY, Mathf.SmoothStep(0f, 1f, elapsed / SlideDuration));
                _wrapper.style.top = _currentY;
                yield return null;
            }

            _currentY          = targetY;
            _wrapper.style.top = targetY;

            if (clearOnDone)
            {
                _bossActive  = false;
                _bossStats   = null;
                _bossParts   = null;
                _totalMaxHp  = 0f;
                _lastHpRatio = -1f;
            }

            _slideCoroutine = null;
        }
    }
}
