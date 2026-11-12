using System;
using System.Collections.Generic;
using UnityEngine;

namespace AngelsAndDemons
{
    public static class DefenseArt
    {
        public static Font Font;
        public static Shader CharacterShader;
        public static Material FlashMaterial;
        public static Sprite Pixel;
        public static Sprite Circle;
        public static Sprite Ring;
        public static Sprite Shadow;
        public static Sprite Spark;
        public static Sprite SlashCrescent;
        public static Sprite PalmWave;
        public static Sprite KunaiLance;
        public static Sprite SkyBackdrop;
        public static Sprite MountainBackdrop;
        public static Sprite ColonnadeBackdrop;
        public static Sprite RoadSurface;
        public static Sprite AngelCastle;
        public static Sprite DemonCastle;

        static readonly Dictionary<int, Sprite> stageBackdrops = new Dictionary<int, Sprite>();
        static readonly Dictionary<string, Sprite> bodySprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Texture2D> portraitTextures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Texture2D> rawTextures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Texture2D> atlasTextures = new Dictionary<string, Texture2D>();

        public static void Initialize()
        {
            if (Pixel != null) return;

            Font = Resources.Load<Font>("AngelsAndDemons/Fonts/NotoSansKR");
            CharacterShader = Resources.Load<Shader>("AngelsAndDemons/SpriteImpact");
            if (CharacterShader == null)
                CharacterShader = Shader.Find("Unlit/Transparent");
            if (CharacterShader == null)
                CharacterShader = Shader.Find("Sprites/Default");

            if (CharacterShader != null)
                FlashMaterial = new Material(CharacterShader);

            Pixel = MakeSprite(4, 4, (x, y) => Color.white, new Vector2(0.5f, 0.5f), 4f);
            Circle = MakeSprite(96, 96, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));
                return new Color(1f, 1f, 1f, Mathf.Clamp01((0.49f - d) * 48f));
            }, new Vector2(0.5f, 0.5f), 96f);

            Ring = MakeSprite(160, 160, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));
                float a = Mathf.Clamp01((0.045f - Mathf.Abs(d - 0.43f)) * 90f);
                return new Color(1f, 1f, 1f, a);
            }, new Vector2(0.5f, 0.5f), 160f);

            Shadow = MakeSprite(64, 32, (x, y) =>
            {
                float dx = (x - 0.5f) * 2f;
                float dy = (y - 0.5f) * 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                return new Color(0f, 0.02f, 0.06f, Mathf.Clamp01(1f - d) * 0.55f);
            }, new Vector2(0.5f, 0.5f), 32f);

            Spark = MakeSprite(96, 96, (x, y) =>
            {
                float a = Mathf.Atan2(y - 0.5f, x - 0.5f);
                float r = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));
                float edge = 0.16f + Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 4f)), 6f) * 0.32f;
                return r < edge ? new Color(1f, 1f, 1f, Mathf.Clamp01((edge - r) * 24f)) : Color.clear;
            }, new Vector2(0.5f, 0.5f), 96f);

            SlashCrescent = MakeSprite(128, 128, (x, y) =>
            {
                Vector2 p = new Vector2(x - 0.32f, y - 0.5f);
                float r = p.magnitude;
                float angle = Mathf.Atan2(p.y, p.x);
                if (Mathf.Abs(angle) > 1.28f) return Color.clear;
                float arcDist = Mathf.Abs(r - 0.44f);
                float thickness = 0.11f * (1f - Mathf.Abs(angle) / 1.28f);
                if (arcDist >= thickness) return Color.clear;
                float a = Mathf.Clamp01((1f - arcDist / Mathf.Max(0.001f, thickness)) * 1.4f);
                return new Color(1f, 1f, 1f, a);
            }, new Vector2(0.35f, 0.5f), 96f);

            PalmWave = MakeSprite(128, 96, (x, y) =>
            {
                float dx = x - 0.22f;
                float dy = (y - 0.5f) * 1.55f;
                if (dx < 0f || dx > 0.74f) return Color.clear;
                float cone = dx * 0.68f + 0.09f;
                if (Mathf.Abs(dy) > cone) return Color.clear;
                float wave = Mathf.Abs(Mathf.Sin(dx * 22f));
                float alpha = (1f - Mathf.Abs(dy) / cone) * (1f - dx / 0.76f) * (0.5f + 0.5f * wave);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(alpha * 1.5f));
            }, new Vector2(0.2f, 0.5f), 96f);

            KunaiLance = MakeSprite(128, 48, (x, y) =>
            {
                float dy = Mathf.Abs(y - 0.5f);
                float taper = x < 0.22f ? x * 0.9f : (1f - x) * 0.40f;
                if (dy > taper) return Color.clear;
                float a = Mathf.Clamp01((taper - dy) * 24f);
                return new Color(1f, 1f, 1f, a);
            }, new Vector2(0.3f, 0.5f), 96f);

            SkyBackdrop = LoadStageBackdropSprite("stage_bg_sky");
            stageBackdrops[0] = SkyBackdrop;
            stageBackdrops[1] = LoadStageBackdropSprite("stage_bg_twilight");
            stageBackdrops[2] = LoadStageBackdropSprite("stage_bg_citadel");
            stageBackdrops[3] = LoadStageBackdropSprite("stage_bg_abyss");

            MountainBackdrop = SkyBackdrop;
            ColonnadeBackdrop = SkyBackdrop;
            RoadSurface = SkyBackdrop;

            AngelCastle = LoadCastleSprite("angel_castle", isAngel: true);
            DemonCastle = LoadCastleSprite("demon_castle", isAngel: false);
        }

        public static Sprite GetStageBackdrop(int stageNumber)
        {
            Initialize();
            int themeIdx = 0;
            if (stageNumber >= 16) themeIdx = 3;
            else if (stageNumber >= 11) themeIdx = 2;
            else if (stageNumber >= 6) themeIdx = 1;
            if (stageBackdrops.TryGetValue(themeIdx, out var sp) && sp != null)
                return sp;
            return SkyBackdrop;
        }

        static Sprite LoadStageBackdropSprite(string name)
        {
            var tex = Resources.Load<Texture2D>("AngelsAndDemons/Environment/" + name);
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                // 2560x720 at 76 PPU -> 33.68 x 9.47 world units; pivot Y=0.16 places the painted stone road at the unit lane!
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.16f), 76f, 0, SpriteMeshType.FullRect);
            }
            return MakeSprite(512, 256, (x, y) => Color.Lerp(new Color(0.18f, 0.25f, 0.42f), new Color(0.45f, 0.65f, 0.90f), y), new Vector2(0.5f, 0.16f), 28f);
        }

        static Sprite LoadCastleSprite(string name, bool isAngel)
        {
            var tex = Resources.Load<Texture2D>("AngelsAndDemons/Environment/" + name);
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                // 560x760 at 120 PPU -> 4.67 x 6.33 world units, pivot at bottom gate base (0.5, 0.03)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.03f), 120f, 0, SpriteMeshType.FullRect);
            }
            return MakeSprite(256, 384, (x, y) => isAngel ? new Color(0.85f, 0.9f, 1f) : new Color(0.3f, 0.15f, 0.25f), new Vector2(0.5f, 0.03f), 64f);
        }

        public static Material CreateCharacterMaterial(Texture2D tex)
        {
            Initialize();
            var mat = new Material(CharacterShader);
            mat.mainTexture = tex;
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_Flash")) mat.SetFloat("_Flash", 0f);
            return mat;
        }

        public static Texture2D GetCharacterTexture(string id)
        {
            Initialize();
            if (rawTextures.TryGetValue(id, out var cached) && cached != null) return cached;
            var tex = Resources.Load<Texture2D>("AngelsAndDemons/Characters/" + id);
            if (tex == null)
            {
                tex = BuildFallbackCharacterTexture(id);
            }
            rawTextures[id] = tex;
            return tex;
        }

        public static Texture2D GetCharacterAtlasTexture(string id)
        {
            Initialize();
            if (atlasTextures.TryGetValue(id, out var cached) && cached != null) return cached;
            var tex = Resources.Load<Texture2D>("AngelsAndDemons/Animations/" + id + "_atlas");
            if (tex == null)
            {
                tex = GetCharacterTexture(id);
            }
            else
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
            }
            atlasTextures[id] = tex;
            return tex;
        }

        public static Sprite GetCharacterSprite(string id)
        {
            Initialize();
            if (bodySprites.TryGetValue(id, out var sprite) && sprite != null) return sprite;
            var tex = GetCharacterTexture(id);
            float pivotY = 44f / Mathf.Max(1f, tex.height);
            float ppu = tex.height / 2.35f;
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, pivotY), ppu, 0, SpriteMeshType.FullRect);
            bodySprites[id] = sprite;
            return sprite;
        }

        public static Texture2D GetPortraitTexture(string id)
        {
            Initialize();
            if (portraitTextures.TryGetValue(id, out var p) && p != null) return p;
            var tex = GetCharacterTexture(id);
            portraitTextures[id] = tex;
            return tex;
        }

        static Texture2D BuildFallbackCharacterTexture(string id)
        {
            int w = 256, h = 512;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - w * 0.5f) / (w * 0.22f);
                    float dy = (y - h * 0.5f) / (h * 0.44f);
                    pixels[y * w + x] = (dx * dx + dy * dy <= 1f) ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static Sprite MakeSprite(int w, int h, Func<float, float, Color> fn, Vector2 pivot, float ppu)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var colors = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    colors[y * w + x] = fn((x + 0.5f) / w, (y + 0.5f) / h);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu);
        }
    }
}
