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
        public static ConfigEntry<bool> disableHDTextures;
        public static ConfigEntry<KeyboardShortcut> turretMoveKey;
        public static ConfigEntry<bool> enableElevators;
        public static ConfigEntry<bool> autoTriggers;
        public static ConfigEntry<float> elevatorSpeed;
        public static ConfigEntry<bool> turretAllowMoving;
        public static ConfigEntry<float> beaconRevealRadius;
        public static ConfigEntry<bool> beaconUnlimitedFuel;
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

        public static ConfigEntry<bool> enableLadders;
        public static ConfigEntry<float> ladderClimbSpeed;
        public static ConfigEntry<float> ladderFastClimbSpeed;
        public static ConfigEntry<float> ladderFastClimbStamina;
        public static ConfigEntry<float> ladderSlideSkillLevel;
        public static ConfigEntry<float> ladderSlideSpeed;
        public static ConfigEntry<float> ladderSlideStamina;

        public static ConfigEntry<bool> enableAutoDocking;

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

        public static ConfigEntry<bool> enableOrganCannon;
        public static ConfigEntry<int> cannonRange;
        public static ConfigEntry<int> cannonFireArc;
        public static ConfigEntry<float> cannonReloadTime;
        public static ConfigEntry<int> cannonVolleySize;
        public static ConfigEntry<float> cannonBallDamage;
        public static ConfigEntry<int> cannonMaxAmmo;
        public static ConfigEntry<bool> cannonSpareBuildings;
        public static ConfigEntry<bool> cannonTargetPlayers;
        public static ConfigEntry<bool> cannonTargetTamed;
        public static ConfigEntry<bool> cannonTargetEnemies;
        public static ConfigEntry<bool> cannonAutoReload;
        public static ConfigEntry<float> cannonAutoReloadRange;
        public static ConfigEntry<bool> cannonAllowMoving;



        public static ConfigEntry<bool> enableTraps;
        public static ConfigEntry<float> trapControllerRadius;
        public static ConfigEntry<float> fenceControllerRadius;
        public static ConfigEntry<float> fenceEitrMinutes;
        public static ConfigEntry<float> electricFenceDamage;
        public static ConfigEntry<float> flameTrapTriggerRadius;
        public static ConfigEntry<float> sawWallTriggerRadius;
        public static ConfigEntry<float> spikeFloorTriggerRadius;
        public static ConfigEntry<float> entranceTrapTriggerRadius;
        public static ConfigEntry<float> floorBladesTriggerRadius;
        public static ConfigEntry<float> floorBladesDamage;
        public static ConfigEntry<bool> groundTrapIgnoresPlayers;
        public static ConfigEntry<bool> groundTrapGrass;
        public static ConfigEntry<float> groundTrapDamage;
        public static ConfigEntry<float> flameTrapDamage;
        public static ConfigEntry<float> sawWallDamage;
        public static ConfigEntry<float> spikeFloorDamage;
        public static ConfigEntry<float> entranceTrapDamage;


        public static ConfigEntry<float> gateSoundVolume;
        public static ConfigEntry<float> largeDrawbridgeSoundVolume;
        public static ConfigEntry<float> chainTowerSoundVolume;
        public static ConfigEntry<float> elevatorSoundVolume;
        public static ConfigEntry<float> ziplineSoundVolume;
        public static ConfigEntry<float> turretSoundVolume;
        public static ConfigEntry<float> flameTrapSoundVolume;
        public static ConfigEntry<float> sawWallSoundVolume;
        public static ConfigEntry<float> spikeFloorSoundVolume;
        public static ConfigEntry<float> entranceTrapSoundVolume;
        public static ConfigEntry<float> electricFenceSoundVolume;
        public static ConfigEntry<float> fenceControllerSoundVolume;
        public static ConfigEntry<float> floorBladesSoundVolume;


        public static void InitConfigs(ConfigFile config)
        {
            modEnabled = config.BindConfigInOrder("General Settings", "Enable Plugin", defaultValue: true, "Enable the CorewoodFortified plugin.\n(Restart required. It is read before connecting to a server, so it is a local setting).\n", synced: false, sectionOrder: true, settingOrder: true);
            disableHDTextures = config.BindConfigInOrder("General Settings", "Disable HD", defaultValue: false, "Enabling this swaps the piece materials for low quality versions that look more vanilla like.\n", synced: false, sectionOrder: true, settingOrder: true);
            disableHDTextures.SettingChanged += (_, _) => LowQualityMaterials.OnSettingChanged();
            enableElevators =config.BindConfigInOrder("General Settings", "Enable Elevators", defaultValue: true, "If Off, elevators are hidden from the build menu and placed elevators do not move. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            autoTriggers = config.BindConfigInOrder("General Settings", "Automatic Elevators", defaultValue: true, "If On, elevators move automatically when a player steps on the platform. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            elevatorSpeed = config.BindConfigInOrder("General Settings", "Elevator Speed", defaultValue: 2f, "How fast elevator platforms move, in metres per second. A trip already under way keeps the speed it started with. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0.5f, 10f));
            turretMoveKey = config.BindConfigInOrder("General Settings", "Turret Move Key", defaultValue: new KeyboardShortcut(UnityEngine.KeyCode.U), "Look at a fortified ballista or organ cannon and press this key to pick it up and put it down somewhere else: aim where it should go, scroll to turn it, attack to put it down, block or this key again to leave it where it was.", synced: false, sectionOrder: true, settingOrder: true);
            beaconRevealRadius = config.BindConfigInOrder("General Settings", "Beacon Reveal Radius", defaultValue: 300f, "How far around a tower beacon the map is revealed while a lit small fortified brazier stands on its top, in metres. The brazier has to be burning: fuelled, not switched off and not covered over. The map is made of 64 m squares, so the edge of the circle is rough. Each player's map is revealed when the beacon is loaded near them, and raising this reveals the extra. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(100f, 3000f));
            beaconUnlimitedFuel = config.BindConfigInOrder("General Settings", "Beacon Unlimited Fuel", defaultValue: false, "If On, a small fortified brazier standing on a tower beacon's top never runs out of fuel: it is kept full, lights even when empty, and wind and rain do not put it out. Switching it off or covering it over still puts it out. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);

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
            turretAllowMoving = config.BindConfigInOrder("Fortified Ballista", "Allow Moving", defaultValue: true, "If On, a placed fortified ballista can be picked up with the Turret Move Key and put down somewhere else, keeping its bolts and trophies. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);

            enableOrganCannon = config.BindConfigInOrder("Organ Cannon", "Enable Organ Cannon", defaultValue: true, "If Off, the organ cannon is hidden from the build menu, and placed ones stay but do not aim, fire or take cannon balls. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            cannonRange = config.BindConfigInOrder("Organ Cannon", "Range", defaultValue: 40, "How far away the organ cannon finds and fires at enemies, in metres. The range circle matches it. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<int>(10, 60));
            cannonFireArc = config.BindConfigInOrder("Organ Cannon", "Fire Arc", defaultValue: 60, "How far the organ cannon turns from one side to the other, in degrees. It only fires at enemies inside this arc in front of it. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<int>(10, 120));
            cannonReloadTime = config.BindConfigInOrder("Organ Cannon", "Reload Time", defaultValue: 6f, "Seconds between the organ cannon's volleys. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 30f));
            cannonVolleySize = config.BindConfigInOrder("Organ Cannon", "Volley Size", defaultValue: 13, "How many of the organ cannon's 13 barrels fire in each volley. Each barrel fires one cannon ball. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<int>(1, 13));
            cannonBallDamage = config.BindConfigInOrder("Organ Cannon", "Cannon Ball Damage", defaultValue: 40f, "Blunt damage of each cannon ball. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(5f, 300f));
            cannonMaxAmmo = config.BindConfigInOrder("Organ Cannon", "Max Ammo", defaultValue: 65, "How many cannon balls an organ cannon holds. Balls already loaded above a lowered limit stay until they are fired. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<int>(13, 260));
            cannonSpareBuildings = config.BindConfigInOrder("Organ Cannon", "Spare Buildings", defaultValue: true, "If On, cannon balls that miss do no damage to building pieces. If Off, they damage whatever they hit. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            cannonTargetPlayers = config.BindConfigInOrder("Organ Cannon", "Target Players", defaultValue: false, "If On, the organ cannon can fire at players. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            cannonTargetTamed = config.BindConfigInOrder("Organ Cannon", "Target Tames", defaultValue: false, "If On, the organ cannon can fire at tamed creatures. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            cannonTargetEnemies = config.BindConfigInOrder("Organ Cannon", "Target Enemies", defaultValue: true, "If On, the organ cannon fires at enemies. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            cannonAutoReload = config.BindConfigInOrder("Organ Cannon", "Auto Reload", defaultValue: true, "If On, an organ cannon that is not full takes cannon balls from chests nearby, every few seconds. It only uses chests its builder may open, and never while someone has the chest open. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            cannonAutoReloadRange = config.BindConfigInOrder("Organ Cannon", "Auto Reload Range", defaultValue: 10f, "How far from an organ cannon a chest can be for it to take cannon balls from, in metres. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(2f, 20f));
            cannonAllowMoving = config.BindConfigInOrder("Organ Cannon", "Allow Moving", defaultValue: true, "If On, a placed organ cannon can be picked up with the Turret Move Key and put down somewhere else, keeping its cannon balls and trophies. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);

            enableTraps = config.BindConfigInOrder("Traps", "Enable Traps", defaultValue: true, "If Off, the traps and the trap controller are hidden from the build menu, and placed traps stay but are switched off and do no damage. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            trapControllerRadius = config.BindConfigInOrder("Traps", "Trap Controller Radius", defaultValue: 30f, "How far from a trap controller its traps can be, in metres. Placed controllers and their range circles change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(15f, 200f));
            flameTrapTriggerRadius = config.BindConfigInOrder("Traps", "Flame Trap Trigger Radius", defaultValue: 8f, "How close a creature or player has to come to a switched-on flame trap for it to go off, in metres. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 20f));
            sawWallTriggerRadius = config.BindConfigInOrder("Traps", "Saw Wall Trigger Radius", defaultValue: 8f, "How close a creature or player has to come to a switched-on saw wall for it to go off, in metres. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 20f));
            spikeFloorTriggerRadius = config.BindConfigInOrder("Traps", "Spike Floor Trigger Radius", defaultValue: 8f, "How close a creature or player has to come to a switched-on spike floor for it to go off, in metres. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 20f));
            entranceTrapTriggerRadius = config.BindConfigInOrder("Traps", "Entrance Trap Trigger Radius", defaultValue: 6f, "How close a creature or player has to come to a switched-on fortified entrance trap for its spikes to close in, in metres. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 20f));
            floorBladesTriggerRadius = config.BindConfigInOrder("Traps", "Floor Blades Trigger Radius", defaultValue: 2f, "How close a creature or player has to come to switched-on floor blades for them to spin, in metres. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 20f));
            flameTrapDamage = config.BindConfigInOrder("Traps", "Flame Trap Damage", defaultValue: 100f, "How much fire damage the flame trap does each time it hits. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 1000f));
            sawWallDamage = config.BindConfigInOrder("Traps", "Saw Wall Damage", defaultValue: 50f, "How much slash damage each of the saw wall's blades does each time it hits. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 1000f));
            spikeFloorDamage = config.BindConfigInOrder("Traps", "Spike Floor Damage", defaultValue: 100f, "How much pierce damage the spike floor does each time it hits. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 1000f));
            entranceTrapDamage = config.BindConfigInOrder("Traps", "Entrance Trap Damage", defaultValue: 100f, "How much slash damage each of the fortified entrance trap's spiked rollers does each time it hits. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 1000f));
            floorBladesDamage = config.BindConfigInOrder("Traps", "Floor Blades Damage", defaultValue: 50f, "How much slash damage the floor blades do each time they hit. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 1000f));
            groundTrapDamage = config.BindConfigInOrder("Traps", "Ground Trap Damage", defaultValue: 50f, "How much pierce damage the ground trap's spikes do each time they hit. Placed traps change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 1000f));
            groundTrapIgnoresPlayers = config.BindConfigInOrder("Traps", "Ground Trap Ignores Players", defaultValue: false, "If On, players never open a ground trap or keep it open, so they can walk over it safely; only creatures do. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            groundTrapGrass = config.BindConfigInOrder("Traps", "Ground Trap Grass", defaultValue: true, "If On, grass like the grass round it grows on a ground trap's cover and frame, so it is harder to spot. Placed traps change too.", synced: false, sectionOrder: true, settingOrder: true);

            fenceControllerRadius = config.BindConfigInOrder("Electric Fence", "Fence Controller Radius", defaultValue: 50f, "How far from a fence controller its electric fences can be, in metres. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(5f, 200f));
            fenceEitrMinutes = config.BindConfigInOrder("Electric Fence", "Minutes Per Eitr", defaultValue: 10f, "How many minutes one Eitr keeps a fence controller's fences live. It only burns while the controller is on and has a fence in range. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 120f));
            electricFenceDamage = config.BindConfigInOrder("Electric Fence", "Electric Fence Damage", defaultValue: 100f, "How much lightning damage a live electric fence does each time it shocks. Placed fences change too. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 1000f));

            enableLadders = config.BindConfigInOrder("Ladders", "Enable Ladders", defaultValue: true, "If Off, ladders are hidden from the build menu, and placed ladders stay but cannot be climbed. Anyone on a ladder lets go. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);
            ladderClimbSpeed = config.BindConfigInOrder("Ladders", "Climb Speed", defaultValue: 0.9f, "How fast you climb up and down a ladder, in metres per second. The climbing animation is made for 0.9. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0.3f, 4f));
            ladderFastClimbSpeed = config.BindConfigInOrder("Ladders", "Fast Climb Speed", defaultValue: 1.2f, "How fast you climb while holding Run, in metres per second. The climbing animation is made for 1.2. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0.3f, 6f));
            ladderFastClimbStamina = config.BindConfigInOrder("Ladders", "Fast Climb Stamina", defaultValue: 8f, "Stamina used per second while climbing fast, up to 25% less with the Climbing skill. 0 = free. Out of stamina, you climb at the normal speed. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 30f));
            ladderSlideSkillLevel = config.BindConfigInOrder("Ladders", "Slide Skill Level", defaultValue: 20f, "The Climbing skill level from which holding Run while climbing down slides you down the ladder. 0 = anyone can slide. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 100f));
            ladderSlideSpeed = config.BindConfigInOrder("Ladders", "Slide Speed", defaultValue: 4.5f, "How fast you slide down a ladder, in metres per second. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 10f));
            ladderSlideStamina = config.BindConfigInOrder("Ladders", "Slide Stamina", defaultValue: 2f, "Stamina used per second while sliding down, up to 25% less with the Climbing skill. 0 = free. Out of stamina, you climb down at the normal speed. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 30f));

            enableAutoDocking = config.BindConfigInOrder("Docks", "Auto Docking", defaultValue: true, "If On, a ship bigger than the raft that sails towards a dock capstan from in front of it, within 30 m, is brought in by itself, without turning, and held with its hull 6 m out from the capstan. Raise the speed from the helm to sail off again. If anything is in the way, everyone on board is told docking is blocked. Synced from the server.", synced: true, sectionOrder: true, settingOrder: true);

            gateSoundVolume = config.BindConfigInOrder("Sounds", "Gate Sound Volume", defaultValue: 1f, "Volume of the fortified lift gate and drawbridge sounds, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            largeDrawbridgeSoundVolume = config.BindConfigInOrder("Sounds", "Large Drawbridge Sound Volume", defaultValue: 1f, "Volume of the large and roofed fortified drawbridge sounds, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            elevatorSoundVolume = config.BindConfigInOrder("Sounds", "Elevator Sound Volume", defaultValue: 0.5f, "Volume of the elevator sound, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            chainTowerSoundVolume = config.BindConfigInOrder("Sounds", "Chain Tower Sound Volume", defaultValue: 0.5f, "Volume of the chain tower winch sounds, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            ziplineSoundVolume = config.BindConfigInOrder("Sounds", "Zipline Sound Volume", defaultValue: 1f, "Volume of the rope sound while riding, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            turretSoundVolume = config.BindConfigInOrder("Sounds", "Ballista Scan Sound Volume", defaultValue: 0.5f, "Volume of the fortified ballista's sound while it sweeps looking for a target, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            flameTrapSoundVolume = config.BindConfigInOrder("Sounds", "Flame Trap Sound Volume", defaultValue: 1f, "Volume of the flame trap's sound while it burns, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            sawWallSoundVolume = config.BindConfigInOrder("Sounds", "Saw Wall Sound Volume", defaultValue: 1f, "Volume of the saw wall's sound while it cuts, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            spikeFloorSoundVolume = config.BindConfigInOrder("Sounds", "Spike Floor Sound Volume", defaultValue: 1f, "Volume of the spike floor's sound each time its spikes go up or down, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            entranceTrapSoundVolume = config.BindConfigInOrder("Sounds", "Entrance Trap Sound Volume", defaultValue: 1f, "Volume of the fortified entrance trap's sound while its spikes spin, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            floorBladesSoundVolume = config.BindConfigInOrder("Sounds", "Floor Blades Sound Volume", defaultValue: 1f, "Volume of the floor blades' sound while they spin, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            electricFenceSoundVolume = config.BindConfigInOrder("Sounds", "Electric Fence Sound Volume", defaultValue: 1f, "Volume of the electric fence's hum while it is live, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            fenceControllerSoundVolume = config.BindConfigInOrder("Sounds", "Fence Controller Sound Volume", defaultValue: 0.5f, "Volume of the fence controller's sound while it powers its fences, from 0 (silent) to 1 (full).", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));

            CarryOverOldSections(config);

            // Decimal settings rounded (and kept in range) whichever editor sets them
            MagicMike.ModSettings.ConfigRounding.RoundAll(config);
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
            HashSet<ConfigEntryBase> handled = new HashSet<ConfigEntryBase>();
            bool saveOnSet = config.SaveOnConfigSet;
            config.SaveOnConfigSet = false;
            try
            {
                // An earlier renumbering can leave the same setting under several old numbers; the highest is the latest
                foreach (ConfigDefinition old in orphans.Keys.OrderByDescending(d => SectionNumber(d.Section)).ToList())
                {
                    string oldName = SectionName(old.Section);
                    ConfigEntryBase entry = ((IDictionary<ConfigDefinition, ConfigEntryBase>)config).Values.FirstOrDefault(e => e.Definition.Key == old.Key && e.Definition.Section != old.Section
                        && (SectionName(e.Definition.Section) == oldName || OldSections.TryGetValue(SectionName(e.Definition.Section), out string[] before) && before.Contains(oldName)));
                    if (entry == null)
                    {
                        continue;   // (not one of ours any more: left as it is)
                    }
                    if (handled.Add(entry) && Equals(entry.BoxedValue, entry.DefaultValue))
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

        private static int SectionNumber(string section)
        {
            Match match = Regex.Match(section ?? "", @"^(\d+)\s*-");
            return match.Success ? int.Parse(match.Groups[1].Value) : -1;
        }
    }
}
