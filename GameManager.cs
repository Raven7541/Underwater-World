
using UnityEngine;
using System.Collections;

//add using System.Collections.Generic; to use the generic list format
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // attributes
    private bool HUNT_MODE;  // debug purposes

    public GameObject seeker;
    //public GameObject target;
    public GameObject flocker;
    public GameObject seekerWaypoint;

    public GameObject seekerPrefab;
    public GameObject flockerPrefab;
    //public GameObject obstaclePrefab;
    public GameObject waypointPrefab;
    public GameObject bloodPrefab;  // ewwwwww...cool!

    private GameObject[] obstacles;  // array of obstacles
    private Waypoint[] path;
    private List<GameObject> flock;  // list of flockers

    public Vector3 centroid;  // center of flock
    public Vector3 flockDirection;  // average flock direction
    public int numFlockers;  // number of flockers

    private bool checkForTarget = false;  // is the prey nearby?
    private bool checkForHunter = false;  // is the hunter nearby?
    private bool waypointReached = false;  // did the hunter reached the pit stop?

    public Camera[] cameras;  // in-game cameras for the player to switch between
    private int currentCameraIndex;


    // Accessors
    public bool HuntMode
    {
        get { return HUNT_MODE; }
    }

    public GameObject[] Obstacles
    {
        get { return obstacles; }
    }

    public List<GameObject> Flock
    {
        get { return flock; }
    }

    public GameObject FlockLeader
    {
        get { return flock[0]; }
    }

    public Vector3 Centroid
    {
        get { return centroid; }
    }

    public Vector3 FlockDirection
    {
        get { return flockDirection; }
    }

    public bool CheckForTarget
    {
        get { return checkForTarget; }
    }

    public bool CheckForHunter
    {
        get { return checkForHunter; }
    }

    public GameObject SeekerWaypoint
    {
        get { return seekerWaypoint; }
    }

    public bool WaypointReached
    {
        get { return waypointReached; }
        set { waypointReached = value; }
    }

    public Waypoint[] Path
    {
        get { return path; }
    }


    // Start!
    void Start()
    {
        HUNT_MODE = true;

        // Set up the list of flockers
        flock = new List<GameObject>();

        // Set up the seeker
        Vector3 pos = new Vector3(100, Random.Range(10, 50), Random.Range(100, Terrain.activeTerrain.terrainData.size.z - 100));
        seeker = (GameObject)Instantiate(seekerPrefab, pos, Quaternion.identity);


        // Create multiple flockers, set their hunter and add them into the list
        for (int i = 0; i < numFlockers; i++)
        {
            pos = new Vector3(50, Random.Range(25, 50), Random.Range(50, Terrain.activeTerrain.terrainData.size.z - 50));
            flocker = (GameObject)Instantiate(flockerPrefab, pos, Quaternion.identity);
            flocker.GetComponent<Flock>().hunter = seeker;
            flock.Add(flocker);
        }

        flock[0].GetComponent<Flock>().leader = true;  // set the leader


        // Set up the obstacles
        obstacles = GameObject.FindGameObjectsWithTag("Obstacle");  // add them into the array


        // Create seeker's waypoint
        pos = new Vector3(Random.Range(50, Terrain.activeTerrain.terrainData.size.x - 50), Random.Range(25, 50),
                                Random.Range(50, Terrain.activeTerrain.terrainData.size.z - 50));
        seekerWaypoint = (GameObject)Instantiate(waypointPrefab, pos, Quaternion.identity);

        // Double check
        do
        {
            SetupWaypoint();
        }
        while (NearAnObstacle());


        // Create the path
        path = new Waypoint[7];
        path[0] = new Waypoint(GameObject.Find("WP0").transform.position, GameObject.Find("WP1").transform.position);
        path[1] = new Waypoint(GameObject.Find("WP1").transform.position, GameObject.Find("WP2").transform.position);
        path[2] = new Waypoint(GameObject.Find("WP2").transform.position, GameObject.Find("WP3").transform.position);
        path[3] = new Waypoint(GameObject.Find("WP3").transform.position, GameObject.Find("WP4").transform.position);
        path[4] = new Waypoint(GameObject.Find("WP4").transform.position, GameObject.Find("WP5").transform.position);
        path[5] = new Waypoint(GameObject.Find("WP5").transform.position, GameObject.Find("WP6").transform.position);
        path[6] = new Waypoint(GameObject.Find("WP6").transform.position, GameObject.Find("WP0").transform.position);


        // Camera code
        // Set up the cameras
        currentCameraIndex = 0;

        // Turn all but the default of the cameras off
        for (int i = 1; i < cameras.Length; i++)
        {
            cameras[i].gameObject.SetActive(false);
        }

        // If any cameras were added into the controller, enable the first one
        if (cameras.Length > 0)
        {
            cameras[0].gameObject.SetActive(true);

            // Debug
            Debug.Log("Camera with name: " + cameras[0].GetComponent<Camera>().name + ", is active");
        }

        // Set up the cameras' targets
        cameras[0].GetComponent<SmoothFollow>().target = GameObject.Find("GameManagerGO").transform;
        cameras[1].GetComponent<SmoothFollow>().target = flock[0].transform;
    }

    // Update!
    void Update()
    {
        Debug.Log("Hunting mode: " + HUNT_MODE);
        // Debug - Toggle with behaviors
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (HUNT_MODE)
            {
                HUNT_MODE = false;
            }
            else
            {
                HUNT_MODE = true;
            }
        }

        // Cycle between cameras
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentCameraIndex++;
            //Debug.Log("Switching cameras!");  // debug

            // Switch cameras
            if (currentCameraIndex < cameras.Length)
            {
                cameras[currentCameraIndex - 1].gameObject.SetActive(false);
                cameras[currentCameraIndex].gameObject.SetActive(true);

                // Debug
                //Debug.Log("Camera with name: " + cameras[currentCameraIndex].GetComponent<Camera>().name + ", is active");
            }
            else
            {
                cameras[currentCameraIndex - 1].gameObject.SetActive(false);
                currentCameraIndex = 0;
                cameras[currentCameraIndex].gameObject.SetActive(true);

                // Debug
                //Debug.Log("Camera with name: " + cameras[currentCameraIndex].GetComponent<Camera>().name + ", is active");
            }
        }

        // Calculate centroid and flock direction
        CalcCentroid();
        CalcFlockDirection();

        // Compare the distance between the seeker and flock
        // Must set HUNT_MODE to true to activate
        if (!checkForTarget)
        {
            for (int i = 0; i < numFlockers; i++)
            {
                float dist = Vector3.Distance(seeker.transform.position, flock[i].transform.position);

                Debug.DrawLine(seeker.transform.position, flock[i].transform.position);  // debug

                if (HUNT_MODE)
                {
                    // Randomize the target's position
                    if (dist < (seeker.GetComponent<Vehicle>().safeDistance + flock[i].GetComponent<Vehicle>().safeDistance))
                    {
                        seeker.GetComponent<Seeker>().seekerTarget = flock[i];

                        checkForTarget = true;
                        checkForHunter = true;
                        break;
                    }
                }
            }
        }
        else
        {
            // Debug
            Debug.DrawLine(seeker.transform.position, seeker.GetComponent<Seeker>().seekerTarget.transform.position, Color.cyan);

            if (Vector3.Distance(seeker.transform.position, seeker.GetComponent<Seeker>().seekerTarget.transform.position)
                    < (seeker.GetComponent<Vehicle>().safeDistance
                    + seeker.GetComponent<Seeker>().seekerTarget.GetComponent<Vehicle>().safeDistance) * 0.6)
            {
                // Remove the target from existence
                flock.Remove(seeker.GetComponent<Seeker>().seekerTarget);
                numFlockers--;

                if (seeker.GetComponent<Seeker>().seekerTarget.GetComponent<Flock>().leader == true
                            && numFlockers > 0)
                {
                    // Pass the torch and the camera
                    flock[0].GetComponent<Flock>().leader = true;
                    cameras[1].GetComponent<SmoothFollow>().target = flock[0].transform;
                }

                // Oh gawd, it's bleeding to death
                Instantiate(bloodPrefab, seeker.GetComponent<Seeker>().seekerTarget.transform.position, Quaternion.identity);
                Destroy(seeker.GetComponent<Seeker>().seekerTarget);

                // Reset
                checkForTarget = false;
                checkForHunter = false;
            }
        }

        // Check if the seeker is near the waypoint
        if (waypointReached)
        {
            do
            {
                SetupWaypoint();
            }
            while (NearAnObstacle());
        }
    }

    // Check for obstacle avoidance
    private bool NearAnObstacle()
    {
        // Iterate through all obstacles and compare the distance between each obstacle and the waypoint
        for (int i = 0; i < obstacles.Length; i++)
        {
            // If it's too close to the obstacle, move it!
            if (Vector3.Distance(seekerWaypoint.transform.position,
                Vector3.Scale(obstacles[i].transform.position, obstacles[i].transform.localScale) * obstacles[i].GetComponent<ObstacleScript>().Radius * 2.0f) < 10.0f
                || Vector3.Distance(seekerWaypoint.transform.position,
                    obstacles[i].transform.position * obstacles[i].GetComponent<ObstacleScript>().Radius * 2.0f) < 10.0f)
            {
                return true;
            }
        }

        //Otherwise, the waypoint is not near an obstacle
        return false;
    }

    // Determine where the center of flock is
    private void CalcCentroid()
    {
        // Reset the centroid
        centroid = Vector3.zero;

        // Get the sum of positions
        for (int i = 0; i < numFlockers; i++)
        {
            centroid += flock[i].transform.position;
        }

        // Divide by the number of members in the flock
        centroid = centroid / numFlockers;
    }

    // Calculate which direction to flock to
    private void CalcFlockDirection()
    {
        // Reset flocking direction
        flockDirection = Vector3.zero;

        // Get the sum of the flock's forward vectors
        for (int i = 0; i < numFlockers; i++)
        {
            flockDirection += flock[i].transform.forward;
        }
    }

    // Reset the waypoint for the seeker
    private void SetupWaypoint()
    {
        // Pick a new spot
        Vector3 pos = new Vector3(Random.Range(50, Terrain.activeTerrain.terrainData.size.x - 50), Random.Range(25, 50),
                                Random.Range(50, Terrain.activeTerrain.terrainData.size.z - 50));
        seekerWaypoint.transform.position = pos;

        // Reset
        waypointReached = false;
    }
}
