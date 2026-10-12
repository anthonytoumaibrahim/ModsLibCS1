using UnityEngine;

namespace ModsLibCS1.UI
{
    /// <summary>How an on/off row is drawn.</summary>
    public enum ToggleShape
    {
        /// <summary>A switch, lined up with the sliders (See-through).</summary>
        Switch,

        /// <summary>A tick box, lined up with the sliders (Game blue).</summary>
        TickAligned,

        /// <summary>The game's tick box left of the name (Vanilla).</summary>
        TickLeft,
    }

    /// <summary>How a choice row is drawn.</summary>
    public enum ChoiceShape
    {
        /// <summary>A row of buttons; a dropdown when the names don't fit.</summary>
        Segments,

        /// <summary>A slider over the choices, the choice's name as its value (Vanilla).</summary>
        Slider,
    }

    /// <summary>How the tab names are drawn.</summary>
    public enum TabShape
    {
        /// <summary>Names on the panel, a line under the open one (See-through).</summary>
        Underline,

        /// <summary>Pills on a dark strip (Game blue).</summary>
        Pills,

        /// <summary>The game's tab sprites (Vanilla).</summary>
        GameTabs,

        /// <summary>Rounded-top blocks standing on a darker band, the open one blue (Frosted).</summary>
        Blocks,
    }

    /// <summary>
    /// One skin: its sprites, colours, sizes and the shape of the four things that differ
    /// (on/off rows, choice rows, values, tabs). Colours are the mockup's, picked by the author.
    /// </summary>
    public sealed class Theme
    {
        public Skin Skin;
        public SkinSprites Sprites;

        public ToggleShape Toggle;
        public ChoiceShape Choice;
        public TabShape Tabs;
        public bool BoxedValues;
        public bool TitleStrip;
        public bool TextShadow;
        public bool HeadingRule;
        public bool HeadingCaps;

        /// <summary>The heading's line runs under it, full width, rather than after it.</summary>
        public bool HeadingLineUnder;

        /// <summary>A blurred copy of the screen behind the panel (<see cref="Backdrop"/>).</summary>
        public bool Blur;

        /// <summary>Row names, buttons, tabs and choices in capitals with a little spacing.</summary>
        public bool Caps;

        /// <summary>A faint line under every control row.</summary>
        public bool RowLines;

        public bool TitleCentered;

        // Text
        public Color32 Text, Title, Heading, Status, Warning, Value;
        public Color32 TabText, TabHoverText, TabSelectedText;

        /// <summary>Icon tabs (Builder.AddTabStrip with icons): the icon's colour, hovered, and on the open tab.</summary>
        public Color32 TabIcon, TabIconHover, TabIconSelected;
        public Color32 ButtonText, AccentText;
        public Color32 FieldText, Invalid, Selection;
        public Color32 DropText, ListText;
        public Color32 SegText, SegSelectedText;
        public Color32 Shadow;

        // Sizes (UI units at the game's 1080-high UI)
        public float RowHeight = 34f;
        public float LabelWidth = 196f;
        public float ValueWidth = 128f;
        public float Gap = 6f;
        public float TitleHeight = 40f;
        public float Margin = 14f;
        public float HeadingHeight = 24f;
        public float TrackHeight;
        public float ThumbSize;

        public float TextScale = 0.8f;
        public float TitleScale = 1.1f;
        public float HeadingScale = 0.85f;
        public float TabScale = 0.75f;
        public float NoteScale = 0.7f;

        public static Theme For(Skin skin)
        {
            switch (skin)
            {
                case Skin.GameBlue: return GameBlue();
                case Skin.Vanilla: return Vanilla();
                case Skin.Frosted: return Frosted();
                default: return SeeThrough();
            }
        }

