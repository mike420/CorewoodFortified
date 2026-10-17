using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using Jotunn.Extensions;

namespace CorewoodFortified
{
    internal class MainConfig
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> gateSoundVolume;
        public static ConfigEntry<float> largeDrawbridgeSoundVolume;
        public static ConfigEntry<float> chainTowerSoundVolume;
        public static ConfigEntry<float> elevatorSoundVolume;
        public static ConfigEntry<bool> enableElevators;
        public static ConfigEntry<bool> autoTriggers;
        public static ConfigEntry<float> elevatorSpeed;
        public static ConfigEntry<bool> enableChainTowers;
        public static ConfigEntry<float> chainMaxSpanSmall;
        public static ConfigEntry<float> chainMaxSpanMedium;
        public static ConfigEntry<float> chainMaxSpanLarge;

        public static ConfigEntry<bool> enableZiplines;
        public static ConfigEntry<float> ziplineMaxRopeLength;
        public static ConfigEntry<float> ziplineMaxSlope;
        public static ConfigEntry<int> ziplineMaxRopesPerPost;
        public static ConfigEntry<bool> ziplineRequireClearance;
        public static ConfigEntry<float> ziplineRopeSag;
        public static ConfigEntry<float> ziplineMaxSpeed;
        public static ConfigEntry<float> ziplineGravity;
        public static ConfigEntry<float> ziplineCrawlSpeed;
        public static ConfigEntry<float> ziplinePullSpeed;
        public static ConfigEntry<float> ziplinePullStamina;
        public static ConfigEntry<float> ziplineBrakeStrength;
        public static ConfigEntry<bool> ziplineAutoBrake;
        public static ConfigEntry<float> ziplineArrivalSpeed;
        public static ConfigEntry<string> ziplineRideAnimation;
        public static ConfigEntry<float> ziplineHangHeight;
        public static ConfigEntry<float> ziplineRopeWidth;
        public static ConfigEntry<bool> ziplineGrabOnTouch;
        public static ConfigEntry<bool> ziplineGroundAfterLogout;
        public static ConfigEntry<ZiplineMessages.Level> ziplineMessages;
        public static ConfigEntry<float> ziplineSoundVolume;

        public static ConfigEntry<bool> enableTurrets;
        public static ConfigEntry<int> turretScanArc;
        public static ConfigEntry<float> turretSweepTime;
        public static ConfigEntry<int> turretTurnRate;
        public static ConfigEntry<bool> turretFastTurning;
        public static ConfigEntry<int> turretRange;
        public static ConfigEntry<bool> turretPredictiveAim;
        public static ConfigEntry<bool> turretHomingBolts;
        public static ConfigEntry<bool> turretSpareBuildings;
        public static ConfigEntry<bool> turretTargetPlayers;
        public static ConfigEntry<bool> turretTargetTamed;
        public static ConfigEntry<bool> turretTargetEnemies;
        public static ConfigEntry<int> turretMaxTrophies;
        public static ConfigEntry<int> turretMaxAmmo;
        public static ConfigEntry<bool> turretAutoReload;
        public static ConfigEntry<float> turretAutoReloadRange;
        public static ConfigEntry<float> turretSoundVolume;

        public static ConfigEntry<bool> enableTraps;
        public static ConfigEntry<float> trapControllerRadius;
        public static ConfigEntry<float> flameTrapSoundVolume;
        public static ConfigEntry<float> sawWallSoundVolume;
        public static ConfigEntry<float> spikeFloorSoundVolume;

        public static ConfigEntry<float> fenceControllerRadius;
        public static ConfigEntry<float> fenceEitrMinutes;
        public static ConfigEntry<float> electricFenceSoundVolume;
        public static ConfigEntry<float> flameTrapTriggerRadius;
        public static ConfigEntry<float> sawWallTriggerRadius;
        public static ConfigEntry<float> spikeFloorTriggerRadius;

