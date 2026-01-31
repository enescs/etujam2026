using UnityEngine;
using System.Collections.Generic;

public enum EnemyState
{
    Patrol,
    Chase,
    SpiritIdle,
    SpiritLured
}

public class EnemyAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 8f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolWaitTime = 2f;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 5f;

    [Header("Detection Settings")]
    [SerializeField] private float visionRange = 15f;
    [SerializeField] private float visionAngle = 60f; // half-angle from forward
    [SerializeField] private float detectionContributionClose = 1f;
    [SerializeField] private float detectionContributionMid = 0.5f;
    [SerializeField] private float detectionContributionFar = 0.15f;
    [SerializeField] private float closeRange = 5f;
    [SerializeField] private float midRange = 10f;
    [SerializeField] private float trackingRotationSpeed = 5f;
    [SerializeField] private float losLostHoldTime = 2f;
    [SerializeField] private LayerMask obstructionMask;

    [Header("Spirit World Settings")]
    [SerializeField] private float spiritWanderRadius = 10f;
    [SerializeField] private float spiritWanderSpeed = 1.5f;
    [SerializeField] private float lureSpeed = 3f;
    [SerializeField] private float lureArriveDistance = 1f;
    [SerializeField] private float lureDuration = 4f;

    [Header("Visuals (assign your real/spirit models or renderers)")]
    [SerializeField] private GameObject monsterVisual;
    [SerializeField] private GameObject humanVisual;

    [Header("Group")]
    [SerializeField] private int groupId = 0;

    // State
    public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;
    public int GroupId => groupId;
    public bool HasLOS { get; private set; }

    // Internal
    private Vector3 spawnPosition;
    private Vector3 currentPatrolTarget;
    private float patrolWaitTimer;
    private bool isWaitingAtPatrolPoint;

    private Transform playerTransform;
    private bool isTrackingPlayer;
    private float losLostTimer;

    // Spirit lure
    private Vector3 lureTarget;
    private float lureTimer;

    // Cached
    private CharacterController characterController;

    private void Awake()
    {
        spawnPosition = transform.position;
        characterController = GetComponent<CharacterController>();
        PickNewPatrolPoint();
    }

    private void Start()
    {
        // Find player - adjust this to however your player is referenced
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;

        // Register with the group manager
        EnemyGroupManager.Instance.RegisterEnemy(this);

        // Subscribe to mask events
        MaskSystem.Instance.OnMaskOn += HandleMaskOn;
        MaskSystem.Instance.OnMaskOff += HandleMaskOff;

        SetVisuals(false); // start in real world
    }

    private void OnDestroy()
    {
        if (EnemyGroupManager.Instance != null)
            EnemyGroupManager.Instance.UnregisterEnemy(this);

        if (MaskSystem.Instance != null)
        {
            MaskSystem.Instance.OnMaskOn -= HandleMaskOn;
            MaskSystem.Instance.OnMaskOff -= HandleMaskOff;
        }
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                UpdateDetection();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.SpiritIdle:
                UpdateSpiritIdle();
                break;
            case EnemyState.SpiritLured:
                UpdateSpiritLured();
                break;
        }
    }

    // ─────────────────────────────────────────────
    // PATROL
    // ─────────────────────────────────────────────

    private void UpdatePatrol()
    {
        if (isTrackingPlayer)
        {
            // LOS active: rotate toward player, don't move
            RotateToward(playerTransform.position);
            return;
        }

        if (losLostTimer > 0f)
        {
            // Just lost LOS: hold facing for a moment
            losLostTimer -= Time.deltaTime;
            return;
        }

        if (isWaitingAtPatrolPoint)
        {
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0f)
            {
                isWaitingAtPatrolPoint = false;
                PickNewPatrolPoint();
            }
            return;
        }

        MoveToward(currentPatrolTarget, patrolSpeed);

        if (Vector3.Distance(transform.position, currentPatrolTarget) < 0.5f)
        {
            isWaitingAtPatrolPoint = true;
            patrolWaitTimer = patrolWaitTime;
        }
    }

    private void PickNewPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        currentPatrolTarget = spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    // ─────────────────────────────────────────────
    // DETECTION
    // ─────────────────────────────────────────────

    private void UpdateDetection()
    {
        if (playerTransform == null) return;

        bool previousLOS = HasLOS;
        HasLOS = CheckLineOfSight();

        if (HasLOS)
        {
            isTrackingPlayer = true;
            losLostTimer = losLostHoldTime;

            // Contribute to the shared detection bar
            float contribution = CalculateDetectionContribution();
            DetectionBar.Instance.AddDetection(contribution * Time.deltaTime, this);
        }
        else if (previousLOS && !HasLOS)
        {
            // Just lost LOS - start hold timer, then resume patrol
            isTrackingPlayer = false;
            // losLostTimer is already set, will count down in UpdatePatrol
        }
    }

    private bool CheckLineOfSight()
    {
        if (playerTransform == null) return false;

        Vector3 dirToPlayer = (playerTransform.position - transform.position);
        float distance = dirToPlayer.magnitude;

        // Range check
        if (distance > visionRange) return false;

        // Cone check
        float angle = Vector3.Angle(transform.forward, dirToPlayer.normalized);
        if (angle > visionAngle) return false;

        // Obstruction check
        Vector3 eyePos = transform.position + Vector3.up * 1.5f; // approximate eye height
        Vector3 playerCenter = playerTransform.position + Vector3.up * 1f;
        if (Physics.Raycast(eyePos, (playerCenter - eyePos).normalized, out RaycastHit hit, distance, obstructionMask))
        {
            // Hit something that isn't the player - obstructed
            if (!hit.transform.CompareTag("Player"))
                return false;
        }

        return true;
    }

    private float CalculateDetectionContribution()
    {
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= closeRange)
            return detectionContributionClose;
        else if (distance <= midRange)
            return detectionContributionMid;
        else
            return detectionContributionFar;
    }

    // ─────────────────────────────────────────────
    // CHASE
    // ─────────────────────────────────────────────

    public void TriggerChase()
    {
        if (CurrentState == EnemyState.SpiritIdle || CurrentState == EnemyState.SpiritLured)
            return; // don't chase in spirit world

        CurrentState = EnemyState.Chase;
    }

    private void UpdateChase()
    {
        if (playerTransform == null) return;

        RotateToward(playerTransform.position);
        MoveToward(playerTransform.position, chaseSpeed);

        // TODO: Add attack logic when close enough
        // TODO: Add logic for losing the player / returning to patrol
    }

    // ─────────────────────────────────────────────
    // SPIRIT WORLD
    // ─────────────────────────────────────────────

    private void HandleMaskOn()
    {
        // Cache real world state if needed for non-reset approach
        CurrentState = EnemyState.SpiritIdle;
        isTrackingPlayer = false;
        HasLOS = false;
        SetVisuals(true);
        PickNewPatrolPoint(); // reuse for spirit wandering
    }

    private void HandleMaskOff()
    {
        // Reset to patrol - simple and forgiving
        CurrentState = EnemyState.Patrol;
        isTrackingPlayer = false;
        HasLOS = false;
        losLostTimer = 0f;
        SetVisuals(false);
        PickNewPatrolPoint();
    }

    private void UpdateSpiritIdle()
    {
        // Wander aimlessly in human form, no detection
        if (isWaitingAtPatrolPoint)
        {
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0f)
            {
                isWaitingAtPatrolPoint = false;
                PickNewSpiritWanderPoint();
            }
            return;
        }

        MoveToward(currentPatrolTarget, spiritWanderSpeed);

        if (Vector3.Distance(transform.position, currentPatrolTarget) < 0.5f)
        {
            isWaitingAtPatrolPoint = true;
            patrolWaitTimer = patrolWaitTime;
        }
    }

    public void LureToPosition(Vector3 position)
    {
        if (CurrentState != EnemyState.SpiritIdle) return;

        lureTarget = position;
        lureTimer = lureDuration;
        CurrentState = EnemyState.SpiritLured;
    }

    private void UpdateSpiritLured()
    {
        MoveToward(lureTarget, lureSpeed);

        if (Vector3.Distance(transform.position, lureTarget) < lureArriveDistance)
        {
            lureTimer -= Time.deltaTime;
            if (lureTimer <= 0f)
            {
                CurrentState = EnemyState.SpiritIdle;
                PickNewSpiritWanderPoint();
            }
        }
    }

    private void PickNewSpiritWanderPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spiritWanderRadius;
        currentPatrolTarget = spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    // ─────────────────────────────────────────────
    // MOVEMENT & UTILITY
    // ─────────────────────────────────────────────

    private void MoveToward(Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position);
        direction.y = 0f;

        if (direction.magnitude < 0.1f) return;

        direction.Normalize();
        RotateToward(target);

        if (characterController != null)
        {
            characterController.Move(direction * speed * Time.deltaTime);
        }
        else
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }

    private void RotateToward(Vector3 target)
    {
        Vector3 direction = (target - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, trackingRotationSpeed * Time.deltaTime);
    }

    private void SetVisuals(bool spiritWorld)
    {
        if (monsterVisual != null) monsterVisual.SetActive(!spiritWorld);
        if (humanVisual != null) humanVisual.SetActive(spiritWorld);
    }

    // ─────────────────────────────────────────────
    // DEBUG
    // ─────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Patrol radius
        Gizmos.color = Color.yellow;
        Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);

        // Vision cone
        Gizmos.color = HasLOS ? Color.red : Color.green;
        Vector3 leftBound = Quaternion.Euler(0, -visionAngle, 0) * transform.forward * visionRange;
        Vector3 rightBound = Quaternion.Euler(0, visionAngle, 0) * transform.forward * visionRange;
        Gizmos.DrawLine(transform.position, transform.position + leftBound);
        Gizmos.DrawLine(transform.position, transform.position + rightBound);

        // Detection ranges
        Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
        Gizmos.DrawWireSphere(center, closeRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.1f);
        Gizmos.DrawWireSphere(center, midRange);
    }
}