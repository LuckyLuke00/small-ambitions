using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    [CreateAssetMenu(menuName = "Small Ambitions/Motives/Motive Settings", fileName = "New Motive Settings")]
    public class MotiveSettings : ScriptableObject
    {
        [SerializeField] private SerializableMap<MotiveType, Motive> _motives = new();

        public IReadOnlyDictionary<MotiveType, Motive> Motives => _motives;
    }
}
