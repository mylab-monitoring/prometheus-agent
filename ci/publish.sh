#!/bin/sh

docker build --push --platform linux/amd64 -f ./Dockerfile -t ghcr.io/mylab-monitoring/prometheus-agent:latest -t ghcr.io/mylab-monitoring/prometheus-agent:$1 ../src