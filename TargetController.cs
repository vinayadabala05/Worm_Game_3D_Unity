using UnityEngine;

namespace WormGame
{
    /// <summary>
    /// Attach to the target object. When the agent touches the target,
    /// the target relocates to a new random position.
    /// </summary>
    public class TargetController : MonoBehaviour
    {
        [Header("Target Settings")]
        public float spawnRadius = 10f;
        public float targetHeight = 0.5f;

        private Transform m_AgentTransform;
        private Vector3 m_OriginPos;

        public void Initialize(Transform agentTransform, Vector3 originPos)
        {
            m_AgentTransform = agentTransform;
            m_OriginPos = originPos;
        }

        /// <summary>
        /// Move the target to a new random position within range.
        /// </summary>
        public void MoveTargetToRandomPosition()
        {
            var randomDirection = Random.insideUnitSphere * spawnRadius;
            randomDirection.y = 0;
            var newPos = m_OriginPos + randomDirection;
            newPos.y = targetHeight;
            transform.position = newPos;
        }

        void OnCollisionEnter(Collision col)
        {
            // If the worm touches the target, relocate it
            MoveTargetToRandomPosition();
        }
    }
}
