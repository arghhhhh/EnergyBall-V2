using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Windows.Kinect;
using Joint = Windows.Kinect.Joint;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; } // singleton pattern

    [Label("Settings Config")]
    public SceneSettingsSO so;
    GravityForce gravityForceController;
    HandForce handForceController;
    PlayerScaler playerScaleController;
    MeshesToSDF meshesToSDF = null;
    BodySourceManager bodySourceManager;
    Body[] bodyData;
    List<ulong> trackedIds = new();
    List<ulong> knownIds = new();
    public GameObject playerPrefab;
    public TextMeshProUGUI debugText;
    private readonly Dictionary<ulong, GameObject> players = new();
    public Dictionary<ulong, GameObject> Players
    {
        get { return players; }
    }

    private readonly Dictionary<ulong, GameObject> dummies = new();

    private readonly Dictionary<JointType, JointType> boneMap =
        new()
        {
            // { JointType.FootLeft, JointType.AnkleLeft },
            // { JointType.AnkleLeft, JointType.KneeLeft },
            // { JointType.KneeLeft, JointType.HipLeft },
            // { JointType.HipLeft, JointType.SpineBase },

            // { JointType.FootRight, JointType.AnkleRight },
            // { JointType.AnkleRight, JointType.KneeRight },
            // { JointType.KneeRight, JointType.HipRight },
            // { JointType.HipRight, JointType.SpineBase },

            // { JointType.HandTipLeft, JointType.HandLeft },
            // { JointType.ThumbLeft, JointType.HandLeft },
            { JointType.HandLeft, JointType.WristLeft },
            { JointType.WristLeft, JointType.ElbowLeft },
            { JointType.ElbowLeft, JointType.ShoulderLeft },
            { JointType.ShoulderLeft, JointType.SpineShoulder },
            // { JointType.HandTipRight, JointType.HandRight },
            // { JointType.ThumbRight, JointType.HandRight },
            { JointType.HandRight, JointType.WristRight },
            { JointType.WristRight, JointType.ElbowRight },
            { JointType.ElbowRight, JointType.ShoulderRight },
            { JointType.ShoulderRight, JointType.SpineShoulder },

            // { JointType.SpineBase, JointType.SpineMid },
            // { JointType.SpineMid, JointType.SpineShoulder },
            // { JointType.SpineShoulder, JointType.Neck },
            // { JointType.Neck, JointType.Head },
        };

    public Material SkeletonMaterial;
    public bool drawSkeleton;
    public bool customColors;
    private int lastColorIndex;
    public Color[] colors = new Color[]
    {
        Color.blue,
        Color.cyan,
        Color.green,
        Color.magenta,
        Color.red,
        new Color(1, 0.5f, 0),
        Color.yellow
    };

    [GradientUsage(true)]
    public Gradient[] gradients = new Gradient[]
    {
        new Gradient(),
        new Gradient(),
        new Gradient(),
        new Gradient(),
        new Gradient(),
        new Gradient(),
        new Gradient()
    };

    private void OnEnable()
    {
        // Actions.OnPlayerAdded += AddPlayer;
        // Actions.OnPlayerRemoved += RemovePlayer;
        Actions.OnDummyAdded += InitializeNewDummy;
        Actions.OnDummyRemoved += RemovePlayer;
    }

    private void OnDisable()
    {
        // Actions.OnPlayerAdded -= AddPlayer;
        // Actions.OnPlayerRemoved -= RemovePlayer;
        Actions.OnDummyAdded -= InitializeNewDummy;
        Actions.OnDummyRemoved -= RemovePlayer;
    }

    private void Awake()
    {
        Cursor.visible = false;

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        gravityForceController = new();
        handForceController = new();
        playerScaleController = new();
        bodySourceManager = GetComponent<BodySourceManager>();
        meshesToSDF = GetComponent<MeshesToSDF>();

        // set main camera far clipping plane to so.maxDistanceFromCamera
        // Camera.main.farClipPlane = so.maxDistanceFromCamera + transform.position.z;
    }

    void InitializeNewDummy(PlayerConstructor dummy)
    {
        if (dummy)
        {
            ulong userId = (ulong)Random.Range(90, 100); // generate random userId between 90 and 99
            dummy.name = $"Player {userId}";
            dummy.userId = userId;
            dummy.unscaledSize = new Vector3(
                so.defaultUnscaledSize,
                so.defaultUnscaledSize,
                so.defaultUnscaledSize
            );
            if (customColors)
            {
                ChoosePlayerColor(dummy);
            }

            meshesToSDF.AddToMeshes(dummy);

            dummies[dummy.userId] = dummy.gameObject;
            players[dummy.userId] = dummy.gameObject;

            // debugText.text = $"userId: {userId}\nunscaledSize: {dummy.unscaledSize.x}\nplayer: {players[userId].name}";
        }
    }

    GameObject InitializeNewPlayer(ulong userId)
    {
        GameObject newPlayer = Instantiate(playerPrefab);

        if (newPlayer.TryGetComponent<PlayerConstructor>(out var playerConstructor))
        {
            newPlayer.name = $"Player {userId}";
            playerConstructor.userId = userId;
            if (customColors)
            {
                ChoosePlayerColor(playerConstructor);
            }

            meshesToSDF.AddToMeshes(playerConstructor);
        }

        return newPlayer;
    }

    void RemovePlayer(ulong userId)
    {
        if (players[userId])
        {
            PlayerConstructor playerConstructor = players[userId].GetComponent<PlayerConstructor>();
            if (playerConstructor.isDummy)
            {
                dummies.Remove(userId);
            }
            meshesToSDF.RemoveMeshFromMeshes(playerConstructor);

            Destroy(players[userId]);
            players.Remove(userId);
        }
    }

    void ChoosePlayerColor(PlayerConstructor player)
    {
        int colorIndex = Random.Range(0, colors.Length);
        while (colorIndex == lastColorIndex)
        {
            colorIndex = Random.Range(0, colors.Length);
        }
        lastColorIndex = colorIndex;
        player.skeletonColor = colors[colorIndex];
        player.vfxGraph.SetGradient("playerAuraColor", gradients[colorIndex]);
    }

    void SetPlayerHandStates(Body body, PlayerConstructor player)
    {
        player.leftHandState = body.HandLeftState;
        player.rightHandState = body.HandRightState;
    }

    private Vector3 GetVector3FromJoint(Joint joint)
    {
        return new Vector3(
            joint.Position.X * so.bodyScale,
            joint.Position.Y * so.bodyScale,
            joint.Position.Z * so.bodyScale
        );
    }

    private static Color ColorSkeleton(TrackingState state)
    {
        switch (state)
        {
            case TrackingState.Tracked:
                return Color.white;

            case TrackingState.Inferred:
                return Color.grey;

            default:
                return Color.black;
        }
    }

    void UpdateOtherPlayerData(GameObject player)
    {
        PlayerConstructor playerConstructor = player.GetComponent<PlayerConstructor>();

        if (playerConstructor.beginInitialization)
        {
            playerConstructor.SetAttractionRadius(so.attractionRadiusMultiplier);
            playerConstructor.SetMass();
            playerConstructor.SetPulseSize();
            playerConstructor.SetScale();
            handForceController.ManageHandForce(playerConstructor);
            playerScaleController.ScaleSetup(playerConstructor);
            meshesToSDF.UpdateMeshTransform(playerConstructor);
        }

        // wait until player's hands first open to initialize particles
        if (
            !playerConstructor.beginInitialization
            && playerConstructor.leftHandState == HandState.Open
            && playerConstructor.rightHandState == HandState.Open
        )
        {
            playerConstructor.beginInitialization = true;
            playerConstructor.InitializeParticles();
        }
    }

    void UpdateKinectPlayerData(Body body, GameObject player)
    {
        if (!body.IsRestricted)
        {
            PlayerConstructor playerConstructor = player.GetComponent<PlayerConstructor>();

            foreach (JointType joint in boneMap.Keys)
            {
                Joint sourceJoint = body.Joints[joint];
                Vector3 targetPosition = GetVector3FromJoint(sourceJoint);

                // debugText.text = $"userId: {playerConstructor.userId}\n" +
                //     $"joint: {joint}\n" +
                //     $"position: {targetPosition}\n" +
                //     $"maxDistanceFromCamera: {so.maxDistanceFromCamera}";

                Transform jointObject = playerConstructor.jointMap[joint].transform;
                jointObject.position = targetPosition;

                if (
                    (joint == JointType.ShoulderLeft || joint == JointType.ShoulderRight)
                    && targetPosition.z > so.maxDistanceFromCamera
                )
                {
                    playerConstructor.leftHandState = HandState.Closed;
                    playerConstructor.rightHandState = HandState.Closed;
                    foreach (LineRenderer lr in player.GetComponentsInChildren<LineRenderer>())
                    {
                        lr.enabled = false;
                    }
                    playerConstructor.leftHandCollider.gameObject.SetActive(false);
                    playerConstructor.rightHandCollider.gameObject.SetActive(false);

                    return;
                }

                if (drawSkeleton)
                {
                    playerConstructor.leftHandCollider.gameObject.SetActive(true);
                    playerConstructor.rightHandCollider.gameObject.SetActive(true);

                    if (boneMap.ContainsKey(joint))
                    {
                        Joint? targetJoint = body.Joints[boneMap[joint]];

                        if (targetJoint.HasValue)
                        {
                            LineRenderer lr = jointObject.GetComponent<LineRenderer>();
                            lr.enabled = true;
                            lr.SetPosition(0, jointObject.localPosition);
                            lr.SetPosition(1, GetVector3FromJoint(targetJoint.Value));
                            if (customColors)
                            {
                                lr.startColor = playerConstructor.skeletonColor;
                                lr.endColor = playerConstructor.skeletonColor;
                            }
                            else
                            {
                                lr.startColor = ColorSkeleton(sourceJoint.TrackingState);
                                lr.endColor = ColorSkeleton(targetJoint.Value.TrackingState);
                            }
                        }
                    }
                }
                else
                {
                    LineRenderer lr = jointObject.GetComponent<LineRenderer>();
                    lr.enabled = false;
                }
            }

            SetPlayerHandStates(body, playerConstructor);
        }
    }

    private void DeleteAllBodies(List<ulong> ids)
    {
        foreach (ulong trackingId in ids)
        {
            if (!dummies.ContainsKey(trackingId))
            {
                Destroy(players[trackingId]);
                players.Remove(trackingId);
            }
        }
    }

    void FixedUpdate()
    {
        if (!so.dummyOnlyMode)
        {
            bodyData = bodySourceManager.GetData();
            if (bodyData == null)
            {
                return;
            }

            trackedIds.Clear();
            foreach (var body in bodyData)
            {
                if (body == null)
                    continue;

                if (body.IsTracked)
                    trackedIds.Add(body.TrackingId);
            }

            knownIds = new List<ulong>(players.Keys);
            foreach (ulong trackingId in knownIds)
            {
                if (!trackedIds.Contains(trackingId) && !dummies.ContainsKey(trackingId))
                {
                    RemovePlayer(trackingId);
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                DeleteAllBodies(knownIds);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            foreach (var body in bodyData)
            {
                if (body == null)
                    continue;

                if (body.IsTracked)
                {
                    if (!players.ContainsKey(body.TrackingId))
                    {
                        players[body.TrackingId] = InitializeNewPlayer(body.TrackingId);
                    }

                    UpdateKinectPlayerData(body, players[body.TrackingId]);
                    UpdateOtherPlayerData(players[body.TrackingId]);
                }
            }
        }

        if (dummies.Count > 0)
        {
            foreach (var dummy in dummies)
            {
                UpdateOtherPlayerData(dummy.Value);
            }
        }

        gravityForceController.ManageGravity();
    }
}
