[//]: # ( Equipment Durability )

# Equipment Durability
There's lots of interesting equipment to be found, but most of it gets ignored once the 'optimal' equipment is found.  This mod aims to rectify that by giving equipment a limited durability, incentivizing swapping out your equipment from time to time.  Using your equipment repeatedly in quick succession wears it out even faster, so beware those who are tempted by the Gesture of the Drowned.

### Mechanics
All equipments have a limited durability.  When you use an equipment, it loses a small amount of durability.  When the durability is exhausted, the equipment breaks and you'll need to find something new.  Fuel cells make the equipment lose durability less quickly, but Gesture of the Drowned does not.

### Configuration
You can configure the durability of equipment in terms of the expected lifetime, in seconds.  Durability loss will be scaled automatically so that if you were to use the item immediately when it comes off cooldown, equipment would last this long.  Lunar equipment has a separate lifetime (by default, twice that of regular equipment)

### Installation
Place JarlykMods.Durability.dll in your BepInEx\plugins folder.  If you're using a Mod Manager, it may place it under its own folder under plugins, which is also fine.

### Contact
You can reach me via Github or can find me on the modding Discord at https://discord.gg/5MbXZvd.  As with most mods, this is a hobby project, so please understand that response times to questions and time to update for new RoR2 releases may vary.

### License
This mod is released under the standard MIT license, which is a permissive license that allows for free use, while disclaiming liability.  The text of this is included in the release archive in License.txt.

### Changelog

1.0.0 | 2020-08-17
- Updated for RoR2 v1.0 Release
- Increased nominal durability to 20/40 minutes

0.1.1 | 2019-10-01
- Fixed issue where durability wouldn't persist between stages
- Fixed issue where using an item with no effect (like Aspect or Royal Cap with no target) would still deduct durability
- Increased default nominal equipment lifetime to 12/24 minutes

0.1.0 | 2019-09-29
- Initial tracked version