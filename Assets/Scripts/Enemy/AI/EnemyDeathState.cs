using UnityEngine;

public sealed class EnemyDeathState : IEnemyState
{
    private readonly EnemyStateMachine context;
    private readonly System.Action onDeathComplete;
    private bool finished;

    public EnemyDeathState(EnemyStateMachine context, System.Action onDeathComplete)
    {
        this.context = context;
        this.onDeathComplete = onDeathComplete;
    }

    public void Enter()
    {
        finished = false;
        context.StopMovement();

        if (context.Body != null)
        {
            context.Body.simulated = false;
        }

        Collider2D col = context.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        context.Animation.OnDeathAnimationComplete += HandleDeathAnimationComplete;
        context.Animation.SetState(EnemyAnimState.Death);
    }

    public void Execute()
    {
    }

    public void Exit()
    {
        context.Animation.OnDeathAnimationComplete -= HandleDeathAnimationComplete;
    }

    private void HandleDeathAnimationComplete()
    {
        if (finished)
        {
            return;
        }

        finished = true;
        onDeathComplete?.Invoke();
    }
}
