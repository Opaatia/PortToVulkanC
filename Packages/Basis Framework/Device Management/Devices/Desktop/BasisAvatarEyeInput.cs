using Basis.Scripts.BasisSdk.Helpers;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Drivers;
using Basis.Scripts.TransformBinders.BoneControl;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Basis.Scripts.Device_Management.Devices.Desktop
{
    public class BasisAvatarEyeInput : BasisInput
    {
        public Camera Camera;
        public BasisLocalAvatarDriver AvatarDriver;
        public BasisLocalInputActions characterInputActions;
        public static BasisAvatarEyeInput Instance;
        public float RangeOfMotionBeforeTurn = 13;
        public float headDownwardForce = 0.003f;
        public float headUpwardForce = 0.001f;
        public float adjustment;
        public float crouchPercentage = 0.5f;
        public float rotationSpeed = 0.1f;
        public float rotationY;
        public float rotationX;
        public float minimumY = -80f;
        public float maximumY = 80f;
        public float DelayedResponseForRotation = 0.6f;
        public float FallBackHeight = 1.73f;
        public bool BlockCrouching;
        public float InjectedX = 0;
        public float InjectedZ = 0;
        public bool HasEyeEvents = false;
        public bool PauseLook = false;
        public async Task Initalize(string ID = "Desktop Eye", string subSystems = "BasisDesktopManagement")
        {
            Debug.Log("Initalizing Avatar Eye");
            if (BasisLocalPlayer.Instance.AvatarDriver != null)
            {
                LocalRawPosition = new Vector3(InjectedX, BasisLocalPlayer.Instance.PlayerEyeHeight, InjectedZ);
                LocalRawRotation = Quaternion.identity;
            }
            else
            {
                LocalRawPosition = new Vector3(InjectedX, FallBackHeight, InjectedZ);
                LocalRawRotation = Quaternion.identity;
            }
            FinalPosition = LocalRawPosition;
            FinalRotation = LocalRawRotation;
            await InitalizeTracking(ID, ID, subSystems, true, BasisBoneTrackedRole.CenterEye);
            if (BasisHelpers.CheckInstance(Instance))
            {
                Instance = this;
            }
            PlayerInitialized();
            BasisCursorManagement.OverrideableLock(nameof(BasisAvatarEyeInput));
            if (HasEyeEvents == false)
            {
                BasisLocalPlayer.Instance.OnLocalAvatarChanged += PlayerInitialized;
                BasisLocalPlayer.Instance.OnPlayersHeightChanged += BasisLocalPlayer_OnPlayersHeightChanged;
                BasisCursorManagement.OnCursorStateChange += OnCursorStateChange;
                BasisPointRaycaster.UseWorldPosition = false;
                HasEyeEvents = true;
            }
        }
        private void OnCursorStateChange(CursorLockMode cursor, bool newCursorVisible)
        {
            Debug.Log("cursor changed to : " + cursor.ToString() + " | Cursor Visible : " + newCursorVisible);
            if (cursor == CursorLockMode.Locked)
            {
                PauseLook = false;
            }
            else
            {
                PauseLook = true;
            }
        }
        public new void OnDestroy()
        {
            if (HasEyeEvents)
            {
                BasisLocalPlayer.Instance.OnLocalAvatarChanged -= PlayerInitialized;
                BasisLocalPlayer.Instance.OnPlayersHeightChanged -= BasisLocalPlayer_OnPlayersHeightChanged;
                BasisCursorManagement.OnCursorStateChange -= OnCursorStateChange;
                HasEyeEvents = false;
            }
            base.OnDestroy();
        }
        private void BasisLocalPlayer_OnPlayersHeightChanged(bool State)
        {
            BasisLocalPlayer.Instance.PlayerEyeHeight = BasisLocalPlayer.Instance.AvatarDriver.ActiveEyeHeight();
        }
        public void PlayerInitialized()
        {
            characterInputActions = BasisLocalInputActions.Instance;
            if (characterInputActions != null)
            {
                characterInputActions.CharacterEyeInput = this;
            }
            AvatarDriver = BasisLocalPlayer.Instance.AvatarDriver;
            Camera = BasisLocalCameraDriver.Instance.Camera;
            BasisDeviceManagement Device = BasisDeviceManagement.Instance;
            int count = Device.BasisLockToInputs.Count;
            for (int Index = 0; Index < count; Index++)
            {
                Device.BasisLockToInputs[Index].FindRole();
            }
        }
        public new void OnDisable()
        {
            BasisLocalPlayer.Instance.OnLocalAvatarChanged -= PlayerInitialized;
            base.OnDisable();
        }
        public void HandleMouseRotation(Vector2 lookVector)
        {
            BasisPointRaycaster.ScreenPoint = Mouse.current.position.value;
            if (!isActiveAndEnabled || PauseLook)
            {
                return;
            }
            rotationX += lookVector.x * rotationSpeed;
            rotationY -= lookVector.y * rotationSpeed;
        }
        public override void PollData()
        {
            if (hasRoleAssigned)
            {
                // Apply modulo operation to keep rotation within 0 to 360 range
                rotationX %= 360f;
                rotationY %= 360f;
                // Clamp rotationY to stay within the specified range
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
                LocalRawRotation = Quaternion.Euler(rotationY, rotationX, 0);
                Vector3 adjustedHeadPosition = new Vector3(InjectedX, BasisLocalPlayer.Instance.PlayerEyeHeight, InjectedZ);
                if (BasisLocalInputActions.Crouching && BlockCrouching == false)
                {
                    adjustedHeadPosition.y -= Control.TposeLocal.position.y * crouchPercentage;
                }

                CalculateAdjustment();
                adjustedHeadPosition.y -= adjustment;
                LocalRawPosition = adjustedHeadPosition;
                Control.IncomingData.position = LocalRawPosition;
                Control.IncomingData.rotation = LocalRawRotation;
            }
            FinalPosition = LocalRawPosition;
            FinalRotation = LocalRawRotation;
            UpdatePlayerControl();
            BasisInputEye.LeftPosition = this.transform.position;
            BasisInputEye.RightPosition = this.transform.position;
        }
        public void CalculateAdjustment()
        {
            if (rotationY > 0)
            {
                // Positive rotation
                adjustment = Mathf.Abs(rotationY) * (headDownwardForce * BasisLocalPlayer.Instance.AvatarDriver.ActiveEyeHeight() / Control.TposeLocal.position.y);
            }
            else
            {
                // Negative rotation
                adjustment = Mathf.Abs(rotationY) * (headUpwardForce * BasisLocalPlayer.Instance.AvatarDriver.ActiveEyeHeight() / Control.TposeLocal.position.y);
            }
        }
    }
}