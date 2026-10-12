using System;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace ModsLibCS1.UI
{
    /// <summary>
    /// Builds a panel's controls out of the game's own UI components, in one skin.
    /// </summary>
    /// <remarks>
    /// Ported from Volumetric Weather's UIBuilder, behaviour and comments kept; Vanilla draws
    /// exactly what that UIBuilder drew. Make one with <see cref="For"/> per panel build: a skin
    /// change rebuilds the panel with a new Builder.
    ///
    /// Sprite names are not compile-checked, and a wrong one fails silently as an invisible
    /// control. The library's own names are checked against the sheet when the Builder is made
    /// (a skin missing any falls back to Vanilla); the Vanilla names were read out of the live
    /// 'Ingame' atlas rather than guessed.
    /// </remarks>
    public sealed class Builder
    {
        public readonly Theme Theme;
        public readonly UITextureAtlas Atlas;

        private SkinSprites S { get { return Theme.Sprites; } }
        private bool Vanilla { get { return Theme.Skin == Skin.Vanilla; } }

        /// <summary>The last frame's tint (Frosted): the band under the tabs goes just above it.</summary>
        private UIComponent _tint;

        private Builder(Theme theme, UITextureAtlas atlas)
        {
            Theme = theme;
            Atlas = atlas;
            LabelWidth = theme.LabelWidth;
        }

        /// <summary>
        /// The column the rows' names stand in (the theme's 196 to begin with). A panel may widen it
        /// for a page whose names are long (<see cref="NameWidth"/>); every row built after the
        /// change lines up on the new width.
        /// </summary>
        public float LabelWidth { get; set; }

        /// <summary>
        /// Room kept free at the right end of every row for a reset button (<see cref="AddReset"/>);
        /// 0, the default, keeps none.
        /// </summary>
        public float ResetColumn { get; set; }

        /// <summary>Where a row's right-hand parts end.</summary>
        private float Right(UIComponent parent)
        {
            return parent.width - ResetColumn;
        }

        /// <summary>
        /// A Builder for <paramref name="skin"/>; Vanilla's if the library's sheet couldn't be
        /// loaded or lacks a sprite the skin needs (logged), so no skin can leave a blank panel.
        /// </summary>
        public static Builder For(Skin skin)
        {
            Theme theme = Theme.For(skin);
            UITextureAtlas atlas = UI.Atlas.For(theme.Sprites);
            string missing = UI.Atlas.FirstMissing(atlas, theme.Sprites);

            if (missing != null && theme.Skin != Skin.Vanilla)
            {
                LibLog.Msg("skin " + skin + ": " + (atlas == null
                    ? "no atlas (" + (UI.Atlas.Problem ?? "the game's UI isn't there yet") + ")"
                    : "sprite '" + missing + "' missing") + ", drawing Vanilla instead");
                theme = Theme.Vanilla();
                atlas = UI.Atlas.Game;
                missing = UI.Atlas.FirstMissing(atlas, theme.Sprites);
            }

            if (missing != null)
                LibLog.Msg("skin Vanilla: the game's atlas lacks '" + missing + "'");

            return new Builder(theme, atlas);
        }

        /// <summary>The skin this Builder really draws (Vanilla after a fallback).</summary>
        public Skin Skin
        {
            get { return Theme.Skin; }
        }

        public float RowHeight
        {
            get { return Theme.RowHeight; }
        }

        // ------------------------------------------------------------------ text

        /// <summary>Colour and, in skins that want it, a drop shadow (text over the see-through panel).</summary>
        public void Paint(UITextComponent text, Color32 colour)
        {
            text.textColor = colour;
            if (Theme.TextShadow)
            {
                text.useDropShadow = true;
                text.dropShadowColor = Theme.Shadow;
                text.dropShadowOffset = new Vector2(0f, -1f);
            }
        }

        /// <summary>
        /// The width of <paramref name="text"/> in UI units at <paramref name="scale"/>, in the
        /// game's default font -- the way UIButton.AutoSize measures (UIFont.ObtainRenderer,
        /// pixelRatio = PixelsToUnits).
        /// </summary>
        public float Measure(string text, float scale)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;

            UIView view = UIView.GetAView();
            UIFont font = view != null ? view.defaultFont : null;
            if (font == null || !font.isValid)
                return text.Length * 9f * scale;   // a guess on the wide side: a too-wide row becomes a dropdown

            using (UIFontRenderer renderer = font.ObtainRenderer())
            {
                renderer.wordWrap = false;
                renderer.multiLine = false;
                renderer.processMarkup = false;
                renderer.maxSize = new Vector2(10000f, 100f);
                renderer.textScale = scale;
                renderer.pixelRatio = view.PixelsToUnits();
                return renderer.MeasureString(text).x;
            }
        }

        /// <summary>A name, button or tab text as the skin writes it: in capitals for Frosted.</summary>
        public string Caps(string text)
        {
            return Theme.Caps && text != null ? text.ToUpperInvariant() : text;
        }

        /// <summary>Capitals get one unit between letters, as the mockup's letter-spacing.</summary>
        private void Spaced(UITextComponent text)
        {
            if (Theme.Caps)
                text.characterSpacing = 1;
        }

        /// <summary>The faint line under a control row (Frosted); not greyed with the row.</summary>
        private void AddRowLine(UIComponent parent, float y, BuiltRow row)
        {
            row.Line = AddRowLine(parent, y);
        }

        /// <summary>
        /// The faint line under a row the caller builds itself (a mod's own kind of row), in skins
        /// that draw one; null in the others.
        /// </summary>
        public UISprite AddRowLine(UIComponent parent, float y)
        {
            if (!Theme.RowLines || S.RowLine == null)
                return null;

            UISprite line = parent.AddUIComponent<UISprite>();
            line.atlas = Atlas;
            line.spriteName = S.RowLine;
            line.size = new Vector2(parent.width, 1f);
            line.relativePosition = new Vector3(0f, y + Theme.RowHeight - 1f);
            line.isInteractive = false;
            return line;
        }

        public UILabel AddLabel(UIComponent parent, string text, Vector3 position, float scale)
        {
            UILabel label = parent.AddUIComponent<UILabel>();
            label.text = text;
            label.textScale = scale;
            label.relativePosition = position;
            Paint(label, Theme.Text);
            return label;
        }

        /// <summary>
        /// A wrapped paragraph under a row: the place for a warning, which a label is read once
        /// and a tooltip never.
        /// </summary>
        public UILabel AddNote(UIComponent parent, string text, float y, float height, Color32 colour)
        {
            UILabel note = AddLabel(parent, text, new Vector3(24f, y), Theme.NoteScale);
            note.autoSize = false;
            note.wordWrap = true;
            note.size = new Vector2(parent.width - 28f, height);
            Paint(note, colour);
            return note;
        }

        /// <summary>A status line: a whole row of text in the status colour.</summary>
        public UILabel AddStatus(UIComponent parent, float y, string text)
        {
            UILabel status = AddLabel(parent, text, new Vector3(0f, y + 8f), Theme.TextScale);
            status.autoSize = false;
            status.size = new Vector2(parent.width, 20f);
            Paint(status, Theme.Status);
            return status;
        }

        /// <summary>A group heading inside a page; in the new skins small grey capitals and a line.</summary>
        public HeadingRow AddHeading(UIComponent parent, string text, float y)
        {
            return AddHeading(parent, text, y, 0f, new HeadingRow());
        }

        /// <summary>
        /// A heading that folds the rows under it away and back: an arrow before its name (down while
        /// open, right while folded), the whole width clickable and softly lit on hover. It only draws:
        /// the caller hides its rows in <paramref name="onToggle"/>, which gets the new state. Without
        /// the library's sheet it is a plain heading (Fold null).
        /// </summary>
        /// <param name="y">Where the name stands, as for <see cref="AddHeading(UIComponent,string,float)"/>;
        /// the clickable strip starts 4 above it and is <see cref="Theme.HeadingHeight"/> tall.</param>
        public HeadingRow AddFoldingHeading(UIComponent parent, string text, float y, bool open, Action<bool> onToggle)
        {
            UITextureAtlas ours = UI.Atlas.Ours;
            if (ours == null || S.FoldOpen == null || ours[S.FoldOpen] == null || ours[S.FoldShut] == null)
                return AddHeading(parent, text, y);

            HeadingRow row = new HeadingRow();

            // First, so it draws under the name and the arrow; they let the clicks through to it.
            UIButton fold = row.Add(parent.AddUIComponent<UIButton>());
            fold.atlas = ours;
            fold.size = new Vector2(parent.width, Theme.HeadingHeight);
            fold.relativePosition = new Vector3(0f, y - 4f);
            fold.canFocus = false;
            string hover = S.OwnSheet ? S.ButtonHover : "st-button-hover";
            if (ours[hover] != null)
            {
                fold.hoveredBgSprite = hover;
                fold.pressedBgSprite = hover;
            }
            row.Fold = fold;

            UISprite arrow = row.Add(parent.AddUIComponent<UISprite>());
            arrow.atlas = ours;
            arrow.color = Theme.Heading;
            arrow.isInteractive = false;
            row.Arrow = arrow;
            row.ArrowY = y;
            row.OpenSprite = S.FoldOpen;
            row.ShutSprite = S.FoldShut;
            row.SetOpen(open);

            AddHeading(parent, text, y, FoldIndent, row);
            row.Label.isInteractive = false;
            if (row.Rule != null)
                row.Rule.isInteractive = false;

            fold.eventClick += (component, e) =>
            {
                row.SetOpen(!row.Open);
                if (onToggle != null)
                    onToggle(row.Open);
            };

            return row;
        }

        /// <summary>How far right of the arrow's left edge a folding heading's name starts.</summary>
        private const float FoldIndent = 16f;

        private HeadingRow AddHeading(UIComponent parent, string text, float y, float x0, HeadingRow row)
        {
            string shown = Theme.HeadingCaps && text != null ? text.ToUpperInvariant() : text;

            UILabel heading = row.Add(AddLabel(parent, shown, new Vector3(x0, y), Theme.HeadingScale));
            heading.autoSize = false;
            heading.size = new Vector2(parent.width - x0, 20f);
            if (Theme.HeadingCaps)
                heading.characterSpacing = 1;
            Paint(heading, Theme.Heading);
            row.Label = heading;

            if (Theme.HeadingRule && Theme.HeadingLineUnder && S.Rule != null)
            {
                UISprite under = row.Add(parent.AddUIComponent<UISprite>());
                under.atlas = Atlas;
                under.spriteName = S.Rule;
                under.size = new Vector2(parent.width, 1f);
                under.relativePosition = new Vector3(0f, y + 22f);
                row.Rule = under;
            }
            else if (Theme.HeadingRule && S.Rule != null)
            {
                // Measured with the spacing the caps get: one unit per character.
                float textWidth = Measure(shown, Theme.HeadingScale) + (Theme.HeadingCaps ? shown.Length : 0);
                float x = x0 + Mathf.Ceil(textWidth) + 10f;
                if (x < parent.width - 10f)
                {
                    UISprite rule = row.Add(parent.AddUIComponent<UISprite>());
                    rule.atlas = Atlas;
                    rule.spriteName = S.Rule;
                    rule.size = new Vector2(parent.width - x, 1f);
                    rule.relativePosition = new Vector3(x, y + 9f);
                    row.Rule = rule;
                }
            }

            return row;
        }

        // ------------------------------------------------------------------ sliders and values

        /// <summary>A row: name on the left, slider in the middle, live value on the right.</summary>
        public SliderRow AddSlider(UIComponent parent, float y, string label, float min, float max,
            float step, float value, Func<float, string> format, Action<float> onChanged)
        {
            // Wide enough for the longest of each in Volumetric Weather: "Clouds dim at full
            // overcast to" on the left, "400% (every 0.8 s)" on the right. Both used to run into
            // the slider.
            SliderRow row = new SliderRow { Format = format, ShowValue = ShowValue };
            float labelWidth = LabelWidth;
            float valueWidth = Theme.ValueWidth;
            float sliderWidth = Right(parent) - labelWidth - valueWidth - 3f * Theme.Gap;

            row.Name = row.Add(RowName(parent, label, y, labelWidth));

            UISlider slider = row.Add(parent.AddUIComponent<UISlider>());
            slider.size = new Vector2(sliderWidth, 18f);
            slider.relativePosition = new Vector3(labelWidth + Theme.Gap, y + 8f);
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = step;
            slider.scrollWheelAmount = step;
            DressSlider(slider, sliderWidth);
            row.Slider = slider;

            UILabel readout = row.Add(AddLabel(parent, format(value),
                new Vector3(Right(parent) - valueWidth, y + 8f), Theme.BoxedValues ? 0.75f : Theme.TextScale));
            readout.autoSize = false;
            readout.size = new Vector2(valueWidth, 20f);
            readout.textAlignment = UIHorizontalAlignment.Right;
            Paint(readout, Theme.Value);
            if (Theme.BoxedValues && S.Value != null)
            {
                readout.atlas = Atlas;
                readout.backgroundSprite = S.Value;
                readout.textAlignment = UIHorizontalAlignment.Center;
                readout.verticalAlignment = UIVerticalAlignment.Middle;
                readout.padding = new RectOffset(7, 7, 2, 0);
            }
            row.Value = readout;
            ShowValue(readout, format(value));
            AddRowLine(parent, y, row);

            // After min/max, or the component clamps it against the default 0..100 range.
            slider.value = Mathf.Clamp(value, min, max);

            slider.eventValueChanged += (component, v) =>
            {
                ShowValue(readout, format(v));
                if (!row.Quiet)
                    onChanged(v);
            };

            return row;
        }

        /// <summary>
        /// A readout's text; a boxed one is also sized to it (at least 58 wide), its right edge
        /// where the column ends.
        /// </summary>
        private void ShowValue(UILabel readout, string text)
        {
            readout.text = text;
            if (!Theme.BoxedValues || S.Value == null)
                return;

            float right = readout.relativePosition.x + readout.width;
            float width = Mathf.Min(Theme.ValueWidth, Mathf.Max(58f, Mathf.Ceil(Measure(text, readout.textScale)) + 14f));
            readout.width = width;
            readout.relativePosition = new Vector3(right - width, readout.relativePosition.y);
        }

        /// <summary>The track, the fill (new skins) and the knob of a slider 18 high.</summary>
        private void DressSlider(UISlider slider, float width)
        {
            float track = Theme.TrackHeight;
            int top = Mathf.RoundToInt((18f - track) * 0.5f);

            UISlicedSprite trackSprite = slider.AddUIComponent<UISlicedSprite>();
            trackSprite.atlas = Atlas;
            trackSprite.spriteName = S.Track;
            trackSprite.size = new Vector2(width, track);
            trackSprite.relativePosition = new Vector3(0f, Vanilla ? 4f : top);

            if (S.Fill != null)
            {
                // UISlider sizes its fill indicator itself (UpdateValueIndicators): at fillPadding's
                // left/top, (width - horizontal) * the value's share wide, height - vertical high.
                UISlicedSprite fill = slider.AddUIComponent<UISlicedSprite>();
                fill.atlas = Atlas;
                fill.spriteName = S.Fill;
                fill.size = new Vector2(0f, track);
                slider.fillPadding = new RectOffset(0, 0, top, 18 - top - Mathf.RoundToInt(track));
                slider.fillMode = UIFillMode.Stretch;
                slider.fillIndicatorObject = fill;
            }

            UISprite thumb = slider.AddUIComponent<UISprite>();
            thumb.atlas = Atlas;
            thumb.spriteName = S.Thumb;
            thumb.size = new Vector2(Theme.ThumbSize, Theme.ThumbSize);
            slider.thumbObject = thumb;
        }

        private UILabel RowName(UIComponent parent, string label, float y, float width)
        {
            return AddName(parent, label, new Vector3(0f, y + 8f), width);
        }

        /// <summary>
        /// A row's name as the skin writes it (capitals in Frosted), <paramref name="width"/> wide,
        /// for rows the caller builds itself.
        /// </summary>
        public UILabel AddName(UIComponent parent, string label, Vector3 position, float width)
        {
            UILabel name = AddLabel(parent, Caps(label), position, Theme.TextScale);
            name.autoSize = false;
            name.size = new Vector2(width, 20f);
            Spaced(name);
            FitName(name, width);
            return name;
        }

        /// <summary>
        /// How wide a row's name is in this skin (capitals and their spacing included), in UI
        /// units: what a panel measures to choose its <see cref="LabelWidth"/>.
        /// </summary>
        public float NameWidth(string label)
        {
            string shown = Caps(label) ?? string.Empty;
            return Mathf.Ceil(Measure(shown, Theme.TextScale) + (Theme.Caps ? shown.Length : 0));
        }

        /// <summary>
        /// A name wider than its column is drawn smaller until it fits (down to 75% of the row's
        /// text size), never cut off: a fixed-size label clips what runs past its edge.
        /// </summary>
        private void FitName(UILabel name, float width)
        {
            float wide = NameWidth(name.text);
            if (wide <= width || wide <= 0f)
                return;

            float share = (width - 2f) / wide;
            if (share >= 0.75f)
            {
                name.textScale = Theme.TextScale * share;
                return;
            }

            // Too long even at 75%: two lines, centred on the row, a little smaller.
            name.textScale = Theme.TextScale * 0.85f;
            name.wordWrap = true;
            name.verticalAlignment = UIVerticalAlignment.Middle;
            name.relativePosition += new Vector3(0f, -7f);
            name.size = new Vector2(width, 32f);
        }

        // ------------------------------------------------------------------ on/off

        /// <summary>
        /// An on/off row: the game's tick box left of the name (Vanilla), or the name with a switch
        /// or tick box lined up with the sliders. The whole row is the click target.
        /// </summary>
        public ToggleRow AddToggle(UIComponent parent, float y, string label, bool value, Action<bool> onChanged)
        {
            ToggleRow row = new ToggleRow();

            UICheckBox box = row.Add(parent.AddUIComponent<UICheckBox>());
            box.size = new Vector2(Right(parent), 22f);
            box.relativePosition = new Vector3(0f, y + 6f);

            Vector2 markSize = Vanilla ? new Vector2(16f, 16f) : SpriteSize(S.ToggleOff, new Vector2(16f, 16f));
            Vector3 markAt = Theme.Toggle == ToggleShape.TickLeft
                ? new Vector3(0f, Vanilla ? 2f : Mathf.Round((22f - markSize.y) * 0.5f))
                : new Vector3(LabelWidth + Theme.Gap, Mathf.Round((22f - markSize.y) * 0.5f));

            UISprite background = box.AddUIComponent<UISprite>();
            background.atlas = Atlas;
            background.spriteName = S.ToggleOff;
            background.size = markSize;
            background.relativePosition = markAt;

            UISprite check = background.AddUIComponent<UISprite>();
            check.atlas = Atlas;
            check.spriteName = S.ToggleOn;
            check.size = markSize;
            check.relativePosition = Vector3.zero;
            box.checkedBoxObject = check;

            box.label = box.AddUIComponent<UILabel>();
            box.label.text = Caps(label);
            box.label.textScale = Theme.TextScale;
            Spaced(box.label);
            Paint(box.label, Theme.Text);
            if (Theme.Toggle == ToggleShape.TickLeft)
            {
                box.label.relativePosition = new Vector3(Vanilla ? 24f : markSize.x + 10f, 3f);
            }
            else
            {
                box.label.autoSize = false;
                box.label.size = new Vector2(LabelWidth, 20f);
                box.label.relativePosition = new Vector3(0f, 2f);
                FitName(box.label, LabelWidth);
            }
            row.Check = box;
            row.Name = box.label;

            box.isChecked = value;
            box.eventCheckChanged += (component, isChecked) =>
            {
                if (!row.Quiet)
                    onChanged(isChecked);
            };

            AddRowLine(parent, y, row);
            return row;
        }

        private Vector2 SpriteSize(string sprite, Vector2 fallback)
        {
            UITextureAtlas.SpriteInfo info = Atlas != null && sprite != null ? Atlas[sprite] : null;
            return info != null && info.texture != null ? info.pixelSize : fallback;
        }

        // ------------------------------------------------------------------ choices

        /// <summary>
        /// A choice row. Vanilla: a slider over the choices with the choice's name as its value.
        /// New skins: a row of buttons, or a dropdown when the names don't fit beside the row's
        /// name (measured, so it follows the language). Whatever the shape, onChanged gets the
        /// INDEX of the picked choice; what that stores is the caller's.
        /// </summary>
        public ChoiceRow AddChoice(UIComponent parent, float y, string label, string[] choices, int index,
            Action<int> onChanged)
        {
            ChoiceRow row = new ChoiceRow();
            int count = choices != null ? choices.Length : 0;
            index = count > 0 ? Mathf.Clamp(index, 0, count - 1) : 0;
            row.Index = index;

            if (Theme.Choice == ChoiceShape.Slider || count == 0)
            {
                // A slider over the choices, with the choice's name as the readout. Needs no
                // sprite beyond the slider's, and three or four positions drag perfectly well.
                SliderRow slider = AddSlider(parent, y, label, 0f, Mathf.Max(1, count - 1), 1f, index,
                    v => ChoiceName(choices, v),
                    v =>
                    {
                        int picked = Mathf.Clamp(Mathf.RoundToInt(v), 0, Mathf.Max(0, count - 1));
                        row.Index = picked;
                        if (!row.Quiet)
                            onChanged(picked);
                    });
                row.Name = slider.Name;
                row.Slider = slider.Slider;
                row.Value = slider.Value;
                row.Parts.AddRange(slider.Parts);
                row.Show = i => slider.SetValue(i);
                return row;
            }

            row.Name = row.Add(RowName(parent, label, y, LabelWidth));
            AddRowLine(parent, y, row);

            float x = LabelWidth + Theme.Gap;
            float available = Right(parent) - x;
            const float scale = 0.75f;

            float[] textWidths = new float[count];
            for (int i = 0; i < count; i++)
                textWidths[i] = Measure(Caps(choices[i]), scale) + (Theme.Caps ? choices[i].Length : 0);

            bool fits = ChoiceFit.Fits(textWidths, available);
            LibLog.Msg("choice '" + label + "': " + Widths(textWidths) + " of " + available.ToString("F0") +
                       " -> " + (fits ? "buttons" : "dropdown"));

            if (!fits)
            {
                string missing;
                UIDropDown drop = AddDropDown(parent, new Vector2(Mathf.Min(available, 260f), 26f),
                    new Vector3(x, y + 4f), count, out missing);
                if (drop != null)
                {
                    row.Add(drop);
                    drop.items = choices;
                    drop.selectedIndex = index;
                    drop.eventSelectedIndexChanged += (component, picked) =>
                    {
                        row.Index = picked;
                        if (!row.Quiet && picked >= 0)
                            onChanged(picked);
                    };
                    row.Drop = drop;
                    row.Show = i => drop.selectedIndex = i;
                    return row;
                }

                LibLog.Msg("choice '" + label + "': no dropdown (sprite '" + missing + "' missing), buttons anyway");
            }

            float[] widths = ChoiceFit.SegmentWidths(textWidths);

            UIPanel strip = row.Add(parent.AddUIComponent<UIPanel>());
            strip.atlas = Atlas;
            strip.backgroundSprite = S.SegStrip;
            strip.size = new Vector2(Mathf.Min(available, ChoiceFit.StripWidth(widths)), 26f);
            strip.relativePosition = new Vector3(x, y + 4f);
            row.Strip = strip;

            float at = ChoiceFit.StripPadding;
            for (int i = 0; i < count; i++)
            {
                UIButton segment = strip.AddUIComponent<UIButton>();
                segment.atlas = Atlas;
                segment.text = Caps(choices[i]);
                segment.textScale = scale;
                Spaced(segment);
                segment.size = new Vector2(widths[i], 26f - 2f * ChoiceFit.StripPadding);
                segment.relativePosition = new Vector3(at, ChoiceFit.StripPadding);
                segment.textVerticalAlignment = UIVerticalAlignment.Middle;
                Paint(segment, Theme.SegText);
                at += widths[i] + ChoiceFit.Gap;

                int captured = i;
                segment.eventClick += (component, e) =>
                {
                    if (row.Index == captured)
                        return;

                    row.Index = captured;
                    PaintSegments(row);
                    if (!row.Quiet)
                        onChanged(captured);
                };
                row.Segments.Add(segment);
            }

            row.Show = i => PaintSegments(row);
            PaintSegments(row);
            return row;
        }

        private void PaintSegments(ChoiceRow row)
        {
            for (int i = 0; i < row.Segments.Count; i++)
            {
                UIButton segment = row.Segments[i];
                bool picked = i == row.Index;
                string normal = picked ? S.SegSelected : string.Empty;

                segment.normalBgSprite = normal;
                segment.focusedBgSprite = normal;   // a clicked button keeps the focus
                segment.hoveredBgSprite = picked ? S.SegSelected : S.SegHover;
                segment.pressedBgSprite = S.SegSelected;
                segment.disabledBgSprite = normal;

                Color32 text = picked ? Theme.SegSelectedText : Theme.SegText;
                segment.textColor = text;
                segment.focusedTextColor = text;
                segment.hoveredTextColor = picked ? Theme.SegSelectedText : Theme.TabHoverText;
                segment.pressedTextColor = Theme.SegSelectedText;
            }
        }

        private static string ChoiceName(string[] choices, float v)
        {
            int index = Mathf.RoundToInt(v);
            return choices != null && index >= 0 && index < choices.Length ? choices[index] : string.Empty;
        }

        private static string Widths(float[] widths)
        {
            string text = string.Empty;
            for (int i = 0; i < widths.Length; i++)
                text += (i == 0 ? "" : "+") + widths[i].ToString("F0");
            return text;
        }

        // ------------------------------------------------------------------ buttons and fields

        public UIButton AddButton(UIComponent parent, string text, Vector2 size, Vector3 position)
        {
            UIButton button = parent.AddUIComponent<UIButton>();
            button.atlas = Atlas;
            button.text = Caps(text);
            button.textScale = Theme.TextScale;
            Spaced(button);
            button.size = size;
            button.relativePosition = position;
            SetAccent(button, false);
            return button;
        }

        /// <summary>
        /// A row's reset button: the library's own round-arrow icon, no text, at the right end of
        /// the row (keep <see cref="ResetColumn"/> free for it). Its sprite always comes from the
        /// library's sheet, Vanilla too; null when the sheet is missing (the row works without
        /// it). The caller wires the click and enables it while the row is off its default
        /// (<see cref="SetResetEnabled"/>).
        /// </summary>
        public UIButton AddReset(UIComponent parent, float y, string tooltip)
        {
            UITextureAtlas ours = UI.Atlas.Ours;
            if (ours == null || ours[ResetSprite] == null)
                return null;

            const float size = 22f;
            UIButton button = parent.AddUIComponent<UIButton>();
            button.atlas = ours;
            button.size = new Vector2(size, size);
            button.relativePosition = new Vector3(parent.width - size, y + Mathf.Round((Theme.RowHeight - size) * 0.5f));
            button.canFocus = false;
            button.tooltip = tooltip;

            // A soft square behind the icon on hover, from the skin's own buttons where it has them.
            string hover = S.OwnSheet ? S.ButtonHover : "st-button-hover";
            if (ours[hover] != null)
            {
                button.hoveredBgSprite = hover;
                button.pressedBgSprite = hover;
            }

            Vector2 glyph = new Vector2(14f, 14f);
            UITextureAtlas.SpriteInfo info = ours[ResetSprite];
            if (info.texture != null)
                glyph = info.pixelSize;

            UISprite icon = button.AddUIComponent<UISprite>();
            icon.atlas = ours;
            icon.spriteName = ResetSprite;
            icon.size = glyph;
            icon.relativePosition = new Vector3(Mathf.Round((size - glyph.x) * 0.5f), Mathf.Round((size - glyph.y) * 0.5f));
            icon.isInteractive = false;

            return button;
        }

        /// <summary>A reset button lit (the row is off its default) or faded and unclickable.</summary>
        public static void SetResetEnabled(UIButton reset, bool enabled)
        {
            if (reset == null || reset.isEnabled == enabled && reset.opacity == (enabled ? 1f : 0.3f))
                return;

            reset.isEnabled = enabled;
            reset.opacity = enabled ? 1f : 0.3f;
        }

        /// <summary>The library's reset icon, shared by every skin.</summary>
        public const string ResetSprite = "reset";

        /// <summary>
        /// The accent look (Save while there are unsaved changes) or the plain one. Vanilla has
        /// no accent: its buttons all look alike.
        /// </summary>
        public void SetAccent(UIButton button, bool accent)
        {
            bool on = accent && S.Accent != null;
            button.normalBgSprite = on ? S.Accent : S.Button;
            button.hoveredBgSprite = on ? S.AccentHover : S.ButtonHover;
            button.pressedBgSprite = on ? S.Accent : S.ButtonPressed;
            button.focusedBgSprite = on ? S.Accent : S.Button;
            button.disabledBgSprite = on ? S.Accent : S.ButtonDisabled;

            if (Vanilla)
                return;   // the game's button text colours, untouched, as before

            Color32 text = on ? Theme.AccentText : Theme.ButtonText;
            Paint(button, text);
            button.hoveredTextColor = text;
            button.pressedTextColor = text;
            button.focusedTextColor = text;
        }

        /// <summary>
        /// A one-line text field. Ctrl+C and Ctrl+V work in it natively (UITextField.OnKeyDown).
        /// Enter submits and Escape cancels, each after ending its focus (UITextField.OnSubmit /
        /// OnCancel); a click elsewhere submits.
        /// </summary>
        public UITextField AddTextField(UIComponent parent, Vector2 size, Vector3 position, int maxLength)
        {
            UITextField field = parent.AddUIComponent<UITextField>();
            field.atlas = Atlas;
            field.size = size;
            field.relativePosition = position;
            field.normalBgSprite = S.Field;
            field.hoveredBgSprite = S.FieldHover;
            field.focusedBgSprite = S.FieldFocus;
            field.disabledBgSprite = S.Field;
            field.selectionSprite = S.Selection;
            field.selectionBackgroundColor = Theme.Selection;
            field.color = new Color32(255, 255, 255, 255);
            field.textColor = Theme.FieldText;
            field.textScale = 0.85f;
            field.padding = new RectOffset(6, 6, 6, 3);
            field.maxLength = maxLength;
            field.builtinKeyNavigation = true;
            field.isInteractive = true;
            field.readOnly = false;
            field.canFocus = true;
            field.selectOnFocus = true;
            field.submitOnFocusLost = true;
            return field;
        }

        /// <summary>
        /// A dropdown built the way Render It! builds its own (RenderIt.UIUtils.CreateDropDown, read
        /// from its IL): a child button over its whole face as the trigger -- a UIDropDown opens
        /// its list only from its triggerButton's click (UIDropDown.OnTriggerClick) and has no
        /// trigger of its own. Null, with the sprite that is missing, when the atlas lacks one.
        /// </summary>
        public UIDropDown AddDropDown(UIComponent parent, Vector2 size, Vector3 position, int visibleItems,
            out string missing)
        {
            missing = null;
            string[] sprites = { S.Drop, S.DropHover, S.DropFocus, S.DropDisabled, S.List, S.ListHover, S.ListHighlight };
            foreach (string sprite in sprites)
            {
                if (Atlas == null || sprite == null || Atlas[sprite] == null)
                {
                    missing = sprite ?? "(none)";
                    return null;
                }
            }

            const int itemHeight = 24;
            const int padding = 5;

            UIDropDown drop = parent.AddUIComponent<UIDropDown>();
            drop.atlas = Atlas;
            drop.size = size;
            drop.relativePosition = position;
            drop.textScale = Theme.TextScale;
            drop.normalBgSprite = S.Drop;
            drop.hoveredBgSprite = S.DropHover;
            drop.focusedBgSprite = S.DropFocus;
            drop.disabledBgSprite = S.DropDisabled;
            drop.foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            drop.horizontalAlignment = Vanilla ? UIHorizontalAlignment.Center : UIHorizontalAlignment.Left;
            drop.verticalAlignment = UIVerticalAlignment.Middle;
            drop.listBackground = S.List;
            drop.itemHover = S.ListHover;
            drop.itemHighlight = S.ListHighlight;
            drop.itemHeight = itemHeight;
            drop.listHeight = visibleItems * itemHeight + 2 * padding;
            drop.listWidth = (int)size.x;
            drop.listPosition = UIDropDown.PopupListPosition.Automatic;
            drop.itemPadding = new RectOffset(padding, padding, padding, padding);
            drop.listPadding = new RectOffset(padding, padding, padding, padding);
            drop.textFieldPadding = new RectOffset(10, Vanilla ? 5 : 24, (int)Mathf.Max(4f, (size.y - 14f) * 0.5f), 5);
            drop.popupColor = new Color32(255, 255, 255, 255);
            drop.popupTextColor = Theme.ListText;
            Paint(drop, Theme.DropText);

            // Not Render It!'s: a dropdown keeps the focus after a pick, and with key navigation
            // the arrow keys -- which also pan the camera; UIView.HasInputFocus is true only for
            // a text field -- would step through the list. Nor may a wheel turned over the panel
            // change a setting. The mouse does everything.
            drop.builtinKeyNavigation = false;
            drop.enableMouseWheel = false;

            if (S.DropArrow != null)
            {
                Vector2 arrowSize = SpriteSize(S.DropArrow, new Vector2(10f, 6f));
                UISprite arrow = drop.AddUIComponent<UISprite>();
                arrow.atlas = Atlas;
                arrow.spriteName = S.DropArrow;
                arrow.size = arrowSize;
                arrow.relativePosition = new Vector3(size.x - 12f - arrowSize.x, Mathf.Round((size.y - arrowSize.y) * 0.5f));
                arrow.isInteractive = false;
            }

            UIButton trigger = drop.AddUIComponent<UIButton>();
            trigger.size = size;
            trigger.relativePosition = Vector3.zero;
            drop.triggerButton = trigger;

            return drop;
        }

        // ------------------------------------------------------------------ tabs

        public UIButton AddTab(UIComponent parent, string text, Vector2 size, Vector3 position)
        {
            UIButton tab = parent.AddUIComponent<UIButton>();
            tab.atlas = Atlas;
            tab.text = Caps(text);
            tab.textScale = Theme.TabScale;
            Spaced(tab);
            tab.size = size;
            tab.relativePosition = position;
            SetTabSelected(tab, false);
            return tab;
        }

        /// <summary>
        /// Tabs here are plain buttons rather than a UITabstrip, so the selected state is
        /// expressed by swapping which sprite counts as "normal".
        /// </summary>
        public void SetTabSelected(UIButton tab, bool selected)
        {
            PaintTab(tab, selected);

            // An icon tab (AddTabStrip with icons): its icon in the colour its name would have.
            UISprite icon = tab.objectUserData as UISprite;
            if (icon != null)
                icon.color = selected ? Theme.TabSelectedText : Theme.TabText;
        }

        private void PaintTab(UIButton tab, bool selected)
        {
            switch (Theme.Tabs)
            {
                case TabShape.GameTabs:
                    tab.normalBgSprite = selected ? S.TabSelected : S.Tab;
                    tab.focusedBgSprite = tab.normalBgSprite;
                    tab.hoveredBgSprite = selected ? S.TabSelected : S.TabHover;
                    tab.pressedBgSprite = S.TabPressed;
                    tab.textColor = selected ? Theme.TabSelectedText : Theme.TabText;
                    return;

                default:
                    string normal = selected ? S.TabSelected : (S.Tab ?? string.Empty);
                    tab.normalBgSprite = normal;
                    tab.focusedBgSprite = normal;
                    tab.hoveredBgSprite = selected ? S.TabSelected : (S.TabHover ?? string.Empty);
                    tab.pressedBgSprite = S.TabPressed ?? normal;
                    Color32 text = selected ? Theme.TabSelectedText : Theme.TabText;
                    Paint(tab, text);
                    tab.focusedTextColor = text;
                    tab.hoveredTextColor = selected ? Theme.TabSelectedText : Theme.TabHoverText;
                    tab.pressedTextColor = Theme.TabSelectedText;
                    return;
            }
        }

        /// <summary>
        /// A row of equal tabs across <paramref name="width"/>, with what the skin draws behind
        /// them: a line under them (See-through), a dark strip they sit in (Game blue), nothing
        /// (Vanilla). <paramref name="onSelect"/> gets the clicked tab's index; the strip marks it.
        /// </summary>
        public TabStrip AddTabStrip(UIComponent parent, Vector3 position, float width, float height,
            IList<string> names, Action<int> onSelect)
        {
            return AddTabStrip(parent, position, width, height, names, onSelect, null);
        }

        /// <summary>The width of an icon tab (the new skins, with icons).</summary>
        public const float IconTabWidth = 48f;

        /// <summary>
        /// The same, each tab drawn as an icon in the skins with the library's sheet: "icon-" +
        /// <paramref name="icons"/>[i] (Lucide names; THIRD-PARTY-NOTICES.md), its name the tooltip,
        /// the tabs <see cref="IconTabWidth"/> wide from the left. Vanilla keeps its named tabs, and so
        /// does any skin whose sheet lacks one of the icons. A null entry or list: names.
        /// </summary>
        public TabStrip AddTabStrip(UIComponent parent, Vector3 position, float width, float height,
            IList<string> names, Action<int> onSelect, IList<string> icons)
        {
            TabStrip strip = new TabStrip { Paint = SetTabSelected };
            bool useIcons = HasIcons(icons, names.Count);
            float inset = 0f, gap = 0f;

            if (Theme.Tabs == TabShape.Pills && S.TabStrip != null)
            {
                UISlicedSprite background = parent.AddUIComponent<UISlicedSprite>();
                background.atlas = Atlas;
                background.spriteName = S.TabStrip;
                background.size = new Vector2(width, height);
                background.relativePosition = position;
                strip.Background = background;
                inset = 3f;
                gap = 3f;
            }
            else if (Theme.Tabs == TabShape.Blocks)
            {
                gap = 4f;
                if (S.Band != null)
                {
                    // The darker band the tabs stand on: from under them to the panel's bottom
                    // margin, edge to edge, and growing with the panel (a page shown or hidden).
                    float top = position.y + height;
                    UISlicedSprite band = parent.AddUIComponent<UISlicedSprite>();
                    band.atlas = Atlas;
                    band.spriteName = S.Band;
                    band.isInteractive = false;
                    band.relativePosition = new Vector3(0f, top);
                    band.size = new Vector2(parent.width, Mathf.Max(1f, parent.height - top - Theme.Margin));
                    band.anchor = UIAnchorStyle.All;

                    // Behind whatever the page already has, in front of the frame's tint.
                    if (_tint != null && _tint.parent == parent)
                        band.zOrder = _tint.zOrder + 1;

                    strip.Background = band;
                }
            }
            else if (Theme.Tabs == TabShape.Underline && S.Rule != null)
            {
                UISprite line = parent.AddUIComponent<UISprite>();
                line.atlas = Atlas;
                line.spriteName = S.Rule;
                line.size = new Vector2(width, 1f);
                line.relativePosition = position + new Vector3(0f, height - 1f);
                strip.Background = line;
            }

            int count = names.Count;
            float tabWidth = count > 0 ? (width - 2f * inset - gap * (count - 1)) / count : 0f;
            if (useIcons)
                tabWidth = Mathf.Min(tabWidth, IconTabWidth);

            for (int i = 0; i < count; i++)
            {
                UIButton tab = AddTab(parent, useIcons ? string.Empty : names[i], new Vector2(tabWidth, height - 2f * inset),
                    position + new Vector3(inset + i * (tabWidth + gap), inset));
                if (useIcons)
                    AddTabIcon(tab, icons[i], names[i]);

                int captured = i;
                tab.eventClick += (component, e) =>
                {
                    strip.Select(captured);
                    if (onSelect != null)
                        onSelect(captured);
                };
                strip.Tabs.Add(tab);
            }

            if (icons != null)
                LibLog.Msg("tabs: " + (useIcons ? "drawn as icons" : "drawn with their names") + " (" + Skin + ")");

            return strip;
        }

        private bool HasIcons(IList<string> icons, int count)
        {
            if (icons == null || icons.Count < count || Vanilla || !S.OwnSheet || Atlas == null)
                return false;

            for (int i = 0; i < count; i++)
            {
                if (icons[i] == null || Atlas["icon-" + icons[i]] == null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// The icon in the middle of a tab, lit on hover like a name would be. SetTabSelected finds it
        /// through objectUserData.
        /// </summary>
        private void AddTabIcon(UIButton tab, string icon, string name)
        {
            tab.tooltip = name;

            UISprite sprite = tab.AddUIComponent<UISprite>();
            sprite.atlas = Atlas;
            sprite.spriteName = "icon-" + icon;
            Vector2 size = SpriteSize(sprite.spriteName, new Vector2(18f, 18f));
            sprite.size = size;
            sprite.relativePosition = new Vector3(Mathf.Round((tab.width - size.x) * 0.5f), Mathf.Round((tab.height - size.y) * 0.5f));
            sprite.isInteractive = false;
            tab.objectUserData = sprite;

            tab.eventMouseEnter += (component, e) => sprite.color = Theme.TabHoverText;
            tab.eventMouseLeave += (component, e) =>
            {
                // Back to whatever SetTabSelected last gave it: the tab's normal sprite says which.
                sprite.color = tab.normalBgSprite == S.TabSelected ? Theme.TabSelectedText : Theme.TabText;
            };
            sprite.color = Theme.TabText;
        }

        /// <summary>
        /// A second, dimmer title after the panel's own -- the open tab's name when the tabs are icons
        /// (the new skins only: Vanilla stays as it was). Null or empty hides it.
        /// </summary>
        public void SetSubtitle(Frame frame, string text)
        {
            if (Vanilla || frame == null)
                return;

            if (frame.Subtitle == null)
            {
                UILabel sub = frame.Panel.AddUIComponent<UILabel>();
                sub.textScale = Theme.TitleScale;
                sub.autoSize = false;
                sub.isInteractive = false;
                sub.verticalAlignment = UIVerticalAlignment.Middle;
                Spaced(sub);
                Paint(sub, Theme.TabText);
                frame.Subtitle = sub;
            }

            frame.Subtitle.text = string.IsNullOrEmpty(text) ? string.Empty : "·  " + Caps(text);
            frame.Layout(frame.Panel.width);
        }

        /// <summary>The title and the subtitle side by side, where the title alone would stand.</summary>
        private void PlaceSubtitle(Frame frame, float width)
        {
            UILabel title = frame.Title, sub = frame.Subtitle;
            if (sub == null)
                return;

            float bar = Theme.TitleHeight;
            float titleWidth = Mathf.Ceil(Measure(title.text, Theme.TitleScale) + title.characterSpacing * title.text.Length) + 12f;
            float subWidth = Mathf.Ceil(Measure(sub.text, Theme.TitleScale) + sub.characterSpacing * sub.text.Length) + 4f;
            float x = Theme.TitleCentered ? Mathf.Round((width - titleWidth - subWidth) * 0.5f) : title.relativePosition.x;

            title.textAlignment = UIHorizontalAlignment.Left;
            title.size = new Vector2(titleWidth, bar);
            title.relativePosition = new Vector3(x, title.relativePosition.y);
            sub.size = new Vector2(subWidth, bar);
            sub.relativePosition = new Vector3(x + titleWidth, title.relativePosition.y);
        }

        // ------------------------------------------------------------------ the panel itself

        /// <summary>
        /// Dresses a panel: its background, the title bar (a strip in the new skins), the title,
        /// a close button and a drag handle over the title. Only the title bar drags the panel:
        /// whatever is under it is controls.
        /// </summary>
        public Frame AddFrame(UIPanel panel, string title, Action onClose)
        {
            Frame frame = new Frame { Panel = panel };
            panel.atlas = Atlas;

            if (Theme.Blur)
            {
                // A panel draws its own background before its children, so the blur and the
                // tint over it are its first two children instead, sized with it.
                panel.backgroundSprite = string.Empty;

                frame.Backdrop = panel.AddUIComponent<Backdrop>();
                frame.Backdrop.Radius = 14f;
                frame.Backdrop.relativePosition = Vector3.zero;

                frame.Tint = panel.AddUIComponent<UISlicedSprite>();
                frame.Tint.atlas = Atlas;
                frame.Tint.spriteName = S.Panel;
                frame.Tint.isInteractive = false;
                frame.Tint.relativePosition = Vector3.zero;
                _tint = frame.Tint;
            }
            else
            {
                panel.backgroundSprite = S.Panel;
            }

            if (Theme.TitleStrip && S.Title != null)
            {
                // Inside the panel's 1 px border.
                frame.Strip = panel.AddUIComponent<UISlicedSprite>();
                frame.Strip.atlas = Atlas;
                frame.Strip.spriteName = S.Title;
                frame.Strip.relativePosition = new Vector3(1f, 1f);
            }

            frame.Title = panel.AddUIComponent<UILabel>();
            frame.Title.text = Caps(title);
            frame.Title.textScale = Theme.TitleScale;
            frame.Title.autoSize = false;
            Spaced(frame.Title);
            Paint(frame.Title, Theme.Title);
            if (!Vanilla)
                frame.Title.verticalAlignment = UIVerticalAlignment.Middle;
            if (Theme.TitleCentered)
                frame.Title.textAlignment = UIHorizontalAlignment.Center;

            UIButton close = panel.AddUIComponent<UIButton>();
            close.atlas = Atlas;
            close.size = new Vector2(32f, 32f);
            close.normalBgSprite = S.Close ?? string.Empty;
            close.hoveredBgSprite = S.CloseHover;
            close.pressedBgSprite = S.ClosePressed;
            if (S.CloseGlyph != null)
            {
                Vector2 glyphSize = SpriteSize(S.CloseGlyph, new Vector2(12f, 12f));
                UISprite glyph = close.AddUIComponent<UISprite>();
                glyph.atlas = Atlas;
                glyph.spriteName = S.CloseGlyph;
                glyph.size = glyphSize;
                glyph.relativePosition = new Vector3(Mathf.Round((32f - glyphSize.x) * 0.5f), Mathf.Round((32f - glyphSize.y) * 0.5f));
                glyph.isInteractive = false;
            }
            close.eventClick += (component, e) =>
            {
                if (onClose != null)
                    onClose();
            };
            frame.Close = close;

            UIDragHandle drag = panel.AddUIComponent<UIDragHandle>();
            drag.target = panel;
            frame.Drag = drag;

            frame.Layout = width =>
            {
                float bar = Theme.TitleHeight;
                if (frame.Strip != null)
                    frame.Strip.size = new Vector2(width - 2f, bar - 1f);

                if (frame.Backdrop != null)
                {
                    // Anchored after sizing: from then on they follow the panel's height.
                    foreach (UIComponent layer in new UIComponent[] { frame.Backdrop, frame.Tint })
                    {
                        layer.anchor = UIAnchorStyle.None;
                        layer.size = new Vector2(width, panel.height);
                        layer.relativePosition = Vector3.zero;
                        layer.anchor = UIAnchorStyle.All;
                    }
                }

                if (Vanilla)
                {
                    frame.Title.size = new Vector2(width - bar, bar);
                    frame.Title.relativePosition = new Vector3(Theme.Margin, Theme.Margin);
                }
                else if (Theme.TitleCentered)
                {
                    // Centred on the panel: as much room on the left as the close button takes.
                    frame.Title.size = new Vector2(width - 2f * 40f, bar);
                    frame.Title.relativePosition = new Vector3(40f, 0f);
                }
                else
                {
                    frame.Title.size = new Vector2(width - 2f * Theme.Margin - 32f, bar);
                    frame.Title.relativePosition = new Vector3(Theme.Margin, 0f);
                }

                close.relativePosition = new Vector3(width - 36f, 4f);
                drag.size = new Vector2(width - 40f, bar);
                drag.relativePosition = Vector3.zero;

                PlaceSubtitle(frame, width);
            };
            frame.Layout(panel.width);

            return frame;
        }

        /// <summary>
        /// Keeps a top-level component on the screen (the game's UI is 1080 high at any
        /// resolution): a panel that grew downwards, or a button saved where a smaller screen
        /// has no room.
        /// </summary>
        public static void KeepOnScreen(UIComponent component)
        {
            UIView view = component.GetUIView();
            if (view == null)
                return;

            Vector3 at = component.relativePosition;
            float x = Mathf.Clamp(at.x, 0f, Mathf.Max(0f, Mathf.Floor(view.fixedWidth - component.width)));
            float y = Mathf.Clamp(at.y, 0f, Mathf.Max(0f, Mathf.Floor(view.fixedHeight - component.height)));
            if (x != at.x || y != at.y)
                component.relativePosition = new Vector3(x, y, at.z);
        }

        // ------------------------------------------------------------------ key binding

        /// <summary>
        /// A rebind row: click the button, press a combination, it is stored. Escape cancels,
        /// Backspace unbinds. Modelled on the game's own OptionsKeymappingPanel.
        /// <paramref name="pressText"/> is the button's text while it waits ("Press a key");
        /// <paramref name="onChanged"/> runs after every capture (the mod saves there).
        /// </summary>
        public KeyRow AddKeyBinding(UIComponent parent, float y, string label, SavedInputKey key,
            string pressText, Action onChanged)
        {
            KeyRow row = new KeyRow();
            row.Name = row.Add(RowName(parent, label, y, 200f));

            UIButton button = row.Add(AddButton(parent, key.ToLocalizedString("KEYNAME"),
                new Vector2(170f, 26f), new Vector3(Right(parent) - 170f, y + 4f)));
            row.Button = button;

            bool editing = false;

            button.eventMouseDown += (component, e) =>
            {
                if (editing)
                    return;

                e.Use();
                editing = true;
                button.buttonsMask = UIMouseButton.Left | UIMouseButton.Right | UIMouseButton.Middle;
                button.text = pressText;
                button.Focus();
                // Modal, so the key being captured doesn't also fire as a game hotkey --
                // including the mod's own toggle.
                UIView.PushModal(button);
            };

            button.eventKeyDown += (component, e) =>
            {
                if (!editing)
                    return;

                e.Use();

                if (e.keycode == KeyCode.Backspace)
                    key.value = SavedInputKey.Encode(KeyCode.None, false, false, false);
                else if (IsModifier(e.keycode))
                    return; // wait for a real key alongside the modifier
                else if (e.keycode != KeyCode.Escape)
                    key.value = SavedInputKey.Encode(e.keycode, e.control, e.shift, e.alt);

                editing = false;
                UIView.PopModal();
                button.buttonsMask = UIMouseButton.Left;
                button.text = key.ToLocalizedString("KEYNAME");
                if (onChanged != null)
                    onChanged();
            };

            AddRowLine(parent, y, row);
            return row;
        }

        private static bool IsModifier(KeyCode code)
        {
            return code == KeyCode.LeftControl || code == KeyCode.RightControl
                || code == KeyCode.LeftShift || code == KeyCode.RightShift
                || code == KeyCode.LeftAlt || code == KeyCode.RightAlt;
        }
    }
}
