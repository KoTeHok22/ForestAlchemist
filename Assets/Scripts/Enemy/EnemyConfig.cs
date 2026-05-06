using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "Game/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("General")]
    public string enemyName;
    public int maxHealth = 50;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float chaseSpeed = 3f;

    [Header("Combat")]
    public int attackDamage = 10;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.5f;

    [Header("Detection")]
    public float detectionRange = 8f;
    public float loseInterestRange = 15f;

    [Header("Patrol")]
    public float patrolRadius = 20f;
    public float patrolWaitTime = 1f;

    [Header("Reward")]
    public int scoreReward = 100;

    [Header("Animation")]
    public string animationResourcePath = "Game/Orc/1";
    public int animationFps = 10;

    [Header("Visuals")]
    public Vector3 spriteScale = Vector3.one;
}
