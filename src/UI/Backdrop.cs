using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;

namespace ModsLibCS1.UI
{
    /// <summary>
    /// The blurred screen behind a Frosted panel: the game's UI has no blur, so this draws a
    /// small, softened copy of the finished frame (<see cref="FrameGrab"/>) through the part of
    /// the screen the panel covers, in the panel's rounded outline.
    /// </summary>
    /// <remarks>
    /// A UITextureSprite whose geometry is ours: the game's own OnRebuildRenderData (read from its
    /// IL) makes a quad with UVs 0..1 in the material "UI/Default UI Shader" with the texture as
    /// _MainTex; this replaces that quad with a rounded polygon whose UVs are the panel's place on
    /// the screen. The rounding comes from the outline, so no shader of our own is needed. The
    /// polygon's edge is not antialiased; the panel's tint sprite, drawn over it, has a soft edge.
    /// </remarks>
    public sealed class Backdrop : UITextureSprite
    {
        /// <summary>Corner radius in UI units: the tint sprite's.</summary>
        public float Radius = 14f;

        private const int ArcSteps = 6;

        private Vector3 _lastAt;
        private Vector2 _lastSize;
        private Vector2 _lastScreen;

        public override void Start()
        {
            base.Start();
            pivot = UIPivotPoint.TopLeft;
            isInteractive = false;

            UITextureAtlas game = Atlas.Game;
            if (material == null && game != null)
                material = game.material;
        }

        public override void Update()
        {
            base.Update();

            bool wanted = isVisible;
            FrameGrab.Want(this, wanted);

            RenderTexture frame = wanted ? FrameGrab.Result : null;
            if (texture != frame)
            {
                // The texture setter would size an unsized sprite to the texture: keep ours.
                Vector2 keep = size;
                texture = frame;
                size = keep;
            }

            UIView view = GetUIView();
            Vector2 screen = view != null ? view.GetScreenResolution() : Vector2.zero;
            Vector3 at = absolutePosition;
            if (at != _lastAt || size != _lastSize || screen != _lastScreen)
            {
                _lastAt = at;
                _lastSize = size;
                _lastScreen = screen;
                Invalidate();
            }
        }

        public override void OnDestroy()
        {
            FrameGrab.Want(this, false);
            base.OnDestroy();
        }

        protected override void OnRebuildRenderData()
        {
            base.OnRebuildRenderData();

            UIView view = GetUIView();
            if (texture == null || view == null || renderData.vertices.Count == 0)
                return;

            Vector2 screen = view.GetScreenResolution();
            if (screen.x <= 0f || screen.y <= 0f)
                return;

            float units = PixelsToUnits();
            float w = width, h = height;
            float r = Mathf.Clamp(Radius, 0f, Mathf.Min(w, h) * 0.5f);
            Vector3 at = absolutePosition;

            renderData.vertices.Clear();
            renderData.uvs.Clear();
            renderData.triangles.Clear();
            renderData.colors.Clear();

            Color32 colour = color;
            colour.a = (byte)Mathf.RoundToInt(colour.a * opacity);

            // A fan round the centre: four corner arcs, clockwise from the top left.
            List<Vector2> outline = Outline(w, h, r);
            Add(new Vector2(w * 0.5f, h * 0.5f), at, screen, units, colour);
            foreach (Vector2 p in outline)
                Add(p, at, screen, units, colour);

            int n = outline.Count;
            for (int i = 0; i < n; i++)
            {
                renderData.triangles.Add(0);
                renderData.triangles.Add(1 + i);
                renderData.triangles.Add(1 + (i + 1) % n);
            }
        }

        /// <summary>One point, in the component's own space (UI units, y down from its top left).</summary>
        private void Add(Vector2 p, Vector3 at, Vector2 screen, float units, Color32 colour)
        {
            renderData.vertices.Add(new Vector3(p.x * units, -p.y * units, 0f));
            renderData.uvs.Add(new Vector2((at.x + p.x) / screen.x, 1f - (at.y + p.y) / screen.y));
            renderData.colors.Add(colour);
        }

        private static List<Vector2> Outline(float w, float h, float r)
        {
            List<Vector2> points = new List<Vector2>();
            Corner(points, r, r, r, 180f);           // top left
            Corner(points, w - r, r, r, 270f);       // top right
            Corner(points, w - r, h - r, r, 0f);     // bottom right
            Corner(points, r, h - r, r, 90f);        // bottom left
            return points;
        }

