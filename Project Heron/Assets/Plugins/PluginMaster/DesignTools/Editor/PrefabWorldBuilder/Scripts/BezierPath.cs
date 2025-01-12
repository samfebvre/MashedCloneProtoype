using System.Collections.Generic;
using UnityEngine;

namespace PluginMaster
{
    public static class BezierPath
    {

        #region Statics and Constants

        private const float MINIMUM_SQR_DISTANCE = 0.01f;
        private const float DIVISION_THRESHOLD   = -0.99f;

        #endregion

        #region Public Methods

        public static Vector3[] GetBezierPoints( Vector3[] segmentPoints, float[] scales )
        {
            Vector3[] controlPoints = Interpolate( segmentPoints, scales );
            int       curveCount    = ( controlPoints.Length - 1 ) / 3;
            Vector3[] pathPoints    = GetDrawingPoints( controlPoints, curveCount );
            return pathPoints;
        }

        public static Vector3[] GetDrawingPoints( Vector3[] controlPoints, int curveCount )
        {
            List<Vector3> drawingPoints = new List<Vector3>();
            for ( int curveIndex = 0; curveIndex < curveCount; ++curveIndex )
            {
                List<Vector3> bezierCurveDrawingPoints = FindDrawingPoints( curveIndex, controlPoints );
                if ( curveIndex != 0 )
                {
                    bezierCurveDrawingPoints.RemoveAt( 0 );
                }

                drawingPoints.AddRange( bezierCurveDrawingPoints );
            }

            return drawingPoints.ToArray();
        }

        #endregion

        #region Private Methods

        private static Vector3 CalculateBezierPoint( int curveIndex, Vector3[] controlPoints, float t )
        {
            int     nodeIndex = curveIndex * 3;
            Vector3 p0        = controlPoints[ nodeIndex ];
            Vector3 p1        = controlPoints[ nodeIndex + 1 ];
            Vector3 p2        = controlPoints[ nodeIndex + 2 ];
            Vector3 p3        = controlPoints[ nodeIndex + 3 ];
            return CalculateBezierPoint( t, p0, p1, p2, p3 );
        }

        private static Vector3 CalculateBezierPoint( float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3 )
        {
            float   u   = 1 - t;
            float   tt  = t   * t;
            float   uu  = u   * u;
            float   uuu = uu  * u;
            float   ttt = tt  * t;
            Vector3 p   = uuu * p0;  //first term
            p += 3   * uu * t  * p1; //second term
            p += 3   * u  * tt * p2; //third term
            p += ttt * p3;           //fourth term
            return p;
        }

        private static List<Vector3> FindDrawingPoints( int curveIndex, Vector3[] controlPoints )
        {
            List<Vector3> pointList = new List<Vector3>();
            Vector3       left      = CalculateBezierPoint( curveIndex, controlPoints, 0 );
            Vector3       right     = CalculateBezierPoint( curveIndex, controlPoints, 1 );
            pointList.Add( left );
            pointList.Add( right );
            FindDrawingPoints( curveIndex, 0, 1, pointList, controlPoints, 1 );
            return pointList;
        }

        private static int FindDrawingPoints( int           curveIndex, float     t0,            float t1,
                                              List<Vector3> pointList,  Vector3[] controlPoints, int   insertionIndex )
        {
            Vector3 left  = CalculateBezierPoint( curveIndex, controlPoints, t0 );
            Vector3 right = CalculateBezierPoint( curveIndex, controlPoints, t1 );
            if ( ( left - right ).sqrMagnitude < MINIMUM_SQR_DISTANCE )
            {
                return 0;
            }

            float   tMid           = ( t0 + t1 ) / 2;
            Vector3 mid            = CalculateBezierPoint( curveIndex, controlPoints, tMid );
            Vector3 leftDirection  = ( left  - mid ).normalized;
            Vector3 rightDirection = ( right - mid ).normalized;
            if ( Vector3.Dot( leftDirection, rightDirection ) < DIVISION_THRESHOLD
                 && Mathf.Abs( tMid - 0.5f )                  > 0.0001f )
            {
                return 0;
            }

            int pointsAddedCount = 0;
            pointsAddedCount += FindDrawingPoints( curveIndex, t0, tMid, pointList, controlPoints, insertionIndex );
            pointList.Insert( insertionIndex + pointsAddedCount, mid );
            pointsAddedCount++;
            pointsAddedCount += FindDrawingPoints( curveIndex, tMid, t1, pointList,
                controlPoints,                                 insertionIndex + pointsAddedCount );
            return pointsAddedCount;
        }

        private static Vector3[] Interpolate( Vector3[] segmentPoints, float[] scales )
        {
            List<Vector3> controlPoints = new List<Vector3>();
            if ( segmentPoints.Length < 2 )
            {
                return segmentPoints;
            }

            for ( int i = 0; i < segmentPoints.Length; i++ )
            {
                if ( i == 0 )
                {
                    Vector3 p1      = segmentPoints[ i ];
                    Vector3 p2      = segmentPoints[ i + 1 ];
                    Vector3 tangent = p2 - p1;
                    Vector3 q1      = p1 + scales[ i ] * tangent;
                    controlPoints.Add( p1 );
                    controlPoints.Add( q1 );
                }
                else if ( i == segmentPoints.Length - 1 )
                {
                    Vector3 p0      = segmentPoints[ i - 1 ];
                    Vector3 p1      = segmentPoints[ i ];
                    Vector3 tangent = p1 - p0;
                    Vector3 q0      = p1 - scales[ i ] * tangent;
                    controlPoints.Add( q0 );
                    controlPoints.Add( p1 );
                }
                else
                {
                    Vector3 p0      = segmentPoints[ i - 1 ];
                    Vector3 p1      = segmentPoints[ i ];
                    Vector3 p2      = segmentPoints[ i + 1 ];
                    Vector3 tangent = ( p2 - p0 ).normalized;
                    Vector3 q0      = p1 - scales[ i ] * tangent * ( p1 - p0 ).magnitude;
                    Vector3 q1      = p1 + scales[ i ] * tangent * ( p2 - p1 ).magnitude;
                    controlPoints.Add( q0 );
                    controlPoints.Add( p1 );
                    controlPoints.Add( q1 );
                }
            }

            return controlPoints.ToArray();
        }

        #endregion

    }
}