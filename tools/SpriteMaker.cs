// Draws every sprite of the See-through and Game blue skins and packs them into one sheet.
// Compiled and run by make-sprites.ps1 (Windows PowerShell 5.1's compiler: C# 5, no newer syntax).
//
// The colours are the mockup's (.vw[data-style="glass"] over the base .vw block, and
// .vw[data-style="blue"]). Vanilla has no sprites here: it uses the game's own atlas.
//
// Deterministic: the same script gives the same bytes, so a rebuild with no change shows no diff.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;

public static class SpriteMaker
{
    private class Sprite
    {
        public string Name;
        public Bitmap Bitmap;
        public int Left, Right, Top, Bottom;   // the 9-slice border, in pixels
        public int X, Y;                        // where it landed in the sheet
    }

    private class Palette
    {
        public string Prefix;
        public Color PanelTop, PanelBottom, PanelBorder;
        public int PanelRadius;
        public Color Title, TitleLine, CloseHover, CloseGlyph;
        public Color Button, ButtonHover, ButtonPressed, ButtonBorder;
        public Color Accent, AccentHover;
        public Color Field, FieldBorder;
        public Color Drop, DropHover, DropBorder, DropArrow, List, ListHover, ListHighlight;
        public Color Track, Fill, FillEnd, Thumb, ThumbBorder;
        public int TrackHeight, ThumbSize, ThumbBorderWidth;
        public bool TrackInset, ThumbShadow;
        public Color Value;
        public Color SegStrip, SegHover, SegSelected;
        public Color SwitchOff, Knob;
        public Color Box, BoxBorder, Tick;
        public Color TabStrip, TabHover, TabSelected;
        public bool TabPills;
        public Color Rule;
        public Color Hud, HudBorder, HudHover, HudHoverBorder;
    }

    private const int Gap = 1;        // clear pixels between two sprites' extruded edges
    private const int Extrude = 1;    // each sprite's edge pixels copied outwards (no bilinear bleed)

