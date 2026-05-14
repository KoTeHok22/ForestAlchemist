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
    public System.Collections.Generic.List<LootEntry> lootTable = new System.Collections.Generic.List<LootEntry>();

    [Header("Animation")]
public string animationResourcePath = "Game/Orc/1";
    public int animationFps = 10;

    [Header("Element")]
    public ElementType element = ElementType.None;

    [Header("Visuals")]
    public Vector3 spriteScale = Vector3.one;

    [Header("Shaman (ranged)")]
    public bool isShaman;
    public GameObject shamanProjectilePrefab;
    public float shamanAttackRange = 6f;
    public float shamanAttackCooldown = 2f;
    public float shamanProjectileSpeed = 5f;

    [Header("Boss")]
    public bool isBoss;
    public string bossTrophyItemId;
}

    [System.Serializable]
    public struct LootEntry
    {
    public string itemName;
    public float chance; // 0 to 1
    public int minAmount;
    public int maxAmount;
    }
