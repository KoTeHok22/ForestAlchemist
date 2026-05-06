using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyStateMachine : MonoBehaviour
{
    private const int SteeringRayCount = 12;
    private const float ObstacleCheckDistance = 1.5f;
    private const float SeparationRadius = 1.8f;
    private const float SeparationStrength = 2.5f;

    public Transform BaseTransform { get; private set; }
    public Transform PlayerTarget { get; private set; }
    public EnemyConfig Config { get; private set; }
    public Rigidbody2D Body { get; private set; }
    public EnemyAnimationController Animation { get; private set; }
    public EnemyHealth Health { get; private set; }

    private IEnemyState currentState;
    private Vector2 currentVelocity;
    private LayerMask obstacleMask;
    private int enemyLayer;

    private readonly float[] interestMap = new float[SteeringRayCount];
    private readonly float[] dangerMap = new float[SteeringRayCount];
    private readonly Vector2[] rayDirections = new Vector2[SteeringRayCount];

    public void Initialize(
        EnemyConfig config,
        Transform baseTransform,
        Transform playerTarget,
        Rigidbody2D body,
        EnemyAnimationController animation,
        EnemyHealth health)
    {
        Config = config;
        BaseTransform = baseTransform;
        PlayerTarget = playerTarget;
        Body = body;
        Animation = animation;
        Health = health;

        int defaultLayer = LayerMask.NameToLayer("Default");
        obstacleMask = (defaultLayer >= 0) ? (1 << defaultLayer) : Physics2D.DefaultRaycastLayers;
        enemyLayer = LayerMask.NameToLayer("Enemy");

        for (int i = 0; i < SteeringRayCount; i++)
        {
            float angle = (360f / SteeringRayCount) * i * Mathf.Deg2Rad;
            rayDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }

    public void ChangeState(IEnemyState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    private void Update()
    {
        if (!Health.IsAlive && !(currentState is EnemyDeathState))
        {
            return;
        }

        currentState?.Execute();
    }

    private void FixedUpdate()
    {
        if (Body == null || currentVelocity.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector2 newPos = Body.position + currentVelocity * Time.fixedDeltaTime;
        Body.MovePosition(newPos);
    }

    public float DistanceToPlayer()
    {
        if (PlayerTarget == null)
        {
            return float.MaxValue;
        }

        return Vector2.Distance(transform.position, PlayerTarget.position);
    }

    public Vector2 DirectionToPlayer()
    {
        if (PlayerTarget == null)
        {
            return Vector2.zero;
        }

        return ((Vector2)PlayerTarget.position - (Vector2)transform.position).normalized;
    }

    public void MoveToward(Vector2 target, float speed)
    {
        Vector2 desired = (target - (Vector2)transform.position).normalized;
        Vector2 steeringDir = ContextSteering(desired);
        Vector2 separation = CalculateSeparation();
        Vector2 finalDir = (steeringDir + separation * 0.5f).normalized;
        currentVelocity = finalDir * speed;
    }

    public void StopMovement()
    {
        currentVelocity = Vector2.zero;
    }

    public Vector2 GetMovementDirection()
    {
        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            return currentVelocity.normalized;
        }

        return Vector2.zero;
    }

    /// <summary>
    /// Context-based steering: build interest & danger maps, pick best direction.
    /// </summary>
    private Vector2 ContextSteering(Vector2 desiredDirection)
    {
        if (desiredDirection.sqrMagnitude < 0.001f)
        {
            return Vector2.zero;
        }

        Vector2 origin = (Vector2)transform.position;

        for (int i = 0; i < SteeringRayCount; i++)
        {
            float dot = Vector2.Dot(rayDirections[i], desiredDirection);
            interestMap[i] = Mathf.Max(0f, dot);

            RaycastHit2D hit = Physics2D.Raycast(origin, rayDirections[i], ObstacleCheckDistance, obstacleMask);
            if (hit.collider != null)
            {
                float proximity = 1f - (hit.distance / ObstacleCheckDistance);
                dangerMap[i] = proximity * proximity;
            }
            else
            {
                dangerMap[i] = 0f;
            }
        }

        Vector2 chosenDir = Vector2.zero;
        for (int i = 0; i < SteeringRayCount; i++)
        {
            float weight = interestMap[i] - dangerMap[i];
            if (weight > 0f)
            {
                chosenDir += rayDirections[i] * weight;
            }
        }

        if (chosenDir.sqrMagnitude < 0.001f)
        {
            // All forward directions blocked — pick the least dangerous one
            float minDanger = float.MaxValue;
            int bestIdx = 0;
            for (int i = 0; i < SteeringRayCount; i++)
            {
                if (dangerMap[i] < minDanger)
                {
                    minDanger = dangerMap[i];
                    bestIdx = i;
                }
            }
            chosenDir = rayDirections[bestIdx];
        }

        return chosenDir.normalized;
    }

    /// <summary>
    /// Push away from nearby enemies so they don't stack on the same spot.
    /// </summary>
    private Vector2 CalculateSeparation()
    {
        if (enemyLayer < 0)
        {
            return Vector2.zero;
        }

        Vector2 origin = (Vector2)transform.position;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(origin, SeparationRadius, 1 << enemyLayer);

        Vector2 separation = Vector2.zero;
        int count = 0;

        for (int i = 0; i < nearby.Length; i++)
        {
            if (nearby[i] == null || nearby[i].gameObject == gameObject)
            {
                continue;
            }

            Vector2 otherPos = (Vector2)nearby[i].transform.position;
            Vector2 diff = origin - otherPos;
            float dist = diff.magnitude;

            if (dist < 0.01f)
            {
                diff = Random.insideUnitCircle.normalized;
                dist = 0.01f;
            }

            float weight = 1f - (dist / SeparationRadius);
            separation += diff.normalized * (weight * SeparationStrength);
            count++;
        }

        return (count > 0) ? separation / count : Vector2.zero;
    }
}
