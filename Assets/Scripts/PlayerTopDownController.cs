using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerTopDownController : MonoBehaviour
{
    private enum FacingDirection
    {
        South,
        North,
        East,
        West
    }

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runMultiplier = 1.6f;

    [Header("Attack")]
    [SerializeField] private float attackDuration = 0.35f;
    [SerializeField] private float attackStaminaCost = 20f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float runStaminaCostPerSecond = 18f;
    [SerializeField] private float staminaRecoveryPerSecond = 12f;
    [SerializeField] private Scrollbar staminaScrollbar;

    [Header("Animation")]
    [SerializeField] private float animationFps = 10f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D playerRigidbody;
    private Camera mainCamera;

    private Sprite[] idleNorth;
    private Sprite[] idleSouth;
    private Sprite[] idleEast;
    private Sprite[] idleWest;
    private Sprite[] walkNorth;
    private Sprite[] walkSouth;
    private Sprite[] walkEast;
    private Sprite[] walkWest;
    private Sprite[] runNorth;
    private Sprite[] runSouth;
    private Sprite[] runEast;
    private Sprite[] runWest;

    private Sprite attackNorth;
    private Sprite attackSouth;
    private Sprite attackEast;
    private Sprite attackWest;

    private Vector2 movementInput;
    private Vector2 lookDirection = Vector2.down;
    private Vector2 currentVelocity;
    private FacingDirection facing = FacingDirection.South;

    private float frameTimer;
    private int frameIndex;
    private float attackTimer;
    private float currentStamina;
    private float speedMultiplier = 1f;

    private bool IsAttacking => attackTimer > 0f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerRigidbody = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        currentStamina = maxStamina;
        ResolveStaminaScrollbar();
        LoadAnimations();
        UpdateStaminaBar();

        playerRigidbody.gravityScale = 0f;
        playerRigidbody.freezeRotation = true;
        playerRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        playerRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        UpdateLookDirection();

        if (IsAttacking)
        {
            movementInput = Vector2.zero;
            attackTimer -= Time.deltaTime;
            ApplyAttackFrame();
            return;
        }

        ReadMovementInput();
        UpdateVelocity();
        RecoverStamina(Time.deltaTime);
        UpdateAnimation(Time.deltaTime);
        UpdateStaminaBar();
    }

    private void FixedUpdate()
    {
        ApplyMovement(Time.fixedDeltaTime);
    }

    private void ReadMovementInput()
    {
        Vector2 rawInput = ReadMoveInput();
        float horizontal = rawInput.x;
        float vertical = rawInput.y;

        movementInput = new Vector2(horizontal, vertical);
        if (movementInput.sqrMagnitude > 1f)
        {
            movementInput.Normalize();
        }
    }

    private void UpdateLookDirection()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector3 mouseScreenPosition = ReadMouseScreenPosition();
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        Vector2 directionToMouse = (mouseWorldPosition - transform.position);

        if (directionToMouse.sqrMagnitude > 0.0001f)
        {
            lookDirection = directionToMouse.normalized;
            facing = GetFacingDirection(lookDirection);
        }
    }

    private void ApplyMovement(float deltaTime)
    {
        float speed = walkSpeed * speedMultiplier;
        bool isRunning = movementInput.sqrMagnitude > 0f && IsRunPressed() && CanRun(deltaTime);
        if (isRunning)
        {
            speed *= runMultiplier;
            SpendStamina(runStaminaCostPerSecond * deltaTime);
        }

        currentVelocity = movementInput * speed;
        Vector2 targetPosition = playerRigidbody.position + currentVelocity * deltaTime;
        playerRigidbody.MovePosition(targetPosition);
    }

    public bool TryStartAttack()
    {
        if (!CanAttack() || IsAttacking)
        {
            return false;
        }

        SpendStamina(attackStaminaCost);
        attackTimer = attackDuration;
        frameTimer = 0f;
        frameIndex = 0;
        ApplyAttackFrame();
        UpdateStaminaBar();
        return true;
    }

    private void UpdateAnimation(float deltaTime)
    {
        Sprite[] activeFrames = GetCurrentLoop();
        if (activeFrames == null || activeFrames.Length == 0)
        {
            return;
        }

        if (activeFrames.Length == 1)
        {
            frameIndex = 0;
            frameTimer = 0f;
            spriteRenderer.sprite = activeFrames[0];
            return;
        }

        frameTimer += deltaTime;
        float frameDuration = 1f / Mathf.Max(1f, animationFps);

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % activeFrames.Length;
        }

        spriteRenderer.sprite = activeFrames[frameIndex];
    }

    private void ApplyAttackFrame()
    {
        Sprite attackSprite = GetAttackSprite();
        if (attackSprite != null)
        {
            spriteRenderer.sprite = attackSprite;
            return;
        }

        Sprite[] fallbackFrames = GetIdleFrames(facing);
        if (fallbackFrames != null && fallbackFrames.Length > 0)
        {
            spriteRenderer.sprite = fallbackFrames[0];
        }
    }

    private Sprite[] GetCurrentLoop()
    {
        bool isMoving = movementInput.sqrMagnitude > 0.0001f;
        bool isRunning = isMoving && currentVelocity.sqrMagnitude > (walkSpeed * walkSpeed) + 0.0001f;

        if (!isMoving)
        {
            return GetIdleFrames(facing);
        }

        return isRunning ? GetRunFrames(GetFacingDirection(movementInput)) : GetWalkFrames(GetFacingDirection(movementInput));
    }

    private FacingDirection GetFacingDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x >= 0f ? FacingDirection.East : FacingDirection.West;
        }

        return direction.y >= 0f ? FacingDirection.North : FacingDirection.South;
    }

    private Sprite[] GetIdleFrames(FacingDirection direction)
    {
        switch (direction)
        {
            case FacingDirection.North:
                return idleNorth;
            case FacingDirection.East:
                return idleEast;
            case FacingDirection.West:
                return idleWest;
            default:
                return idleSouth;
        }
    }

    private Sprite[] GetWalkFrames(FacingDirection direction)
    {
        switch (direction)
        {
            case FacingDirection.North:
                return walkNorth;
            case FacingDirection.East:
                return walkEast;
            case FacingDirection.West:
                return walkWest;
            default:
                return walkSouth;
        }
    }

    private Sprite[] GetRunFrames(FacingDirection direction)
    {
        switch (direction)
        {
            case FacingDirection.North:
                return runNorth;
            case FacingDirection.East:
                return runEast;
            case FacingDirection.West:
                return runWest;
            default:
                return runSouth;
        }
    }

    private Sprite GetAttackSprite()
    {
        switch (facing)
        {
            case FacingDirection.North:
                return attackNorth;
            case FacingDirection.East:
                return attackEast;
            case FacingDirection.West:
                return attackWest;
            default:
                return attackSouth;
        }
    }

    private void LoadAnimations()
    {
        idleNorth = LoadAnimationSet("Game/Character/Idle/Idle_Strip_North", "Game/Character/Idle/IdleNorth");
        idleSouth = LoadAnimationSet("Game/Character/Idle/Idle_Strip_South", "Game/Character/Idle/IdleSouth");
        idleEast = LoadAnimationSet("Game/Character/Idle/Idle_Strip_East", "Game/Character/Idle/IdleEast");
        idleWest = LoadAnimationSet("Game/Character/Idle/Idle_Strip_West", "Game/Character/Idle/IdleWest", idleEast);

        walkNorth = LoadAnimationSet("Game/Character/Walk/Walk_Strip_North", "Game/Character/Walk/WalkNorth");
        walkSouth = LoadAnimationSet("Game/Character/Walk/Walk_Strip_South", "Game/Character/Walk/WalkSouth");
        walkEast = LoadAnimationSet("Game/Character/Walk/Walk_Strip_East", "Game/Character/Walk/WalkEast");
        walkWest = LoadAnimationSet("Game/Character/Walk/Walk_Strip_West", "Game/Character/Walk/WalkWest");

        runNorth = LoadAnimationSet("Game/Character/Run/Run_Strip_North", "Game/Character/Run/RunNorth", walkNorth);
        runSouth = LoadAnimationSet("Game/Character/Run/Run_Strip_South", "Game/Character/Run/RunSouth", walkSouth);
        runEast = LoadAnimationSet("Game/Character/Run/Run_Strip_East", "Game/Character/Run/RunEast", walkEast);
        runWest = LoadAnimationSet("Game/Character/Run/Run_Strip_West", "Game/Character/Run/RunWest", walkWest);

        attackNorth = LoadSingleSprite("Game/Character/Attack/Attack_North_0");
        attackSouth = LoadSingleSprite("Game/Character/Attack/Attack_South_0");
        attackEast = LoadSingleSprite("Game/Character/Attack/Attack_East_0");
        attackWest = LoadSingleSprite("Game/Character/Attack/Attack_West_0");

        Sprite[] initialIdle = GetIdleFrames(facing);
        if (initialIdle != null && initialIdle.Length > 0)
        {
            spriteRenderer.sprite = initialIdle[0];
        }
    }

    private Sprite[] LoadAnimationSet(string sheetPath, string fallbackPath, Sprite[] secondaryFallback = null)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(sheetPath);
        if (sprites != null && sprites.Length > 0)
        {
            return sprites;
        }

        sprites = Resources.LoadAll<Sprite>(fallbackPath);
        if (sprites != null && sprites.Length > 0)
        {
            return sprites;
        }

        return secondaryFallback;
    }

    private Sprite LoadSingleSprite(string path)
    {
        return Resources.Load<Sprite>(path);
    }

    public Vector2 LookDirection => lookDirection;

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0.2f, multiplier);
    }

    private bool CanAttack()
    {
        return currentStamina >= attackStaminaCost;
    }

    private bool CanRun(float deltaTime)
    {
        return currentStamina >= runStaminaCostPerSecond * deltaTime;
    }

    private void SpendStamina(float amount)
    {
        currentStamina = Mathf.Max(0f, currentStamina - Mathf.Max(0f, amount));
    }

    private void RecoverStamina(float deltaTime)
    {
        bool tryingToRun = movementInput.sqrMagnitude > 0f && IsRunPressed() && currentStamina > 0f;
        if (tryingToRun)
        {
            return;
        }

        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRecoveryPerSecond * deltaTime);
    }

    private void ResolveStaminaScrollbar()
    {
        if (staminaScrollbar != null)
        {
            return;
        }

        Scrollbar[] scrollbars = Object.FindObjectsByType<Scrollbar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Scrollbar scrollbar in scrollbars)
        {
            if (scrollbar != null && scrollbar.gameObject.name == "Stamina")
            {
                staminaScrollbar = scrollbar;
                return;
            }
        }
    }

    private void UpdateStaminaBar()
    {
        if (staminaScrollbar == null)
        {
            ResolveStaminaScrollbar();
        }

        if (staminaScrollbar == null)
        {
            return;
        }

        float normalizedStamina = maxStamina > 0f ? currentStamina / maxStamina : 0f;
        staminaScrollbar.size = normalizedStamina;
    }

    private void UpdateVelocity()
    {
        if (IsAttacking)
        {
            currentVelocity = Vector2.zero;
            return;
        }

        float speed = walkSpeed * speedMultiplier;
        bool isRunning = movementInput.sqrMagnitude > 0f && IsRunPressed() && CanRun(Time.deltaTime);
        if (isRunning)
        {
            speed *= runMultiplier;
        }

        currentVelocity = movementInput * speed;
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                input.x -= 1f;
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                input.x += 1f;
            }

            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                input.y -= 1f;
            }

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                input.y += 1f;
            }
        }

        return input;
    }

    private bool IsRunPressed()
    {
        return Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
    }

    private Vector3 ReadMouseScreenPosition()
    {
        if (Mouse.current == null)
        {
            return Vector3.zero;
        }

        Vector2 position = Mouse.current.position.ReadValue();
        return new Vector3(position.x, position.y, 0f);
    }
}
