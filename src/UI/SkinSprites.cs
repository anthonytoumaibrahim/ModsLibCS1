using System.Collections.Generic;

namespace ModsLibCS1.UI
{
    /// <summary>
    /// The sprite each part of a skin draws with. See-through and Game blue name sprites of the
    /// library's own sheet; Vanilla names the game's 'Ingame' atlas sprites the mods used before
    /// (each read out of the live atlas, never guessed). A null part is not drawn.
    /// </summary>
    /// <remarks>Pure, C# 5: the offline test checks every name here against sheet.txt.</remarks>
    public sealed class SkinSprites
    {
        public string Panel, Title, Rule;
        public string Close, CloseHover, ClosePressed, CloseGlyph;
        public string Button, ButtonHover, ButtonPressed, ButtonDisabled, Accent, AccentHover;
        public string Field, FieldHover, FieldFocus, Selection;
        public string Drop, DropHover, DropFocus, DropDisabled, DropArrow, List, ListHover, ListHighlight;
        public string Track, Fill, Thumb;
        public string SegStrip, SegHover, SegSelected;
        public string ToggleOff, ToggleOn;
        public string Value;
        public string TabStrip, Tab, TabHover, TabPressed, TabSelected;
        public string Hud, HudHover;

        /// <summary>Frosted: the darker band under the tabs, and the line between rows.</summary>
        public string Band, RowLine;

        /// <summary>
        /// A row's reset icon. In every skin's list that has the library's sheet; Vanilla's reset
        /// button takes it from the sheet too (Builder.AddReset).
        /// </summary>
        public string Reset;

        /// <summary>True for the library's own sheet, false for the game's atlas.</summary>
        public bool OwnSheet;

        public static SkinSprites SeeThrough()
        {
            SkinSprites s = Ours("st-");
            s.ToggleOff = "st-switch-off";
            s.ToggleOn = "st-switch-on";
            s.Value = "st-value";
            s.TabSelected = "st-tab-selected";
            return s;
        }

        public static SkinSprites GameBlue()
        {
            SkinSprites s = Ours("gb-");
            s.ToggleOff = "gb-tick-off";
            s.ToggleOn = "gb-tick-on";
            s.TabStrip = "gb-tab-strip";
            s.TabHover = "gb-tab-hover";
            s.TabPressed = "gb-tab-selected";
            s.TabSelected = "gb-tab-selected";
            return s;
        }

        public static SkinSprites Frosted()
        {
            SkinSprites s = Ours("fr-");
            s.ToggleOff = "fr-tick-off";
            s.ToggleOn = "fr-tick-on";
            s.Tab = "fr-tab";
            s.TabHover = "fr-tab-hover";
            s.TabPressed = "fr-tab-selected";
            s.TabSelected = "fr-tab-selected";
            s.Band = "fr-band";
            s.RowLine = "fr-row-line";
            return s;
        }

        public static SkinSprites Vanilla()
        {
            SkinSprites s = new SkinSprites();
            s.OwnSheet = false;
            s.Panel = "MenuPanel2";
            s.Close = "buttonclose";
            s.CloseHover = "buttonclosehover";
            s.ClosePressed = "buttonclosepressed";
            s.Button = "ButtonMenu";
            s.ButtonHover = "ButtonMenuHovered";
            s.ButtonPressed = "ButtonMenuPressed";
            s.ButtonDisabled = "ButtonMenuDisabled";
            s.Field = "TextFieldPanelHovered";
            s.FieldHover = "TextFieldPanelHovered";
            s.FieldFocus = "TextFieldPanel";
            s.Selection = "EmptySprite";
            s.Drop = "OptionsDropbox";
            s.DropHover = "OptionsDropboxHovered";
            s.DropFocus = "OptionsDropboxFocused";
            s.DropDisabled = "OptionsDropboxDisabled";
            s.List = "OptionsDropboxListbox";
            s.ListHover = "ListItemHover";
            s.ListHighlight = "ListItemHighlight";
            s.Track = "BudgetSlider";
            s.Thumb = "SliderBudget";
            s.ToggleOff = "check-unchecked";
            s.ToggleOn = "check-checked";
            s.Tab = "GenericTab";
            s.TabHover = "GenericTabHovered";
            s.TabPressed = "GenericTabPressed";
            s.TabSelected = "GenericTabFocused";
            return s;
        }

        private static SkinSprites Ours(string x)
        {
            SkinSprites s = new SkinSprites();
            s.OwnSheet = true;
            s.Panel = x + "panel";
            s.Title = x + "title";
            s.Rule = x + "rule";
            s.CloseHover = x + "close-hover";
            s.ClosePressed = x + "close-hover";
            s.CloseGlyph = x + "close";
            s.Button = x + "button";
            s.ButtonHover = x + "button-hover";
            s.ButtonPressed = x + "button-pressed";
            s.ButtonDisabled = x + "button";
            s.Accent = x + "accent";
            s.AccentHover = x + "accent-hover";
            s.Field = x + "field";
            s.FieldHover = x + "field";
            s.FieldFocus = x + "field-focus";
            s.Selection = "white";
            s.Drop = x + "drop";
            s.DropHover = x + "drop-hover";
            s.DropFocus = x + "drop-focus";
            s.DropDisabled = x + "drop";
            s.DropArrow = x + "arrow";
            s.List = x + "list";
            s.ListHover = x + "list-hover";
            s.ListHighlight = x + "list-highlight";
            s.Track = x + "track";
            s.Fill = x + "fill";
            s.Thumb = x + "thumb";
            s.SegStrip = x + "seg-strip";
            s.SegHover = x + "seg-hover";
            s.SegSelected = x + "seg-selected";
            s.Hud = x + "hud";
            s.HudHover = x + "hud-hover";
            s.Reset = "reset";
            return s;
        }

        /// <summary>Every sprite named, once each.</summary>
        public string[] All()
        {
            string[] names =
            {
                Panel, Title, Rule, Close, CloseHover, ClosePressed, CloseGlyph,
                Button, ButtonHover, ButtonPressed, ButtonDisabled, Accent, AccentHover,
                Field, FieldHover, FieldFocus, Selection,
                Drop, DropHover, DropFocus, DropDisabled, DropArrow, List, ListHover, ListHighlight,
                Track, Fill, Thumb, SegStrip, SegHover, SegSelected, ToggleOff, ToggleOn, Value,
                TabStrip, Tab, TabHover, TabPressed, TabSelected, Hud, HudHover, Band, RowLine, Reset,
            };

            List<string> all = new List<string>();
            foreach (string name in names)
            {
                if (name != null && !all.Contains(name))
                    all.Add(name);
            }
            return all.ToArray();
        }
    }
}
