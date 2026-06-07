using UnityEngine;

public enum ProjectileType { Straight, Homing, QuadraticHoming, CubicHoming }

public class ProjectileEmisson : MonoBehaviour
{
    [SerializeField] private ProjectileType projectileType   = ProjectileType.Straight;
    [SerializeField] private int            projectileCount  = 3;
    [SerializeField] private float          cooldownTime     = 2f;
    [SerializeField] private GameObject[]   projectiles;
    [SerializeField] private Transform      projectileSpawnPoint;
    [SerializeField] private Transform      target;

    private int   _currentIndex;
    private float _lastFireTime;
    private float _lastCooldownTime;

    private const float FireInterval = 0.05f;

    public bool IsSkillAvailable => Time.time - _lastCooldownTime > cooldownTime;

    private void Update() => OnSkill();

    public void OnSkill()
    {
        if (!IsSkillAvailable) return;
        if (Time.time - _lastFireTime <= FireInterval) return;

        Instantiate(projectiles[(int)projectileType], projectileSpawnPoint.position, Quaternion.identity);
        _currentIndex++;
        _lastFireTime = Time.time;

        if (_currentIndex >= projectileCount)
        {
            _currentIndex    = 0;
            _lastCooldownTime = Time.time;
        }
    }
}
