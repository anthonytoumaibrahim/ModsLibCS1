using System;
using System.IO;
using System.Reflection;
using System.Text;
using ColossalFramework.UI;
using UnityEngine;

namespace ModsLibCS1.UI
{
    /// <summary>
    /// The library's sprites as a UITextureAtlas, built once from the sheet embedded in the mod's
    /// own DLL (ModsLibCS1.props embeds it under fixed names), with the mod's own icons merged in
    /// when it has them.
    /// </summary>
    /// <remarks>
    /// Every mod carries its own copy of this class, so this static is per mod, and the atlas is
    /// named after the mod's assembly. Nothing ever looks an atlas up by name.
    ///
    /// The library's sheet holds only what every mod uses. A mod's own icons (its tab icons,
    /// "icon-" + a name) come from a sheet of its own (tools/make-icons.ps1), which the props embed
    /// as ModsLibCS1.icons.png / .txt. An atlas draws from ONE texture (its material's; a sprite's
    /// region is a part of it), so the two sheets are stacked into one texture here: the library's
    /// at the top, the mod's below it. Every panel sprite stays in one atlas, so one draw call can
    /// still batch them all. A mod's icon of the same name as one of the library's wins.
    /// </remarks>
    public static class Atlas
    {
        private const string PngResource = "ModsLibCS1.sheet.png";
        private const string TableResource = "ModsLibCS1.sheet.txt";
        private const string IconsPngResource = "ModsLibCS1.icons.png";
        private const string IconsTableResource = "ModsLibCS1.icons.txt";

        /// <summary>Clear rows between the library's sheet and the mod's in the stacked texture.</summary>
        private const int StackGap = 2;

        private static UITextureAtlas _atlas;
        private static bool _tried;

        /// <summary>Why <see cref="Ours"/> is null, if it is.</summary>
        public static string Problem { get; private set; }

        /// <summary>The game's own atlas ('Ingame'), what Vanilla draws with.</summary>
        public static UITextureAtlas Game
        {
            get
            {
                UIView view = UIView.GetAView();
                return view != null ? view.defaultAtlas : null;
            }
        }

        /// <summary>The library's atlas, built on first use; null (see <see cref="Problem"/>) if it can't be.</summary>
        public static UITextureAtlas Ours
        {
            get
            {
                // Before the game's UI exists there is nothing to copy the material from: try
                // again next time rather than give up for the whole session.
                if (!_tried && Game != null)
                {
                    _tried = true;
                    try
                    {
                        _atlas = Build();
                    }
                    catch (Exception e)
                    {
                        _atlas = null;
                        Problem = e.GetType().Name + ": " + e.Message;
                    }

                    if (_atlas == null)
                        LibLog.Msg("atlas NOT built (" + Problem + "): every skin draws as Vanilla");
                }

                return _atlas;
            }
        }

        /// <summary>The atlas a skin's sprites live in.</summary>
        public static UITextureAtlas For(SkinSprites sprites)
        {
            return sprites.OwnSheet ? Ours : Game;
        }

        /// <summary>The first sprite of <paramref name="sprites"/> the atlas lacks, or null if it has them all.</summary>
        public static string FirstMissing(UITextureAtlas atlas, SkinSprites sprites)
        {
            foreach (string name in sprites.All())
            {
                if (atlas == null || atlas[name] == null)
                    return name;
            }
            return null;
        }

        private static UITextureAtlas Build()
        {
            UITextureAtlas game = Game;
            if (game == null || game.material == null)
            {
                Problem = "the game's atlas isn't there yet";
                return null;
            }

            Assembly assembly = typeof(Atlas).Assembly;
            SheetTable table;
            Texture2D sheet;
            string problem;
            if (!Load(assembly, PngResource, TableResource, out sheet, out table, out problem))
            {
                Problem = problem ?? "the sprite sheet is not embedded in " + assembly.GetName().Name;
                return null;
            }

            // The mod's own icons: optional. Without them (or with a broken sheet, logged) an icon
            // tab draws its name instead (Builder.AddTabStrip).
            SheetTable icons;
            Texture2D iconSheet;
            if (!Load(assembly, IconsPngResource, IconsTableResource, out iconSheet, out icons, out problem) && problem != null)
                LibLog.Msg("the mod's own icons NOT used (" + problem + "): its icon tabs show their names");

            // One texture: the library's sheet alone, as it always was, or both stacked.
            Texture2D texture = sheet;
            int iconsTop = table.Height + StackGap;
            if (icons != null)
            {
                int width = Mathf.NextPowerOfTwo(Mathf.Max(table.Width, icons.Width));
                int height = Mathf.NextPowerOfTwo(iconsTop + icons.Height);
                Color[] pixels = new Color[width * height];
                CopyInto(pixels, width, height, sheet, 0);
                CopyInto(pixels, width, height, iconSheet, iconsTop);

                texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
                texture.SetPixels(pixels);
                texture.Apply(false);
                UnityEngine.Object.Destroy(sheet);
                UnityEngine.Object.Destroy(iconSheet);
            }

            string name = "ModsLibCS1-" + assembly.GetName().Name;
            texture.name = name;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            atlas.name = name;
            Material material = UnityEngine.Object.Instantiate(game.material);
            material.mainTexture = texture;
            atlas.material = material;

            // The mod's icons first: one named as a library sprite replaces it.
            int count = 0;
            if (icons != null)
            {
                foreach (SheetTable.Entry e in icons.Entries)
                    count += AddSprite(atlas, texture, e, iconsTop);
            }

            int own = count;
            foreach (SheetTable.Entry e in table.Entries)
            {
                if (icons == null || !icons.Contains(e.Name))
                    count += AddSprite(atlas, texture, e, 0);
            }

            LibLog.Msg("atlas " + name + " built: " + count + " sprites (" +
                       (icons != null ? own + " of them the mod's own icons" : "no icons of the mod's own") + "), " +
                       texture.width + "x" + texture.height);
            return atlas;
        }

