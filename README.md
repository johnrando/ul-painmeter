# PainMeter

A 7 Days To Die mod. Shows a zombie's **pain meter** — how close it is to shrugging off your hits
and attacking straight through them.

Every zombie keeps a hidden number the game calls pain resistance. Each hit that makes it flinch
adds to it, and it drains at 0.2 a second. While it is below 1, every such hit staggers the zombie:
for half a second it cannot attack and barely moves. At 1 or more it ignores the stagger and
swings anyway — that is the free hit you take when you keep clubbing a zombie that has stopped
flinching. Two hits on almost any zombie get it there. PainMeter draws that number so you can
see it coming.

## Installing

Download the zip from Releases and extract it into the game's `Mods/`. The mod folder is the root
of the archive, so it lands as:

```
Mods/PainMeter/
├── ModInfo.xml
└── PainMeter.dll
```

Load order does not matter, and nothing needs building.

## Console commands

`pm` prints the menu and changes nothing — `painmeter` is an alias. Every line names the command
that changes it and says what it is for, so the menu is also the reference:

```
PainMeter is ON
  pm on|off               : [ >on< | off ]               - show how close a zombie is to attacking through hits
  pm bar                  : [ >on< | off ]               - strip along the bottom of Undead Legacy's target health bar
  pm target               : [ on | >off< ]               - bar over the head of the zombie under the crosshair
  pm all                  : [ on | >off< ]               - bars over every zombie in range
  pm range {m}            : 15 m reach for 'pm all'
  pm hidezero             : [ >on< | off ]               - hide a meter that reads zero
  pm flash                : [ >on< | off ]               - flash while the 0.5 s stagger lockout runs
  pm scale                : [ >threshold< | full | hits ] - what the bar spans: 0-1, 0-3 with a tick at 1, or hits
  pm timer                : [ >bar< | pips | off ]       - how long until the meter drops back below 1
  pm pips {n}             : 10 pips, one per second (with 'pm timer' on pips)
  pm size {w} {h}         : 60 x 6 px overhead bar
  pm offset {m}           : 0.25 m above the head
  pm opacity {0-1}        : 0.85
  pm colour {l} {h} {lock}: 60,200,60 at empty / 220,50,50 at full / 70,130,220 locked (r,g,b)
  pm animals              : [ on | >off< ]               - hostile animals too, not just zombies
  pm focus                : [ >on< | off ]               - WhackLash's focus meter: pips, locked tint, bonus
  pm focuspos {x} {y}     : 22 12 px: pips right of / above the bar's end
  pm pipcolour {l} {m} {h}: 255,215,0 at empty / 255,130,0 midway / 225,40,40 at the cap (focus pips by level)
  pm accents {t} {f} {m}  : 255,255,255 timer / 255,255,255 flash / 255,255,255 break frame, bonus and tick
```

`pm on` and `pm off` are the master switch — with it off nothing is drawn and every other setting
is inert.

The three displays are independent and can be combined:

- **`pm bar`** — a thin strip along the bottom edge of Undead Legacy's target health bar, inside
  the bar so UL's debuff icons underneath never cover it. It follows UL's bar exactly: same target,
  same visibility, gone when UL's own *Target HP* option is off. Needs Undead Legacy.
- **`pm target`** — a bar floating over the head of the zombie under your crosshair.
- **`pm all`** — a bar over every zombie within `pm range` metres. Includes the crosshair target,
  so `pm target` adds nothing while this is on.

