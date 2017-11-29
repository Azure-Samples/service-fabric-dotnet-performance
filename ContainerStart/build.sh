#!/bin/bash

CurrentDir=`dirname $0`
ContainerDemoAppPkgDir=$CurrentDir/ContainerDemoAppPkg
ContainerAggregatorServicePkgDir=$ContainerDemoAppPkgDir/ContainerAggregatorPkg
WebFrontServicePkgDir=$ContainerDemoAppPkgDir/WebFrontPkg

# Create Applciation package structure.
mkdir -p $ContainerAggregatorServicePkgDir/Code
mkdir -p $ContainerAggregatorServicePkgDir/Config
mkdir -p $WebFrontServicePkgDir/Code
mkdir -p $WebFrontServicePkgDir/Config
cp $CurrentDir/ContainerDemoApp/ApplicationPackageRoot/ApplicationManifest.xml $ContainerDemoAppPkgDir/ApplicationManifest.xml -f
cp $CurrentDir/ContainerAggregator/PackageRoot/ServiceManifest_Linux.xml $ContainerAggregatorServicePkgDir/ServiceManifest.xml -f
cp $CurrentDir/ContainerAggregator/PackageRoot/Config/Settings.xml $ContainerAggregatorServicePkgDir/Config/Settings.xml -f
cp $CurrentDir/WebFront/PackageRoot/ServiceManifest_Linux.xml $WebFrontServicePkgDir/ServiceManifest.xml -f
cp $CurrentDir/WebFront/PackageRoot/Config/Settings.xml $WebFrontServicePkgDir/Config/Settings.xml -f

# Copy the entrypoint for code packages.
cp $CurrentDir/ContainerAggregator/entryPoint.sh $ContainerAggregatorServicePkgDir/Code/entryPoint.sh -f
cp $CurrentDir/WebFront/entryPoint.sh $WebFrontServicePkgDir/Code/entryPoint.sh -f


cd Microsoft.ServiceFabric.Samples.Utility
dotnet restore -s /opt/microsoft/sdk/servicefabric/csharp/packages -s https://api.nuget.org/v3/index.json
dotnet build 
cd -

cd ContainerAggregator.Interfaces
dotnet restore -s /opt/microsoft/sdk/servicefabric/csharp/packages -s https://api.nuget.org/v3/index.json
dotnet build 
cd -

cd ContainerAggregator
dotnet restore -s /opt/microsoft/sdk/servicefabric/csharp/packages -s https://api.nuget.org/v3/index.json
dotnet build 
dotnet publish -o ../ContainerDemoAppPkg/ContainerAggregatorPkg/Code/

cd -

cd WebFront
dotnet restore -s /opt/microsoft/sdk/servicefabric/csharp/packages -s https://api.nuget.org/v3/index.json
dotnet build 
dotnet publish -o ../ContainerDemoAppPkg/WebFrontPkg/Code/
cd -

