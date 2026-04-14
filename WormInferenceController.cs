using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.Barracuda;

namespace WormGame
{
    /// <summary>
    /// Inference controller that runs the trained model on worm agents.
    /// 
    /// When a trained model (.onnx) is available, this script:
    ///   1. Loads the model and assigns it to all agents
    ///   2. Switches agents from training mode to inference mode
    ///   3. The agents then play autonomously using the learned policy
    /// 
    /// Attach this to the same GameObject as WormEnvironmentSetup
    /// or to any persistent GameObject in the scene.
    /// 
    /// To use:
    ///   1. After training, find the .onnx model in results/<run-id>/Worm.onnx
    ///   2. Import the .onnx file into Unity (Assets folder)
    ///   3. Assign it to the NNModel field on this component
    ///   4. Press Play - agents will run autonomously!
    /// </summary>
    public class WormInferenceController : MonoBehaviour
    {
        [Header("Trained Model")]
        [Tooltip("Assign the trained .onnx model file here")]
        public NNModel trainedModel;

        [Header("Inference Settings")]
        [Tooltip("Run in deterministic mode (no exploration noise)")]
        public bool deterministicInference = true;

        void Start()
        {
            if (trainedModel == null)
            {
                Debug.LogWarning(
                    "[WormInference] No trained model assigned! " +
                    "Agents will use default (random) behavior. " +
                    "Train a model first, then assign the .onnx file.");
                return;
            }

            // Find all worm agents in the scene
            var agents = FindObjectsOfType<WormAgent>();

            foreach (var agent in agents)
            {
                var behaviorParams = agent.GetComponent<BehaviorParameters>();
                if (behaviorParams != null)
                {
                    // Assign the trained model
                    behaviorParams.Model = trainedModel;

                    // Set to inference mode
                    behaviorParams.BehaviorType = BehaviorType.InferenceOnly;

                    Debug.Log(
                        $"[WormInference] Loaded model for agent: {agent.name}");
                }
            }

            Debug.Log(
                $"[WormInference] {agents.Length} agents now running autonomously " +
                $"with trained model: {trainedModel.name}");
        }
    }
}
