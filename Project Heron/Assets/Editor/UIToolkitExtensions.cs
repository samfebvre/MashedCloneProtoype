using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    public static class UIToolkitExtensions
    {
        public static Vector2 TranslateAsV2( this VisualElement elem )
        {
            return new Vector2(elem.style.translate.value.x.value, elem.style.translate.value.y.value);
        }
        
        public static void TranslateFromV2( this VisualElement elem, Vector2 value )
        {
            elem.style.translate = new StyleTranslate
            {
                value   = new Translate
                {
                    x = value.x,
                    y = value.y,
                    z = 0
                },
                keyword = StyleKeyword.Undefined,
            };
        }
        
        public static void RotateFromFloat( this VisualElement elem, float value )
        {
            elem.style.rotate = new StyleRotate
            {
                value   = new Rotate(value),
                keyword = StyleKeyword.Undefined
            };
            
            //Debug.Log( $"New Rot: {elem.style.rotate.value.angle.value}" );
        }
        
        public static float RotateAsFloat( this VisualElement elem )
        {
            return elem.style.rotate.value.angle.value;
        }
    }
}