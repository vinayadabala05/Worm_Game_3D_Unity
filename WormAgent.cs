using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

namespace WormGame
{
    /// <summary>
    /// Worm Agent - ML-Agents Reinforcement Learning Agent
    /// 
    /// Set-up: A worm with a head (bodySegment0) and 3 body segments (bodySegment1-3).
    /// Goal: The agent must move its body toward the goal direction.
    /// 
    /// Observation Space: 64 continuous variables
    ///   - Per body part (4 segments × 13 = 52):
    ///     - Position relative to orientation cube (3)
    ///     - Rotation relative to orientation cube (4, quaternion)
    ///     - Linear velocity relative to orientation cube (3)
    ///     - Angular velocity relative to orientation cube (3)
    ///   - Goal velocity in local space (3)
    ///   - Goal direction/position in local space (3)
    ///   - Body acceleration in local space (3)
    ///   - Body angular acceleration in local space (3)
    ///   Total: 52 + 3 + 3 + 3 + 3 = 64
    /// 
    /// Action Space: 9 continuous actions
    ///   - Joint target rotations for bodySegment1 (X, Y) = 2
    ///   - Joint target rotations for bodySegment2 (X, Y) = 2
    ///   - Joint target rotations for bodySegment3 (X, Y) = 2
    ///   - Joint strength for bodySegment1 = 1
    ///   - Joint strength for bodySegment2 = 1
    ///   - Joint strength for bodySegment3 = 1
    ///   Total: 6 + 3 = 9
    /// 
    /// Reward Function: Geometric (product of components, not sum)
    ///   - Body velocity match with goal velocity (normalized 0-1)
    ///   - Body direction alignment with goal direction (normalized 0-1)
    ///   Product reward encourages the agent to maximize ALL rewards simultaneously.
    /// 
    /// Benchmark Mean Reward: 800
    /// </summary>
    [RequireComponent(typeof(JointDriveController))]
    public class WormAgent : Agent
    {
        // Maximum walking speed - used for velocity reward normalization
        const float m_MaxWalkingSpeed = 10f;

        [Header("Target Prefab")]
        [Tooltip("Target prefab to spawn - the agent will walk towards this during training.")]
        public Transform TargetPrefab;
        private Transform m_Target;

        [Header("Body Parts")]
        [Tooltip("Assign the 4 body segments: head (segment0) + 3 body segments")]
        public Transform bodySegment0; // Head
        public Transform bodySegment1; // Body segment 1
        public Transform bodySegment2; // Body segment 2
        public Transform bodySegment3; // Body segment 3 (tail)

        // Stabilized model space reference point for observations
        // Ragdolls move erratically - stabilized reference improves learning
        OrientationCubeController m_OrientationCube;

        // Visual indicator pointing towards the target
        DirectionIndicator m_DirectionIndicator;

        // Joint drive controller for managing body part physics
        JointDriveController m_JdController;

        // Starting position for fall detection
        private Vector3 m_StartingPos;

        // Acceleration tracking for observations
        private Vector3 m_LastLinearVelocity;
        private Vector3 m_LastAngularVelocity;
        private Vector3 m_BodyAcceleration;
        private Vector3 m_BodyAngularAcceleration;

        /// <summary>
        /// Initialize the agent - called once when the agent is first created.
        /// Sets up body parts, spawns target, and caches component references.
        /// </summary>
        public override void Initialize()
        {
            // Spawn the target for this agent
            SpawnTarget(TargetPrefab, transform.position);

            m_StartingPos = bodySegment0.position;

            // Get component references
            m_OrientationCube = GetComponentInChildren<OrientationCubeController>();
            m_DirectionIndicator = GetComponentInChildren<DirectionIndicator>();
            m_JdController = GetComponent<JointDriveController>();

            // Set initial orientation
            UpdateOrientationObjects();

            // Register each body part with the joint drive controller
            m_JdController.SetupBodyPart(bodySegment0);
            m_JdController.SetupBodyPart(bodySegment1);
            m_JdController.SetupBodyPart(bodySegment2);
            m_JdController.SetupBodyPart(bodySegment3);
        }

        /// <summary>
        /// Spawns a target prefab at the given position.
        /// </summary>
        void SpawnTarget(Transform prefab, Vector3 pos)
        {
            m_Target = Instantiate(prefab, pos, Quaternion.identity, transform.parent);
            var targetController = m_Target.GetComponent<TargetController>();
            if (targetController != null)
            {
                targetController.Initialize(bodySegment0, pos);
                targetController.MoveTargetToRandomPosition();
            }
        }

