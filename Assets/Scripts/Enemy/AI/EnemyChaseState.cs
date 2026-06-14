using UnityEngine;

public sealed class EnemyChaseState : IEnemyState
{
    private readonly EnemyStateMachine context;
    private float surroundAngle;

    public EnemyChaseState(EnemyStateMachine context)
    {
        this.context = context;
    }

    public void Enter()
    {
        context.Animation.SetState(EnemyAnimState.Run);
        surroundAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        if (context.EnteredFromPatrol)
        {
            string clip = context.Config != null && context.Config.isBoss
                ? AudioClipId.SfxEnemyBossRoar
                : AudioClipId.SfxEnemyOrcAggro;
            AudioHooks.SfxAtPoint(clip, context.transform.position);
        }
    }

    public void Execute()
    {
        float distanceToPlayer = context.DistanceToPlayer();

        if (distanceToPlayer > context.Config.loseInterestRange)
        {
            context.ChangeState(new EnemyPatrolState(context));
            return;
        }

        if (distanceToPlayer <= context.Config.attackRange)
        {
            context.ChangeState(new EnemyAttackState(context));
            return;
        }

        // Run directly toward the player; separation handles spread
        Vector2 playerPos = context.PlayerTarget.position;
        context.MoveToward(playerPos, context.Config.chaseSpeed);

        Vector2 dirToPlayer = context.DirectionToPlayer();
        context.Animation.SetDirection(dirToPlayer);
    }

    public void Exit()
    {
        context.StopMovement();
    }
}
