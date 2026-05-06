using UnityEngine;

public sealed class EnemyAttackState : IEnemyState
{
    private readonly EnemyStateMachine context;
    private float cooldownTimer;
    private bool attackReady;

    public EnemyAttackState(EnemyStateMachine context)
    {
        this.context = context;
    }

    public void Enter()
    {
        context.StopMovement();
        cooldownTimer = 0f;
        attackReady = true;

        Vector2 dirToPlayer = context.DirectionToPlayer();
        context.Animation.SetDirection(dirToPlayer);
        context.Animation.SetState(EnemyAnimState.Attack);
        context.Animation.OnAttackFrame += HandleAttackFrame;
    }

    public void Execute()
    {
        float distanceToPlayer = context.DistanceToPlayer();

        if (distanceToPlayer > context.Config.attackRange * 1.2f)
        {
            context.ChangeState(new EnemyChaseState(context));
            return;
        }

        Vector2 dirToPlayer = context.DirectionToPlayer();
        context.Animation.SetDirection(dirToPlayer);

        if (!attackReady)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                attackReady = true;
                context.Animation.SetState(EnemyAnimState.Attack);
            }
            else
            {
                context.Animation.SetState(EnemyAnimState.Idle);
            }
        }
    }

    public void Exit()
    {
        context.Animation.OnAttackFrame -= HandleAttackFrame;
    }

    private void HandleAttackFrame()
    {
        if (!attackReady)
        {
            return;
        }

        float distanceToPlayer = context.DistanceToPlayer();
        if (distanceToPlayer <= context.Config.attackRange * 1.2f && context.PlayerTarget != null)
        {
            IDamageable playerDamageable = context.PlayerTarget.GetComponent<IDamageable>();
            if (playerDamageable != null)
            {
                playerDamageable.TakeDamage(context.Config.attackDamage);
            }
            else
            {
                Debug.LogWarning("[EnemyAttack] Player does not have IDamageable component — no damage dealt.");
            }
        }

        attackReady = false;
        cooldownTimer = context.Config.attackCooldown;
    }
}
