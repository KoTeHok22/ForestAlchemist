using UnityEngine;

/// <summary>
/// Subscribes to gameplay events and plays SFX. Attach to AudioManager at runtime.
/// </summary>
public sealed class AudioEventBridge : MonoBehaviour
{
    private AudioManager audioManager;
    private PlayerHealth playerHealth;
    private ResourceGatherer gatherer;
    private PlayerTopDownController playerMovement;
    private Rigidbody2D playerBody;
    private float footstepTimer;
    private int lastQuestProgressFrame = -1;
    private string lastQuestProgressId = string.Empty;

    private void Awake()
    {
        audioManager = GetComponent<AudioManager>();
    }

    private void OnEnable()
    {
        SubscribeQuests();
        SubscribeCrafting();
        SubscribeExpedition();
        BindPlayer();
        EnemyController.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        UnsubscribeQuests();
        UnsubscribeCrafting();
        UnsubscribeExpedition();
        UnbindPlayer();
        EnemyController.OnAnyEnemyDied -= HandleEnemyDied;
    }

    private void Update()
    {
        if (playerMovement == null)
        {
            BindPlayer();
        }

        UpdateFootsteps();
    }

    private void BindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        if (playerHealth == null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnDamaged += HandlePlayerDamaged;
                playerHealth.OnDeath += HandlePlayerDeath;
                playerHealth.OnHealthChanged += HandleHealthChanged;
            }
        }

        if (gatherer == null)
        {
            gatherer = player.GetComponent<ResourceGatherer>();
        }

        if (playerMovement == null)
        {
            playerMovement = player.GetComponent<PlayerTopDownController>();
        }

        if (playerBody == null)
        {
            playerBody = player.GetComponent<Rigidbody2D>();
        }
    }

    private void UnbindPlayer()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged -= HandlePlayerDamaged;
            playerHealth.OnDeath -= HandlePlayerDeath;
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth = null;
        }

        gatherer = null;
        playerMovement = null;
        playerBody = null;
    }

    private void SubscribeQuests()
    {
        QuestManager manager = QuestManager.Instance;
        if (manager == null)
        {
            return;
        }

        manager.OnQuestProgressUpdated += HandleQuestProgress;
        manager.OnQuestCompleted += HandleQuestCompleted;
        manager.OnQuestRewardGranted += HandleQuestReward;
    }

    private void UnsubscribeQuests()
    {
        QuestManager manager = QuestManager.Instance;
        if (manager == null)
        {
            return;
        }

        manager.OnQuestProgressUpdated -= HandleQuestProgress;
        manager.OnQuestCompleted -= HandleQuestCompleted;
        manager.OnQuestRewardGranted -= HandleQuestReward;
    }

    private void SubscribeCrafting()
    {
        CraftingManager crafting = CraftingManager.Instance;
        if (crafting == null)
        {
            return;
        }

        crafting.OnRecipeCrafted += HandleRecipeCrafted;
        crafting.OnSpellCrafted += HandleSpellCrafted;
    }

    private void UnsubscribeCrafting()
    {
        CraftingManager crafting = CraftingManager.Instance;
        if (crafting == null)
        {
            return;
        }

        crafting.OnRecipeCrafted -= HandleRecipeCrafted;
        crafting.OnSpellCrafted -= HandleSpellCrafted;
    }

    private void SubscribeExpedition()
    {
        ExpeditionManager expedition = ExpeditionManager.Instance;
        if (expedition == null)
        {
            return;
        }

        expedition.OnExpeditionStarted += HandleExpeditionStarted;
    }

    private void UnsubscribeExpedition()
    {
        ExpeditionManager expedition = ExpeditionManager.Instance;
        if (expedition == null)
        {
            return;
        }

        expedition.OnExpeditionStarted -= HandleExpeditionStarted;
    }

    private void HandleExpeditionStarted()
    {
        audioManager.PlaySfx(AudioClipId.SfxHomeExpeditionStart);
    }

    private void HandleQuestProgress(string questId, int progress)
    {
        if (Time.frameCount == lastQuestProgressFrame && questId == lastQuestProgressId)
        {
            return;
        }

        lastQuestProgressFrame = Time.frameCount;
        lastQuestProgressId = questId;
        audioManager.PlaySfx(AudioClipId.SfxUiNotificationQuest);
    }

    private void HandleQuestCompleted(string questId)
    {
        audioManager.PlaySfx(AudioClipId.SfxUiNotificationQuestComplete);

        if (questId != null && questId.Contains("boss"))
        {
            audioManager.PlaySfx(AudioClipId.SfxQuestBossComplete);
        }
    }

    private void HandleQuestReward(int amount)
    {
        if (amount > 0)
        {
            audioManager.PlaySfx(AudioClipId.SfxCurrencyBloodGain);
        }
    }

    private void HandleRecipeCrafted(RecipeDefinition recipe)
    {
        audioManager.PlaySfx(AudioClipId.SfxCraftSuccessPotion);
    }

    private void HandleSpellCrafted(SpellDefinition spell)
    {
        audioManager.PlaySfx(AudioClipId.SfxCraftSuccessSpell);
        audioManager.PlaySfx(AudioClipId.SfxSpellUnlocked);
    }

    private void HandlePlayerDamaged(int currentHealth)
    {
        audioManager.PlaySfx(AudioClipId.SfxPlayerTakeDamage);
    }

    private void HandlePlayerDeath()
    {
        audioManager.PlaySfx(AudioClipId.SfxPlayerDeath);
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (current > 0 && current < max)
        {
            // Heal detection is approximate via regen jumps — handled in PlayerHealth.Heal hook below if needed.
        }
    }

    private void HandleEnemyDied(EnemyController enemy)
    {
        if (enemy == null || enemy.Config == null)
        {
            return;
        }

        if (enemy.Config.isBoss)
        {
            audioManager.PlaySfx(AudioClipId.SfxEnemyBossDeath);
        }
    }

    private void UpdateFootsteps()
    {
        if (playerMovement == null && playerBody == null)
        {
            return;
        }

        if (gatherer != null && gatherer.IsBusy)
        {
            return;
        }

        if (Time.timeScale <= 0f)
        {
            return;
        }

        float speed = playerBody != null ? playerBody.linearVelocity.magnitude : 0f;
        if (speed < 0.15f)
        {
            footstepTimer = 0f;
            return;
        }

        float interval = speed > 3f ? 0.28f : 0.42f;
        footstepTimer -= Time.deltaTime;
        if (footstepTimer > 0f)
        {
            return;
        }

        footstepTimer = interval;
        string clip = Random.value > 0.5f
            ? AudioClipId.SfxPlayerFootstepGrass01
            : AudioClipId.SfxPlayerFootstepGrass02;
        if (speed > 3f)
        {
            clip = AudioClipId.SfxPlayerFootstepRun01;
        }

        audioManager.PlaySfx(clip);
    }

    public void PlayGatherStart()
    {
        audioManager.PlaySfx(AudioClipId.SfxGatherStart);
        audioManager.StartLoopSfx(AudioClipId.SfxGatherLoop);
    }

    public void PlayGatherComplete(string itemId)
    {
        audioManager.StopLoopSfx();
        if (itemId == ItemCatalog.RareFlower)
        {
            audioManager.PlaySfx(AudioClipId.SfxGatherFlowerComplete);
        }
        else
        {
            audioManager.PlaySfx(AudioClipId.SfxGatherComplete);
        }

        audioManager.PlaySfx(AudioClipId.SfxItemPickupGeneric);
    }

    public void PlayGatherCancel()
    {
        audioManager.StopLoopSfx();
        audioManager.PlaySfx(AudioClipId.SfxGatherCancel);
    }

    public void PlayMeleeSwing()
    {
        audioManager.PlaySfx(AudioClipId.SfxPlayerAttackSwing);
    }

    public void PlayMeleeHit()
    {
        audioManager.PlaySfx(AudioClipId.SfxPlayerAttackHitFlesh);
    }

    public void PlaySpellCast(string spellId)
    {
        string clip = ResolveSpellCastClip(spellId);
        if (!string.IsNullOrEmpty(clip))
        {
            audioManager.PlaySfx(clip);
        }
    }

    public void PlaySpellImpact(string spellId)
    {
        string clip = ResolveSpellImpactClip(spellId);
        if (!string.IsNullOrEmpty(clip))
        {
            audioManager.PlaySfx(clip);
        }
    }

    public void PlaySpellFailMana()
    {
        audioManager.PlaySfx(AudioClipId.SfxSpellCastFailMana);
    }

    public void PlaySpellFailCooldown()
    {
        audioManager.PlaySfx(AudioClipId.SfxSpellCastFailCooldown);
    }

    public void PlayShopBuy(bool success)
    {
        audioManager.PlaySfx(success ? AudioClipId.SfxShopBuySuccess : AudioClipId.SfxShopBuyFail);
    }

    public void PlayShopSell()
    {
        audioManager.PlaySfx(AudioClipId.SfxShopSellSuccess);
    }

    public void PlayReturnUnlocked()
    {
        audioManager.PlaySfx(AudioClipId.SfxReturnUnlocked);
    }

    public void PlayPortalEnter()
    {
        audioManager.PlaySfx(AudioClipId.SfxPortalEnter);
    }

    public void PlayReturnScroll()
    {
        audioManager.PlaySfx(AudioClipId.SfxReturnScrollTeleport);
    }

    public void PlayEvacuation()
    {
        audioManager.PlaySfx(AudioClipId.SfxEvacuationPointActivate);
    }

    public void PlayAltar(ElementType element)
    {
        audioManager.PlaySfx(element == ElementType.Fire
            ? AudioClipId.SfxAltarFireActivate
            : AudioClipId.SfxAltarWaterActivate);
    }

    public void PlayLocationReached()
    {
        audioManager.PlaySfx(AudioClipId.SfxQuestLocationReached);
    }

    private static string ResolveSpellCastClip(string spellId)
    {
        spellId = ItemCatalog.Normalize(spellId);
        switch (spellId)
        {
            case "spell_firebolt": return AudioClipId.SfxSpellFireboltCast;
            case "spell_infernobolt": return AudioClipId.SfxSpellInfernoboltCast;
            case "spell_airdash": return AudioClipId.SfxSpellAirdashCast;
            case "spell_stoneskin": return AudioClipId.SfxSpellStoneskinCast;
            case "spell_waterspring": return AudioClipId.SfxSpellWaterspringCast;
            case "spell_warchief_wrath": return AudioClipId.SfxSpellWarchiefWrathCast;
            default: return null;
        }
    }

    private static string ResolveSpellImpactClip(string spellId)
    {
        spellId = ItemCatalog.Normalize(spellId);
        switch (spellId)
        {
            case "spell_firebolt": return AudioClipId.SfxSpellFireboltImpact;
            case "spell_infernobolt": return AudioClipId.SfxSpellInfernoboltImpact;
            case "spell_warchief_wrath": return AudioClipId.SfxSpellWarchiefWrathImpact;
            default: return null;
        }
    }
}
