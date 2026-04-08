using System.Drawing;
using _2D_Roguelike;
using UnityEngine;

public class ProjectileCubicHoming : ProjectileBase
{
    private Vector2     start, end, point1 ,point2;
    private float       duration, t = 0f;
    private Transform   target;

    public override void Setup(Transform target, HitInfo info, int maxCount = 1, int index = 0)
    {
        base.Setup(target, info);

        this.target = target;
        start       = transform.position;
        end         = this.target.position;

        // 시작 지점에서 목표까지의 거리 계산
        float distance = Vector3.Distance(start, end);
        // 거리 / 속도 = 시간 으로 시간 계산
        duration = distance / movementRigidBody2D.MoveSpeed;

        //모든 발사체의 포인트가 동일하게 45도 각도 위치에 있도록 설정
        //float angle = 45;

        // 순번에 따른 일정한 각도의 원형으로 위치 설정
        float angle = 360 / maxCount * index;

        // 순번에 따라 위 or 아래 대각선 (45, 315 도) 위치로 설정
        //float angle = 45 + 270 * (index % 2);

        //현재 플레이어의 회전 값 적용을 위해서 angle값에 더해줌
        angle += MathUtil.GetAngleFromPosition(start, end);

        //시작지점에서 목표 지점사이의 angle 각도로 30% 떨어진 위치
        point1 = MathUtil.GetNewPoint(start, angle, distance * 0.3f);
        point2 = MathUtil.GetNewPoint(start, angle * -1, distance * 0.7f);

    }

    public override void Process()
    {
        end = target.position;
        //duration시간 동안 t가 점진적으로 증가
        t += Time.deltaTime / duration;
        transform.position = MathUtil.CubicCurve(start, point1, point2, end, t);
    }
}
