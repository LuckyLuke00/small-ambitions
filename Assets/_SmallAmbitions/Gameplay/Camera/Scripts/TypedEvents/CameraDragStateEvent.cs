using System;
using UnityEngine;
using UnityEngine.Events;

namespace SmallAmbitions
{
    [CreateAssetMenu(menuName = "Small Ambitions/Game Events/Camera Drag State Event", fileName = "CameraDragStateEvent")]
    public sealed class CameraDragStateEvent : GameEvent<CameraDragState>
    { }

    [Serializable]
    public sealed class CameraDragStateUnityEvent : UnityEvent<CameraDragState>
    { }
}
