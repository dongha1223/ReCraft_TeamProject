using System.Collections;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color _flashColor = Color.white;
    [SerializeField] private float _flashTime = 0.25f;

    private SpriteRenderer[] _spriteRenderers;
    private Material[] _materials;
    private Coroutine _damageFlashCoroutine;

    private void Awake()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        Init();
    }

    private void Init()
    {
        _materials = new Material[_spriteRenderers.Length];

        for(int i = 0 ; i < _spriteRenderers.Length ; i++)
        {
            _materials[i] = _spriteRenderers[i].material;
        }
    }

    public void CallDamageFlash()
    {
        _damageFlashCoroutine = StartCoroutine(DamageFlasher());
    }
    private IEnumerator DamageFlasher()
    {
       //set the color
        setFlashColor();

        //lerp the flash amount
        float currentFlashAmount = 0f;
        float elapsedTime = 0f;
        while(elapsedTime < _flashTime)
        {
            elapsedTime += Time.deltaTime;

            currentFlashAmount = Mathf.Lerp(1f, 0f, (elapsedTime / _flashTime));
            setFlashAmount(currentFlashAmount);

            yield return null;
        }
    }

    private void setFlashColor()
    {
        for(int i = 0 ; i < _materials.Length ; i++)
        {
            _materials[i].SetColor("_FlashColor", _flashColor);
        }
    }

    private void setFlashAmount(float amount)
    {
        for(int i = 0 ; i < _materials.Length ; i++)
        {
            _materials[i].SetFloat("_FlashAmount", amount);
        }
    }

}
