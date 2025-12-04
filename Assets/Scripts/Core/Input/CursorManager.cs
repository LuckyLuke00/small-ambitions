using UnityEngine;

namespace SmallAmbitions
{
    public sealed class CursorManager : MonoBehaviour
    {
        [Header("Cursor Graphics")]
        [SerializeField] private CursorTheme _cursorTheme;

        public void SetDefaultCursor()
        {
            SetCursor(_cursorTheme.DefaultCursor);
        }

        public void SetGrabCursor()
        {
            SetCursor(_cursorTheme.GrabCursor);
        }

        public void SetRotateCursor()
        {
            SetCursor(_cursorTheme.RotateCursor);
        }

        private void SetCursor(CursorTheme.CursorData cursorData)
        {
            if (cursorData.Texture == null)
            {
                Debug.LogWarning("Cursor texture is null. Cannot set cursor.");
                return;
            }

            Cursor.SetCursor(cursorData.Texture, cursorData.Hotspot, CursorMode.Auto);
        }

        public void OnCameraDragStateChanged(CameraDragState state)
        {
            switch (state)
            {
                case CameraDragState.None:
                    SetDefaultCursor();
                    break;

                case CameraDragState.Move:
                    SetGrabCursor();
                    break;

                case CameraDragState.Orbit:
                    SetRotateCursor();
                    break;
            }
        }
    }
}
