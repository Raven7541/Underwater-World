using UnityEngine;
using System.Collections;

//add using System.Collections.Generic; to use the generic list format
using System.Collections.Generic;

public class Flock : Vehicle
{
    // attributes
    public GameObject hunter;
    public bool leader;

    public Waypoint waypointTarget;
    private int currentNode;

    private Vector3 steeringForce;
    public float seekWeight = 25.0f;  // Weight (to seek!)
    private float arriveWeight = 25.0f;  // Weight (to arrive!)
    public float fleeWeight = 50.0f;  // Weight (to flee!)
    public float avoidWeight = 25.0f;  // Weight (to avoid!)


    // Start!
    override public void Start()
    {
        base.Start();

        // Set up default value
        steeringForce = Vector3.zero;
		waypointTarget = new Waypoint(Vector3.zero, Vector3.zero);

        currentNode = 0;
    }

    // Calculate steering forces
    protected override void CalcSteeringForces()
    {
        // Get current status
        bool checkForHunter = gm.GetComponent<GameManager>().CheckForHunter;
        Debug.Log("Hunter: " + checkForHunter);  // debug

        if (!checkForHunter)
        {
            if (leader)
            {
				maxSpeed = 6.5f;
                maxForce = 15.0f;

				// Path following
                steeringForce += FollowPath() * seekWeight;
            }
            else
            {
				maxSpeed = 15.0f;
                maxForce = 20.0f;

				// Leader following
                steeringForce += FollowLeader(gm.GetComponent<GameManager>().FlockLeader) * seekWeight;

                // get the separation force
                List<GameObject> group = gm.GetComponent<GameManager>().Flock;
                for (int i = 0; i < group.Count; i++)
                {
                    steeringForce += Separation(Vector3.Distance(transform.position, group[i].transform.position)) * avoidWeight;
                }

				// get the centroid
				steeringForce += Cohesion(gm.GetComponent<GameManager>().Centroid);
				
				// get the flocking direction
				steeringForce += Alignment(gm.GetComponent<GameManager>().FlockDirection);
            }

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
        else
        {
			maxSpeed = 12.0f;
            maxForce = 25.0f;
			// get the fleeing force (based on character movement)
            // and add in weight
            steeringForce = Evade(hunter.GetComponent<Vehicle>()) * fleeWeight;

            // get the separation force
            List<GameObject> group = gm.GetComponent<GameManager>().Flock;
            for (int i = 0; i < group.Count; i++)
            {
                steeringForce += Separation(Vector3.Distance(transform.position, group[i].transform.position));
            }

            // get the centroid
            steeringForce += Cohesion(gm.GetComponent<GameManager>().Centroid);

            // get the flocking direction
            steeringForce += Alignment(gm.GetComponent<GameManager>().FlockDirection);

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

    // Leader following
    private Vector3 FollowLeader(GameObject leader)
    {
        Vector3 leaderPos = leader.GetComponent<Flock>().transform.position;
        Vector3 leaderVelocity = leader.GetComponent<Flock>().velocity;
        Vector3 followForce = new Vector3(0, 0, 0);
        float flockNum = gm.GetComponent<GameManager>().numFlockers;

        // Calculate the ahead point
        leaderVelocity = leaderVelocity.normalized * 3.0f;
        Vector3 aheadPoint = leaderPos + leaderVelocity;

        // Calculate the behind point
        leaderVelocity *= -1;
        Vector3 behindPoint = leaderPos + leaderVelocity;

        // Check if the flocker is in the leader's sight
        if (IsOnLeaderSight(leaderPos, aheadPoint))
        {
            // Evading
            followForce += Evade(leader.GetComponent<Vehicle>()) * fleeWeight * (flockNum * 5.0f);
        }

        // Create a force to arrive at the behind point
		followForce += Seek (behindPoint) * seekWeight * (flockNum*5.0f);
        followForce += Arrive(behindPoint) * arriveWeight * (flockNum*5.0f);

        // get the separation force
        List<GameObject> group = gm.GetComponent<GameManager>().Flock;
        for (int i = 0; i < group.Count-1; i++)
        {
            followForce += Separation(Vector3.Distance(transform.position, group[i].transform.position)) * (avoidWeight*(flockNum/5.0f));
        }

        return followForce;
    }

    // Path following (simple)
    private Vector3 FollowPath()
    {
        // Get the path
        Waypoint[] path = gm.GetComponent<GameManager>().Path;

        // Get the current waypoint
        waypointTarget = path[currentNode];

        if (Vector3.Distance(transform.position, waypointTarget.GetStart) <= 3.0f)
        {
            // Keep going
            currentNode++;

            if (currentNode >= path.Length)
            {
                // Reset
                currentNode = 0;
            }
        }

        return Seek(waypointTarget.GetStart);
    }

    // Check if the flocker is in the leader's sight
    private bool IsOnLeaderSight(Vector3 leaderPos, Vector3 ahead)
    {
        return Vector3.Distance(ahead, transform.position) <= safeDistance*(GetComponent<Collider>().bounds.size.magnitude/4) ||
            Vector3.Distance(leaderPos, transform.position) <= safeDistance*(GetComponent<Collider>().bounds.size.magnitude/4);
    }
}
