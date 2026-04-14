"""
Worm Training Launcher
=====================
Automated script to start ML-Agents training for the Worm environment.

Usage:
    python train.py                          # Start new training
    python train.py --resume                 # Resume previous training
    python train.py --run-id my_run          # Custom run ID
    python train.py --env path/to/build.exe  # Use built environment

The script automatically uses the Python 3.10 virtual environment at d:\Game\venv.
"""

import subprocess
import sys
import os
import argparse
from datetime import datetime


# Path to the mlagents-learn executable in the venv
VENV_MLAGENTS = os.path.join(os.path.dirname(__file__), "venv", "Scripts", "mlagents-learn.exe")


def parse_args():
    parser = argparse.ArgumentParser(
        description="Worm Agent Training Launcher"
    )
    parser.add_argument(
        "--config",
        type=str,
        default="Config/worm_training.yaml",
        help="Path to training config YAML"
    )
    parser.add_argument(
        "--run-id",
        type=str,
        default=None,
        help="Run ID for this training session"
    )
    parser.add_argument(
        "--resume",
        action="store_true",
        help="Resume training from last checkpoint"
    )
    parser.add_argument(
        "--env",
        type=str,
        default=None,
        help="Path to built Unity environment executable"
    )
    parser.add_argument(
        "--num-envs",
        type=int,
        default=1,
        help="Number of parallel environments (only for built envs)"
    )
    parser.add_argument(
        "--no-graphics",
        action="store_true",
        help="Run without graphics (faster training)"
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Force overwrite existing run-id"
    )
    return parser.parse_args()


def main():
    args = parse_args()

    # Generate run-id if not provided
    if args.run_id is None:
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        args.run_id = f"Worm_{timestamp}"

    # Verify config exists
    script_dir = os.path.dirname(os.path.abspath(__file__))
    config_path = os.path.join(script_dir, args.config)
    if not os.path.exists(config_path):
        print(f"ERROR: Config file not found: {config_path}")
        sys.exit(1)

    # Find mlagents-learn
    mlagents_exe = VENV_MLAGENTS
    if not os.path.exists(mlagents_exe):
        print(f"ERROR: mlagents-learn not found at: {mlagents_exe}")
        print()
        print("Setup instructions:")
        print("  1. py -3.10 -m venv d:\\Game\\venv")
        print("  2. d:\\Game\\venv\\Scripts\\pip.exe install mlagents==1.1.0")
        sys.exit(1)

    # Build the mlagents-learn command
    cmd = [
        mlagents_exe,
        config_path,
        "--run-id", args.run_id,
    ]

    if args.resume:
        cmd.append("--resume")
    if args.force:
        cmd.append("--force")
    if args.env:
        cmd.extend(["--env", os.path.abspath(args.env)])
    if args.num_envs > 1:
        cmd.extend(["--num-envs", str(args.num_envs)])
    if args.no_graphics:
        cmd.append("--no-graphics")

    # Print training info
    print("=" * 60)
    print("  🐛 WORM AGENT TRAINING")
    print("=" * 60)
    print(f"  Config:      {config_path}")
    print(f"  Run ID:      {args.run_id}")
    print(f"  Resume:      {args.resume}")
    print(f"  Environment: {args.env or 'Unity Editor (press Play)'}")
    print(f"  No Graphics: {args.no_graphics}")
    print("=" * 60)
    print()

    if args.env is None:
        print("  INSTRUCTIONS:")
        print("  1. Training server will start below")
        print("  2. Open your Unity project")
        print("  3. Press PLAY in Unity Editor")
        print("  4. Training begins automatically!")
        print()
    else:
        print("  Training will start automatically with built environment")
        print()

    print(f"  Command: {' '.join(cmd)}")
    print("=" * 60)
    print()

    # Run training
    try:
        process = subprocess.run(cmd)
        sys.exit(process.returncode)
    except Exception as e:
        print(f"Failed to start training: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
