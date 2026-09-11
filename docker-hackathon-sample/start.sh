#!/bin/sh
# Start a container with GUI support via rocker (X11, CPU)
rocker --name opera-hackathon-sample \
       --x11 \
       --network host \
       --user --home \
       opera-hackathon-sample