        /// <summary>
        /// Called at the start of each training episode.
        /// Resets all body parts to initial positions and randomizes starting rotation.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            // Reset all body parts to starting position/rotation
            foreach (var bodyPart in m_JdController.bodyPartsList)
            {
                bodyPart.Reset(bodyPart);
            }

            // Random start rotation to help generalize learned behavior
            bodySegment0.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

            // Reset acceleration tracking
            m_LastLinearVelocity = Vector3.zero;
            m_LastAngularVelocity = Vector3.zero;
            m_BodyAcceleration = Vector3.zero;
            m_BodyAngularAcceleration = Vector3.zero;

            // Move target to new random position
            var targetController = m_Target.GetComponent<TargetController>();
            if (targetController != null)
            {
                targetController.MoveTargetToRandomPosition();
            }

            UpdateOrientationObjects();
        }

        /// <summary>
        /// Collects observation data for a single body part.
        /// Each body part contributes 13 observation values:
        ///   - Position (3), Rotation (4), Velocity (3), Angular Velocity (3)
        /// All values are relative to the orientation cube for stable observations.
        /// </summary>
        public void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor)
        {
            // Position relative to orientation cube (3 values)
            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformPoint(bp.rb.position));

            // Rotation relative to orientation cube (4 values - quaternion)
            sensor.AddObservation(
                Quaternion.Inverse(m_OrientationCube.transform.rotation) * bp.rb.rotation);

            // Linear velocity relative to orientation cube (3 values)
            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformDirection(bp.rb.velocity));

            // Angular velocity relative to orientation cube (3 values)
            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity));
        }

        /// <summary>
        /// Collects all observations for the agent.
        /// Total: 64 observation variables.
        /// 
        /// Breakdown:
        ///   - Goal velocity (3)
        ///   - Goal position/direction (3)
        ///   - Body acceleration (3)
        ///   - Body angular acceleration (3)
        ///   - 4 body parts × 13 values each (52)
        ///   Total = 3 + 3 + 3 + 3 + 52 = 64
        /// </summary>
        public override void CollectObservations(VectorSensor sensor)
        {
            var cubeForward = m_OrientationCube.transform.forward;
            var velGoal = cubeForward * m_MaxWalkingSpeed;

            // Goal velocity in local space (3 values)
            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformDirection(velGoal));

            // Goal direction/position in local space (3 values)
            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformPoint(m_Target.transform.position));

            // Body acceleration in local space (3 values)
            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformDirection(m_BodyAcceleration));

            // Body angular acceleration in local space (3 values)
            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformDirection(m_BodyAngularAcceleration));

            // Body part observations: 4 parts × 13 values = 52 values
            foreach (var bodyPart in m_JdController.bodyPartsList)
            {
                CollectObservationBodyPart(bodyPart, sensor);
            }
        }

        /// <summary>
        /// Called when the agent touches the target.
        /// </summary>
        public void TouchedTarget()
        {
            AddReward(1f);
        }

        /// <summary>
        /// Receives actions from the neural network and applies them to joint drives.
        /// 
        /// 9 continuous actions:
        ///   [0,1] - bodySegment1 joint rotation (X, Y)
        ///   [2,3] - bodySegment2 joint rotation (X, Y)
        ///   [4,5] - bodySegment3 joint rotation (X, Y)
        ///   [6]   - bodySegment1 joint strength
        ///   [7]   - bodySegment2 joint strength
        ///   [8]   - bodySegment3 joint strength
        /// </summary>
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            var bpDict = m_JdController.bodyPartsDict;
            var i = -1;
            var continuousActions = actionBuffers.ContinuousActions;

            // Set target joint rotations (X, Y) for each body segment
            bpDict[bodySegment1].SetJointTargetRotation(
                continuousActions[++i], continuousActions[++i], 0);
            bpDict[bodySegment2].SetJointTargetRotation(
                continuousActions[++i], continuousActions[++i], 0);
            bpDict[bodySegment3].SetJointTargetRotation(
                continuousActions[++i], continuousActions[++i], 0);

            // Set joint strength for each body segment
            bpDict[bodySegment1].SetJointStrength(continuousActions[++i]);
            bpDict[bodySegment2].SetJointStrength(continuousActions[++i]);
            bpDict[bodySegment3].SetJointStrength(continuousActions[++i]);

            // End episode if the worm falls off the platform
            if (bodySegment0.position.y < m_StartingPos.y - 2)
            {
                EndEpisode();
            }
        }

        /// <summary>
        /// Called every physics step. Updates acceleration tracking and calculates rewards.
        /// 
        /// GEOMETRIC REWARD FUNCTION:
        /// The reward each step is a PRODUCT of all reward components (not a sum).
        /// This encourages the agent to maximize ALL rewards simultaneously,
        /// rather than exploiting just the easiest one.
        /// 
        /// Components:
        ///   1. Velocity Reward (0-1): How well body velocity matches goal velocity
        ///   2. Direction Reward (0-1): How well body direction aligns with goal direction
        ///   
        /// Step Reward = velocityReward × directionReward
        /// </summary>
        void FixedUpdate()
        {
            // Update the stabilized orientation reference
            UpdateOrientationObjects();

            // Track current velocities
            var headRb = m_JdController.bodyPartsDict[bodySegment0].rb;
            var currentLinVel = headRb.velocity;
            var currentAngVel = headRb.angularVelocity;

            // Calculate acceleration (derivative of velocity)
            if (Time.fixedDeltaTime > 0)
            {
                m_BodyAcceleration =
                    (currentLinVel - m_LastLinearVelocity) / Time.fixedDeltaTime;
                m_BodyAngularAcceleration =
                    (currentAngVel - m_LastAngularVelocity) / Time.fixedDeltaTime;
            }

            // Store for next frame
            m_LastLinearVelocity = currentLinVel;
            m_LastAngularVelocity = currentAngVel;

            // ──────────────────────────────────────────────
            // REWARD COMPONENT 1: Velocity Match (0 to 1)
            // How closely the body's velocity matches the goal velocity
            // ──────────────────────────────────────────────
            var goalVelocity = m_OrientationCube.transform.forward * m_MaxWalkingSpeed;
            var velReward = GetMatchingVelocityReward(goalVelocity, currentLinVel);

            // ──────────────────────────────────────────────
            // REWARD COMPONENT 2: Direction Alignment (0 to 1)
            // How closely the body's facing direction aligns with the goal direction
            // ──────────────────────────────────────────────
            var rotAngle = Quaternion.Angle(
                m_OrientationCube.transform.rotation,
                headRb.rotation);
            var facingReward = 1f - (rotAngle / 180f);
            facingReward = Mathf.Clamp(facingReward, 0f, 1f);

            // ──────────────────────────────────────────────
            // GEOMETRIC REWARD: Product of all components
            // This ensures the agent must maximize ALL rewards,
            // not just the easiest one to exploit
            // ──────────────────────────────────────────────
            float stepReward = velReward * facingReward;

            AddReward(stepReward);
        }

        /// <summary>
        /// Calculates a normalized reward (0 to 1) based on how closely
        /// the actual velocity matches the goal velocity.
        /// 
        /// Uses a sigmoid-shaped curve that:
        ///   - Returns ~1.0 when velocities match perfectly
        ///   - Decays toward 0.0 as the difference increases
        /// </summary>
        /// <param name="velocityGoal">The target velocity vector</param>
        /// <param name="actualVelocity">The current velocity vector</param>
        /// <returns>Reward value between 0 and 1</returns>
        public float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
        {
            // Distance between actual velocity and goal velocity
            var velDeltaMagnitude = Mathf.Clamp(
                Vector3.Distance(actualVelocity, velocityGoal),
                0,
                m_MaxWalkingSpeed);

            // Declining sigmoid-shaped curve: 1 at perfect match, 0 at max deviation
            return Mathf.Pow(
                1 - Mathf.Pow(velDeltaMagnitude / m_MaxWalkingSpeed, 2), 2);
        }

        /// <summary>
        /// Updates the orientation cube and direction indicator to face
        /// from the body toward the target.
        /// </summary>
        void UpdateOrientationObjects()
        {
            m_OrientationCube.UpdateOrientation(bodySegment0, m_Target);
            if (m_DirectionIndicator)
            {
                m_DirectionIndicator.MatchOrientation(m_OrientationCube.transform);
            }
        }
    }
}
