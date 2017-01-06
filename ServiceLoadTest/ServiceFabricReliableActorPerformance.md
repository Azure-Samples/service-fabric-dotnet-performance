---
services: service-fabric
platforms: dotnet
author: Abhishek Ram
---

# Performance testing for Service Fabric reliable actors
This article describes how to use the [service load test sample](README.md) to test the performance of a reliable actors service. Readers can perform the experiments described in this article to get a basic understanding of reliable actors performance. They can then use that knowledge to further tweak the tests to measure performance for the configurations that they are interested in.

# Get a baseline: Test on three D2_v2 nodes
This section describes experiments performed on a cluster where three D2_v2 nodes are dedicated to the reliable actors based service. The results from these experiments will be considered a baseline. In subsequent sections we will repeat the experiments on clusters with more hardware resources dedicated to the service and see how the results are impacted.

## <a name="d2-3-create-the-cluster"></a>Create the cluster
The process of setting up the cluster for the service load test sample is described in detail in the [readme file for that sample](README.md#running-the-sample). The steps are summarized below:

1. Open the file  "service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\ClusterSetup\Azure\ClusterParameters.json" and enter the values for the parameters.
2. Run the "service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\ClusterSetup\Azure\CreateCluster.ps1" PowerShell script to create the cluster. The script takes a _TemplateFilePath_ parameter for which the template file ThreeNodeD2.json should be specified. This template file is in the folder "service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\ClusterSetup\Azure".
3. Wait for the cluster to get created and query the health of the cluster to make sure it is healthy.

## <a name="d2-3-create-the-load-generator-application"></a>Create the load generator application
### Determine the partition count
Each load generator client is hosted inside a partition of the load generator service. Therefore, the partition count of the load generator service must be equal to the number of load generation clients. In the current example, [the cluster that was deployed](#d2-3-create-the-cluster) has three nodes (node type LoadGen) dedicated for load generation clients. Therefore the load generator service is created with three partitions.

The partition count for the load generator service can be changed by changing the application parameters configuration file for the load generator service: "service-fabric-dotnet-performance\ServiceLoadTest\Framework\LoadDriverApplication\ApplicationParameters\Cloud.xml".

### Create the application
The process of creating the load generator application is described in the [readme file for the service load test sample](README.md#create-the-load-generator-application).

## <a name="d2-3-create-the-actor-application"></a>Create the actor application
### Be aware of the state provider used in the sample application
The sample actor application that is included with the service load test sample uses the [persisted state provider](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reliable-actors-state-management) for storing the actors' state. *Use of a different state provider will yield different performance test results, hence it is very important to be aware of the above choice.*

### Determine the partition count
In the current example, [the cluster that was deployed](#d2-3-create-the-cluster) has three nodes (node type LoadTgt) dedicated for actor service. Therefore the service should be created with at least three partitions, so that it can make optimal use of all the hardware available to it. Service Fabric will distribute those partitions uniformly across the three nodes.

It is acceptable to have more partitions than the number of nodes. In fact, it is recommended for scenarios where the cluster might need to be expanded in the future by adding more nodes to accommodate an increased workload. As more nodes get added, Service Fabric will automatically relocate existing partitions to maintain a uniform distribution of partitions across nodes. This is how the service scales out to utilize the additional nodes.

For example, consider a service is initially deployed on 3 nodes, but the node count is eventually expected to go up to 12 as the workload increases. In this case, the service could be deployed with 12 partitions. Initially, those 12 partitions would be packed into the 3 available nodes with 4 primary replicas on each node. As nodes are added in the future, Service Fabric would automatically redistribute the partitions across those nodes. For example, if the node count was increased to 12, then Service Fabric would place 1 primary replica on each node. On the other hand, if the service had initially been deployed with only 3 partitions to begin with, then it would have only 3 primary replicas. This means even if nodes were added to the cluster in future, there would be no more primary replicas left to place on the new nodes. This will likely result in the new nodes being under-utilized. Hence it is important to take future needs into account when determining the partition count for a service.

The partition count for the actor service can be changed by changing the application parameters configuration file: "service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\Actor\SFActorApplication\ApplicationParameters\Cloud.xml". The default value is 6, which is fine for the current test where we have 3 nodes dedicated for the actor service. Each node will have 2 primary replicas placed on it.

### Create the application
The process of creating the actor application is similar to the process of creating a reliable dictionary based application that is described in the [readme file for the service load test sample](README.md#create-the-target-application). The steps are summarized below:

1. Open the file "service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\Actor\SFActorApplication\PublishProfiles\Cloud.xml" and enter the values for the cluster connection parameters.
2. Build and package the application using Visual Studio.
3. Run the PowerShell command "service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\Actor\SFActorApplication\Scripts\Deploy-FabricApplication.ps1" to deploy the application.
4. Verify that the application is healthy.

## <a name="d2-3-run-the-load-test"></a>Run the load test
As mentioned in the [readme file for the service load test sample](README.md#run-the-load-test), we exercise load on the service by launching the test management client executable.

Before running this executable, it is convenient to update its App.config to specify some parameters that will not change during the course of our experiments.
* The _ClusterAddress_, _ClientConnectionEndpoint_, _ReverseProxyEndpoint_, _ServerCertificateThumbprint_, _ClientCertificateThumbprint_ parameters are all cluster specific so they will not change as long as the cluster on which we are running our experiments remains the same.
* The value of the _TargetServiceType_ parameter should be _SfActor_ because the experiments in the current article pertain to a reliable actors based service.
The parameters listed above can be set in the App.config file instead of being specified every time via the command line. For the rest of the parameters, it may be more convenient to specify via the command line because we are likely to tweak them as we perform our experiments.

Listed below are some example combinations of arguments that the test management client executable can be launched with.

    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:30000 /NumOutstandingWriteOperations:24 /NumReadOperationsTotal:150000 /NumOutstandingReadOperations:12 /NumItems:9000 /NumClients:3
    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:60000 /NumOutstandingWriteOperations:48 /NumReadOperationsTotal:300000 /NumOutstandingReadOperations:24 /NumItems:9000 /NumClients:3
    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:120000 /NumOutstandingWriteOperations:96 /NumReadOperationsTotal:600000 /NumOutstandingReadOperations:48 /NumItems:9000 /NumClients:3
    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:240000 /NumOutstandingWriteOperations:192 /NumReadOperationsTotal:1200000 /NumOutstandingReadOperations:96 /NumItems:9000 /NumClients:3
    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:480000 /NumOutstandingWriteOperations:384 /NumReadOperationsTotal:2400000 /NumOutstandingReadOperations:192 /NumItems:9000 /NumClients:3

_Notes_
* The _NumClients_ parameter specifies the number of load generator clients used to send to request workload to the target service. Recall that in the current example, [the cluster that was deployed](#d2-3-create-the-cluster) has three nodes (node type LoadGen) dedicated for load generation clients and the load generator service was [created with three partitions](#d2-3-create-the-load-generator-application). Therefore, the value of the _NumClients_ parameter is specified as 3 in the examples above.
* When the target service is an actor based service (as it is in our current experiment), the _NumItems_ parameter refers to the total number of actors that are targeted by the clients. In the examples shown above the clients will collectively target (i.e. send requests to) 9000 actors in the actor-based service.
* The _OperationDataSizeInBytes_ refers to the size of the data associated with the read or write requests sent to the service. In the examples shown above, each 128 bytes of data will be written or read for each request processed by the service.
* The volume of requests sent to the target service is controlled via the _NumOutstandingWriteOperations_ and _NumOutstandingReadOperations_ parameters for write and read operations respectively. Those parameters define the number of operations that the clients submit concurrently to the service at any given time. Higher the number, more is the request volume that the service has to process. In the sequence of example commands above, the request volume steadily increases - for writes the _NumOutstandingWriteOperations_ parameter goes from 24 to 384, and for reads the _NumOutstandingReadOperations_ goes from 12 to 192.

### Expected results for the load test
As mentioned above, each command in the example sequence above exercises a higher load on the service than the previous command. Therefore, it is expected that the test will follow the trends described in the [readme file for the service load test sample](README.md#expected-performance-trends). The throughput numbers yielded by the example commands above are expected to rise sharply initially and then gradually flatten once the saturation point is reached. The latency numbers yielded by the example commands above are expected to remain mostly flat or rise gently initially and then rise sharply once the saturation point is reached.

# Observe the impact of additional hardware resources
In this section we repeat the experiments from the previous section on a cluster with more hardware resources dedicated to the reliable actors based service and compare the results with the baseline results obtained in the previous section.

## Use more machines: Test on six D2_v2 nodes
In this section we perform experiments on a cluster with six D2_v2 nodes dedicated to the reliable actors based service. Thus, the reliable actors service has twice as many machines (of the same type) when compared to the baseline experiments that were performed with three D2_v2 nodes.

### <a name="d2-6-create-the-cluster"></a>Create the cluster
The cluster is set up in a manner similar to the baseline experiment described above with three D2_v2 nodes. The only difference is that when running the "service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\ClusterSetup\Azure\CreateCluster.ps1" PowerShell script, the template file SixNodeD2.json should be specified.

### <a name="d2-6-create-the-load-generator-application"></a>Create the load generator application
The load generator application is created in a manner similar to the baseline experiment described above with three D2_v2 nodes. One difference is that the cluster has six nodes (node type LoadGen) dedicated for load generation clients. Therefore the load generator service should be created with six partitions, so that the load generation clients can be spread out over the six available nodes and can make optimum use of all the hardware available to them. This can be done by modifying the partition count for the load generator service from "3" to "6" in the application parameters configuration file: "service-fabric-dotnet-performance\ServiceLoadTest\Framework\LoadDriverApplication\ApplicationParameters\Cloud.xml".

### <a name="d2-6-create-the-actor-application"></a>Create the actor application
The actor application is created in a manner similar to the baseline experiment described above with three D2_v2 nodes. The default partition count of 6 works fine in the current cluster which has six nodes (node type LoadTgt) dedicated for the actor service. Each node will have 1 primary replica placed on it.

### <a name="d2-6-run-the-load-test"></a>Run the load test
The load test is run in a manner similar to the baseline experiment described above with three D2_v2 nodes. Listed below are some example combinations of arguments that the test management client executable can be launched with.

    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:60000 /NumOutstandingWriteOperations:48 /NumReadOperationsTotal:300000 /NumOutstandingReadOperations:24 /NumItems:9000 /NumClients:6
    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:120000 /NumOutstandingWriteOperations:96 /NumReadOperationsTotal:600000 /NumOutstandingReadOperations:48 /NumItems:9000 /NumClients:6
    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:240000 /NumOutstandingWriteOperations:192 /NumReadOperationsTotal:1200000 /NumOutstandingReadOperations:96 /NumItems:9000 /NumClients:6
    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:480000 /NumOutstandingWriteOperations:384 /NumReadOperationsTotal:2400000 /NumOutstandingReadOperations:192 /NumItems:9000 /NumClients:6
    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:960000 /NumOutstandingWriteOperations:768 /NumReadOperationsTotal:4800000 /NumOutstandingReadOperations:384 /NumItems:9000 /NumClients:6

_Notes_
* The number of load generator clients (specified by the _NumClients_ parameter) is set to 6 (as opposed to 3 in the baseline experiment) to match the number of nodes in the cluster dedicated to load generation clients.
* The volume of requests sent to the actor service (controlled via the _NumOutstandingWriteOperations_ and _NumOutstandingReadOperations_ parameters) is twice the volume that was sent in the baseline experiment. This is because the actor service has twice as much hardware at its disposal in the current experiment (six D2_v2 nodes) as it had in the baseline experiment (three D2_v2 nodes).

#### Expected results for the load test
Due to the additional hardware available to the actor service, it is expected that:
* The service will reach saturation at a throughput that is double the throughput at which the service in the baseline experiment was saturated.
* The request latencies will be similar to the request latencies observed in the baseline experiment, even though the volume of requests being pushed to the service is twice the volume that was pushed in the baseline experiment.

## Use more powerful machines: Test on three D3_v2 nodes
In this section we perform experiments on a cluster with three D3_v2 nodes dedicated to the reliable actors based service.  The [D3_v2 machines have twice as much hardware (processors, memory, disk) when compared to the D2_v2 machines](https://docs.microsoft.com/en-us/azure/virtual-machines/virtual-machines-windows-sizes) that were used in the baseline experiments.

### <a name="d3-3-create-the-cluster"></a>Create the cluster
The cluster is set up in a manner similar to the baseline experiment described above with three D2_v2 nodes. The only difference is that when running the "service-fabric-dotnet-performance\ServiceLoadTest\ServiceFabric\ClusterSetup\Azure\CreateCluster.ps1" PowerShell script, the template file ThreeNodeD3.json should be specified.

### <a name="d3-3-create-the-load-generator-application"></a>Create the load generator application
The load generator application is created in a manner similar to the baseline experiment described above with three D2_v2 nodes.

### <a name="d3-3-create-the-actor-application"></a>Create the actor application
The actor application is created in a manner similar to the baseline experiment described above with three D2_v2 nodes.

### <a name="d3-3-run-the-load-test"></a>Run the load test
The load test is run in a manner similar to the baseline experiment described above with three D2_v2 nodes. Listed below are some example combinations of arguments that the test management client executable can be launched with.

    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:60000 /NumOutstandingWriteOperations:48 /NumReadOperationsTotal:300000 /NumOutstandingReadOperations:24 /NumItems:9000 /NumClients:3
    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:120000 /NumOutstandingWriteOperations:96 /NumReadOperationsTotal:600000 /NumOutstandingReadOperations:48 /NumItems:9000 /NumClients:3
    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:240000 /NumOutstandingWriteOperations:192 /NumReadOperationsTotal:1200000 /NumOutstandingReadOperations:96 /NumItems:9000 /NumClients:3
    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:480000 /NumOutstandingWriteOperations:384 /NumReadOperationsTotal:2400000 /NumOutstandingReadOperations:192 /NumItems:9000 /NumClients:3
    ServiceLoadTestClient.exe /OperationDataSizeInBytes:128 /NumWriteOperationsTotal:960000 /NumOutstandingWriteOperations:768 /NumReadOperationsTotal:4800000 /NumOutstandingReadOperations:384 /NumItems:9000 /NumClients:3

_Note:_ The volume of requests sent to the actor service (controlled via the _NumOutstandingWriteOperations_ and _NumOutstandingReadOperations_ parameters) is twice the volume that was sent in the baseline experiment. This is because the actor service has twice as much hardware at its disposal in the current experiment (three D3_v2 nodes) as it had in the baseline experiment (three D2_v2 nodes).

#### Expected results for the load test
Due to the additional hardware available to the actor service, it is expected that:
* The service will reach saturation at a throughput that is double the throughput at which the service in the baseline experiment was saturated.
* The request latencies will be similar to the request latencies observed in the baseline experiment, even though the volume of requests being pushed to the service is twice the volume that was pushed in the baseline experiment.
