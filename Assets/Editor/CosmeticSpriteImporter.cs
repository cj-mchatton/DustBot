#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DustBot.Editor
{
    /// <summary>Keep cosmetic art crisp and inexpensive on iPhone.</summary>
    public sealed class CosmeticSpriteImporter : AssetPostprocessor
    {
        private const string CosmeticPath = "Assets/Resources/Sprites/Cosmetics/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(CosmeticPath, System.StringComparison.Ordinal))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 512;
            importer.isReadable = false;
        }
    }
}
#endif
