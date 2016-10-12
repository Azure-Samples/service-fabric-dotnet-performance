---
services: service-fabric
platforms: dotnet
author: Abhishek Ram
---

# Service Load Test Sample
This sample shows clients sending requests to a service and measuring the throughput and latency of request processing. The current implementation of the sample supports the following types of services:
* Service Fabric reliable stateful service that uses a reliable dictionary
* Service Fabric reliable actor service

## Conceptual overview
### Major components
The sample consists of the following components:
* A target service that processes requests from load generator clients
* Load generator clients that send requests to the target service
* Test management client that controls the overall flow of the test

### Overall flow of the test
A test consists of the following sequence of events:
1. Test management client sends instructions to each of the load generator clients to start sending requests to the target service.

  ![Test sequence step1][1]

2. The load generator clients start sending requests to the target service and the latter processes the requests and sends the responses back.

  ![Test sequence step2][2]

3. As each load generator client finishes sending the total number of requests it was supposed to, it responds to the test management client with its results. After receiving responses from all the load generator clients, the test management client computes the aggregated results.

  ![Test sequence step3][3]

### Load generator service
The sample enables the load generator clients to be spread across multiple machines. This is useful is scenarios where we want to exercise more load on the service than a single machine can generate.

Spreading the clients across multiple machines is achieved using a Service Fabric cluster. The cluster has a load generator service deployed on it. The load generator service is a Service Fabric reliable stateless service. The instances of the stateless service are spread across the machines in the cluster. Each instance of this stateless service hosts a load generator client, therefore the load generator clients are also spread across the machines in the cluster. This is shown in the diagram below.

![Load generator on SF cluster][4]

### Target service
If the target service is a Service Fabric service, it can be deployed to the same Service Fabric cluster that the load generator service is deployed to. The concept of node types and placement constraints can be used to separate the machines hosting the load generator service from the machines hosting the target service.

Both of the target services supported by the current implementation of the sample are Service Fabric services and they are deployed to the same cluster that the load generator service is deployed to. The idea is shown in the diagram below.

![Target service on SF cluster][5]

A Service Fabric target service could also be deployed to a different Service Fabric cluster, but in this sample it is deployed to the same cluster for the convenience of having to manage just one cluster.

The sample could be extended to support other target services. For example, the Azure Storage Table service could be supported as a target service. If the sample were to be extended to support this, the architecture would be as shown in the diagram below.

![Azure Storage Table target service][6]

### Request sender plugins
Most of the load generation logic is not specific to any particular target service type. For example, the total number of requests to send to the target service, how many requests to send concurrently, how many clients to create, size of the data associated with each request etc. are all generic concepts that apply to all target service types. All of this generic logic is implemented in a library that is used for all target service types.

The exact mechanism of sending a request to a target service is specific to that target service. Therefore, it is implemented in a separate request sender plugin for that target service. The generic load generation library interacts with the specific request sender plugin in order to send requests to the target service. For example, the current implementation of the sample contains request sender plugins for the two target service types that are included in the sample - Service Fabric actors and Service Fabric reliable dictionary. If the sample were to be extended to support more target service types, new request sender plugins would need to be created for those. However, the generic load generation logic that is already implemented could be reused even for the new target service types.

![Request sender plugin][7]

## Projects in the sample
The following table describes the projects in the sample.

