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
