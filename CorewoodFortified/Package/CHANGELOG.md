<table>
<thead>
  <tr>
    <th>Version</th>
    <th>Changes</th>
  </tr>
</thead>
<tbody>
  <tr>
    <td>1.0.5</td>
    <td>
        <ul>
            <li>New: Traps. Flame Trap, Saw Wall and Spike Floor, built at the forge. The flame trap burns anyone who steps on it; the others hurt only enemies.</li>
            <li>New: Trap Controller, built at the workbench. Press Use on it to turn the traps within 30m of it off, and again to turn them back on. Traps only work within range of a controller.</li>
            <li>New config section: Traps. Enable Traps turns all traps off and hides them from the build menu, and Trap Controller Radius sets how far a controller reaches (15m to 200m, default 30m). Both synced from the server.</li>
            <li>Traps only run while someone is near them, and stop a couple of seconds after they leave. How near is set per trap by its Trigger Radius setting (synced, default 8m).</li>
            <li>Trap sounds: the Flame Trap roars while it burns, the Saw Wall's blade whirs while it cuts, and the Spike Floor's spikes clank as they snap up and back down. Each trap has its own sound volume setting (each player's own).</li>
            <li>New: Electric Fence and Fence Controller, built at the galdr table. The fence crackles with green lightning, hums and shocks enemies while a Fence Controller within 50m powers it. The controller holds only Eitr and burns one every 10 minutes while its fences are live; looking at it shows how long its power will last, and its range circle shows while placing it or a fence. Shift + Use turns it off and on.</li>
            <li>New config section: Electric Fence. Fence Controller Radius (5m to 100m, default 50m) and Minutes Per Eitr (1 to 120, default 10), both synced from the server, and Electric Fence Sound Volume (each player's own).</li>
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