        /// <summary>C, the author's pick: charcoal at ~75%, the sky shows through.</summary>
        public static Theme SeeThrough()
        {
            Theme t = new Theme();
            t.Skin = Skin.SeeThrough;
            t.Sprites = SkinSprites.SeeThrough();
            t.Toggle = ToggleShape.Switch;
            t.Choice = ChoiceShape.Segments;
            t.Tabs = TabShape.Underline;
            t.BoxedValues = true;
            t.TitleStrip = true;
            t.TextShadow = true;
            t.HeadingRule = true;
            t.HeadingCaps = true;
            t.HeadingScale = 0.7f;

            t.Text = Hex(0xeef1f5);
            t.Title = Hex(0xffffff);
            t.Heading = Hex(0xa9b4c2);
            t.Status = Hex(0xa3d0ff);
            t.Warning = Hex(0xffc07a);
            t.Value = Hex(0xf1f3f6);
            t.TabText = Hex(0xb4bdc9);
            t.TabHoverText = Hex(0xffffff);
            t.TabSelectedText = Hex(0xffffff);
            t.TabIcon = Hex(0x9aa5b3);
            t.TabIconHover = Hex(0xffffff);
            t.TabIconSelected = Hex(0x4c9bff);   // blue over the white underline
            t.ButtonText = Hex(0xf1f3f6);
            t.AccentText = Hex(0xffffff);
            t.FieldText = Hex(0xe8ebef);
            t.Invalid = Hex(0xff6b6b);
            t.Selection = new Color32(76, 155, 255, 255);
            t.DropText = Hex(0xe8ebef);
            t.ListText = Hex(0xc9cfd7);
            t.SegText = Hex(0xc3cad4);
            t.SegSelectedText = Hex(0xffffff);
            t.Shadow = new Color32(0, 0, 0, 153);
            t.TrackHeight = 4f;
            t.ThumbSize = 18f;   // 14 px knob + its shadow
            return t;
        }

        /// <summary>B: the same layout on a deep-blue opaque panel.</summary>
        public static Theme GameBlue()
        {
            Theme t = new Theme();
            t.Skin = Skin.GameBlue;
            t.Sprites = SkinSprites.GameBlue();
            t.Toggle = ToggleShape.TickAligned;
            t.Choice = ChoiceShape.Segments;
            t.Tabs = TabShape.Pills;
            t.BoxedValues = false;
            t.TitleStrip = true;
            t.TextShadow = false;
            t.HeadingRule = true;
            t.HeadingCaps = false;

            t.Text = Hex(0xe3eef7);
            t.Title = Hex(0xffffff);
            t.Heading = Hex(0xffffff);
            t.Status = Hex(0x9fd3ff);
            t.Warning = Hex(0xffc285);
            t.Value = Hex(0xcfe6fa);
            t.TabText = Hex(0x9cc4e4);
            t.TabHoverText = Hex(0xd6ebfb);
            t.TabSelectedText = Hex(0xffffff);
            t.TabIcon = Hex(0x6f93b3);
            t.TabIconHover = Hex(0xd6ebfb);
            t.TabIconSelected = Hex(0xffffff);   // white on the open pill
            t.ButtonText = Hex(0xffffff);
            t.AccentText = Hex(0x08202f);
            t.FieldText = Hex(0xe3eef7);
            t.Invalid = Hex(0xff7a7a);
            t.Selection = new Color32(73, 176, 242, 255);
            t.DropText = Hex(0xffffff);
            t.ListText = Hex(0xc3dcef);
            t.SegText = Hex(0x9cc4e4);
            t.SegSelectedText = Hex(0xffffff);
            t.Shadow = new Color32(0, 0, 0, 153);
            t.TrackHeight = 6f;
            t.ThumbSize = 20f;   // 16 px knob + its shadow
            return t;
        }

