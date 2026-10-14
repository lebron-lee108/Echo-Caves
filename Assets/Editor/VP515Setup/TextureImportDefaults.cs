using UnityEditor;
using UnityEngine;

namespace VP515.Editor
{
    // New images only: reimporting an existing artist asset never overrides its chosen settings.
    public sealed class TextureImportDefaults : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing) return;

            if (assetPath.StartsWith("Assets/_VP515/Art/Pixel/"))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 24;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
            }
            else if (assetPath.StartsWith("Assets/_VP515/Art/Backgrounds/"))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
            }
        }
    }
}
