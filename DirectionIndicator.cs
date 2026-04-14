using UnityEngine;

namespace WormGame
{
    /// <summary>
    /// Visual indicator that shows the direction the agent should move toward.
    /// Points from the agent toward the target.
    /// </summary>
    public class DirectionIndicator : MonoBehaviour
    {
        [Header("Indicator Settings")]
        public bool updatedAutonomously = true;
        public Transform transformToFollow;
        public Transform targetToLookAt;
        public float heightOffset = 1f;

        private void Update()
        {
            if (updatedAutonomously)
            {
                MatchOrientation(transformToFollow);
            }
        }

        /// <summary>
        /// Match the indicator's rotation to the provided transform.
        /// </summary>
        public void MatchOrientation(Transform orientationCubeTransform)
        {
            if (orientationCubeTransform == null) return;

            transform.position = new Vector3(
                orientationCubeTransform.position.x,
                orientationCubeTransform.position.y + heightOffset,
                orientationCubeTransform.position.z
            );
            transform.rotation = orientationCubeTransform.rotation;
        }
    }
}
