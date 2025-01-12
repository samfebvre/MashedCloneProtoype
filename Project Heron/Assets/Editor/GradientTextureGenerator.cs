using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class GradientTextureGenerator
    {
        public static Texture2D GradientToTexture(
            int      width,
            int      height,
            Gradient gradient )
        {
            Texture2D texture = new Texture2D( width, height, TextureFormat.RGBA32, false );
            for ( int x = 0; x < width; x++ )
            {
                float t = x / (float)width;
                Color color = gradient.Evaluate( t );
                for ( int y = 0; y < height; y++ )
                {
                    texture.SetPixel( x, y, color );
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D GradientToTexture_WithRotation(
            int      width,
            int      height,
            Gradient gradient,
            float    rotation )
        {
            // Create a gradient texture such that the gradient is rotated by the specified angle
            Texture2D texture = new Texture2D( width, height, TextureFormat.RGBA32, false );
            for ( int x = 0; x < width; x++ )
            {
                float t = x / (float)width;
                Color color = gradient.Evaluate( t );
                for ( int y = 0; y < height; y++ )
                {
                    float u = x / (float)width - 0.5f;
                    float v = y / (float)height - 0.5f;
                    float uPrime = u * Mathf.Cos( rotation ) - v * Mathf.Sin( rotation );
                    float vPrime = u * Mathf.Sin( rotation ) + v * Mathf.Cos( rotation );
                    float tPrime = uPrime + 0.5f;
                    texture.SetPixel( x, y, gradient.Evaluate( tPrime ) );
                }
            }
            
            texture.Apply();
            return texture;
        }

        public static void GenerateTextureAssetForGradient(
            string   path,
            int      width,
            int      height,
            Gradient gradient )
        {
            // Convert the gradient to a texture
            Texture2D texture = GradientToTexture( width, height, gradient );

            // Save the texture to disk
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes( path, bytes );

            // Modify the import settings
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset( path );
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath( path );
            if ( importer == null )
            {
                Debug.LogError( "Failed to get TextureImporter for path: " + path );
                return;
            }

            importer.textureType         = TextureImporterType.Default;
            importer.alphaSource         = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture         = true;
            importer.mipmapEnabled       = false;
            importer.wrapMode            = TextureWrapMode.Clamp;
            importer.filterMode          = FilterMode.Bilinear;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            AssetDatabase.WriteImportSettingsIfDirty( path );
            AssetDatabase.Refresh();
        }
    }
}