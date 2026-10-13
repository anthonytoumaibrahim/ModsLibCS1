using ColossalFramework.UI;
using UnityEngine;

namespace ModsLibCS1.UI
{
    /// <summary>
    /// A fixed-height window onto pages taller than it (Builder.AddScrollView): a panel whose
    /// height stays put while its tabs differ, the rest scrolling. The pages are children of
    /// <see cref="Viewport"/>, which clips them; the one shown (<see cref="Show"/>) moves with the
    /// thin bar beside it (drag the thumb, click the track) or the mouse wheel anywhere over it.
    /// Each page keeps where it was scrolled to -- it is the page's own position -- so going back
    /// to a tab finds it as it was left.
    /// </summary>
    /// <remarks>
    /// Read from the game's IL: a click on whatever a clipping parent hides goes nowhere
    /// (UIInput.ClipCast -> IsInsideClippingRegion), so rows scrolled out of sight under a tab
    /// strip never take its clicks. The wheel climbs from the control under the mouse through its
    /// parents until one uses it (UIComponent.OnMouseWheelEvenIfInactive); a slider uses it unless
    /// told not to, so the sliders of a page shown here are (UISlider.enableMouseWheel): the wheel
    /// moves the page, never a setting the mouse happens to pass over.
    /// </remarks>
    public sealed class ScrollView
    {
        /// <summary>The window: clips the pages added to it (<see cref="AddPage"/>).</summary>
        public UIPanel Viewport;

        /// <summary>The bar at the side; null without the library's sheet (the wheel still scrolls).</summary>
        public UIScrollbar Bar;

        /// <summary>How far one notch of the wheel moves the page.</summary>
        public float WheelStep = 68f;

        private UIComponent _page;
        private float _height;
        private bool _setting;

        /// <summary>How far down the shown page is scrolled.</summary>
        public float Offset
        {
            get { return _page != null ? -_page.relativePosition.y : 0f; }
        }

        /// <summary>The most it can be scrolled: how much of the shown page the window can't hold.</summary>
        public float MaxOffset
        {
            get { return Mathf.Max(0f, _height - Viewport.height); }
        }

        /// <summary>A page in the window: as wide as it, at its top. Show it with <see cref="Show"/>.</summary>
        public UIPanel AddPage()
        {
            UIPanel page = Viewport.AddUIComponent<UIPanel>();
            page.size = new Vector2(Viewport.width, Viewport.height);
            page.relativePosition = Vector3.zero;
            return page;
        }

        /// <summary>
        /// The page that scrolls now (the caller shows it and hides the others) and how tall its
        /// content is. Where it was scrolled to is kept, as far as it still can be.
        /// </summary>
        public void Show(UIComponent page, float height)
        {
            if (page != _page)
            {
                _page = page;
                foreach (UISlider slider in page.GetComponentsInChildren<UISlider>(true))
                    slider.enableMouseWheel = false;
            }

            SetHeight(height);
        }

        /// <summary>The shown page's content grew or shrank (rows shown, hidden, folded): the bar follows.</summary>
        public void SetHeight(float height)
        {
            _height = height;
            if (_page == null)
                return;

            float offset = Mathf.Clamp(Offset, 0f, MaxOffset);
            if (Bar != null)
            {
                Bar.isVisible = MaxOffset > 0.5f;

                _setting = true;
                try
                {
                    Bar.maxValue = Mathf.Max(height, Viewport.height);
                    Bar.scrollSize = Viewport.height;
                    Bar.value = offset;
                }
                finally
                {
                    _setting = false;
                }

                // A bar lays its thumb out only while it shows (UIScrollbar.UpdateThumb), and again
                // whenever it is drawn anew: drawn anew, for the new size and place.
                Bar.Invalidate();
            }

            Place(offset);
        }

        /// <summary>
        /// A new height for the window (a panel fitting it to one of its pages once they are built),
        /// its bar with it; the shown page keeps where it was scrolled to, as far as it still can.
        /// </summary>
        public void Resize(float height)
        {
            Viewport.height = height;
            if (Bar != null)
            {
                Bar.height = height;
                if (Bar.trackObject != null)
                    Bar.trackObject.height = height;
            }

            SetHeight(_height);
        }

        /// <summary>Scrolls the shown page to <paramref name="offset"/>, as far as it goes.</summary>
        public void ScrollTo(float offset)
        {
            offset = Mathf.Clamp(offset, 0f, MaxOffset);
            if (Bar != null)
                Bar.value = offset;   // its event places the page
            else
                Place(offset);
        }

        /// <summary>
        /// Scrolls just enough for the stretch from <paramref name="top"/> to <paramref name="bottom"/>
        /// (in the page) to show -- a group just opened at the bottom -- its top first when it can't all.
        /// </summary>
        public void Reveal(float top, float bottom)
        {
            float offset = Offset;
            if (bottom > offset + Viewport.height)
                offset = bottom - Viewport.height;
            if (top < offset)
                offset = top;

            ScrollTo(offset);
        }

        internal void Wire()
        {
            Viewport.eventMouseWheel += (component, p) =>
            {
                if (_page == null || p.used || MaxOffset <= 0f)
                    return;

                ScrollTo(Offset - p.wheelDelta * WheelStep);
                p.Use();
            };

            if (Bar != null)
            {
                Bar.eventValueChanged += (component, value) =>
                {
                    if (!_setting)
                        Place(value);
                };
            }
        }

        /// <summary>Whole units, or the text goes soft.</summary>
        private void Place(float offset)
        {
            if (_page != null)
                _page.relativePosition = new Vector3(_page.relativePosition.x, -Mathf.Round(offset));
        }
    }
}
