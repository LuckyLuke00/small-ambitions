using UnityEngine;

namespace SmallAmbitions
{
    [System.Serializable]
    public sealed class AnimatorParameter
    {
        [SerializeField] private string _name = string.Empty;
        [SerializeField, HideInInspector] private int _hash = 0;

        public string Name => _name;
        public int Hash
        {
            get
            {
                if (_hash == 0)
                {
                    _hash = Animator.StringToHash(_name);
                    Debug.LogWarning($"AnimatorParameter: Hash for '{_name}' was uninitialized. Calculated on access: {_hash}");
                }

                return _hash;
            }
        }
    }
}
