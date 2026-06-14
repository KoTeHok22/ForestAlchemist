using UnityEngine;

/// <summary>
/// Sits on the "ВыходВЛес" exit. Opens the expedition preparation screen when the
/// player walks up to the exit (proximity zone). The expedition itself starts only
/// after the player presses "В лес" inside that screen — this script never starts
/// it directly. Re-entry: the player must leave the zone before it opens again.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class ExpeditionStarter : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.45f, 1f);
    [SerializeField] private ExpeditionPreparationUI preparationUI;

    // Hysteresis so the panel doesn't flicker open/closed right at the edge of the zone.
    private const float ExitMargin = 0.75f;

    private SpriteRenderer sr;
    private Color defaultColor;
    private Transform player;
    private bool wasInRange;
    private bool openedThisVisit;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) defaultColor = sr.color;

        if (preparationUI == null)
        {
            preparationUI = FindFirstObjectByType<ExpeditionPreparationUI>();
        }

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Update()
    {
        if (HomeUIBlocker.IsBlocked)
        {
            return;
        }

        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            if (player == null) return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        bool inRange = distance <= interactionDistance;
        bool outOfRange = distance > interactionDistance + ExitMargin;

        // Rising edge: player just walked into the zone → open the preparation screen.
        if (inRange && !wasInRange && !openedThisVisit)
        {
            OpenPreparation();
            openedThisVisit = true;
        }

        // Once the player has clearly left, arm the trigger again for the next approach.
        if (outOfRange)
        {
            openedThisVisit = false;
        }

        if (inRange != wasInRange)
        {
            wasInRange = inRange;
            if (sr != null) sr.color = inRange ? highlightColor : defaultColor;
        }
    }

    private void OpenPreparation()
    {
        if (preparationUI == null)
        {
            preparationUI = FindFirstObjectByType<ExpeditionPreparationUI>();
        }

        if (preparationUI != null)
        {
            preparationUI.Open();
            if (!preparationUI.IsOpen)
            {
                openedThisVisit = false;
            }
        }
    }
}
