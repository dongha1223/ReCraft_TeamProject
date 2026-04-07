using System.Collections;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color _flashColor = Color.white;
    [SerializeField] private float _flashTime  = 0.25f;

    private SpriteRenderer[] _spriteRenderers;
    private Material[]       _materials;
    private Coroutine        _damageFlashCoroutine;

    // ── 상태이상 틴트 상태 ────────────────────────────────────────────
    private bool  _hasStatusTint;
    private Color _statusTintColor;
    private float _statusTintAmount;

    private void Awake()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        Init();
    }

    private void Init()
    {
        _materials = new Material[_spriteRenderers.Length];
        for (int i = 0; i < _spriteRenderers.Length; i++)
            _materials[i] = _spriteRenderers[i].material;
    }

    // ── 피격 플래시 ───────────────────────────────────────────────────

    public void CallDamageFlash()
    {
        if (_damageFlashCoroutine != null)
            StopCoroutine(_damageFlashCoroutine);
        _damageFlashCoroutine = StartCoroutine(DamageFlasher());
    }

    private IEnumerator DamageFlasher()
    {
        SetColor(_flashColor);

        float elapsedTime = 0f;
        while (elapsedTime < _flashTime)
        {
            elapsedTime += Time.deltaTime;
            SetAmount(Mathf.Lerp(1f, 0f, elapsedTime / _flashTime));
            yield return null;
        }

        // 플래시 종료 후 활성 상태이상 틴트 복원
        if (_hasStatusTint)
            ApplyStatusTint();
    }

    // ── 상태이상 틴트 (지속) ──────────────────────────────────────────

    /// <summary>
    /// 상태이상이 걸리는 동안 유지되는 색상 틴트를 설정한다.
    /// amount: 0 = 원본, 1 = 완전히 틴트 색 (테스트용 기본값 0.5)
    /// </summary>
    public void SetStatusTint(Color color, float amount = 0.5f)
    {
        _hasStatusTint    = true;
        _statusTintColor  = color;
        _statusTintAmount = amount;
        ApplyStatusTint();
    }

    /// <summary>상태이상 해제 시 틴트를 제거한다.</summary>
    public void ClearStatusTint()
    {
        _hasStatusTint = false;
        SetAmount(0f);
    }

    private void ApplyStatusTint()
    {
        SetColor(_statusTintColor);
        SetAmount(_statusTintAmount);
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────

    private void SetColor(Color color)
    {
        for (int i = 0; i < _materials.Length; i++)
            _materials[i].SetColor("_FlashColor", color);
    }

    private void SetAmount(float amount)
    {
        for (int i = 0; i < _materials.Length; i++)
            _materials[i].SetFloat("_FlashAmount", amount);
    }
}

