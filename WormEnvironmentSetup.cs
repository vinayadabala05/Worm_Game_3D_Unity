using UnityEngine;

namespace WormGame
{
    /// <summary>
    /// Scene setup script that programmatically creates the Worm training environment.
    /// 
    /// Creates:
    ///   - A ground plane
    ///   - 10 worm agents arranged in a grid (as required by the task)
    ///   - Each worm has: 1 head + 3 body segments with configurable joints
    ///   - An orientation cube and direction indicator per agent
    ///   - A target per agent
    ///   
    /// Attach this script to an empty GameObject in the scene to auto-generate
    /// the full training environment.
    /// </summary>
    public class WormEnvironmentSetup : MonoBehaviour
    {
        [Header("Environment Settings")]
        [Tooltip("Number of worm agents to spawn (spec requires 10)")]
        public int numberOfAgents = 10;

        [Tooltip("Spacing between agents in the grid")]
        public float agentSpacing = 15f;

        [Header("Worm Physics")]
        [Tooltip("Mass of each body segment")]
        public float segmentMass = 1f;

        [Tooltip("Length of each segment")]
        public float segmentLength = 1.5f;

        [Tooltip("Radius of each segment")]
        public float segmentRadius = 0.3f;

        [Header("Joint Settings")]
        public float jointSpring = 40000f;
        public float jointDamper = 5000f;
        public float jointMaxForce = 40000f;
        public float angularXLimit = 30f;
        public float angularYLimit = 30f;

        [Header("Target Settings")]
        public float targetSpawnRadius = 10f;

        [Header("Ground Settings")]
        public float groundSize = 200f;

        [Header("Visual Settings")]
        public Material groundMaterial;
        public Material headMaterial;
        public Material bodyMaterial;
        public Material targetMaterial;
        public Material cubeMaterial;
        public Material indicatorMaterial;

        void Start()
        {
            CreateMaterials();
            CreateGround();
            CreateAgents();
        }

        /// <summary>
        /// Create default materials if none assigned.
        /// </summary>
        void CreateMaterials()
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("HDRP/Lit");

