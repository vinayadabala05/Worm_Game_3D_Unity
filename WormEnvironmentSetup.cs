using UnityEngine;

namespace WormGame
{
    public class WormEnvironmentSetup : MonoBehaviour
    {
        public int numberOfAgents = 10;
        public float agentSpacing = 15f;

        public float segmentMass = 1f;
        public float segmentLength = 1.5f;

        public float jointSpring = 40000f;
        public float jointDamper = 5000f;
        public float jointMaxForce = 40000f;

        public float targetSpawnRadius = 10f;

        public Material headMaterial;
        public Material bodyMaterial;
        public Material targetMaterial;

        // 🔥 IMPORTANT: assign in inspector
        public Transform targetPrefab;

        void Start()
        {
            CreateGround();
            CreateAgents();
        }

        void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20, 1, 20);
        }

        void CreateAgents()
        {
            for (int i = 0; i < numberOfAgents; i++)
            {
                Vector3 pos = new Vector3(i * agentSpacing, 1, 0);
                CreateSingleWormAgent(pos, i);
            }
        }

        void CreateSingleWormAgent(Vector3 position, int index)
        {
            var root = new GameObject($"WormAgent_{index}");
            root.transform.position = position;

            Transform[] segments = new Transform[4];

            for (int i = 0; i < 4; i++)
            {
                segments[i] = CreateSegment(root.transform, position + Vector3.forward * i * segmentLength, i == 0);
            }

            for (int i = 1; i < 4; i++)
            {
                var joint = segments[i].gameObject.AddComponent<ConfigurableJoint>();
                joint.connectedBody = segments[i - 1].GetComponent<Rigidbody>();
            }

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = root.transform;
            cube.transform.position = position + Vector3.up * 2;
            Destroy(cube.GetComponent<Collider>());
            cube.AddComponent<OrientationCubeController>();

            var indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.transform.parent = root.transform;
            indicator.transform.position = position + Vector3.up * 3;
            Destroy(indicator.GetComponent<Collider>());
            indicator.AddComponent<DirectionIndicator>();

            var agent = root.AddComponent<WormAgent>();

            agent.bodySegment0 = segments[0];
            agent.bodySegment1 = segments[1];
            agent.bodySegment2 = segments[2];
            agent.bodySegment3 = segments[3];

            // 🔥 FIXED: real prefab from inspector
            agent.TargetPrefab = targetPrefab;

            var jd = root.GetComponent<JointDriveController>();
            jd.maxJointSpring = jointSpring;
            jd.jointDampen = jointDamper;
            jd.maxJointForceLimit = jointMaxForce;

            var dr = root.AddComponent<Unity.MLAgents.DecisionRequester>();
            dr.DecisionPeriod = 5;
        }

        Transform CreateSegment(Transform parent, Vector3 pos, bool isHead)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.parent = parent;
            obj.transform.position = pos;

            var rb = obj.AddComponent<Rigidbody>();
            rb.mass = segmentMass;

            obj.GetComponent<Renderer>().material = isHead ? headMaterial : bodyMaterial;

            return obj.transform;
        }
    }
}