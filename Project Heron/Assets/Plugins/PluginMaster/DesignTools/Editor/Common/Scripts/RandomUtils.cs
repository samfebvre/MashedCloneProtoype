using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PluginMaster
{
    public static class RandomUtils
    {
        [Serializable]
        public class Range : ISerializationCallbackReceiver
        {

            #region Serialized

            [SerializeField] private float _v1  = -1f;
            [SerializeField] private float _v2  = 1f;
            [SerializeField] private float _min = -1f;
            [SerializeField] private float _max = 1f;

            #endregion

            #region Public Properties

            public float max => Mathf.Max( _v1, _v2 );

            public float min => Mathf.Min( _v1, _v2 );

            public float randomValue => Random.Range( min, max );

            public float v1
            {
                get => _v1;
                set => _v1 = value;
            }

            public float v2
            {
                get => _v2;
                set => _v2 = value;
            }

            #endregion

            #region Public Constructors

            public Range()
            {
            }

            public Range( Range other )
            {
                ( _v1, _v2 ) = ( other._v1, other._v2 );
            }

            public Range( float v1, float v2 )
            {
                _v1 = v1;
                _v2 = v2;
            }

            #endregion

            #region Public Methods

            public override bool Equals( object obj ) => obj is Range range && ( _v1 == range._v1 ) & ( _v2 == range._v2 );

            public override int GetHashCode()
            {
                int hashCode = -1605643878;
                hashCode = hashCode * -1521134295 + _v1.GetHashCode();
                hashCode = hashCode * -1521134295 + _v2.GetHashCode();
                return hashCode;
            }

            public void OnAfterDeserialize()
            {
                _v1 = _min;
                _v2 = _max;
            }

            public void OnBeforeSerialize()
            {
                _min = min;
                _max = max;
            }

            public static bool operator ==( Range value1, Range value2 ) => Equals( value1, value2 );
            public static bool operator !=( Range value1, Range value2 ) => !Equals( value1, value2 );

            #endregion

        }

        [Serializable]
        public class Range3
        {

            #region Serialized

            public Range x = new Range( 0, 0 );
            public Range y = new Range( 0, 0 );
            public Range z = new Range( 0, 0 );

            #endregion

            #region Public Properties

            public Vector3 max => new Vector3( x.max, y.max, z.max );

            public Vector3 min => new Vector3( x.min, y.min, z.min );

            public Vector3 randomVector => new Vector3( x.randomValue, y.randomValue, z.randomValue );

            public Vector3 v1
            {
                get => new Vector3( x.v1, y.v1, z.v1 );
                set
                {
                    x.v1 = value.x;
                    y.v1 = value.y;
                    z.v1 = value.z;
                }
            }

            public Vector3 v2
            {
                get => new Vector3( x.v2, y.v2, z.v2 );
                set
                {
                    x.v2 = value.x;
                    y.v2 = value.y;
                    z.v2 = value.z;
                }
            }

            #endregion

            #region Public Constructors

            public Range3( Vector3 v1, Vector3 v2 )
            {
                x = new Range( v1.x, v2.x );
                y = new Range( v1.y, v2.y );
                z = new Range( v1.z, v2.z );
            }

            public Range3( Range3 other )
            {
                x = new Range( other.x );
                y = new Range( other.y );
                z = new Range( other.z );
            }

            #endregion

            #region Public Methods

            public override bool Equals( object obj )
                => obj is Range3 range3 && x == range3.x && y == range3.y && z == range3.z;

            public override int GetHashCode()
            {
                int hashCode = 373119288;
                hashCode = hashCode * -1521134295 + x.GetHashCode();
                hashCode = hashCode * -1521134295 + y.GetHashCode();
                hashCode = hashCode * -1521134295 + z.GetHashCode();
                return hashCode;
            }

            public static bool operator ==( Range3 value1, Range3 value2 ) => Equals( value1, value2 );
            public static bool operator !=( Range3 value1, Range3 value2 ) => !Equals( value1, value2 );

            #endregion

        }
    }
}