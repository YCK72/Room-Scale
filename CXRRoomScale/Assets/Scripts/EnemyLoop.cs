using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyLoop : MonoBehaviour
{
    [HideInInspector]
    public Transform Player;
    public LayerMask HidableLayers;
    public EnemyLineOfSightChecker LineOfSightChecker;
    public NavMeshAgent Agent;
    [Range(-1, 1)]
    [Tooltip("Lower is a better hiding spot")]
    public float HideSensitivity = 0;
    [Range(1, 10)]
    public float MinPlayerDistance = 5f;
    [Range(0, 5f)]
    public float MinObstacleHeight = 1.25f;
    [Range(0.01f, 1f)]
    public float UpdateFrequency = 0.25f;
    [Range(1f, 300f)]
    public float MaxHideTime = 10f; 
    [Range(1f, 300f)]
    public float MaxChaseTime = 10f;

    private Coroutine MovementCoroutine;

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        LineOfSightChecker.OnGainSight += HandleGainSight;
        LineOfSightChecker.OnLoseSight += HandleLoseSight;
    }

    private void HandleGainSight(Transform Target)
    {
        if (MovementCoroutine != null)
        {
            StopCoroutine(MovementCoroutine);
        }
        Player = Target;
        MovementCoroutine = StartCoroutine(HideAndChase(Target));
    }

    private void HandleLoseSight(Transform Target)
    {
        if (MovementCoroutine != null)
        {
            StopCoroutine(MovementCoroutine);
        }
        Player = null;
    }

    private IEnumerator HideAndChase(Transform Target)
    {
        WaitForSeconds Wait = new WaitForSeconds(UpdateFrequency);

        while (true) 
        {
            float hideTimer = 0f;
            float chaseTimer = 0f;

            // Hide logic
            while (hideTimer < MaxHideTime)
            {
                if (Target == null)
                {
                    yield break; 
                }

                HideFromPlayer(Target);
                hideTimer += UpdateFrequency;
                yield return Wait;
            }

            
            hideTimer = 0f;
            chaseTimer = 0f;

            
            while (Player != null && chaseTimer < MaxChaseTime)
            {
                if (Target == null)
                {
                    yield break; 
                }

                Agent.SetDestination(Player.position);
                chaseTimer += UpdateFrequency;
                yield return Wait;
            }

            
            while (hideTimer < MaxHideTime)
            {
                
                if (Target != null)
                {
                    HideFromPlayer(Target);
                }
                hideTimer += UpdateFrequency;
                yield return Wait;
            }
        }
    }

    private void HideFromPlayer(Transform Target)
    {
        Collider[] colliders = new Collider[10];
        int hits = Physics.OverlapSphereNonAlloc(Agent.transform.position, LineOfSightChecker.Collider.radius, colliders, HidableLayers);

        for (int i = 0; i < hits; i++)
        {
            if (Vector3.Distance(colliders[i].transform.position, Target.position) < MinPlayerDistance || colliders[i].bounds.size.y < MinObstacleHeight)
            {
                colliders[i] = null;
            }
        }

        System.Array.Sort(colliders, ColliderArraySortComparer);

        for (int i = 0; i < hits; i++)
        {
            if (NavMesh.SamplePosition(colliders[i].transform.position, out NavMeshHit hit, 2f, Agent.areaMask))
            {
                if (!NavMesh.FindClosestEdge(hit.position, out hit, Agent.areaMask))
                {
                    Debug.LogError($"Unable to find edge close to {hit.position}");
                }

                if (Vector3.Dot(hit.normal, (Target.position - hit.position).normalized) < HideSensitivity)
                {
                    Agent.SetDestination(hit.position);
                    return;
                }
                else
                {
                    
                    if (NavMesh.SamplePosition(colliders[i].transform.position - (Target.position - hit.position).normalized * 2, out NavMeshHit hit2, 2f, Agent.areaMask))
                    {
                        if (!NavMesh.FindClosestEdge(hit2.position, out hit2, Agent.areaMask))
                        {
                            Debug.LogError($"Unable to find edge close to {hit2.position} (second attempt)");
                        }

                        if (Vector3.Dot(hit2.normal, (Target.position - hit2.position).normalized) < HideSensitivity)
                        {
                            Agent.SetDestination(hit2.position);
                            return;
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"Unable to find NavMesh near object {colliders[i].name} at {colliders[i].transform.position}");
            }
        }
    }

    public int ColliderArraySortComparer(Collider A, Collider B)
    {
        if (A == null && B != null)
        {
            return 1;
        }
        else if (A != null && B == null)
        {
            return -1;
        }
        else if (A == null && B == null)
        {
            return 0;
        }
        else
        {
            return Vector3.Distance(Agent.transform.position, A.transform.position).CompareTo(Vector3.Distance(Agent.transform.position, B.transform.position));
        }
    }
}
