namespace ModsLibCS1.UI
{
    /// <summary>
    /// Whether a choice row's names fit side by side as a row of buttons. When they don't
    /// (Spanish and French names run longer) the row is a dropdown instead.
    /// </summary>
    /// <remarks>Pure, C# 5: compiled on its own by the offline tests.</remarks>
    public static class ChoiceFit
    {
        /// <summary>No segment narrower than this, however short its name.</summary>
        public const float MinSegment = 72f;

        /// <summary>Room round a name inside its segment, both sides together.</summary>
        public const float SegmentPadding = 28f;

        /// <summary>Between two segments.</summary>
        public const float Gap = 2f;

        /// <summary>Round the segments, inside the strip, on each side.</summary>
        public const float StripPadding = 2f;

        public static float[] SegmentWidths(float[] textWidths)
        {
            float[] widths = new float[textWidths.Length];
            for (int i = 0; i < textWidths.Length; i++)
            {
                float w = textWidths[i] + SegmentPadding;
                widths[i] = w < MinSegment ? MinSegment : (float)System.Math.Ceiling(w);
            }
            return widths;
        }

        public static float StripWidth(float[] segmentWidths)
        {
            if (segmentWidths.Length == 0)
                return 2f * StripPadding;

            float total = 2f * StripPadding + Gap * (segmentWidths.Length - 1);
            for (int i = 0; i < segmentWidths.Length; i++)
                total += segmentWidths[i];
            return total;
        }

        public static bool Fits(float[] textWidths, float available)
        {
            return textWidths.Length > 0 && StripWidth(SegmentWidths(textWidths)) <= available;
        }

        /// <summary>Kept clear between a row's name and buttons that reach into the names' column.</summary>
        public const float NameRoom = 12f;

        /// <summary>
        /// Where a row of buttons starts: at the names' <paramref name="column"/> when they fit
        /// between it and <paramref name="right"/>; else ending at <paramref name="right"/> (where
        /// every row's controls end) and reaching left into the column, as far as the row's own
        /// name, <paramref name="nameWidth"/> wide, leaves room (a short name on a page of long ones:
        /// "Cloud style" beside "Classic layer under Mixed"); -1 when neither fits (a dropdown).
        /// </summary>
        public static float Start(float[] textWidths, float column, float right, float nameWidth)
        {
            if (textWidths.Length == 0)
                return -1f;

            float strip = StripWidth(SegmentWidths(textWidths));
            if (column + strip <= right)
                return column;

            float start = right - strip;
            return start >= nameWidth + NameRoom ? start : -1f;
        }
    }
}
