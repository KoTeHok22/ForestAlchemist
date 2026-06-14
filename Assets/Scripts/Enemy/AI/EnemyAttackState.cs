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
            string attackClip = context.Config != null && context.Config.isBoss
                ? AudioClipId.SfxEnemyBossStomp
                : AudioClipId.SfxEnemyOrcAttackMelee;
            AudioHooks.SfxAtPoint(attackClip, context.transform.position);

            IDamageable playerDamageable = context.PlayerTarget.GetComponent<IDamageable>();
            if (playerDamageable != null)
            {
                int baseDamage = context.Config.attackDamage;
                float evolutionMult = 1f;
                var progress = GameCore.Instance.CurrentProgress;
                if (progress != null) evolutionMult = progress.orcs.statMultiplier;

                int finalDamage = Mathf.RoundToInt(baseDamage * evolutionMult);

                ElementType attackElement = context.Config.element;
                if (attackElement != ElementType.None)
                {
                    PlayerBuffReceiver buffReceiver = context.PlayerTarget.GetComponent<PlayerBuffReceiver>();
                    ElementType counterElement = attackElement.GetCounter();

                    float elementalMult = 1f;
                    if (counterElement == ElementType.Fire && attackElement == ElementType.Water) elementalMult = 1.5f;
                    finalDamage = Mathf.RoundToInt(finalDamage * elementalMult);
                }

                playerDamageable.TakeDamage(finalDamage);
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
