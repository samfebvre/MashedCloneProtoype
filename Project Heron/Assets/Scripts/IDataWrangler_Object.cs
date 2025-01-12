using UnityEngine;

namespace DefaultNamespace
{
    public interface IDataWrangler_Object
    {
        private MonoBehaviour ThisMono      => this as MonoBehaviour;
        public  Vector3       WorldPosition => ThisMono.transform.position;
        public  string        GetDebugInfo();
    }
}