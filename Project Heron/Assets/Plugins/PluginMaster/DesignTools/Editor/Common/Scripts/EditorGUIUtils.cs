using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PluginMaster
{
    public static class EditorGUIUtils
    {

        #region LAYER MASK FIELD

        public static LayerMask FieldToLayerMask( int field )
        {
            LayerMask mask   = 0;
            string[]  layers = InternalEditorUtility.layers;
            for ( int layerIdx = 0; layerIdx < layers.Length; layerIdx++ )
            {
                if ( ( field & ( 1 << layerIdx ) ) == 0 )
                {
                    continue;
                }

                mask |= 1 << LayerMask.NameToLayer( layers[ layerIdx ] );
            }

            return mask;
        }

        public static int LayerMaskToField( LayerMask mask )
        {
            int      field  = 0;
            string[] layers = InternalEditorUtility.layers;
            for ( int layerIdx = 0; layerIdx < layers.Length; layerIdx++ )
            {
                if ( ( mask & ( 1 << LayerMask.NameToLayer( layers[ layerIdx ] ) ) ) == 0 )
                {
                    continue;
                }

                field |= 1 << layerIdx;
            }

            return field;
        }

        #endregion

        #region CUSTOM FIELDS

        #region AXIS FIELD

        private static Vector3[] directions =
        {
            Vector3.right, Vector3.left,
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back,
        };

        private static string[] directionNames =
        {
            "+X", "-X",
            "+Y", "-Y",
            "+Z", "-Z",
        };

        public static Vector3 AxisField( string label, Vector3 value )
        {
            int selectedIndex = Array.IndexOf( directions, value );
            selectedIndex = EditorGUILayout.Popup( label, selectedIndex, directionNames );
            return directions[ selectedIndex ];
        }

        #endregion

        #region RANGE FIELD

        public static RandomUtils.Range RangeField( string label, RandomUtils.Range value )
        {
            using ( new GUILayout.HorizontalScope() )
            {
                if ( label != string.Empty )
                {
                    GUILayout.Label( label );
                }

                float prevLabelW = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 30;
                GUILayout.Label( "Between:" );
                value.v1                    = EditorGUILayout.FloatField( value.v1 );
                value.v2                    = EditorGUILayout.FloatField( value.v2 );
                EditorGUIUtility.labelWidth = prevLabelW;
            }

            return value;
        }

        public static RandomUtils.Range3 Range3Field( string label, RandomUtils.Range3 value )
        {
            using ( new GUILayout.VerticalScope() )
            {
                if ( label != string.Empty )
                {
                    GUILayout.Label( label );
                }

                GUILayout.Label( "Between:" );
                value.v1 = EditorGUILayout.Vector3Field( string.Empty, value.v1 );
                value.v2 = EditorGUILayout.Vector3Field( string.Empty, value.v2 );

            }

            return value;
        }

        #endregion

        #region MULTITAG FIELD

        public class MultiTagField
        {

            #region Statics and Constants

            private const string NOTHING    = "Nothing";
            private const string EVERYTHING = "Everything";
            private const string MIXED      = "Mixed ...";

            #endregion

            #region Public Fields

            public Action<List<string>,
                List<string>, string> OnChange;

            #endregion

            #region Public Methods

            public static MultiTagField Instantiate( string label, List<string> tags, string key )
            {
                MultiTagField field = new MultiTagField( label, tags, key );
                field.Show();
                return field;
            }

            #endregion

            #region Private Fields

            private string _key;

            private string       _label;
            private List<string> _tags;

            #endregion

            #region Private Constructors

            private MultiTagField( string label, List<string> tags, string key )
            {
                ( _label, _tags, _key ) = ( label, tags, key );
            }

            #endregion

            #region Private Methods

            private void SelectTag( object obj )
            {
                List<string>    originalList = new List<string>( _tags );
                HashSet<string> originalSet  = new HashSet<string>( _tags );

                void CheckChange()
                {
                    HashSet<string> newSet = new HashSet<string>( _tags );
                    if ( !originalSet.SetEquals( newSet ) )
                    {
                        OnChange( originalList, _tags, _key );
                    }
                }

                string tag = (string)obj;
                if ( tag == NOTHING )
                {
                    _tags.Clear();
                    CheckChange();
                    return;
                }

                if ( tag == EVERYTHING )
                {
                    _tags.Clear();
                    _tags.AddRange( InternalEditorUtility.tags );
                    CheckChange();
                    return;
                }

                if ( _tags.Contains( tag ) )
                {
                    _tags.Remove( tag );
                }
                else
                {
                    _tags.Add( tag );
                }

                CheckChange();
            }

            private void Show()
            {
                string[] allTags = InternalEditorUtility.tags;
                string text = _tags.Count == 0
                    ? NOTHING
                    : _tags.Count == allTags.Length
                        ? EVERYTHING
                        : _tags.Count > 1
                            ? MIXED
                            : _tags[ 0 ];

                using ( new GUILayout.HorizontalScope() )
                {
                    if ( _label    != null
                         && _label != string.Empty )
                    {
                        GUILayout.Label( _label, GUILayout.Width( EditorGUIUtility.labelWidth ) );
                    }

                    if ( GUILayout.Button( text, EditorStyles.popup,
                            GUILayout.MinWidth( EditorGUIUtility.fieldWidth ) ) )
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem( new GUIContent( NOTHING ),    false, SelectTag, NOTHING );
                        menu.AddItem( new GUIContent( EVERYTHING ), false, SelectTag, EVERYTHING );
                        foreach ( string tag in InternalEditorUtility.tags )
                        {
                            menu.AddItem( new GUIContent( tag ), _tags.Contains( tag ), SelectTag, tag );
                        }

                        menu.ShowAsContext();
                    }
                }
            }

            #endregion

        }

        #endregion

        #region OBJECT ARRAY FIELD

        public static OBJ_TYPE[] ObjectArrayField<OBJ_TYPE>( string label, OBJ_TYPE[] objArray, ref bool foldout )
            where OBJ_TYPE : Object
        {
            int        size   = objArray == null ? 0 : objArray.Length;
            OBJ_TYPE[] result = objArray;
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup( foldout, label );
            if ( !foldout )
            {
                return result;
            }

            using ( new GUILayout.HorizontalScope() )
            {
                GUILayout.Space( 20 );
                using ( new GUILayout.VerticalScope() )
                {
                    EditorGUIUtility.labelWidth = 40;
                    size                        = Mathf.Clamp( EditorGUILayout.IntField( "Size:", size ), 0, 10 );
                    result                      = new OBJ_TYPE[ size ];
                    for ( int i = 0; i < size; ++i )
                    {
                        OBJ_TYPE obj = i < objArray.Length ? objArray[ i ] : null;
                        result[ i ] = (OBJ_TYPE)EditorGUILayout.ObjectField( obj, typeof(OBJ_TYPE), true );
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            return result;
        }

        #endregion

        #region OBJECT ARRAY FIELD WITH BUTTONS

        public static OBJ_TYPE[] ObjectArrayFieldWithButtons<OBJ_TYPE>( string   label,   OBJ_TYPE[] objArray,
                                                                        ref bool foldout, out bool   changed )
            where OBJ_TYPE : Object
        {
            List<OBJ_TYPE> result    = new List<OBJ_TYPE>();
            int            removeIdx = -1;
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup( foldout, label );
            changed = false;
            if ( !foldout )
            {
                return objArray;
            }

            using ( new GUILayout.HorizontalScope() )
            {
                GUILayout.Space( 20 );
                using ( new GUILayout.VerticalScope() )
                {
                    if ( objArray != null )
                    {
                        foreach ( OBJ_TYPE obj in objArray )
                        {
                            using ( new GUILayout.HorizontalScope() )
                            {
                                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                                {
                                    result.Add( (OBJ_TYPE)EditorGUILayout.ObjectField( obj,
                                        typeof(OBJ_TYPE), true ) );
                                    if ( check.changed )
                                    {
                                        changed = true;
                                    }
                                }

                                if ( GUILayout.Button( "Remove", GUILayout.Width( 70 ) ) )
                                {
                                    removeIdx = result.Count - 1;
                                    changed   = true;
                                }
                            }
                        }
                    }

                    using ( new GUILayout.HorizontalScope() )
                    {
                        GUILayout.FlexibleSpace();
                        if ( GUILayout.Button( "Add", GUILayout.Width( 70 ) ) )
                        {
                            result.Add( null );
                            changed = true;
                        }
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            if ( removeIdx >= 0 )
            {
                result.RemoveAt( removeIdx );
            }

            return result.ToArray();
        }

        #endregion

        #endregion

    }
}