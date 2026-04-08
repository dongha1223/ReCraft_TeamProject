using _2D_Roguelike;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHPUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] Image _hpFillImage;
    [SerializeField] EnemyStats _EnemyStats;

    void Awake()
    {
        //_EnemyStats = GetComponent<EnemyStats>();
    }

    void Update()
    {
        SyncStatUI();
    }

    void SyncStatUI()
    {
        if (_EnemyStats == null) return;

        float _targetHPFill = _EnemyStats.getCurrnetHP() / _EnemyStats.getMaxHP();

        _hpFillImage.fillAmount = Mathf.Lerp(_hpFillImage.fillAmount, _targetHPFill, Time.deltaTime * 5);
    }
}
