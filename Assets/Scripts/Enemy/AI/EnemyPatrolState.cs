using UnityEngine;

public sealed class EnemyPatrolState : IEnemyState
{
    private readonly EnemyStateMachine context;
    private Vector2 patrolTarget;
    private float waitTimer;
    private bool isWaiting;

    public EnemyPatrolState(EnemyStateMachine context)
    {
        this.context = context;
    }

    public void Enter()
    {
        isWaiting = false;
        waitTimer = 0f;
        PickNewPatrolPoint();
        context.Animation.SetState(EnemyAnimState.Walk);
    }

    public void Execute()
    {
        float distanceToPlayer = context.DistanceToPlayer();
        if (distanceToPlayer < context.Config.detectionRange)
        {
            context.ChangeState(new EnemyChaseState(context));
            return;
        }

        if (isWaiting)
        {
            context.Animation.SetState(EnemyAnimState.Idle);
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                PickNewPatrolPoint();
                context.Animation.SetState(EnemyAnimState.Walk);
            }
            return;
        }

        float distanceToTarget = Vector2.Distance(context.transform.position, patrolTarget);
        if (distanceToTarget < 0.5f)
        {
            context.StopMovement();
            isWaiting = true;
            waitTimer = context.Config.patrolWaitTime;
            return;
        }

        context.MoveToward(patrolTarget, context.Config.moveSpeed);
        Vector2 moveDir = context.GetMovementDirection();
        if (moveDir.sqrMagnitude > 0.01f)
        {
            context.Animation.SetDirection(moveDir);
        }
        else
        {
            context.Animation.SetDirection(patrolTarget - (Vector2)context.transform.position);
        }
    }

    public void Exit()
    {
        context.StopMovement();
    }

    private void PickNewPatrolPoint()
    {
        Vector2 basePos = context.BaseTransform != null
            ? (Vector2)context.BaseTransform.position
            : (Vector2)context.transform.position;

        float radius = context.Config.patrolRadius;
        Vector2 randomOffset = Random.insideUnitCircle * radius;
        patrolTarget = basePos + randomOffset;
    }
}
