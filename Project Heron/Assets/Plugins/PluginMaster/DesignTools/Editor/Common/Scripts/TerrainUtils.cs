using UnityEngine;

namespace PluginMaster
{
    public static class TerrainUtils
    {

        #region Public Methods

        public static Vector3[] GetCorners( Terrain terrain, Space space )
        {
            TerrainData terrainData = terrain.terrainData;
            Vector3     origin      = space == Space.Self ? Vector3.zero : terrain.transform.position;
            int         max         = terrainData.heightmapResolution - 1;
            Vector3     scale       = terrainData.heightmapScale;
            Vector3[] corners = new[]
            {
                origin + new Vector3( 0,             terrainData.GetHeight( 0,   0 ),   0 ),
                origin + new Vector3( max * scale.x, terrainData.GetHeight( max, 0 ),   0 ),
                origin + new Vector3( max * scale.x, terrainData.GetHeight( max, max ), max * scale.z ),
                origin + new Vector3( 0,             terrainData.GetHeight( 0,   max ), max * scale.z ),
            };
            return corners;
        }

        #endregion

    }
}