using Assets.Scripts;
using UnityEngine;
using Windows.Kinect;

public class HandForce
{
    readonly SceneController controller = SceneController.Instance;

    public void ManageHandForce(PlayerConstructor player)
    {
        // controller.debugText.text =
        //     $"leftHandState: {player.leftHandState}\nrightHandState: {player.rightHandState}";
        if (
            (player.leftHandState == HandState.Closed || player.leftHandState == HandState.Unknown)
            && (
                player.rightHandState == HandState.Closed
                || player.rightHandState == HandState.Unknown
            )
        )
        {
            player.pushParticles = true;
            if (player.initialized && player.turnOnParticles)
            {
                player.vfxAnimator.CrossFade(player.closedClip.name, 1f);
                AlignAndCalculateVectors(player);
            }
            player.turnOnParticles = false;
        }
        else
        {
            if (player.initialized && !player.turnOnParticles)
            {
                player.vfxAnimator.CrossFade(player.openClip.name, 1f);
            }
            player.pushParticles = false;
            player.turnOnParticles = true;
            AlignAndCalculateVectors(player);
        }
    }

    void AlignAndCalculateVectors(PlayerConstructor player)
    {
        Vector3 handMidpoint =
            (player.HandLeft.transform.position + player.HandRight.transform.position) * 0.5f;

        // get vector from leftWrist to leftHandTip and normalize it
        Vector3 leftHandVector = (
            player.HandtipLeft.transform.position - player.WristLeft.transform.position
        ).normalized;
        // do same for right hand
        Vector3 rightHandVector = (
            player.HandtipLeft.transform.position - player.WristRight.transform.position
        ).normalized;
        // lerp halfway between the two vectors
        Vector3 alignmentVector = Vector3.Lerp(leftHandVector, rightHandVector, 0.5f);

        // get the distance between the two hands and remap it to 0-1
        // use that value to evaluate the alignmentVectorStrength curve
        float handDistance = Vector3.Distance(
            player.HandRight.transform.position,
            player.HandLeft.transform.position
        );
        // controller.debugText.text = "User " + player.userId + " Hand Distance: " + handDistance;

        float remappedHandDistance = Mathf.InverseLerp(
            0,
            controller.so.maxDistanceBetweenHands,
            Mathf.Clamp(handDistance, 0, controller.so.maxDistanceBetweenHands)
        );
        float alignmentVectorScaler =
            controller.so.alignmentVectorStrength.Evaluate(remappedHandDistance)
            * controller.so.alignmentVectorStrengthScaler;

        player.midpoint = handMidpoint + alignmentVector * alignmentVectorScaler;
        float distance = Vector3.Distance(player.midpoint, player.sphere.transform.position);
        Vector3 direction = player.midpoint - player.sphere.position;

        PushToTarget(player, distance, direction);
    }

    void PushToTarget(PlayerConstructor player, float distance, Vector3 direction)
    {
        float relativeDistance = Mathf.InverseLerp(
            controller.so.maxDistanceBetweenHands,
            0,
            distance
        );
        float forceDamper = controller.so.forceToMiddle.Evaluate(relativeDistance);

        Vector3 forceDirection = direction.normalized;
        Vector3 forceVector = controller.so.pushForce * forceDamper * forceDirection;

        if (player.pushParticles)
        {
            forceVector *= controller.so.handPushScaler;
            player.sphere.drag = 0;
        }
        else
        {
            // need to make sure drag never goes below min drag. Might as well clamp both sides
            player.sphere.drag = Utils.RemapClamped(
                distance,
                controller.so.maxDistanceBetweenHands,
                0,
                controller.so.minDrag,
                controller.so.maxDrag
            );
        }

        player.sphere.AddForce(forceVector);
    }
}
