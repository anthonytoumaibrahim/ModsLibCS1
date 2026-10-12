namespace ModsLibCS1.UI
{
    /// <summary>
    /// The look a player picks on a mod's options page. Each mod stores the pick in its own
    /// settings file BY VALUE: the numbers are file format, so they never change and new skins
    /// are only ever appended.
    /// </summary>
    public enum Skin
    {
        /// <summary>A charcoal panel the sky shows through. (Which skin is a mod's default is the mod's.)</summary>
        SeeThrough = 0,

        /// <summary>The same layout on a deep-blue opaque panel.</summary>
        GameBlue = 1,

        /// <summary>The game's own look, bit for bit. Also what any skin falls back to.</summary>
        Vanilla = 2,

        /// <summary>
        /// After Cities: Skylines II's photo mode: blue-grey glass over a blurred copy of the
        /// screen, rounded-top tabs, capitals, tick boxes, a line between rows.
        /// </summary>
        Frosted = 3,
    }
}
