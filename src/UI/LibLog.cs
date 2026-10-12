using System;
using UnityEngine;

namespace ModsLibCS1.UI
{
    /// <summary>
    /// Where the library's one-shot log lines go. The mod sets <see cref="Sink"/> (e.g. its own
    /// log's Msg) before building anything; until then the lines go to the Unity log.
    /// </summary>
    public static class LibLog
    {
        public static Action<string> Sink;

        internal static void Msg(string text)
        {
            string line = "ui: " + text;
            Action<string> sink = Sink;

            if (sink != null)
            {
                try
                {
                    sink(line);
                    return;
                }
                catch (Exception)
                {
                    // A broken sink must not take the panel down; fall through to Unity's log.
                }
            }

            Debug.Log("[ModsLibCS1] " + line);
        }
    }
}
