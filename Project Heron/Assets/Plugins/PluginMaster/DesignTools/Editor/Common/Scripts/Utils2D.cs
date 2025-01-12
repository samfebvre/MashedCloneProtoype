using System.Linq;
using UnityEngine;

namespace PluginMaster
{
    public static class Utils2D
    {

        #region Public Methods

        public static bool Is2DAsset( GameObject obj )
        {
            SpriteRenderer[] sprites = obj.GetComponentsInChildren<SpriteRenderer>()
                                          .Where( s => s.enabled && s.sprite != null && s.gameObject.activeSelf ).ToArray();
            return sprites.Length > 0;
        }

        #endregion

    }
}