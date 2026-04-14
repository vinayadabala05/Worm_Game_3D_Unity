using UnityEngine;

namespace WormGame
{
    /// <summary>
    /// Provides a stabilized reference frame for observations.
    /// Because ragdolls can move erratically during training, using a stabilized
    /// reference transform improves learning by providing consistent observation space.
    /// 
    /// The cube's position tracks the body and its forward direction points toward the target.
    /// </summary>
    public class OrientationCubeController : MonoBehaviour
    {
        /// <summary>
        /// Update the orientation cube to face from the body toward the target.
        /// </summary>
        /// <param name="rootBody">The root body part (head segment).</param>
        /// <param name="target">The target transform to orient toward.</param>
        public void UpdateOrientation(Transform rootBody, Transform target)
        {
            // Position the cube at the root body's position (at ground level)
            var dirVector = target.position - rootBody.position;
            dirVector.y = 0; // Keep orientation on horizontal plane
            
            // Update position to follow body
            transform.position = rootBody.position;
            
            // Rotate to face toward target direction
            if (dirVector != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dirVector);
            }
        }
    }
}