        /// <summary>
        /// A quarter circle from <paramref name="start"/> degrees on, clockwise on screen
        /// (y points down here, so increasing angles turn clockwise).
        /// </summary>
        private static void Corner(List<Vector2> points, float cx, float cy, float r, float start)
        {
            if (r <= 0f)
            {
                points.Add(new Vector2(cx, cy));
                return;
            }

            for (int i = 0; i <= ArcSteps; i++)
            {
                float a = (start + 90f * i / ArcSteps) * Mathf.Deg2Rad;
                points.Add(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r));
            }
        }
    }

    /// <summary>
    /// A small, softened copy of the main camera's finished frame (no UI in it: the UI has its own
    /// camera), made only while some Backdrop on screen wants it.
    /// </summary>
    /// <remarks>
    /// Added to the main camera as the LAST image effect (components run OnRenderImage in order),
    /// so it sees the frame after the game's tonemapping and every other effect. It passes the
    /// frame on unchanged, then halves a copy with plain bilinear blits (each halving averages 2x2
    /// pixels) until it is about 70 pixels high whatever the resolution, and scales it back up
    /// twice so the upscale is smooth. Removed, and its texture released, when the last Backdrop
    /// stops wanting it. Per mod: each mod's copy of the library has its own.
    /// </remarks>
    public sealed class FrameGrab : MonoBehaviour
    {
        private const int SmallestHeight = 70;
        private const int Ups = 2;

        private static readonly HashSet<Backdrop> Users = new HashSet<Backdrop>();
        private static FrameGrab _instance;
        private static RenderTexture _result;
        private static bool _logged;

        /// <summary>The blurred frame, or null before the first one.</summary>
        public static RenderTexture Result
        {
            get { return _result; }
        }

        public static void Want(Backdrop user, bool wanted)
        {
            if (wanted)
                Users.Add(user);
            else
                Users.Remove(user);

            if (Users.Count > 0)
                Attach();
            else
                Detach();
        }

        private static void Attach()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            if (_instance != null && _instance.gameObject == camera.gameObject)
                return;

            Detach();
            _instance = camera.gameObject.AddComponent<FrameGrab>();
            if (!_logged)
            {
                _logged = true;
                LibLog.Msg("frosted: blurring the frame behind the panel (camera " + camera.name + ")");
            }
        }

        private static void Detach()
        {
            if (_instance != null)
            {
                Destroy(_instance);
                _instance = null;
            }

            if (_result != null)
            {
                _result.Release();
                Destroy(_result);
                _result = null;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination);

            if (this != _instance || source == null)
                return;

            int w = source.width, h = source.height;
            int levels = 0;
            while (h / 2 >= SmallestHeight && levels < 8)
            {
                w = Mathf.Max(1, w / 2);
                h /= 2;
                levels++;
            }

            int ups = Mathf.Min(Ups, levels);
            int resultW = w << ups, resultH = h << ups;
            if (_result == null || _result.width != resultW || _result.height != resultH)
            {
                if (_result != null)
                {
                    _result.Release();
                    Destroy(_result);
                }

                _result = new RenderTexture(resultW, resultH, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                {
                    name = "ModsLibCS1 frosted",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                _result.Create();
            }

            // Down: halve the frame until it is about 70 high.
            RenderTexture current = null;
            int cw = source.width, ch = source.height;
            for (int i = 0; i < levels; i++)
            {
                cw = Mathf.Max(1, cw / 2);
                ch = Mathf.Max(1, ch / 2);
                RenderTexture next = Temporary(cw, ch);
                Graphics.Blit(current != null ? current : source, next);
                if (current != null)
                    RenderTexture.ReleaseTemporary(current);
                current = next;
            }

            if (current == null)
                return;

            // Up: double it back, all but the last step in temporaries, the last into the result.
            for (int i = 0; i < ups - 1; i++)
            {
                cw *= 2;
                ch *= 2;
                RenderTexture next = Temporary(cw, ch);
                Graphics.Blit(current, next);
                RenderTexture.ReleaseTemporary(current);
                current = next;
            }

            Graphics.Blit(current, _result);
            RenderTexture.ReleaseTemporary(current);
        }

        private static RenderTexture Temporary(int w, int h)
        {
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            return rt;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
