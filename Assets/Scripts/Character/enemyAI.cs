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
    [SerializeField] private float detectionSpeed = 4;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolWaitTime = 2f;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float catchDistance = 0.8f;

    [Header("Detection Settings")]
    [SerializeField] private float visionRange = 15f;
    [SerializeField] private float visionAngle = 60f; // half-angle from forward
    [SerializeField] private float detectionContributionClose = 1f;
    [SerializeField] private float detectionContributionMid = 0.5f;
    [SerializeField] private float detectionContributionFar = 0.6f;
    [SerializeField] private float closeRange = 5f;
    [SerializeField] private float midRange = 10f;
    [SerializeField] private float losLostHoldTime = 2f;
    [SerializeField] private LayerMask obstructionMask;

    /// <summary>
    /// Facing direction derived from sprite X flip. Right = (1,0,0), Left = (-1,0,0).
    /// </summary>
    private Vector3 FacingDirection => transform.localScale.x >= 0f ? Vector3.right : Vector3.left;

    [Header("Spirit World Settings")]
    [SerializeField] private float spiritWanderRadius = 10f;
    [SerializeField] private float spiritWanderSpeed = 1.5f;
    [SerializeField] private float spiritWaitTime = 4f; // Ruhlar aleminde bekleme süresi
    [SerializeField] private float lureSpeed = 3f;
    [SerializeField] private float lureArriveDistance = 1f;
    [SerializeField] private float lureDuration = 4f;

    [Header("Visuals (assign your real/spirit models or renderers)")]
    [SerializeField] private GameObject monsterVisual;
    [SerializeField] private GameObject humanVisual;

    [Header("Group")]
    [SerializeField] private int groupId = 0;

    [Header("Cliff & Hole Avoidance")]
    [SerializeField] private LayerMask cliffLayer;
    [SerializeField] private LayerMask holeLayer;
    [SerializeField] private float cliffCheckRadius = 0.5f;

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
    private Rigidbody2D rb;

    private void Awake()
    {
        spawnPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        PickNewPatrolPoint();
        Physics2D.IgnoreLayerCollision(7, 9, true);
        Physics2D.IgnoreLayerCollision(7, 6, true);
    }

    private void Start()
    {
        // Find player - adjust this to however your player is referenced
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;

            // Düşman ve oyuncu birbirini itemesin
            Collider2D playerCol = playerObj.GetComponent<Collider2D>();
            Collider2D myCol = GetComponent<Collider2D>();
            if (playerCol != null && myCol != null)
            {
                Physics2D.IgnoreCollision(playerCol, myCol, true);
            }
        }

        // Register with the group manager
        if (EnemyGroupManager.Instance != null)
            EnemyGroupManager.Instance.RegisterEnemy(this);

        // Subscribe to mask events
        if (MaskSystem.Instance != null)
        {
            MaskSystem.Instance.OnMaskOn += HandleMaskOn;
            MaskSystem.Instance.OnMaskOff += HandleMaskOff;
        }

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

    private void FixedUpdate()
    {
        // Game over ise hareket etme
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            return;
        }

        // Debug: State kontrolü
        if (Time.frameCount % 60 == 0)
            Debug.Log($"[EnemyAI] State: {CurrentState}, HasLOS: {HasLOS}, GameManager: {GameManager.Instance != null}, IsGameOver: {GameManager.Instance?.IsGameOver}");

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
            // LOS active: face toward player, don't move
            FlipToward(playerTransform.position);
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
        // Uçurum veya delik olmayan bir nokta bul (max 10 deneme)
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = transform.position + new Vector3(randomCircle.x, randomCircle.y, 0f);

            if (!IsPointDangerous(candidate))
            {
                currentPatrolTarget = candidate;
                return;
            }
        }

        // 10 denemede bulamadıysa mevcut pozisyonda kal
        currentPatrolTarget = transform.position;
    }

    /// <summary>
    /// Verilen noktanın uçurum üzerinde olup olmadığını kontrol eder.
    /// </summary>
    private bool IsPointOverCliff(Vector3 point)
    {
        // Cliff layer'ında collider var mı?
        Collider2D hit = Physics2D.OverlapCircle(point, cliffCheckRadius, cliffLayer);
        return hit != null;
    }

    /// <summary>
    /// Verilen noktanın (kapatılmamış) delik üzerinde olup olmadığını kontrol eder.
    /// </summary>
    private bool IsPointOverHole(Vector3 point)
    {
        // Hole layer'ında collider var mı?
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, cliffCheckRadius, holeLayer);
        foreach (var hit in hits)
        {
            Hole hole = hit.GetComponent<Hole>();
            // Delik kapalı değilse tehlikeli
            if (hole != null && !hole.IsCovered)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Verilen noktanın tehlikeli (cliff veya açık hole) olup olmadığını kontrol eder.
    /// </summary>
    private bool IsPointDangerous(Vector3 point)
    {
        return IsPointOverCliff(point) || IsPointOverHole(point);
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
            if (DetectionBar.Instance != null)
                DetectionBar.Instance.AddDetection(contribution * Time.deltaTime * detectionSpeed, this);

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

        // Oyuncu gizliyse görme
        if (PlayerHiding.Instance != null && PlayerHiding.Instance.IsHidden)
            return false;

        // Oyuncu uçuruma veya deliğe düşüyorsa görme
        if (Cliff.IsPlayerFalling || Hole.IsPlayerFallingInHole)
            return false;

        Vector3 dirToPlayer = (playerTransform.position - transform.position);
        float distance = dirToPlayer.magnitude;

        // Range check
        if (distance > visionRange) return false;

        // Cone check - use sprite facing direction instead of transform.forward
        float angle = Vector3.Angle(FacingDirection, dirToPlayer.normalized);
        if (angle > visionAngle) return false;

        // Obstruction check (2D raycast)
        Vector2 origin = (Vector2)transform.position;
        Vector2 playerPos = (Vector2)playerTransform.position;
        Vector2 dir2D = (playerPos - origin).normalized;
        RaycastHit2D hit = Physics2D.Raycast(origin, dir2D, distance, obstructionMask);
        if (hit.collider != null)
        {
            if (!hit.collider.CompareTag("Player"))
                return false;
        }

        return true;
    }

    private float CalculateDetectionContribution()
    {
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;

        // Angle factor: 1.0 when directly in front, 0.3 at cone edges
        float angle = Vector3.Angle(FacingDirection, dirToPlayer);
        float angleFactor = Mathf.Lerp(1f, 0.3f, angle / visionAngle);

        // Distance factor
        float distanceFactor;
        if (distance <= closeRange)
            distanceFactor = detectionContributionClose;
        else if (distance <= midRange)
            distanceFactor = detectionContributionMid;
        else
            distanceFactor = detectionContributionFar;

        return distanceFactor * angleFactor;
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

        // Oyuncu gizlendiyse veya uçuruma/deliğe düşüyorsa takibi bırak
        if ((PlayerHiding.Instance != null && PlayerHiding.Instance.IsHidden) || Cliff.IsPlayerFalling || Hole.IsPlayerFallingInHole)
        {
            CurrentState = EnemyState.Patrol;
            isTrackingPlayer = false;
            HasLOS = false;
            PickNewPatrolPoint();
            return;
        }

        FlipToward(playerTransform.position);
        MoveToward(playerTransform.position, chaseSpeed);

        // Oyuncuyu yakala
        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Debug: Her frame mesafeyi göster
        if (Time.frameCount % 30 == 0)
            Debug.Log($"[EnemyAI] CHASE - Distance to player: {distToPlayer:F2}, catchDistance: {catchDistance}");

        if (distToPlayer <= catchDistance)
        {
            Debug.Log($"[EnemyAI] CAUGHT PLAYER! Distance: {distToPlayer}");
            CatchPlayer();
        }
    }

    private void CatchPlayer()
    {
        Debug.Log($"[EnemyAI] CatchPlayer called. GameManager exists: {GameManager.Instance != null}");

        if (GameManager.Instance != null)
        {
            Debug.Log($"[EnemyAI] Calling TriggerGameOver. Already game over: {GameManager.Instance.IsGameOver}");
            GameManager.Instance.TriggerGameOver();
        }
        else
        {
            // GameManager yoksa manuel olarak sahneyi yeniden yükle
            Debug.LogWarning("[EnemyAI] GameManager not found! Reloading scene directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }

    // ─────────────────────────────────────────────
    // SPIRIT WORLD
    // ─────────────────────────────────────────────

    private void HandleMaskOn()
    {
        // Instant snap to spirit world
        CurrentState = EnemyState.SpiritIdle;
        isTrackingPlayer = false;
        HasLOS = false;
        isWaitingAtPatrolPoint = false;
        losLostTimer = 0f;
        SetVisuals(true);
        PickNewSpiritWanderPoint();
    }

    private void HandleMaskOff()
    {
        // Instant snap back to real world - reset to patrol
        CurrentState = EnemyState.Patrol;
        isTrackingPlayer = false;
        HasLOS = false;
        isWaitingAtPatrolPoint = false;
        losLostTimer = 0f;
        SetVisuals(false);
        PickNewPatrolPoint();
    }

    private void UpdateSpiritIdle()
    {
        // RUHLAR ALEMİNDE SÜREKLİ HAREKET - bekleme yok!
        MoveToward(currentPatrolTarget, spiritWanderSpeed);

        // Hedefe vardıysa hemen yeni hedef seç
        if (Vector3.Distance(transform.position, currentPatrolTarget) < 0.5f)
        {
            PickNewSpiritWanderPoint();
        }
    }

    public void LureToPosition(Vector3 position)
    {

        if (CurrentState != EnemyState.SpiritIdle && CurrentState != EnemyState.SpiritLured)
            return;

        lureTarget = position;
        lureTimer = lureDuration;
        CurrentState = EnemyState.SpiritLured;
    }

    private void UpdateSpiritLured()
    {
        MoveToward(lureTarget, lureSpeed);

        if (Vector3.Distance(transform.position, lureTarget) < lureArriveDistance)
        {
            Debug.Log(lureTimer);

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
        // Uçurum veya delik olmayan bir nokta bul (max 10 deneme)
        for (int i = 0; i < 10; i++)
        {
            // Minimum yarım radius kadar uzakta bir nokta seç (gıdım gıdım değil)
            float minRadius = spiritWanderRadius * 0.5f;
            float maxRadius = spiritWanderRadius;
            float distance = Random.Range(minRadius, maxRadius);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            
            Vector3 candidate = transform.position + new Vector3(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance,
                0f
            );

            if (!IsPointDangerous(candidate))
            {
                currentPatrolTarget = candidate;
                return;
            }
        }

        // 10 denemede bulamadıysa mevcut pozisyonda kal
        currentPatrolTarget = transform.position;
    }

    // ─────────────────────────────────────────────
    // MOVEMENT & UTILITY
    // ─────────────────────────────────────────────

    private void MoveToward(Vector3 target, float speed)
    {
        Vector2 current = rb.position;
        Vector2 target2D = (Vector2)target;

        Vector2 direction = target2D - current;

        if (direction.magnitude < 0.1f) return;

        direction.Normalize();
        FlipToward(target);

        rb.MovePosition(current + direction * speed * Time.fixedDeltaTime);
    }


    private void FlipToward(Vector3 target)
    {
        float dirX = target.x - transform.position.x;
        if (Mathf.Abs(dirX) < 0.01f) return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dirX > 0f ? 1f : -1f);
        transform.localScale = scale;
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

        // Vision cone (based on sprite facing direction)
        Gizmos.color = HasLOS ? Color.red : Color.green;
        Vector3 facing = Application.isPlaying ? FacingDirection : Vector3.right;
        Vector3 leftBound = Quaternion.Euler(0, 0, visionAngle) * facing * visionRange;
        Vector3 rightBound = Quaternion.Euler(0, 0, -visionAngle) * facing * visionRange;
        Gizmos.DrawLine(transform.position, transform.position + leftBound);
        Gizmos.DrawLine(transform.position, transform.position + rightBound);

        // Detection ranges
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.1f);
        Gizmos.DrawWireSphere(center, midRange);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("[EnemyAI] Collision with Player!");
            CatchPlayer();
        }
    }
}