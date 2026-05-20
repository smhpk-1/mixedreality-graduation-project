using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class NPCScene3Wanderer : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    [Header("Movement")]
    public bool useNavMeshWhenAvailable = true;
    [Min(0.5f)] public float wanderRadius = 6f;
    [Min(0.25f)] public float minWanderLeg = 2f;
    [Min(0.5f)] public float maxWanderLeg = 5f;
    [Min(0.05f)] public float walkSpeed = 1.05f;
    [Min(0.05f)] public float acceleration = 2.4f;
    [Min(0.1f)] public float turnSpeed = 4.5f;
    [Min(0.01f)] public float stoppingDistance = 0.45f;
    [Min(0f)] public float pauseMin = 0.65f;
    [Min(0f)] public float pauseMax = 2.0f;

    [Header("Grounding And Avoidance")]
    public LayerMask groundMask = ~0;
    public LayerMask obstacleMask = ~0;
    [Min(0.1f)] public float groundProbeHeight = 2.5f;
    [Min(0.1f)] public float groundProbeDistance = 6f;
    [Range(0.1f, 1f)] public float minGroundNormalY = 0.45f;
    public float groundOffset = 0.02f;
    [Min(0.1f)] public float obstacleProbeHeight = 0.75f;
    [Min(0.05f)] public float obstacleProbeRadius = 0.22f;
    [Min(0.1f)] public float obstacleProbeDistance = 1.15f;

    [Header("Looking Around")]
    public bool lookAround = true;
    public Transform[] interestingLookTargets;
    [Min(0.1f)] public float lookIntervalMin = 1.2f;
    [Min(0.1f)] public float lookIntervalMax = 3.2f;
    [Range(5f, 120f)] public float maxLookYaw = 70f;
    [Range(1f, 60f)] public float maxLookPitch = 20f;
    [Min(0.5f)] public float lookDistance = 4f;
    [Min(0.1f)] public float headTurnSpeed = 3.5f;
    [Range(0f, 1f)] public float chestLookWeight = 0.18f;
    [Range(0f, 1f)] public float neckLookWeight = 0.32f;
    [Range(0f, 1f)] public float headLookWeight = 0.58f;

    [Header("External Control")]
    [Tooltip("True yapıldığında locomotion dışarıdan yönetilir; animasyon ve baş hareketi devam eder.")]
    public bool externalControl = false;
    [Tooltip("externalControl=true iken NPCTrainPassenger tarafından set edilir.")]
    public float externalSpeed = 0f;
    [Tooltip("True iken tüm kemik animasyonu durdurulur (tren içinde hareketsiz dururken).")]
    public bool freezeAnimation = false;

    [Header("Procedural Walk")]
    public bool animateBody = true;
    [Min(0.1f)] public float strideFrequency = 1.35f;
    [Range(0f, 1f)] public float limbPoseWeight = 0.85f;
    [Range(0f, 1f)] public float legPoseWeight = 0.5f;
    [Range(0f, 0.12f)] public float hipBob = 0.035f;
    [Range(0f, 0.08f)] public float hipSway = 0.018f;
    [Range(0f, 12f)] public float torsoCounterTwist = 4f;
    [Range(0f, 1f)] public float armSwingAmount = 0.42f;
    [Range(0f, 1f)] public float legSwingAmount = 0.32f;

    private struct BonePose
    {
        public Transform bone;
        public Quaternion localRotation;
        public Vector3 localPosition;

        public BonePose(Transform source)
        {
            bone = source;
            localRotation = source != null ? source.localRotation : Quaternion.identity;
            localPosition = source != null ? source.localPosition : Vector3.zero;
        }
    }

    private NavMeshAgent agent;
    private bool usingNavMesh;
    private bool targetReady;
    private Vector3 homePosition;
    private Vector3 targetPosition;
    private float waitTimer;
    private float currentSpeed;
    private float targetSegmentSpeed;
    private float walkedDistance;

    private bool hasHumanoidPose;
    private BonePose hips;
    private BonePose spine;
    private BonePose chest;
    private BonePose neck;
    private BonePose head;
    private BonePose leftUpperArm;
    private BonePose leftLowerArm;
    private BonePose leftHand;
    private BonePose rightUpperArm;
    private BonePose rightLowerArm;
    private BonePose rightHand;
    private BonePose leftUpperLeg;
    private BonePose leftLowerLeg;
    private BonePose leftFoot;
    private BonePose rightUpperLeg;
    private BonePose rightLowerLeg;
    private BonePose rightFoot;

    private Vector3 lookTarget;
    private float lookTimer;
    private float currentLookYaw;
    private float currentLookPitch;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        NPCIdlePose idlePose = GetComponent<NPCIdlePose>();
        if (idlePose != null)
            idlePose.enabled = false;

        CaptureHumanoidPose();
    }

    private void Start()
    {
        homePosition = transform.position;
        targetSegmentSpeed = walkSpeed;
        usingNavMesh = TryUseNavMesh();

        PickNewDestination();
        PickNewLookTarget();

        // Play'e basınca anlık kaymağı önlemek için kısa başlangıç durakması
        BeginPause();
    }

    private void Update()
    {
        // ── Dış kontrol modu (NPCTrainPassenger) ──────────────────────────
        if (externalControl)
        {
            currentSpeed   = externalSpeed;
            if (!freezeAnimation)
            {
                walkedDistance += externalSpeed * Time.deltaTime;
                UpdateLookTarget();
            }
            return;
        }

        // ── Normal wander modu ────────────────────────────────────────────
        float previousSpeed = currentSpeed;
        Vector3 previousPosition = transform.position;

        if (usingNavMesh)
            MoveWithNavMesh();
        else
            MoveManually();

        walkedDistance += FlatDistance(previousPosition, transform.position);
        UpdateLookTarget();

        if (currentSpeed < 0.01f && previousSpeed > 0.01f)
            walkedDistance += 0.03f;
    }

    private bool TryUseNavMesh()
    {
        if (!useNavMeshWhenAvailable)
            return false;

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            return false;

        transform.position = hit.position;
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = gameObject.AddComponent<NavMeshAgent>();

        agent.speed = walkSpeed;
        // Yüksek acceleration: NavMeshAgent fiziksel hızı hemen toplasın,
        // procedural walk ile senkron başlasın (kaymayı önler)
        agent.acceleration = 60f;
        agent.angularSpeed = 360f;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
        agent.updateRotation = false;
        agent.radius = 0.28f;
        agent.height = 1.75f;

        return agent.isOnNavMesh;
    }

    private void MoveWithNavMesh()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            usingNavMesh = false;
            targetReady = false;
            PickNewDestination();
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            agent.isStopped = true;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * Time.deltaTime);

            if (waitTimer <= 0f)
                PickNewDestination();

            return;
        }

        if (!targetReady)
            PickNewDestination();

        agent.isStopped = false;
        Vector3 desired = agent.desiredVelocity;
        desired.y = 0f;

        if (desired.sqrMagnitude > 0.01f)
            RotateTowards(desired.normalized);

        // currentSpeed'i agent'ın GERÇEK velocity'sine bağla — smoothing yok.
        // Bu sayede NPC fiziksel olarak hareket ettiği anda procedural walk da
        // tam motion'da animasyona başlar (ayak kayma sorununu önler).
        Vector3 actual = agent.velocity;
        actual.y = 0f;
        currentSpeed = actual.magnitude;

        if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
            BeginPause();
    }

    private void MoveManually()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * Time.deltaTime);

            if (waitTimer <= 0f)
                PickNewDestination();

            return;
        }

        if (!targetReady)
            PickNewDestination();

        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= stoppingDistance * stoppingDistance)
        {
            BeginPause();
            return;
        }

        Vector3 desiredDirection = toTarget.normalized;
        if (BlockedAhead(desiredDirection))
        {
            targetReady = false;
            PickNewDestination();
            return;
        }

        RotateTowards(desiredDirection);

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSegmentSpeed, acceleration * Time.deltaTime);
        // forward yerine desiredDirection kullan: NPC daha dönmemişken yan/geri kaymayı önler
        Vector3 moveDir = Vector3.Dot(transform.forward, desiredDirection) > 0.3f ? transform.forward : desiredDirection;
        Vector3 nextPosition = transform.position + moveDir * currentSpeed * Time.deltaTime;

        if (TryProjectToGround(nextPosition, out Vector3 grounded))
            nextPosition = grounded;
        else
            nextPosition.y = transform.position.y;

        if (FlatDistance(homePosition, nextPosition) > wanderRadius + 0.75f)
        {
            targetPosition = homePosition;
            targetReady = true;
        }
        else
        {
            transform.position = nextPosition;
        }
    }

    private void PickNewDestination()
    {
        targetReady = false;
        targetSegmentSpeed = walkSpeed * Random.Range(0.85f, 1.12f);

        if (usingNavMesh && agent != null && agent.isOnNavMesh)
        {
            for (int i = 0; i < 18; i++)
            {
                Vector3 candidate = RandomWanderPoint();
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    continue;

                targetPosition = hit.position;
                targetReady = agent.SetDestination(targetPosition);
                if (targetReady)
                    return;
            }

            BeginPause();
            return;
        }

        for (int i = 0; i < 18; i++)
        {
            Vector3 candidate = RandomWanderPoint();
            if (TryProjectToGround(candidate, out Vector3 grounded))
                candidate = grounded;
            else
                candidate.y = transform.position.y;

            if (FlatDistance(candidate, transform.position) < minWanderLeg * 0.75f)
                continue;

            if (IsSpotBlocked(candidate))
                continue;

            targetPosition = candidate;
            targetReady = true;
            return;
        }

        targetPosition = homePosition;
        targetReady = true;
    }

    private Vector3 RandomWanderPoint()
    {
        float minRadius = Mathf.Min(minWanderLeg, maxWanderLeg);
        float maxRadius = Mathf.Max(minRadius, maxWanderLeg);
        float radius = Random.Range(minRadius, maxRadius);
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

        if (FlatDistance(homePosition, transform.position + offset) > wanderRadius)
            offset = (homePosition - transform.position).normalized * Random.Range(minRadius, maxRadius);

        Vector3 candidate = transform.position + offset;
        candidate.y = homePosition.y;
        return candidate;
    }

    private void BeginPause()
    {
        waitTimer = Random.Range(Mathf.Min(pauseMin, pauseMax), Mathf.Max(pauseMin, pauseMax));
        targetReady = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private bool TryProjectToGround(Vector3 position, out Vector3 grounded)
    {
        Vector3 origin = position + Vector3.up * groundProbeHeight;
        float distance = groundProbeHeight + groundProbeDistance;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != null && hit.transform.IsChildOf(transform))
            {
                grounded = position;
                return false;
            }

            if (hit.normal.y >= minGroundNormalY)
            {
                grounded = hit.point + Vector3.up * groundOffset;
                return true;
            }
        }

        grounded = position;
        return false;
    }

    private bool IsSpotBlocked(Vector3 position)
    {
        Vector3 center = position + Vector3.up * obstacleProbeHeight;
        Collider[] hits = Physics.OverlapSphere(center, obstacleProbeRadius, obstacleMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && !hits[i].transform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    private bool BlockedAhead(Vector3 direction)
    {
        Vector3 origin = transform.position + Vector3.up * obstacleProbeHeight;
        if (!Physics.SphereCast(origin, obstacleProbeRadius, direction, out RaycastHit hit, obstacleProbeDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        return hit.transform != null && !hit.transform.IsChildOf(transform);
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void UpdateLookTarget()
    {
        if (!lookAround)
        {
            currentLookYaw = Mathf.LerpAngle(currentLookYaw, 0f, headTurnSpeed * Time.deltaTime);
            currentLookPitch = Mathf.Lerp(currentLookPitch, 0f, headTurnSpeed * Time.deltaTime);
            return;
        }

        lookTimer -= Time.deltaTime;
        if (lookTimer <= 0f)
            PickNewLookTarget();

        Vector3 origin = GetLookOrigin();
        Vector3 toLook = lookTarget - origin;
        if (toLook.sqrMagnitude < 0.01f)
            return;

        Vector3 local = transform.InverseTransformDirection(toLook.normalized);
        float desiredYaw = Mathf.Atan2(local.x, Mathf.Max(0.01f, local.z)) * Mathf.Rad2Deg;
        float desiredPitch = Mathf.Asin(Mathf.Clamp(local.y, -1f, 1f)) * Mathf.Rad2Deg;

        desiredYaw = Mathf.Clamp(desiredYaw, -maxLookYaw, maxLookYaw);
        desiredPitch = Mathf.Clamp(desiredPitch, -maxLookPitch, maxLookPitch);

        currentLookYaw = Mathf.LerpAngle(currentLookYaw, desiredYaw, headTurnSpeed * Time.deltaTime);
        currentLookPitch = Mathf.Lerp(currentLookPitch, desiredPitch, headTurnSpeed * Time.deltaTime);
    }

    private void PickNewLookTarget()
    {
        Vector3 origin = GetLookOrigin();
        lookTimer = Random.Range(Mathf.Min(lookIntervalMin, lookIntervalMax), Mathf.Max(lookIntervalMin, lookIntervalMax));

        Camera mainCamera = Camera.main;
        if (mainCamera != null && Random.value < 0.18f)
        {
            lookTarget = mainCamera.transform.position + Vector3.down * 0.08f;
            return;
        }

        Transform interest = RandomInterestingTarget();
        if (interest != null && Random.value < 0.35f)
        {
            lookTarget = interest.position + Vector3.up * 1.35f;
            return;
        }

        if (targetReady && currentSpeed > 0.1f && Random.value < 0.55f)
        {
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
            {
                Vector3 scanDirection = Quaternion.AngleAxis(Random.Range(-28f, 28f), Vector3.up) * toTarget.normalized;
                lookTarget = origin + scanDirection * lookDistance + Vector3.up * Random.Range(-0.1f, 0.45f);
                return;
            }
        }

        float yaw = Random.Range(-maxLookYaw, maxLookYaw);
        float pitch = Random.Range(-maxLookPitch * 0.45f, maxLookPitch * 0.65f);
        Vector3 direction = Quaternion.AngleAxis(yaw, Vector3.up) * transform.forward;
        direction = Quaternion.AngleAxis(-pitch, transform.right) * direction;
        lookTarget = origin + direction.normalized * lookDistance;
    }

    private Transform RandomInterestingTarget()
    {
        if (interestingLookTargets == null || interestingLookTargets.Length == 0)
            return null;

        for (int i = 0; i < interestingLookTargets.Length; i++)
        {
            Transform target = interestingLookTargets[Random.Range(0, interestingLookTargets.Length)];
            if (target != null)
                return target;
        }

        return null;
    }

    private Vector3 GetLookOrigin()
    {
        if (head.bone != null)
            return head.bone.position;

        return transform.position + Vector3.up * 1.55f;
    }

    private void CaptureHumanoidPose()
    {
        if (animator == null || !animator.isHuman)
        {
            hasHumanoidPose = false;
            return;
        }

        hips = Capture(HumanBodyBones.Hips);
        spine = Capture(HumanBodyBones.Spine);
        chest = Capture(HumanBodyBones.Chest);
        neck = Capture(HumanBodyBones.Neck);
        head = Capture(HumanBodyBones.Head);
        leftUpperArm = Capture(HumanBodyBones.LeftUpperArm);
        leftLowerArm = Capture(HumanBodyBones.LeftLowerArm);
        leftHand = Capture(HumanBodyBones.LeftHand);
        rightUpperArm = Capture(HumanBodyBones.RightUpperArm);
        rightLowerArm = Capture(HumanBodyBones.RightLowerArm);
        rightHand = Capture(HumanBodyBones.RightHand);
        leftUpperLeg = Capture(HumanBodyBones.LeftUpperLeg);
        leftLowerLeg = Capture(HumanBodyBones.LeftLowerLeg);
        leftFoot = Capture(HumanBodyBones.LeftFoot);
        rightUpperLeg = Capture(HumanBodyBones.RightUpperLeg);
        rightLowerLeg = Capture(HumanBodyBones.RightLowerLeg);
        rightFoot = Capture(HumanBodyBones.RightFoot);

        hasHumanoidPose = true;
    }

    private BonePose Capture(HumanBodyBones bone)
    {
        return new BonePose(animator.GetBoneTransform(bone));
    }

    private void LateUpdate()
    {
        if (!hasHumanoidPose || freezeAnimation)
            return;

        ResetPose();

        float motion = Mathf.Clamp01(currentSpeed / Mathf.Max(0.01f, walkSpeed));
        if (animateBody)
            ApplyProceduralWalk(motion);

        ApplyLookPose();
    }

    private void ResetPose()
    {
        ResetBone(hips, true);
        ResetBone(spine, false);
        ResetBone(chest, false);
        ResetBone(neck, false);
        ResetBone(head, false);
        ResetBone(leftUpperArm, false);
        ResetBone(leftLowerArm, false);
        ResetBone(leftHand, false);
        ResetBone(rightUpperArm, false);
        ResetBone(rightLowerArm, false);
        ResetBone(rightHand, false);
        ResetBone(leftUpperLeg, false);
        ResetBone(leftLowerLeg, false);
        ResetBone(leftFoot, false);
        ResetBone(rightUpperLeg, false);
        ResetBone(rightLowerLeg, false);
        ResetBone(rightFoot, false);
    }

    private void ResetBone(BonePose pose, bool includePosition)
    {
        if (pose.bone == null)
            return;

        pose.bone.localRotation = pose.localRotation;
        if (includePosition)
            pose.bone.localPosition = pose.localPosition;
    }

    private void ApplyProceduralWalk(float motion)
    {
        float phase = walkedDistance * strideFrequency * Mathf.PI * 2f;
        float stride = Mathf.Sin(phase);
        float doubleStep = Mathf.Abs(Mathf.Sin(phase * 2f));
        float idleBreath = Mathf.Sin(Time.time * 1.4f) * (1f - motion);

        if (hips.bone != null)
        {
            hips.bone.localPosition = hips.localPosition
                + Vector3.up * (doubleStep * hipBob * motion + idleBreath * 0.006f)
                + Vector3.right * (stride * hipSway * motion);
        }

        RotateBoneWorld(spine.bone, transform.up, -stride * torsoCounterTwist * 0.35f * motion);
        RotateBoneWorld(chest.bone, transform.up, -stride * torsoCounterTwist * motion);
        RotateBoneWorld(chest.bone, transform.right, idleBreath * 0.5f);

        PoseArm(leftUpperArm, leftLowerArm, leftHand, -1f, -stride, motion);
        PoseArm(rightUpperArm, rightLowerArm, rightHand, 1f, stride, motion);
        PoseLeg(leftUpperLeg, leftLowerLeg, leftFoot, -1f, stride, motion);
        PoseLeg(rightUpperLeg, rightLowerLeg, rightFoot, 1f, -stride, motion);
    }

    private void PoseArm(BonePose upper, BonePose lower, BonePose hand, float side, float swing, float motion)
    {
        if (upper.bone == null || lower.bone == null)
            return;

        Vector3 upperTarget = (Vector3.down
            + transform.right * side * 0.24f
            + transform.forward * swing * armSwingAmount * motion).normalized;
        AlignSegment(upper.bone, lower.bone.position - upper.bone.position, upperTarget, limbPoseWeight);

        if (hand.bone == null)
            return;

        Vector3 lowerTarget = (Vector3.down
            + transform.right * side * 0.1f
            + transform.forward * swing * armSwingAmount * 0.35f * motion).normalized;
        AlignSegment(lower.bone, hand.bone.position - lower.bone.position, lowerTarget, limbPoseWeight * 0.65f);
    }

    private void PoseLeg(BonePose upper, BonePose lower, BonePose foot, float side, float swing, float motion)
    {
        if (upper.bone == null || lower.bone == null)
            return;

        Vector3 upperTarget = (Vector3.down
            + transform.right * side * 0.04f
            + transform.forward * swing * legSwingAmount * motion).normalized;
        AlignSegment(upper.bone, lower.bone.position - upper.bone.position, upperTarget, legPoseWeight * motion);

        if (foot.bone == null)
            return;

        float kneeFollow = -Mathf.Abs(swing) * legSwingAmount * 0.45f * motion;
        Vector3 lowerTarget = (Vector3.down
            + transform.right * side * 0.02f
            + transform.forward * kneeFollow).normalized;
        AlignSegment(lower.bone, foot.bone.position - lower.bone.position, lowerTarget, legPoseWeight * 0.8f * motion);
    }

    private void AlignSegment(Transform bone, Vector3 currentDirection, Vector3 targetDirection, float weight)
    {
        if (bone == null || currentDirection.sqrMagnitude < 0.0001f || targetDirection.sqrMagnitude < 0.0001f || weight <= 0f)
            return;

        Vector3 current = currentDirection.normalized;
        Vector3 target = Vector3.Slerp(current, targetDirection.normalized, Mathf.Clamp01(weight));
        Quaternion delta = Quaternion.FromToRotation(current, target);
        bone.rotation = delta * bone.rotation;
    }

    private void ApplyLookPose()
    {
        RotateBoneWorld(chest.bone, transform.up, currentLookYaw * chestLookWeight);
        RotateBoneWorld(chest.bone, transform.right, -currentLookPitch * chestLookWeight);
        RotateBoneWorld(neck.bone, transform.up, currentLookYaw * neckLookWeight);
        RotateBoneWorld(neck.bone, transform.right, -currentLookPitch * neckLookWeight);
        RotateBoneWorld(head.bone, transform.up, currentLookYaw * headLookWeight);
        RotateBoneWorld(head.bone, transform.right, -currentLookPitch * headLookWeight);
    }

    private void RotateBoneWorld(Transform bone, Vector3 axis, float degrees)
    {
        if (bone == null || Mathf.Abs(degrees) < 0.001f || axis.sqrMagnitude < 0.001f)
            return;

        bone.rotation = Quaternion.AngleAxis(degrees, axis.normalized) * bone.rotation;
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Vector3 center = Application.isPlaying ? homePosition : transform.position;
        Gizmos.DrawWireSphere(center, wanderRadius);

        if (!targetReady)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(targetPosition + Vector3.up * 0.1f, 0.18f);
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, targetPosition + Vector3.up * 0.1f);
    }
}
