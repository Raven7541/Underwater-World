using UnityEngine;
using System.Collections;

//add using System.Collections.Generic; to use the generic list format
using System.Collections.Generic;

public class Seeker : Vehicle
{
    // attributes
    public GameObject seekerTarget;

    private Vector3 steeringForce;
    public float seekWeight = 25.0f;  // Weight (to seek!)
	private float arriveWeight = 25.0f;  // Weight (to arrive!)
    public float avoidWeight = 25.0f;  // Weight (to avoid!)


    // Call Inherited Start and then do our own
    override public void Start()
    {
        base.Start();

        // Set up default value
        steeringForce = Vector3.zero;
    }

    // Calculate steering forces
    protected override void CalcSteeringForces()
    {
        // Get current status
        bool checkForTarget = gm.GetComponent<GameManager>().CheckForTarget;
        Debug.Log("Target: " + checkForTarget);  // debug

        if (!checkForTarget)
        {
            
            // Tra la la, there's nothing to hunt for now...
            maxSpeed = 20.0f;
            maxForce = 25.0f;

            // Get the waypoint
            seekerTarget = gm.GetComponent<GameManager>().SeekerWaypoint;

            // get the seeking and arrival force (based on character movement)
            // and add in weight
            arriveWeight = 25.0f;
            steeringForce = Seek(seekerTarget.transform.position) * seekWeight;

            // get the avoiding force
            GameObject[] blocks = gm.GetComponent<GameManager>().Obstacles;
            for (int j = 0; j < blocks.Length; j++)
            {
                steeringForce += AvoidObstacle(blocks[j], safeDistance) * avoidWeight;
            }

            // stay in bounds!
            steeringForce += StayInBounds(radius, transform.position) * stageWeight;

            // limit the ultimate steering force
            steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);

            // apply it to acceleration
            ApplyForce(steeringForce);

            // reset
            steeringForce = Vector3.zero;

            // reset the waypoint if reached
            if (Vector3.Distance(transform.position, seekerTarget.transform.position) < safeDistance*3)
            {
                gm.GetComponent<GameManager>().WaypointReached = true;
            }
        }
        else
        {
            // get the seeking and arrival force (based on character movement)
            // and add in weight
            maxSpeed = 50.0f;
            maxForce = 100.0f;

            steeringForce = Pursue(seekerTarget.GetComponent<Vehicle>()) * seekWeight;
            steeringForce += Arrive(seekerTarget.GetComponent<Vehicle>().transform.position) * arriveWeight;

            // get the avoiding force
            GameObject[] blocks = gm.GetComponent<GameManager>().Obstacles;
            for (int i = 0; i < blocks.Length; i++)
            {
                steeringForce += AvoidObstacle(blocks[i], safeDistance) * avoidWeight;
            }

            // stay in bounds!
            steeringForce += StayInBounds(radius, transform.position) * stageWeight;

            // limit the ultimate steering force
            steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);

            // apply it to acceleration
            ApplyForce(steeringForce);

            // reset
            steeringForce = Vector3.zero;
        }
    }
}
