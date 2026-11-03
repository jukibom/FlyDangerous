using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BedtimeCore.EditorHistory
{
    [Serializable]
    public class SerializedTarget
    {
        [SerializeField]
        private Object _unityObject;

        private object _normalObject;

        public SerializedTarget(object obj) => SetValue(obj);

        public void SetValue(object obj)
        {
            if (obj == null)
            {
                _unityObject = null;
                _normalObject = null;
                return;    
            }
            
            if (obj is Object unityObject)
            {
                _unityObject = unityObject;
                _normalObject = null;
            }
            else
            {
                _normalObject = obj;
                _unityObject = null;
            }
        }
        
        public object Value => IsUnityObject ? UnityObject : NormalObject;

        public Object UnityObject => _unityObject;

        public object NormalObject => _normalObject;
        
        public bool IsUnityObject => _unityObject != null;
        
        public bool IsNormalObject => _normalObject != null;

        public bool IsValid => UnityObject != null || NormalObject != null;
    }
}