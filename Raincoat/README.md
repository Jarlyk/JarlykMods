
[//]: # ( Raincoat )

# Raincoat
This a collection of various small tweaks and adjustments.  Each feature can be independently disabled in configuration.  Features are noted below as to which require [C]lient and which require [H]ost installation of the mod.

#### Dropping Recent Items [C/H]
Did you pick up the Engi's fungus?  Grabbed a personal shield generator while on five shaped glass?  Press 'G' to drop it.  Works on items (not equipment) and only those picked up in the last 10 seconds, to avoid cheesy item juggling while using 3d printers or cauldrons.  Key is configurable.

#### Ping Improvements [C]
Pings on interactables now last for 5 minutes.  Some other ping improvements are under consideration, but that's all it does for now.

#### Allied Team Improvements [C]
Having allied monsters with malachite effects can be useful, but their malachite patches look just like what hostile monsters create, making it hard to know where you can walk safely.  This changes the allied malachite patches to a non-threatening blue color.

#### Ally Card Improvements [C]
As Engi, you always want to know whether your turrets are still alive.  They show up in the ally list, but if you accumulate a few too many allies, they can be difficult to spot.  This changes the label for Engi Turrets so that they show up with easily noticeable bright red text.

#### Boss Shop Dropping [H]
When it comes to creating a character build, you're often at the mercy of RNG, particularly in single player.  This offers a bit more choice by replacing boss drops with free green item shops instead.  'Special' boss drop items are mixed into the shops at the usual drop rate, just in case you _really_ want a Beetle Gland.  There's also a small chance at later stages of the shops spawning as red item shops.  These changes are a pretty substantial buff to the player, so consider mixing with other mods to up the difficulty to compensate.  Or just have fun wrecking things.

#### Starter Pack [H]
Early game is kinda slow, both in terms of DPS and lacking speed items.  If you have this enabled, the host can press 'F1' to give everybody a starter pack of items to help get things rolling.  This is a pretty large advantage, but helps alleviate some of the early game boredom.  Key is configurable.

### Dependencies
R2API, MiniRpcLib

### Installation
Place JarlykMods.Raincoat.dll into your BepInEx\plugins folder.

### Changelog

0.1.1 | 2019-07-31
Initial version