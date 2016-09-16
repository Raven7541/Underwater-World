using UnityEngine;
using System.Collections;

//use the Generic system here to make use of a Flocker list later on
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]

abstract public class Vehicle : MonoBehaviour 
{
	// attributes
	protected GameManager gm;

	protected Vector3 velocity;
	protected Vector3 acceleration;
	protected Vector3 desiredVelocity;

	public float maxSpeed = 12.0f;
	public float maxForce = 15.0f;
	public float mass = 1.0f;
    public float radius;
    public float safeDistance = 2.5f;  // Safe distance!
    protected float stageWeight = 800.0f;  // Stay in bounds, you idiot!

    private float[] neighborDistance;

	CharacterController charControl;

	// Accessor(s)
	Vector3 Velocity { get { return velocity; } }

	// Abstract method
	abstract protected void CalcSteeringForces();


	// Start!
	virtual public void Start()
	{
		// Set up default values
		acceleration = Vector3.zero;
		velocity = transform.forward;
		desiredVelocity = Vector3.zero;

		// store access to character controller component
		charControl = GetComponent<CharacterController> ();

		// Set up the game manager
        gm = GameObject.Find("GameManagerGO").GetComponent<GameManager>();
	}
	
	// Update is called once per frame
	protected void Update () 
	{
		// Calculate steering forces
		CalcSteeringForces();

		// Add acceleration to velocity, limit the velocity, and add velocity to position
		velocity += acceleration * Time.deltaTime;

		// Limit velocity to max speed
		// and normalize
		velocity = Vector3.ClampMagnitude (velocity, maxSpeed);
		transform.forward = velocity.normalized;

		// Move the character based on velocity
		charControl.Move (velocity * Time.deltaTime);

		// Reset acceleration
		acceleration = Vector3.zero;
	}

	// Apply force
	protected void ApplyForce(Vector3 steeringForce)
	{
		acceleration += (steeringForce / mass);
	}

	// Seek!
	protected Vector3 Seek(Vector3 targetPosition)
	{
        Debug.DrawLine(transform.position, targetPosition, Color.red);
        
        // Calculate desired velocity
		desiredVelocity = targetPosition - transform.position;

		// Normalize and multiply
		desiredVelocity = desiredVelocity.normalized * maxSpeed;

		// Calculate steering force
		desiredVelocity -= velocity;
		desiredVelocity = Vector3.ClampMagnitude (desiredVelocity, maxForce);  // limit

		return desiredVelocity;
	}

    // Flee!
    protected Vector3 Flee(Vector3 targetPosition)
    {
        Debug.DrawLine(transform.position, targetPosition, Color.yellow);
        
        // Calculate desired velocity
        desiredVelocity = transform.position - targetPosition;

        // Normalize and multiply
        desiredVelocity = desiredVelocity.normalized * maxSpeed;

        // Calculate steering force
        desiredVelocity -= velocity;
        desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, maxForce);  // limit

