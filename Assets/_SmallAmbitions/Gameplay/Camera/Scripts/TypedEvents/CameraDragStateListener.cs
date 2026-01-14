namespace SmallAmbitions
{
    public sealed class CameraDragStateListener : GameEventListener<CameraDragState, CameraDragStateEvent, CameraDragStateUnityEvent>
    {
        protected override void OnEventRaised(CameraDragState value)
        {
            _response?.Invoke(value);
        }
    }
}
