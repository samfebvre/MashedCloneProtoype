using System;
using System.Collections.Generic;
using UnityEngine;

namespace PluginMaster
{
    public static class AxesUtils
    {

        #region Public Enums

        public enum Axis
        {
            X,
            Y,
            Z,
        }

        #endregion

        #region Public Methods

        public static void AddValueToAxis( ref Vector3 v, Axis axis, float value )
        {
            if ( axis == Axis.X )
            {
                v.x += value;
            }
            else if ( axis == Axis.Y )
            {
                v.y += value;
            }
            else
            {
                v.z += value;
            }
        }

        public static float GetAxisValue( Vector3 v, Axis axis ) => axis == Axis.X ? v.x : axis == Axis.Y ? v.y : v.z;

        public static Axis[] GetOtherAxes( Axis axis )
            => axis    == Axis.X ? new[] { Axis.Y, Axis.Z }
                : axis == Axis.Y ? new[] { Axis.X, Axis.Z } : new[] { Axis.X, Axis.Y };

        public static Axis GetOtherAxis( Axis axis1, Axis axis2 )
            => ( axis1    == Axis.X && axis2 == Axis.Z ) || ( axis1 == Axis.Z && axis2 == Axis.X ) ? Axis.Y
                : ( axis1 == Axis.Y && axis2 == Axis.Z ) || ( axis1 == Axis.Z && axis2 == Axis.Y ) ? Axis.X : Axis.Z;

        public static Vector3 GetVector( float signedSize, Axis axis )
            => ( axis == Axis.X ? Vector3.right : axis == Axis.Y ? Vector3.up : Vector3.forward ) * signedSize;

        public static Vector3 GetVector( Vector3 v, Axis axis )
            => axis == Axis.X ? Vector3.right * v.x : axis == Axis.Y ? Vector3.up * v.y : Vector3.forward * v.z;

        public static bool IsParallelToAxis( Vector3 v, out Axis axis )
        {
            axis = Axis.Y;
            if ( v.x    == 0
                 && v.z == 0 )
            {
                axis = Axis.Y;
                return true;
            }

            if ( v.y    == 0
                 && v.z == 0 )
            {
                axis = Axis.X;
                return true;
            }

            if ( v.x    == 0
                 && v.y == 0 )
            {
                axis = Axis.Z;
                return true;
            }

            return false;
        }

        public static void SetAxisValue( ref Vector3 v, Axis axis, float value )
        {
            if ( axis == Axis.X )
            {
                v.x = value;
            }
            else if ( axis == Axis.Y )
            {
                v.y = value;
            }
            else
            {
                v.z = value;
            }
        }

        #endregion

        [Serializable]
        public class SignedAxis
        {

            #region Statics and Constants

            public const int RIGHT   = 0;
            public const int LEFT    = 1;
            public const int UP      = 2;
            public const int DOWN    = 3;
            public const int FORWARD = 4;
            public const int BACK    = 5;

            private static readonly Dictionary<Vector3, SignedAxis> _dictionary
                = new Dictionary<Vector3, SignedAxis>
                {
                    { Vector3.right, RIGHT },
                    { Vector3.left, LEFT },
                    { Vector3.up, UP },
                    { Vector3.down, DOWN },
                    { Vector3.forward, FORWARD },
                    { Vector3.back, BACK },
                };

            #endregion

            #region Serialized

            [SerializeField] private Axis _axis;
            [SerializeField] private int  _sign;

            #endregion

            #region Public Properties

            public Axis axis
            {
                get => _axis;
                set => _axis = value;
            }

            public int sign
            {
                get => _sign;
                set => _sign = value;
            }

            #endregion

            #region Public Methods

            public override                 bool Equals( object     obj )                  => Equals( obj as SignedAxis );
            public                          bool Equals( SignedAxis other )                => _axis == other._axis && _sign == other._sign;
            public override                 int  GetHashCode()                             => (int)_axis * 2 + ( _sign > 0 ? 0 : 1 );
            public static                   bool operator ==( SignedAxis l, SignedAxis r ) => l.Equals( r );
            public static implicit operator Axis( SignedAxis             value ) => value._axis;

            public static implicit operator SignedAxis( Axis value )
                => value == Axis.X ? RIGHT : value == Axis.Y ? UP : FORWARD;

            public static implicit operator int( SignedAxis value ) => value.GetHashCode();

            public static implicit operator SignedAxis( int value )
            {
                if ( value == RIGHT )
                {
                    return new SignedAxis( Axis.X, 1 );
                }

                if ( value == LEFT )
                {
                    return new SignedAxis( Axis.X, -1 );
                }

                if ( value == UP )
                {
                    return new SignedAxis( Axis.Y, 1 );
                }

                if ( value == DOWN )
                {
                    return new SignedAxis( Axis.Y, -1 );
                }

                if ( value == FORWARD )
                {
                    return new SignedAxis( Axis.Z, 1 );
                }

                return new SignedAxis( Axis.Z, -1 );
            }

            public static implicit operator SignedAxis( Vector3 value )
            {
                if ( _dictionary.ContainsKey( value ) )
                {
                    return _dictionary[ value ];
                }

                Debug.LogWarning( "Rotated Axis" );
                return UP;
            }

            public static implicit operator Vector3( SignedAxis value )
            {
                if ( value == RIGHT )
                {
                    return Vector3.right;
                }

                if ( value == LEFT )
                {
                    return Vector3.left;
                }

                if ( value == UP )
                {
                    return Vector3.up;
                }

                if ( value == DOWN )
                {
                    return Vector3.down;
                }

                if ( value == FORWARD )
                {
                    return Vector3.forward;
                }

                return Vector3.back;
            }

            public static bool operator !=( SignedAxis l, SignedAxis r ) => !l.Equals( r );

            #endregion

            #region Private Constructors

            private SignedAxis( Axis axis, int sign )
            {
                _axis = axis;
                _sign = sign;
            }

            #endregion

        }
    }
}