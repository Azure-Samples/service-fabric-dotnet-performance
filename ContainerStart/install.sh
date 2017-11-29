#!/bin/bash

CurrentDir=`dirname $0`
appPkg=ContainerDemoAppPkg

sfctl application upload --path $appPkg
sfctl application provision --application-type-build-path ContainerDemoAppPkg
sfctl application create --app-name fabric:/ContainerDemoApp --app-type ContainerDemoAppType --app-version 1.0.0

# azure servicefabric application package copy ContainerDemoAppPkg fabric:ImageStore
# azure servicefabric application type register ContainerDemoAppPkg
# azure servicefabric application create fabric:/ContainerDemoApplication ContainerDemoAppType 1.0.0