            if (groundMaterial == null)
            {
                groundMaterial = new Material(shader);
                groundMaterial.color = new Color(0.35f, 0.55f, 0.35f); // Green ground
            }
            if (headMaterial == null)
            {
                headMaterial = new Material(shader);
                headMaterial.color = new Color(0.9f, 0.3f, 0.2f); // Red head
            }
            if (bodyMaterial == null)
            {
                bodyMaterial = new Material(shader);
                bodyMaterial.color = new Color(0.3f, 0.5f, 0.9f); // Blue body
            }
            if (targetMaterial == null)
            {
                targetMaterial = new Material(shader);
                targetMaterial.color = new Color(1f, 0.85f, 0.1f); // Yellow target
                targetMaterial.EnableKeyword("_EMISSION");
                targetMaterial.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.1f) * 0.5f);
            }
            if (cubeMaterial == null)
            {
                cubeMaterial = new Material(shader);
                cubeMaterial.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                // Make semi-transparent
                cubeMaterial.SetFloat("_Mode", 3);
                cubeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                cubeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                cubeMaterial.SetInt("_ZWrite", 0);
                cubeMaterial.DisableKeyword("_ALPHATEST_ON");
                cubeMaterial.EnableKeyword("_ALPHABLEND_ON");
                cubeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                cubeMaterial.renderQueue = 3000;
            }
            if (indicatorMaterial == null)
            {
                indicatorMaterial = new Material(shader);
                indicatorMaterial.color = new Color(0.2f, 0.9f, 0.3f); // Green indicator
            }
        }

        /// <summary>
        /// Create the ground plane.
        /// </summary>
        void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.tag = "ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(
                groundSize / 10f, 1, groundSize / 10f);
            ground.GetComponent<Renderer>().material = groundMaterial;
            ground.isStatic = true;
        }

        /// <summary>
        /// Create all 10 worm agents in a grid layout.
        /// </summary>
        void CreateAgents()
        {
            int cols = 5;
            int rows = Mathf.CeilToInt((float)numberOfAgents / cols);

            for (int i = 0; i < numberOfAgents; i++)
            {
                int row = i / cols;
                int col = i % cols;

                Vector3 agentPos = new Vector3(
                    col * agentSpacing - (cols - 1) * agentSpacing / 2f,
                    0.5f,
                    row * agentSpacing - (rows - 1) * agentSpacing / 2f
                );

                CreateSingleWormAgent(agentPos, i);
            }
        }

        /// <summary>
        /// Create a single worm agent with all required components.
        /// </summary>
        void CreateSingleWormAgent(Vector3 position, int agentIndex)
        {
            // ── Agent Root ──
            var agentRoot = new GameObject($"WormAgent_{agentIndex}");
            agentRoot.transform.position = position;

            // ── Create Body Parts ──
            Transform[] segments = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                segments[i] = CreateBodySegment(
                    agentRoot.transform,
                    position + Vector3.forward * (i * segmentLength),
                    i,
                    i == 0 // isHead
                );
            }

            // ── Create Configurable Joints ──
            // Segments 1, 2, 3 connect back to their predecessor
            for (int i = 1; i < 4; i++)
            {
                CreateJoint(segments[i], segments[i - 1]);
            }

            // ── Create Orientation Cube ──
            var orientationCube = CreateOrientationCube(agentRoot.transform, position);

            // ── Create Direction Indicator ──
            var directionIndicator = CreateDirectionIndicator(
                agentRoot.transform, position);

            // ── Create Target Prefab Template ──
            var targetPrefab = CreateTargetPrefab(agentRoot.transform, position);

            // ── Setup Agent Component ──
            var agent = agentRoot.AddComponent<WormAgent>();
            agent.bodySegment0 = segments[0];
            agent.bodySegment1 = segments[1];
            agent.bodySegment2 = segments[2];
            agent.bodySegment3 = segments[3];
            agent.TargetPrefab = targetPrefab;

            // ── Setup Joint Drive Controller ──
            var jdController = agentRoot.GetComponent<JointDriveController>();
            jdController.maxJointSpring = jointSpring;
            jdController.jointDampen = jointDamper;
            jdController.maxJointForceLimit = jointMaxForce;

            // ── Setup Decision Requester ──
            var decisionRequester = agentRoot.AddComponent<Unity.MLAgents.DecisionRequester>();
            decisionRequester.DecisionPeriod = 5;
            decisionRequester.TakeActionsBetweenDecisions = true;

            // ── Setup Behavior Parameters ──
            // This is auto-added by the Agent component
            var behaviorParams = agentRoot.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
            if (behaviorParams != null)
            {
                behaviorParams.BehaviorName = "Worm";
                behaviorParams.BrainParameters.VectorObservationSize = 64;
                behaviorParams.BrainParameters.ActionSpec =
                    Unity.MLAgents.Actuators.ActionSpec.MakeContinuous(9);
            }

            // Destroy the template target (agent will spawn its own in Initialize)
            Destroy(targetPrefab.gameObject);
        }

        /// <summary>
        /// Create a single body segment (capsule).
        /// </summary>
        Transform CreateBodySegment(Transform parent, Vector3 position, int index, bool isHead)
        {
            var segment = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            segment.name = isHead ? "Head_Segment0" : $"Body_Segment{index}";
            segment.transform.parent = parent;
            segment.transform.position = position;
            segment.transform.rotation = Quaternion.Euler(90, 0, 0); // Lay flat

            // Scale the capsule to worm proportions
            float radius = isHead ? segmentRadius * 1.3f : segmentRadius;
            segment.transform.localScale = new Vector3(
                radius * 2f,
                segmentLength / 2f,
                radius * 2f
            );

            // Setup Rigidbody
            var rb = segment.AddComponent<Rigidbody>();
            rb.mass = segmentMass;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Setup Material
            segment.GetComponent<Renderer>().material = isHead ? headMaterial : bodyMaterial;

            // Setup Ground Contact
            var gc = segment.AddComponent<GroundContact>();
            gc.penalizeGroundContact = false;

            return segment.transform;
        }

        /// <summary>
        /// Create a configurable joint between two body segments.
        /// </summary>
        void CreateJoint(Transform child, Transform connectedBody)
        {
            var joint = child.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = connectedBody.GetComponent<Rigidbody>();

            // Lock position (segments stay connected)
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            // Allow rotation on X and Y axes
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            // Set angular limits
            var lowXLimit = joint.lowAngularXLimit;
            lowXLimit.limit = -angularXLimit;
            joint.lowAngularXLimit = lowXLimit;

            var highXLimit = joint.highAngularXLimit;
            highXLimit.limit = angularXLimit;
            joint.highAngularXLimit = highXLimit;

            var yLimit = joint.angularYLimit;
            yLimit.limit = angularYLimit;
            joint.angularYLimit = yLimit;

            // Setup joint drive
            var drive = new JointDrive
            {
                positionSpring = jointSpring,
                positionDamper = jointDamper,
                maximumForce = jointMaxForce
            };
            joint.slerpDrive = drive;
            joint.rotationDriveMode = RotationDriveMode.Slerp;

            // Anchor at the midpoint between segments
            joint.anchor = new Vector3(0, -0.5f, 0);
            joint.autoConfigureConnectedAnchor = true;
        }

        /// <summary>
        /// Create the orientation cube (stabilized reference frame).
        /// </summary>
        Transform CreateOrientationCube(Transform parent, Vector3 position)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "OrientationCube";
            cube.transform.parent = parent;
            cube.transform.position = position + Vector3.up * 2f;
            cube.transform.localScale = Vector3.one * 0.3f;

            // Disable collider so it doesn't interfere with physics
            Destroy(cube.GetComponent<Collider>());

            // Semi-transparent material
            cube.GetComponent<Renderer>().material = cubeMaterial;

            // Add the orientation controller
            cube.AddComponent<OrientationCubeController>();

            return cube.transform;
        }

        /// <summary>
        /// Create the direction indicator arrow.
        /// </summary>
        Transform CreateDirectionIndicator(Transform parent, Vector3 position)
        {
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.name = "DirectionIndicator";
            indicator.transform.parent = parent;
            indicator.transform.position = position + Vector3.up * 3f;
            indicator.transform.localScale = new Vector3(0.1f, 0.1f, 1f);

            // Disable collider
            Destroy(indicator.GetComponent<Collider>());

            indicator.GetComponent<Renderer>().material = indicatorMaterial;

            var dirIndicator = indicator.AddComponent<DirectionIndicator>();
            dirIndicator.updatedAutonomously = false;

            return indicator.transform;
        }

        /// <summary>
        /// Create a target prefab template.
        /// </summary>
        Transform CreateTargetPrefab(Transform parent, Vector3 position)
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "Target";
            target.transform.position = position + Vector3.forward * 10f;
            target.transform.localScale = Vector3.one * 1.5f;

            target.GetComponent<Renderer>().material = targetMaterial;

            // Add target controller
            var tc = target.AddComponent<TargetController>();
            tc.spawnRadius = targetSpawnRadius;
            tc.targetHeight = 0.5f;

            // Set it inactive until the agent spawns its own copy
            target.SetActive(false);
            target.transform.parent = parent;

            return target.transform;
        }
    }
}