        /// <summary>
        /// D in the mockup, after Cities: Skylines II's photo mode (the author: "I really like its
        /// style, rounded, see through with blur").
        /// </summary>
        public static Theme Frosted()
        {
            Theme t = new Theme();
            t.Skin = Skin.Frosted;
            t.Sprites = SkinSprites.Frosted();
            t.Toggle = ToggleShape.TickLeft;
            t.Choice = ChoiceShape.Segments;
            t.Tabs = TabShape.Blocks;
            t.BoxedValues = false;
            t.TitleStrip = true;
            t.TextShadow = true;
            t.HeadingRule = true;
            t.HeadingLineUnder = true;
            t.HeadingCaps = true;
            t.Blur = true;
            t.Caps = true;
            t.RowLines = true;
            t.TitleCentered = true;
            t.TextScale = 0.75f;
            t.HeadingScale = 0.8f;
            t.TitleScale = 1.05f;
            t.TabScale = 0.75f;

            t.Text = Hex(0xf3f6f9);
            t.Title = Hex(0x46b9f0);
            t.Heading = Hex(0xffffff);
            t.Status = Hex(0xa8dcff);
            t.Warning = Hex(0xffc27a);
            t.Value = Hex(0xffffff);
            t.TabText = Hex(0xd8e0e8);
            t.TabHoverText = Hex(0xffffff);
            t.TabSelectedText = Hex(0xffffff);
            t.TabIcon = Hex(0xa9b8c8);
            t.TabIconHover = Hex(0xffffff);
            t.TabIconSelected = Hex(0xffffff);   // white on the blue block (navy read as black)
            t.ButtonText = Hex(0xffffff);
            t.AccentText = Hex(0xffffff);
            t.FieldText = Hex(0xffffff);
            t.Invalid = Hex(0xff7a7a);
            t.Selection = new Color32(63, 169, 240, 255);
            t.DropText = Hex(0xffffff);
            t.ListText = Hex(0xd8e0e8);
            t.SegText = Hex(0xd8e0e8);
            t.SegSelectedText = Hex(0xffffff);
            t.Shadow = new Color32(0, 0, 0, 90);
            t.TrackHeight = 4f;
            t.ThumbSize = 18f;
            return t;
        }

        /// <summary>Today's panel, bit for bit: the game's sprites and the layout the mods had.</summary>
        public static Theme Vanilla()
        {
            Theme t = new Theme();
            t.Skin = Skin.Vanilla;
            t.Sprites = SkinSprites.Vanilla();
            t.Toggle = ToggleShape.TickLeft;
            t.Choice = ChoiceShape.Slider;
            t.Tabs = TabShape.GameTabs;
            t.BoxedValues = false;
            t.TitleStrip = false;
            t.TextShadow = false;
            t.HeadingRule = false;
            t.HeadingCaps = false;
            t.ValueWidth = 118f;
            t.Gap = 4f;

            t.Text = Hex(0xffffff);
            t.Title = Hex(0xffffff);
            t.Heading = Hex(0xffffff);
            t.Status = Hex(0xb9ddfe);
            t.Warning = Hex(0xffbe78);
            t.Value = Hex(0xffffff);
            t.TabText = Hex(0xb9ddfe);
            t.TabHoverText = Hex(0xb9ddfe);
            t.TabSelectedText = Hex(0xffffff);
            t.ButtonText = Hex(0xffffff);
            t.AccentText = Hex(0xffffff);
            t.FieldText = Hex(0x000000);
            t.Invalid = new Color32(190, 30, 30, 255);
            t.Selection = new Color32(0, 172, 234, 255);
            t.DropText = Hex(0xffffff);
            t.ListText = new Color32(170, 170, 170, 255);
            t.SegText = Hex(0xffffff);
            t.SegSelectedText = Hex(0xffffff);
            t.Shadow = new Color32(0, 0, 0, 0);
            t.TrackHeight = 9f;
            t.ThumbSize = 16f;
            return t;
        }

        private static Color32 Hex(int rgb)
        {
            return new Color32((byte)((rgb >> 16) & 255), (byte)((rgb >> 8) & 255), (byte)(rgb & 255), 255);
        }
    }
}
