using System.Collections.Generic;
using UnityEngine;

namespace Editor.Custom
{
    public static class EditorCustomUtility
    {
        private static readonly Dictionary<Color, Texture2D> textures = new Dictionary<Color, Texture2D>();

        public static void DrawRect(Rect rect, Color color)
        {
            var dotTexture = GetDotTexture(color);
            if (dotTexture != null)
            {
                GUI.DrawTexture(rect, dotTexture);
            }
        }

        public static void ClearTextures()
        {
            textures.Clear();
        }
        private static Texture GetDotTexture(Color color)
        {
            if (textures.TryGetValue(color, out var tex))
            {
                return tex;
            }


            tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();

            textures.Add(color, tex);

            return tex;
        }
    }
}