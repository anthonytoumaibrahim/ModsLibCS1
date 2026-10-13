# ModsLibCS1

A shared UI library for Cities: Skylines 1 mods: one builder for options panels (labels, sliders,
switches, choice rows, tabs, text fields, key rows, dropdowns) and a choice of **skins** the player
picks on the mod's options page:

- **See-through**: a charcoal panel the sky shows through, thin sliders with a blue fill,
  switches, choice rows as a row of buttons, boxed values.
- **Game blue**: the same layout on a deep-blue opaque panel.
- **Vanilla**: the game's own panel look, unchanged.
- **Frosted**: after Cities: Skylines II's photo mode: blue-grey glass over a blurred copy of the
  screen, rounded-top tabs, capitals, tick boxes, a faint line between rows.

Which skin a mod starts with is that mod's choice; each mod stores the player's pick in its own
settings. It is built on the game's own `ColossalFramework.UI` components with its own sprite sheet, so it
costs what the game's UI costs. Frosted's blur adds a few small copies of the finished frame per
frame (`FrameGrab`, the main camera's last image effect), and only while a Frosted panel is open.

> **Status:** first version, not yet used by any mod or tried in-game.

## Using it in a mod

The library is not a DLL of its own: it is compiled into each mod's DLL. No extra file to ship,
nothing for players to subscribe to, and no mod ever runs another mod's copy.

1. Add it as a git submodule at the mod's repository root:

   ```
   git submodule add https://github.com/anthonytoumaibrahim/ModsLibCS1.git ModsLibCS1
   ```

   Anyone cloning the mod then needs `git clone --recurse-submodules` (or
   `git submodule update --init` after a plain clone).

2. Import the props in the mod's csproj, with a clear error if the submodule isn't checked out:

   ```xml
   <PropertyGroup>
     <ModsLibCS1Props>$(MSBuildThisFileDirectory)..\ModsLibCS1\ModsLibCS1.props</ModsLibCS1Props>
   </PropertyGroup>
   <Import Project="$(ModsLibCS1Props)" Condition="Exists('$(ModsLibCS1Props)')" />
   <Target Name="CheckModsLibCS1" BeforeTargets="BeforeBuild" Condition="!Exists('$(ModsLibCS1Props)')">
     <Error Text="ModsLibCS1 is missing: run git submodule update --init" />
   </Target>
   ```

   (The path assumes the csproj sits one folder below the repository root.)

   The props adds every `src\**\*.cs` file to the mod's compile and embeds the sprite sheet under
   the fixed names `ModsLibCS1.sheet.png` / `ModsLibCS1.sheet.txt`. The mod needs the usual game
   references (`Assembly-CSharp`, `ColossalManaged`, `ICities`, `UnityEngine`).

3. Its own icons (tab icons), if it draws its tabs as icons. The library's sheet holds only what
   every mod uses (the skins, the reset, fold, close and dropdown glyphs, the scroll bar); a mod's
   icons are its own. List them in `<project>\Resources\PanelIcons.lucide.txt`, one per line, the
   name and then each SVG path of the icon (Lucide's 24-unit grid; a circle or rect written as a
   path), separated by ` | `:

   ```
   sun | M8 12a4 4 0 1 0 8 0a4 4 0 1 0 -8 0Z | M12 2v2 | M12 20v2 | ...
   ```

   then draw them, which writes `PanelIcons.png` + `PanelIcons.txt` beside the list:

   ```
   powershell -File ModsLibCS1\tools\make-icons.ps1 -Spec <project>\Resources\PanelIcons.lucide.txt
   ```

   The props embed those two files when both are there (`-p:ModsLibCS1Icons=<path without
   extension>` points elsewhere), and the atlas merges them in: `AddTabStrip(..., icons)` asks for
   `"icon-" + name` as before. Without them the tabs show their names. Credit the icons in the mod's
   own notices.

A library change reaches a mod only when that mod moves its submodule pointer and is rebuilt.

## Building a panel

```csharp
using ModsLibCS1.UI;

LibLog.Sink = Log.Msg;                     // the mod's own log; once, at start
Builder ui = Builder.For(Skin.SeeThrough); // the player's pick; a new Builder per panel build

Frame frame = ui.AddFrame(panel, "My mod", () => panel.Hide());
TabStrip tabs = ui.AddTabStrip(panel, new Vector3(14f, 40f), panel.width - 28f, 28f, names, SelectPage);
SliderRow speed = ui.AddSlider(page, y, "Speed", 0f, 5f, 0.1f, value, v => v.ToString("F1") + "x", OnSpeed);
ToggleRow fog = ui.AddToggle(page, y += ui.RowHeight, "Fog", true, OnFog);
ChoiceRow style = ui.AddChoice(page, y += ui.RowHeight, "Style", new[] { "Classic", "Cumulus" }, 0, OnStyle);

fog.SetValue(false);        // refresh from the setting: no callback
speed.SetEnabled(false);    // grey the whole row
```

`Builder.For` falls back to Vanilla (and logs why) if the sprite sheet can't be loaded or lacks
a sprite the skin needs, so no skin can leave a blank panel. Every `Add*` row returns its parts.
Choice rows hand back the picked INDEX; what that stores stays the mod's business.

## Rules for library code

Every mod carries its own copy, and several copies run side by side in one game:

- Nothing global by name: no atlas, GameObject or type looked up by a shared name
  (no `Type.GetType` by string, no `FindObjectsOfTypeAll` for our own things by name). Each mod's
  atlas is named after that mod's assembly.
- Statics are per mod and stay that way; nothing is shared between mods' copies.
- No reference to any mod's own code: player text, logging and callbacks are passed in.
- `net35` / C# 7.3 (Unity 5.6, Mono's .NET 3.5 profile); no LINQ on hot paths.

## Layout

```
ModsLibCS1.props        imported by each mod's csproj
src/UI/Builder.cs       every control, panel frame, tabs, KeepOnScreen
src/UI/Theme.cs         one per skin: colours, sizes, the shape of toggles/choices/tabs/values
src/UI/SkinSprites.cs   the sprite names each skin draws with (pure)
src/UI/Atlas.cs         the library's UITextureAtlas, built once from the embedded sheet (+ the mod's icons)
src/UI/SheetTable.cs    reads sheet.txt (pure); ChoiceFit.cs: buttons or dropdown (pure)
src/UI/Rows.cs          what each Add* returns; LibLog.cs, Skin.cs
src/UI/Backdrop.cs      Frosted's blur: FrameGrab (the frame, shrunk) + Backdrop (drawn in the panel's outline)
sprites/                sheet.png + sheet.txt -- generated, don't edit
tools/make-sprites.ps1  draws every sprite (SpriteMaker.cs) and packs the sheet; deterministic
tools/make-icons.ps1    draws a mod's own icons from its list into a sheet of its own (SpriteMaker.RunIcons)
tools/test-sheet.ps1    offline checks: table vs PNG, skins vs sheet, no mod icons in it, icon lists, the fit rule
tools/check/            compiles the library on its own against the installed game
```

```
powershell -File tools\make-sprites.ps1                 # after changing a colour or shape
powershell -File tools\test-sheet.ps1
dotnet build tools\check\ModsLibCS1.Check.csproj        # warnings are errors
```

## License

MIT, see [LICENSE](LICENSE).
