using System.Collections.Generic;
using MeWorld.Core;
using UnityEngine;

namespace MeWorld.UI
{
    public static class ProceduralSpriteFactory
    {
        private static Sprite _whiteSquare;
        private static Sprite _circleFill;
        private static Sprite _circleRing;
        private static Sprite _roundedPanel;
        private static Sprite _crownSprite;
        private static Sprite _shieldSprite;
        private static TMPro.TMP_FontAsset _fallbackFont;
        private static Sprite _swordsIcon;
        private static Sprite _globeIcon;
        private static Sprite _settingsGearIcon;
        private static Sprite _bookIcon;
        private static Sprite _doorExitIcon;
        private static Sprite _sliderKnobSprite;
        private static Sprite _handshakeIcon;
        private static Sprite _brokenHeartIcon;
        private static Sprite _embargoIcon;
        private static Sprite _coinIcon;
        private static Sprite _shieldIcon;
        private static Sprite _chatBubbleIcon;
        private static Sprite _searchIcon;
        private static readonly Dictionary<StructureType, Sprite> _actionIcons = new Dictionary<StructureType, Sprite>();
        private static readonly Sprite[] _topBarIcons = new Sprite[6];

        public static Sprite GetSwordsIcon()
        {
            if (_swordsIcon != null) return _swordsIcon;
            int s = 32;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;
            DrawThickLine(tex, new Vector2(6, 6), new Vector2(26, 26), 2.5f, w);
            DrawThickLine(tex, new Vector2(26, 6), new Vector2(6, 26), 2.5f, w);
            DrawThickLine(tex, new Vector2(8, 14), new Vector2(14, 8), 2.2f, w);
            DrawThickLine(tex, new Vector2(24, 14), new Vector2(18, 8), 2.2f, w);
            tex.Apply();
            _swordsIcon = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _swordsIcon;
        }

