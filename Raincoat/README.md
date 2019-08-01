
[//]: # ( Raincoat )

# Raincoat
This a collection of various small tweaks and adjustments.  Each feature can be independently disabled in configuration.  Features are noted below as to which require [C]lient and which require [H]ost installation of the mod.

##### Dropping Recent Items [C/H]
Did you pick up the Engi's fungus?  Grabbed a personal shield generator while on five shaped glass?  Press 'G' to drop it.  Works on items (not equipment) and only one picked up in the last 10 seconds, to avoid cheesy item juggling while using 3d printers or cauldrons.  Key is configurable.

##### Ping Improvements [H]
Pings on interactables now last for 5 minutes.  Some other ping improvements are under consideration, but that's all it does for now.  This feature inspired by the mod TwentyMinutePings by TeaBoneJones.

##### Allied Team Improvements [C]
Having allied monsters with malachite effects can be useful, but their malachite patches look just like what hostile monsters create, making it hard to know where you can walk safely.  This changes the allied malachite patches to a non-threatening blue color.

This also makes it so that if you have Gesture of the Drowned and Royal Capacitor, lightning will not fire while in the Bazaar.  No more walking backwards into the shop to avoid pissing off the shopkeeper.

##### Ally Card Improvements [C]
As Engi, you always want to know whether your turrets are still alive.  They show up in the ally list, but if you accumulate a few too many allies, they can be difficult to spot.  This changes the label for Engi Turrets so that they show up with easily noticeable bright red text.  Currently this only works when using English text.

##### Boss Shop Dropping [H]
When it comes to creating a character build, you're often at the mercy of RNG, particularly in single player.  This offers a bit more choice by replacing boss drops with free green item shops instead.  'Special' boss drop items are mixed into the shops at the usual drop rate, just in case you _really_ want a Beetle Gland.  There's also a small chance at later stages of the shops spawning as red item shops.  These changes are a pretty substantial buff to the player, so consider mixing with other mods to up the difficulty to compensate.  Or just have fun wrecking things.

##### Starter Pack [H]
Early game is kinda slow, both in terms of DPS and lacking speed items.  If you have this enabled, the host can press 'F1' to give everybody a starter pack of items to help get things rolling.  This is a pretty large advantage, but helps alleviate some of the early game boredom.  Key is configurable.

### Dependencies
R2API, MiniRpcLib

### Installation
Place JarlykMods.Raincoat.dll in your BepInEx\plugins folder.  If you're using a Mod Manager, it may place it under its own folder under plugins, which is also fine.

### Development
The Git repo includes mod dependencies, but does not include the Unity or RoR2 Assemblies.  Prior to building, you'll need to copy the necessary files from the Managed folder in your RoR2 installation to the local Dependencies folder.

### Contact
You can reach me via Github or can find me on the modding Discord at https://discord.gg/5MbXZvd.  As with most mods, this is a hobby project, so please understand that response times to questions and time to update for new RoR2 releases may vary.

### Changelog

0.1.1 | 2019-07-31
- Initial tracked version