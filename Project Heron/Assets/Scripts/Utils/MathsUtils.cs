using UnityEngine;

namespace DefaultNamespace.Utils
{
    public static class MathsUtils
    {

        #region Public Methods

        public static bool IntersectionPointOfTwoLines( Line line1, Line line2, out Vector3 outVec, bool flatten = false ) =>
            IntersectionPointOfTwoVectors( line1.Centre, line1.Direction, line2.Centre, line2.Direction, out outVec, flatten );

        /// <summary>
        ///     Returns true if the two vectors intersect, and sets the intersection point in outVec
        /// </summary>
        public static bool IntersectionPointOfTwoVectors( Vector3     linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2,
                                                          out Vector3 outVec,     bool    flatten = false )
        {
            if ( flatten )
            {
                lineVec1.y   = 0;
                lineVec2.y   = 0;
                linePoint1.y = 0;
                linePoint2.y = 0;
            }

            Vector3 lineVec3      = linePoint2 - linePoint1;
            Vector3 crossVec1And2 = Vector3.Cross( lineVec1, lineVec2 );
            Vector3 crossVec3And2 = Vector3.Cross( lineVec3, lineVec2 );

            float planarFactor = Vector3.Dot( lineVec3, crossVec1And2 );

            //is coplanar, and not parallel
            if ( Mathf.Abs( planarFactor )     < 0.0001f
                 && crossVec1And2.sqrMagnitude > 0.0001f )
            {
                float s = Vector3.Dot( crossVec3And2, crossVec1And2 ) / crossVec1And2.sqrMagnitude;
                outVec = linePoint1 + lineVec1 * s;
                return true;
            }

            outVec = Vector3.zero;
            return false;
        }

        public static Vector3 NearestPointOnLineToPoint( Vector3 linePnt, Vector3 lineDir, Vector3 pnt, float lineLength )
        {
            lineDir.Normalize(); //this needs to be a unit vector
            Vector3 v            = pnt - linePnt;
            float   d            = Vector3.Dot( v, lineDir );
            Vector3 nearestPoint = linePnt + lineDir * d;

            // if ( DEBUG )
            // {
            //     Debug.DrawLine( pnt, nearestPoint, Color.red );
            // }

            if ( Vector3.Distance( nearestPoint, linePnt ) > lineLength / 2.0f )
            {
                Vector3 difVec = nearestPoint - linePnt;
                difVec.Normalize();
                nearestPoint = linePnt + difVec * lineLength / 2.0f;

                // if ( DEBUG )
                // {
                //     Debug.DrawLine( pnt, nearestPoint, Color.green );
                // }
            }

            return nearestPoint;
        }

        public static Vector3 NearestPointOnLineToPoint( Line line, Vector3 pnt ) =>
            NearestPointOnLineToPoint( line.Centre, line.Direction, pnt, line.Length );

        public static Quaternion QuaternionSmoothDamp( Quaternion rot, Quaternion target, ref Quaternion vel, float time, float maxSpeed )
        {
            if ( Time.deltaTime < Mathf.Epsilon )
            {
                return rot;
            }

            // account for double-cover
            float dot   = Quaternion.Dot( rot, target );
            float multi = dot > 0f ? 1f : -1f;
            target.x *= multi;
            target.y *= multi;
            target.z *= multi;
            target.w *= multi;

            // smooth damp (nlerp approx)
            Vector4 result = new Vector4(
                Mathf.SmoothDamp( rot.x, target.x, ref vel.x, time, maxSpeed ),
                Mathf.SmoothDamp( rot.y, target.y, ref vel.y, time, maxSpeed ),
                Mathf.SmoothDamp( rot.z, target.z, ref vel.z, time, maxSpeed ),
                Mathf.SmoothDamp( rot.w, target.w, ref vel.w, time, maxSpeed )
            ).normalized;

            // ensure vel is tangent
            Vector4 velError = Vector4.Project( new Vector4( vel.x, vel.y, vel.z, vel.w ), result );
            vel.x -= velError.x;
            vel.y -= velError.y;
            vel.z -= velError.z;
            vel.w -= velError.w;

            return new Quaternion( result.x, result.y, result.z, result.w );
        }

        #endregion

        public struct Line
        {

            #region Public Fields

            public Vector3 End;
            public Vector3 Start;

            #endregion

            #region Public Properties

            public Vector3 Centre    => ( Start + End ) / 2;
            public Vector3 Direction => DivVec.normalized;
            public Vector3 DivVec    => End - Start;
            public float   Length    => DivVec.magnitude;

            #endregion

            #region Public Constructors

            public Line( Vector3 end, Vector3 start )
            {
                End   = end;
                Start = start;
            }

            #endregion

            #region Public Methods

            public void DrawLine( Color color = default )
            {
                Debug.DrawLine( Start, End, color );
            }

            #endregion

        }
    }
}