        public static Sprite GetGlobeIcon()
        {
            if (_globeIcon != null) return _globeIcon;
            int s = 32;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    if (d >= 11.5f && d <= 13.5f) tex.SetPixel(x, y, w);
                    if (Mathf.Abs(y - 15.5f) <= 1.0f && d <= 12f) tex.SetPixel(x, y, w);
                    if (Mathf.Abs(x - 15.5f) <= 1.0f && d <= 12f) tex.SetPixel(x, y, w);
                    float ex = (x - 15.5f) / 6.0f;
                    float ey = (y - 15.5f) / 12.0f;
                    if (Mathf.Abs(ex * ex + ey * ey - 1f) < 0.25f && d <= 12f) tex.SetPixel(x, y, w);
                }
            }
            tex.Apply();
            _globeIcon = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _globeIcon;
        }

        public static Sprite GetSettingsGearIcon()
        {
            if (_settingsGearIcon != null) return _settingsGearIcon;
            int s = 32;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;
            FillCircle(tex, 16, 16, 8.5f, w);
            FillCircle(tex, 16, 16, 4.2f, Color.clear);
            FillRect(tex, 14, 3, 4, 26, w);
            FillRect(tex, 3, 14, 26, 4, w);
            DrawThickLine(tex, new Vector2(6, 6), new Vector2(26, 26), 4f, w);
            DrawThickLine(tex, new Vector2(6, 26), new Vector2(26, 6), 4f, w);
            FillCircle(tex, 16, 16, 3.2f, Color.clear);
            tex.Apply();
            _settingsGearIcon = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _settingsGearIcon;
        }

        public static Sprite GetBookIcon()
        {
            if (_bookIcon != null) return _bookIcon;
            int s = 32;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;
            FillRect(tex, 6, 6, 9, 20, w);
            FillRect(tex, 17, 6, 9, 20, w);
            FillRect(tex, 14, 4, 4, 24, w);
            FillCircle(tex, 16, 5, 2f, w);
            tex.Apply();
            _bookIcon = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _bookIcon;
        }

        public static Sprite GetDoorExitIcon()
        {
            if (_doorExitIcon != null) return _doorExitIcon;
            int s = 32;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;
            FillRect(tex, 6, 4, 12, 24, w);
            FillRect(tex, 9, 7, 7, 18, Color.clear);
            FillRect(tex, 14, 14, 10, 4, w);
            FillTriangle(tex, new Vector2(22, 11), new Vector2(28, 16), new Vector2(22, 21), w);
            tex.Apply();
            _doorExitIcon = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _doorExitIcon;
        }

        public static Sprite GetSliderKnobSprite()
        {
            if (_sliderKnobSprite != null) return _sliderKnobSprite;
            int s = 24;
            Texture2D tex = CreateBlankTex(s);
            FillCircle(tex, 12, 12, 9f, Color.white);
            FillCircle(tex, 12, 12, 6f, new Color32(245, 158, 42, 255));
            tex.Apply();
            _sliderKnobSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 24f);
            return _sliderKnobSprite;
        }

        public static Sprite GetTopBarIcon(int index)
        {
            if (index < 0 || index >= 6) index = 0;
            if (_topBarIcons[index] != null) return _topBarIcons[index];

            int s = 24;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;

            switch (index)
            {
                case 0: // Music / Note
                    FillCircle(tex, 7, 7, 3.2f, w);
                    FillCircle(tex, 16, 10, 3.2f, w);
                    FillRect(tex, 9, 7, 2, 11, w);
                    FillRect(tex, 18, 10, 2, 10, w);
                    FillRect(tex, 9, 17, 11, 3, w);
                    break;

                case 1: // Target / Focus Crosshairs
                    FillCircle(tex, 12, 12, 1.8f, w);
                    // Circle ring
                    for (int y = 0; y < s; y++)
                    {
                        for (int x = 0; x < s; x++)
                        {
                            float d = Vector2.Distance(new Vector2(x, y), new Vector2(11.5f, 11.5f));
                            if (d >= 5.5f && d <= 7.5f) tex.SetPixel(x, y, w);
                        }
                    }
                    FillRect(tex, 11, 2, 2, 4, w);
                    FillRect(tex, 11, 18, 2, 4, w);
                    FillRect(tex, 2, 11, 4, 2, w);
                    FillRect(tex, 18, 11, 4, 2, w);
                    break;

                case 2: // Bar Chart / Leaderboard
                    FillRect(tex, 4, 5, 4, 6, w);
                    FillRect(tex, 10, 5, 4, 11, w);
                    FillRect(tex, 16, 5, 4, 15, w);
                    FillRect(tex, 2, 3, 20, 2, w);
                    break;

                case 3: // Alliances (Two flags)
                    FillRect(tex, 5, 4, 2, 16, w);
                    FillTriangle(tex, new Vector2(7, 19), new Vector2(14, 16), new Vector2(7, 13), w);
                    FillRect(tex, 17, 4, 2, 16, w);
                    FillTriangle(tex, new Vector2(17, 17), new Vector2(10, 14), new Vector2(17, 11), w);
                    break;

                case 4: // Refresh / Circular Arrow
                    for (int y = 0; y < s; y++)
                    {
                        for (int x = 0; x < s; x++)
                        {
                            float d = Vector2.Distance(new Vector2(x, y), new Vector2(11.5f, 11.5f));
                            if (d >= 5.2f && d <= 7.8f && !(x < 12 && y < 10))
                            {
                                tex.SetPixel(x, y, w);
                            }
                        }
                    }
                    FillTriangle(tex, new Vector2(5, 12), new Vector2(12, 12), new Vector2(8, 18), w);
                    break;

                case 5: // Settings Gear
                    FillCircle(tex, 12, 12, 6.5f, w);
                    FillCircle(tex, 12, 12, 3.2f, Color.clear);
                    FillRect(tex, 10, 2, 4, 20, w);
                    FillRect(tex, 2, 10, 20, 4, w);
                    FillCircle(tex, 12, 12, 2.5f, Color.clear);
                    break;
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 24f);
            _topBarIcons[index] = sp;
            return sp;
        }

        public static Sprite GetHandshakeIcon()
        {
            if (_handshakeIcon != null) return _handshakeIcon;
            int s = 32;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;
            FillRect(tex, 4, 18, 8, 6, w);
            FillRect(tex, 20, 8, 8, 6, w);
            DrawThickLine(tex, new Vector2(10, 20), new Vector2(17, 13), 3.5f, w);
            DrawThickLine(tex, new Vector2(15, 12), new Vector2(22, 10), 3.5f, w);
            FillCircle(tex, 16, 15, 3.8f, w);
            tex.Apply();
            _handshakeIcon = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _handshakeIcon;
        }

        public static Sprite GetBrokenHeartIcon()
        {
            if (_brokenHeartIcon != null) return _brokenHeartIcon;
            int s = 32;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;
            FillCircle(tex, 11, 20, 6.2f, w);
            FillCircle(tex, 21, 20, 6.2f, w);
            FillTriangle(tex, new Vector2(5, 18), new Vector2(27, 18), new Vector2(16, 5), w);
            DrawThickLine(tex, new Vector2(16, 25), new Vector2(14, 19), 2.2f, Color.clear);
            DrawThickLine(tex, new Vector2(14, 19), new Vector2(18, 13), 2.2f, Color.clear);
            DrawThickLine(tex, new Vector2(18, 13), new Vector2(16, 6), 2.2f, Color.clear);
            tex.Apply();
            _brokenHeartIcon = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _brokenHeartIcon;
        }

        public static Sprite GetEmbargoIcon()
        {
            if (_embargoIcon != null) return _embargoIcon;
            int s = 32;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    if (d >= 8.5f && d <= 12.5f) tex.SetPixel(x, y, w);
                }
            }
            DrawThickLine(tex, new Vector2(8, 23), new Vector2(23, 8), 2.8f, w);
            tex.Apply();
            _embargoIcon = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _embargoIcon;
        }

        public static Sprite GetCoinIcon()
        {
            if (_coinIcon != null) return _coinIcon;
            int s = 32;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;
            FillCircle(tex, 16, 16, 11f, w);
            FillCircle(tex, 16, 16, 8.5f, Color.clear);
            FillRect(tex, 14, 8, 4, 16, w);
            FillRect(tex, 12, 14, 8, 4, w);
            tex.Apply();
            _coinIcon = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _coinIcon;
        }

        public static Sprite GetShieldIcon()
        {
            if (_shieldIcon != null) return _shieldIcon;
            int s = 32;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;
            for (int y = 5; y <= 26; y++)
            {
                float t = (y - 5f) / 21f;
                int halfW = y > 15 ? 10 : Mathf.RoundToInt(Mathf.Lerp(1.5f, 10f, t / 0.5f));
                for (int x = 16 - halfW; x <= 16 + halfW; x++)
                {
                    tex.SetPixel(x, y, w);
                }
            }
            FillRect(tex, 14, 11, 4, 10, Color.clear);
            FillRect(tex, 11, 14, 10, 4, Color.clear);
            tex.Apply();
            _shieldIcon = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _shieldIcon;
        }

        public static Sprite GetChatBubbleIcon()
        {
            if (_chatBubbleIcon != null) return _chatBubbleIcon;
            int s = 32;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;
            FillCircle(tex, 16, 17, 10.5f, w);
            FillTriangle(tex, new Vector2(8, 12), new Vector2(16, 12), new Vector2(6, 4), w);
            FillCircle(tex, 12, 17, 1.8f, Color.clear);
            FillCircle(tex, 16, 17, 1.8f, Color.clear);
            FillCircle(tex, 20, 17, 1.8f, Color.clear);
            tex.Apply();
            _chatBubbleIcon = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _chatBubbleIcon;
        }

        public static Sprite GetSearchIcon()
        {
            if (_searchIcon != null) return _searchIcon;
            int s = 32;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(13.5f, 18.5f));
                    if (d >= 6.5f && d <= 9.5f) tex.SetPixel(x, y, w);
                }
            }
            DrawThickLine(tex, new Vector2(19, 13), new Vector2(27, 5), 3.2f, w);
            tex.Apply();
            _searchIcon = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _searchIcon;
        }

        public static TMPro.TMP_FontAsset GetDefaultFont()
        {
            if (_fallbackFont != null) return _fallbackFont;
            _fallbackFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (_fallbackFont != null) return _fallbackFont;
            if (TMPro.TMP_Settings.instance != null && TMPro.TMP_Settings.defaultFontAsset != null)
            {
                _fallbackFont = TMPro.TMP_Settings.defaultFontAsset;
                return _fallbackFont;
            }
            Font osFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
            if (osFont != null)
            {
                _fallbackFont = TMPro.TMP_FontAsset.CreateFontAsset(osFont);
            }
            return _fallbackFont;
        }

        public static Sprite GetWhiteSquare()
        {
            if (_whiteSquare != null) return _whiteSquare;
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color32[] cols = new Color32[16];
            for (int i = 0; i < 16; i++) cols[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(cols);
            tex.Apply();
            _whiteSquare = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            return _whiteSquare;
        }

        public static Sprite GetRoundedPanelSprite()
        {
            if (_roundedPanel != null) return _roundedPanel;
            int size = 32;
            float radius = 8f;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0f;
                    if (x < radius) dx = radius - x;
                    else if (x > size - 1 - radius) dx = x - (size - 1 - radius);

                    float dy = 0f;
                    if (y < radius) dy = radius - y;
                    else if (y > size - 1 - radius) dy = y - (size - 1 - radius);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            _roundedPanel = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
            return _roundedPanel;
        }

        public static Sprite GetCircleSprite(bool ringOnly = false)
        {
            if (!ringOnly && _circleFill != null) return _circleFill;
            if (ringOnly && _circleRing != null) return _circleRing;

            int size = 48;
            float center = (size - 1) * 0.5f;
            float outerR = size * 0.46f;
            float innerR = size * 0.34f;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float a = Mathf.Clamp01(outerR + 0.8f - dist);
                    if (ringOnly)
                    {
                        float innerA = Mathf.Clamp01(dist - innerR + 0.8f);
                        a *= innerA;
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 48f);
            if (ringOnly) _circleRing = sp;
            else _circleFill = sp;
            return sp;
        }

        public static Sprite GetCrownSprite()
        {
            if (_crownSprite != null) return _crownSprite;
            int s = 24;
            Texture2D tex = CreateBlankTex(s);
            // Base band
            FillRect(tex, 3, 4, 18, 4, Color.white);
            // Three spikes of crown
            FillTriangle(tex, new Vector2(3, 8), new Vector2(9, 8), new Vector2(4, 19), Color.white);
            FillTriangle(tex, new Vector2(7, 8), new Vector2(17, 8), new Vector2(12, 20), Color.white);
            FillTriangle(tex, new Vector2(15, 8), new Vector2(21, 8), new Vector2(20, 19), Color.white);
            tex.Apply();
            _crownSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 24f);
            return _crownSprite;
        }

        public static Sprite GetShieldSprite()
        {
            if (_shieldSprite != null) return _shieldSprite;
            int s = 24;
            Texture2D tex = CreateBlankTex(s);
            for (int y = 3; y <= 20; y++)
            {
                float t = (y - 3f) / 17f;
                int halfW = y > 10 ? 8 : Mathf.RoundToInt(Mathf.Lerp(1f, 8f, t / 0.42f));
                for (int x = 12 - halfW; x <= 12 + halfW; x++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
            tex.Apply();
            _shieldSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 24f);
            return _shieldSprite;
        }

        public static Sprite GetActionIcon(StructureType type)
        {
            if (_actionIcons.TryGetValue(type, out Sprite cached) && cached != null)
                return cached;

            int s = 32;
            Texture2D tex = CreateBlankTex(s);
            Color w = Color.white;

            switch (type)
            {
                case StructureType.City:
                    // Skyscraper / City towers silhouette (like screenshot icon 1)
                    FillRect(tex, 5, 4, 6, 15, w);
                    FillRect(tex, 12, 4, 8, 23, w);
                    FillRect(tex, 21, 4, 6, 18, w);
                    // Windows cutout
                    FillRect(tex, 14, 21, 2, 2, Color.clear);
                    FillRect(tex, 17, 21, 2, 2, Color.clear);
                    FillRect(tex, 14, 16, 2, 2, Color.clear);
                    FillRect(tex, 17, 16, 2, 2, Color.clear);
                    break;

                case StructureType.Port:
                    // Anchor / Port crane icon
                    FillRect(tex, 15, 7, 3, 18, w);
                    FillRect(tex, 10, 19, 13, 3, w);
                    FillCircle(tex, 16, 25, 3, w);
                    // Curved bottom flukes
                    FillRect(tex, 7, 6, 19, 3, w);
                    FillRect(tex, 6, 8, 3, 5, w);
                    FillRect(tex, 24, 8, 3, 5, w);
                    break;

                case StructureType.Factory:
                    // Industrial Factory with sawtooth roof & chimneys
                    FillRect(tex, 5, 4, 22, 11, w);
                    FillRect(tex, 6, 15, 4, 11, w);
                    FillRect(tex, 12, 15, 4, 9, w);
                    FillTriangle(tex, new Vector2(16, 15), new Vector2(22, 15), new Vector2(22, 21), w);
                    FillTriangle(tex, new Vector2(22, 15), new Vector2(27, 15), new Vector2(27, 21), w);
                    break;

                case StructureType.DefensePost:
                    // Shield / Bastion emblem
                    for (int y = 5; y <= 26; y++)
                    {
                        float t = (y - 5f) / 21f;
                        int halfW = y > 14 ? 9 : Mathf.RoundToInt(Mathf.Lerp(1f, 9f, t / 0.45f));
                        for (int x = 16 - halfW; x <= 16 + halfW; x++)
                        {
                            tex.SetPixel(x, y, w);
                        }
                    }
                    break;

                case StructureType.SAMLauncher:
                    // Anti-air SAM turret pointing diagonally up-right
                    FillRect(tex, 6, 5, 20, 4, w);
                    FillRect(tex, 12, 9, 8, 5, w);
                    DrawThickLine(tex, new Vector2(10, 14), new Vector2(24, 26), 3.2f, w);
                    DrawThickLine(tex, new Vector2(14, 11), new Vector2(27, 22), 2.5f, w);
                    break;

                case StructureType.MissileSilo:
                    // Vertical ICBM Rocket in silo
                    FillRect(tex, 6, 4, 20, 4, w);
                    FillRect(tex, 13, 8, 6, 14, w);
                    FillTriangle(tex, new Vector2(12, 22), new Vector2(20, 22), new Vector2(16, 29), w);
                    FillTriangle(tex, new Vector2(9, 8), new Vector2(13, 8), new Vector2(13, 14), w);
                    FillTriangle(tex, new Vector2(19, 8), new Vector2(23, 8), new Vector2(19, 14), w);
                    break;

                case StructureType.AtomBomb:
                    // Tactical Bomb / Radiation symbol
                    FillCircle(tex, 16, 16, 4, w);
                    DrawThickLine(tex, new Vector2(16, 16), new Vector2(8, 25), 3.2f, w);
                    DrawThickLine(tex, new Vector2(16, 16), new Vector2(24, 25), 3.2f, w);
                    DrawThickLine(tex, new Vector2(16, 16), new Vector2(16, 6), 3.2f, w);
                    break;

                case StructureType.HydrogenBomb:
                    // Mushroom cloud silhouette
                    FillRect(tex, 7, 4, 18, 3, w);
                    FillRect(tex, 13, 7, 6, 10, w);
                    FillCircle(tex, 16, 21, 8, w);
                    FillCircle(tex, 10, 19, 5, w);
                    FillCircle(tex, 22, 19, 5, w);
                    break;

                case StructureType.Warship:
                    // Battleship hull + superstructure
                    for (int y = 7; y <= 13; y++)
                    {
                        int trim = (13 - y);
                        FillRect(tex, 4 + trim, y, 24 - trim * 2, 1, w);
                    }
                    FillRect(tex, 11, 14, 9, 5, w);
                    FillRect(tex, 14, 19, 3, 5, w);
                    DrawThickLine(tex, new Vector2(20, 15), new Vector2(26, 18), 2f, w);
                    break;

                case StructureType.Alliance:
                    // Handshake / Laurel / Flag icon
                    FillRect(tex, 7, 5, 3, 22, w);
                    FillRect(tex, 10, 16, 15, 10, w);
                    break;
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            _actionIcons[type] = sp;
            return sp;
        }

        private static Texture2D CreateBlankTex(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            Color32[] clear = new Color32[size * size];
            tex.SetPixels32(clear);
            return tex;
        }

        private static void FillRect(Texture2D tex, int rx, int ry, int rw, int rh, Color col)
        {
            for (int y = ry; y < ry + rh; y++)
            {
                for (int x = rx; x < rx + rw; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                        tex.SetPixel(x, y, col);
                }
            }
        }

        private static void FillCircle(Texture2D tex, int cx, int cy, float radius, Color col)
        {
            int r = Mathf.CeilToInt(radius);
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) <= radius)
                            tex.SetPixel(x, y, col);
                    }
                }
            }
        }

        private static void FillTriangle(Texture2D tex, Vector2 a, Vector2 b, Vector2 c, Color col)
        {
            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, tex.width - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, tex.width - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, tex.height - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, tex.height - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    if (PointInTriangle(p, a, b, c))
                        tex.SetPixel(x, y, col);
                }
            }
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static void DrawThickLine(Texture2D tex, Vector2 a, Vector2 b, float thickness, Color col)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b) * 2f);
            float r = thickness * 0.5f;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, (float)i / Mathf.Max(1, steps));
                FillCircle(tex, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), r, col);
            }
        }
    }
}
