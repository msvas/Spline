using UnityEngine;
using System.Collections;

public class Walker : MonoBehaviour {

    public Grapher1 graph;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        float elapsedTime = Time.time;
        Vector3 newPosition = graph.GetPoint(elapsedTime);

        transform.position = newPosition;
    }
}
