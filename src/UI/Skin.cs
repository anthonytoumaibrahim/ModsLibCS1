namespace ModsLibCS1.UI
{
    /// <summary>
    /// The look a player picks on a mod's options page. Each mod stores the pick in its own
    /// settings file BY VALUE: the numbers are file format, so they never change and new skins
    /// are only ever appended.
    /// </summary>
    public enum Skin
    {
        /// <summary>A charcoal panel the sky shows through. The default.</summary>
        SeeThrough = 0,

        /// <summary>The same layout on a deep-blue opaque panel.</summary>
        GameBlue = 1,

        /// <summary>The game's own look, bit for bit. Also what any skin falls back to.</summary>
        Vanilla = 2,
    }
}
