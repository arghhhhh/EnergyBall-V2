using Assets.Scripts;
using UnityEngine;

public class PlayerScaler
{
    readonly SceneController controller = SceneController.Instance;

    public void ScaleSetup(PlayerConstructor player)
    {
        player.leftHandCollider.position = player.HandLeft.transform.position;
        player.rightHandCollider.position = player.HandRight.transform.position;

        Vector3 leftHandDisplacement =
            player.HandLeft.transform.position - player.leftHandPrevPosition;
        Vector3 rightHandDisplacement =
            player.HandRight.transform.position - player.rightHandPrevPosition;

        Vector3 leftHandVelocity = leftHandDisplacement / Time.fixedDeltaTime;
        Vector3 rightHandVelocity = rightHandDisplacement / Time.fixedDeltaTime;

        float distanceBtwnHands = (
            player.HandLeft.transform.position - player.HandRight.transform.position
        ).magnitude;
        float distanceBtwnLeftAndBody = (
            player.sphere.position - player.HandLeft.transform.position
        ).magnitude;
        float distanceBtwnRightAndBody = (
            player.sphere.position - player.HandRight.transform.position
        ).magnitude;

        player.sphere.gameObject.GetComponent<MeshRenderer>().enabled = false;

        // only scale sphere when it's between the hands
        if (
            distanceBtwnHands > distanceBtwnLeftAndBody
            && distanceBtwnHands > distanceBtwnRightAndBody
            && Time.frameCount > 1
        // don't do any scaling in the first frame of the game
        )
        {
            // we may want to scale player by each hand independently
            // for example, if left hand is stationary and right hand moves toward left
            // we want to negatively scale the sphere based on the movement of the right hand
            if (controller.so.singleHandScaling)
            {
                if (
                    leftHandVelocity.magnitude < 15f
                    && leftHandDisplacement.magnitude > controller.so.minHandDisplacementPerFrame
                )
                {
                    CreateRays(
                        player,
                        player.leftHandCollider,
                        player.rightHandCollider,
                        leftHandVelocity
                    );
                    player.leftHandCollider.rotation = Quaternion.LookRotation(leftHandVelocity);
                }
                if (
                    rightHandVelocity.magnitude < 15f
                    && rightHandDisplacement.magnitude > controller.so.minHandDisplacementPerFrame
                )
                {
                    CreateRays(
                        player,
                        player.rightHandCollider,
                        player.leftHandCollider,
                        rightHandVelocity
                    );
                    player.rightHandCollider.rotation = Quaternion.LookRotation(rightHandVelocity);
                }
            }
            else if (
                leftHandVelocity.magnitude < 15f
                && leftHandDisplacement.magnitude > controller.so.minHandDisplacementPerFrame
                && rightHandVelocity.magnitude < 15f
                && rightHandDisplacement.magnitude > controller.so.minHandDisplacementPerFrame
            )
            {
                CreateRays(
                    player,
                    player.leftHandCollider,
                    player.rightHandCollider,
                    leftHandVelocity
                );
                CreateRays(
                    player,
                    player.rightHandCollider,
                    player.leftHandCollider,
                    rightHandVelocity
                );
                player.leftHandCollider.rotation = Quaternion.LookRotation(leftHandVelocity);
                player.rightHandCollider.rotation = Quaternion.LookRotation(rightHandVelocity);
            }
        }

        player.leftHandPrevPosition = player.HandLeft.transform.position;
        player.rightHandPrevPosition = player.HandRight.transform.position;
    }

    void CreateRays(PlayerConstructor player, Transform hand, Transform oppoHand, Vector3 velocity)
    {
        Ray positiveRay = new(hand.position, velocity.normalized);
        Ray negativeRay = new(hand.position, (-velocity).normalized);

        Collider bodyCol = player.sphere.GetComponent<Collider>();
        Collider oppoHandCol = oppoHand.GetComponent<Collider>();

        if (
            CheckRaycast(
                positiveRay,
                bodyCol,
                oppoHandCol,
                out Vector3 bodyHP,
                out Vector3 oppoHandHP
            )
        )
        {
            // positive hit
            GenerateScaleVectorFromHand(player, hand, oppoHand, bodyHP, oppoHandHP, velocity, true);
        }
        else if (CheckRaycast(negativeRay, bodyCol, oppoHandCol, out bodyHP, out oppoHandHP))
        {
            // negative hit
            GenerateScaleVectorFromHand(
                player,
                hand,
                oppoHand,
                bodyHP,
                oppoHandHP,
                velocity,
                false
            );
        }
    }

