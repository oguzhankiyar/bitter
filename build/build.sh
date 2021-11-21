#!/bin/bash

dotnet clean $APP_SOLUTIONFILE -c $APP_CONFIGURATION

find ./ -type d -name 'bin' -exec rm -r {} +
find ./ -type d -name 'obj' -exec rm -r {} +

dotnet restore $APP_SOLUTIONFILE

dotnet build $APP_SOLUTIONFILE -c $APP_CONFIGURATION /p:Version=$APP_VERSION

dotnet test $APP_SOLUTIONFILE -c $APP_CONFIGURATION