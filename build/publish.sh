#!/bin/bash

echo $DOCKER_KEY | docker login -u $DOCKER_USERNAME --password-stdin $DOCKER_SOURCE
docker-compose pull || true
docker-compose build
docker-compose push