|Project name|Description|
|---|---|
|ServiceLoadTestClient|Test management client that controls the overall flow of the test|
|LoadDriverApplication, LoadDriverService and LoadDriverServiceInterface|Service Fabric reliable stateless service that hosts the load generation clients.
|LoadDriverLib|Library that implements the core logic for load generation clients. The implementation is target service agnostic, i.e. this library does not communicate with the target service directly. Instead, it interfaces with a request sender plugin that is specific to a target service in order to communicate with it.|
|RequestSenderInterface|Interface that must be implemented by a request sender plugin in order to enable the load generation client to communicate with the target service.|
|SFActorRequestSender|Request sender plugin that enables the load generation client to communicate with the target service that is based on Service Fabric reliable actors.|
|SFDictionaryRequestSender|Request sender plugin that enables the load generation client to communicate with the target service that uses a Service Fabric reliable dictionary.|
|ServiceFabricRequestSender|Helper library for request sender plugins that communicate with Service Fabric based services. The SFActorRequestSender and SFDictionaryRequestSender plugins use this helper library.|
|SFActorApplication, SFActor, SFActorInterface|Target service that is based on Service Fabric reliable actors.|
|SFDictionaryApplication, SFDictionaryService, SFDictionaryInterface|Target service that uses a Service Fabric reliable dictionary.|
|ServiceLoadTestUtilities|Library that implements some miscellaneous utilities.|
|SimulatorRequestSender|Mock request sender plugin that is only used for testing the load generation logic.|