        return desiredVelocity;
    }

    // Pursue!
    protected Vector3 Pursue(Vehicle target)
    {
        // Calculate desired velocity
        Vector3 distanceVector = target.transform.position - transform.position;

        // Calculate time to reach target
        float time = distanceVector.magnitude / target.maxSpeed;

        // Predict future position of target
        Vector3 futurePos = target.transform.position + (target.velocity * (Time.deltaTime * time));

        return Seek(futurePos);
    }

    // Evade!
    protected Vector3 Evade(Vehicle target)
    {
        // Calculate desired velocity
        Vector3 distanceVector = target.transform.position - transform.position;

        // Calculate time to reach target
        float time = distanceVector.magnitude / target.maxSpeed;

        // Predict future position of target
        Vector3 futurePos = target.transform.position + (target.velocity * (Time.deltaTime * time));

        return Flee(futurePos);
    }

    // Arrive!
    protected Vector3 Arrive(Vector3 targetPosition)
    {
        // Calculate desired velocity
        Vector3 distance = targetPosition - transform.position;

        // Check if the pursuer has reached within the target's safe zone
        if (distance.magnitude < safeDistance*2)
        {
			// Set the magnitude according to how close it is   
			Debug.DrawLine(transform.position, targetPosition, Color.magenta);  // debug
            desiredVelocity = Vector3.ClampMagnitude(Vector3.Scale(desiredVelocity, distance), maxSpeed);
        }
        else
        {
			// Otherwise, proceed at max speed
            desiredVelocity *= maxSpeed * Time.deltaTime;
        }

        // Calculate steering force
        desiredVelocity -= velocity;
        desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, maxForce);

        return desiredVelocity;
    }

	// Arrive (at the waypoint)!
    //protected Vector3 ArriveWaypoint(GameObject target)
    //{
    //    // Calculate desired velocity
    //    Vector3 distance = target.transform.position - transform.position;

    //    // Check if the pursuer has reached within the range of waypoint
    //    if (distance.magnitude < safeDistance*4)
    //    {
    //        // Debug
    //        Debug.DrawLine(transform.position, target.transform.position, Color.green);

    //        // Set the magnitude according to how close it is
    //        desiredVelocity = Vector3.ClampMagnitude(Vector3.Scale(desiredVelocity, distance.normalized), maxSpeed/2);
    //    }
    //    else
    //    {
    //        // Otherwise, proceed at max speed
    //        desiredVelocity *= maxSpeed * Time.deltaTime;
    //    }
		
    //    // Calculate steering force
    //    desiredVelocity -= velocity;
    //    desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, maxForce);

    //    return desiredVelocity;
    //}


	// Avoid obstacles!
	protected Vector3 AvoidObstacle(GameObject obst, float safeDistance)
	{
		// Reset
		desiredVelocity = Vector3.zero;

		// Get the obstacle's radius
		float obRadius = obst.GetComponent<ObstacleScript>().Radius;

		// Calculate distance from obstacle
		Vector3 vectToCenter = obst.transform.position - transform.position;

		if (safeDistance < vectToCenter.magnitude) 
		{
			return Vector3.zero;
		}
		if (Vector3.Dot (vectToCenter, transform.forward) < 0) 
		{
			return Vector3.zero;
		}

		// Find the distance between the centers of vehicle and obstacle
		float distance = Vector3.Dot (vectToCenter, transform.right);

		if (Mathf.Abs(distance) > (radius + obRadius)) 
		{
			return Vector3.zero;
		} 
		else if (distance > 0) 
		{
			// Turn left!
			desiredVelocity = transform.right * -maxSpeed;

			// Debug!
			Debug.DrawLine(transform.position, obst.transform.position, Color.green);
		} 
		else 
		{
			// Turn right!
			desiredVelocity = transform.right * maxSpeed;

			// Debug!
			Debug.DrawLine(transform.position, obst.transform.position, Color.red);
		}

		return desiredVelocity;
	}

	// Separate!
	public Vector3 Separation(float separationDistance)
	{
        // Set up steering force for separation
        Vector3 separateForce = new Vector3(0, 0, 0);

        // Set up array for separation
        neighborDistance = new float[gm.GetComponent<GameManager>().numFlockers];

        // Too close!
		if (separationDistance <= 5) 
		{
            // Save the distance
            for (int i = 0; i < neighborDistance.Length; i++)
            {
                if (neighborDistance[i] == 0)
                {
                    neighborDistance[i] = separationDistance;
                }
            }
		}

		// Flee (kinda)!
        for (int i = 0; i < neighborDistance.Length; i++)
        {
            // Find the distance
            Vector3 avoidNeighbor = transform.position - new Vector3(transform.position.x + neighborDistance[i], 
                                transform.position.y + neighborDistance[i], transform.position.z + neighborDistance[i]);

            // Normalize
            avoidNeighbor = avoidNeighbor.normalized;

            // Add to sum
            // with weights inversely proportional to distance
            separateForce += avoidNeighbor * (1/neighborDistance[i]);
        }

        // Normalize the sum and multiply
        desiredVelocity = separateForce.normalized * maxSpeed;

        // Calculate steering force
        desiredVelocity -= velocity;
        desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, maxForce);  // limit

        return desiredVelocity;
	}

	// Align with neighbors!
	public Vector3 Alignment(Vector3 alignVector)
	{
        // Compute the desired velocity
        desiredVelocity = alignVector.normalized * maxSpeed;

        // Compute the steering force
        desiredVelocity -= velocity;
        desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, maxForce);  // limit

		return desiredVelocity;
	}

	// Seek the center of the flock!
	public Vector3 Cohesion(Vector3 cohesionVector)
	{
		return Seek(cohesionVector);
	}

	// Stay within the boundary of the scene!
	public Vector3 StayInBounds(float radius, Vector3 center)
	{
		// Reset
		desiredVelocity = Vector3.zero;

		// Get current position
		Vector3 currentPos = new Vector3(center.x + radius, center.y + radius, center.z + radius);

		// Check if it's out of bounds
        if (currentPos.x < 25 || currentPos.z < 25
            || currentPos.x >= Terrain.activeTerrain.terrainData.size.x-50 || currentPos.z >= Terrain.activeTerrain.terrainData.size.z-50
            || currentPos.y < 20 || currentPos.y >= 75)
		{
            // Seek the center of the terrain
            desiredVelocity = Terrain.activeTerrain.terrainData.size;
            desiredVelocity.y = 45;
            desiredVelocity.x = desiredVelocity.x/2;
            desiredVelocity.z = desiredVelocity.x;

			return Seek (desiredVelocity);
		}
		
		// It's safe, move on
		return Vector3.zero;
	}
}
