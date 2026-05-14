using AnyRPG;
//using System.Drawing.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnyRPG
{
    public class AnyRPGCameraController : ConfiguredMonoBehaviour
    {
        [SerializeField]
        private Camera usedCamera = null;

        private Transform cameraTransform = null;

        [SerializeField]
        private Transform target = null;

        [Tooltip("Ignore these layers when checking if walls are in the way of the camera view of the character")]
        [SerializeField]
        private LayerMask ignoreMask = ~0;

        public float cameraFollowSpeed = 10f;
        public float zoomSpeed = 4f;
        private float gamepadZoomSpeed = 0.05f;
        public float minZoom = 2f;
        public float maxZoom = 15f;
        public float maxVerticalPan = 45;
        public float minVerticalPan = -45;
        public float firstPersonMaxVerticalPan = 75;
        public float firstPersonMinVerticalPan = -35;
        private float currentMaxVerticalPan = 45;
        private float currentMinVerticalPan = -45;

        public float initialYDegrees = 0f;
        public float initialXDegrees = 0f;
        public float initialZoomDistance = 4f;

        public float pitch = 2f;
        public float yawSpeed = 10f;
        public float analogYawSpeed = 5f;

        public bool followBehind = true;

        // private variables
        private Vector3 wantedPosition;
        private Vector3 targetPosition;
        private float userOffsetAngle;
        private Vector3 cameraTransformForward;
        private bool firstPersonView = false;
        private Vector3 wantedDirection;

        private float currentZoomDistance = 0f;
        private float currentYDegrees = 0f;
        private float currentXDegrees = 0f;

        private bool cameraPan = false;
        private bool cameraZoom = false;
        private bool turnWithCamera = false;

        private RaycastHit wallHit = new RaycastHit();

        // game manager references
        protected InputManager inputManager = null;
        protected NamePlateManager namePlateManager = null;
        protected UIManager uIManager = null;
        protected WindowManager windowManager = null;
        protected PlayerManagerClient playerManagerClient = null;
        protected ControlsManager controlsManager = null;

        //[Header("Action Camera")]
        //[Tooltip("Maximum distance to detect interactables when in Action mode.")]
        //[SerializeField] private float actionInteractDistance = 5f;

        //[Tooltip("Layers to include in the Action mode interaction raycast.")]
        //[SerializeField] private LayerMask actionInteractMask = ~0;

        //[Tooltip("Key used to interact in Action mode.")]
        //[SerializeField] private KeyCode actionInteractKey = KeyCode.E;

        //[Tooltip("Mouse sensitivity when in Action mode.")]
        //[SerializeField] private float actionMouseSensitivity = 1f;

        //[Tooltip("Assign your existing crosshair dot Image here.")]
        //[SerializeField] private Image crosshairDot = null;

        //[SerializeField] private Color crosshairDefaultColor = Color.white;
        //[SerializeField] private Color crosshairInteractColor = Color.green;
        //[SerializeField] private Color crosshairOutOfRangeColor = Color.orange;

        //private Interactable actionLookedAtInteractable = null;
        //private bool actionLookedAtCanInteract = false;

        public Transform Target { get => target; set => target = value; }
        public Vector3 WantedDirection { get => wantedDirection; set => wantedDirection = value; }
        public bool FirstPersonView { get => firstPersonView; }

        public float CurrentXDegrees { get => currentXDegrees; set => currentXDegrees = value; }
        public float CurrentYDegrees { get => currentYDegrees; set => currentYDegrees = value; }
        public float UserOffsetAngle { get => userOffsetAngle; set => userOffsetAngle = value; }
        public float CurrentMinVerticalPan { get => currentMinVerticalPan; }
        public float CurrentMaxVerticalPan { get => currentMaxVerticalPan; }


        public override void Configure(SystemGameManager systemGameManager)
        {
            base.Configure(systemGameManager);

            if (usedCamera != null)
            {
                cameraTransform = usedCamera.transform;
            }
            currentMaxVerticalPan = maxVerticalPan;
            currentMinVerticalPan = minVerticalPan;

            if (systemConfigurationManager.AllowFirstPersonCamera)
            {
                minZoom = 0.05f;
            }
            SetInitialDegreesAndZoom();

            // If starting in Action mode, lock the cursor immediately
            //if (systemConfigurationManager.CameraViewMode == CameraViewMode.Action)
            //{
            //    LockCursor();
            //}

            //CameraMode = (CameraViewMode)PlayerPrefs.GetInt("CameraControlMode", 0);
            //bool actionMode = CameraMode == CameraViewMode.Action;
            //if (actionMode)
            //{
            //    crosshairDot.enabled = true;
            //}
        }

        public override void SetGameManagerReferences()
        {
            base.SetGameManagerReferences();
            inputManager = systemGameManager.InputManager;
            uIManager = systemGameManager.UIManager;
            namePlateManager = uIManager.NamePlateManager;
            windowManager = systemGameManager.WindowManager;
            playerManagerClient = systemGameManager.PlayerManagerClient;
            controlsManager = systemGameManager.ControlsManager;
        }

        private void SetInitialDegreesAndZoom()
        {
            if (systemConfigurationManager.CameraViewMode == CameraViewMode.Isometric)
            {
                SetIsometricInitialValues();
            }
            else
            {
                // Classic and Action both use the free orbit starting values
                SetFreeInitialvalues();
            }
        }

        private void SetIsometricInitialValues()
        {
            currentZoomDistance = systemConfigurationManager.InitialIsometricVector.magnitude;
            Quaternion isoRotation = Quaternion.LookRotation(-systemConfigurationManager.InitialIsometricVector);
            currentXDegrees = isoRotation.eulerAngles.y;
            currentYDegrees = -isoRotation.eulerAngles.x;
        }

        private void SetFreeInitialvalues()
        {
            if (firstPersonView == true)
            {
                currentZoomDistance = minZoom;
            }
            else
            {
                currentZoomDistance = initialZoomDistance;
            }
            currentYDegrees = initialYDegrees;
            currentXDegrees = initialXDegrees;
        }

        public void ClearTarget()
        {
            target = null;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            SetTargetPosition();
        }

        private void SetTargetPosition()
        {
            targetPosition = target.position + Vector3.up * pitch;
        }

        public void SetTargetPositionRaw(Vector3 rawTargetPosition, Vector3 forwardDirection)
        {
            SetInitialDegreesAndZoom();

            targetPosition = rawTargetPosition + Vector3.up * pitch;
            if (systemConfigurationManager.CameraViewMode == CameraViewMode.Isometric)
            {
                Quaternion isoRotation = Quaternion.Euler(-currentYDegrees, currentXDegrees, 0);
                transform.position = targetPosition + (isoRotation * Vector3.back * currentZoomDistance);
            }
            else
            {
                if (forwardDirection == Vector3.zero)
                {
                    transform.position = targetPosition - new Vector3(0, 0, currentZoomDistance);
                }
                else
                {
                    transform.position = targetPosition - (Quaternion.LookRotation(forwardDirection) * new Vector3(0, 0, currentZoomDistance));
                }
            }

            LookAtTargetPosition();
        }

        public void InitializeCamera(Transform newTarget)
        {
            SetTarget(newTarget);
            JumpToFollowSpot();
        }

        public void InitializeCamera(Transform newTarget, float newPitch)
        {
            pitch = newPitch;
            SetTarget(newTarget);
            JumpToFollowSpot();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            cameraPan = false;
            cameraZoom = false;
            turnWithCamera = false;
            bool wasMinZoom = currentZoomDistance == minZoom;

            SetTargetPosition();

            // ====MOUSE ZOOM====
            if (inputManager.mouseScrolled
                && (!EventSystem.current.IsPointerOverGameObject() || namePlateManager.MouseOverNamePlate()))
            {
                currentZoomDistance += (Input.GetAxis("Mouse ScrollWheel") * zoomSpeed * -1);
                currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoom, maxZoom);
                cameraZoom = true;
                if (systemConfigurationManager.AllowFirstPersonCamera)
                {
                    if (currentZoomDistance == minZoom && wasMinZoom == false)
                    {
                        ActivateFirstPersonView();
                    }
                    else if (currentZoomDistance > minZoom && wasMinZoom == true)
                    {
                        DeactivateFirstPersonView();
                    }
                }
            }

            // ====GAMEPAD ZOOM====
            if (playerManagerClient.ActiveUnitController?.CharacterAbilityManager.WaitingForTarget() == false)
            {
                if ((windowManager.CurrentWindow == null || windowManager.CurrentWindow.CaptureCamera == false)
                    && Input.GetAxis("RightAnalogVertical") != 0f
                    && inputManager.KeyBindWasPressedOrHeld("JOYSTICKBUTTON9"))
                {
                    currentZoomDistance += (Input.GetAxis("RightAnalogVertical") * gamepadZoomSpeed * -1);
                    currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoom, maxZoom);
                    cameraZoom = true;
                }
            }

            // ====ACTION MODE====
            if (systemConfigurationManager.CameraViewMode == CameraViewMode.Action)
            {
                HandleActionModeLook();
                //HandleActionModeInteract();
            }

            // ====CLASSIC MODE====
            if (systemConfigurationManager.CameraViewMode == CameraViewMode.Classic)
            {
                if (IsMousePanning())
                {
                    float usedTurnSpeed = 0f;
                    if (IsTurningWithCamera())
                    {
                        usedTurnSpeed = PlayerPrefs.GetFloat("MouseTurnSpeed") + 0.5f;
                        turnWithCamera = true;
                        userOffsetAngle = 0f;
                    }
                    else
                    {
                        usedTurnSpeed = PlayerPrefs.GetFloat("MouseLookSpeed") + 0.5f;
                    }
                    currentXDegrees += Input.GetAxis("Mouse X") * yawSpeed * usedTurnSpeed;
                    currentYDegrees += (Input.GetAxis("Mouse Y") * yawSpeed * usedTurnSpeed) * (PlayerPrefs.GetInt("MouseInvert") == 0 ? 1 : -1);

                    if (inputManager.rightMouseButtonDown == false && playerManagerClient.PlayerController.mouseLookActive == false)
                    {
                        userOffsetAngle = Mathf.DeltaAngle(target.eulerAngles.y, currentXDegrees);
                    }

                    cameraPan = true;
                }
                else if (firstPersonView == true && playerManagerClient.PlayerController.MovementData.HasMoveInput())
                {
                    if (playerManagerClient.PlayerController.MovementData.HasTurnInput() == false)
                    {
                        turnWithCamera = true;
                        userOffsetAngle = 0f;
                    }
                }
            }

            // ====GAMEPAD PAN==== (Classic and Action both support gamepad pan)
            if (systemConfigurationManager.CameraViewMode == CameraViewMode.Classic ||
                systemConfigurationManager.CameraViewMode == CameraViewMode.Action)
            {
                if (playerManagerClient.ActiveUnitController?.CharacterAbilityManager.WaitingForTarget() == false)
                {
                    if ((windowManager.CurrentWindow == null || windowManager.CurrentWindow.CaptureCamera == false)
                    && inputManager.KeyBindWasPressedOrHeld("JOYSTICKBUTTON9") == false
                    && (Input.GetAxis("RightAnalogHorizontal") != 0f || Input.GetAxis("RightAnalogVertical") != 0f))
                    {
                        if (Input.GetAxis("RightAnalogHorizontal") != 0f)
                        {
                            currentXDegrees += Input.GetAxis("RightAnalogHorizontal") * analogYawSpeed * (PlayerPrefs.GetFloat("JoystickLookSpeed"));
                            cameraPan = true;
                        }
                        if (Input.GetAxis("RightAnalogVertical") != 0f)
                        {
                            currentYDegrees += (Input.GetAxis("RightAnalogVertical") * analogYawSpeed * (PlayerPrefs.GetFloat("JoystickLookSpeed"))) * (PlayerPrefs.GetInt("JoystickInvert") == 0 ? 1 : -1);
                            cameraPan = true;
                        }
                    }
                }

                if (currentXDegrees > 180f)
                {
                    currentXDegrees -= 360f;
                }
                if (currentXDegrees < -180f)
                {
                    currentXDegrees += 360f;
                }

                if (cameraPan || (currentZoomDistance == minZoom && wasMinZoom == false))
                {
                    currentYDegrees = Mathf.Clamp(currentYDegrees, currentMinVerticalPan, currentMaxVerticalPan);
                }
            }

            SetWantedPosition();

            CompensateForWalls();
            if (cameraZoom || cameraPan)
            {
                JumpToWantedPosition();
            }
            else
            {
                SmoothToWantedPosition();
            }
            LookAtTargetPosition();

            SystemEventManager.TriggerEvent("AfterCameraUpdate", new EventParamProperties());
        }

        //=========ACTION MODE==========
        private void HandleActionModeLook()
        {
            // Stop camera movement and release cursor when any UI window is open
            if (windowManager.WindowStack.Count > 0)
            {
                return;
            }

            float turnSpeed = (PlayerPrefs.GetFloat("MouseTurnSpeed") + 0.1f);
            // Read raw mouse delta and apply directly to the orbit angles
            float mouseX = Input.GetAxis("Mouse X") * yawSpeed * turnSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * yawSpeed * turnSpeed * (PlayerPrefs.GetInt("MouseInvert") == 0 ? 1 : -1);

            if (mouseX != 0f || mouseY != 0f)
            {
                currentXDegrees += mouseX;
                currentYDegrees += mouseY;
                currentYDegrees = Mathf.Clamp(currentYDegrees, currentMinVerticalPan, currentMaxVerticalPan);
                cameraPan = true;
            }

            // In Action mode the player always faces the camera direction
            turnWithCamera = true;
            userOffsetAngle = 0f;
        }

        private bool IsTurningWithCamera()
        {
            if (playerManagerClient.PlayerController.mouseLookActive)
            {
                return true;
            }
            if (inputManager.rightMouseButtonDown
                && (inputManager.rightMouseButtonDownPosition != Input.mousePosition || playerManagerClient.PlayerController.MovementData.HasMoveInput()))
            {
                return true;
            }
            return false;
        }

        private bool IsMousePanning()
        {
            if (uIManager.DragInProgress)
            {
                return false;
            }
            if (playerManagerClient.PlayerController.mouseLookActive)
            {
                return true;
            }
            if (inputManager.rightMouseButtonDown && !inputManager.rightMouseButtonClickedOverUI)
            {
                return true;
            }
            if (inputManager.leftMouseButtonDown && !inputManager.leftMouseButtonClickedOverUI)
            {
                return true;
            }
            return false;
        }

        private void ActivateFirstPersonView()
        {
            firstPersonView = true;
            playerManagerClient.UnitController?.UnitModelController.ActivateFirstPersonView();
            currentMaxVerticalPan = firstPersonMaxVerticalPan;
            currentMinVerticalPan = firstPersonMinVerticalPan;
        }

        private void DeactivateFirstPersonView()
        {
            firstPersonView = false;
            playerManagerClient.UnitController.UnitModelController.DeactivateFirstPersonView();
            playerManagerClient.UnitController.NamePlateController.AddNamePlate();
            currentMaxVerticalPan = maxVerticalPan;
            currentMinVerticalPan = minVerticalPan;
        }

        //public void SetCameraMode(CameraViewMode mode)
        //{
        //    CameraMode = mode;
        //    PlayerPrefs.SetInt("CameraControlMode", (int)mode);
        //    PlayerPrefs.Save();

        //    bool actionMode = mode == CameraViewMode.Action;
        //    //systemGameManager.UIManager.SetCrosshairActive(freeLookMode);
        //}

        private void CompensateForWalls()
        {
            Debug.DrawLine(targetPosition, wantedPosition, Color.cyan);
            if (Physics.Linecast(targetPosition, wantedPosition, out wallHit, ~ignoreMask))
            {
                Debug.DrawRay(wallHit.point, wallHit.point - targetPosition, Color.red);
                wantedPosition = new Vector3(wallHit.point.x, wallHit.point.y, wallHit.point.z);
                wantedPosition = Vector3.MoveTowards(wantedPosition, targetPosition, 0.2f);
            }
        }

        private void SetWantedPosition()
        {
            if (!turnWithCamera)
            {
                if (playerManagerClient.PlayerController != null
                    && systemConfigurationManager.CameraViewMode != CameraViewMode.Isometric
                    && systemConfigurationManager.CameraViewMode != CameraViewMode.Action
                    && (playerManagerClient.PlayerController.MovementData.HasMoveInput() || playerManagerClient.PlayerController.MovementData.HasTurnInput())
                    && playerManagerClient.PlayerController.MovementData.RotateModelMode == false)
                {
                    currentXDegrees = target.eulerAngles.y + userOffsetAngle;
                }
                else
                {
                    if (systemGameManager.GameMode == GameMode.Local)
                    {
                        userOffsetAngle = Mathf.DeltaAngle(target.eulerAngles.y, currentXDegrees);
                    }
                    else
                    {
                        userOffsetAngle = Mathf.DeltaAngle(target.parent.transform.eulerAngles.y, currentXDegrees);
                    }
                }
            }

            Quaternion orbitRotation = Quaternion.Euler(-currentYDegrees, currentXDegrees, 0);
            Vector3 directionToCamera = orbitRotation * Vector3.back;

            wantedPosition = target.position + new Vector3(0, pitch, 0) + (directionToCamera * currentZoomDistance);
            wantedDirection = orbitRotation * Vector3.forward;
        }

        private void JumpToWantedPosition()
        {
            transform.position = wantedPosition;
        }

        private void SmoothToWantedPosition()
        {
            transform.position = Vector3.MoveTowards(transform.position, wantedPosition, cameraFollowSpeed);
        }

        private void LookAtTargetPosition()
        {
            cameraTransformForward = new Vector3(targetPosition.x, 0f, targetPosition.z) - new Vector3(transform.position.x, 0f, transform.position.z);
            if (cameraTransformForward != Vector3.zero)
            {
                transform.forward = cameraTransformForward;
            }
            cameraTransform.LookAt(targetPosition);
        }

        private void InitializeFollowLocation()
        {
            if (systemConfigurationManager.CameraViewMode != CameraViewMode.Isometric)
            {
                userOffsetAngle = 0f;
                currentXDegrees = target.eulerAngles.y + userOffsetAngle;
            }
            else
            {
                userOffsetAngle = Mathf.DeltaAngle(target.eulerAngles.y, currentXDegrees);
            }
        }

        private void JumpToFollowSpot()
        {
            InitializeFollowLocation();
            SetWantedPosition();
            JumpToWantedPosition();
            LookAtTargetPosition();
        }

    }
}