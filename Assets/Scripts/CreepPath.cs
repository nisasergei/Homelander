using UnityEngine;
using UnityEngine.AI;

public class CreepPath : MonoBehaviour
{
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // Если точки не привязаны вручную, ищем их на сцене
        if (waypoints == null || waypoints.Length == 0)
        {
            GameObject wpParent = GameObject.Find("Waypoints");
            if (wpParent != null)
            {
                waypoints = new Transform[wpParent.transform.childCount];
                for (int i = 0; i < wpParent.transform.childCount; i++)
                {
                    waypoints[i] = wpParent.transform.GetChild(i);
                }
            }
        }

        SetNextDestination();
    }

    void Update()
    {
        // Когда крип почти дошел до точки, отправляем к следующей
        if (!agent.pathPending && agent.remainingDistance < 0.8f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            SetNextDestination();
        }
    }

    void SetNextDestination()
    {
        if (waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }
}