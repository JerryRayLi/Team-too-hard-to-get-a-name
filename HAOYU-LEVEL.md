# Level Haoyu

Open `Assets/Scenes/Level_Haoyu.unity` in Unity **6000.3.22f1**.

Controls: A/D move, Space jump, R restart. Reach the green exit. The level includes proximity spikes, disguised collapsing ground, hidden ceiling spikes that reveal on contact, a weighted floor, a chaser, and a final proximity spike trap.

## Build for the browser

Install the matching WebGL Build Support module. Select **Build > Haoyu WebGL** in the Editor, or run Unity in batch mode with `-buildTarget WebGL -executeMethod BuildHaoyuWeb.Build`. The script builds only this scene into `Builds/HaoyuWeb`; set the `HAOYU_WEB_OUTPUT` environment variable to change the output folder. It uses Gzip compression with decompression fallback for static hosting.

## Credits

Spike movement/trigger scripts and death/respawn particle objects are adapted from Tianyu Zhang's `Tianyu_Zhang` branch of [xlin3364-code/game_levelup, commit 33df47b](https://github.com/xlin3364-code/game_levelup/tree/33df47bb958a09cfc05ab284a60d362b0078f799), particularly `Assets/Scripts/Tianyu_script` and `Assets/Scenes/Level05.unity`. The scripts are namespaced `HaoyuTianyu` and add reset/contact guards for this level's manager. Both floor spike groups and the inverted ceiling group share the source spike appearance. Death and respawn particle modules retain the source settings.

The level layout, fixed camera, movement tuning, collapse approach rule, weighted state, chaser, ceiling reveal rule, and local completion/reset behavior are level-specific. Shared Square/Triangle sprites and sprite materials are Unity package assets.

## Verification

The final trap revision passed four isolated Unity Play Mode checks using simulated Input System keyboard input: four-jump completion, death when the final jump is missed followed by reset, R resetting the final trap, and completion with a short reaction pause. The run reported no runtime errors. This is automated validation, not teammate playtesting or a claim about human difficulty.

# Level Haoyu 2 (SLOW 2)

Open `Assets/Scenes/Level_Haoyu/Level_Haoyu_2.unity`. It is listed as SLOW > LEVEL 2 in the main menu and follows `Level_Haoyu` in Build Settings.

SLOW 2 keeps SLOW 1's empty floor and hidden traps, and each trap answers the habit the previous one taught:

1. Popup spikes as in SLOW 1, but they fire about 1.8 units ahead so the player can stop and watch, and after 0.5 s they shake for 0.2 s and retract over 0.3 s (once only).
2. A cracked floor sits where a jump over the spikes lands and breaks on impact. Walking onto it is safe, so the way through is to bait the spikes, wait for them to retract and walk.
3. A slab with visible ceiling spikes makes players afraid to jump, but a normal jump clears the tips by 0.14 units; walking under it raises floor spikes, so this stretch must be jumped.
4. The strip at the pit edge starts a 0.3 s timer the moment the player lands on it (the floor shakes), then spikes rise, so hopping in place to probe the gap is fatal.
5. The pit is too wide for a normal jump. Inside it the player falls slowly (faint falling dust is the hint), so a jump with forward held the whole time glides across; walking off or letting go early sinks into the pit.
6. The same retracting popup spikes again before the exit, for players who rush after the glide.

The camera is zoomed out (`FinalFixedCamera.orthographicSize` 11; SLOW 1 keeps 9) so the traps have room between them. In both SLOW levels the camera now fills the screen instead of drawing black bars: it always shows at least the 16:9 view and zooms out on narrower screens.

New scripts: `FinalLandingCrack`, `FinalSlowFallZone`, `FinalTimedSpikeTrigger`, `FinalDustMotes`. `FinalLevelPlayerController.MarkSlowFall` softens only the descent, so jumps keep their height. `FinalLevelManager` resets every mechanic of each type in the scene, and both SLOW levels now draw their header with the shared `GameHUD.DrawHeader`. `HaoyuTianyu.SpikeTrap` gains optional `retractWarning`, `retractShake` and `retractDuration` for automatic retracts; their defaults keep SLOW 1 unchanged.

## Verification

Automated Unity Play Mode runs with simulated Input System keyboard input passed: walking into the first spikes dies; jumping over them breaks the crack; baiting, waiting and walking survives; walking under the spike slab dies and a jump passes 0.14 units under the tips; landing at the pit edge and hopping in place dies on the edge spikes; walking off the pit and a short hop both fall, while a held jump straight off the landing glides from x 2.7 to x 11.3 in 2.2 s (the far edge is x 10.8); rushing to the exit hits the exit spikes; the intended route reaches the exit with no deaths in 13 s and loads the next scene; no runtime errors. A Level Haoyu smoke run (death, reset) also passed. This is automated validation, not teammate playtesting.
