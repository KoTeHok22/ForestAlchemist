#!/usr/bin/env python3
"""Batch-generate all Forest Alchemist SFX via ElevenLabs website HTTP."""

from __future__ import annotations

import argparse
import sys
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass
from pathlib import Path

from generate_sfx import GenerationResult, generate_sfx, save_variants

ROOT = Path(__file__).resolve().parent
AUDIO_ROOT = ROOT.parent / "Assets" / "Audio" / "SFX"

PREFIX = "short game sound effect, clean, no reverb tail longer than 0.5s unless noted, fantasy, 2D game, "


@dataclass(frozen=True)
class SfxJob:
    rel_path: str
    prompt: str
    loop: bool = False


JOBS: list[SfxJob] = [
    # 2.1 UI
    SfxJob("UI/sfx_ui_button_click.wav", "Short UI click, soft wooden button tap on fantasy game menu, warm mid tone, 0.05-0.12 sec, single hit, no tail, cozy not plastic"),
    SfxJob("UI/sfx_ui_panel_open.wav", "Panel open whoosh, parchment unfold mixed with soft wooden creak, 0.3-0.5 sec, fantasy game UI, satisfying not heavy"),
    SfxJob("UI/sfx_ui_panel_close.wav", "Panel close thud, gentle book close, 0.2-0.4 sec, warm low-mid"),
    SfxJob("UI/sfx_ui_toggle_on.wav", "Soft switch click on, wooden latch, 0.06 sec"),
    SfxJob("UI/sfx_ui_toggle_off.wav", "Soft switch click off, wooden latch, slightly lower pitch, 0.06 sec"),
    SfxJob("UI/sfx_ui_error_deny.wav", "Deny buzz, muted low horn nope, 0.25 sec, not annoying, fantasy game"),
    SfxJob("UI/sfx_ui_success_confirm.wav", "Positive confirm chime, crystal ding, 0.3 sec, bright major third"),
    SfxJob("UI/sfx_ui_notification_quest.wav", "Small quest progress blip, parchment stamp, 0.1 sec, subtle"),
    SfxJob("UI/sfx_ui_notification_quest_complete.wav", "Quest complete fanfare micro, harp and coin shimmer, 0.6-0.8 sec, rewarding"),
    # 2.2 Menu
    SfxJob("UI/sfx_menu_login_success.wav", "Login success, magical door unlock, key turn and soft glow, 0.5 sec"),
    SfxJob("UI/sfx_menu_login_fail.wav", "Login fail, dull thud, 0.2 sec"),
    SfxJob("UI/sfx_menu_register_success.wav", "Registration complete, quill write flourish, 0.4 sec"),
    SfxJob("UI/sfx_menu_new_game_confirm.wav", "Serious confirm gong, single low bell, 0.5 sec, slightly ominous"),
    SfxJob("UI/sfx_menu_exit_game.wav", "Exit whoosh fade, fireplace dim, 0.6 sec"),
    # 2.3 Home
    SfxJob("Home/sfx_home_chest_open.wav", "Old wooden chest lid creak open, metal hinge, 0.6 sec, fantasy RPG"),
    SfxJob("Home/sfx_home_chest_close.wav", "Chest lid close thud, latch, 0.4 sec"),
    SfxJob("Home/sfx_home_shop_bell.wav", "Merchant shop bell on door, brass ding, 0.4 sec, welcoming"),
    SfxJob("Home/sfx_home_craft_station_open.wav", "Alchemy station open, glass clinks, burner hiss, 0.5 sec"),
    SfxJob("Home/sfx_home_quest_board_rustle.wav", "Bulletin board paper rustle, nails on wood, 0.35 sec"),
    SfxJob("Home/sfx_home_quest_accept.wav", "Quest accepted, wax seal stamp, 0.25 sec, decisive"),
    SfxJob("Home/sfx_home_garden_harvest.wav", "Garden harvest, snip plants, basket fill, leaves rustle, 0.5 sec, satisfying"),
    SfxJob("Home/sfx_home_expedition_start.wav", "Expedition departure, forest gate open, wind gust forward, boots on dirt, 1.0 sec, adventurous"),
    SfxJob("Home/sfx_home_stat_upgrade_purchase.wav", "Stat upgrade power surge, body empower thump and magic sparkle, 0.5 sec, satisfying RPG level-up feel"),
    # 2.4 Shop
    SfxJob("Items/sfx_shop_buy_success.wav", "Purchase success, coins or glass vials exchange, merchant handoff, 0.35 sec, positive"),
    SfxJob("Items/sfx_shop_buy_fail.wav", "Empty pouch shake, 0.2 sec"),
    SfxJob("Items/sfx_shop_sell_success.wav", "Sell item, coins received clink, 0.3 sec"),
    SfxJob("Items/sfx_currency_blood_gain.wav", "Blood vial clink, thick liquid slosh in glass, 0.15 sec, slightly wet"),
    # 2.5 Craft
    SfxJob("Home/sfx_craft_success_potion.wav", "Alchemy craft complete, bubble boil pop, cork into bottle, 0.6 sec, satisfying"),
    SfxJob("Home/sfx_craft_success_spell.wav", "Spell inscribed into grimoire, arcane whoosh and page glow, 0.8 sec, epic small moment"),
    SfxJob("Home/sfx_craft_fail_resources.wav", "Craft fail, empty cauldron tap, 0.15 sec"),
    SfxJob("Home/sfx_craft_level_up.wav", "Crafting level up, anvil-lite ding and sparkle, 0.7 sec"),
    # 2.6 Player
    SfxJob("Player/sfx_player_footstep_grass_01.wav", "Single footstep on forest grass and dirt, soft, top-down game, 0.08 sec, mono"),
    SfxJob("Player/sfx_player_footstep_grass_02.wav", "Single footstep on forest grass and dirt, soft, top-down game, 0.08 sec, mono, variant 2"),
    SfxJob("Player/sfx_player_footstep_run_01.wav", "Faster running footstep grass, slightly heavier, 0.06 sec, mono"),
    SfxJob("Player/sfx_player_attack_swing.wav", "Melee sword or staff swing whoosh, light fantasy alchemist weapon, 0.15 sec, airy not metallic heavy"),
    SfxJob("Player/sfx_player_attack_hit_flesh.wav", "Hit impact on creature flesh, wet thud, 0.1 sec, not gory"),
    SfxJob("Player/sfx_player_take_damage.wav", "Player hurt body impact thud and cloth, 0.2 sec, painful, no voice"),
    SfxJob("Player/sfx_player_heal.wav", "Healing sparkle wash, warm chime ascending, 0.4 sec"),
    SfxJob("Player/sfx_player_death.wav", "Player death fall, body collapse on grass, spirit dissipate whisper, 1.0 sec"),
    SfxJob("Player/sfx_player_mana_restore.wav", "Mana refill, blue magic glug and chime, 0.35 sec"),
    # 2.7 Shield/Buffs
    SfxJob("Player/sfx_shield_apply.wav", "Magic shield activate, stone or energy barrier form, low hum start, 0.5 sec"),
    SfxJob("Player/sfx_shield_hit_absorb.wav", "Shield absorb impact, dull energy deflect, 0.12 sec"),
    SfxJob("Player/sfx_shield_break.wav", "Shield shatter, glass-crystal crack, 0.35 sec"),
    SfxJob("Player/sfx_buff_consumable_drink.wav", "Potion drink gulp, cork pop before, 0.4 sec"),
    SfxJob("Player/sfx_buff_scroll_unfurl.wav", "Magic scroll unfurl, paper and arcane spark, 0.35 sec"),
    # 2.8 Spells
    SfxJob("Spells/sfx_spell_firebolt_cast.wav", "Small fire spell cast, quick whoosh and flame puff, 0.25 sec, orange bright"),
    SfxJob("Spells/sfx_spell_firebolt_impact.wav", "Fire impact on enemy, flame burst pop, 0.2 sec"),
    SfxJob("Spells/sfx_spell_infernobolt_cast.wav", "Heavy fire spell cast, roaring flame charge, 0.4 sec, powerful"),
    SfxJob("Spells/sfx_spell_infernobolt_travel_loop.wav", "Large fireball travel, deeper rumble, 0.5 sec seamless loop", loop=True),
    SfxJob("Spells/sfx_spell_infernobolt_impact.wav", "Explosive fire impact, AoE splash, 0.35 sec, bass thump"),
    SfxJob("Spells/sfx_spell_warchief_wrath_cast.wav", "Ultimate orc-fire spell cast, war cry energy and flame, 0.6 sec, epic"),
    SfxJob("Spells/sfx_spell_warchief_wrath_impact.wav", "Massive fire explosion, debris, 0.5 sec"),
    SfxJob("Spells/sfx_spell_waterspring_cast.wav", "Water healing spring, bubbling rise, gentle wave chime, 0.5 sec, soothing blue"),
    SfxJob("Spells/sfx_spell_stoneskin_cast.wav", "Earth armor spell, rocks grind up around body, stone clack, 0.5 sec"),
    SfxJob("Spells/sfx_spell_airdash_cast.wav", "Wind dash burst, air slash whoosh, quick teleport glide, 0.3 sec"),
    SfxJob("Spells/sfx_spell_airdash_land.wav", "Dash end skid on grass, 0.15 sec"),
    SfxJob("Spells/sfx_spell_cast_fail_mana.wav", "Not enough mana, dull fizzle, 0.2 sec"),
    SfxJob("Spells/sfx_spell_cast_fail_cooldown.wav", "On cooldown, soft clock tick, 0.1 sec"),
    SfxJob("Spells/sfx_spell_unlocked.wav", "First time spell added to hotbar, arcane unlock sparkle chime, 0.5 sec, rewarding"),
    # 2.10 Enemies
    SfxJob("Enemies/sfx_enemy_orc_aggro.wav", "Orc growl alert, short aggressive grunt no words, 0.3 sec"),
    SfxJob("Enemies/sfx_enemy_orc_attack_melee.wav", "Orc melee swipe, claw or club whoosh, 0.2 sec"),
    SfxJob("Enemies/sfx_enemy_orc_hit.wav", "Orc pain grunt, flesh hit, 0.15 sec, no words"),
    SfxJob("Enemies/sfx_enemy_orc_death.wav", "Orc death fall, body drop, 0.5 sec"),
    SfxJob("Enemies/sfx_enemy_shaman_cast.wav", "Shaman curse cast, rattling bones and magic whisper, 0.4 sec"),
    SfxJob("Enemies/sfx_enemy_shaman_projectile.wav", "Dark magic orb travel whoosh, 0.3 sec"),
    SfxJob("Enemies/sfx_enemy_shaman_projectile_hit.wav", "Magic poison hit on player, splat, 0.2 sec"),
    SfxJob("Enemies/sfx_enemy_boss_roar.wav", "Boss orc roar, deep and wide, 1.0 sec, intimidating, no words"),
    SfxJob("Enemies/sfx_enemy_boss_stomp.wav", "Heavy stomp attack, ground shake, 0.35 sec"),
    SfxJob("Enemies/sfx_enemy_boss_death.wav", "Boss collapse, earth rumble, 1.2 sec"),
    SfxJob("Enemies/sfx_enemy_base_wave_spawn.wav", "Distant war drums crescendo, orcs arriving, 0.8 sec"),
    SfxJob("Enemies/sfx_enemy_base_defeated.wav", "Orc camp defeated, fire extinguish, victory thud, 1.0 sec"),
    # 2.11 World
    SfxJob("World/sfx_gather_start.wav", "Gather start, hands on bark, tool ready, 0.2 sec"),
    SfxJob("World/sfx_gather_loop.wav", "Tree chopping or picking loop, soft rhythm, 2 sec seamless, quiet", loop=True),
    SfxJob("World/sfx_gather_complete.wav", "Gather success, branch snap, item pop into bag, 0.35 sec"),
    SfxJob("World/sfx_gather_cancel.wav", "Gather cancel, disappointed rustle, 0.15 sec"),
    SfxJob("World/sfx_gather_flower_complete.wav", "Rare flower pick, magical sparkle, 0.35 sec"),
    SfxJob("World/sfx_altar_fire_activate.wav", "Fire altar, flame pillar ignite, crackling rise, 1.0 sec, powerful"),
    SfxJob("World/sfx_altar_water_activate.wav", "Water altar, spring burst, water rush up, 1.0 sec, serene"),
    SfxJob("World/sfx_evacuation_point_activate.wav", "Evacuation beacon activate, sci-fantasy flare whistle, 0.6 sec"),
    SfxJob("World/sfx_portal_enter.wav", "Magic portal enter, swirl vortex, 0.8 sec"),
    SfxJob("World/sfx_return_scroll_teleport.wav", "Return scroll teleport home, paper burn and warp, 0.7 sec"),
    SfxJob("World/sfx_return_unlocked.wav", "Return path unlocked, distant horn and chime, 0.5 sec"),
    SfxJob("World/sfx_weather_storm_thunder.wav", "Thunder crack one-shot, 1-2 sec, dramatic"),
    SfxJob("World/sfx_weather_change_whoosh.wav", "Weather shift whoosh, 0.5 sec"),
    # 2.12 Inventory
    SfxJob("Items/sfx_item_pickup_generic.wav", "Item pickup blip, soft pop, 0.08 sec"),
    SfxJob("Items/sfx_item_pickup_rare.wav", "Rare item pickup, shimmer chime, 0.25 sec"),
    # 2.13 Expedition
    SfxJob("Expedition/sfx_expedition_inventory_lost.wav", "Items dissolving away, magic dispel downward, 0.6 sec, sad"),
    # 2.15 Quests
    SfxJob("Expedition/sfx_quest_boss_complete.wav", "Boss quest complete, war horn and chime, 0.8 sec"),
    SfxJob("Expedition/sfx_quest_location_reached.wav", "Location objective complete, flag plant, 0.4 sec"),
]


