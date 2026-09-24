using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace GameFramework.Gameplay
{
    public static class UIFontUtility
    {
        private const string FontResourcePath = "Fonts/NotoSansKR-VF";

        private static Font _cachedLegacyFont;
        private static TMP_FontAsset _cachedTmpFont;

        public static void ApplyToHierarchy(Transform root)
        {
            if (root == null)
                return;

            var tmpFont = TmpFontAsset;
            if (tmpFont != null)
            {
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                    text.font = tmpFont;
            }

            var legacyFont = LegacyFont;
            if (legacyFont != null)
            {
                foreach (var text in root.GetComponentsInChildren<Text>(true))
                    text.font = legacyFont;
            }
        }

        public static TMP_FontAsset TmpFontAsset
        {
            get
            {
                if (_cachedTmpFont != null)
                    return _cachedTmpFont;

                var sourceFont = LegacyFont;
                if (sourceFont == null)
                    return null;

                _cachedTmpFont = TMP_FontAsset.CreateFontAsset(
                    sourceFont,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024);

                if (_cachedTmpFont != null)
                {
                    _cachedTmpFont.name = "NotoSansKR-VF TMP Runtime";
                    _cachedTmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    _cachedTmpFont.isMultiAtlasTexturesEnabled = true;
                }

                return _cachedTmpFont;
            }
        }

        public static Font LegacyFont
        {
            get
            {
                if (_cachedLegacyFont != null)
                    return _cachedLegacyFont;

                _cachedLegacyFont = Resources.Load<Font>(FontResourcePath);
                if (_cachedLegacyFont == null)
                    _cachedLegacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _cachedLegacyFont;
            }
        }
    }
}
