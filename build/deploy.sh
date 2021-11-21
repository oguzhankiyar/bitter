#!/bin/bash

echo $DOCKER_KEY | docker login -u $DOCKER_USERNAME --password-stdin $DOCKER_SOURCE
docker-compose stop
docker-compose down
docker-compose pull
docker-compose up -d