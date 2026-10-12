using System;
using System.Collections.Generic;
using ColossalFramework.UI;

namespace ModsLibCS1.UI
{
    /// <summary>
    /// What a Builder row made. <see cref="Parts"/> is every component the row put on its parent,
    /// so a caller can grey the whole row (opacity 0.45 + isEnabled false) or hide it without
    /// digging siblings out of parent.components by index.
    /// </summary>
    public abstract class BuiltRow
    {
        public readonly List<UIComponent> Parts = new List<UIComponent>();

        /// <summary>The faint line under the row in skins that draw one; not in Parts (never greyed).</summary>
        public UISprite Line;

        /// <summary>Greys the row out (or back): every part 45% opaque and not clickable.</summary>
        public void SetEnabled(bool enabled)
        {
            foreach (UIComponent part in Parts)
            {
                part.isEnabled = enabled;
                part.opacity = enabled ? 1f : 0.45f;
            }
        }

        public void SetVisible(bool visible)
        {
            foreach (UIComponent part in Parts)
                part.isVisible = visible;

            if (Line != null)
                Line.isVisible = visible;
        }

        internal T Add<T>(T component) where T : UIComponent
        {
            if (component != null)
                Parts.Add(component);
            return component;
        }
    }

    public sealed class HeadingRow : BuiltRow
    {
        public UILabel Label;

        /// <summary>The line after the heading; null in skins without one.</summary>
        public UISprite Rule;

        /// <summary>A folding heading (Builder.AddFoldingHeading): the clickable strip and its arrow. Null on a plain one.</summary>
        public UIButton Fold;
        public UISprite Arrow;

        /// <summary>A folding heading's state: true while its rows show. Always true on a plain one.</summary>
        public bool Open = true;

        internal float ArrowY;
        internal string OpenSprite, ShutSprite;

        /// <summary>Turns the arrow (down open, right folded). Does not call the heading's onToggle.</summary>
        public void SetOpen(bool open)
        {
            Open = open;
            if (Arrow == null)
                return;

            // Both centred on the same point, level with the name's capitals -- where the name stands
            // now: the page may have moved the heading since it was built.
            float y = Label != null ? Label.relativePosition.y : ArrowY;
            Arrow.spriteName = open ? OpenSprite : ShutSprite;
            Arrow.size = open ? new UnityEngine.Vector2(10f, 6f) : new UnityEngine.Vector2(6f, 10f);
            Arrow.relativePosition = open ? new UnityEngine.Vector3(1f, y + 5f) : new UnityEngine.Vector3(3f, y + 3f);
        }
    }

    public sealed class SliderRow : BuiltRow
    {
        public UILabel Name;
        public UISlider Slider;
        public UILabel Value;

        internal Func<float, string> Format;
        internal Action<UILabel, string> ShowValue;
        internal bool Quiet;

        /// <summary>Moves the slider and its readout WITHOUT calling the row's onChanged.</summary>
        public void SetValue(float value)
        {
            Quiet = true;
            try
            {
                Slider.value = value;
            }
            finally
            {
                Quiet = false;
            }

            // Writing the same value raises no event: the readout is set either way.
            ShowValue(Value, Format(Slider.value));
        }
    }

    public sealed class ToggleRow : BuiltRow
    {
        public UICheckBox Check;

        /// <summary>The row's name (the check box's own label).</summary>
        public UILabel Name;

        internal bool Quiet;

        /// <summary>Sets the switch WITHOUT calling the row's onChanged.</summary>
        public void SetValue(bool value)
        {
            if (Check.isChecked == value)
                return;

            Quiet = true;
            try
            {
                Check.isChecked = value;
            }
            finally
            {
                Quiet = false;
            }
        }
    }

    /// <summary>
    /// A choice row, in whichever shape the skin and the names allowed: a row of buttons
    /// (<see cref="Segments"/>), a dropdown when they don't fit (<see cref="Drop"/>), or Vanilla's
    /// slider over the choices (<see cref="Slider"/> + <see cref="Value"/>). The others are null.
    /// </summary>
    public sealed class ChoiceRow : BuiltRow
    {
        public UILabel Name;
        public UIPanel Strip;
        public readonly List<UIButton> Segments = new List<UIButton>();
        public UIDropDown Drop;
        public UISlider Slider;
        public UILabel Value;

        public int Index { get; internal set; }

        internal Action<int> Show;
        internal bool Quiet;

        /// <summary>Shows the choice at <paramref name="index"/> WITHOUT calling the row's onChanged.</summary>
        public void SetIndex(int index)
        {
            Quiet = true;
            try
            {
                Index = index;
                Show(index);
            }
            finally
            {
                Quiet = false;
            }
        }
    }

    public sealed class KeyRow : BuiltRow
    {
        public UILabel Name;
        public UIButton Button;
    }

    /// <summary>The panel's own parts: background, title strip and text, close button, drag handle.</summary>
    public sealed class Frame
    {
        public UIPanel Panel;
        public UISlicedSprite Strip;

        /// <summary>Frosted: the blurred screen and the tint over it (the panel's own background is empty).</summary>
        public Backdrop Backdrop;
        public UISlicedSprite Tint;

        public UILabel Title;

        /// <summary>The dimmer title after it (Builder.SetSubtitle); null until one is set.</summary>
        public UILabel Subtitle;

        public UIButton Close;
        public UIDragHandle Drag;

        internal Action<float> Layout;

        /// <summary>Lays the title bar out again for a new panel width.</summary>
        public void Resize(float width)
        {
            Layout(width);
        }
    }

    /// <summary>A row of tabs with whatever the skin draws behind them.</summary>
    public sealed class TabStrip
    {
        public readonly List<UIButton> Tabs = new List<UIButton>();

        /// <summary>The strip behind the tabs, or the line under them; null for Vanilla.</summary>
        public UIComponent Background;

        public int Selected { get; private set; }

        internal Action<UIButton, bool> Paint;

        /// <summary>Marks a tab as the open one. Doesn't call onSelect.</summary>
        public void Select(int index)
        {
            Selected = index;
            for (int i = 0; i < Tabs.Count; i++)
                Paint(Tabs[i], i == index);
        }
    }
}