        public static void InitConfigs(ConfigFile config)
        {
            modEnabled = config.BindConfigInOrder("General Settings", "Enable Plugin", defaultValue: true, "Enable the CorewoodFortified plugin.\n(Restart required. It is read before connecting to a server, so it is a local setting).\n", synced: false, sectionOrder: true, settingOrder: true);
            gateSoundVolume = config.BindConfigInOrder("General Settings", "Gate Sound Volume", defaultValue: 1f, "Volume of the fortified lift gate and drawbridge sounds, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            largeDrawbridgeSoundVolume = config.BindConfigInOrder("General Settings", "Large Drawbridge Sound Volume", defaultValue: 1f, "Volume of the large and roofed fortified drawbridge sounds, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            chainTowerSoundVolume = config.BindConfigInOrder("General Settings", "Chain Tower Sound Volume", defaultValue: 0.5f, "Volume of the chain tower winch sounds, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            enableElevators = config.BindConfigInOrder("General Settings", "Enable Elevators", defaultValue: true, "If Off, elevators are hidden from the build menu and placed elevators do not move. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            autoTriggers = config.BindConfigInOrder("General Settings", "Automatic Elevators", defaultValue: true, "If On, elevators move automatically when a player steps on the platform. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            elevatorSpeed = config.BindConfigInOrder("General Settings", "Elevator Speed", defaultValue: 2f, "How fast elevator platforms move, in metres per second. A trip already under way keeps the speed it started with. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0.5f, 10f));
            elevatorSoundVolume = config.BindConfigInOrder("General Settings", "Elevator Sound Volume", defaultValue: 0.5f, "Volume of the elevator sound, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));

            enableChainTowers = config.BindConfigInOrder("Chain Towers", "Enable Chain Towers", defaultValue: true, "If Off, chain towers are hidden from the build menu, and placed towers stay but their winches cannot be used. A net keeps the height it had. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            chainMaxSpanSmall = config.BindConfigInOrder("Chain Towers", "Max Span Small", defaultValue: 15f, "The longest net a small chain tower can hold, in metres (ring to ring). Two towers of different sizes together hold the shorter of their two. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(15f, 64f));
            chainMaxSpanMedium = config.BindConfigInOrder("Chain Towers", "Max Span Medium", defaultValue: 30f, "The longest net a medium chain tower can hold, in metres (ring to ring). Two towers of different sizes together hold the shorter of their two. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(15f, 64f));
            chainMaxSpanLarge = config.BindConfigInOrder("Chain Towers", "Max Span Large", defaultValue: 60f, "The longest net a large chain tower can hold, in metres (ring to ring). Two towers of different sizes together hold the shorter of their two. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(15f, 60f));

            enableZiplines = config.BindConfigInOrder("Ziplines", "Enable Ziplines", defaultValue: true, "If Off, the zipline post is hidden from the build menu, placed posts and their ropes stay but cannot be used, and ropes cannot be caught in mid-air. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            ziplineMaxRopeLength = config.BindConfigInOrder("Ziplines", "Max Rope Length", defaultValue: 150f, "The longest rope you can tie between two zipline posts, in metres. Ropes over about 250 m may not show from the far end. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(10f, 400f));
            ziplineMaxSlope = config.BindConfigInOrder("Ziplines", "Max Slope", defaultValue: 60f, "The steepest rope allowed, in degrees from horizontal. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(10f, 80f));
            ziplineMaxRopesPerPost = config.BindConfigInOrder("Ziplines", "Max Ropes Per Post", defaultValue: 4, "How many ropes one zipline post can hold. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<int>(1, 8));
            ziplineRequireClearance = config.BindConfigInOrder("Ziplines", "Require Clearance", defaultValue: true, "If On, ropes are refused where a rider would scrape the ground, water, trees, rocks or buildings on the way. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            ziplineRopeSag = config.BindConfigInOrder("Ziplines", "Rope Sag", defaultValue: 0.02f, "How much a new rope sags in the middle, as a share of its length (0.02 = 2 m on a 100 m rope). Ropes already tied keep the sag they were tied with. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 0.1f));
            ziplineMaxSpeed = config.BindConfigInOrder("Ziplines", "Max Speed", defaultValue: 20f, "Top speed on a zipline, in metres per second. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(4f, 40f));
            ziplineGravity = config.BindConfigInOrder("Ziplines", "Gravity", defaultValue: 1f, "How strongly a downhill rope speeds you up. 1 = real gravity. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0.2f, 3f));
            ziplineCrawlSpeed = config.BindConfigInOrder("Ziplines", "Crawl Speed", defaultValue: 0f, "Minimum speed towards the far post, in metres per second, so riders arrive without pulling. 0 = off: uphill you hold Forward to pull yourself up. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 6f));
            ziplinePullSpeed = config.BindConfigInOrder("Ziplines", "Pull Speed", defaultValue: 4f, "Speed, in metres per second, when you hold Forward to pull yourself along the rope. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 10f));
            ziplinePullStamina = config.BindConfigInOrder("Ziplines", "Pull Stamina", defaultValue: 6f, "Stamina used per second while pulling yourself along. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 50f));
            ziplineBrakeStrength = config.BindConfigInOrder("Ziplines", "Brake Strength", defaultValue: 10f, "How fast Backward (or Block) slows you down, in metres per second per second. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 40f));
            ziplineAutoBrake = config.BindConfigInOrder("Ziplines", "Auto Brake", defaultValue: true, "If On, riders slow down automatically before the far post. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            ziplineArrivalSpeed = config.BindConfigInOrder("Ziplines", "Arrival Speed", defaultValue: 3f, "Speed, in metres per second, that Auto Brake slows riders to before the far post. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0.5f, 10f));
            ziplineRideAnimation = config.BindConfigInOrder("Ziplines", "Ride Animation", defaultValue: "hang", "Body pose while riding. hang = hanging from the trolley with both arms up, repell = the rappel pose, grappling = arms forward, attach_chair = sitting, none = no pose.", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueList<string>("hang", "repell", "grappling", "attach_chair", "none"));
            ziplineHangHeight = config.BindConfigInOrder("Ziplines", "Hang Height", defaultValue: 2.3f, "Distance from the rope down to your feet while riding, in metres.", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1.2f, 3f));
            ziplineRopeWidth = config.BindConfigInOrder("Ziplines", "Rope Thickness", defaultValue: 0.05f, "Thickness of the drawn zipline ropes, in metres.", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0.01f, 0.2f));
            ziplineGrabOnTouch = config.BindConfigInOrder("Ziplines", "Grab Rope In Mid-Air", defaultValue: true, "If On, jumping or falling into a zipline rope catches it and rides on in the direction you were going. If Off, you only get on at a post.", synced: false, sectionOrder: true, settingOrder: true);
            ziplineGroundAfterLogout = config.BindConfigInOrder("Ziplines", "Log Back In On The Ground", defaultValue: true, "If On, logging out while riding brings you back on the ground by a post of that rope, not in the air where you hung.", synced: false, sectionOrder: true, settingOrder: true);
            ziplineMessages = config.BindConfigInOrder("Ziplines", "Messages", defaultValue: ZiplineMessages.Level.Short, "Short: a few words in the top-left corner. All: the full help in the middle of the screen. Off: no messages.", synced: false, sectionOrder: true, settingOrder: true);
            ziplineSoundVolume = config.BindConfigInOrder("Ziplines", "Zipline Sound Volume", defaultValue: 1f, "Volume of the rope sound while riding, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));

            enableTurrets = config.BindConfigInOrder("Fortified Ballista", "Enable Fortified Ballista", defaultValue: true, "If Off, the fortified ballista is hidden from the build menu, and placed ones stay but do not turn, fire or take bolts, by hand or from chests. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            turretScanArc = config.BindConfigInOrder("Fortified Ballista", "Scan Arc", defaultValue: 100, "How wide the fortified ballista sweeps while looking for a target, in degrees from one side to the other. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<int>(10, 178));
            turretSweepTime = config.BindConfigInOrder("Fortified Ballista", "Sweep Time", defaultValue: 7f, "Seconds the fortified ballista takes to sweep from one side of its scan arc to the other. Lower sweeps faster. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 15f));
            turretTurnRate = config.BindConfigInOrder("Fortified Ballista", "Turn Rate", defaultValue: 45, "How fast the fortified ballista turns, in degrees per second, both while sweeping and while following a target. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<int>(15, 90));
            turretFastTurning = config.BindConfigInOrder("Fortified Ballista", "Fast Turning", defaultValue: true, "If On, the fortified ballista gets up to its turn rate and settles on its aim quickly, so it keeps up with moving targets and fires sooner. If Off, it speeds up and slows down as gently as the vanilla ballista. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            turretRange = config.BindConfigInOrder("Fortified Ballista", "Range", defaultValue: 30, "How far away the fortified ballista finds and shoots at targets, in metres. The range circle matches it. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<int>(3, 50));
            turretPredictiveAim = config.BindConfigInOrder("Fortified Ballista", "Predictive Aiming", defaultValue: true, "If On, the fortified ballista aims where a moving target will be when the bolt reaches it, and allows for the bolt dropping on the way. If Off, it aims like the vanilla ballista. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            turretHomingBolts = config.BindConfigInOrder("Fortified Ballista", "Homing Bolts", defaultValue: true, "If On, bolts from the fortified ballista turn in flight towards the target they were fired at, once within 25 m of it. They turn only so fast, so a dodging target can still be missed. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            turretSpareBuildings = config.BindConfigInOrder("Fortified Ballista", "Spare Buildings", defaultValue: true, "If On, bolts from the fortified ballista that miss do no damage to building pieces, including other ballistas. If Off, they damage whatever they hit, like the vanilla ballista. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            turretTargetPlayers = config.BindConfigInOrder("Fortified Ballista", "Target Players", defaultValue: false, "If On, the fortified ballista can shoot at players. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            turretTargetTamed = config.BindConfigInOrder("Fortified Ballista", "Target Tames", defaultValue: false, "If On, the fortified ballista can shoot at tamed creatures. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            turretTargetEnemies = config.BindConfigInOrder("Fortified Ballista", "Target Enemies", defaultValue: true, "If On, the fortified ballista shoots at enemies. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            turretMaxTrophies = config.BindConfigInOrder("Fortified Ballista", "Max Target Trophies", defaultValue: 10, "How many trophies can be loaded into a fortified ballista to pick which creatures it shoots at. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<int>(1, 10));
            turretMaxAmmo = config.BindConfigInOrder("Fortified Ballista", "Max Ammo", defaultValue: 40, "How many bolts a fortified ballista holds. Bolts already loaded above a lowered limit stay until they are fired. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<int>(5, 100));
            turretAutoReload = config.BindConfigInOrder("Fortified Ballista", "Auto Reload", defaultValue: true, "If On, a fortified ballista that is not full takes bolts from chests nearby, every few seconds. It only uses chests its builder may open, never while someone has the chest open, and only the kind of bolt already loaded. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            turretAutoReloadRange = config.BindConfigInOrder("Fortified Ballista", "Auto Reload Range", defaultValue: 10f, "How far from a fortified ballista a chest can be for it to take bolts from, in metres. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(2f, 20f));
            turretSoundVolume = config.BindConfigInOrder("Fortified Ballista", "Scan Sound Volume", defaultValue: 0.5f, "Volume of the fortified ballista's sound while it sweeps looking for a target, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));

            enableTraps = config.BindConfigInOrder("Traps", "Enable Traps", defaultValue: true, "If Off, the traps and the trap controller are hidden from the build menu, and placed traps stay but are switched off and do no damage. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            trapControllerRadius = config.BindConfigInOrder("Traps", "Trap Controller Radius", defaultValue: 50f, "How far from a trap controller its traps can be, in metres. Placed controllers and their range circles change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(15f, 200f));
            flameTrapTriggerRadius = config.BindConfigInOrder("Traps", "Flame Trap Trigger Radius", defaultValue: 8f, "How close a creature or player has to come to a switched-on flame trap for it to go off, in metres. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 20f));
            sawWallTriggerRadius = config.BindConfigInOrder("Traps", "Saw Wall Trigger Radius", defaultValue: 8f, "How close a creature or player has to come to a switched-on saw wall for it to go off, in metres. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 20f));
            spikeFloorTriggerRadius = config.BindConfigInOrder("Traps", "Spike Floor Trigger Radius", defaultValue: 8f, "How close a creature or player has to come to a switched-on spike floor for it to go off, in metres. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 20f));
            flameTrapSoundVolume = config.BindConfigInOrder("Traps", "Flame Trap Sound Volume", defaultValue: 1f, "Volume of the flame trap's sound while it burns, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            sawWallSoundVolume = config.BindConfigInOrder("Traps", "Saw Wall Sound Volume", defaultValue: 1f, "Volume of the saw wall's sound while it cuts, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            spikeFloorSoundVolume = config.BindConfigInOrder("Traps", "Spike Floor Sound Volume", defaultValue: 1f, "Volume of the spike floor's sound each time its spikes go up or down, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));

            fenceControllerRadius = config.BindConfigInOrder("Electric Fence", "Fence Controller Radius", defaultValue: 20f, "How far from a fence controller its electric fences can be, in metres. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(5f, 100f));
            fenceEitrMinutes = config.BindConfigInOrder("Electric Fence", "Minutes Per Eitr", defaultValue: 10f, "How many minutes one Eitr keeps a fence controller's fences live. It only burns while the controller is on and has a fence in range. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 120f));
            electricFenceSoundVolume = config.BindConfigInOrder("Electric Fence", "Electric Fence Sound Volume", defaultValue: 1f, "Volume of the electric fence's hum while it is live, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));

            CarryOverOldSections(config);
        }

        private static readonly Dictionary<string, string[]> OldSections = new Dictionary<string, string[]>
        {
        };

        private static void CarryOverOldSections(ConfigFile config)
        {
            Dictionary<ConfigDefinition, string> orphans;
            try
            {
                PropertyInfo property = typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                orphans = property?.GetValue(config) as Dictionary<ConfigDefinition, string>;
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogWarning("[CorewoodFortified] Could not look for settings under old section names: " + e.Message);
                return;
            }
            if (orphans == null || orphans.Count == 0)
            {
                return;
            }

            int carried = 0, dropped = 0;
            bool saveOnSet = config.SaveOnConfigSet;
            config.SaveOnConfigSet = false;
            try
            {
                foreach (ConfigDefinition old in orphans.Keys.ToList())
                {
                    string oldName = SectionName(old.Section);
                    ConfigEntryBase entry = ((IDictionary<ConfigDefinition, ConfigEntryBase>)config).Values.FirstOrDefault(e => e.Definition.Key == old.Key && e.Definition.Section != old.Section
                        && (SectionName(e.Definition.Section) == oldName || OldSections.TryGetValue(SectionName(e.Definition.Section), out string[] before) && before.Contains(oldName)));
                    if (entry == null)
                    {
                        continue;   // (not one of ours any more: left as it is)
                    }
                    if (Equals(entry.BoxedValue, entry.DefaultValue))
                    {
                        try
                        {
                            entry.SetSerializedValue(orphans[old]);
                            carried++;
                        }
                        catch (System.Exception e)
                        {
                            Jotunn.Logger.LogWarning("[CorewoodFortified] Could not carry over " + old.Section + " / " + old.Key + ": " + e.Message);
                        }
                    }
                    orphans.Remove(old);
                    dropped++;
                }
            }
            finally
            {
                config.SaveOnConfigSet = saveOnSet;
            }
            if (dropped > 0)
            {
                config.Save();
                Jotunn.Logger.LogInfo("[CorewoodFortified] Carried " + carried + " setting(s) over from old section names in the config file.");
            }
        }

        // A section's name without the number in front ("4 - Fortified Ballista": Fortified Ballista)
        private static string SectionName(string section) => Regex.Replace(section ?? "", @"^\d+\s*-\s*", "");
    }
}
