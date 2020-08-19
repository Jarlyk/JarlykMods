[//]: # ( Elite Spawning Overhaul )

# Elite Spawning Overhaul
The current system for spawning elites in Risk of Rain 2 is fairly limiting for modders, as you can only specify hp/dmg boosts on a per-tier basis and have no way to adjust spawning probabilities.  This mod overhauls the elite spawning logic to provide finer-grained control.  This mod doesn't do much by itself, but may be used as a dependency by mods that work with elites.

### Elite Affix Cards
Elites are made eligible for spawning by creating an EliteAffixCard and adding it to the Eso.Cards list.  This list is pre-populated with cards for the vanilla elites.

### Installation
Place EliteSpawningOverhaul.dll in your BepInEx\plugins folder.  If you're using a Mod Manager, it may place it under its own folder under plugins, which is also fine.

### Contact
You can reach me via Github or can find me on the modding Discord at https://discord.gg/5MbXZvd.  As with most mods, this is a hobby project, so please understand that response times to questions and time to update for new RoR2 releases may vary.

### License
This mod is released under the standard MIT license, which is a permissive license that allows for free use with attribution, while disclaiming liability.  The text of this is included in the release archive in License.txt.

### Changelog

1.0.1 | 2020-08-18
- Fixed bug where often non-elites would spawn with elite hp/dmg

1.0.0 | 2020-08-15
- Updated for RoR2 v1.0 Release

0.3.1 | 2019-12-18
- Fixed build issues with December Content Update

0.3.0 | 2019-11-14
- Updated for new BepInEx/R2API

0.2.0 | 2019-09-20
- Updated for RoR2 Skills 2.0 Release

0.1.0 | 2019-08-16
- Initial tracked version