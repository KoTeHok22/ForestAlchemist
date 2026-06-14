#!/usr/bin/env python3
"""Delete redundant SFX wav files (see AUDIO.md)."""
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent / "Assets" / "Audio" / "SFX"

REMOVED = [
    "UI/sfx_ui_button_hover.wav",
    "UI/sfx_ui_typewriter.wav",
    "UI/sfx_ui_slider_tick.wav",
    "UI/sfx_ui_tab_switch.wav",
    "UI/sfx_hud_heart_lost.wav",
    "UI/sfx_hud_quest_tracker_update.wav",
    "UI/sfx_minimap_ping_player.wav",
    "UI/sfx_menu_records_open.wav",
    "Home/sfx_home_interact_highlight.wav",
    "Home/sfx_home_garden_stage_grow.wav",
    "Home/sfx_home_expedition_prep_open.wav",
    "Home/sfx_home_expedition_prep_item_add.wav",
    "Home/sfx_home_expedition_prep_item_remove.wav",
    "Home/sfx_home_stat_upgrade_open.wav",
    "Home/sfx_home_stat_upgrade_max.wav",
    "Home/sfx_craft_bubbling_loop.wav",
    "Items/sfx_currency_blood_spend.wav",
    "Items/sfx_inventory_open.wav",
    "Items/sfx_inventory_close.wav",
    "Items/sfx_item_stack.wav",
    "Player/sfx_player_footstep_grass_03.wav",
    "Player/sfx_player_footstep_run_02.wav",
    "Player/sfx_player_footstep_run_03.wav",
    "Player/sfx_player_stamina_low.wav",
    "Player/sfx_player_attack_hit_miss.wav",
    "Player/sfx_hotbar_slot_use.wav",
    "Player/sfx_hotbar_cooldown_ready.wav",
    "Player/sfx_hotbar_empty.wav",
    "Spells/sfx_spell_firebolt_travel_loop.wav",
    "Spells/sfx_spell_waterspring_aura_loop.wav",
    "Enemies/sfx_enemy_orc_footstep.wav",
    "Enemies/sfx_enemy_green_orc_idle.wav",
    "Enemies/sfx_enemy_blue_orc_attack.wav",
    "Enemies/sfx_enemy_boss_trophy_drop.wav",
    "World/sfx_gather_sakura_complete.wav",
    "World/sfx_gather_oak_complete.wav",
    "World/sfx_gather_apple_complete.wav",
    "World/sfx_weather_rain_loop.wav",
    "World/sfx_weather_fog_wind.wav",
    "World/sfx_weather_heatwave_cicadas.wav",
    "World/sfx_world_chunk_ambient.wav",
    "World/sfx_world_statue_ambient.wav",
    "Expedition/sfx_expedition_result_success.wav",
    "Expedition/sfx_expedition_result_death.wav",
    "Expedition/sfx_expedition_loot_transfer.wav",
    "Expedition/sfx_pause_open.wav",
    "Expedition/sfx_pause_close.wav",
    "Expedition/sfx_quest_reward_blood.wav",
]

if __name__ == "__main__":
    deleted = missing = 0
    for rel in REMOVED:
        path = ROOT / rel
        if path.exists():
            path.unlink()
            deleted += 1
            print(f"deleted {rel}")
        else:
            missing += 1
        meta = ROOT / f"{rel}.meta"
        if meta.exists():
            meta.unlink()
            print(f"deleted meta {rel}.meta")
    print(f"\n{deleted} deleted, {missing} missing, {len(REMOVED)} total")
