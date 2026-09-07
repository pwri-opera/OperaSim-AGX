###############################################################################
# Dockerfile for Opera-Sim hackathon Sample Code
# https://github.com/pwri-opera/Opera-hackathon_SampleCode
#
# Base image:  osrf/ros:humble-desktop-full  (Ubuntu 22.04 + ROS 2 Humble)
#
# Build:
#   ./build.sh
#
# Run (with GUI via rocker):
#   ./start.sh           # CPU / X11
#   ./start-nvidia.sh    # NVIDIA GPU / X11
#
# Inside the container:
#   source /ws/install/setup.bash
#   # Terminal 1:
#   ros2 launch ros_tcp_endpoint endpoint.py
#   # Terminal 2:
#   ros2 launch task_manager hackathon_sample.launch.py
###############################################################################

FROM osrf/ros:humble-desktop-full

SHELL ["/bin/bash", "-c"]

ENV DEBIAN_FRONTEND noninteractive

# ---------------------------------------------------------------------------
# Install ROS 2 packages and system dependencies
# ---------------------------------------------------------------------------
RUN apt-get update && \
    apt-get upgrade -y && \
    apt-get install -y --no-install-recommends \
        git \
        vim \
        python-is-python3 \
        python3-colcon-common-extensions \
        python3-rosdep2 \
        ros-humble-rosbridge-suite \
        ros-humble-tf-transformations \
        ros-humble-navigation2 \
        ros-humble-nav2-bringup \
        ros-humble-robot-localization \
        ros-humble-joint-state-publisher \
        ros-humble-joint-trajectory-controller \
        ros-humble-joint-state-broadcaster \
        ros-humble-controller-manager \
        ros-humble-moveit \
        ros-humble-moveit-planners-ompl \
        ros-humble-moveit-planners-chomp \
        ros-humble-moveit-visual-tools \
        ros-humble-diagnostic-updater \
        libboost-all-dev \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# ---------------------------------------------------------------------------
# Create workspace and clone all required repositories
#   (branches per the hackathon Sample Code README)
# ---------------------------------------------------------------------------
RUN mkdir -p /ws/src

WORKDIR /ws/src

RUN git clone -b main-ros2 https://github.com/Unity-Technologies/ROS-TCP-Endpoint.git && \
    git clone -b feature/tmp_ic120_unity_setting https://github.com/pwri-opera/ic120_ros2.git && \
    git clone -b main https://github.com/Ryoya1012/Opera-sim_hackathon-Sample.git && \
    git clone -b hackathon https://github.com/Ryoya1012/ZX200_autonomy_state_machine.git && \
    git clone -b main https://github.com/Ryoya1012/Opera-sim_ic120_hackathon.git && \
    git clone -b main https://github.com/pwri-opera/com3_ros.git && \
    git clone -b main https://github.com/pwri-opera/gnss_localizer_ros2.git

# ---------------------------------------------------------------------------
# Copy waypoints.csv into the workspace root (as required by the README)
# ---------------------------------------------------------------------------
COPY waypoints.csv /ws/waypoints.csv

# ---------------------------------------------------------------------------
# Build all packages
#   --packages-up-to pulls in every transitive dependency automatically
# ---------------------------------------------------------------------------
WORKDIR /ws

RUN source /opt/ros/$ROS_DISTRO/setup.bash && \
    colcon build --symlink-install \
        --packages-up-to \
            ic120_navigation \
            task_manager \
            zx200_autonomy \
            ic120_autonomy \
            ic120_unity \
            com3_msgs \
            ros_tcp_endpoint

# ---------------------------------------------------------------------------
# Convenience: source the overlay automatically in every new shell
# ---------------------------------------------------------------------------
RUN echo "source /ws/install/setup.bash" >> /etc/bash.bashrc

WORKDIR /ws