## Running the sample
### Create the cluster
Documentation for creating a Service Fabric cluster via an ARM template is available [here](https://azure.microsoft.com/en-us/documentation/articles/service-fabric-cluster-creation-via-arm/). **Familiarity with this documentation is essential for performing the steps mentioned in this section.**

1. Open the file  "service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\ClusterSetup\Azure\ClusterParameters.json" and enter the values for the parameters. For example:

        {
            "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#",
            "contentVersion": "1.0.0.0",
            "parameters": {
                "clusterName": {
                    "value": "serviceloadtest"
                },
                "clusterLocation": {
                    "value": "West US 2"
                },
                "computeLocation": {
                    "value": "West US 2"
                },
                "vmStorageAccountName": {
                    "value": "serviceloadtest"
                },
                "dnsName": {
                    "value": "serviceloadtest"
                },
                "sourceVaultValue": {
                    "value": "/subscriptions/00000000-1111-2222-3333-444444444444/resourceGroups/mysecrets-keyvault/providers/Microsoft.KeyVault/vaults/mysecretsvault"
                },
                "certificateUrlValue": {
                    "value": "https://mysecretsvault.vault.azure.net:443/secrets/ServiceLoadTestClusterCert/aaaaaa0000000000bbbbbb1111111111"
                },
                "certificateThumbprint": {
                    "value": "0123456789ABCDEF0123456789ABCDEF01234567"
                },
                "clientCertificateThumbprint": {
                    "value": "0123456789ABCDEF0123456789ABCDEF01234567"
                }
            }
        }

2. Run the "service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\ClusterSetup\Azure\CreateCluster.ps1" PowerShell script to create the cluster. For example:

        .\CreateCluster.ps1 -SubscriptionId 00000000-1111-2222-3333-444444444444 -ResourceGroupName serviceloadtest -ResourceGroupLocation "West US 2" -TemplateFilePath .\ThreeNodeD2.json -ParameterFilePath .\ClusterParameters.json

    Follow the prompts on screen to sign in to your Azure account and to enter the administrator password for the VMs.

3. Wait for the cluster to get created

  Once created, the cluster will be visible in the portal as shown in the screenshot below.

  ![Cluster in portal][8]

  Note that the cluster has two node types LoadGen and LoadTgt for the load generation service and target service respectively.

  [Connect to the cluster via PowerShell](https://azure.microsoft.com/en-us/documentation/articles/service-fabric-connect-to-secure-cluster/).

        Connect-ServiceFabricCluster -ConnectionEndpoint serviceloadtest.westus2.cloudapp.azure.com:19000  -X509Credential -ServerCertThumbprint 0123456789ABCDEF0123456789ABCDEF01234567 -FindType FindByThumbprint -FindValue 0123456789ABCDEF0123456789ABCDEF01234567 -StoreLocation LocalMachine -StoreName My

  Verify that the cluster is healthy.

        PS F:\> Get-ServiceFabricClusterHealth

        AggregatedHealthState   : Ok
                :
                :
                :

### Create the target application
This section describes the steps for deploying the Service Fabric reliable dictionary based service as the target service. The steps for deploying the Service Fabric reliable actor based service are similar.

1. Open the file  "service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\Dictionary\SFDictionaryApplication\PublishProfiles\Cloud.xml" and enter the values for the cluster connection parameters. For example:

        <ClusterConnectionParameters ConnectionEndpoint="serviceloadtest.westus2.cloudapp.azure.com:19000"
                                     X509Credential="true"
                                     ServerCertThumbprint="0123456789ABCDEF0123456789ABCDEF01234567"
                                     FindType="FindByThumbprint"
                                     FindValue="0123456789ABCDEF0123456789ABCDEF01234567"
                                     StoreLocation="LocalMachine"
                                     StoreName="My" />

2. Build and package the application using Visual Studio.

  ![Build and package][9]

3. Run the PowerShell command "service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\Dictionary\SFDictionaryApplication\Scripts\Deploy-FabricApplication.ps1" to deploy the application.

        PS F:\gitsamp\service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\Dictionary\SFDictionaryApplication\Scripts> .\Deploy-FabricApplication.ps1 -PublishProfileFile ..\PublishProfiles\Cloud.xml -ApplicationPackagePath ..\pkg\Release

        Copying application to image store...
        Copy application package succeeded
        Registering application type...
        Register application type succeeded
        Removing application package from image store...
        Remove application package succeeded
        Creating application...

        ApplicationName        : fabric:/SFDictionaryApplication
        ApplicationTypeName    : SFDictionaryApplicationType
        ApplicationTypeVersion : 1.0.0
        ApplicationParameters  : { "SfDictionaryService_MinReplicaSetSize" = "2";
                                 "SfDictionaryService_TargetReplicaSetSize" = "3";
                                 "SfDictionaryService_PartitionCount" = "6" }

        Create application succeeded.

4. Verify that the application is healthy.

        PS F:\gitsamp\service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\Dictionary\SFDictionaryApplication\Scripts> Get-ServiceFabricApplicationHealth fabric:/SFDictionaryApplication

        ApplicationName                 : fabric:/SFDictionaryApplication
        AggregatedHealthState           : Ok
        ServiceHealthStates             :
                                          ServiceName           : fabric:/SFDictionaryApplication/SFDictionaryService
                                          AggregatedHealthState : Ok

        DeployedApplicationHealthStates :
                                          ApplicationName       : fabric:/SFDictionaryApplication
                                          NodeName              : _LoadTgt_0
                                          AggregatedHealthState : Ok

                                          ApplicationName       : fabric:/SFDictionaryApplication
                                          NodeName              : _LoadTgt_2
                                          AggregatedHealthState : Ok

                                          ApplicationName       : fabric:/SFDictionaryApplication
                                          NodeName              : _LoadTgt_1
                                          AggregatedHealthState : Ok

        HealthEvents                    :
                                          SourceId              : System.CM
                                          Property              : State
                                          HealthState           : Ok
                                          SequenceNumber        : 226
                                          SentAt                : 9/17/2016 4:01:10 PM
                                          ReceivedAt            : 9/17/2016 4:01:10 PM
                                          TTL                   : Infinite
                                          Description           : Application has been created.
                                          RemoveWhenExpired     : False
                                          IsExpired             : False
                                          Transitions           : Warning->Ok = 9/17/2016 4:01:10 PM, LastError = 1/1/0001 12:00:00 AM


### Create the load generator application
The steps for creating the load generator application are very similar to the steps for creating the target application. The steps are described in brief below. For more details, please see the section on creating the target application.

1. Open the file "service-fabric-dotnet-performance\ServiceLoadTest\Framework\LoadDriverApplication\PublishProfiles\Cloud.xml" and enter the values for the cluster connection parameters.
2. Build and package the application using Visual Studio.
3. Run the PowerShell command "service-fabric-dotnet-performance\ServiceLoadTest\Framework\LoadDriverApplication\Scripts\Deploy-FabricApplication.ps1" to deploy the application.
4. Verify that the application is healthy.

### Run the load test
The load test is run by launching the test management client executable. This is a .NET executable that is accompanied by an app.config file that contains default values for customizable settings. If needed, the default values can be overridden via command-line arguments when launching the executable.

In the sample, the test management client application is implemented by the ServiceLoadTestClient project in the solution.

1. Open the file "service-fabric-dotnet-performance\ServiceLoadTest\Framework\TestClient\App.config" and enter the values for the settings. For example:

        <appSettings>
          <!--Address of the cluster. e.g. mycluster.westus.cloudapp.azure.com -->
          <add key="ClusterAddress" value="serviceloadtest.westus2.cloudapp.azure.com"/>

          <!--Endpoint used by clients to connect to the cluster to perform management and query operations.-->
          <add key="ClientConnectionEndpoint" value="19000"/>

          <!--Endpoint used by clients to connect to the reverse proxy.-->
          <add key="ReverseProxyEndpoint" value="19008"/>

          <!--Thumbprint of server certificate-->
          <add key="ServerCertificateThumbprint" value="0123456789ABCDEF0123456789ABCDEF01234567"/>

          <!--Thumbprint of client certificate-->
          <add key="ClientCertificateThumbprint" value="0123456789ABCDEF0123456789ABCDEF01234567"/>

          <!--Total number of write operations to be performed on the service.-->
          <add key="NumWriteOperationsTotal" value="262144"/>

          <!--Number of write operations sent to the service at any given time.-->
          <add key="NumOutstandingWriteOperations" value="64"/>

          <!--Total number of read operations to be performed on the service.-->
          <add key="NumReadOperationsTotal" value="524288"/>

          <!--Number of read operations sent to the service at any given time.-->
          <add key="NumOutstandingReadOperations" value="16"/>

          <!--Size in bytes of the data associated with each operation (i.e. read or write) performed on the service.-->
          <add key="OperationDataSizeInBytes" value="1024"/>

          <!--Number of items (e.g. number of rows in a table) that the operations are distributed across in the service.-->
          <add key="NumItems" value="2048"/>

          <!--Number of clients used to perform the operations on the service.-->
          <add key="NumClients" value="1"/>

          <!--Target service type on which the operations should be performed. Supported values are the values of the TargetService.Types enumeration defined in TargetService.cs-->
          <add key="TargetServiceType" value="SfDictionary"/>
        </appSettings>

  _Notes_
  * The value of [ReverseProxyEndpoint](https://azure.microsoft.com/en-us/documentation/articles/service-fabric-reverseproxy/) can be found in the ARM template file that was used to create the Service Fabric cluster. For example, in the ARM template file "service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\ClusterSetup\Azure\ClusterParameters.json", it is the value of the "nt0sfReverseProxyPort" and "nt1sfReverseProxyPort" parameters, which are both defined as 19008 (one parameter for each node type).
  * The value of TargetServiceType is defined as SfDictionary, which corresponds to the target service that is a Service Fabric reliable dictionary based service.
2. Build and run the application. When the test finishes running, the results are printed on the console.

        PS F:\gitsamp\service-fabric-dotnet-performance\ServiceLoadTest\Framework\TestClient\bin\x64\Release> .\ServiceLoadTestClient.exe

<!--Image references-->
[1]: ./media/TestSequenceStep1.png
[2]: ./media/TestSequenceStep2.png
[3]: ./media/TestSequenceStep3.png
[4]: ./media/LoadGeneratorOnSFCluster.png
[5]: ./media/TargetServiceOnSFCluster.png
[6]: ./media/AzureTableTargetService.png
[7]: ./media/RequestSenderPlugin.png
[8]: ./media/ClusterPortal.png
[9]: ./media/BuildAndPackage.png
