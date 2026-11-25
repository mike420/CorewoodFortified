<table>
<thead>
  <tr>
    <th>Version</th>
    <th>Changes</th>
  </tr>
</thead>
<tbody>
  <tr>
    <td>1.2.7</td>
    <td>
    <ul>
      <li>Fixed auto docking sometimes pushing a ship slowly away from a Dock Capstan when sailing off, with no control at the helm, until "Docking Blocked!" showed. Seen with mods that show and hide parts of a ship.</li>
      <li>A ship being brought in to a capstan is now let go if it takes longer than a minute or ends up out of range.</li>
      <li>Raising or lowering the speed at the helm now lets a ship go while it is still being brought in, not only once it is docked.</li>
      <li>Thinner grass on the Village Hut II turf roof.</li>
    </ul>
    </td>
  </tr>
  <tr>
    <td>1.2.6</td>
    <td>
    <ul>
      <li>Removed Nexus links.</li>
      <li>Added Village Hut II, a second small log hut built at the Workbench from Core Wood, Wood and Stone. Its turf roof grows the game's own grass, like the grass round it.</li>
    </ul>
    </td>
  </tr>
  <tr>
    <td>1.2.5</td>
    <td>
        <ul>
            <li>Updated readme and links.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.2.4</td>
    <td>
        <ul>
            <li>Made the icy Biome Overlays fainter, so the ice no longer covers as much of the wood and stone.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.2.3</td>
    <td>
        <ul>
            <li>Added a Drawbridge Opens For Ships setting (default on). A large drawbridge now opens by itself when a ship comes within 50 m and closes again once none is within 50 m. It waits while a player is on or near the bridge, and only closes a bridge it opened itself.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.2.2</td>
    <td>
        <ul>
            <li>Added an Enable Biome Overlays setting (default off). It gives the wood, log and stone materials of the Corewood Fortified pieces a faint overlay that depends on the biome each piece stands in: ice and snow in the Mountain and the Deep North, moss everywhere else. Only materials used by this mod's pieces change. It also works with the Disable HD materials, and applies straight away, including to pieces already built.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.2.1</td>
    <td>
        <ul>
            <li>Fixed stacks bigger than the normal size being cut back to the normal size whenever a Large Storage was loaded (reopening it, reloading the world, or another player taking over the chest), which lost the rest of the items.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.2.0</td>
    <td>
        <ul>
            <li>Fixed items disappearing from the Large Storage when Multi User Chest, or Azu AutoStore (which has its own copy of it), was installed. They worked out free space from the normal stack size and lost whatever was over it. The Large Storage now tells them to leave it alone.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.1.9</td>
    <td>
        <ul>
            <li>Added more materials to the Low Quality toggle.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.1.8</td>
    <td>
        <ul>
            <li>Added the Large Storage, built at the Workbench from Core Wood, Wood and Iron. Each slot holds up to 65,535 of an item (see Max Stack Size in the config), and items keep their normal stack size everywhere else. While one is open you can Deposit All, trash or recycle its items from the Trash tab (materials go back into the chest, and big stacks ask to confirm first), search its items, sort them by name, weight or type, mark items to keep with you, and split stacks with + and - buttons or the mouse wheel.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.1.7</td>
    <td>
        <ul>
            <li>Fixed a conflict with other mods that refuse items going into an inventory: an item they refused could be lost on Take All instead of staying in the chest.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.1.6</td>
    <td>
        <ul>
            <li>New setting in General Settings: Beacon Unlimited Fuel. If On, a Fortified Brazier Small on a Tower Beacon never runs out of fuel: it is kept full, lights even when empty, and wind and rain do not put it out, but switching it off or covering it over still puts it out (default Off, synced from the server).</li>
            <li>Added the Dock End, built at the Workbench from Core Wood and Wood. Like the other docks, its posts reach down to the ground below.</li>
            <li>Added the Wooden Bridge End, built at the Workbench from Core Wood and Wood.</li>
            <li>Added the Walkway End, built at the Workbench from Core Wood and Wood.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.1.5</td>
    <td>
        <ul>
            <li>Added the Village Hut, a small log hut built at the Workbench from Core Wood, Wood and Stone.</li>
            <li>Added the Village Hut Door, made to fit the Village Hut, built at the Workbench from Core Wood and Bronze.</li>
            <li>Added Dock Steps, wooden steps leading down from a dock, built at the Workbench from Core Wood and Wood. Like the other docks, their posts reach down to the ground below.</li>
            <li>Updated readme and links.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.1.4</td>
    <td>
        <ul>
            <li>Added the Large Drawbridge, a wooden drawbridge on log posts whose legs reach down to the ground below, so it can span a moat or a river. It is built at the Workbench from Core Wood, Wood, Linen Thread and Stone, and has its own open and close sounds.</li>
            <li>Placing a Large Drawbridge also places a second half facing it, so the two meet in the middle as one full drawbridge. This takes materials for both halves, and nothing is built if the other half would be inside a ward or there are not enough materials. The two halves open and close together, and destroying one half destroys the other, each dropping its own materials.</li>
            <li>Added Wooden Bridges at the Workbench: 4m, 8m, Corner, T-Junction and Cross. Their posts reach down to the ground below, so a bridge can be built over a moat, a river or a gap.</li>
            <li>Pieces now show a short description in the build menu, translated in all nine languages.</li>
            <li>Zipline post rope coils now use the rope material.</li>
            <li>Fixed pieces turning transparent when Disable HD was switched back off.</li>
            <li>Fixed a "Destroying object multiple times" warning in the log when a rope was taken down.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.1.3</td>
    <td>
        <ul>
            <li>Added more materials to the Low Quality toggle.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.1.2</td>
    <td>
        <ul>
            <li>(Experimental): Added a Disable HD setting. It swaps the piece materials for low quality versions that look more vanilla like, and toggling it applies straight away, including to pieces already built.</li>
            <li>Fixed a docked ship sometimes slipping its moorings when logging out or moving away from the dock, and drifting off in a storm. The ship now keeps its docking until it is let go from the helm or its Dock Capstan is taken down, and is held still while the dock loads back in.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.1.1</td>
    <td>
        <ul>
            <li>Added multiple snappoints to the Straight dock piece for attaching ladders.</li>
            <li>Fixed the Tower Beacon not revealing the map when a lit small fortified brazier was snapped on its top.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.1.0</td>
    <td>
        <ul>
            <li>New: dock pieces, built with the hammer at the workbench. Dock Straight (8 Round Log, 12 Wood), Dock Corner (9 Round Log, 14 Wood), Dock T-Junction (10 Round Log, 16 Wood), Dock Cross (12 Round Log, 20 Wood) and Dock Capstan, a straight dock with a capstan for mooring (10 Round Log, 16 Wood). Once placed, each post reaches down to the ground below it, so a dock can be built out over water and stands on the sea floor with full support. The posts work out their length again when the ground under the dock is dug or raised.</li>
            <li>New: auto docking. A ship bigger than the raft that sails towards a Dock Capstan from in front of it, within 30m, is brought in by itself, without turning, and held with its hull 6m out from the capstan, tied to the capstan by two ropes, and takes no damage from waves while docked. If a dock lies alongside the ship once it is there, within 15 m, the ship is moved sideways up to it, stopping just short. Raise the speed from the helm to sail off again. If anything is in the way, everyone on board sees "Docking Blocked!". New config section "Docks" with Auto Docking to turn it on or off (synced from the server).</li>
            <li>New: Walkway Cross, built with the hammer at the workbench (10 Round Log, 18 Wood). It splits the walkway four ways.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.0.9</td>
    <td>
        <ul>
            <li>New: Tower Beacon, built with the hammer at the workbench (2 Round Log, 4 Wood). Put a Fortified Brazier Small on the stand on its top and light it: while the brazier burns, the land around the beacon is revealed on your map. Each player's map is revealed when they come near the beacon, and stays revealed after the brazier goes out.</li>
            <li>New: Tower Ladder 6m, built with the hammer at the workbench (6 Round Log). It snaps onto the Tower Beacon and climbs like the other ladders.</li>
            <li>New setting in General Settings: Beacon Reveal Radius, how far around a Tower Beacon the map is revealed, from 100m to 3000m (default 300m, synced from the server).</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.0.8</td>
    <td>
        <ul>
            <li>New: climbable ladders, built with the hammer at the workbench. Ladder 2m (2 Round Log) and Ladder 3m (3 Round Log). Stack them on each other's snap points to climb as one ladder. Press Use to get on, from the ground or from the floor at the top. Forward climbs up and Backward climbs down, and Run climbs faster for stamina. At the top you step off onto the floor across the ladder. Jump pushes off, Crouch drops and Use lets go. A new Climbing skill rises as you climb: it makes fast climbing cheaper on stamina, and from level 20 holding Run while climbing down slides you down. Jumping off uses and raises your Jump skill. Hands and feet take hold of the rungs as you climb, and other players see it too. Climbing and its animation are ported from The Concerned Cat's Concerned Ladders.</li>
            <li>New config section "Ladders": Enable Ladders, Climb Speed, Fast Climb Speed, Fast Climb Stamina, Slide Skill Level, Slide Speed and Slide Stamina (synced from the server).</li>
            <li>New: translations in nine languages: English, Russian, German, Chinese, Brazilian Portuguese, Spanish, French, Polish and Turkish. The files are in BepInEx\config\CorewoodFortified\Translations, one folder per language, and you can edit them. Changes show in game straight away, with no restart. Any text missing from a file falls back to English.</li>
            <li>Fixed Walkway alignments.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.0.7</td>
    <td>
        <ul>
            <li>Fixed Fortified Ballistas breaking on the top of the Fortified Corner Tower. They now take support like iron instead of wood, so they stay up anywhere on the top of a tall piece.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.0.6</td>
    <td>
        <ul>
            <li>New: Walkways, built with the hammer at the workbench. Walkway 2m (2 Core Wood, 4 Wood), Walkway 3m (2 Core Wood, 5 Wood), Walkway 4m (4 Core Wood, 8 Wood), Walkway Corner (8 Core Wood, 14 Wood) and Walkway T-Junction (8 Core Wood, 16 Wood).</li>
            <li>Organ cannon recoils when it fires, kicking back and rolling forward into place on its wheels</li>
            <li>The asset bundle (corewood_fortified) is now a separate file next to CorewoodFortified.dll instead of being packed inside it. This lowers memory use and speeds up loading. If you install manually, copy the whole plugins folder, not just the dll.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.0.5</td>
    <td>
        <ul>
            <li>New: Traps. Flame Trap, Saw Wall and Spike Floor, built at the artisan table. The flame trap burns anyone who steps on it; the others hurt only enemies.</li>
            <li>New: Trap Controller, built at the artisan table. Press Use on it to turn the traps within 30m of it off, and again to turn them back on. Traps only work within range of a controller.</li>
            <li>New: Electric Fence (2m and 4m) and Fence Controller, built at the galdr table. The fence crackles with green lightning, hums, clanks as it switches on and off, and shocks enemies while a Fence Controller within 50m powers it. The controller holds only Eitr and burns one every 10 minutes while its fences are live; looking at it shows how long its power will last, and its range circle shows while placing it or a fence. Shift + Use turns it off and on.</li>
            <li>New: Organ Cannon, built at the forge. Load it with Cannon Balls and it turns on its wheels towards enemies in front of it and fires a volley, one ball from each barrel. Cannon Balls are forged at the forge, 1 Iron each. Like the ballista, it reloads itself from chests within 10m.</li>
            <li>New: Fortified Entrance Trap, built at the artisan table. A gateway with a spiked roller each side: when anyone comes near, the rollers spin up and roll in to meet in the middle, then spin down and roll back once they have gone. The spikes whirr like the Saw Wall while they spin. Its Entrance Trap Trigger Radius setting (synced, default 6m) sets how near, and Entrance Trap Sound Volume (each player's own) how loud.</li>
            <li>New: Floor Blades, built at the artisan table. Blades in the floor that spin fast with a high saw whine when anyone comes near, and cut enemies. Floor Blades Trigger Radius (synced, default 2m) and Floor Blades Sound Volume (each player's own).</li>
            <li>New: Ground Trap, built at the artisan table. Placing it digs a spiked pit into the ground (the ground comes back when it is removed), hidden under a false floor that drops away when anyone steps onto it, and closes over again once the pit is clear. Ground Trap Ignores Players (synced, default off) lets players walk over it safely. Grass from its biome grows on its cover and frame, which Ground Trap Grass (each player's own) can turn off.</li>
            <li>Each trap has a damage setting (synced): Flame Trap Damage, Saw Wall Damage, Spike Floor Damage, Entrance Trap Damage, Floor Blades Damage and Ground Trap Damage, and the Electric Fence has Electric Fence Damage. Placed traps and fences change with them.</li>
            <li>All the sound volume settings are now together in their own Sounds section of the config. The ballista's Scan Sound Volume is now Ballista Scan Sound Volume. Volumes you had changed go back to their defaults once, as they have moved.</li>
            <li>Fortified Ballista and Organ Cannon can be moved. Look at one and press U (Turret Move Key in General Settings): a copy follows where you aim, scroll turns it, attack puts it down, and block or U again leaves it. It keeps its ammo and trophies. Each has an Allow Moving setting, synced from the server.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.0.4</td>
    <td>
        <ul>
            <li>Fixed the turret range marker flooding the log with "The Object you want to instantiate is null" errors while placing turrets</li>
            <li>Fortified Ballista: Spare Buildings. Bolts that miss no longer damage building pieces, so the ballista cannot shoot down your own walls, towers, braziers or other ballistas. On by default.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.0.3</td>
    <td>
        <ul>
            <li>Fixed the Chain Tower placement arrow and link preview not showing, with some other mods installed, until you pressed Tab or picked a different piece. The arrow also always lies flat along the tower's front now.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.0.2</td>
    <td>
        <ul>
            <li>New: Fortified Watchtower levers. Each floor has a lever that calls the platform to that floor, and every player sees it pulled.</li>
            <li>Fortified Watchtower is now built at the Forge and costs 60 Core Wood, 20 Wood, 3 Iron.</li>
            <li>Fixed Fortified Ballista settings (scan arc, sweep time, turning, range, targets, trophies and max ammo) not applying on servers or when changed in game. On a server the ballista now uses the server's settings.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.0.1</td>
    <td>
        <ul>
            <li>New: Fortified Ballista (Forge: 10 Black Metal, 10 Yggdrasil Wood, 3 Mechanical Springs). A ballista on a raised Core Wood base that fires every vanilla bolt and can be given trophies to pick its targets.</li>
            <li>Fortified Ballista: Predictive Aiming. It aims where a moving target will be when the bolt arrives, and allows for the bolt dropping on the way.</li>
            <li>Fortified Ballista: Fast Turning. It gets up to speed and settles on its aim quickly, so it keeps up with moving targets.</li>
            <li>Fortified Ballista: Homing Bolts. Within 25 m of the target, bolts turn in flight towards it.</li>
            <li>Fortified Ballista: Auto Reload. It takes bolts from nearby chests its builder may open, never while someone has the chest open. Safe in multiplayer: every player and the server need this version.</li>
            <li>Fortified Ballista: a scanning sound while it sweeps looking for a target.</li>
            <li>Fortified Ballista: new config section for scan arc, sweep time, turn rate, range, targets, trophies, max ammo, and each of the features above. All but the sound volume are synced from the server.</li>  
            <li>All chain tower and zipline messages and hover text can now be translated: they are in the translation file with everything else.</li>
            <li>Fixed Magic Mike's Mod Settings (F7) not saving: settings changed in the panel were lost when the game was closed. Changed server-synced settings are now also sent on to the other players when the panel closes.</li>
        </ul>
    </td>
  </tr>
  <tr>
    <td>1.0.0</td>
    <td>
        <ul>            
            <li>Initial Release</li>
        </ul>
    </td>
  </tr>
</tbody>
</table>