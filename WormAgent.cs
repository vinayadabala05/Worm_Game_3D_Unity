using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

namespace WormGame
{
    [RequireComponent(typeof(JointDriveController))]
    public class WormAgent : Agent
    {
        const float m_MaxWalkingSpeed = 10f;

        public Transform TargetPrefab;
        private Transform m_Target;

        public Transform bodySegment0;
        public Transform bodySegment1;
        public Transform bodySegment2;
        public Transform bodySegment3;

        OrientationCubeController m_OrientationCube;
        DirectionIndicator m_DirectionIndicator;
        JointDriveController m_JdController;

        private Vector3 m_StartingPos;

        private Vector3 m_LastLinearVelocity;
        private Vector3 m_LastAngularVelocity;
        private Vector3 m_BodyAcceleration;
        private Vector3 m_BodyAngularAcceleration;

        public override void Initialize()
        {
            m_JdController = GetComponent<JointDriveController>();
            m_OrientationCube = GetComponentInChildren<OrientationCubeController>();
            m_DirectionIndicator = GetComponentInChildren<DirectionIndicator>();

            if (TargetPrefab == null)
            {
                Debug.LogError("TargetPrefab NOT assigned!");
                return;
            }

            if (bodySegment0 == null || bodySegment1 == null ||
                bodySegment2 == null || bodySegment3 == null)
            {
                Debug.LogError("Body segments NOT assigned!");
                return;
            }

            if (m_OrientationCube == null)
            {
                Debug.LogError("OrientationCube missing!");
                return;
            }

            SpawnTarget(TargetPrefab, transform.position);

            if (m_Target == null)
            {
                Debug.LogError("Target not created!");
                return;
            }

            m_StartingPos = bodySegment0.position;

            m_JdController.SetupBodyPart(bodySegment0);
            m_JdController.SetupBodyPart(bodySegment1);
            m_JdController.SetupBodyPart(bodySegment2);
            m_JdController.SetupBodyPart(bodySegment3);

            UpdateOrientationObjects();
        }

        void SpawnTarget(Transform prefab, Vector3 pos)
        {
            if (prefab == null) return;

            m_Target = Instantiate(prefab, pos, Quaternion.identity, transform.parent);

            var tc = m_Target.GetComponent<TargetController>();
            if (tc != null)
            {
                tc.Initialize(bodySegment0, pos);
                tc.MoveTargetToRandomPosition();
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // 🔥 MAIN FIX
            if (m_OrientationCube == null || m_Target == null || m_JdController == null)
                return;

            var cubeForward = m_OrientationCube.transform.forward;
            var velGoal = cubeForward * m_MaxWalkingSpeed;

            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformDirection(velGoal));

            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformPoint(m_Target.position));

            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformDirection(m_BodyAcceleration));

            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformDirection(m_BodyAngularAcceleration));

            if (m_JdController.bodyPartsList == null) return;

            foreach (var bodyPart in m_JdController.bodyPartsList)
            {
                if (bodyPart != null)
                    CollectObservationBodyPart(bodyPart, sensor);
            }
        }

        public void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor)
        {
            if (bp == null || m_OrientationCube == null) return;

            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformPoint(bp.rb.position));

            sensor.AddObservation(
                Quaternion.Inverse(m_OrientationCube.transform.rotation) * bp.rb.rotation);

            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformDirection(bp.rb.velocity));

            sensor.AddObservation(
                m_OrientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity));
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            if (m_JdController == null) return;

            var bpDict = m_JdController.bodyPartsDict;
            var actions = actionBuffers.ContinuousActions;

            int i = -1;

            if (bpDict.ContainsKey(bodySegment1))
                bpDict[bodySegment1].SetJointTargetRotation(actions[++i], actions[++i], 0);

            if (bpDict.ContainsKey(bodySegment2))
                bpDict[bodySegment2].SetJointTargetRotation(actions[++i], actions[++i], 0);

            if (bpDict.ContainsKey(bodySegment3))
                bpDict[bodySegment3].SetJointTargetRotation(actions[++i], actions[++i], 0);

            if (bpDict.ContainsKey(bodySegment1))
                bpDict[bodySegment1].SetJointStrength(actions[++i]);

            if (bpDict.ContainsKey(bodySegment2))
                bpDict[bodySegment2].SetJointStrength(actions[++i]);

            if (bpDict.ContainsKey(bodySegment3))
                bpDict[bodySegment3].SetJointStrength(actions[++i]);

            if (bodySegment0 != null && bodySegment0.position.y < m_StartingPos.y - 2)
            {
                EndEpisode();
            }
        }

        void FixedUpdate()
        {
            if (m_JdController == null || m_OrientationCube == null) return;

            UpdateOrientationObjects();

            if (!m_JdController.bodyPartsDict.ContainsKey(bodySegment0)) return;

            var headRb = m_JdController.bodyPartsDict[bodySegment0].rb;

            var currentLinVel = headRb.velocity;
            var currentAngVel = headRb.angularVelocity;

            if (Time.fixedDeltaTime > 0)
            {
                m_BodyAcceleration =
                    (currentLinVel - m_LastLinearVelocity) / Time.fixedDeltaTime;

                m_BodyAngularAcceleration =
                    (currentAngVel - m_LastAngularVelocity) / Time.fixedDeltaTime;
            }

            m_LastLinearVelocity = currentLinVel;
            m_LastAngularVelocity = currentAngVel;

            var goalVelocity = m_OrientationCube.transform.forward * m_MaxWalkingSpeed;
            var velReward = GetMatchingVelocityReward(goalVelocity, currentLinVel);

            var rotAngle = Quaternion.Angle(
                m_OrientationCube.transform.rotation,
                headRb.rotation);

            var facingReward = Mathf.Clamp(1f - (rotAngle / 180f), 0f, 1f);

            AddReward(velReward * facingReward);
        }

        public float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
        {
            var velDelta = Mathf.Clamp(
                Vector3.Distance(actualVelocity, velocityGoal),
                0,
                m_MaxWalkingSpeed);

            return Mathf.Pow(1 - Mathf.Pow(velDelta / m_MaxWalkingSpeed, 2), 2);
        }

        void UpdateOrientationObjects()
        {
            if (m_OrientationCube == null || bodySegment0 == null || m_Target == null)
                return;

            m_OrientationCube.UpdateOrientation(bodySegment0, m_Target);

            if (m_DirectionIndicator != null)
            {
                m_DirectionIndicator.MatchOrientation(m_OrientationCube.transform);
            }
        }
    }
}