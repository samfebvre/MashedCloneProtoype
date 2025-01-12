using UnityEngine;

namespace PluginMaster
{
    public static class PlaceOnSurfaceUtils
    {
        public class PlaceOnSurfaceData
        {

            #region Public Properties

            public LayerMask mask                { get; set; } = ~0;
            public Vector3   objectOrientation   { get; set; } = Vector3.down;
            public bool      placeOnColliders    { get; set; } = true;
            public Vector3   projectionDirection { get; set; } = Vector3.down;

            public Space projectionDirectionSpace { get; set; } = Space.Self;

            public bool  rotateToSurface { get; set; } = true;
            public float surfaceDistance { get; set; } = 0f;

            #endregion

            #region Private Fields

            #endregion

        }
    }
}