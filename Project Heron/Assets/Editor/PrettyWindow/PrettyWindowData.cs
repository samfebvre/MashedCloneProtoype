using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Editor.PrettyWindow
{
    public class PrettyWindowData : ScriptableSingleton<PrettyWindowData>
    {
        public string MyString;

        // private void OnEnable()
        // {
        //     hideFlags = HideFlags.HideAndDontSave;
        // }

        public void Save()
        {
            Save(true);
        }
    }
}