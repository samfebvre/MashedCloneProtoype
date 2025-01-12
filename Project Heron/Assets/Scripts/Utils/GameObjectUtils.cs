using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.Utils
{
    public static class GameObjectUtils
    {

        #region Public Methods

        public static void GetAllChildrenOfTransform( this Transform transform, ref List<Transform> children )
        {
            for ( int i = 0; i < transform.childCount; i++ )
            {
                Transform child = transform.GetChild( i );
                children.Add( child );
                child.GetAllChildrenOfTransform( ref children );
            }
        }

        public static bool TraverseHierarchyLookingForTransformWithName( this Transform transform, string objName, out Transform foundTransform )
        {
            Transform childWithName = transform.Find( objName );
            if ( childWithName != null )
            {
                foundTransform = childWithName;
                return true;
            }

            for ( int i = 0; i < transform.childCount; i++ )
            {
                Transform child = transform.GetChild( i );
                bool      found = TraverseHierarchyLookingForTransformWithName( child, objName, out foundTransform );
                if ( found )
                {
                    return true;
                }
            }

            foundTransform = null;
            return false;
        }

        #endregion

    }
}