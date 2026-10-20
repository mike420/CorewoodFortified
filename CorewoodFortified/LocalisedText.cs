using System;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;

namespace CorewoodFortified;

internal class LocalisedText
{
    public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

    private static readonly Dictionary<string, string> English = new Dictionary<string, string>
    {
        { "piece_mm_corewood_fortified_entrance", "Fortified Entrance" },
        { "piece_mm_corewood_fortified_wall", "Fortified Wall" },
        { "piece_mm_corewood_fortified_wall_stairs", "Fortified Wall Stairs" },
        { "piece_mm_corewood_fortified_wall_stairs_alt", "Fortified Wall Stairs (Alt)" },
        { "piece_mm_corewood_fortified_corner", "Fortified Corner" },
        { "piece_mm_corewood_fortified_corner_alt", "Fortified Corner (Alt)" },
        { "piece_mm_corewood_fortified_corner_curved", "Fortified Curved Corner" },
        { "piece_mm_corewood_fortified_tjunction", "Fortified T-Junction" },
        { "piece_mm_corewood_fortified_wall_elevator", "Elevator Wall" },
        { "piece_mm_corewood_fortified_gates", "Fortified Gates" },
        { "piece_mm_corewood_fortified_drawbridge", "Fortified Drawbridge" },
        { "piece_mm_corewood_fortified_liftgate", "Fortified Lift Gate" },
        { "piece_mm_corewood_fortified_brazier_small", "Fortified Brazier Small" },
        { "piece_mm_corewood_fortified_brazier_large", "Fortified Brazier Large" },
        { "piece_mm_corewood_fortified_spikes", "Fortified Spikes" },
        { "piece_mm_corewood_fortified_spikes_bronze", "Bronze Spikes" },
        { "piece_mm_corewood_fortified_spikes_iron", "Iron Spikes" },
        { "piece_mm_corewood_fortified_spikes_blackmetal", "Black Metal Spikes" },
        { "piece_mm_corewood_fortified_spikes_flametal", "Flametal Spikes" },
        { "piece_mm_corewood_fortified_spikes_gold", "Bloodgold Spikes" },
        { "piece_mm_corewood_fortified_entrance_roofed", "Roofed Entrance" },
        { "piece_mm_corewood_fortified_drawbridge_roofed", "Roofed Drawbridge" },
        { "piece_mm_corewood_fortified_wall_roofed", "Roofed Wall" },
        { "piece_mm_corewood_fortified_wall_stairs_roofed", "Roofed Stairs" },
        { "piece_mm_corewood_fortified_wall_stairs_alt_roofed", "Roofed Stairs (Alt)" },
        { "piece_mm_corewood_fortified_corner_roofed", "Roofed Corner" },
        { "piece_mm_corewood_fortified_corner_roofed_alt", "Roofed Corner (Alt)" },
        { "piece_mm_corewood_fortified_corner_curved_roofed", "Roofed Curved Corner" },
        { "piece_mm_corewood_fortified_tjunction_roofed", "Roofed T-Junction" },
        { "piece_mm_corewood_fortified_fence", "Fortified Fence" },
        { "piece_mm_corewood_fortified_fence_large", "Large Fortified Fence" },
        { "piece_mm_corewood_fortified_fence_windowed", "Windowed Fortified Fence" },
        { "piece_mm_corewood_fortified_fence_gate", "Fortified Fence Gate" },
        { "piece_mm_corewood_fortified_drawbridge_large", "Large Fortified Drawbridge" },
        { "piece_mm_corewood_internal_wall", "Internal Wall" },
        { "piece_mm_corewood_internal_doorway", "Internal Doorway" },
        { "piece_mm_corewood_internal_corner", "Internal Corner" },
        { "piece_mm_corewood_internal_corner_curved", "Internal Corner Curved" },
        { "piece_mm_corewood_internal_tjunction", "Internal T-Junction" },
        { "piece_mm_corewood_internal_wall_roofed", "Roofed Internal Wall" },
        { "piece_mm_corewood_chain_tower_small", "Small Chain Tower 8m" },
        { "piece_mm_corewood_chain_tower_medium", "Medium Chain Tower 15m" },
        { "piece_mm_corewood_chain_tower_large", "Large Chain Tower 21m" },
        { "piece_mm_corewood_chain_tower_description", "One end of a harbour chain, a log blockhouse. Build a second tower facing it across the water: the winch raises a net of chains from the seabed." },
        { "piece_mm_corewood_fortified_watchtower", "Fortified Watchtower" },
        { "piece_mm_corewood_zipline_post", "Zipline Post" },
        { "piece_mm_corewood_zipline_post_description", "A log post with an iron pulley. Take a rope from it to another post, then ride the rope between them." },
        { "piece_mm_corewood_fortified_turret", "Fortified Ballista" },
        { "piece_mm_corewood_fortified_turret_description", "A ballista on a raised log base. Load it with bolts and it fires at enemies in range." },
        { "piece_mm_corewood_organ_cannon", "Organ Cannon" },
        { "piece_mm_corewood_organ_cannon_description", "Thirteen barrels on a wheeled carriage. Load it with cannon balls and it fires a volley at enemies in front of it." },
        { "piece_mm_corewood_flame_trap", "Flame Trap" },
        { "piece_mm_corewood_flame_trap_description", "The floor is fire. Burns anyone who walks over it." },
        { "piece_mm_corewood_saw_wall", "Saw Wall" },
        { "piece_mm_corewood_saw_wall_description", "Deck the walls with rusty saws. Cuts enemies that come near." },
        { "piece_mm_corewood_spike_floor", "Spike Floor" },
        { "piece_mm_corewood_spike_floor_description", "The floor is spikes. Stabs enemies that walk over it." },
        { "piece_mm_corewood_fortified_entrance_trap", "Fortified Entrance Trap" },
        { "piece_mm_corewood_floor_blades", "Floor Blades" },
        { "piece_mm_corewood_ground_trap", "Ground Trap" },
        { "piece_mm_corewood_ground_trap_description", "A spiked pit dug into the ground under a false floor. The floor drops away when enemies come near." },
        { "piece_mm_corewood_floor_blades_description", "Blades set into the floor that whirl into life and cut the legs from enemies that come near." },
        { "piece_mm_corewood_fortified_entrance_trap_description", "A gateway with a spiked roller each side. They spin up and close in on enemies who try to come through." },
        { "piece_mm_corewood_electric_fence", "Electric Fence 2m" },
        { "piece_mm_corewood_electric_fence_4m", "Electric Fence 4m" },
        { "piece_mm_corewood_electric_fence_description", "Crackling beams that shock enemies who touch them. Live only while a powered Fence Controller is nearby." },
        { "piece_mm_corewood_fence_controller", "Fence Controller" },
        { "piece_mm_corewood_fence_controller_description", "Powers the electric fences around it. Holds only Eitr, and burns it while its fences are live." },
        { "piece_mm_corewood_trap_controller", "Trap Controller" },
        { "piece_mm_corewood_trap_controller_description", "Turns the traps around it on and off. Traps only work within range of a controller." },
        { "piece_mm_corewood_walkway_2m", "Walkway 2m" },
        { "piece_mm_corewood_walkway_2m_description", "A 2 metre wooden walkway." },
        { "piece_mm_corewood_walkway_3m", "Walkway 3m" },
        { "piece_mm_corewood_walkway_3m_description", "A 3 metre wooden walkway." },
        { "piece_mm_corewood_walkway_4m", "Walkway 4m" },
        { "piece_mm_corewood_walkway_4m_description", "A 4 metre wooden walkway." },
        { "piece_mm_corewood_walkway_corner", "Walkway Corner" },
        { "piece_mm_corewood_walkway_corner_description", "A wooden walkway corner that turns the path at a right angle." },
        { "piece_mm_corewood_walkway_tjunction", "Walkway T-Junction" },
        { "piece_mm_corewood_walkway_tjunction_description", "A wooden walkway junction that splits the path three ways." },
        { "item_mm_cannonball", "Cannon Ball" },
        { "item_mm_cannonball_description", "An iron ball for the organ cannon. Forged one for each iron." },
        { "text_organ_cannons_disabled", "Organ cannons are disabled" },
        { "text_cannon_add_ammo", "Add Cannonball" },
        { "text_turret_move", "Move" },
        { "text_turret_move_help", "Aim where it should go and attack to put it down. Scroll to turn it. Block or {key} to leave it." },
        { "text_turret_moved", "Moved" },
        { "text_turret_move_cancelled", "Left where it was" },
        { "text_fence_controller_turned_on", "Fences turned on" },
        { "text_fence_controller_turned_off", "Fences turned off" },
        { "text_fence_needs_controller", "Needs a Fence Controller nearby" },
        { "text_fence_no_power", "No Eitr" },
        { "text_fence_power_lasts", "Power lasts" },
        { "text_fortified_lift_gate", "Fortified Lift Gate" },
        { "text_fortified_gates", "Fortified Gates" },
        { "text_fortified_drawbridge", "Fortified Drawbridge" },
        { "text_drawbridge", "Drawbridge" },
        { "text_use_elevator", "Use Elevator" },
        { "text_elevators_disabled", "Elevators are disabled" },
        { "text_ziplines_disabled", "Ziplines are disabled" },
        { "text_chain_towers_disabled", "Chain towers are disabled" },
        { "text_turrets_disabled", "Fortified ballistas are disabled" },
        { "text_traps_disabled", "Traps are disabled" },
        { "text_trap_needs_controller", "Needs a Trap Controller nearby" },
        { "text_trap_controller_turned_on", "Traps turned on" },
        { "text_trap_controller_turned_off", "Traps turned off" },
        { "text_trap_controller_off", "Off" },
        { "text_metres", "$1 m" },
        { "text_chain_tower", "Chain tower" },
        { "text_chain_too_far", "Too far: $1 m (at most $2)" },
        { "text_chain_too_close", "Too close: $1 m (at least $2)" },
        { "text_chain_must_face", "The towers must face each other" },
        { "text_chain_links_across", "Links across $1 m" },
        { "text_chain_not_linked", "(not linked)" },
        { "text_chain_not_linked_msg", "Not linked" },
        { "text_chain_ship_in_way", "a ship is in the way" },
        { "text_chain_raised", "raised" },
        { "text_chain_lowered", "lowered" },
        { "text_chain_raising", "raising, $1 s" },
        { "text_chain_lowering", "lowering, $1 s" },
        { "text_chain_raise_net", "Raise the net" },
        { "text_chain_lower_net", "Lower the net" },
        { "text_zipline_rope_taken_down", "A rope from the moved post was taken down: $1" },
        { "text_zipline_rope_taken_down_short", "Rope taken down: $1" },
        { "text_zipline_in_the_way", "Something is in the way" },
        { "text_zipline_put_back", "Put the rope back" },
        { "text_zipline_tie_here", "Tie the rope here ($1 m)" },
        { "text_zipline_ride", "Ride the zipline ($1 m, $2)" },
        { "text_zipline_level", "level" },
        { "text_zipline_down", "down $1 m" },
        { "text_zipline_up", "up $1 m" },
        { "text_zipline_rope_of", "Rope $1 of $2 - look along a rope to choose it" },
        { "text_zipline_take_rope", "Take a rope to another post" },
        { "text_zipline_slot_used", "The rope slot is used" },
        { "text_zipline_slots_used", "All $1 rope slots are used" },
        { "text_zipline_holding_this", "You are holding a rope from this post. Hold $KEY_AltPlace and press $KEY_Use to put it back." },
        { "text_zipline_holding_this_short", "$KEY_AltPlace + $KEY_Use puts the rope back" },
        { "text_zipline_tie_hint", "Hold $KEY_AltPlace and press $KEY_Use to tie the rope here" },
        { "text_zipline_tie_hint_short", "$KEY_AltPlace + $KEY_Use ties the rope here" },
        { "text_zipline_no_rope", "No rope on this post yet. Hold $KEY_AltPlace and press $KEY_Use to take one to another post." },
        { "text_zipline_no_rope_short", "No rope here yet" },
        { "text_zipline_get_up", "Get up first: you need both hands free to take a rope" },
        { "text_zipline_get_up_short", "Get up first to take a rope" },
        { "text_zipline_post_holds_one", "This post already holds a rope" },
        { "text_zipline_post_holds_many", "This post already holds $1 ropes" },
        { "text_zipline_post_full", "This post is full" },
        { "text_zipline_rope_in_hand", "Rope in hand. Walk to another zipline post (up to $1 m away), hold $KEY_AltPlace and press $KEY_Use. Hold $KEY_Use while looking at nothing to drop it." },
        { "text_zipline_rope_in_hand_short", "Rope in hand ($1 m) · hold $KEY_Use on nothing to drop" },
        { "text_zipline_put_rope_back", "You put the rope back" },
        { "text_zipline_put_rope_back_short", "Rope put back" },
        { "text_zipline_not_tied", "The rope could not be tied (see the BepInEx log)" },
        { "text_zipline_not_tied_short", "The rope could not be tied" },
        { "text_zipline_ready", "Zipline ready: $1 m. Press $KEY_Use at either post to ride." },
        { "text_zipline_ready_short", "Zipline ready: $1 m" },
        { "text_zipline_source_gone", "The post you took this rope from is gone" },
        { "text_zipline_too_close", "Too close to the other post" },
        { "text_zipline_too_far", "Too far: $1 m (the rope is $2 m long)" },
        { "text_zipline_too_steep", "Too steep ($1°, max $2°)" },
        { "text_zipline_no_slots", "This post has no free rope slots" },
        { "text_zipline_source_no_slots", "The post you took this rope from has no free rope slots any more" },
        { "text_zipline_connected", "These posts are already connected" },
        { "text_zipline_hits_ground", "Too low: a rider would hit the ground $1 m along. Raise one end." },
        { "text_zipline_hits_water", "Too low: a rider would drag through the water $1 m along" },
        { "text_zipline_blocked", "Blocked by $1 $2 m along" },
        { "text_zipline_building_piece", "a building piece ($1)" },
        { "text_zipline_the_ground", "the ground" },
        { "text_zipline_portal_short", "The rope slipped back (no ropes through portals)" },
        { "text_zipline_post_gone_short", "Rope dropped: its post is gone" },
        { "text_zipline_ran_out_short", "The rope ran out ($1 m)" },
        { "text_zipline_post_moved_short", "Rope dropped: its post moved" },
        { "text_zipline_portal", "You can't take a rope through a portal, so it slipped back to the post" },
        { "text_zipline_post_gone", "The post you took the rope from is gone, so you dropped the rope" },
        { "text_zipline_ran_out", "The rope ran out ($1 m) and slipped back to the post" },
        { "text_zipline_let_go", "You let go of the rope and it slipped back to the post" },
        { "text_zipline_post_moved", "The post you took the rope from moved, so you dropped the rope" },
        { "text_zipline_turned_off", "Ziplines were turned off, so you dropped the rope" },
        { "text_zipline_turned_off_short", "Rope dropped: ziplines are off" },
        { "text_zipline_went_wrong", "Something went wrong with the rope, so you dropped it" },
        { "text_zipline_went_wrong_short", "Rope dropped (something went wrong)" },
        { "text_zipline_almost_out", "Rope almost out: $1 m left" },
        { "text_zipline_already_riding", "You are already on a zipline" },
        { "text_zipline_rope_in_hand_first", "Put the rope in your hand back first" },
        { "text_zipline_holding_something", "You are already holding on to something" },
        { "text_zipline_swimming", "You can't reach the rope while swimming" },
        { "text_zipline_cart", "Let go of the cart first" },
        { "text_zipline_too_short", "This rope is too short to ride" },
        { "text_zipline_not_tied_far", "This rope isn't tied to anything at the other end" },
        { "text_zipline_no_grab", "You couldn't grab the rope" },
        { "text_zipline_controls", "Zipline: hold $KEY_Forward to pull yourself along and $KEY_Backward to brake; the mouse turns you. $KEY_Jump or $KEY_Use lets go." },
        { "text_zipline_controls_short", "$KEY_Forward pulls · $KEY_Backward brakes · $KEY_Jump lets go" },
        { "text_zipline_too_heavy", "You're carrying too much to pull yourself along" },
        { "text_zipline_too_heavy_short", "Too heavy to pull yourself along" },
        { "text_zipline_out_of_stamina", "Out of stamina: keep holding $KEY_Forward to hang on and catch your breath, then you pull on. Let go of $KEY_Forward to slide back." },
        { "text_zipline_out_of_stamina_short", "Out of stamina: hold $KEY_Forward to hang on" },
        { "text_zipline_came_down", "The rope came down" }
    };

