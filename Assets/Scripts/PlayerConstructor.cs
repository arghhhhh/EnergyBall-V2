using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.VFX;
using Windows.Kinect;

public class PlayerConstructor : MonoBehaviour
{
    [HideInInspector]
    public ulong userId;

    private SceneController controller = null;
    public Rigidbody sphere;
    public SpriteRenderer radiusSprite;

    [HideInInspector]
    public VisualEffect vfxGraph;

    [HideInInspector]
    public Animator vfxAnimator;

    [HideInInspector]
    public bool pushParticles = false;

    [Foldout("Animations")]
    public AnimationClip emptyClip;

    [Foldout("Animations")]
    public AnimationClip initializeClip;

    [Foldout("Animations")]
    public AnimationClip openClip;

    [Foldout("Animations")]
    public AnimationClip closedClip;

    [HideInInspector]
    public bool turnOnParticles = false;

    [HideInInspector]
    public bool beginInitialization = false;

    [HideInInspector]
    public bool initialized = false;

    [HideInInspector]
    public float attractionRadius = 1f;

    [Range(0.001f, 1)]
    public float attractionRadiusScaler = 1f;

    [HideInInspector]
    public Dictionary<PlayerConstructor, float> orbitalBodies = new();

    [HideInInspector]
    public Vector3 unscaledSize = Vector3.one;

    [HideInInspector]
    public Vector3 intrinsicPulseSize = Vector3.zero;
    float pulseOffset;

    [HideInInspector]
    public Vector3 pulseSizeFromMotion = Vector3.zero;

    [HideInInspector]
    public Vector3 leftHandPrevPosition = Vector3.zero;

    [HideInInspector]
    public Vector3 rightHandPrevPosition = Vector3.zero;

    [HideInInspector]
    public Vector3 prevVelocity = Vector3.zero;

    [HideInInspector]
    public float prevAcceleration = 0f;
    public Transform leftHandCollider;
    public Transform rightHandCollider;

    #region joints
    [Foldout("Joints")]
    public GameObject SpineBase;

    [Foldout("Joints")]
    public GameObject SpineMid;

    [Foldout("Joints")]
    public GameObject Neck;

    [Foldout("Joints")]
    public GameObject Head;

    [Foldout("Joints")]
    public GameObject ShoulderLeft;

    [Foldout("Joints")]
    public GameObject ElbowLeft;

    [Foldout("Joints")]
    public GameObject WristLeft;

    [Foldout("Joints")]
    public GameObject HandLeft;

    [Foldout("Joints")]
    public GameObject ShoulderRight;

    [Foldout("Joints")]
    public GameObject ElbowRight;

    [Foldout("Joints")]
    public GameObject WristRight;

    [Foldout("Joints")]
    public GameObject HandRight;

    [Foldout("Joints")]
    public GameObject HipLeft;

    [Foldout("Joints")]
    public GameObject KneeLeft;

    [Foldout("Joints")]
    public GameObject AnkleLeft;

    [Foldout("Joints")]
    public GameObject FootLeft;

    [Foldout("Joints")]
    public GameObject HipRight;

    [Foldout("Joints")]
    public GameObject KneeRight;

    [Foldout("Joints")]
    public GameObject AnkleRight;

    [Foldout("Joints")]
    public GameObject FootRight;

    [Foldout("Joints")]
    public GameObject SpineShoulder;

    [Foldout("Joints")]
    public GameObject HandtipLeft;

    [Foldout("Joints")]
    public GameObject ThumbLeft;

    [Foldout("Joints")]
    public GameObject HandtipRight;

    [Foldout("Joints")]
    public GameObject ThumbRight;
    #endregion
    public Dictionary<JointType, GameObject> jointMap;

    [HideInInspector]
    public HandState leftHandState;

    [HideInInspector]
    public HandState rightHandState;

    [HideInInspector]
    public Vector3 midpoint;

    [HideInInspector]
    public Color skeletonColor;
    public bool isDummy = false;

