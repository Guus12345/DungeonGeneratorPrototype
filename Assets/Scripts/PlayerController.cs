using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private NavMeshAgent navMeshAgent; // Navmesh agent for player movement
    public Vector3 clickPosition; // Position after clicking and sending out raycast
    private Ray _debugRay; // Debug raycast
    private float _debugRayDistance; // Distance for debug raycast

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }
    // Update is called once per frame
    void Update()
    {
        // Get the mouse click position in world space and set the navmesh destination to that chosen position
        if (Input.GetMouseButtonDown(0))
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit hitInfo))
            {
                Vector3 clickWorldPosition = hitInfo.point;
                clickPosition = clickWorldPosition;

                _debugRay = mouseRay;
                _debugRayDistance = hitInfo.distance;

                navMeshAgent.SetDestination(clickPosition);
            }
        }

        DebugExtension.DebugWireSphere(clickPosition, Color.blue, .5f);
        Debug.DrawRay(_debugRay.origin, _debugRay.direction * _debugRayDistance, Color.yellow);
    }
}
