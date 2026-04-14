using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;

namespace WormGame
{
    /// <summary>
    /// Inference controller that runs the trained model on worm agents.
    /// 
    /// After training is complete:
    ///   1. Find the .onnx model in results/<run-id>/Worm.onnx
    ///   2. Import the .onnx file into Unity (Assets folder)
    ///   3. Select each WormAgent in Hierarchy
    ///   4. In Behavior Parameters, drag the .onnx to the "Model" field
    ///   5. Set "Behavior Type" to "Inference Only"
    ///   6. Press Play - agents will run autonomously!
    /// 
    /// Alternatively, attach this script to a GameObject and assign the model
    /// to apply it to all agents automatically.
    /// </summary>
    public class WormInferenceController : MonoBehaviour
    {
        [Header("Inference Settings")]
        [Tooltip("Run in deterministic mode (no exploration noise)")]
        public bool deterministicInference = true;

        void Start()
        {
            // Find all worm agents in the scene
            var agents = FindObjectsOfType<WormAgent>();

            if (agents.Length == 0)
            {
                Debug.LogWarning("[WormInference] No WormAgent instances found in scene.");
                return;
            }

            int configuredCount = 0;
            foreach (var agent in agents)
            {
                var behaviorParams = agent.GetComponent<BehaviorParameters>();
                if (behaviorParams != null && behaviorParams.Model != null)
                {
                    // Set to inference mode if a model is assigned
                    behaviorParams.BehaviorType = BehaviorType.InferenceOnly;
                    configuredCount++;

                    Debug.Log(
                        $"[WormInference] Agent '{agent.name}' set to inference mode " +
                        $"with model: {behaviorParams.Model.name}");
                }
            }

            if (configuredCount > 0)
            {
                Debug.Log(
                    $"[WormInference] {configuredCount}/{agents.Length} agents " +
                    $"now running autonomously!");
            }
            else
            {
                Debug.LogWarning(
                    "[WormInference] No agents have a trained model assigned. " +
                    "Please assign .onnx model to each agent's Behavior Parameters → Model field.");
            }
        }
    }
}