    // One translated text for text built in code, with its $1, $2... filled in with the values given
    public static string Get(string key, params string[] values)
    {
        global::Localization game = global::Localization.instance;
        return game != null ? game.Localize("$" + key, values) : key;
    }

    // Fills in only the English keys the translation JSON files did not provide,
    // so an outdated or missing JSON never leaves raw tokens in game.
    public static int AddMissingLocalisations()
    {
        int added = 0;
        foreach (KeyValuePair<string, string> entry in English)
        {
            if (!Localization.Contains("English", entry.Key))
            {
                Localization.AddTranslation("English", entry.Key, entry.Value);
                added++;
            }
        }
        return added;
    }

    // Reloads the translation files and pushes the result into the game's live dictionary,
    // so edits show without a restart and keys removed from the files stop being used.
    public static void Reload(Action loadFiles)
    {
        HashSet<string> previousKeys = new HashSet<string>();
        foreach (string language in Localization.GetLanguages())
        {
            IReadOnlyDictionary<string, string> translations = Localization.GetTranslations(language);
            if (translations != null)
            {
                previousKeys.UnionWith(translations.Keys);
            }
        }

        Localization.ClearAll();
        try
        {
            loadFiles();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning("[CorewoodFortified] Could not reload the translation files: " + ex.Message);
        }
        AddMissingLocalisations();
        ApplyToGame(previousKeys);
    }

    // English goes in first so the selected language only overrides the keys it provides.
    private static void ApplyToGame(IEnumerable<string> previousKeys)
    {
        global::Localization game = global::Localization.instance;
        if (game == null)
        {
            return;
        }
        foreach (string key in previousKeys)
        {
            game.m_translations.Remove(key);
        }
        string selected = game.GetSelectedLanguage();
        foreach (string language in selected == "English" ? new[] { "English" } : new[] { "English", selected })
        {
            IReadOnlyDictionary<string, string> translations = Localization.GetTranslations(language);
            if (translations == null)
            {
                continue;
            }
            foreach (KeyValuePair<string, string> entry in translations)
            {
                game.AddWord(entry.Key, entry.Value);
            }
        }
    }
}