    public static string Run(string pngPath, string txtPath)
    {
        List<Sprite> sprites = new List<Sprite>();
        AddSkin(sprites, SeeThrough());
        AddSkin(sprites, GameBlue());
        AddFrosted(sprites);

        // Plain white, tinted by whoever draws it: a text field's selection and cursor (the
        // game's fields use its 'EmptySprite', which lives in the game's atlas, not ours).
        sprites.Add(Box("white", 4, 4, 0, Color.White, Color.White, Color.Transparent, 0, 0));

        // Every icon is Lucide's (lucide.dev, ISC licence: THIRD-PARTY-NOTICES.md), drawn here from
        // its own SVG paths (Lucide's 24-unit grid, 2-unit round strokes).
        //
        // A row's reset button, every skin: rotate-ccw.
        sprites.Add(Icon("reset", 16, Color.FromArgb(235, 255, 255, 255), Lucide.RotateCcw));

        // A folding heading's arrow, every skin: down while its rows show, right while they are
        // folded away. White, tinted to the heading's colour by whoever draws it.
        sprites.Add(Icon("fold-open", 10, 6, ChevronDownCrop, Color.White, Lucide.ChevronDown));
        sprites.Add(Icon("fold-shut", 6, 10, ChevronRightCrop, Color.White, Lucide.ChevronRight));

        // Tab icons (the new skins draw a tab as its icon, its name the tooltip), 24 px with a heavier
        // stroke than Lucide's 2 units (the author: bigger and thicker): "icon-" + Lucide's
        // name, white, tinted to the tab's text colour. A mod names the ones it wants.
        foreach (KeyValuePair<string, string[]> icon in Lucide.TabIcons)
            sprites.Add(Icon("icon-" + icon.Key, 24, 24, FullGrid, Color.White, icon.Value, TabIconStroke));

        int width, height;
        Pack(sprites, out width, out height);

        using (Bitmap sheet = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        {
            foreach (Sprite s in sprites)
                Blit(sheet, s);

            sheet.Save(pngPath, ImageFormat.Png);
        }

        StringBuilder text = new StringBuilder();
        text.Append("# ModsLibCS1 sprite sheet, made by tools/make-sprites.ps1. Do not edit by hand.\n");
        text.Append("# name x y width height left right top bottom  (pixels; y from the TOP of the sheet)\n");
        text.Append("sheet " + width + " " + height + "\n");
        foreach (Sprite s in sprites)
        {
            text.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5} {6} {7} {8}\n",
                s.Name, s.X, s.Y, s.Bitmap.Width, s.Bitmap.Height, s.Left, s.Right, s.Top, s.Bottom));
        }
        File.WriteAllText(txtPath, text.ToString(), new UTF8Encoding(false));

        foreach (Sprite s in sprites)
            s.Bitmap.Dispose();

        return sprites.Count + " sprites, sheet " + width + "x" + height;
    }

    // ------------------------------------------------------------------ the two palettes

    private static Palette SeeThrough()
    {
        Palette p = new Palette();
        p.Prefix = "st-";
        p.PanelTop = p.PanelBottom = Rgba(18, 21, 26, .74);
        p.PanelBorder = White(.10);
        p.PanelRadius = 10;
        p.Title = Rgba(0, 0, 0, .28);
        p.TitleLine = White(.08);
        p.CloseHover = Rgba(200, 70, 78, .9);
        p.CloseGlyph = Hex("ffffff");
        p.Button = White(.10);
        p.ButtonHover = White(.17);
        p.ButtonPressed = White(.06);
        p.ButtonBorder = Color.Transparent;
        p.Accent = Hex("4c9bff");
        p.AccentHover = Hex("6aadff");
        p.Field = Rgba(0, 0, 0, .35);
        p.FieldBorder = White(.14);
        p.Drop = White(.10);
        p.DropHover = White(.17);
        p.DropBorder = White(.14);
        p.DropArrow = Hex("e8ebef");
        p.List = Rgba(24, 28, 34, .96);
        p.ListHover = White(.10);
        p.ListHighlight = White(.16);
        p.Track = White(.22);
        p.Fill = p.FillEnd = Hex("4c9bff");
        p.TrackHeight = 4;
        p.Thumb = Hex("ffffff");
        p.ThumbBorder = Color.Transparent;
        p.ThumbSize = 14;
        p.ThumbBorderWidth = 0;
        p.ThumbShadow = true;
        p.Value = White(.10);
        p.SegStrip = White(.10);
        p.SegHover = White(.12);
        p.SegSelected = Hex("4c9bff");
        p.SwitchOff = White(.25);
        p.Knob = Hex("ffffff");
        p.Box = Hex("17191d");
        p.BoxBorder = Hex("5a6470");
        p.Tick = Hex("ffffff");
        p.TabStrip = Color.Transparent;
        p.TabHover = Color.Transparent;
        p.TabSelected = Hex("4c9bff");   // the line under the open tab
        p.TabPills = false;
        p.Rule = White(.12);
        p.Hud = Rgba(18, 21, 26, .74);
        p.HudBorder = White(.12);
        p.HudHover = Rgba(40, 46, 54, .9);
        p.HudHoverBorder = White(.3);
        return p;
    }

    private static Palette GameBlue()
    {
        Palette p = new Palette();
        p.Prefix = "gb-";
        p.PanelTop = Hex("213040");
        p.PanelBottom = Hex("1a2632");
        p.PanelBorder = Hex("33485c");
        p.PanelRadius = 6;
        p.Title = Hex("162130");
        p.TitleLine = Hex("2a3d50");
        p.CloseHover = Hex("2f5574");
        p.CloseGlyph = Hex("ffffff");
        p.Button = Hex("2c4a66");
        p.ButtonHover = Hex("36597a");
        p.ButtonPressed = Hex("233c54");
        p.ButtonBorder = Hex("3b5d7d");
        p.Accent = Hex("49b0f2");
        p.AccentHover = Hex("6cc0f5");
        p.Field = Hex("0f1820");
        p.FieldBorder = Hex("33485c");
        p.Drop = Hex("22384c");
        p.DropHover = Hex("2c4a66");
        p.DropBorder = Hex("3b5d7d");
        p.DropArrow = Hex("ffffff");
        p.List = Hex("1c2e3f");
        p.ListHover = Hex("2c4a66");
        p.ListHighlight = Hex("2f5574");
        p.Track = Hex("0f1820");
        p.TrackInset = true;
        p.Fill = Hex("2b86c9");
        p.FillEnd = Hex("58c0ff");
        p.TrackHeight = 6;
        p.Thumb = Hex("eaf5ff");
        p.ThumbBorder = Hex("49b0f2");
        p.ThumbSize = 16;
        p.ThumbBorderWidth = 2;
        p.ThumbShadow = true;
        p.Value = Color.Transparent;   // Game blue's values are plain text: no box sprite
        p.SegStrip = Hex("13202b");
        p.SegHover = Hex("22394f");
        p.SegSelected = Hex("2f6f9e");
        p.SwitchOff = Color.Transparent;   // Game blue uses the tick box, not the switch
        p.Knob = Hex("ffffff");
        p.Box = Hex("0f1820");
        p.BoxBorder = Hex("5a7a96");
        p.Tick = Hex("ffffff");
        p.TabStrip = Hex("142030");
        p.TabHover = Hex("22394f");
        p.TabSelected = Hex("2f5574");
        p.TabPills = true;
        p.Rule = Hex("33485c");
        p.Hud = Hex("1e2a36");
        p.HudBorder = Hex("33485c");
        p.HudHover = Hex("2f5574");
        p.HudHoverBorder = Hex("49b0f2");
        return p;
    }

    // ------------------------------------------------------------------ one skin's sprites

    private static void AddSkin(List<Sprite> list, Palette p)
    {
        string x = p.Prefix;
        int r = p.PanelRadius;

        // The panel: rounded, 1 px border. Its title strip sits inside that border, so only its
        // top corners are rounded, one pixel less.
        list.Add(Box(x + "panel", 32, 32, r, p.PanelTop, p.PanelBottom, p.PanelBorder, 1, r + 2));
        list.Add(TitleStrip(x + "title", 32, 32, r - 1, p.Title, p.TitleLine));
        list.Add(Box(x + "close-hover", 16, 16, 4, p.CloseHover, p.CloseHover, Color.Transparent, 0, 6));
        list.Add(Icon(x + "close", 12, 12, XCrop, p.CloseGlyph, Lucide.X));

        // Buttons, plus the accent pair (Save while there are unsaved changes, a picked segment).
        list.Add(Box(x + "button", 16, 16, 4, p.Button, p.Button, p.ButtonBorder, 1, 6));
        list.Add(Box(x + "button-hover", 16, 16, 4, p.ButtonHover, p.ButtonHover, p.ButtonBorder, 1, 6));
        list.Add(Box(x + "button-pressed", 16, 16, 4, p.ButtonPressed, p.ButtonPressed, p.ButtonBorder, 1, 6));
        list.Add(Box(x + "accent", 16, 16, 4, p.Accent, p.Accent, p.Accent, 1, 6));
        list.Add(Box(x + "accent-hover", 16, 16, 4, p.AccentHover, p.AccentHover, p.AccentHover, 1, 6));

        // Text field, dropdown and its list.
        list.Add(Box(x + "field", 16, 16, 4, p.Field, p.Field, p.FieldBorder, 1, 6));
        list.Add(Box(x + "field-focus", 16, 16, 4, p.Field, p.Field, p.Accent, 1, 6));
        list.Add(Box(x + "drop", 16, 16, 4, p.Drop, p.Drop, p.DropBorder, 1, 6));
        list.Add(Box(x + "drop-hover", 16, 16, 4, p.DropHover, p.DropHover, p.DropBorder, 1, 6));
        list.Add(Box(x + "drop-focus", 16, 16, 4, p.Drop, p.Drop, p.Accent, 1, 6));
        list.Add(Icon(x + "arrow", 10, 6, ChevronDownCrop, p.DropArrow, Lucide.ChevronDown));
        list.Add(Box(x + "list", 16, 16, 4, p.List, p.List, p.DropBorder, 1, 6));
        list.Add(Box(x + "list-hover", 12, 12, 3, p.ListHover, p.ListHover, Color.Transparent, 0, 4));
        list.Add(Box(x + "list-highlight", 12, 12, 3, p.ListHighlight, p.ListHighlight, Color.Transparent, 0, 4));

        // Slider: thin pill track, the fill over it, the knob.
        list.Add(Track(x + "track", p.TrackHeight, p.Track, p.Track, p.TrackInset));
        list.Add(Track(x + "fill", p.TrackHeight, p.Fill, p.FillEnd, false));
        list.Add(Thumb(x + "thumb", p.ThumbSize, p.Thumb, p.ThumbBorder, p.ThumbBorderWidth, p.ThumbShadow));

        // Choice rows: the strip, a hovered and a picked segment.
        list.Add(Box(x + "seg-strip", 16, 16, 5, p.SegStrip, p.SegStrip, Color.Transparent, 0, 6));
        list.Add(Box(x + "seg-hover", 16, 16, 4, p.SegHover, p.SegHover, Color.Transparent, 0, 6));
        list.Add(Box(x + "seg-selected", 16, 16, 4, p.SegSelected, p.SegSelected, Color.Transparent, 0, 6));

        // On/off: See-through's switch, Game blue's tick box (each skin draws only its own).
        if (p.SwitchOff.A > 0)
        {
            list.Add(Switch(x + "switch-off", p.SwitchOff, p.Knob, false));
            list.Add(Switch(x + "switch-on", p.Accent, p.Knob, true));
        }
        else
        {
            list.Add(TickBox(x + "tick-off", p.Box, p.BoxBorder, Color.Empty));
            list.Add(TickBox(x + "tick-on", p.Accent, p.Accent, p.Tick));
        }

        // Values in a small box (See-through only).
        if (p.Value.A > 0)
            list.Add(Box(x + "value", 16, 16, 4, p.Value, p.Value, Color.Transparent, 0, 6));

        // Tabs: See-through underlines the open one; Game blue has pills on a dark strip.
        if (p.TabPills)
        {
            list.Add(Box(x + "tab-strip", 16, 16, 6, p.TabStrip, p.TabStrip, Color.Transparent, 0, 7));
            list.Add(Box(x + "tab-hover", 16, 16, 4, p.TabHover, p.TabHover, Color.Transparent, 0, 6));
            list.Add(Box(x + "tab-selected", 16, 16, 4, p.TabSelected, p.TabSelected, Color.Transparent, 0, 6));
        }
        else
        {
            list.Add(Underline(x + "tab-selected", p.TabSelected));
        }

        // A 1 px line: under headings, under the tab names.
        list.Add(Line(x + "rule", p.Rule));

        // The mod's floating button.
        list.Add(Box(x + "hud", 16, 16, 6, p.Hud, p.Hud, p.HudBorder, 1, 7));
        list.Add(Box(x + "hud-hover", 16, 16, 6, p.HudHover, p.HudHover, p.HudHoverBorder, 1, 7));
    }

    // ------------------------------------------------------------------ Frosted

    /// <summary>
    /// Frosted, after Cities: Skylines II's photo mode (the mockup's .vw[data-style="photo"]):
    /// blue-grey glass over the blurred frame, a dark title bar, rounded-top tabs (the open one
    /// blue), a darker band under them, tick boxes, a faint line between rows. Its panel sprite is
    /// a TINT drawn over the blur, not the panel's own background.
    /// </summary>
    private static void AddFrosted(List<Sprite> list)
    {
        const string x = "fr-";
        Color blueTop = Hex("55bbf3"), blueBottom = Hex("2a7dd8"), accent = Hex("3fa9f0");

        // Denser than at first (the author: "hard to read the text" against a bright sky), and a
        // neutral dark grey (his call: no blue hue over the blur).
        list.Add(Box(x + "panel", 32, 32, 14, Rgba(30, 32, 36, .72), Rgba(20, 22, 26, .78), White(.08), 1, 15));
        list.Add(TitleStrip(x + "title", 32, 32, 13, Rgba(14, 20, 28, .94), Color.Transparent));
        list.Add(Box(x + "close-hover", 16, 16, 6, White(.12), White(.12), Color.Transparent, 0, 7));
        list.Add(Icon(x + "close", 14, 14, XCrop, Hex("ffffff"), Lucide.X));

        list.Add(Box(x + "button", 16, 16, 6, White(.15), White(.15), Color.Transparent, 0, 7));
        list.Add(Box(x + "button-hover", 16, 16, 6, White(.24), White(.24), Color.Transparent, 0, 7));
        list.Add(Box(x + "button-pressed", 16, 16, 6, White(.08), White(.08), Color.Transparent, 0, 7));
        list.Add(Box(x + "accent", 16, 16, 6, accent, accent, accent, 1, 7));
        list.Add(Box(x + "accent-hover", 16, 16, 6, Hex("63baf4"), Hex("63baf4"), Hex("63baf4"), 1, 7));

        list.Add(Box(x + "field", 16, 16, 6, Rgba(10, 16, 24, .55), Rgba(10, 16, 24, .55), White(.22), 1, 7));
        list.Add(Box(x + "field-focus", 16, 16, 6, Rgba(10, 16, 24, .55), Rgba(10, 16, 24, .55), accent, 1, 7));
        list.Add(Box(x + "drop", 16, 16, 6, White(.15), White(.15), White(.16), 1, 7));
        list.Add(Box(x + "drop-hover", 16, 16, 6, White(.24), White(.24), White(.16), 1, 7));
        list.Add(Box(x + "drop-focus", 16, 16, 6, White(.15), White(.15), accent, 1, 7));
        list.Add(Icon(x + "arrow", 10, 6, ChevronDownCrop, Hex("ffffff"), Lucide.ChevronDown));
        list.Add(Box(x + "list", 16, 16, 6, Rgba(32, 46, 60, .97), Rgba(32, 46, 60, .97), White(.16), 1, 7));
        list.Add(Box(x + "list-hover", 12, 12, 4, White(.12), White(.12), Color.Transparent, 0, 5));
        list.Add(Box(x + "list-highlight", 12, 12, 4, White(.18), White(.18), Color.Transparent, 0, 5));

        list.Add(Track(x + "track", 4, White(.26), White(.26), false));
        list.Add(Track(x + "fill", 4, blueBottom, blueTop, false));
        list.Add(Thumb(x + "thumb", 14, Hex("ffffff"), Color.Transparent, 0, true));

        list.Add(Box(x + "seg-strip", 20, 20, 7, White(.12), White(.12), Color.Transparent, 0, 8));
        list.Add(Box(x + "seg-hover", 16, 16, 5, White(.20), White(.20), Color.Transparent, 0, 6));
        list.Add(Box(x + "seg-selected", 16, 16, 5, blueTop, blueBottom, Color.Transparent, 0, 6));

        list.Add(TickBox(x + "tick-off", 18, 4, Rgba(14, 22, 30, .78), White(.55), 1.5f, Color.Empty));
        list.Add(TickBox(x + "tick-on", 18, 4, Rgba(14, 22, 30, .78), Hex("ffffff"), 1.5f, Hex("ffffff")));

        list.Add(TopTab(x + "tab", White(.17), White(.17)));
        list.Add(TopTab(x + "tab-hover", White(.26), White(.26)));
        list.Add(TopTab(x + "tab-selected", blueTop, blueBottom));
        list.Add(Box(x + "band", 4, 4, 0, Rgba(14, 22, 30, .24), Rgba(14, 22, 30, .24), Color.Transparent, 0, 0));

        list.Add(Line(x + "rule", White(.16)));
        list.Add(Line(x + "row-line", White(.10)));

        list.Add(Box(x + "hud", 20, 20, 8, Rgba(48, 70, 92, .62), Rgba(48, 70, 92, .62), White(.10), 1, 9));
        list.Add(Box(x + "hud-hover", 20, 20, 8, blueTop, blueBottom, Color.Transparent, 0, 9));
    }

    // ------------------------------------------------------------------ Lucide icons

    /// <summary>
    /// The Lucide icons the library draws (lucide-static 0.460.0, ISC licence, copied from its SVG
    /// files: THIRD-PARTY-NOTICES.md). Each is its elements' path data on Lucide's 24-unit grid; a
    /// circle is written as two arcs, a polyline as M/L.
    /// </summary>
    private static class Lucide
    {
        public static readonly string[] RotateCcw = { "M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8", "M3 3v5h5" };
        public static readonly string[] ChevronDown = { "m6 9 6 6 6-6" };
        public static readonly string[] ChevronRight = { "m9 18 6-6-6-6" };
        public static readonly string[] X = { "M18 6 6 18", "m6 6 12 12" };

        /// <summary>The tab icons, by Lucide's name (the sprite is "icon-" + it).</summary>
        public static readonly KeyValuePair<string, string[]>[] TabIcons =
        {
            new KeyValuePair<string, string[]>("clock", new[]
            {
                Circle(12, 12, 10), "M12 6L12 12L16 14",
            }),
            new KeyValuePair<string, string[]>("cloud", new[]
            {
                "M17.5 19H9a7 7 0 1 1 6.71-9h1.79a4.5 4.5 0 1 1 0 9Z",
            }),
            new KeyValuePair<string, string[]>("cloudy", new[]
            {
                "M17.5 21H9a7 7 0 1 1 6.71-9h1.79a4.5 4.5 0 1 1 0 9Z",
                "M22 10a3 3 0 0 0-3-3h-2.207a5.502 5.502 0 0 0-10.702.5",
            }),
            new KeyValuePair<string, string[]>("cloud-fog", new[]
            {
                "M4 14.899A7 7 0 1 1 15.71 8h1.79a4.5 4.5 0 0 1 2.5 8.242", "M16 17H7", "M17 21H9",
            }),
            new KeyValuePair<string, string[]>("cloud-rain", new[]
            {
                "M4 14.899A7 7 0 1 1 15.71 8h1.79a4.5 4.5 0 0 1 2.5 8.242", "M16 14v6", "M8 14v6", "M12 16v6",
            }),
            new KeyValuePair<string, string[]>("cloud-sun-rain", new[]
            {
                "M12 2v2", "m4.93 4.93 1.41 1.41", "M20 12h2", "m19.07 4.93-1.41 1.41",
                "M15.947 12.65a4 4 0 0 0-5.925-4.128", "M3 20a5 5 0 1 1 8.9-4H13a3 3 0 0 1 2 5.24",
                "M11 20v2", "M7 19v2",
            }),
            new KeyValuePair<string, string[]>("sun", new[]
            {
                Circle(12, 12, 4), "M12 2v2", "M12 20v2", "m4.93 4.93 1.41 1.41", "m17.66 17.66 1.41 1.41",
                "M2 12h2", "M20 12h2", "m6.34 17.66-1.41 1.41", "m19.07 4.93-1.41 1.41",
            }),
            new KeyValuePair<string, string[]>("lightbulb", new[]
            {
                "M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5",
                "M9 18h6", "M10 22h4",
            }),
        };

        private static string Circle(float cx, float cy, float r)
        {
            return string.Format(CultureInfo.InvariantCulture, "M{0} {1}a{2} {2} 0 1 0 {3} 0a{2} {2} 0 1 0 {4} 0Z",
                cx - r, cy, r, 2f * r, -2f * r);
        }
    }

    // The part of Lucide's grid a cropped glyph shows (its strokes included).
    private static readonly RectangleF ChevronDownCrop = new RectangleF(5f, 8f, 14f, 8.4f);
    private static readonly RectangleF ChevronRightCrop = new RectangleF(8f, 5f, 8.4f, 14f);
    private static readonly RectangleF XCrop = new RectangleF(5f, 5f, 14f, 14f);
    private static readonly RectangleF FullGrid = new RectangleF(0f, 0f, 24f, 24f);

    /// <summary>The tab icons' stroke, in grid units (Lucide's own is 2).</summary>
    private const float TabIconStroke = 2.5f;

    /// <summary>A whole Lucide icon, <paramref name="size"/> pixels square.</summary>
    private static Sprite Icon(string name, int size, Color colour, string[] paths)
    {
        return Icon(name, size, size, FullGrid, colour, paths, 2f);
    }

    /// <summary>
    /// A Lucide icon's <paramref name="crop"/> of the grid at w x h pixels: its paths stroked 2 units
    /// wide, round caps and joins, as Lucide draws them.
    /// </summary>
    private static Sprite Icon(string name, int w, int h, RectangleF crop, Color colour, string[] paths)
    {
        return Icon(name, w, h, crop, colour, paths, 2f);
    }

    /// <summary>The same, stroked <paramref name="stroke"/> grid units wide.</summary>
    private static Sprite Icon(string name, int w, int h, RectangleF crop, Color colour, string[] paths, float stroke)
    {
        Graphics g;
        Sprite s = New(name, w, h, 0, 0, 0, 0, out g);
        float scale = w / crop.Width;
        using (g)
        using (Pen pen = new Pen(colour, stroke * scale))
        {
            pen.StartCap = pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            foreach (string d in paths)
            {
                using (GraphicsPath path = SvgPath.Parse(d))
                {
                    using (Matrix m = new Matrix())
                    {
                        m.Scale(scale, scale);
                        m.Translate(-crop.X, -crop.Y);
                        path.Transform(m);
                    }
                    g.DrawPath(pen, path);
                }
            }
        }
        return s;
    }

    /// <summary>
    /// SVG path data to a GraphicsPath: M L H V C S Q T A Z, absolute and relative. Arcs become
    /// cubic Béziers (the endpoint-to-centre conversion of the SVG spec, appendix F.6).
    /// </summary>
    private static class SvgPath
    {
        public static GraphicsPath Parse(string d)
        {
            GraphicsPath path = new GraphicsPath();
            List<string> tokens = Tokens(d);
            int i = 0;
            char command = 'M';
            PointF current = PointF.Empty, start = PointF.Empty, control = PointF.Empty;
            char last = ' ';

            while (i < tokens.Count)
            {
                if (char.IsLetter(tokens[i][0]))
                    command = tokens[i++][0];

                bool rel = char.IsLower(command);
                char c = char.ToUpperInvariant(command);
                PointF o = rel ? current : PointF.Empty;

                switch (c)
                {
                    case 'M':
                    {
                        PointF p = Add(o, Num(tokens, ref i), Num(tokens, ref i));
                        path.StartFigure();
                        current = start = p;
                        command = rel ? 'l' : 'L';   // pairs after a move are lines
                        break;
                    }
                    case 'L':
                    {
                        PointF p = Add(o, Num(tokens, ref i), Num(tokens, ref i));
                        path.AddLine(current, p);
                        current = p;
                        break;
                    }
                    case 'H':
                    {
                        float x = Num(tokens, ref i) + (rel ? current.X : 0f);
                        PointF p = new PointF(x, current.Y);
                        path.AddLine(current, p);
                        current = p;
                        break;
                    }
                    case 'V':
                    {
                        float y = Num(tokens, ref i) + (rel ? current.Y : 0f);
                        PointF p = new PointF(current.X, y);
                        path.AddLine(current, p);
                        current = p;
                        break;
                    }
                    case 'C':
                    {
                        PointF c1 = Add(o, Num(tokens, ref i), Num(tokens, ref i));
                        PointF c2 = Add(o, Num(tokens, ref i), Num(tokens, ref i));
                        PointF p = Add(o, Num(tokens, ref i), Num(tokens, ref i));
                        path.AddBezier(current, c1, c2, p);
                        control = c2;
                        current = p;
                        break;
                    }
                    case 'S':
                    {
                        PointF c1 = last == 'C' || last == 'S' ? Mirror(control, current) : current;
                        PointF c2 = Add(o, Num(tokens, ref i), Num(tokens, ref i));
                        PointF p = Add(o, Num(tokens, ref i), Num(tokens, ref i));
                        path.AddBezier(current, c1, c2, p);
                        control = c2;
                        current = p;
                        break;
                    }
                    case 'Q':
                    case 'T':
                    {
                        PointF q = c == 'Q'
                            ? Add(o, Num(tokens, ref i), Num(tokens, ref i))
                            : (last == 'Q' || last == 'T' ? Mirror(control, current) : current);
                        PointF p = Add(o, Num(tokens, ref i), Num(tokens, ref i));
                        path.AddBezier(current, Lerp(current, q, 2f / 3f), Lerp(p, q, 2f / 3f), p);
                        control = q;
                        current = p;
                        break;
                    }
                    case 'A':
                    {
                        float rx = Num(tokens, ref i), ry = Num(tokens, ref i), angle = Num(tokens, ref i);
                        bool large = Num(tokens, ref i) != 0f, sweep = Num(tokens, ref i) != 0f;
                        PointF p = Add(o, Num(tokens, ref i), Num(tokens, ref i));
                        Arc(path, current, p, rx, ry, angle, large, sweep);
                        current = p;
                        break;
                    }
                    case 'Z':
                        path.CloseFigure();
                        current = start;
                        break;
                    default:
                        throw new FormatException("path command " + command + " in " + d);
                }

                last = c;
            }

            return path;
        }

        private static List<string> Tokens(string d)
        {
            List<string> tokens = new List<string>();
            int i = 0;
            while (i < d.Length)
            {
                char ch = d[i];
                if (char.IsLetter(ch) && ch != 'e' && ch != 'E')
                {
                    tokens.Add(ch.ToString());
                    i++;
                }
                else if (char.IsDigit(ch) || ch == '-' || ch == '+' || ch == '.')
                {
                    // One number: a sign, digits with at most one point, an exponent. "1.5.7" is two.
                    int s = i;
                    if (ch == '-' || ch == '+')
                        i++;
                    bool point = false;
                    while (i < d.Length && (char.IsDigit(d[i]) || (d[i] == '.' && !point)))
                    {
                        if (d[i] == '.')
                            point = true;
                        i++;
                    }
                    if (i < d.Length && (d[i] == 'e' || d[i] == 'E'))
                    {
                        i++;
                        if (i < d.Length && (d[i] == '-' || d[i] == '+'))
                            i++;
                        while (i < d.Length && char.IsDigit(d[i]))
                            i++;
                    }
                    tokens.Add(d.Substring(s, i - s));
                }
                else
                {
                    i++;   // spaces and commas
                }
            }
            return tokens;
        }

        private static float Num(List<string> tokens, ref int i)
        {
            if (i >= tokens.Count || char.IsLetter(tokens[i][0]))
                throw new FormatException("a number missing in a path");
            return float.Parse(tokens[i++], NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static PointF Add(PointF o, float x, float y)
        {
            return new PointF(o.X + x, o.Y + y);
        }

        private static PointF Mirror(PointF control, PointF about)
        {
            return new PointF(2f * about.X - control.X, 2f * about.Y - control.Y);
        }

        private static PointF Lerp(PointF a, PointF b, float t)
        {
            return new PointF(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        private static void Arc(GraphicsPath path, PointF p0, PointF p1, float rx, float ry, float angleDeg,
            bool large, bool sweep)
        {
            if (rx == 0f || ry == 0f || (p0.X == p1.X && p0.Y == p1.Y))
            {
                path.AddLine(p0, p1);
                return;
            }

            double phi = angleDeg * Math.PI / 180.0, cos = Math.Cos(phi), sin = Math.Sin(phi);
            double dx = (p0.X - p1.X) / 2.0, dy = (p0.Y - p1.Y) / 2.0;
            double x1 = cos * dx + sin * dy, y1 = -sin * dx + cos * dy;
            double a = Math.Abs(rx), b = Math.Abs(ry);
            double lambda = x1 * x1 / (a * a) + y1 * y1 / (b * b);
            if (lambda > 1.0)
            {
                a *= Math.Sqrt(lambda);
                b *= Math.Sqrt(lambda);
            }

            double num = a * a * b * b - a * a * y1 * y1 - b * b * x1 * x1;
            double den = a * a * y1 * y1 + b * b * x1 * x1;
            double k = Math.Sqrt(Math.Max(0.0, num / den)) * (large == sweep ? -1.0 : 1.0);
            double cx1 = k * a * y1 / b, cy1 = -k * b * x1 / a;
            double cx = cos * cx1 - sin * cy1 + (p0.X + p1.X) / 2.0;
            double cy = sin * cx1 + cos * cy1 + (p0.Y + p1.Y) / 2.0;

            double t1 = Math.Atan2((y1 - cy1) / b, (x1 - cx1) / a);
            double t2 = Math.Atan2((-y1 - cy1) / b, (-x1 - cx1) / a);
            double dt = t2 - t1;
            if (sweep && dt < 0) dt += 2 * Math.PI;
            if (!sweep && dt > 0) dt -= 2 * Math.PI;

            int segments = (int)Math.Ceiling(Math.Abs(dt) / (Math.PI / 2) - 1e-9);
            double step = dt / segments;
            double alpha = 4.0 / 3.0 * Math.Tan(step / 4);
            double t = t1;
            PointF from = p0;
            for (int s = 0; s < segments; s++)
            {
                double ta = t, tb = t + step;
                PointF c1 = Ellipse(cx, cy, a, b, cos, sin, ta, alpha, true);
                PointF c2 = Ellipse(cx, cy, a, b, cos, sin, tb, alpha, false);
                PointF to = s == segments - 1 ? p1 : On(cx, cy, a, b, cos, sin, tb);
                path.AddBezier(from, c1, c2, to);
                from = to;
                t = tb;
            }
        }

        private static PointF On(double cx, double cy, double a, double b, double cos, double sin, double t)
        {
            double x = a * Math.Cos(t), y = b * Math.Sin(t);
            return new PointF((float)(cx + cos * x - sin * y), (float)(cy + sin * x + cos * y));
        }

        /// <summary>A Bézier control point at angle t: the point on the ellipse pushed along its tangent.</summary>
        private static PointF Ellipse(double cx, double cy, double a, double b, double cos, double sin, double t,
            double alpha, bool forward)
        {
            double x = a * Math.Cos(t), y = b * Math.Sin(t);
            double tx = -a * Math.Sin(t), ty = b * Math.Cos(t);
            double f = forward ? alpha : -alpha;
            x += f * tx;
            y += f * ty;
            return new PointF((float)(cx + cos * x - sin * y), (float)(cy + sin * x + cos * y));
        }
    }

    /// <summary>A tab: top corners rounded, square at the bottom where it meets the band.</summary>
    private static Sprite TopTab(string name, Color top, Color bottom)
    {
        Graphics g;
        Sprite s = New(name, 20, 16, 8, 8, 8, 1, out g);
        using (g)
        using (GraphicsPath path = TopRounded(0, 0, 20, 16, 7))
        using (Brush b = top == bottom
            ? (Brush)new SolidBrush(top)
            : new LinearGradientBrush(new PointF(0, 0), new PointF(0, 16), top, bottom))
            g.FillPath(b, path);
        return s;
    }

    // ------------------------------------------------------------------ shapes

    private static Sprite New(string name, int w, int h, int l, int r, int t, int b, out Graphics g)
    {
        Sprite s = new Sprite { Name = name, Bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb),
            Left = l, Right = r, Top = t, Bottom = b };
        g = Graphics.FromImage(s.Bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.Clear(Color.Transparent);
        return s;
    }

    /// <summary>A rounded box: fill (a vertical gradient when top != bottom) inside a border ring.</summary>
    private static Sprite Box(string name, int w, int h, float radius, Color top, Color bottom, Color border,
        float borderWidth, int slice)
    {
        Graphics g;
        Sprite s = New(name, w, h, slice, slice, slice, slice, out g);
        using (g)
        {
            bool ring = border.A > 0 && borderWidth > 0;
            float inset = ring ? borderWidth : 0f;

            using (GraphicsPath inner = Rounded(inset, inset, w - 2 * inset, h - 2 * inset, Math.Max(0f, radius - inset)))
            using (Brush fill = top == bottom
                ? (Brush)new SolidBrush(top)
                : new LinearGradientBrush(new PointF(0, 0), new PointF(0, h), top, bottom))
            {
                g.FillPath(fill, inner);

                if (ring)
                {
                    using (GraphicsPath outer = Rounded(0, 0, w, h, radius))
                    using (Region band = new Region(outer))
                    using (SolidBrush b = new SolidBrush(border))
                    {
                        band.Exclude(inner);
                        g.FillRegion(b, band);
                    }
                }
            }
        }
        return s;
    }

    private static Sprite TitleStrip(string name, int w, int h, float radius, Color fill, Color line)
    {
        Graphics g;
        int slice = (int)radius + 1;
        Sprite s = New(name, w, h, slice, slice, slice, 2, out g);
        using (g)
        {
            using (GraphicsPath path = TopRounded(0, 0, w, h, radius))
            using (SolidBrush b = new SolidBrush(fill))
                g.FillPath(b, path);

            g.SmoothingMode = SmoothingMode.None;
            using (SolidBrush b = new SolidBrush(line))
                g.FillRectangle(b, 0, h - 1, w, 1);
        }
        return s;
    }

    private static Sprite Track(string name, int height, Color start, Color end, bool inset)
    {
        Graphics g;
        int w = 16;
        int end_ = height / 2 + 1;
        Sprite s = New(name, w, height, end_, end_, 0, 0, out g);
        using (g)
        {
            using (GraphicsPath path = Rounded(0, 0, w, height, height / 2f))
            using (Brush b = start == end
                ? (Brush)new SolidBrush(start)
                : new LinearGradientBrush(new PointF(0, 0), new PointF(w, 0), start, end))
                g.FillPath(b, path);

            if (inset)
            {
                // The mockup's "inset 0 1px 1px": a darker top line inside the track.
                g.SetClip(new Rectangle(0, 0, w, 1));
                using (GraphicsPath path = Rounded(0, 0, w, height, height / 2f))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
                    g.FillPath(b, path);
                g.ResetClip();
            }
        }
        return s;
    }

    private static Sprite Thumb(string name, int size, Color fill, Color border, int borderWidth, bool shadow)
    {
        Graphics g;
        int pad = shadow ? 2 : 0;
        int w = size + 2 * pad;
        Sprite s = New(name, w, w, 0, 0, 0, 0, out g);
        using (g)
        {
            if (shadow)
            {
                // A soft shadow one pixel down: rings of falling alpha.
                for (int i = 2; i >= 0; i--)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(40 + (2 - i) * 30, 0, 0, 0)))
                        g.FillEllipse(b, pad - i * 0.6f, pad + 1 - i * 0.6f, size + i * 1.2f, size + i * 1.2f);
                }
            }

            if (borderWidth > 0)
            {
                using (SolidBrush b = new SolidBrush(border))
                    g.FillEllipse(b, pad, pad, size, size);
                using (SolidBrush b = new SolidBrush(fill))
                    g.FillEllipse(b, pad + borderWidth, pad + borderWidth, size - 2 * borderWidth, size - 2 * borderWidth);
            }
            else
            {
                using (SolidBrush b = new SolidBrush(fill))
                    g.FillEllipse(b, pad, pad, size, size);
            }
        }
        return s;
    }

    private static Sprite Switch(string name, Color track, Color knob, bool on)
    {
        Graphics g;
        Sprite s = New(name, 32, 18, 0, 0, 0, 0, out g);
        using (g)
        {
            using (GraphicsPath path = Rounded(0, 0, 32, 18, 9))
            using (SolidBrush b = new SolidBrush(track))
                g.FillPath(b, path);

            float x = on ? 16f : 2f;
            using (SolidBrush b = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                g.FillEllipse(b, x - 0.5f, 2.5f, 15f, 15f);
            using (SolidBrush b = new SolidBrush(knob))
                g.FillEllipse(b, x, 2f, 14f, 14f);
        }
        return s;
    }

    private static Sprite TickBox(string name, Color fill, Color border, Color tick)
    {
        return TickBox(name, 16, 3, fill, border, 1f, tick);
    }

    private static Sprite TickBox(string name, int size, float radius, Color fill, Color border, float borderWidth, Color tick)
    {
        Sprite s = Box(name, size, size, radius, fill, fill, border, borderWidth, 0);
        if (tick.A > 0)
        {
            float k = size / 16f;
            using (Graphics g = Graphics.FromImage(s.Bitmap))
            using (Pen pen = new Pen(tick, 2f * k))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                pen.StartCap = pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                g.DrawLines(pen, new[] { new PointF(4f * k, 8.2f * k), new PointF(6.8f * k, 11f * k), new PointF(12f * k, 5f * k) });
            }
        }
        return s;
    }

    private static Sprite Underline(string name, Color line)
    {
        Graphics g;
        Sprite s = New(name, 8, 8, 0, 0, 0, 2, out g);
        using (g)
        using (SolidBrush b = new SolidBrush(line))
            g.FillRectangle(b, 0, 6, 8, 2);
        return s;
    }

    private static Sprite Line(string name, Color line)
    {
        Graphics g;
        Sprite s = New(name, 4, 1, 0, 0, 0, 0, out g);
        using (g)
        using (SolidBrush b = new SolidBrush(line))
            g.FillRectangle(b, 0, 0, 4, 1);
        return s;
    }

    private static GraphicsPath Rounded(float x, float y, float w, float h, float r)
    {
        GraphicsPath path = new GraphicsPath();
        r = Math.Min(r, Math.Min(w, h) / 2f);
        if (r <= 0f)
        {
            path.AddRectangle(new RectangleF(x, y, w, h));
            return path;
        }

        float d = 2f * r;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        path.AddArc(x, y + h - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static GraphicsPath TopRounded(float x, float y, float w, float h, float r)
    {
        GraphicsPath path = new GraphicsPath();
        float d = 2f * r;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddLine(x + w, y + h, x, y + h);
        path.CloseFigure();
        return path;
    }

    // ------------------------------------------------------------------ packing

    /// <summary>Shelf packing, tallest first, into the narrowest power-of-two square-ish sheet.</summary>
    private static void Pack(List<Sprite> sprites, out int width, out int height)
    {
        List<Sprite> order = new List<Sprite>(sprites);
        // Stable: tallest first, then the order they were made in.
        order.Sort((a, b) =>
        {
            int c = b.Bitmap.Height.CompareTo(a.Bitmap.Height);
            return c != 0 ? c : sprites.IndexOf(a).CompareTo(sprites.IndexOf(b));
        });

        const int pad = Extrude * 2 + Gap;
        for (width = 64; ; width *= 2)
        {
            int x = 0, y = 0, shelf = 0;
            bool fits = true;
            foreach (Sprite s in order)
            {
                int w = s.Bitmap.Width + pad, h = s.Bitmap.Height + pad;
                if (w > width) { fits = false; break; }
                if (x + w > width)
                {
                    x = 0;
                    y += shelf;
                    shelf = 0;
                }
                s.X = x + Extrude;
                s.Y = y + Extrude;
                x += w;
                shelf = Math.Max(shelf, h);
            }

            int used = y + shelf;
            if (fits && used <= width)
            {
                height = 1;
                while (height < used)
                    height *= 2;
                return;
            }
        }
    }

    /// <summary>Copies a sprite in and repeats its edge pixels one pixel outwards.</summary>
    private static void Blit(Bitmap sheet, Sprite s)
    {
        Bitmap b = s.Bitmap;
        for (int y = -Extrude; y < b.Height + Extrude; y++)
        {
            for (int x = -Extrude; x < b.Width + Extrude; x++)
            {
                int sx = Math.Min(Math.Max(x, 0), b.Width - 1);
                int sy = Math.Min(Math.Max(y, 0), b.Height - 1);
                sheet.SetPixel(s.X + x, s.Y + y, b.GetPixel(sx, sy));
            }
        }
    }

    // ------------------------------------------------------------------ colours

    private static Color Hex(string hex)
    {
        int v = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return Color.FromArgb(255, (v >> 16) & 255, (v >> 8) & 255, v & 255);
    }

    private static Color Rgba(int r, int g, int b, double a)
    {
        return Color.FromArgb((int)Math.Round(a * 255), r, g, b);
    }

    private static Color White(double a)
    {
        return Rgba(255, 255, 255, a);
    }
}