    bool CheckRaycast(
        Ray ray,
        Collider sphere,
        Collider oppoHand,
        out Vector3 bodyHP,
        out Vector3 oppoHandHP
    )
    {
        bool bodyHit = sphere.Raycast(ray, out RaycastHit bodyRH, Mathf.Infinity);
        bool oppoHandHit = oppoHand.Raycast(ray, out RaycastHit handRH, Mathf.Infinity);

        if (controller.so.showSphereMeshOnHandCollision && bodyHit)
        {
            sphere.gameObject.GetComponent<MeshRenderer>().enabled = true;
        }

        bodyHP = bodyRH.point;
        oppoHandHP = handRH.point;

        // bodyHit is much less likely
        if (oppoHandHit)
        {
            return true;
        }
        return false;
    }

    void GenerateScaleVectorFromHand(
        PlayerConstructor player,
        Transform hand,
        Transform oppoHand,
        Vector3 bodyHP,
        Vector3 oppoHandHP,
        Vector3 handVelo,
        bool sign
    )
    {
        // project hand velocity vector onto distance between hands vector
        if (!sign)
        {
            handVelo = -handVelo;
        }

        Vector3 normal = oppoHand.position - hand.position; // distance between hands
        Vector3 projVelocity = Vector3.Project(handVelo, normal.normalized);

        // In total, the motion-based scale should be calculated by a combination of three factors:
        // 1. Velocity at which a hand moves relative to the sphere (displacement per frame)
        // 2. Ratio between hand distance and sphere diameter
        // 3. Alignment of the opposite hand with a hand’s velocity vector

        // 1.
        // When you multiply a velocity by Time.deltaTime, you get displacement since last frame
        float displacementScaler = projVelocity.magnitude * Time.deltaTime;

        // 2.
        // player scale : hand distance ratio
        float sizeDistanceRatio = Mathf.Clamp01(
            Utils.GetVector3Avg(player.unscaledSize) / normal.magnitude
        );
        // larger distance between hands means less scaling
        float relativeHandDistance = Mathf.InverseLerp(
            controller.so.maxDistanceBetweenHands,
            0f,
            normal.magnitude
        );
        float scaleCurveT = controller.so.distanceDamper.Evaluate(relativeHandDistance);
        float distanceDamper = sizeDistanceRatio * scaleCurveT;

        // 3.
        float bodyHitDistFromPerpPoint = GetDistanceFromPerpendicular(
            player.sphere.position,
            hand.position,
            bodyHP
        );
        float bodyAlignment = Mathf.InverseLerp(
            player.sphere.GetComponent<SphereCollider>().radius,
            0,
            bodyHitDistFromPerpPoint
        );
        float oppoHandAlignment = Mathf.InverseLerp(
            0,
            Vector3.Distance(oppoHandHP, oppoHand.position),
            oppoHand.GetComponent<BoxCollider>().size.y * 1.4142f // 1.41 = approximation of sqrt(2)
        );

        // controller.debugText.text =
        //     $"distanceDamper: {distanceDamper}\nhandDistance: {normal.magnitude}\nbodyAlignment: {bodyAlignment}\noppoHandAlignment: {oppoHandAlignment}";

        float hitAccuracyDamper =
            bodyAlignment < oppoHandAlignment && bodyAlignment > 0.1f
                ? bodyAlignment
                : oppoHandAlignment;

        // the total scale amount from the three factors
        float scaleAmt =
            displacementScaler
            * distanceDamper
            * hitAccuracyDamper
            * controller.so.pulseScaleDamper;
        if (sign)
        {
            scaleAmt = -scaleAmt;
        }
        Vector3 scaleAmtVector = Utils.FloatToVector3(scaleAmt);

        player.unscaledSize += scaleAmtVector;
        if (Utils.GetVector3Avg(player.unscaledSize) < controller.so.minimumUnscaledSize)
        {
            player.unscaledSize = Utils.FloatToVector3(controller.so.minimumUnscaledSize);
        }

        // Debug.Log(hand.gameObject.name + ", distance: " + normal.magnitude + ", projected velo:" + projVelocity.magnitude + ", disp: " + projVelocity.magnitude*Time.deltaTime);
    }

    float GetDistanceFromPerpendicular(Vector3 lineStart, Vector3 lineEnd, Vector3 hitpoint)
    {
        Vector3 lineDirection = (lineEnd - lineStart).normalized;

        // Project the vector from pointA to pointInSpace onto the line
        Vector3 projection = Vector3.Dot(hitpoint - lineStart, lineDirection) * lineDirection;

        // Calculate the point on the line closest to pointInSpace
        Vector3 perpendicularPoint = lineStart + projection;

        float distance = Vector3.Distance(hitpoint, perpendicularPoint);

        return distance;
    }
}
