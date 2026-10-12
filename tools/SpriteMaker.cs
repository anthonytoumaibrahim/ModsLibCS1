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

        // A row's reset button, every skin: a round arrow, drawn here from an arc and a triangle
        // (no third-party icon).
        sprites.Add(ResetArrow("reset", 14, Color.FromArgb(235, 255, 255, 255)));

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
        list.Add(Cross(x + "close", 12, p.CloseGlyph));

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
        list.Add(Chevron(x + "arrow", 10, 6, p.DropArrow));
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

        list.Add(Box(x + "panel", 32, 32, 14, Rgba(60, 86, 112, .58), Rgba(42, 62, 82, .62), White(.07), 1, 15));
        list.Add(TitleStrip(x + "title", 32, 32, 13, Rgba(14, 20, 28, .94), Color.Transparent));
        list.Add(Box(x + "close-hover", 16, 16, 6, White(.12), White(.12), Color.Transparent, 0, 7));
        list.Add(Cross(x + "close", 14, Hex("ffffff"), 2.6f));

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
        list.Add(Chevron(x + "arrow", 10, 6, Hex("ffffff")));
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

    /// <summary>
    /// A clockwise round arrow: three quarters and a bit of a circle, open at the top left, the
    /// arrowhead at its end pointing on round the circle.
    /// </summary>
    private static Sprite ResetArrow(string name, int size, Color colour)
    {
        Graphics g;
        Sprite s = New(name, size, size, 0, 0, 0, 0, out g);
        using (g)
        {
            float c = size / 2f, r = size / 2f - 2.6f;
            const float start = 250f, sweep = 285f;   // GDI+ degrees: clockwise on screen, 0 = right

            using (Pen pen = new Pen(colour, 1.9f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Flat;
                g.DrawArc(pen, c - r, c - r, 2f * r, 2f * r, start, sweep);
            }

            // The arrowhead at the arc's end, pointing along it (clockwise).
            double end = (start + sweep) * Math.PI / 180.0;
            PointF tip = new PointF((float)(c + r * Math.Cos(end)), (float)(c + r * Math.Sin(end)));
            PointF along = new PointF((float)-Math.Sin(end), (float)Math.Cos(end));   // clockwise tangent
            PointF outward = new PointF((float)Math.Cos(end), (float)Math.Sin(end));
            const float length = 4.2f, half = 3.1f;
            PointF point = new PointF(tip.X + along.X * length * 0.6f, tip.Y + along.Y * length * 0.6f);
            PointF back = new PointF(tip.X - along.X * length * 0.4f, tip.Y - along.Y * length * 0.4f);
            PointF[] head =
            {
                point,
                new PointF(back.X + outward.X * half, back.Y + outward.Y * half),
                new PointF(back.X - outward.X * half, back.Y - outward.Y * half),
            };
            using (SolidBrush b = new SolidBrush(colour))
                g.FillPolygon(b, head);
        }
        return s;
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

    private static Sprite Cross(string name, int size, Color colour)
    {
        return Cross(name, size, colour, 1.8f);
    }

    private static Sprite Cross(string name, int size, Color colour, float stroke)
    {
        Graphics g;
        Sprite s = New(name, size, size, 0, 0, 0, 0, out g);
        using (g)
        using (Pen pen = new Pen(colour, stroke))
        {
            pen.StartCap = pen.EndCap = LineCap.Round;
            g.DrawLine(pen, 1.5f, 1.5f, size - 1.5f, size - 1.5f);
            g.DrawLine(pen, size - 1.5f, 1.5f, 1.5f, size - 1.5f);
        }
        return s;
    }

    private static Sprite Chevron(string name, int w, int h, Color colour)
    {
        Graphics g;
        Sprite s = New(name, w, h, 0, 0, 0, 0, out g);
        using (g)
        using (Pen pen = new Pen(colour, 1.8f))
        {
            pen.StartCap = pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            g.DrawLines(pen, new[] { new PointF(1f, 1f), new PointF(w / 2f, h - 1f), new PointF(w - 1f, 1f) });
        }
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
