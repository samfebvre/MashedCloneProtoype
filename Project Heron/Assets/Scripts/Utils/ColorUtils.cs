using UnityEngine;

namespace DefaultNamespace.Utils
{
    public static class ColorUtils
    {

        #region Public Methods

        public static Color ConvertSystemDrawingColorToUnityColor( System.Drawing.Color color )
        {
            float r = ConvertSystemDrawingColorComponentToUnityColorComponent( color.R );
            float g = ConvertSystemDrawingColorComponentToUnityColorComponent( color.G );
            float b = ConvertSystemDrawingColorComponentToUnityColorComponent( color.B );
            float a = ConvertSystemDrawingColorComponentToUnityColorComponent( color.A );

            return new Color( r, g, b, a );

            float ConvertSystemDrawingColorComponentToUnityColorComponent( byte byteComponent )
            {
                return byteComponent / 255.0f;
            }
        }

        public static Color GetWhiteOrBlackContrastColor( Color backgroundColor )
        {
            // Choose either white or black text color depending on the background color
            double sumOfParts = backgroundColor.r * 0.299 + backgroundColor.g * 0.587 + backgroundColor.b * 0.114;
            return sumOfParts > 0.186 ? Color.black : Color.white;
        }

        public static Color RandomPleasantColor() => Random.ColorHSV( 0.0f, 1.0f, 0.5f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f );

        #endregion

    }
}