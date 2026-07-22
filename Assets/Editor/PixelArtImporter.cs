using UnityEditor;
using UnityEngine;

namespace DoNotOpen.EditorTools
{
    public sealed class PixelArtImporter : AssetPostprocessor
    {
        private const string PixelArtPath = "Assets/Resources/PixelArt/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(PixelArtPath))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
        }
    }
}
