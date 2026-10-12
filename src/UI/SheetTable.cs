using System.Collections.Generic;
using System.Globalization;

namespace ModsLibCS1.UI
{
    /// <summary>
    /// The sprite sheet's table (sprites/sheet.txt, written by tools/make-sprites.ps1): one line
    /// per sprite, "name x y width height left right top bottom" in pixels with y from the TOP.
    /// </summary>
    /// <remarks>
    /// Pure, and written in C# 5 so the offline tests can compile this file on its own.
    /// </remarks>
    public sealed class SheetTable
    {
        public struct Entry
        {
            public string Name;
            public int X, Y, Width, Height;
            public int Left, Right, Top, Bottom;
        }

        public int Width;
        public int Height;
        public readonly List<Entry> Entries = new List<Entry>();

        private readonly Dictionary<string, int> _index = new Dictionary<string, int>();

        public bool Contains(string name)
        {
            return name != null && _index.ContainsKey(name);
        }

        /// <summary>The table, or null with <paramref name="error"/> saying which line is wrong.</summary>
        public static SheetTable Parse(string text, out string error)
        {
            error = null;
            SheetTable table = new SheetTable();

            if (text == null)
            {
                error = "no text";
                return null;
            }

            string[] lines = text.Replace("\r", "").Split('\n');
            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n].Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;

                string[] parts = line.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

                if (parts[0] == "sheet")
                {
                    if (parts.Length != 3 || !Int(parts[1], out table.Width) || !Int(parts[2], out table.Height)
                        || table.Width <= 0 || table.Height <= 0)
                    {
                        error = "line " + (n + 1) + ": bad sheet size";
                        return null;
                    }
                    continue;
                }

                Entry e = new Entry { Name = parts[0] };
                if (parts.Length != 9
                    || !Int(parts[1], out e.X) || !Int(parts[2], out e.Y)
                    || !Int(parts[3], out e.Width) || !Int(parts[4], out e.Height)
                    || !Int(parts[5], out e.Left) || !Int(parts[6], out e.Right)
                    || !Int(parts[7], out e.Top) || !Int(parts[8], out e.Bottom))
                {
                    error = "line " + (n + 1) + ": expected name and 8 numbers";
                    return null;
                }

                if (table.Width <= 0)
                {
                    error = "line " + (n + 1) + ": sprite before the sheet size";
                    return null;
                }

                if (e.Width <= 0 || e.Height <= 0 || e.X < 0 || e.Y < 0
                    || e.X + e.Width > table.Width || e.Y + e.Height > table.Height)
                {
                    error = "line " + (n + 1) + ": " + e.Name + " lies outside the sheet";
                    return null;
                }

                if (e.Left < 0 || e.Right < 0 || e.Top < 0 || e.Bottom < 0
                    || e.Left + e.Right >= e.Width && e.Left + e.Right > 0
                    || e.Top + e.Bottom >= e.Height && e.Top + e.Bottom > 0)
                {
                    error = "line " + (n + 1) + ": " + e.Name + "'s border leaves no middle";
                    return null;
                }

                if (table._index.ContainsKey(e.Name))
                {
                    error = "line " + (n + 1) + ": " + e.Name + " twice";
                    return null;
                }

                table._index[e.Name] = table.Entries.Count;
                table.Entries.Add(e);
            }

            if (table.Width <= 0)
            {
                error = "no sheet size";
                return null;
            }

            return table;
        }

        private static bool Int(string s, out int value)
        {
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }
}
