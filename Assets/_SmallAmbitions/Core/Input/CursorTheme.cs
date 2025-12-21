using UnityEngine;

namespace SmallAmbitions
{
    [CreateAssetMenu(fileName = "CursorTheme", menuName = "Small Ambitions/UI/Cursor Theme")]
    public sealed class CursorTheme : ScriptableObject
    {
        [System.Serializable]
        public struct CursorData
        {
            [field: SerializeField] public Texture2D Texture { get; private set; }
            [field: SerializeField] public Vector2 Hotspot { get; private set; }
        }

        [Header("Definitions")]
        [field: SerializeField] public CursorData DefaultCursor { get; private set; }
        [field: SerializeField] public CursorData GrabCursor { get; private set; }
        [field: SerializeField] public CursorData RotateCursor { get; private set; }
    }
}
