using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu]
public class SceneSettingsSO : ScriptableObject
{
    [BoxGroup("Gravity Attraction")]
    public float g;

    [BoxGroup("Gravity Attraction")]
    public float maxTowardsForce;

    [BoxGroup("Gravity Attraction")]
    public float maxAwayFromForce;

    [BoxGroup("Gravity Attraction")]
    public float gravityForceDamper;

    [BoxGroup("Gravity Attraction")]
    public float stopGravityDistance;

    [BoxGroup("Gravity Attraction")]
    public float stopMovingDistance;

    [BoxGroup("Gravity Attraction")]
    public float stopVelocity;

    [BoxGroup("Gravity Attraction")]
    public float attractionRadiusMultiplier;

    [BoxGroup("Hands Attraction")]
    public AnimationCurve forceToMiddle;

    [BoxGroup("Hands Attraction")]
    public float pushForce;

    [BoxGroup("Hands Attraction")]
    public float minDrag;

    [BoxGroup("Hands Attraction")]
    public float maxDrag;

    [BoxGroup("Hands Attraction")]
    public AnimationCurve alignmentVectorStrength;

    [BoxGroup("Hands Attraction")]
    public float alignmentVectorStrengthScaler;

    [BoxGroup("Hands Attraction")]
    public float handPushScaler;

    [BoxGroup("Intrinsic Pulsation")]
    [Range(0, 10f)]
    public float pulseAmount;

    [BoxGroup("Intrinsic Pulsation")]
    public float pulseSpeed;

    [BoxGroup("Intrinsic Pulsation")]
    public float graphLimit;

    [BoxGroup("Intrinsic Pulsation")]
    public float[] pulseFreqs;

    [BoxGroup("Movement-Based Pulsation")]
    [Tooltip("Allow scaling to occur with only one hand's velocity.")]
    public bool singleHandScaling;

    [BoxGroup("Movement-Based Pulsation")]
    [Tooltip("The minimum size that the body can scale down to.")]
    public float minimumUnscaledSize;

    [BoxGroup("Movement-Based Pulsation")]
    [Range(0.0001f, 5f)]
    [Tooltip(
        "Used to mask false velocity readings due to position jitter from inaccurate sensor readings."
    )]
    public float minHandDisplacementPerFrame;

    [BoxGroup("Movement-Based Pulsation")]
    [Tooltip(
        "Dampen the ratio between body scale and hand distance based on hand distance relative to maxDistanceBetweenHands."
    )]
    public AnimationCurve distanceDamper;

    [BoxGroup("Movement-Based Pulsation")]
    [Tooltip("An overall damper for the movement-based pulsation scaling.")]
    public float pulseScaleDamper;

    [BoxGroup("Miscellaneous")]
    public float maxDistanceBetweenHands;

    [BoxGroup("Miscellaneous")]
    public float defaultUnscaledSize;

    [BoxGroup("Miscellaneous")]
    public float bodyScale;

    [BoxGroup("Miscellaneous")]
    public float maxDistanceFromCamera;

    [BoxGroup("Animation")]
    [Tooltip(
        "The amount of time it takes for the particle initialization animation to play once a new player is added to the scene."
    )]
    public float particleInitializationDelay;

    [BoxGroup("Debugging")]
    public bool showAttractionRadius;

    [BoxGroup("Debugging")]
    public bool showSphereMeshOnHandCollision;

    [BoxGroup("Debugging")]
    public bool dummyOnlyMode;
}
