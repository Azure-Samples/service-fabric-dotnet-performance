#!/bin/bash

sfctl application delete --application-id ContainerDemoApp
sfctl application unprovision --application-type-name ContainerDemoAppType --application-type-version 1.0.0
sfctl store delete --content-path ContainerDemoAppPkg

# azure servicefabric application delete fabric:/ContainerDemoApplication
# azure servicefabric application type unregister ContainerDemoAppType 1.0.0
