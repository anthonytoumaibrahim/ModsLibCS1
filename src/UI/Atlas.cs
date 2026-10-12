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
    /// own DLL (ModsLibCS1.props embeds it under fixed names).
    /// </summary>
    /// <remarks>
    /// Every mod carries its own copy of this class, so this static is per mod, and the atlas is
    /// named after the mod's assembly. Nothing ever looks an atlas up by name.
    /// </remarks>
    public static class Atlas
    {
        private const string PngResource = "ModsLibCS1.sheet.png";
        private const string TableResource = "ModsLibCS1.sheet.txt";

        private static UITextureAtlas _atlas;
        private static SheetTable _table;
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
            byte[] png = Resource(assembly, PngResource);
            byte[] tableBytes = Resource(assembly, TableResource);
            if (png == null || tableBytes == null)
            {
                Problem = "the sprite sheet is not embedded in " + assembly.GetName().Name;
                return null;
            }

            string error;
            _table = SheetTable.Parse(Encoding.UTF8.GetString(tableBytes), out error);
            if (_table == null)
            {
                Problem = "sheet.txt: " + error;
                return null;
            }

            Texture2D sheet = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            if (!sheet.LoadImage(png) || sheet.width != _table.Width || sheet.height != _table.Height)
            {
                Problem = "sheet.png is " + sheet.width + "x" + sheet.height + ", the table says " +
                          _table.Width + "x" + _table.Height;
                UnityEngine.Object.Destroy(sheet);
                return null;
            }

            string name = "ModsLibCS1-" + assembly.GetName().Name;
            sheet.name = name;
            sheet.filterMode = FilterMode.Bilinear;
            sheet.wrapMode = TextureWrapMode.Clamp;

            UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            atlas.name = name;
            Material material = UnityEngine.Object.Instantiate(game.material);
            material.mainTexture = sheet;
            atlas.material = material;

            float w = _table.Width, h = _table.Height;
            foreach (SheetTable.Entry e in _table.Entries)
            {
                // Unity's texture rows count from the bottom; the table's from the top.
                int bottom = _table.Height - e.Y - e.Height;

                // A sprite's size in UI units is ITS texture's size (SpriteInfo.width reads
                // texture.width), not its region's: each gets a texture of its own, a copy.
                Texture2D own = new Texture2D(e.Width, e.Height, TextureFormat.ARGB32, false);
                own.name = e.Name;
                own.SetPixels(sheet.GetPixels(e.X, bottom, e.Width, e.Height));
                own.Apply(false);

                atlas.AddSprite(new UITextureAtlas.SpriteInfo
                {
                    name = e.Name,
                    texture = own,
                    region = new Rect(e.X / w, bottom / h, e.Width / w, e.Height / h),
                    border = new RectOffset(e.Left, e.Right, e.Top, e.Bottom),
                });
            }

            LibLog.Msg("atlas " + name + " built: " + _table.Entries.Count + " sprites, " +
                       _table.Width + "x" + _table.Height);
            return atlas;
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
