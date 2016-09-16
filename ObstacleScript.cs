using UnityEngine;
using System.Collections;

public class ObstacleScript : MonoBehaviour 
{
	// attributes
    private float radius;

	public float Radius
	{
        get { return radius;}
	}

    void Start()
    {
        Collider collide = GetComponent<Collider>();
		radius = collide.bounds.size.magnitude;
    }
}

	