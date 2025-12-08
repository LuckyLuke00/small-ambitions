using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallAmbitions
{
    public enum CameraDragState
    {
        None,
        Move,
        Orbit
    }

    public sealed class CameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _mainCamera;

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _dragMoveSensitivity = 0.0075f;

        [Header("Zoom Settings")]
        [SerializeField] private float _zoomSpeed = 50f;
        [SerializeField] private float _minFov = 45f;
        [SerializeField] private float _maxFov = 60f;

        [Header("Orbit Settings")]
        [SerializeField] private float _orbitSpeed = 100f;
        [SerializeField] private float _dragOrbitSensitivity = 0.25f;

        [Header("Game Events")]
        [SerializeField] private GameEvent<CameraDragState> _cameraDragStateEvent;

        private PlayerInputActions _inputActionAsset;
        private CameraDragState _currentDragState = CameraDragState.None;

        private Vector2 _moveInput;
        private Vector2 _dragMoveInput;
        private float _zoomInput;
        private float _orbitInput;
        private Vector2 _dragOrbitInput;

        private void Awake()
        {
            _inputActionAsset = new PlayerInputActions();
        }

        private void LateUpdate()
        {
            UpdateMovement();
            UpdateZoom();
            UpdateOrbit();
        }

        private void OnEnable()
        {
            _inputActionAsset.Camera.Enable();

            _inputActionAsset.Camera.Move.performed += OnMove;
            _inputActionAsset.Camera.Move.canceled += OnMove;

            _inputActionAsset.Camera.StartDragMove.started += OnDragMoveStarted;
            _inputActionAsset.Camera.StartDragMove.canceled += OnDragMoveStarted;

            _inputActionAsset.Camera.DragMove.performed += OnDragMove;
            _inputActionAsset.Camera.DragMove.canceled += OnDragMove;

            _inputActionAsset.Camera.Zoom.performed += OnZoom;
            _inputActionAsset.Camera.Zoom.canceled += OnZoom;

            _inputActionAsset.Camera.StartDragOrbit.started += OnDragOrbitStarted;
            _inputActionAsset.Camera.StartDragOrbit.canceled += OnDragOrbitStarted;

            _inputActionAsset.Camera.Orbit.performed += OnOrbit;
            _inputActionAsset.Camera.Orbit.canceled += OnOrbit;

            _inputActionAsset.Camera.DragOrbit.performed += OnDragOrbit;
            _inputActionAsset.Camera.DragOrbit.canceled += OnDragOrbit;
        }

        private void OnDisable()
        {
            _inputActionAsset.Camera.DragOrbit.canceled -= OnDragOrbit;
            _inputActionAsset.Camera.DragOrbit.performed -= OnDragOrbit;

            _inputActionAsset.Camera.StartDragOrbit.canceled -= OnDragOrbitStarted;
            _inputActionAsset.Camera.StartDragOrbit.started -= OnDragOrbitStarted;

            _inputActionAsset.Camera.Orbit.canceled -= OnOrbit;
            _inputActionAsset.Camera.Orbit.performed -= OnOrbit;

            _inputActionAsset.Camera.Zoom.canceled -= OnZoom;
            _inputActionAsset.Camera.Zoom.performed -= OnZoom;

            _inputActionAsset.Camera.DragMove.canceled -= OnDragMove;
            _inputActionAsset.Camera.DragMove.performed -= OnDragMove;

            _inputActionAsset.Camera.StartDragMove.canceled -= OnDragMoveStarted;
            _inputActionAsset.Camera.StartDragMove.started -= OnDragMoveStarted;

            _inputActionAsset.Camera.Move.canceled -= OnMove;
            _inputActionAsset.Camera.Move.performed -= OnMove;

            _inputActionAsset.Camera.Disable();
        }

        private void OnDragMoveStarted(InputAction.CallbackContext ctx)
        {
            if (ctx.started)
            {
                SetCameraDragState(CameraDragState.Move);
            }

            if (ctx.canceled)
            {
                SetCameraDragState(CameraDragState.None);
            }
        }

        private void OnDragMove(InputAction.CallbackContext ctx)
        {
            if (_currentDragState != CameraDragState.Move)
            {
                _dragMoveInput = Vector2.zero;
                return;
            }

            _dragMoveInput = ctx.ReadValue<Vector2>();
        }

        private void OnDragOrbitStarted(InputAction.CallbackContext ctx)
        {
            if (ctx.started)
            {
                SetCameraDragState(CameraDragState.Orbit);
            }

            if (ctx.canceled)
            {
                SetCameraDragState(CameraDragState.None);
            }
        }

        private void OnDragOrbit(InputAction.CallbackContext ctx)
        {
            if (_currentDragState != CameraDragState.Orbit)
            {
                _dragOrbitInput = Vector2.zero;
                return;
            }
            _dragOrbitInput = ctx.ReadValue<Vector2>();
        }

        private void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();

        private void OnOrbit(InputAction.CallbackContext ctx) => _orbitInput = ctx.ReadValue<float>();

        private void OnZoom(InputAction.CallbackContext ctx) => _zoomInput = ctx.ReadValue<float>();

        private void UpdateMovement()
        {
            if (MathUtils.IsNearlyZero(_moveInput) && MathUtils.IsNearlyZero(_dragMoveInput))
            {
                return;
            }

            // WASD / Stick
            Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);
            Vector3 moveDelta = transform.TransformDirection(moveDirection) * _moveSpeed * Time.deltaTime;

            // Mouse drag movement
            Vector3 dragDirection = new Vector3(-_dragMoveInput.x, 0f, -_dragMoveInput.y);
            Vector3 dragDelta = transform.TransformDirection(dragDirection) * _dragMoveSensitivity;

            transform.position += (moveDelta + dragDelta);
        }

        private void UpdateZoom()
        {
            if (MathUtils.IsNearlyZero(_zoomInput))
            {
                return;
            }

            float zoomDelta = _zoomInput * _zoomSpeed * Time.deltaTime;
            _mainCamera.fieldOfView = Mathf.Clamp(_mainCamera.fieldOfView - zoomDelta, _minFov, _maxFov);
        }

        private void UpdateOrbit()
        {
            if (MathUtils.IsNearlyZero(_orbitInput) && MathUtils.IsNearlyZero(_dragOrbitInput))
            {
                return;
            }

            float inputYaw = _orbitInput * _orbitSpeed * Time.deltaTime;
            float dragYaw = _dragOrbitInput.x * _dragOrbitSensitivity;

            transform.Rotate(Vector3.up, inputYaw + dragYaw);
        }

        private void SetCameraDragState(CameraDragState newState)
        {
            if (_currentDragState == newState)
            {
                return;
            }

            _currentDragState = newState;
            _cameraDragStateEvent.Raise(_currentDragState);
        }
    }
}
