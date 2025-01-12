using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PluginMaster
{
    public class BrushstrokeItem : IEquatable<BrushstrokeItem>
    {

        #region Public Fields

        public readonly Vector3                additionalAngle = Vector3.zero;
        public readonly bool                   flipX;
        public readonly bool                   flipY;
        public readonly Vector3                scaleMultiplier = Vector3.zero;
        public readonly MultibrushItemSettings settings;
        public readonly float                  surfaceDistance;
        public          Vector3                nextTangentPosition = Vector3.zero;
        public          Vector3                tangentPosition     = Vector3.zero;

        #endregion

        #region Public Constructors

        public BrushstrokeItem( MultibrushItemSettings settings,        Vector3 tangentPosition,
                                Vector3                additionalAngle, Vector3 scaleMultiplier, bool flipX, bool flipY, float surfaceDistance )
        {
            this.settings        = settings;
            this.tangentPosition = tangentPosition;
            this.additionalAngle = additionalAngle;
            this.scaleMultiplier = scaleMultiplier;
            nextTangentPosition  = tangentPosition;
            this.flipX           = flipX;
            this.flipY           = flipY;
            this.surfaceDistance = surfaceDistance;
        }

        #endregion

        #region Public Methods

        public BrushstrokeItem Clone()
        {
            BrushstrokeItem clone = new BrushstrokeItem( settings, tangentPosition, additionalAngle,
                scaleMultiplier, flipX, flipY, surfaceDistance );
            clone.nextTangentPosition = nextTangentPosition;
            return clone;
        }

        public bool Equals( BrushstrokeItem other ) =>
            settings               == other.settings
            && tangentPosition     == other.tangentPosition
            && additionalAngle     == other.additionalAngle
            && scaleMultiplier     == other.scaleMultiplier
            && nextTangentPosition == other.nextTangentPosition;

        public override bool Equals( object obj ) => obj is BrushstrokeItem other && Equals( other );

        public override int GetHashCode()
        {
            int hashCode = 861157388;
            hashCode = hashCode * -1521134295
                       + EqualityComparer<MultibrushItemSettings>.Default.GetHashCode( settings );
            hashCode = hashCode * -1521134295 + tangentPosition.GetHashCode();
            hashCode = hashCode * -1521134295 + additionalAngle.GetHashCode();
            hashCode = hashCode * -1521134295 + scaleMultiplier.GetHashCode();
            hashCode = hashCode * -1521134295 + nextTangentPosition.GetHashCode();
            hashCode = hashCode * -1521134295 + flipX.GetHashCode();
            hashCode = hashCode * -1521134295 + flipY.GetHashCode();
            hashCode = hashCode * -1521134295 + surfaceDistance.GetHashCode();
            return hashCode;
        }

        public static bool operator ==( BrushstrokeItem lhs, BrushstrokeItem rhs ) => lhs.Equals( rhs );
        public static bool operator !=( BrushstrokeItem lhs, BrushstrokeItem rhs ) => !lhs.Equals( rhs );

        #endregion

    }

    public static class BrushstrokeManager
    {

        #region Statics and Constants

        private static List<BrushstrokeItem> _brushstroke
            = new List<BrushstrokeItem>();

        private static int _currentPinIdx;

        #endregion

        #region Public Properties

        public static BrushstrokeItem[] brushstroke => _brushstroke.ToArray();

        public static BrushstrokeItem[] brushstrokeClone
        {
            get
            {
                BrushstrokeItem[] clone = new BrushstrokeItem[ _brushstroke.Count ];
                for ( int i = 0; i < clone.Length; ++i )
                {
                    clone[ i ] = _brushstroke[ i ].Clone();
                }

                return clone;
            }
        }

        public static int itemCount => _brushstroke.Count;

        #endregion

        #region Public Methods

        public static bool BrushstrokeEqual( BrushstrokeItem[] lhs, BrushstrokeItem[] rhs )
        {
            if ( lhs.Length != rhs.Length )
            {
                return false;
            }

            for ( int i = 0; i < lhs.Length; ++i )
            {
                if ( lhs[ i ] != rhs[ i ] )
                {
                    return false;
                }
            }

            return true;
        }

        public static void ClearBrushstroke() => _brushstroke.Clear();

        public static float GetLineSpacing( int itemIdx, LineSettings settings, Vector3 scale )
        {
            float spacing = 0;
            if ( itemIdx >= 0 )
            {
                spacing = settings.spacing;
            }

            if ( settings.spacingType == LineSettings.SpacingType.BOUNDS
                 && itemIdx           >= 0 )
            {
                MultibrushItemSettings item = PaletteManager.selectedBrush.items[ itemIdx ];
                if ( item.prefab == null )
                {
                    return spacing;
                }

                Bounds bounds = BoundsUtils.GetBoundsRecursive( item.prefab.transform );

                Vector3        size = Vector3.Scale( bounds.size, scale );
                AxesUtils.Axis axis = settings.axisOrientedAlongTheLine;
                if ( item.isAsset2D
                     && SceneView.currentDrawingSceneView.in2DMode
                     && axis == AxesUtils.Axis.Z )
                {
                    axis = AxesUtils.Axis.Y;
                }

                spacing = AxesUtils.GetAxisValue( size, axis );
                if ( spacing <= 0.0001 )
                {
                    spacing = 0.5f;
                }
            }

            spacing += settings.gapSize;
            return spacing;
        }

        public static void SetNextPinBrushstroke( int delta )
        {
            _currentPinIdx = _currentPinIdx + delta;
            int mod = _currentPinIdx % PaletteManager.selectedBrush.itemCount;
            _currentPinIdx = mod < 0 ? PaletteManager.selectedBrush.itemCount + mod : mod;
            _brushstroke.Clear();
            AddBrushstrokeItem( _currentPinIdx, tangentPosition: Vector3.zero,
                angle: Vector3.zero,            scale: Vector3.one, PinManager.settings );
        }

        public static void UpdateBrushstroke( bool brushChange = false )
        {
            if ( ToolManager.tool == ToolManager.PaintTool.SELECTION )
            {
                return;
            }

            if ( ToolManager.tool    == ToolManager.PaintTool.LINE
                 || ToolManager.tool == ToolManager.PaintTool.SHAPE
                 || ToolManager.tool == ToolManager.PaintTool.TILING )
            {
                PWBIO.UpdateStroke();
                return;
            }

            if ( !brushChange
                 && ToolManager.tool == ToolManager.PaintTool.PIN
                 && PinManager.settings.repeat )
            {
                return;
            }

            _brushstroke.Clear();
            if ( PaletteManager.selectedBrush == null )
            {
                return;
            }

            if ( ToolManager.tool == ToolManager.PaintTool.BRUSH )
            {
                UpdateBrushBaseStroke( BrushManager.settings );
            }
            else if ( ToolManager.tool == ToolManager.PaintTool.GRAVITY )
            {
                UpdateBrushBaseStroke( GravityToolManager.settings );
            }
            else if ( ToolManager.tool == ToolManager.PaintTool.PIN )
            {
                UpdatePinBrushstroke();
            }
            else if ( ToolManager.tool == ToolManager.PaintTool.REPLACER )
            {
                UpdatePinBrushstroke();
            }
        }

        public static void UpdateLineBrushstroke( Vector3[] pathPoints )
            => UpdateLineBrushstroke( pathPoints, LineManager.settings );

        public static void UpdatePersistentLineBrushstroke( Vector3[]     pathPoints,
                                                            LineSettings  settings,     List<GameObject> lineObjects,
                                                            out Vector3[] objPositions, out Vector3[]    strokePositions )
        {
            _brushstroke.Clear();
            List<Vector3> objPositionsList     = new List<Vector3>();
            List<Vector3> strokePositionsList  = new List<Vector3>();
            float         lineLength           = 0f;
            float[]       lengthFromFirstPoint = new float[ pathPoints.Length ];
            float[]       segmentLength        = new float[ pathPoints.Length ];
            lengthFromFirstPoint[ 0 ] = 0f;
            for ( int i = 1; i < pathPoints.Length; ++i )
            {
                segmentLength[ i - 1 ]    =  ( pathPoints[ i ] - pathPoints[ i - 1 ] ).magnitude;
                lineLength                += segmentLength[ i - 1 ];
                lengthFromFirstPoint[ i ] =  lineLength;
            }

            float length   = 0f;
            int   segment  = 0;
            float minSpace = lineLength / 1024f;
            if ( PaletteManager.selectedBrush != null )
            {
                if ( PaletteManager.selectedBrush.patternMachine != null )
                {
                    PaletteManager.selectedBrush.patternMachine.Reset();
                }
            }

            int                               objIdx                  = 0;
            const float                       THRESHOLD               = 0.0001f;
            Dictionary<(int, Vector3), float> prefabSpacingDictionary = new Dictionary<(int, Vector3), float>();
            do
            {
                int nextIdx = PaletteManager.selectedBrush != null ? PaletteManager.selectedBrush.nextItemIndex : -1;

                while ( lengthFromFirstPoint[ segment + 1 ] < length )
                {
                    ++segment;
                    if ( segment >= pathPoints.Length - 1 )
                    {
                        break;
                    }
                }

                if ( segment >= pathPoints.Length - 1 )
                {
                    break;
                }

                Vector3 segmentDirection = ( pathPoints[ segment + 1 ] - pathPoints[ segment ] ).normalized;
                float   distance         = length - lengthFromFirstPoint[ segment ];

                Vector3 position = pathPoints[ segment ] + segmentDirection * distance;

                bool    objectExist = objIdx < lineObjects.Count;
                float   spacing     = 0;
                Vector3 scale       = Vector3.one;
                if ( objectExist )
                {
                    spacing = GetLineSpacing( lineObjects[ objIdx ].transform, settings );
                }
                else if ( PaletteManager.selectedBrush != null )
                {
                    MultibrushItemSettings item = PaletteManager.selectedBrush.items[ nextIdx ];
                    scale = item.randomScaleMultiplier
                        ? item.randomScaleMultiplierRange.randomVector
                        : item.scaleMultiplier;
                    if ( LineManager.settings.overwriteBrushProperties )
                    {
                        scale = LineManager.settings.brushSettings.randomScaleMultiplier
                            ? LineManager.settings.brushSettings.randomScaleMultiplierRange.randomVector
                            : LineManager.settings.brushSettings.scaleMultiplier;
                    }

                    (int nextIdx, Vector3 scale) key = ( nextIdx, scale );
                    if ( settings.spacingType == LineSettings.SpacingType.BOUNDS
                         && nextIdx           >= 0 )
                    {
                        if ( item.randomScaleMultiplier )
                        {
                            spacing = GetLineSpacing( nextIdx, settings, scale );
                        }
                        else if ( prefabSpacingDictionary.ContainsKey( key ) )
                        {
                            spacing = prefabSpacingDictionary[ key ];
                        }
                        else
                        {
                            spacing = GetLineSpacing( nextIdx, settings, scale );
                            prefabSpacingDictionary.Add( key, spacing );
                        }
                    }
                    else
                    {
                        spacing = GetLineSpacing( nextIdx, settings, scale );
                    }
                }

                if ( spacing == 0 )
                {
                    break;
                }

                spacing = Mathf.Max( spacing, minSpace );
                int     nearestPathointIdx;
                Vector3 intersection = LineData.NearestPathPoint( position, spacing, pathPoints, out nearestPathointIdx );
                if ( nearestPathointIdx > segment )
                {
                    spacing = ( pathPoints[ nearestPathointIdx ] - position ).magnitude
                              + ( intersection                   - pathPoints[ nearestPathointIdx ] ).magnitude;
                }

                length += spacing;
                if ( lineLength - length < THRESHOLD )
                {
                    break;
                }

                if ( objectExist )
                {
                    ++objIdx;
                    objPositionsList.Add( position );
                }
                else if ( PaletteManager.selectedBrush == null )
                {
                    break;
                }
                else
                {
                    AddBrushstrokeItem( nextIdx, position, angle: Vector3.zero, scale, LineManager.settings );
                    strokePositionsList.Add( position );
                }

            }
            while ( lineLength - length > THRESHOLD );

            objPositions    = objPositionsList.ToArray();
            strokePositions = strokePositionsList.ToArray();
        }

        public static void UpdatePersistentShapeBrushstroke( ShapeData        data,
                                                             List<GameObject> shapeObjects,
                                                             out Pose[]       objPoses )
        {
            _brushstroke.Clear();
            List<Pose>                        objPosesList            = new List<Pose>();
            ShapeSettings                     settings                = data.settings;
            Dictionary<(int, Vector3), float> prefabSpacingDictionary = new Dictionary<(int, Vector3), float>();
            int                               nextItemIdx             = -1;
            objPoses = objPosesList.ToArray();

            Vector3 Scale( MultibrushItemSettings nextItem )
            {
                if ( ShapeManager.settings.overwriteBrushProperties )
                {
                    return ShapeManager.settings.brushSettings.randomScaleMultiplier
                        ? ShapeManager.settings.brushSettings.randomScaleMultiplierRange.randomVector
                        : ShapeManager.settings.brushSettings.scaleMultiplier;
                }

                return nextItem.randomScaleMultiplier
                    ? nextItem.randomScaleMultiplierRange.randomVector
                    : nextItem.scaleMultiplier;
            }

            if ( settings.shapeType == ShapeSettings.ShapeType.CIRCLE )
            {
                const float                                TAU        = 2 * Mathf.PI;
                float                                      perimeter  = TAU * data.radius;
                List<(int idx, float size, bool objExist)> items      = new List<(int idx, float size, bool objExist)>();
                float                                      minspacing = perimeter / 1024f;
                float                                      itemsSize  = 0f;

                Vector3 firstLocalArcIntersection = Quaternion.Inverse( data.planeRotation )
                                                    * ( data.GetArcIntersection( 0 ) - data.center );
                float firstLocalAngle = Mathf.Atan2( firstLocalArcIntersection.z, firstLocalArcIntersection.x );
                if ( firstLocalAngle < 0 )
                {
                    firstLocalAngle += TAU;
                }

                Vector3 secondLocalArcIntersection = Quaternion.Inverse( data.planeRotation )
                                                     * ( data.GetArcIntersection( 1 ) - data.center );
                float secondLocalAngle = Mathf.Atan2( secondLocalArcIntersection.z, secondLocalArcIntersection.x );
                if ( secondLocalAngle < 0 )
                {
                    secondLocalAngle += TAU;
                }

                if ( secondLocalAngle <= firstLocalAngle )
                {
                    secondLocalAngle += TAU;
                }

                float arcDelta     = secondLocalAngle - firstLocalAngle;
                float arcPerimeter = arcDelta / TAU * perimeter;

                int objIdx = 0;

                int GetNextIdx()
                {
                    return PaletteManager.selectedBrush != null ? PaletteManager.selectedBrush.nextItemIndex : -1;
                }

                if ( nextItemIdx < 0 )
                {
                    nextItemIdx = GetNextIdx();
                }

                do
                {
                    float itemSize    = 0;
                    bool  objectExist = objIdx < shapeObjects.Count;
                    if ( objectExist )
                    {
                        itemSize = GetLineSpacing( shapeObjects[ objIdx ].transform, settings );
                    }
                    else if ( PaletteManager.selectedBrush != null )
                    {
                        MultibrushItemSettings           nextItem = PaletteManager.selectedBrush.items[ nextItemIdx ];
                        Vector3                          scale    = Scale( nextItem );
                        (int nextItemIdx, Vector3 scale) key      = ( nextItemIdx, scale );
                        if ( nextItem.randomScaleMultiplier )
                        {
                            itemSize = GetLineSpacing( nextItemIdx, settings, scale );
                        }
                        else if ( prefabSpacingDictionary.ContainsKey( key ) )
                        {
                            itemSize = prefabSpacingDictionary[ key ];
                        }
                        else
                        {
                            itemSize = GetLineSpacing( nextItemIdx, settings, scale );
                            prefabSpacingDictionary.Add( key, itemSize );
                        }
                    }

                    itemSize = Mathf.Max( itemSize, minspacing );
                    if ( itemsSize + itemSize > arcPerimeter )
                    {
                        break;
                    }

                    itemsSize += itemSize;
                    items.Add( ( objectExist ? objIdx : nextItemIdx, itemSize, objectExist ) );
                    nextItemIdx = GetNextIdx();
                    if ( objectExist )
                    {
                        ++objIdx;
                    }
                }
                while ( itemsSize < arcPerimeter );

                float spacing = ( arcPerimeter - itemsSize ) / ( items.Count + 1 );

                if ( items.Count == 0 )
                {
                    return;
                }

                float distance = firstLocalAngle / TAU * perimeter + items[ 0 ].size / 2;
                foreach ( (int idx, float size, bool objExist) item in items )
                {
                    GameObject obj = null;
                    if ( item.objExist )
                    {
                        obj = shapeObjects[ item.idx ];
                    }
                    else if ( PaletteManager.selectedBrush != null )
                    {
                        obj = PaletteManager.selectedBrush.items[ item.idx ].prefab;
                    }

                    if ( obj == null )
                    {
                        continue;
                    }

                    float arcAngle = distance / perimeter * TAU;
                    Vector3 LocalRadiusVector = new Vector3( Mathf.Cos( arcAngle ), 0f, Mathf.Sin( arcAngle ) )
                                                * data.radius;
                    Vector3 radiusVector = data.planeRotation * LocalRadiusVector;
                    Vector3 position     = radiusVector + data.center;
                    Vector3 itemDir = settings.objectsOrientedAlongTheLine
                        ? Vector3.Cross( data.planeRotation * Vector3.up, radiusVector )
                        : Vector3.forward;
                    if ( !settings.perpendicularToTheSurface )
                    {
                        itemDir = Vector3.ProjectOnPlane( itemDir, settings.projectionDirection );
                    }

                    if ( itemDir == Vector3.zero )
                    {
                        itemDir = settings.projectionDirection;
                    }

                    Quaternion lookAt = Quaternion.LookRotation( (AxesUtils.SignedAxis)settings.axisOrientedAlongTheLine,
                        Vector3.up );
                    Quaternion segmentRotation = Quaternion.LookRotation( itemDir, -settings.projectionDirection ) * lookAt;
                    Vector3    angle           = segmentRotation.eulerAngles;
                    if ( item.objExist )
                    {
                        objPosesList.Add( new Pose( position, segmentRotation ) );
                    }
                    else
                    {
                        MultibrushItemSettings nextItem = PaletteManager.selectedBrush.items[ item.idx ];
                        AddBrushstrokeItem( item.idx, position, angle, Scale( nextItem ), ShapeManager.settings );
                    }

                    distance += item.size + spacing;
                }
            }
            else
            {
                List<Vector3> points         = new List<Vector3>();
                int           firstVertexIdx = data.firstVertexIdxAfterIntersection;
                int           lastVertexIdx  = data.lastVertexIdxBeforeIntersection;
                int sidesCount = settings.shapeType == ShapeSettings.ShapeType.POLYGON
                    ? settings.sidesCount
                    : data.circleSideCount;

                int GetNextVertexIdx( int currentIdx )
                {
                    return currentIdx == sidesCount ? 1 : currentIdx + 1;
                }

                int GetPrevVertexIdx( int currentIdx )
                {
                    return currentIdx == 1 ? sidesCount : currentIdx - 1;
                }

                int firstPrev = GetPrevVertexIdx( firstVertexIdx );
                points.Add( data.GetArcIntersection( 0 ) );
                if ( lastVertexIdx != firstPrev
                     || ( lastVertexIdx == firstPrev && data.arcAngle > 120 ) )
                {

                    int vertexIdx     = firstVertexIdx;
                    int nextVertexIdx = firstVertexIdx;

                    do
                    {
                        vertexIdx = nextVertexIdx;
                        if ( vertexIdx       >= data.pointsCount
                             || points.Count >= data.pointsCount )
                        {
                            ShapeData.instance.Update( true );
                            return;
                        }

                        points.Add( data.GetPoint( vertexIdx ) );
                        nextVertexIdx = GetNextVertexIdx( nextVertexIdx );
                    }
                    while ( vertexIdx != lastVertexIdx );
                }

                Vector3 lastPoint = data.GetArcIntersection( 1 );
                if ( points.Last() != lastPoint )
                {
                    points.Add( lastPoint );
                }

                int firstObjInSegmentIdx = 0;

                void AddItemsToLine( Vector3 start, Vector3 end )
                {
                    int GetNextIdx()
                    {
                        return PaletteManager.selectedBrush != null ? PaletteManager.selectedBrush.nextItemIndex : -1;
                    }

                    if ( nextItemIdx < 0 )
                    {
                        nextItemIdx = GetNextIdx();
                    }

                    Vector3 startToEnd = end - start;
                    float   lineLength = startToEnd.magnitude;

                    float                                      itemsSize     = 0f;
                    List<(int idx, float size, bool objExist)> items         = new List<(int idx, float size, bool objExist)>();
                    float                                      minspacing    = lineLength * points.Count / 1024f;
                    int                                        objSegmentIdx = 0;
                    int                                        objIdx        = firstObjInSegmentIdx + objSegmentIdx;
                    do
                    {
                        float itemSize    = 0;
                        bool  objectExist = objIdx < shapeObjects.Count;
                        if ( objectExist )
                        {
                            itemSize = GetLineSpacing( shapeObjects[ objIdx ].transform, settings );
                        }
                        else if ( PaletteManager.selectedBrush != null )
                        {
                            MultibrushItemSettings           nextItem = PaletteManager.selectedBrush.items[ nextItemIdx ];
                            Vector3                          scale    = Scale( nextItem );
                            (int nextItemIdx, Vector3 scale) key      = ( nextItemIdx, scale );
                            if ( nextItem.randomScaleMultiplier )
                            {
                                itemSize = GetLineSpacing( nextItemIdx, settings, scale );
                            }
                            else if ( prefabSpacingDictionary.ContainsKey( key ) )
                            {
                                itemSize = prefabSpacingDictionary[ key ];
                            }
                            else
                            {
                                itemSize = GetLineSpacing( nextItemIdx, settings, scale );
                                prefabSpacingDictionary.Add( key, itemSize );
                            }
                        }

                        itemSize = Mathf.Max( itemSize, minspacing );
                        if ( itemsSize + itemSize > lineLength )
                        {
                            break;
                        }

                        itemsSize += itemSize;
                        items.Add( ( objectExist ? objIdx : nextItemIdx, itemSize, objectExist ) );
                        nextItemIdx = GetNextIdx();
                        if ( objectExist )
                        {
                            ++objIdx;
                        }
                    }
                    while ( itemsSize < lineLength );

                    float   spacing   = ( lineLength - itemsSize ) / ( items.Count + 1 );
                    float   distance  = spacing;
                    Vector3 direction = startToEnd.normalized;
                    Vector3 itemDir = settings.objectsOrientedAlongTheLine && direction != Vector3.zero
                        ? direction
                        : Vector3.forward;
                    if ( !settings.perpendicularToTheSurface )
                    {
                        itemDir = Vector3.ProjectOnPlane( itemDir, settings.projectionDirection );
                    }

                    Quaternion lookAt          = Quaternion.LookRotation( (AxesUtils.SignedAxis)settings.axisOrientedAlongTheLine, Vector3.up );
                    Quaternion segmentRotation = Quaternion.LookRotation( itemDir, -settings.projectionDirection ) * lookAt;
                    Vector3    angle           = segmentRotation.eulerAngles;
                    foreach ( (int idx, float size, bool objExist) item in items )
                    {
                        GameObject obj = null;
                        if ( item.objExist )
                        {
                            obj = shapeObjects[ item.idx ];
                        }
                        else if ( PaletteManager.selectedBrush != null )
                        {
                            obj = PaletteManager.selectedBrush.items[ item.idx ].prefab;
                        }

                        if ( obj == null )
                        {
                            continue;
                        }

                        Vector3 position = start + direction * ( distance + item.size / 2 );
                        if ( item.objExist )
                        {
                            objPosesList.Add( new Pose( position, segmentRotation ) );
                        }
                        else
                        {
                            MultibrushItemSettings nextItem = PaletteManager.selectedBrush.items[ item.idx ];
                            AddBrushstrokeItem( item.idx, position, angle, Scale( nextItem ), settings );
                        }

                        distance += item.size + spacing;
                        ++firstObjInSegmentIdx;
                    }
                }

                for ( int i = 0; i < points.Count - 1; ++i )
                {
                    Vector3 start = points[ i ];
                    Vector3 end   = points[ i + 1 ];
                    AddItemsToLine( start, end );
                }
            }

            objPoses = objPosesList.ToArray();
        }

        public static void UpdatePersistentTilingBrushstroke( Vector3[]        cellCenters, TilingSettings settings,
                                                              List<GameObject> tilingObjects,
                                                              out Vector3[]    objPositions, out Vector3[] strokePositions )
        {
            _brushstroke.Clear();
            List<Vector3> objPositionsList    = new List<Vector3>();
            List<Vector3> strokePositionsList = new List<Vector3>();
            for ( int i = 0; i < cellCenters.Length; ++i )
            {
                bool    objectExist = i < tilingObjects.Count;
                Vector3 position    = cellCenters[ i ];
                if ( objectExist )
                {
                    objPositionsList.Add( position );
                }
                else
                {
                    if ( PaletteManager.selectedBrush == null )
                    {
                        break;
                    }

                    int                    nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                    MultibrushItemSettings item    = PaletteManager.selectedBrush.items[ nextIdx ];
                    Vector3 scale = item.randomScaleMultiplier
                        ? item.randomScaleMultiplierRange.randomVector
                        : item.scaleMultiplier;
                    if ( TilingManager.settings.overwriteBrushProperties )
                    {
                        scale = TilingManager.settings.brushSettings.randomScaleMultiplier
                            ? TilingManager.settings.brushSettings.randomScaleMultiplierRange.randomVector
                            : TilingManager.settings.brushSettings.scaleMultiplier;
                    }

                    AddBrushstrokeItem( nextIdx, position, angle: Vector3.zero, scale, settings );
                    strokePositionsList.Add( position );
                }
            }

            objPositions    = objPositionsList.ToArray();
            strokePositions = strokePositionsList.ToArray();
        }

        public static void UpdateShapeBrushstroke()
        {
            _brushstroke.Clear();
            if ( PaletteManager.selectedBrush == null )
            {
                return;
            }

            if ( ShapeData.instance.state < ToolManager.ToolState.EDIT )
            {
                return;
            }

            ShapeSettings settings       = ShapeManager.settings;
            List<Vector3> points         = new List<Vector3>();
            int           firstVertexIdx = ShapeData.instance.firstVertexIdxAfterIntersection;
            int           lastVertexIdx  = ShapeData.instance.lastVertexIdxBeforeIntersection;
            int sidesCount = settings.shapeType == ShapeSettings.ShapeType.POLYGON
                ? settings.sidesCount
                : ShapeData.instance.circleSideCount;

            int GetNextVertexIdx( int currentIdx )
            {
                return currentIdx == sidesCount ? 1 : currentIdx + 1;
            }

            int GetPrevVertexIdx( int currentIdx )
            {
                return currentIdx == 1 ? sidesCount : currentIdx - 1;
            }

            int firstPrev = GetPrevVertexIdx( firstVertexIdx );
            points.Add( ShapeData.instance.GetArcIntersection( 0 ) );
            if ( lastVertexIdx != firstPrev
                 || ( lastVertexIdx == firstPrev && ShapeData.instance.arcAngle > 120 ) )
            {
                int vertexIdx     = firstVertexIdx;
                int nextVertexIdx = firstVertexIdx;
                do
                {
                    vertexIdx = nextVertexIdx;
                    points.Add( ShapeData.instance.GetPoint( vertexIdx ) );
                    nextVertexIdx = GetNextVertexIdx( nextVertexIdx );
                }
                while ( vertexIdx != lastVertexIdx );
            }

            Vector3 lastPoint = ShapeData.instance.GetArcIntersection( 1 );
            if ( points.Last() != lastPoint )
            {
                points.Add( lastPoint );
            }

            Vector3 Scale( MultibrushItemSettings nextItem )
            {
                if ( ShapeManager.settings.overwriteBrushProperties )
                {
                    return ShapeManager.settings.brushSettings.randomScaleMultiplier
                        ? ShapeManager.settings.brushSettings.randomScaleMultiplierRange.randomVector
                        : ShapeManager.settings.brushSettings.scaleMultiplier;
                }

                return nextItem.randomScaleMultiplier
                    ? nextItem.randomScaleMultiplierRange.randomVector
                    : nextItem.scaleMultiplier;
            }

            Dictionary<(int, Vector3), float> prefabSpacingDictionary = new Dictionary<(int, Vector3), float>();

            void AddItemsToLine( Vector3 start, Vector3 end, ref int nextIdx )
            {
                if ( nextIdx < 0 )
                {
                    nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                }

                Vector3                     startToEnd = end - start;
                float                       lineLength = startToEnd.magnitude;
                float                       itemsSize  = 0f;
                List<(int idx, float size)> items      = new List<(int idx, float size)>();
                float                       minspacing = lineLength * points.Count / 1024f;

                do
                {
                    MultibrushItemSettings       nextItem = PaletteManager.selectedBrush.items[ nextIdx ];
                    Vector3                      scale    = Scale( nextItem );
                    float                        itemSize;
                    (int nextIdx, Vector3 scale) key = ( nextIdx, scale );
                    if ( nextItem.randomScaleMultiplier )
                    {
                        itemSize = GetLineSpacing( nextIdx, settings, scale );
                    }
                    else if ( prefabSpacingDictionary.ContainsKey( key ) )
                    {
                        itemSize = prefabSpacingDictionary[ key ];
                    }
                    else
                    {
                        itemSize = GetLineSpacing( nextIdx, settings, scale );
                        prefabSpacingDictionary.Add( key, itemSize );
                    }

                    itemSize = Mathf.Max( itemSize, minspacing );
                    if ( itemsSize + itemSize > lineLength )
                    {
                        break;
                    }

                    itemsSize += itemSize;
                    items.Add( ( nextIdx, itemSize ) );
                    nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                }
                while ( itemsSize < lineLength );

                float   spacing   = ( lineLength - itemsSize ) / ( items.Count + 1 );
                float   distance  = spacing;
                Vector3 direction = startToEnd.normalized;

                Vector3 itemDir = settings.objectsOrientedAlongTheLine && direction != Vector3.zero
                    ? direction
                    : Vector3.forward;
                if ( !settings.perpendicularToTheSurface )
                {
                    itemDir = Vector3.ProjectOnPlane( itemDir, settings.projectionDirection );
                }

                if ( itemDir == Vector3.zero )
                {
                    itemDir = settings.projectionDirection;
                }

                Quaternion lookAt          = Quaternion.LookRotation( (AxesUtils.SignedAxis)settings.axisOrientedAlongTheLine, Vector3.up );
                Quaternion segmentRotation = Quaternion.LookRotation( itemDir, -settings.projectionDirection ) * lookAt;
                Vector3    angle           = segmentRotation.eulerAngles;
                foreach ( (int idx, float size) item in items )
                {
                    MultibrushItemSettings brushItem = PaletteManager.selectedBrush.items[ item.idx ];
                    if ( brushItem.prefab == null )
                    {
                        continue;
                    }

                    Vector3                position = start + direction * ( distance + item.size / 2 );
                    MultibrushItemSettings nextItem = PaletteManager.selectedBrush.items[ item.idx ];
                    Vector3                scale    = Scale( nextItem );
                    AddBrushstrokeItem( item.idx, position, angle, scale, settings );
                    distance += item.size + spacing;
                }
            }

            int nexItemItemIdx = -1;

            if ( ShapeManager.settings.shapeType == ShapeSettings.ShapeType.CIRCLE )
            {
                const float                 TAU        = 2 * Mathf.PI;
                float                       perimeter  = TAU * ShapeData.instance.radius;
                List<(int idx, float size)> items      = new List<(int idx, float size)>();
                float                       minspacing = perimeter / 1024f;
                float                       itemsSize  = 0f;

                Vector3 firstLocalArcIntersection = Quaternion.Inverse( ShapeData.instance.planeRotation )
                                                    * ( ShapeData.instance.GetArcIntersection( 0 ) - ShapeData.instance.center );
                float firstLocalAngle = Mathf.Atan2( firstLocalArcIntersection.z, firstLocalArcIntersection.x );
                if ( firstLocalAngle < 0 )
                {
                    firstLocalAngle += TAU;
                }

                Vector3 secondLocalArcIntersection = Quaternion.Inverse( ShapeData.instance.planeRotation )
                                                     * ( ShapeData.instance.GetArcIntersection( 1 ) - ShapeData.instance.center );
                float secondLocalAngle = Mathf.Atan2( secondLocalArcIntersection.z, secondLocalArcIntersection.x );
                if ( secondLocalAngle < 0 )
                {
                    secondLocalAngle += TAU;
                }

                if ( secondLocalAngle <= firstLocalAngle )
                {
                    secondLocalAngle += TAU;
                }

                float arcDelta     = secondLocalAngle - firstLocalAngle;
                float arcPerimeter = arcDelta / TAU * perimeter;
                if ( PaletteManager.selectedBrush.patternMachine != null
                     && PaletteManager.selectedBrush.restartPatternForEachStroke )
                {
                    PaletteManager.selectedBrush.patternMachine.Reset();
                }

                do
                {
                    float                        itemSize = 0;
                    int                          nextIdx  = PaletteManager.selectedBrush.nextItemIndex;
                    MultibrushItemSettings       nextItem = PaletteManager.selectedBrush.items[ nextIdx ];
                    Vector3                      scale    = Scale( nextItem );
                    (int nextIdx, Vector3 scale) key      = ( nextIdx, scale );
                    if ( nextItem.randomScaleMultiplier )
                    {
                        itemSize = GetLineSpacing( nextIdx, settings, scale );
                    }
                    else if ( prefabSpacingDictionary.ContainsKey( key ) )
                    {
                        itemSize = prefabSpacingDictionary[ key ];
                    }
                    else
                    {
                        itemSize = GetLineSpacing( nextIdx, settings, scale );
                        prefabSpacingDictionary.Add( key, itemSize );
                    }

                    itemSize = Mathf.Max( itemSize, minspacing );
                    if ( itemsSize + itemSize > arcPerimeter )
                    {
                        break;
                    }

                    itemsSize += itemSize;
                    items.Add( ( nextIdx, itemSize ) );
                }
                while ( itemsSize < arcPerimeter );

                float spacing = ( arcPerimeter - itemsSize ) / items.Count;

                if ( items.Count == 0 )
                {
                    return;
                }

                float distance = firstLocalAngle / TAU * perimeter + items[ 0 ].size / 2;

                for ( int i = 0; i < items.Count; ++i )
                {
                    (int idx, float size) item     = items[ i ];
                    float                 arcAngle = distance / perimeter * TAU;
                    Vector3 LocalRadiusVector = new Vector3( Mathf.Cos( arcAngle ), 0f, Mathf.Sin( arcAngle ) )
                                                * ShapeData.instance.radius;
                    Vector3 radiusVector = ShapeData.instance.planeRotation * LocalRadiusVector;
                    Vector3 position     = radiusVector + ShapeData.instance.center;
                    Vector3 itemDir = settings.objectsOrientedAlongTheLine
                        ? Vector3.Cross( ShapeData.instance.planeRotation * Vector3.up, radiusVector )
                        : Vector3.forward;
                    if ( !settings.perpendicularToTheSurface )
                    {
                        itemDir = Vector3.ProjectOnPlane( itemDir, settings.projectionDirection );
                    }

                    if ( itemDir == Vector3.zero )
                    {
                        itemDir = settings.projectionDirection;
                    }

                    Quaternion lookAt = Quaternion.LookRotation( (AxesUtils.SignedAxis)settings.axisOrientedAlongTheLine,
                        Vector3.up );
                    Quaternion             segmentRotation = Quaternion.LookRotation( itemDir, -settings.projectionDirection ) * lookAt;
                    Vector3                angle           = segmentRotation.eulerAngles;
                    MultibrushItemSettings nextItem        = PaletteManager.selectedBrush.items[ item.idx ];
                    Vector3                scale           = Scale( nextItem );
                    AddBrushstrokeItem( item.idx, position, angle, scale, settings );
                    (int idx, float size) next_Item = items[ ( i + 1 ) % items.Count ];
                    distance += item.size / 2 + next_Item.size / 2 + spacing;
                }
            }
            else
            {
                if ( PaletteManager.selectedBrush.patternMachine != null
                     && PaletteManager.selectedBrush.restartPatternForEachStroke )
                {
                    PaletteManager.selectedBrush.patternMachine.Reset();
                }

                for ( int i = 0; i < points.Count - 1; ++i )
                {
                    Vector3 start = points[ i ];
                    Vector3 end   = points[ i + 1 ];
                    AddItemsToLine( start, end, ref nexItemItemIdx );
                }
            }
        }

        public static void UpdateTilingBrushstroke( Vector3[] cellCenters )
        {
            _brushstroke.Clear();
            if ( PaletteManager.selectedBrush == null )
            {
                return;
            }

            for ( int i = 0; i < cellCenters.Length; ++i )
            {
                int                    nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                MultibrushItemSettings item    = PaletteManager.selectedBrush.items[ nextIdx ];
                Vector3 scale = item.randomScaleMultiplier
                    ? item.randomScaleMultiplierRange.randomVector
                    : item.scaleMultiplier;
                if ( TilingManager.settings.overwriteBrushProperties )
                {
                    scale = TilingManager.settings.brushSettings.randomScaleMultiplier
                        ? TilingManager.settings.brushSettings.randomScaleMultiplierRange.randomVector
                        : TilingManager.settings.brushSettings.scaleMultiplier;
                }

                AddBrushstrokeItem( nextIdx, cellCenters[ i ], angle: Vector3.zero, scale, TilingManager.settings );
            }

            ToolProperties.RepainWindow();
        }

        #endregion

        #region Private Methods

        private static void AddBrushstrokeItem( int                index, Vector3 tangentPosition, Vector3 angle, Vector3 scale,
                                                IPaintToolSettings paintToolSettings )
        {
            if ( index    < 0
                 || index >= PaletteManager.selectedBrush.itemCount )
            {
                return;
            }

            BrushSettings brushSettings = PaletteManager.selectedBrush.items[ index ];
            if ( paintToolSettings != null
                 && paintToolSettings.overwriteBrushProperties )
            {
                brushSettings = paintToolSettings.brushSettings;
            }

            Vector3 additonalAngle = angle;
            if ( brushSettings.addRandomRotation )
            {
                Vector3 randomAngle = brushSettings.randomEulerOffset.randomVector;
                if ( brushSettings.rotateInMultiples )
                {
                    randomAngle = new Vector3(
                        Mathf.Round( randomAngle.x / brushSettings.rotationFactor ) * brushSettings.rotationFactor,
                        Mathf.Round( randomAngle.y / brushSettings.rotationFactor ) * brushSettings.rotationFactor,
                        Mathf.Round( randomAngle.z / brushSettings.rotationFactor ) * brushSettings.rotationFactor );
                }

                additonalAngle += randomAngle;
            }
            else
            {
                additonalAngle += brushSettings.eulerOffset;
            }

            if ( !brushSettings.separateScaleAxes )
            {
                scale.z = scale.y = scale.x;
            }

            bool flipX = brushSettings.flipX == BrushSettings.FlipAction.NONE ? false
                : brushSettings.flipX        == BrushSettings.FlipAction.FLIP ? true : Random.value > 0.5;
            bool flipY = brushSettings.flipY == BrushSettings.FlipAction.NONE ? false
                : brushSettings.flipY        == BrushSettings.FlipAction.FLIP ? true : Random.value > 0.5;
            float surfaceDistance = brushSettings.randomSurfaceDistance
                ? brushSettings.randomSurfaceDistanceRange.randomValue
                : brushSettings.surfaceDistance;
            BrushstrokeItem strokeItem = new BrushstrokeItem( PaletteManager.selectedBrush.items[ index ],
                tangentPosition, additonalAngle, scale, flipX, flipY, surfaceDistance );
            if ( _brushstroke.Count > 0 )
            {
                _brushstroke.Last().nextTangentPosition = tangentPosition;
            }

            _brushstroke.Add( strokeItem );
        }

        private static float GetLineSpacing( Transform transform, LineSettings settings )
        {
            float spacing = settings.spacing;
            if ( settings.spacingType == LineSettings.SpacingType.BOUNDS
                 && transform         != null )
            {
                Bounds bounds = BoundsUtils.GetBoundsRecursive( transform, transform.rotation, false );

                Vector3        size = bounds.size;
                AxesUtils.Axis axis = settings.axisOrientedAlongTheLine;
                if ( Utils2D.Is2DAsset( transform.gameObject )
                     && SceneView.currentDrawingSceneView != null
                     && SceneView.currentDrawingSceneView.in2DMode
                     && axis == AxesUtils.Axis.Z )
                {
                    axis = AxesUtils.Axis.Y;
                }

                spacing = AxesUtils.GetAxisValue( size, axis );
                if ( spacing <= 0.0001 )
                {
                    spacing = 0.5f;
                }
            }

            spacing += settings.gapSize;
            return spacing;
        }

        private static void UpdateBrushBaseStroke( BrushToolBase brushSettings )
        {
            if ( brushSettings.spacingType == BrushToolBase.SpacingType.AUTO )
            {
                float maxSize = 0.1f;
                foreach ( MultibrushItemSettings item in PaletteManager.selectedBrush.items )
                {
                    if ( item.prefab == null )
                    {
                        continue;
                    }

                    Vector3 itemSize = BoundsUtils.GetBoundsRecursive( item.prefab.transform ).size;
                    itemSize = Vector3.Scale( itemSize,
                        item.randomScaleMultiplier ? item.maxScaleMultiplier : item.scaleMultiplier );
                    maxSize = Mathf.Max( itemSize.x, itemSize.z, maxSize );
                }

                brushSettings.minSpacing = maxSize;
                ToolProperties.RepainWindow();
            }

            if ( brushSettings.brushShape == BrushToolBase.BrushShape.POINT )
            {
                int nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                if ( nextIdx == -1 )
                {
                    return;
                }

                if ( PaletteManager.selectedBrush.frequencyMode == MultibrushSettings.FrecuencyMode.PATTERN
                     && nextIdx                                 == -2 )
                {
                    return;
                }

                _brushstroke.Clear();

                AddBrushstrokeItem( nextIdx, tangentPosition: Vector3.zero, angle: Vector3.zero, scale: Vector3.one, brushSettings );
                _currentPinIdx = Mathf.Clamp( nextIdx, 0, PaletteManager.selectedBrush.itemCount - 1 );
            }
            else
            {
                float radius    = brushSettings.radius;
                float radiusSqr = radius * radius;

                float minSpacing = brushSettings.minSpacing * 100f / brushSettings.density;
                if ( brushSettings.randomizePositions )
                {
                    minSpacing *= Mathf.Max( 1 - Random.value * brushSettings.randomness, 0.5f );
                }

                float delta           = minSpacing;
                float maxRandomOffset = delta * brushSettings.randomness;

                int       halfSize = (int)Mathf.Ceil( radius / delta ) + 1;
                const int MAX_SIZE = 32;
                if ( halfSize > MAX_SIZE )
                {
                    halfSize        = MAX_SIZE;
                    delta           = radius / MAX_SIZE;
                    minSpacing      = delta;
                    maxRandomOffset = delta * brushSettings.randomness;
                }

                int   size  = halfSize * 2;
                float col0x = -delta   * halfSize;
                float row0y = -delta   * halfSize;

                List<(int row, int col)> takedCells = new List<(int row, int col)>();

                for ( int row = 0; row < size; ++row )
                {
                    for ( int col = 0; col < size; ++col )
                    {
                        float x = col0x + col * delta;
                        float y = row0y + row * delta;
                        if ( brushSettings.randomizePositions )
                        {
                            if ( Random.value < 0.4 * brushSettings.randomness )
                            {
                                continue;
                            }

                            if ( takedCells.Contains( ( row, col ) ) )
                            {
                                continue;
                            }

                            x += Random.Range( -maxRandomOffset, maxRandomOffset );
                            y += Random.Range( -maxRandomOffset, maxRandomOffset );
                            int randCol = Mathf.RoundToInt( ( x - col0x ) / delta );
                            int randRow = Mathf.RoundToInt( ( y - row0y ) / delta );
                            if ( randRow < row )
                            {
                                continue;
                            }

                            if ( row    != randRow
                                 || col != randRow )
                            {
                                takedCells.Add( ( randRow, randCol ) );
                            }

                            takedCells.RemoveAll( pair => pair.row <= row );
                        }

                        if ( brushSettings.brushShape == BrushToolBase.BrushShape.CIRCLE )
                        {
                            float distanceSqr = x * x + y * y;
                            if ( distanceSqr >= radiusSqr )
                            {
                                continue;
                            }
                        }
                        else if ( brushSettings.brushShape == BrushToolBase.BrushShape.SQUARE )
                        {
                            if ( Mathf.Abs( x )    > radius
                                 || Mathf.Abs( y ) > radius )
                            {
                                continue;
                            }
                        }

                        int     nextItemIdx = PaletteManager.selectedBrush.nextItemIndex;
                        Vector3 position    = new Vector3( x, y, 0f );
                        if ( ( PaletteManager.selectedBrush.frequencyMode
                               == MultibrushSettings.FrecuencyMode.RANDOM
                               && nextItemIdx == -1 )
                             || ( PaletteManager.selectedBrush.frequencyMode
                                  == MultibrushSettings.FrecuencyMode.PATTERN
                                  && nextItemIdx == -2 ) )
                        {
                            continue;
                        }

                        MultibrushItemSettings item = PaletteManager.selectedBrush.items[ nextItemIdx ];
                        Vector3 scale = item.randomScaleMultiplier
                            ? item.randomScaleMultiplierRange.randomVector
                            : item.scaleMultiplier;
                        if ( TilingManager.settings.overwriteBrushProperties )
                        {
                            scale = TilingManager.settings.brushSettings.randomScaleMultiplier
                                ? TilingManager.settings.brushSettings.randomScaleMultiplierRange.randomVector
                                : TilingManager.settings.brushSettings.scaleMultiplier;
                        }
                        else if ( nextItemIdx != -1 )
                        {
                            AddBrushstrokeItem( nextItemIdx, tangentPosition: position,
                                angle: Vector3.zero,         scale, brushSettings );
                        }
                    }
                }
            }
        }

        private static void UpdateLineBrushstroke( Vector3[] points, LineSettings settings )
        {
            _brushstroke.Clear();
            if ( PaletteManager.selectedBrush == null )
            {
                return;
            }

            float   lineLength           = 0f;
            float[] lengthFromFirstPoint = new float[ points.Length ];
            float[] segmentLength        = new float[ points.Length ];
            lengthFromFirstPoint[ 0 ] = 0f;
            for ( int i = 1; i < points.Length; ++i )
            {
                segmentLength[ i - 1 ]    =  ( points[ i ] - points[ i - 1 ] ).magnitude;
                lineLength                += segmentLength[ i - 1 ];
                lengthFromFirstPoint[ i ] =  lineLength;
            }

            float length   = 0f;
            int   segment  = 0;
            float minSpace = lineLength / 1024f;
            if ( PaletteManager.selectedBrush.patternMachine != null )
            {
                PaletteManager.selectedBrush.patternMachine.Reset();
            }

            Dictionary<(int, Vector3), float> prefabSpacingDictionary = new Dictionary<(int, Vector3), float>();
            do
            {
                int nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                while ( lengthFromFirstPoint[ segment + 1 ] < length )
                {
                    ++segment;
                    if ( segment >= points.Length - 1 )
                    {
                        break;
                    }
                }

                if ( segment >= points.Length - 1 )
                {
                    break;
                }

                Vector3                segmentDirection = ( points[ segment + 1 ] - points[ segment ] ).normalized;
                float                  distance         = length            - lengthFromFirstPoint[ segment ];
                Vector3                position         = points[ segment ] + segmentDirection * distance;
                float                  spacing          = minSpace;
                MultibrushItemSettings item             = PaletteManager.selectedBrush.items[ nextIdx ];
                Vector3 scale = item.randomScaleMultiplier
                    ? item.randomScaleMultiplierRange.randomVector
                    : item.scaleMultiplier;
                if ( LineManager.settings.overwriteBrushProperties )
                {
                    scale = LineManager.settings.brushSettings.randomScaleMultiplier
                        ? LineManager.settings.brushSettings.randomScaleMultiplierRange.randomVector
                        : LineManager.settings.brushSettings.scaleMultiplier;
                }

                if ( settings.spacingType == LineSettings.SpacingType.BOUNDS )
                {
                    (int nextIdx, Vector3 scale) key = ( nextIdx, scale );
                    if ( item.randomScaleMultiplier )
                    {
                        spacing = GetLineSpacing( nextIdx, settings, scale );
                    }
                    else if ( prefabSpacingDictionary.ContainsKey( key ) )
                    {
                        spacing = prefabSpacingDictionary[ key ];
                    }
                    else
                    {
                        spacing = GetLineSpacing( nextIdx, settings, scale );
                        prefabSpacingDictionary.Add( key, spacing );
                    }
                }
                else
                {
                    spacing = GetLineSpacing( nextIdx, settings, scale );
                }

                float delta = Mathf.Max( spacing, minSpace );
                if ( delta <= 0 )
                {
                    break;
                }

                length += Mathf.Max( spacing, minSpace );
                if ( length > lineLength )
                {
                    break;
                }

                AddBrushstrokeItem( nextIdx, position, angle: Vector3.zero, scale, settings );
            }
            while ( length < lineLength );
        }

        private static void UpdatePinBrushstroke()
        {
            int nextIdx = PaletteManager.selectedBrush.nextItemIndex;
            if ( nextIdx == -1 )
            {
                return;
            }

            if ( PaletteManager.selectedBrush.frequencyMode == MultibrushSettings.FrecuencyMode.PATTERN
                 && nextIdx                                 == -2 )
            {
                if ( PaletteManager.selectedBrush.patternMachine != null )
                {
                    PaletteManager.selectedBrush.patternMachine.Reset();
                }
                else
                {
                    return;
                }
            }

            AddBrushstrokeItem( nextIdx, tangentPosition: Vector3.zero, angle: Vector3.zero,
                scale: Vector3.one,      PinManager.settings );

            const int maxTries = 10;
            int       tryCount = 0;
            while ( _brushstroke.Count == 0
                    && ++tryCount      < maxTries )
            {
                nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                if ( nextIdx >= 0 )
                {
                    AddBrushstrokeItem( nextIdx, tangentPosition: Vector3.zero, angle: Vector3.zero,
                        scale: Vector3.one,      PinManager.settings );
                    break;
                }
            }

            _currentPinIdx = Mathf.Clamp( nextIdx, 0, PaletteManager.selectedBrush.itemCount - 1 );
        }

        #endregion

    }
}