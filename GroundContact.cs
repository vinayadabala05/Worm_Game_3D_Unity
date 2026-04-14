using UnityEngine;

namespace WormGame
{
    /// <summary>
    /// Detects when a body part touches the ground.
    /// Attach this to each body segment that needs ground contact detection.
    /// </summary>
    public class GroundContact : MonoBehaviour
    {
        [HideInInspector] public BodyPart thisBodyPart;
        public bool touchingGround;

        /// <summary>
        /// Whether to penalize the agent for this body part touching the ground.
        /// </summary>
        [Header("Ground Contact Settings")]
        public bool penalizeGroundContact;
        public float groundContactPenalty = -1f;

        void OnCollisionEnter(Collision col)
        {
            if (col.transform.CompareTag("ground"))
            {
                touchingGround = true;
            }
        }

        void OnCollisionExit(Collision col)
        {
            if (col.transform.CompareTag("ground"))
            {
                touchingGround = false;
            }
        }
    }
}
