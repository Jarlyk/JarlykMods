
[//]: # ( Hailstorm )

# Hailstorm
Will you stare into the Darkness?  Will you conquer it?  This mod adds new challenges, including some new elites.  If you run into issues or have constructive suggestions for how to improve the mod, feel free to let me know.

## Features

### Dark Elites
These elites spawn somewhat rarely, but they bring the darkness with them.  When you look in their direction, the darkness closes in.  Look away and it will begin to recede, but eventually you'll need to defeat them.  Find them in the darkness by following their purple spectral light, but try not to get killed by the many other threats unseen.

### Barrier Elites
These elites provide Barrier to their nearby allies while they are alive, so they're a priority target.

### Storm Elites
These elites show up relatively late (around the time that Malachites start spawning) and they're just about as tough.  They bring storms in their wake that dislocate friend and foe alike.

### Mimics
Not all chests are what they appear to be.  Occasionally a chest will reveal itself to be a monster in disguise; beware, for it wields the item that was within the chest and will not yield it until it is dead.  As of 1.3.0, a new type of mimic has arrived which likes to pose as small chests; this is still in early beta, so reports of issues or constructive suggestions for balance are welcome.

## Technical Notes

### Installation
Place JarlykMods.Hailstorm.dll in your BepInEx\plugins folder.  If you're using a Mod Manager, it may place it under its own folder under plugins, which is also fine.  Please note that this mod has several dependencies (as visible in the Thunderstore) that will need to be present in order for it to function.

### Configuration
After you run the mod once, a configuration file will be created under BepInEx\config.  It is a simple text file with configuration options that are commented as to what they do.  Here you can disable particular features and configure the spawn rate for mimics.

### Credits
Special thanks for the mimic model and animations, provided by rob
Monster log for mimic written by tera

### Contact
You can reach me via Github or can find me on the modding Discord at https://discord.gg/5MbXZvd.  As with most mods, this is a hobby project, so please understand that response times to questions and time to update for new RoR2 releases may vary.

### License
This mod is released under the standard MIT license, which is a permissive license that allows for free use, while disclaiming liability.  The text of this is included in the release archive in License.txt.

### Changelog

1.3.1 | 2020-09-08
- Fixed missing R2API submodule dependency

1.3.0 | 2020-09-05
- Added new monster for small chest mimics
- Fixed issue where the purple texture on Dark elites wasn't showing up properly in the darkness
- Fixed issues with darkness shader impacting emissive textures oddly
- Barrier elites now apply substantially more barrier to their allies, but also have slightly less total hp than before

1.2.2 | 2020-08-23
- Darkness background breathing sound now attenuates significantly with distance from the nearest Dark elite

1.2.1 | 2020-08-19
- Fixed missing LanguageAPI attribute

1.2.0 | 2020-08-18
- Toned down barrier visual effect significantly, as it was causing significant hits to frame rates on some systems
- Improved Darkness shader to have smoother state transitions
- Darkness is now significantly reduced in intensity when the Dark elite is far away from you
- Fixed issue where mimics would sometimes not spawn
- Fixed issue where fallback from failed mimic spawning wasn't working, causing no item to be dropped
- Mimics now spawn at the location of the chest

1.1.0 | 2020-08-17
- Fixed boss descriptions when affixed as Hailstorm elites
- Dark elites no longer spawn on the moon
- Added new visual effect for barrier elites

1.0.0 | 2020-08-15
- Updated for RoR2 v1.0 Release
- Reduced duration of tornadoes from storm elites
- When storm elites are killed, their tornadoes despawn
- Split out Cataclysm into a separate mod project (since the incomplete content was bloating the size of this mod)

0.4.1 | 2019-12-18
- Fixed build issues with December Content Update

0.4.0 | 2019-11-14
- Updated for new BepInEx/R2API

0.3.2 | 2019-09-25
- Made tornadoes less deafening

0.3.1 | 2019-09-21
- Added missing dependency reference

0.3.0 | 2019-09-20
- Updated for RoR2 Skills 2.0 Release
- Prep work for future fun stuff (sorry about the file size increase...)

0.2.0 | 2019-08-27
- Added Storm elites and accompanying tornado projectiles
- Increased spawn rate of dark elites
- Disabled some debugging keys to avoid interfering with other mods

0.1.0 | 2019-08-16
- Initial tracked version