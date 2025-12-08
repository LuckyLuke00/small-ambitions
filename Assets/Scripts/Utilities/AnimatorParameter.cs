using UnityEngine;

namespace SmallAmbitions
{
    [System.Serializable]
    public sealed class AnimatorParameter : ISerializationCallbackReceiver
    {
        [SerializeField] private string _name = string.Empty;
        [SerializeField, HideInInspector] private int _hash = 0;

        public string Name => _name;
        public int Hash => _hash;

        public void OnBeforeSerialize() { /* No action needed before serialization. */ }

        public void OnAfterDeserialize()
        {
            _hash = Animator.StringToHash(_name);
        }
    }
}
