using System;
using UnityEngine;

public enum EnemyAnimState
{
    Idle,
    Walk,
    Run,
    Attack,
    Hurt,
    Death
}

[DisallowMultipleComponent]
public sealed class EnemyAnimationController : MonoBehaviour
{
    private enum FacingDirection
    {
        South,
        North,
        East,
        West
    }

    private SpriteRenderer spriteRenderer;
    private float animationFps = 10f;

    private Sprite[] idleSouth, idleNorth, idleEast, idleWest;
    private Sprite[] walkSouth, walkNorth, walkEast, walkWest;
    private Sprite[] runSouth, runNorth, runEast, runWest;
    private Sprite[] attackSouth, attackNorth, attackEast, attackWest;
    private Sprite[] hurtSouth, hurtNorth, hurtEast, hurtWest;
    private Sprite[] deathSouth, deathNorth, deathEast, deathWest;

    private EnemyAnimState currentState = EnemyAnimState.Idle;
    private FacingDirection currentFacing = FacingDirection.South;
    private float frameTimer;
    private int frameIndex;

    private bool deathAnimationPlaying;
    private bool deathAnimationFinished;

    public event Action OnDeathAnimationComplete;
    public event Action OnAttackFrame;

    public void Initialize(EnemyConfig config, SpriteRenderer renderer)
    {
        spriteRenderer = renderer;
        animationFps = Mathf.Max(1f, config.animationFps);
        LoadAllAnimations(config.animationResourcePath);

        Sprite[] initialFrames = GetFramesForState(EnemyAnimState.Idle, FacingDirection.South);
        if (initialFrames != null && initialFrames.Length > 0)
        {
            spriteRenderer.sprite = initialFrames[0];
        }
    }

    public void SetState(EnemyAnimState state)
    {
        if (deathAnimationPlaying && state != EnemyAnimState.Death)
        {
            return;
        }

        if (state == currentState)
        {
            return;
        }

        currentState = state;
        frameIndex = 0;
        frameTimer = 0f;

        if (state == EnemyAnimState.Death)
        {
            deathAnimationPlaying = true;
            deathAnimationFinished = false;
        }
    }

    public void SetDirection(Vector2 direction)
    {
        if (deathAnimationPlaying)
        {
            return;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        currentFacing = GetFacingDirection(direction);
    }

    private void Update()
    {
        Sprite[] frames = GetFramesForState(currentState, currentFacing);
        if (frames == null || frames.Length == 0 || spriteRenderer == null)
        {
            return;
        }

        float frameDuration = 1f / animationFps;
        frameTimer += Time.deltaTime;

        if (deathAnimationPlaying)
        {
            UpdateDeathAnimation(frames, frameDuration);
            return;
        }

        if (currentState == EnemyAnimState.Attack)
        {
            UpdateAttackAnimation(frames, frameDuration);
            return;
        }

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;
        }

        spriteRenderer.sprite = frames[frameIndex];
    }

    private void UpdateDeathAnimation(Sprite[] frames, float frameDuration)
    {
        if (deathAnimationFinished)
        {
            spriteRenderer.sprite = frames[frames.Length - 1];
            return;
        }

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;
        }

        if (frameIndex >= frames.Length)
        {
            frameIndex = frames.Length - 1;
            deathAnimationFinished = true;
            spriteRenderer.sprite = frames[frameIndex];
            OnDeathAnimationComplete?.Invoke();
            return;
        }

