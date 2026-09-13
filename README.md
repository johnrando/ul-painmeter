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
  pm on|off             : [ >on< | off ]               - show how close a zombie is to attacking through hits
  pm bar                : [ >on< | off ]               - row under Undead Legacy's target health bar
  pm target             : [ on | >off< ]               - bar over the head of the zombie under the crosshair
  pm all                : [ on | >off< ]               - bars over every zombie in range
  pm range {m}          : 15 m reach for 'pm all'
  pm hidezero           : [ >on< | off ]               - hide a meter that reads zero
  pm flash              : [ >on< | off ]               - flash while the 0.5 s stagger lockout runs
  pm timer              : [ >bar< | pips | off ]       - how long until the meter drops back below 1
  pm pips {n}           : 10 pips, one per second (with 'pm timer' on pips)
  pm size {w} {h}       : 60 x 6 px overhead bar
  pm offset {m}         : 0.25 m above the head
  pm opacity {0-1}      : 0.85
  pm colour {low} {high}: 60,200,60 at empty / 220,50,50 at full (r,g,b)
  pm animals            : [ on | >off< ]               - hostile animals too, not just zombies
```

`pm on` and `pm off` are the master switch — with it off nothing is drawn and every other setting
is inert.

The three displays are independent and can be combined:

- **`pm bar`** — a row under Undead Legacy's target health bar. It follows UL's bar exactly: same
  target, same visibility, gone when UL's own *Target HP* option is off. Needs Undead Legacy.
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
| decay timer | bar |
| timer pips | 10 |
| overhead bar size | 60 x 6 px |
| head offset | 0.25 m |
| opacity | 0.85 |
| colours | 60,200,60 at empty / 220,50,50 at full |
| hostile animals | off |

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
timer           = bar          # pm timer - bar, pips or off
timer.pips      = 10           # pm pips {n}
size.width      = 60           # pm size {w} {h}
size.height     = 6            # pm size {w} {h}
offset          = 0.25         # pm offset {m}
opacity         = 0.85         # pm opacity {0-1}
colour.low      = 60,200,60    # pm colour {low} {high}
colour.high     = 220,50,50    # pm colour {low} {high}
animals         = off          # pm animals
```

Edit it by hand with the game closed — it is rewritten whenever a `pm` command changes something.
A line that will not parse is logged and ignored rather than fatal, and deleting the file brings
back the defaults above (which live in `Settings.cs`).

## Undead Legacy

**Required for the health-bar row, optional for the overhead bars.** The row is built under UL's
own target health bar controller; without UL it has nothing to sit under, so `pm bar` is saved but
inert and the two overhead modes are what you get. Tested against **UL 2.7.32**.

UL re-implements the game's damage response but keeps the pain rules as they are, so the meter
means the same thing with or without it.

The overhead bars live in the game's on-screen icons layer — the one quest markers are drawn in —
so UL's *On-screen icons* option hides them too. `pm info` says so when it happens.

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
