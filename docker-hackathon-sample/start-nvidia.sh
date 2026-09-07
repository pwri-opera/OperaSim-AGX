#!/bin/sh
# Start a container with GUI support via rocker (NVIDIA GPU acceleration)
# Requires nvidia-container-toolkit installed on the host.
rocker --name opera-hackathon-sample \
       --x11 --nvidia \
       --network host \
       --user --home \
       opera-hackathon-sample