        spriteRenderer.sprite = frames[frameIndex];
    }

    private void UpdateAttackAnimation(Sprite[] frames, float frameDuration)
    {
        bool firedAttack = false;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            int prevIndex = frameIndex;
            frameIndex++;

            if (frameIndex >= frames.Length)
            {
                frameIndex = 0;
            }

            int hitFrame = Mathf.Max(0, frames.Length / 2);
            if (prevIndex < hitFrame && frameIndex >= hitFrame && !firedAttack)
            {
                firedAttack = true;
                OnAttackFrame?.Invoke();
            }
        }

        spriteRenderer.sprite = frames[Mathf.Min(frameIndex, frames.Length - 1)];
    }

    private void LoadAllAnimations(string basePath)
    {
        LoadDirectionalSet(basePath, "idle_with_shadow", out idleSouth, out idleNorth, out idleEast, out idleWest);
        LoadDirectionalSet(basePath, "walk_with_shadow", out walkSouth, out walkNorth, out walkEast, out walkWest);
        LoadDirectionalSet(basePath, "run_with_shadow", out runSouth, out runNorth, out runEast, out runWest);
        LoadDirectionalSet(basePath, "attack_with_shadow", out attackSouth, out attackNorth, out attackEast, out attackWest);
        LoadDirectionalSet(basePath, "hurt_with_shadow", out hurtSouth, out hurtNorth, out hurtEast, out hurtWest);
        LoadDirectionalSet(basePath, "death_with_shadow", out deathSouth, out deathNorth, out deathEast, out deathWest);

        if (walkSouth == null) walkSouth = idleSouth;
        if (walkNorth == null) walkNorth = idleNorth;
        if (walkEast == null) walkEast = idleEast;
        if (walkWest == null) walkWest = idleWest;

        if (runSouth == null) runSouth = walkSouth;
        if (runNorth == null) runNorth = walkNorth;
        if (runEast == null) runEast = walkEast;
        if (runWest == null) runWest = walkWest;
    }

    private void LoadDirectionalSet(string basePath, string animName,
        out Sprite[] south, out Sprite[] north, out Sprite[] east, out Sprite[] west)
    {
        string orcNumber = ExtractOrcNumber(basePath);
        string sheetPath = $"{basePath}/orc{orcNumber}_{animName}";
        Sprite[] allSprites = Resources.LoadAll<Sprite>(sheetPath);

        if (allSprites == null || allSprites.Length == 0)
        {
            south = north = east = west = null;
            return;
        }

        SplitIntoDirections(allSprites, out south, out north, out east, out west);
    }

    private string ExtractOrcNumber(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "1";
        }

        int lastSlash = path.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < path.Length - 1)
        {
            return path.Substring(lastSlash + 1);
        }

        return path;
    }

    private void SplitIntoDirections(Sprite[] allSprites, out Sprite[] south, out Sprite[] north, out Sprite[] east, out Sprite[] west)
    {
        int totalFrames = allSprites.Length;
        int framesPerDirection = totalFrames / 4;

        if (framesPerDirection <= 0)
        {
            south = allSprites;
            north = east = west = allSprites;
            return;
        }

        south = new Sprite[framesPerDirection];
        north = new Sprite[framesPerDirection];
        west = new Sprite[framesPerDirection];
        east = new Sprite[framesPerDirection];

        Array.Copy(allSprites, 0, south, 0, framesPerDirection);
        Array.Copy(allSprites, framesPerDirection, north, 0, framesPerDirection);
        Array.Copy(allSprites, framesPerDirection * 2, west, 0, framesPerDirection);
        Array.Copy(allSprites, framesPerDirection * 3, east, 0, framesPerDirection);
    }

    private Sprite[] GetFramesForState(EnemyAnimState state, FacingDirection facing)
    {
        switch (state)
        {
            case EnemyAnimState.Idle: return GetDirectional(idleSouth, idleNorth, idleEast, idleWest, facing);
            case EnemyAnimState.Walk: return GetDirectional(walkSouth, walkNorth, walkEast, walkWest, facing);
            case EnemyAnimState.Run: return GetDirectional(runSouth, runNorth, runEast, runWest, facing);
            case EnemyAnimState.Attack: return GetDirectional(attackSouth, attackNorth, attackEast, attackWest, facing);
            case EnemyAnimState.Hurt: return GetDirectional(hurtSouth, hurtNorth, hurtEast, hurtWest, facing);
            case EnemyAnimState.Death: return GetDirectional(deathSouth, deathNorth, deathEast, deathWest, facing);
            default: return GetDirectional(idleSouth, idleNorth, idleEast, idleWest, facing);
        }
    }

    private Sprite[] GetDirectional(Sprite[] south, Sprite[] north, Sprite[] east, Sprite[] west, FacingDirection facing)
    {
        switch (facing)
        {
            case FacingDirection.North: return north ?? south;
            case FacingDirection.East: return east ?? south;
            case FacingDirection.West: return west ?? east ?? south;
            default: return south;
        }
    }

    private FacingDirection GetFacingDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x >= 0f ? FacingDirection.East : FacingDirection.West;
        }

        return direction.y >= 0f ? FacingDirection.North : FacingDirection.South;
    }
}