    private void Awake()
    {
        jointMap = new Dictionary<JointType, GameObject>()
        {
            { JointType.SpineBase, SpineBase },
            { JointType.SpineMid, SpineMid },
            { JointType.Neck, Neck },
            { JointType.Head, Head },
            { JointType.ShoulderLeft, ShoulderLeft },
            { JointType.ElbowLeft, ElbowLeft },
            { JointType.WristLeft, WristLeft },
            { JointType.HandLeft, HandLeft },
            { JointType.ShoulderRight, ShoulderRight },
            { JointType.ElbowRight, ElbowRight },
            { JointType.WristRight, WristRight },
            { JointType.HandRight, HandRight },
            { JointType.HipLeft, HipLeft },
            { JointType.KneeLeft, KneeLeft },
            { JointType.AnkleLeft, AnkleLeft },
            { JointType.FootLeft, FootLeft },
            { JointType.HipRight, HipRight },
            { JointType.KneeRight, KneeRight },
            { JointType.AnkleRight, AnkleRight },
            { JointType.FootRight, FootRight },
            { JointType.SpineShoulder, SpineShoulder },
            { JointType.HandTipLeft, HandtipLeft },
            { JointType.ThumbLeft, ThumbLeft },
            { JointType.HandTipRight, HandtipRight },
            { JointType.ThumbRight, ThumbRight }
        };

        vfxGraph = GetComponentInChildren<VisualEffect>();
        vfxAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        controller = SceneController.Instance;
        unscaledSize = new Vector3(
            controller.so.defaultUnscaledSize,
            controller.so.defaultUnscaledSize,
            controller.so.defaultUnscaledSize
        );
        pulseOffset = Random.Range(0, 10) / 10f + Random.Range(0, 10);
        radiusSprite.enabled = false;

        if (isDummy)
        {
            Actions.OnDummyAdded?.Invoke(this);
        }
        // else {
        //     Actions.OnPlayerAdded?.Invoke(this);
        // }
    }

    public void InitializeParticles()
    {
        StartCoroutine(DelayInitialization(controller.so.particleInitializationDelay));
    }

    private IEnumerator DelayInitialization(float delay)
    {
        yield return new WaitForSeconds(delay);

        vfxAnimator.Play(initializeClip.name, -1, 0f);

        yield return new WaitForSeconds(initializeClip.length);

        initialized = true;
        turnOnParticles = true;
    }

    public void SetAttractionRadius(float radiusMultiplier)
    {
        attractionRadius =
            Utils.GetVector3Avg(sphere.transform.localScale)
            * radiusMultiplier
            * attractionRadiusScaler
            / 2f; // divide by two since it's the radius

        if (controller.so.showAttractionRadius)
        {
            radiusSprite.enabled = true;
            // set size of radius sprite
            radiusSprite.transform.localScale = new Vector3(
                radiusMultiplier * 0.4f * attractionRadiusScaler,
                radiusMultiplier * 0.4f * attractionRadiusScaler,
                radiusMultiplier * 0.4f * attractionRadiusScaler
            );
        }
    }

    public void SetMass()
    {
        sphere.mass = Utils.GetVector3Avg(sphere.transform.localScale);
    }

    public void SetPulseSize()
    {
        if (controller.so.pulseAmount == 0)
        {
            intrinsicPulseSize = Vector3.zero;
        }
        else
        {
            float playerPulseAmt =
                Utils.GetVector3Avg(unscaledSize) * controller.so.pulseAmount / 10f;
            // float playerPulseSpeed = acceleration * pulseSpeed / 100f;
            float pulseFunc = 0f;
            // use desmos graphing calculator to find graph bounds
            float playerLimit = controller.so.graphLimit * playerPulseAmt;
            for (int i = 0; i < controller.so.pulseFreqs.Length; i++)
            {
                // y=sin(2x)+sin(0.8x)+sin(0.3x)...
                pulseFunc += Mathf.Sin(
                    controller.so.pulseFreqs[i]
                        * controller.so.pulseSpeed
                        * (Time.fixedTime + pulseOffset)
                );
            }

            float simplePulseSize = playerPulseAmt * pulseFunc;
            float remappedSize = Utils.Remap(
                simplePulseSize,
                -playerLimit,
                playerLimit,
                0f,
                playerPulseAmt
            );

            intrinsicPulseSize = Utils.FloatToVector3(remappedSize);
        }
    }

    public void SetScale()
    {
        Vector3 sizeScaler = Vector3.zero;

        foreach (KeyValuePair<PlayerConstructor, float> orbitalBody in orbitalBodies)
        {
            // float startScaleDistance = Utils.GetVector3Avg(unscaledSize+orbitalBody.Key.unscaledSize) / 2f;
            float startScaleDistance =
                Utils.GetVector3Avg(
                    unscaledSize
                        + intrinsicPulseSize
                        + orbitalBody.Key.unscaledSize
                        + orbitalBody.Key.intrinsicPulseSize
                ) / 2f;
            float t = Mathf.InverseLerp(startScaleDistance, 0f, orbitalBody.Value);
            sizeScaler += (orbitalBody.Key.unscaledSize + orbitalBody.Key.intrinsicPulseSize) * t;
        }

        sphere.transform.localScale = unscaledSize + intrinsicPulseSize + sizeScaler;

        // we only get the unscaled size when there are no orbiting bodies
        if (orbitalBodies.Count == 0)
        {
            // remove pulseSize when calculating true unscaledSize
            unscaledSize = sphere.transform.localScale - intrinsicPulseSize;
        }
    }

    private void OnDisable()
    {
        if (isDummy)
        {
            Actions.OnDummyRemoved?.Invoke(userId);
            return;
        }
        // Actions.OnPlayerRemoved?.Invoke(userId);
    }
}
