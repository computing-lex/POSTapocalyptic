using UnityEngine;

public class menubat : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform[] waypoints;

    [Header("Settings")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;
    public float waypointRadius = 0.2f; // how close before moving to next point
    public bool loop = true;  // loop back to start when finished

    private int currentWaypoint = 0;
    private bool finished = false;

    void Update()
    {
        if (finished || waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypoint];

        // Move toward current waypoint
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * Time.deltaTime
        );

        // Rotate to face current waypoint
        Vector3 dir = (target.position - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            Quaternion baseOffset = Quaternion.Euler(90f, 0f, 0f); // corrects upward-facing mesh
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot * baseOffset, rotateSpeed * Time.deltaTime);
        }

        // Check if reached waypoint
        if (Vector3.Distance(transform.position, target.position) <= waypointRadius)
        {
            currentWaypoint++;

            if (currentWaypoint >= waypoints.Length)
            {
                if (loop)
                    currentWaypoint = 0;
                else
                    finished = true;
            }
        }
    }
}
