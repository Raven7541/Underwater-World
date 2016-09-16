using UnityEngine;
using System.Collections;

//add using System.Collections.Generic; to use the generic list format
using System.Collections.Generic;

public class Waypoint 
{
    // attributes
    private Vector3 start, end;
    private Vector3 segment;  // from point A to B
    private Vector3 unit;  // segment normalized
    //private float magnitude;

    public Vector3 GetStart
    {
        get { return start; }
    }

    public Vector3 End
    {
        get { return end; }
    }

	// Use this for initialization
	void Start () 
    {

	}

    // Constructor
    public Waypoint(Vector3 startPoint, Vector3 endPoint)
    {
        start = startPoint;
        end = endPoint;

        segment = end-start;
        unit = segment.normalized;
        //magnitude = segment.magnitude;
    }

    // Calculate the closest point
    public Vector3 ClosestPoint(Vector3 point)
    {
        Debug.DrawLine(start, end, Color.cyan);  // debug

        // Calculate the projection 
        float projection = Vector3.Dot(point-start, segment);

        // Calculate the closest point
        Vector3 closestPoint = start + unit * projection;

        return closestPoint;
    }
}