A setter called with no arguments prints its usage and current value. **Changes are saved** — see
[Settings file](#settings-file).

Unlike most console commands, `pm` runs on the machine that typed it: every setting is about what
that machine draws, so on a dedicated server each player sets their own.

Two more: `pm info` prints the same block with the patch state and counters added, and `pm reset`
zeroes those counters.

## Reading the meter

- **The fill** is the pain number, empty at 0 and full at 1. It shifts from the *low* colour to the
  *high* one on the way (green to red by default).
- **A flash** means the half-second stagger lockout is running: the zombie cannot attack right now.
  `pm flash` turns it off.
- **Full** means the zombie attacks through your hits. The **timer** under the bar shows how long
  until it drains back below 1 — a thin bar that empties, or one pip per second (`pm timer`
  cycles bar, pips, off). The number tops out at 3, which is ten seconds of draining, so the timer
  is full at the cap.
- **No bar at all** means the meter reads zero. `pm hidezero` off draws it empty instead.

With [WhackLash](../ul-whacklash) installed there is more to read (`pm focus` turns it off):

- **The pips off the right end of the bar** (above it, on the overhead bars) are WhackLash's focus
  meter, one pip per point up to its cap (5 by default). Their colour follows the level: yellow
  when the meter has just started, orange midway, red near the cap (`pm pipcolour` changes the
  three stops). `pm focuspos {x} {y}` nudges them, in pixels right of and above the bar's
  end, if they do not sit where you want on your screen. It builds 1 per melee hit you land, less for arrows and bullets, and drains at
  the same 0.2 a second as the pain meter. The last lit pip drains visibly.
- **The framed pip** is WhackLash's break point (3 by default). When the meter reaches it,
  WhackLash pins the pain number just under 1: the zombie cannot attack through your hits until
  the focus meter drains back below the break.
- **A blue bar** (the *locked* colour) means exactly that has happened. The fill and the pips
  switch colour and the stagger flash stops, because the pain number is being held and no longer
  tells you anything on its own.
- **`+N%`** after the last pip is the damage bonus WhackLash gives your next hit.

`pm scale` changes what the fill spans:

- **threshold** (default): 0 to 1, as above. Full is where hits stop staggering.
- **full**: 0 to 3, the whole number, with a white tick where 1 falls. The overshoot shows as a
  level rather than only as the timer.
- **hits**: one segment per pain hit of that zombie's class, five for a 0.7 feral, four for a 0.9
  radiated. Each segment fills as the hit lands and drains as the number decays. Segments in the
  *low* colour are hits it will still stagger through; the first one in the *high* colour is the
  hit that takes it past 1. The per-hit amount is fixed by the zombie's class, so your weapon and
  whether it was a power attack make no difference: the count is known before you swing.

Per-hit values come from the game's entity classes: about 0.55 for an ordinary zombie, 0.7 feral,
0.9 radiated. So an ordinary zombie is still staggering after one hit and attacking through by the
second; a radiated one is at 1.8 after two, four seconds from staggering again.

## Defaults

All settable in-game, and all written back to the settings file as soon as you set them. These are
what a first run starts from:

| Setting | Default |
|---|---|
| health-bar row | on |
| overhead bar over the crosshair target | off |
| overhead bars over everything in range | off |
| range | 15 m |
| hide a meter that reads zero | on |
| stagger flash | on |
| meter scale | threshold |
| decay timer | bar |
| timer pips | 10 |
| overhead bar size | 60 x 6 px |
| head offset | 0.25 m |
| opacity | 0.85 |
| colours | 60,200,60 at empty / 220,50,50 at full / 70,130,220 locked |
| hostile animals | off |
| WhackLash focus pips | on |
| focus pip position | 22 12 px |
| pip colours | 255,215,0 / 255,130,0 / 225,40,40 by level |
| accents | timer, flash, break frame, bonus and tick all 255,255,255 |

Sizes are in UI pixels and scale with the game's UI scale option.

## Settings file

Every setting survives a restart. A change made with `pm` is written straight out to:

```
%APPDATA%/7DaysToDie/PainMeter/settings.txt
```

— the game's own user data folder, next to `Saves`, rather than `Mods/PainMeter/`, so updating
the mod does not take your settings with it. `pm info` prints the full path and whether the last
read or write worked.

It is plain `key = value` text, one line per setting, each naming the command that sets it:

```
enabled         = on           # pm on|off
bar             = on           # pm bar
overhead.target = off          # pm target
overhead.all    = off          # pm all
range           = 15           # pm range {m}
hidezero        = on           # pm hidezero
flash           = on           # pm flash
scale           = threshold    # pm scale - threshold, full or hits
timer           = bar          # pm timer - bar, pips or off
timer.pips      = 10           # pm pips {n}
size.width      = 60           # pm size {w} {h}
size.height     = 6            # pm size {w} {h}
offset          = 0.25         # pm offset {m}
opacity         = 0.85         # pm opacity {0-1}
colour.low      = 60,200,60    # pm colour {low} {high} {locked}
colour.high     = 220,50,50    # pm colour {low} {high} {locked}
animals         = off          # pm animals
focus           = on           # pm focus - WhackLash focus pips, locked tint and bonus label
focus.x         = 22           # pm focuspos {x} {y} - pips right of / above the bar's end
focus.y         = 12           # pm focuspos {x} {y}
colour.locked   = 70,130,220   # pm colour {low} {high} {locked}
pip.low         = 255,215,0    # pm pipcolour {low} {mid} {high} - focus pips by meter level
pip.mid         = 255,130,0    # pm pipcolour {low} {mid} {high}
pip.high        = 225,40,40    # pm pipcolour {low} {mid} {high}
colour.timer    = 255,255,255  # pm accents {timer} {flash} {mark}
colour.flash    = 255,255,255  # pm accents {timer} {flash} {mark}
colour.mark     = 255,255,255  # pm accents {timer} {flash} {mark} - break frame, +N% before break, tick at 1
```

Edit it by hand with the game closed — it is rewritten whenever a `pm` command changes something.
A line that will not parse is logged and ignored rather than fatal, and deleting the file brings
back the defaults above (which live in `Settings.cs`).

## Undead Legacy

**Required for the health-bar strip, optional for the overhead bars.** The strip is built inside
UL's own target health bar controller; without UL it has nothing to sit in, so `pm bar` is saved but
inert and the two overhead modes are what you get. Tested against **UL 2.7.32**.

UL re-implements the game's damage response but keeps the pain rules as they are, so the meter
means the same thing with or without it.

The overhead bars live in the game's on-screen icons layer — the one quest markers are drawn in —
so UL's *On-screen icons* option hides them too. `pm info` says so when it happens.

## WhackLash

**Optional.** [WhackLash](../ul-whacklash) builds a focus meter on every enemy you keep hitting and,
past a break point, holds the pain meter down so the zombie stops attacking through. With both
mods installed PainMeter draws that meter under the bar, as described in
[Reading the meter](#reading-the-meter). The link is read-only and found by reflection at startup:
either mod works with the other absent, and `pm info` reports whether it was wired up. If one of
the two is much newer than the other the readout may fail to bind; the log says so and the pain bar
is drawn on its own.

## Limitations

- **Read-only, client-side.** The mod reads the numbers the game already keeps on your machine
  and changes nothing. On a dedicated server the client mirrors those numbers from the server's
  damage packets, so they can lag a tick or two behind.
- **Sleeping zombies get no bar**, the same way UL's health bar leaves them alone.
- **No row on the vanilla target bar.** Without UL, use `pm target` or `pm all`.
- The overhead bar is drawn over the head bone; it is a UI element, not a particle effect, so it
  is not occluded by walls.

## Building

Requires the .NET SDK; there are no NuGet dependencies. The mod builds in place inside the game
install, against the game's own assemblies and the installed `Mods/UndeadLegacy/UndeadLegacy.dll`.

```
dotnet build src/PainMeter/PainMeter.csproj -c Release
```

That restages `dist/PainMeter/`, ready to copy into `Mods/`. To also build the release archive:

```
dotnet build src/PainMeter/PainMeter.csproj -c Release -t:Package
```

That writes `release/PainMeter-<version>-<date>.zip`, taking the version from `ModInfo.xml`.
Neither `dist/` nor `release/` is tracked — the zip is published as a GitHub Release instead.

## License

MIT — see `LICENSE`.
