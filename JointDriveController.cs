using System.Collections.Generic;
using UnityEngine;

namespace WormGame
{
    /// <summary>
    /// Holds information about a single body part (limb/segment).
    /// </summary>
    [System.Serializable]
    public class BodyPart
    {
        [Header("Body Part Info")]
        public Transform transform;
        public Rigidbody rb;
        public ConfigurableJoint joint;

        [Header("Ground Contact")]
        public GroundContact groundContact;

        [HideInInspector] public Vector3 startingPos;
        [HideInInspector] public Quaternion startingRot;

        [Header("Current Joint Settings")]
        [Space(10)]
        public Vector3 currentEularJointRotation;

        [HideInInspector] public float currentStrength;
        public float currentXNormalizedRot;
        public float currentYNormalizedRot;
        public float currentZNormalizedRot;

        /// <summary>
        /// Reset body part to its initial position and rotation.
        /// </summary>
        public void Reset(BodyPart bp)
        {
            bp.rb.transform.position = bp.startingPos;
            bp.rb.transform.rotation = bp.startingRot;
            bp.rb.velocity = Vector3.zero;
            bp.rb.angularVelocity = Vector3.zero;
            if (bp.groundContact)
            {
                bp.groundContact.touchingGround = false;
            }
        }

        /// <summary>
        /// Sets the target rotation for the joint based on normalized input values (-1 to 1).
        /// </summary>
        public void SetJointTargetRotation(float x, float y, float z)
        {
            currentXNormalizedRot = x;
            currentYNormalizedRot = y;
            currentZNormalizedRot = z;

            // Map from [-1, 1] to the actual joint angle limits
            var xRot = joint.angularXMotion != ConfigurableJointMotion.Locked
                ? Mathf.Lerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, (x + 1f) / 2f)
                : 0f;
            var yRot = joint.angularYMotion != ConfigurableJointMotion.Locked
                ? Mathf.Lerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, (y + 1f) / 2f)
                : 0f;
            var zRot = joint.angularZMotion != ConfigurableJointMotion.Locked
                ? Mathf.Lerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, (z + 1f) / 2f)
                : 0f;

            currentEularJointRotation = new Vector3(xRot, yRot, zRot);
            joint.targetRotation = Quaternion.Euler(xRot, yRot, zRot);
        }

        /// <summary>
        /// Sets the strength (spring/damper) for the joint drive.
        /// </summary>
        public void SetJointStrength(float strength)
        {
            var rawVal = (strength + 1f) * 0.5f * thisJdController.maxJointForceLimit;
            var jd = new JointDrive
            {
                positionSpring = thisJdController.maxJointSpring,
                positionDamper = thisJdController.jointDampen,
                maximumForce = rawVal
            };
            joint.slerpDrive = jd;
            currentStrength = jd.maximumForce;
        }

        [HideInInspector] public JointDriveController thisJdController;
    }

    /// <summary>
    /// Controller that manages all body parts and their joint drives.
    /// </summary>
    public class JointDriveController : MonoBehaviour
    {
        [Header("Joint Drive Settings")]
        [Space(10)]
        public float maxJointSpring = 40000f;
        public float jointDampen = 5000f;
        public float maxJointForceLimit = 40000f;

        [HideInInspector] public Dictionary<Transform, BodyPart> bodyPartsDict = new Dictionary<Transform, BodyPart>();
        [HideInInspector] public List<BodyPart> bodyPartsList = new List<BodyPart>();

        /// <summary>
        /// Create BodyPart object and add it to dictionary/list.
        /// </summary>
        public void SetupBodyPart(Transform t)
        {
            var bp = new BodyPart
            {
                rb = t.GetComponent<Rigidbody>(),
                joint = t.GetComponent<ConfigurableJoint>(),
                startingPos = t.position,
                startingRot = t.rotation,
                transform = t,
                thisJdController = this
            };
            bp.groundContact = t.GetComponent<GroundContact>();
            if (bp.groundContact)
            {
                bp.groundContact.thisBodyPart = bp;
            }
            bodyPartsDict[t] = bp;
            bodyPartsList.Add(bp);
        }

        public BodyPart GetBodyPart(Transform t)
        {
            return bodyPartsDict[t];
        }
    }
}
