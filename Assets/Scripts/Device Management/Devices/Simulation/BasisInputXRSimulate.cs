﻿using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.TransformBinders.BoneControl;
using UnityEngine;

namespace Basis.Scripts.Device_Management.Devices.Simulation
{
public class BasisInputXRSimulate : BasisInput
{
    public Transform FollowMovement;
    public bool AddSomeRandomizedInput = false;
    public float MinMaxOffset = 0.0001f;
    public float LerpAmount = 0.1f;
    public BasisInputSkeleton BasisInputSkeleton;
    public void Start()
    {
        if (BasisInputSkeleton == null)
        {
            BasisInputSkeleton = new BasisInputSkeleton();
            if (TryGetRole(out BasisBoneTrackedRole Role))
            {
                if (Role == BasisBoneTrackedRole.LeftHand)
                {
                    BasisInputSkeleton.AssignAsLeft();
                }
                else
                {
                    if (Role == BasisBoneTrackedRole.RightHand)
                    {
                        BasisInputSkeleton.AssignAsRight();
                    }
                }
            }
        }
    }
    public override void PollData()
    {
        if (AddSomeRandomizedInput)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-MinMaxOffset, MinMaxOffset), Random.Range(-MinMaxOffset, MinMaxOffset), Random.Range(-MinMaxOffset, MinMaxOffset));

            Quaternion randomRotation = Random.rotation;
            Quaternion lerpedRotation = Quaternion.Lerp(FollowMovement.localRotation, randomRotation, LerpAmount * Time.deltaTime);

            Vector3 originalPosition = FollowMovement.localPosition;
            Vector3 newPosition = Vector3.Lerp(originalPosition, originalPosition + randomOffset, LerpAmount * Time.deltaTime);

            FollowMovement.SetLocalPositionAndRotation(newPosition, lerpedRotation);
        }
        FollowMovement.GetLocalPositionAndRotation(out LocalRawPosition, out LocalRawRotation);
        LocalRawPosition /= BasisLocalPlayer.Instance.RatioPlayerToAvatarScale;

        FinalPosition = LocalRawPosition * BasisLocalPlayer.Instance.RatioPlayerToAvatarScale;
        FinalRotation = LocalRawRotation;
        if (hasRoleAssigned)
        {
            if (Control.HasTracked != BasisHasTracked.HasNoTracker)
            {
                if (AssociatedFound)
                {
                    AvatarPositionOffset = BasisDeviceMatchableNames.AvatarPositionOffset;//normally we dont do this but im doing it so we can see direct colliation
                }
                else
                {
                    AvatarPositionOffset = Vector3.zero;
                }
                Control.IncomingData.position = FinalPosition - FinalRotation * AvatarPositionOffset;
            }
            if (Control.HasTracked != BasisHasTracked.HasNoTracker)
            {
                if (AssociatedFound)
                {
                    AvatarRotationOffset = Quaternion.Euler(BasisDeviceMatchableNames.AvatarRotationOffset);//normally we dont do this but im doing it so we can see direct colliation
                }
                else
                {
                    AvatarRotationOffset = Quaternion.identity;
                }
                Control.IncomingData.rotation = FinalRotation * AvatarRotationOffset;
            }


        }
        UpdatePlayerControl();
        BasisInputSkeleton.Simulate();
    }
    public void OnDrawGizmos()
    {
        if (BasisInputSkeleton != null)
        {
            BasisInputSkeleton.OnDrawGizmos();
        }
    }
    public new void OnDestroy()
    {
        if (FollowMovement != null)
        {
            GameObject.Destroy(FollowMovement.gameObject);
        }
        base.OnDestroy();
    }
}
}