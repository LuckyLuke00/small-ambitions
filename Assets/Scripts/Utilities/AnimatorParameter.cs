using UnityEngine;

namespace SmallAmbitions
{
    [System.Serializable]
    public sealed class AnimatorParameter
    {
        public string name;
        [HideInInspector] public int hash;

        public int GetHash() => hash != 0 ? hash : (hash = Animator.StringToHash(name));
    }
}
