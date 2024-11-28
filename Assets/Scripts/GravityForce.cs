using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using UnityEngine;

public class GravityForce
{
    readonly SceneController controller = SceneController.Instance;
    bool isMovingTowards = true;
    List<GameObject> players = new();
    PlayerConstructor playerAConstructor;
    PlayerConstructor playerBConstructor;

    public void ManageGravity()
    {
        players.Clear();
        players = controller.Players.Values.ToList();
        foreach (GameObject PlayerA in players)
        {
            foreach (GameObject PlayerB in players)
            {
                if (PlayerA != PlayerB)
                {
                    playerAConstructor = PlayerA.GetComponent<PlayerConstructor>();
                    playerBConstructor = PlayerB.GetComponent<PlayerConstructor>();
                    float distance = Vector3.Distance(
                        playerAConstructor.sphere.transform.position,
                        playerBConstructor.sphere.transform.position
                    );
                    Vector3 direction =
                        playerBConstructor.sphere.position - playerAConstructor.sphere.position;
                    // we want to start attraction once the attractee itself, not its attraction radius, enters the attractor's attraction radius
                    // radius of attractee is attractee.transform.localScale.x / 2f
                    if (
                        distance
                            - (
                                Utils.GetVector3Avg(playerBConstructor.sphere.transform.localScale)
                                / 2f
                            )
                        < playerAConstructor.attractionRadius
                    )
                    {
                        AddGravityForce(
                            playerAConstructor,
                            playerBConstructor,
                            direction,
                            distance
                        );
                    }

                    ManageOrbitalBodies(playerAConstructor, playerBConstructor, distance);
                }
            }
        }
    }

    void AddGravityForce(
        PlayerConstructor player,
        PlayerConstructor attractor,
        Vector3 direction,
        float distance
    )
    {
        // F = G * ((m1*m2)/r^2)
        float massProduct = attractor.sphere.mass * player.sphere.mass;
        float unscaledForceMagnitude = massProduct / (distance * distance);
        float forceMagnitude = controller.so.g * unscaledForceMagnitude;

        // Check whether objects are moving towards each other
        Vector3 relativeVelocity = player.sphere.velocity - attractor.sphere.velocity;
        float relativeDot = Vector3.Dot(direction, relativeVelocity);

        isMovingTowards = relativeDot > 0;

        if (isMovingTowards && forceMagnitude >= controller.so.maxTowardsForce)
        {
            forceMagnitude = controller.so.maxTowardsForce * controller.so.gravityForceDamper;
        }
        else if (!isMovingTowards && forceMagnitude >= controller.so.maxAwayFromForce)
        {
            forceMagnitude = controller.so.maxAwayFromForce;
        }

        // Only add gravity until a certain distance
        if (distance > controller.so.stopGravityDistance)
        {
            Vector3 forceDirection = direction.normalized;
            Vector3 forceVector = forceDirection * forceMagnitude;
            player.sphere.AddForce(forceVector);
        }
        // Stop object at a certain distance and speed
        else if (
            distance < controller.so.stopMovingDistance
            && relativeVelocity.magnitude < controller.so.stopVelocity
        )
        {
            Vector3 stopForce =
                -1f
                * player.sphere.mass
                * ((player.sphere.velocity - player.prevVelocity) / Time.fixedDeltaTime);
            player.sphere.AddForce(stopForce);
        }
    }

    void ManageOrbitalBodies(PlayerConstructor player, PlayerConstructor attractor, float distance)
    {
        float startScaleDistance =
            Utils.GetVector3Avg(player.unscaledSize + attractor.unscaledSize) / 2f;
        if (distance < startScaleDistance)
        {
            // this means we just entered radius. trigger event that adds sphere
            if (!player.orbitalBodies.ContainsKey(attractor))
            {
                player.orbitalBodies.Add(attractor, distance);
            }
            else
            {
                // set distance for targetScaler
                player.orbitalBodies[attractor] = distance;
            }
        }
        else
        {
            // this means we just left radius. trigger event that removes sphere
            if (player.orbitalBodies.ContainsKey(attractor))
            {
                player.orbitalBodies.Remove(attractor);
            }
        }
    }
}