def run_job(
    job: SfxJob,
    *,
    skip_existing: bool,
    jitter_sec: float,
    free_proxy: bool,
) -> tuple[SfxJob, list[GenerationResult] | None, str | None]:
    dest = AUDIO_ROOT / job.rel_path
    if skip_existing and dest.exists() and dest.stat().st_size > 1000:
        return job, None, "skipped"

    prompt = PREFIX + job.prompt
    try:
        variants = generate_sfx(prompt, jitter_sec=jitter_sec, free_proxy=free_proxy)
        results = save_variants(
            variants,
            prompt=prompt,
            output_file=dest,
            output_dir=ROOT / "output",
            name_prefix=dest.stem,
            save_all=False,
            convert_wav=False,
        )
        return job, results, None
    except Exception as exc:
        return job, None, str(exc)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Batch-generate Forest Alchemist SFX")
    parser.add_argument("-j", "--jobs", type=int, default=8, help="Parallel workers (default 8)")
    parser.add_argument("--skip-existing", action="store_true", help="Skip files that already exist")
    parser.add_argument("--only", nargs="*", help="Generate only matching path substrings")
    parser.add_argument(
        "--jitter",
        type=float,
        default=2.0,
        help="Max random delay (sec) before each request — fresh fingerprint + spacing",
    )
    parser.add_argument(
        "--free-proxy",
        action="store_true",
        help="Use free public proxies instead of Proxy6",
    )
    args = parser.parse_args(argv)

    jobs = JOBS
    if args.only:
        needles = [n.lower() for n in args.only]
        jobs = [j for j in jobs if any(n in j.rel_path.lower() for n in needles)]

    workers = 1 if args.free_proxy else args.jobs
    print(f"Generating {len(jobs)} SFX with {workers} workers -> {AUDIO_ROOT}")
    if args.free_proxy:
        print("Proxy mode: free public lists")
    t0 = time.time()
    ok = skip = fail = 0
    errors: list[str] = []

    with ThreadPoolExecutor(max_workers=workers) as pool:
        futures = {
            pool.submit(
                run_job,
                j,
                skip_existing=args.skip_existing,
                jitter_sec=args.jitter,
                free_proxy=args.free_proxy,
            ): j
            for j in jobs
        }
        for fut in as_completed(futures):
            job, results, err = fut.result()
            if err == "skipped":
                skip += 1
                print(f"SKIP {job.rel_path}")
            elif err:
                fail += 1
                errors.append(f"{job.rel_path}: {err}")
                print(f"FAIL {job.rel_path}: {err}", file=sys.stderr)
            else:
                ok += 1
                dur = results[0].duration_seconds if results else "?"
                print(f"OK   {job.rel_path} ({dur}s)")

    elapsed = time.time() - t0
    print(f"\nDone in {elapsed:.1f}s — ok={ok} skip={skip} fail={fail}")
    if errors:
        print("\nFailures:", file=sys.stderr)
        for e in errors:
            print(f"  {e}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