        /// <summary>
        /// One sprite of a sheet whose top row is <paramref name="top"/> in the atlas's texture.
        /// Returns 1 (added), for counting.
        /// </summary>
        private static int AddSprite(UITextureAtlas atlas, Texture2D texture, SheetTable.Entry e, int top)
        {
            // Unity's texture rows count from the bottom; the tables' from the top.
            int bottom = texture.height - top - e.Y - e.Height;

            // A sprite's size in UI units is ITS texture's size (SpriteInfo.width reads
            // texture.width), not its region's: each gets a texture of its own, a copy.
            Texture2D own = new Texture2D(e.Width, e.Height, TextureFormat.ARGB32, false);
            own.name = e.Name;
            own.SetPixels(texture.GetPixels(e.X, bottom, e.Width, e.Height));
            own.Apply(false);

            float w = texture.width, h = texture.height;
            atlas.AddSprite(new UITextureAtlas.SpriteInfo
            {
                name = e.Name,
                texture = own,
                region = new Rect(e.X / w, bottom / h, e.Width / w, e.Height / h),
                border = new RectOffset(e.Left, e.Right, e.Top, e.Bottom),
            });
            return 1;
        }

        /// <summary>
        /// Copies a sheet into a texture's pixels (rows from the bottom, as GetPixels gives them) with
        /// the sheet's top row at row <paramref name="top"/> counted from the texture's top.
        /// </summary>
        private static void CopyInto(Color[] pixels, int width, int height, Texture2D sheet, int top)
        {
            Color[] from = sheet.GetPixels();
            int w = sheet.width, h = sheet.height;
            int firstRow = height - top - h;
            for (int row = 0; row < h; row++)
                Array.Copy(from, row * w, pixels, (firstRow + row) * width, w);
        }

        /// <summary>
        /// A sheet and its table from the mod's DLL. False with <paramref name="problem"/> null when it
        /// is not embedded at all; false with the reason when it is there but unusable.
        /// </summary>
        private static bool Load(Assembly assembly, string pngName, string tableName, out Texture2D sheet,
            out SheetTable table, out string problem)
        {
            sheet = null;
            table = null;
            problem = null;

            byte[] png = Resource(assembly, pngName);
            byte[] tableBytes = Resource(assembly, tableName);
            if (png == null && tableBytes == null)
                return false;
            if (png == null || tableBytes == null)
            {
                problem = (png == null ? pngName : tableName) + " is not embedded beside the other";
                return false;
            }

            string error;
            table = SheetTable.Parse(Encoding.UTF8.GetString(tableBytes), out error);
            if (table == null)
            {
                problem = tableName + ": " + error;
                return false;
            }

            sheet = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            if (!sheet.LoadImage(png) || sheet.width != table.Width || sheet.height != table.Height)
            {
                problem = pngName + " is " + sheet.width + "x" + sheet.height + ", its table says " +
                          table.Width + "x" + table.Height;
                UnityEngine.Object.Destroy(sheet);
                sheet = null;
                table = null;
                return false;
            }

            return true;
        }

        private static byte[] Resource(Assembly assembly, string name)
        {
            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                    return null;

                byte[] buffer = new byte[stream.Length];
                int offset = 0;
                while (offset < buffer.Length)
                {
                    int read = stream.Read(buffer, offset, buffer.Length - offset);
                    if (read <= 0)
                        break;
                    offset += read;
                }
                return buffer;
            }
        }
    }
}
