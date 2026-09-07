# Opera-Sim Hackathon Sample Code — Docker Environment

Dockerfile and helper scripts to build a ready-to-run ROS 2 Humble environment for the
[Opera-Sim hackathon Sample Code](https://github.com/pwri-opera/Opera-hackathon_SampleCode).

GUI is enabled via [rocker](https://github.com/osrf/rocker).

## Prerequisites (host)

1. **Docker**

   ```bash
   wget -O get-docker.sh https://get.docker.com
   sudo sh ./get-docker.sh
   ```

2. **rocker**

   ```bash
   sudo apt-get update
   sudo apt-get install -y python3-rocker
   ```

3. *(Optional)* **NVIDIA Container Toolkit** — only needed for GPU acceleration

   See [NVIDIA's install guide](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/latest/install-guide.html).

## Quick start

```bash
cd docker-hackathon-sample

# 1. Build the image
./build.sh

# 2. Launch a container with GUI (X11)
./start.sh            # CPU
# or
./start-nvidia.sh     # NVIDIA GPU

# 3. Open another terminal in the same container
./enter.sh
```

## Running the sample code

Inside the container the workspace is at `/ws` and the overlay is sourced
automatically (via `/etc/bash.bashrc`).

**Terminal 1** — start the ROS-TCP endpoint:

```bash
source /ws/install/setup.bash
ros2 launch ros_tcp_endpoint endpoint.py
```

**Terminal 2** — launch the hackathon sample:

```bash
source /ws/install/setup.bash
ros2 launch task_manager hackathon_sample.launch.py
```

Then configure the Unity side (Robotics → ROS Settings) with the ROS IP and
port (default 10000), set Protocol to ROS2, and press Play.

## What the Dockerfile does

| Step | Detail |
|------|--------|
| Base image | `osrf/ros:humble-desktop-full` (Ubuntu 22.04 + ROS 2 Humble) |
| ROS packages | navigation2, nav2-bringup, robot-localization, tf-transformations, rosbridge-suite, moveit, controllers, diagnostic-updater, etc. |
| Cloned repos | `ros_tcp_endpoint` (main-ros2), `ic120_ros2` (feature/tmp_ic120_unity_setting), `Opera-sim_hackathon-Sample` (main), `ZX200_autonomy_state_machine` (hackathon), `Opera-sim_ic120_hackathon` (main), `com3_ros` (main), `gnss_localizer_ros2` (main) |
| Build | `colcon build --symlink-install --packages-up-to ic120_navigation task_manager zx200_autonomy ic120_autonomy ic120_unity com3_msgs ros_tcp_endpoint` |
| waypoints.csv | Copied to `/ws/waypoints.csv` |

## Files

| File | Purpose |
|------|---------|
| `Dockerfile` | Image definition |
| `build.sh` | `docker build` wrapper |
| `start.sh` | rocker launch with `--x11` (CPU) |
| `start-nvidia.sh` | rocker launch with `--x11 --nvidia` (GPU) |
| `enter.sh` | `docker exec` into the running container |
| `waypoints.csv` | Waypoint data copied into the image |
