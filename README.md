# 🐛 Worm Game - ML-Agents Reinforcement Learning

A physics-based worm locomotion game where an AI agent learns to move a segmented worm toward a target direction using Unity ML-Agents.

---

## 📋 Task Specification

| Requirement | Implementation |
|---|---|
| **Set-up** | Worm with 1 head + 3 body segments (4 total) |
| **Goal** | Agent moves body toward goal direction |
| **Agents** | 10 agents with same Behavior Parameters |
| **Reward Function** | Geometric (product, not sum) |
| **Observations** | 64 continuous variables |
| **Actions** | 9 continuous actions |
| **Visual Observations** | None |
| **Float Properties** | None |
| **Benchmark Mean Reward** | 800 |

---

## 🧠 Agent Design

### Observation Space (64 variables)

| Component | Variables | Count |
|---|---|---|
| Goal velocity (local space) | x, y, z | 3 |
| Goal direction/position (local space) | x, y, z | 3 |
| Body acceleration (local space) | x, y, z | 3 |
| Body angular acceleration (local space) | x, y, z | 3 |
| **Per body part (×4 segments):** | | |
| → Position (relative to orientation cube) | x, y, z | 3 |
| → Rotation (relative to orientation cube) | x, y, z, w | 4 |
| → Linear velocity (relative to orientation cube) | x, y, z | 3 |
| → Angular velocity (relative to orientation cube) | x, y, z | 3 |
| **Body parts subtotal** | 13 × 4 | **52** |
| **Total** | | **64** |

### Action Space (9 continuous actions)

| Action | Description |
|---|---|
| [0] | Body Segment 1 - Joint X rotation |
| [1] | Body Segment 1 - Joint Y rotation |
| [2] | Body Segment 2 - Joint X rotation |
| [3] | Body Segment 2 - Joint Y rotation |
| [4] | Body Segment 3 - Joint X rotation |
| [5] | Body Segment 3 - Joint Y rotation |
| [6] | Body Segment 1 - Joint strength |
| [7] | Body Segment 2 - Joint strength |
| [8] | Body Segment 3 - Joint strength |

### Geometric Reward Function

```
stepReward = velocityReward × directionReward
```

- **Velocity Reward** (0→1): Sigmoid-shaped reward based on how closely body velocity matches goal velocity
- **Direction Reward** (0→1): Linear reward based on angular alignment between body heading and goal direction
- **Product** ensures the agent must maximize BOTH components (not just the easiest one)

---

## 📁 Project Structure

```
Game/
├── Scripts/
│   ├── WormAgent.cs                 # Main ML-Agent with geometric reward
│   ├── JointDriveController.cs      # Physics joint management
│   ├── GroundContact.cs             # Ground collision detection
│   ├── OrientationCubeController.cs # Stabilized reference frame
│   ├── DirectionIndicator.cs        # Visual direction arrow
│   ├── TargetController.cs          # Target spawning & relocation
│   ├── WormEnvironmentSetup.cs      # Scene builder (10 agents)
│   └── WormInferenceController.cs   # Autonomous play with trained model
├── Config/
│   └── worm_training.yaml           # ML-Agents training configuration
├── train.py                         # Python training launcher
└── README.md                        # This file
```

---

## 🚀 Setup Instructions

### Prerequisites
- Unity 2021.3+ (LTS recommended)
- Unity ML-Agents Package (com.unity.ml-agents)
- Python 3.8+ with `mlagents` package

### Step 1: Unity Project Setup

1. **Create a new Unity project** (or use existing)
2. **Install ML-Agents package**:
   - Window → Package Manager → Add package by name
   - Enter: `com.unity.ml-agents`
3. **Copy the `Scripts/` folder** into your Unity `Assets/` directory
4. **Create a new Scene**
5. **Create an empty GameObject** and attach `WormEnvironmentSetup.cs`
6. **Press Play** — 10 worm agents will be created automatically

### Step 2: Install Python Training Tools

```bash
# Create virtual environment (recommended)
python -m venv venv
venv\Scripts\activate    # Windows
# source venv/bin/activate  # Linux/Mac

# Install ML-Agents
pip install mlagents==1.1.0
```

### Step 3: Train the Agent

```bash
# From the Game directory:
python train.py

# Or directly:
mlagents-learn Config/worm_training.yaml --run-id Worm_training_1
```

Then **press Play in Unity** to start training.

### Step 4: Run Trained Agent (Autonomous Play)

1. After training, find your model: `results/Worm_training_1/Worm.onnx`
2. Import the `.onnx` file into Unity Assets
3. Add `WormInferenceController` to a GameObject in the scene
4. Assign the `.onnx` model to the `Trained Model` field
5. Press Play — agents play autonomously!

---

## 📊 Training Configuration

| Parameter | Value | Rationale |
|---|---|---|
| Trainer | PPO | Best for continuous locomotion |
| Learning Rate | 3.0e-4 | Standard for locomotion tasks |
| Batch Size | 2048 | Large batch for stable gradients |
| Buffer Size | 20480 | 10× batch for diverse experience |
| Hidden Units | 512 | Large network for complex movement |
| Layers | 3 | Deep network for nuanced control |
| Gamma | 0.995 | High discount for long-horizon task |
| Max Steps | 30M | Sufficient for locomotion convergence |
| Time Horizon | 1000 | Long episodes for locomotion |

---

## 🔧 Monitoring Training

```bash
# View training progress with TensorBoard
tensorboard --logdir results
```

Key metrics to watch:
- **Environment/Cumulative Reward**: Should approach 800
- **Policy/Entropy**: Should gradually decrease
- **Policy/Learning Rate**: Linear decay from 3e-4

---

## 🏗️ Architecture Overview

```
WormAgent (ML-Agent)
├── Observations (64 vars) ──→ Neural Network (PPO)
│   ├── Goal velocity (3)           ├── Hidden: 512 × 3 layers
│   ├── Goal position (3)           └── Output: 9 continuous actions
│   ├── Body acceleration (3)
│   ├── Body angular accel (3)
│   └── 4× Body Parts (52)
│       ├── Position (3)
│       ├── Rotation (4)
│       ├── Velocity (3)
│       └── Angular Velocity (3)
│
├── Actions (9) ──→ Physics Joints
│   ├── Segment 1: rotation X,Y + strength
│   ├── Segment 2: rotation X,Y + strength
│   └── Segment 3: rotation X,Y + strength
│
└── Reward (geometric) ──→ Training Signal
    ├── velReward = sigmoid(velocity_match)     ∈ [0,1]
    ├── dirReward = 1 - (angle / 180)           ∈ [0,1]
    └── stepReward = velReward × dirReward
```
