using Shapes;
using UnityEngine;

namespace DefaultNamespace
{
    public class BezierDraw : MonoBehaviour
    {

        #region Serialized

        public Vector3 a             = new Vector3( 0,  0, 0 );
        public Vector3 b             = new Vector3( -2, 1, 1 );
        public Vector3 c             = new Vector3( 2,  1, 3 );
        public Vector3 d             = new Vector3( 0,  0, 4 );
        public float   lineThickness = 0.05f;
        public bool    polyline;
        public int     pointCount = 48;

        #endregion

        #region Unity Functions

        private void OnDrawGizmos()
        {
            if ( polyline )
            {
                // Drawing using billboard polylines
                Draw.PolylineGeometry = PolylineGeometry.Billboard;
                using ( PolylinePath path = new PolylinePath() )
                {
                    path.AddPoint( a );
                    path.BezierTo( b, c, d, pointCount );
                    Draw.Polyline( path, lineThickness, PolylineJoins.Simple );
                }

                Draw.DiscGeometry = DiscGeometry.Billboard;
                Draw.Disc( a, lineThickness / 2.0f );
                Draw.Disc( d, lineThickness / 2.0f );
            }
            else
            {
                // Drawing using 3D lines
                Draw.LineGeometry = LineGeometry.Volumetric3D;
                Vector3[] pts = new Vector3[ pointCount ];
                for ( int i = 0; i < pointCount; i++ )
                {
                    pts[ i ] = GetBezierPt( a, b, c, d, i / ( pointCount - 1f ) );
                }

                for ( int i = 0; i < pointCount - 1; i++ )
                {
                    Draw.Line( pts[ i ], pts[ i + 1 ], lineThickness, LineEndCap.Round );
                }
            }
        }

        #endregion

        #region Private Methods

        private static Vector3 GetBezierPt( Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t )
        {
            float omt  = 1f - t;
            float omt2 = omt  * omt;
            return a * ( omt2 * omt ) + b * ( 3f * omt2 * t ) + c * ( 3f * omt * t * t ) + d * ( t * t * t );
        }

        #endregion

    }
}