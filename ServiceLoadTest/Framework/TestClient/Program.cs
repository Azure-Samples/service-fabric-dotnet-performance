// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace ServiceLoadTestClient
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Fabric;
    using System.Fabric.Query;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel;
    using System.Threading.Tasks;
    using LoadDriverLib;
    using LoadDriverServiceInterface;
    using ServiceLoadTestUtilities;

    internal class Program
    {
        private const string StandardServiceNamePrefix = "fabric:/";
        private const string ReverseProxyUriTemplate = "http://{0}{1}?PartitionKey={2}&PartitionKind=Int64Range&Timeout={3}";
        private const int TimeoutInSeconds = 3600;
        private static readonly Uri LoadDriverServiceUri = new Uri("fabric:/LoadDriverApplication/LoadDriverService");

        private static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            // Read the parameters from the configuration file and command line
            Parameters parameters = new Parameters();
            parameters.ReadFromConfigFile();
            parameters.OverrideFromCommandLine(args);

            // Create the test specifications for each client
            int numClients = (int) parameters.ParameterValues[Parameters.Id.NumClients];
            TestSpecifications[] testSpecifications = CreateTestSpecifications(parameters, numClients);

            // Wait until the load driver service (that hosts the clients) is ready
            X509Credentials credentials = new X509Credentials()
            {
                StoreLocation = StoreLocation.LocalMachine,
                StoreName = new X509Store(StoreName.My, StoreLocation.LocalMachine).Name,
                FindType = X509FindType.FindByThumbprint,
                FindValue = (string) parameters.ParameterValues[Parameters.Id.ClientCertificateThumbprint],
            };
            credentials.RemoteCertThumbprints.Add(
                (string) parameters.ParameterValues[Parameters.Id.ServerCertificateThumbprint]);

            FabricClient fabricClient = new FabricClient(
                credentials,
                new FabricClientSettings(),
                GetEndpointAddress(
                    (string) parameters.ParameterValues[Parameters.Id.ClusterAddress],
                    (int) parameters.ParameterValues[Parameters.Id.ClientConnectionPort]));
            ServicePartitionList partitionList = await AwaitPartitionReadyOperation.PerformAsync(
                fabricClient,
                LoadDriverServiceUri);

            // Verify that the load driver service has at least as many partitions as the number of
            // clients that we need to create.
            if (partitionList.Count < numClients)
            {
                string message = String.Format(
                    "The value for parameter '{0}' ({1}) should not be greater than the number of partitions ({2}) of the '{3}' service.",
                    Parameters.ParameterNames.Single(kvp => (kvp.Value == Parameters.Id.NumClients)).Key,
                    numClients,
                    partitionList.Count,
                    LoadDriverServiceUri.AbsoluteUri);
                throw new ConfigurationErrorsException(message);
            }

            // Get the interfaces for each instance of the load driver service.
            ILoadDriver[] loadDrivers = CreateLoadDrivers(
                GetEndpointAddress(
                    (string) parameters.ParameterValues[Parameters.Id.ClusterAddress],
                    (int) parameters.ParameterValues[Parameters.Id.ReverseProxyPort]),
                partitionList);

            // Create and initialize the clients inside the load driver service.
            Task[] initializationTasks = new Task[numClients];
            for (int i = 0; i < numClients; i++)
            {
                initializationTasks[i] = loadDrivers[i].InitializeAsync(testSpecifications[i]);
            }
            await Task.WhenAll(initializationTasks);

            // Run the tests
            TestResults writeTestResults = await RunTestAsync(
                numClients,
                loadDrivers,
                ld => ld.RunWriteTestAsync());
            TestResults readTestResults = await RunTestAsync(
                numClients,
                loadDrivers,
                ld => ld.RunReadTestAsync());

            // Display the results
            Console.WriteLine("Write test results - {0}", writeTestResults);
            Console.WriteLine("Read test results - {0}", readTestResults);
        }

        private static async Task<TestResults> RunTestAsync(
            int numClients,
            ILoadDriver[] loadDrivers,
            Func<ILoadDriver, Task<TestResults>> runTestOnSingleDriverInstance)
        {
            Task<TestResults>[] testTasks = new Task<TestResults>[numClients];

            // Trigger the test run for each of the clients and wait for them all
            // to finish. Also measure how to long it took for all of them to
            // finish running their tests.
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < numClients; i++)
            {
                testTasks[i] = runTestOnSingleDriverInstance(loadDrivers[i]);
            }
            await Task.WhenAll(testTasks);
            stopwatch.Stop();

            // Merge the raw results from all of the clients
            TestResults results = testTasks[0].Result;
            for (int i = 1; i < numClients; i++)
            {
                results = TestResults.Combine(results, testTasks[i].Result);
            }

            // Compute averages based on the raw results from the clients.
            results.ComputeAverages(stopwatch.Elapsed);
            return results;
        }

        private static ILoadDriver[] CreateLoadDrivers(string reverseProxyAddress, ServicePartitionList partitionList)
        {
            ILoadDriver[] loadDrivers = new ILoadDriver[partitionList.Count];
            int i = 0;
            foreach (Partition partition in partitionList)
            {
                // We use the reverse proxy to communicate with each partition of the
                // load driver service (which hosts the clients).
                string uri = GetUriExposedByReverseProxy(
                    LoadDriverServiceUri.AbsoluteUri,
                    partition,
                    reverseProxyAddress);
                loadDrivers[i] = ChannelFactory<ILoadDriver>.CreateChannel(
                    BindingUtility.CreateHttpBinding(),
                    new EndpointAddress(uri));
                i++;
            }
            return loadDrivers;
        }

        private static string GetUriExposedByReverseProxy(string serviceName, Partition partition, string reverseProxyAddress)
        {
            string serviceNameSuffix = serviceName.Remove(0, StandardServiceNamePrefix.Length);
            return String.Format(
                ReverseProxyUriTemplate,
                String.Concat(
                    reverseProxyAddress,
                    reverseProxyAddress.EndsWith("/") ? String.Empty : "/"),
                serviceNameSuffix,
                ((Int64RangePartitionInformation) partition.PartitionInformation).LowKey,
                TimeoutInSeconds.ToString("D"));
        }

        private static string GetEndpointAddress(string clusterAddress, int port)
        {
            return String.Format("{0}:{1}", clusterAddress, port);
        }

        private static TestSpecifications[] CreateTestSpecifications(Parameters parameters, int numClients)
        {
            TargetService.Description targetServiceType =
                TargetService.SupportedTypes[(TargetService.Types) parameters.ParameterValues[Parameters.Id.TargetServiceType]];
            TestSpecifications[] testSpecifications = new TestSpecifications[numClients];

            // Distribute the total work among the clients
            int numWriteOperationsTotal = (int) parameters.ParameterValues[Parameters.Id.NumWriteOperationsTotal];
            int numWriteOperationsPerClient = numWriteOperationsTotal/numClients;
            int numWriteOperationsRemainder = numWriteOperationsTotal%numClients;

            int numOutstandingWriteOperations = (int) parameters.ParameterValues[Parameters.Id.NumOutstandingWriteOperations];
            int numOutstandingWriteOperationsPerClient = numOutstandingWriteOperations/numClients;
            int numOutstandingWriteOperationsRemainder = numOutstandingWriteOperations%numClients;

            int numReadOperationsTotal = (int) parameters.ParameterValues[Parameters.Id.NumReadOperationsTotal];
            int numReadOperationsPerClient = numReadOperationsTotal/numClients;
            int numReadOperationsRemainder = numReadOperationsTotal%numClients;

            int numOutstandingReadOperations = (int) parameters.ParameterValues[Parameters.Id.NumOutstandingReadOperations];
            int numOutstandingReadOperationsPerClient = numOutstandingReadOperations/numClients;
            int numOutstandingReadOperationsRemainder = numOutstandingReadOperations%numClients;

            int numItems = (int) parameters.ParameterValues[Parameters.Id.NumItems];
            int numItemsPerClient = numItems/numClients;
            int numItemsRemainder = numItems%numClients;

            for (int i = 0; i < numClients; i++)
            {
                // Create test specification for client
                testSpecifications[i] = new TestSpecifications()
                {
                    NumWriteOperationsTotal = numWriteOperationsPerClient,
                    NumOutstandingWriteOperations = numOutstandingWriteOperationsPerClient,
                    NumReadOperationsTotal = numReadOperationsPerClient,
                    NumOutstandingReadOperations = numOutstandingReadOperationsPerClient,
                    OperationDataSizeInBytes = (int) parameters.ParameterValues[Parameters.Id.OperationDataSizeInBytes],
                    NumItems = numItemsPerClient,
                    RequestSenderAssemblyName = targetServiceType.AssemblyName,
                    RequestSenderTypeName = targetServiceType.TypeName
                };

                if (i < numWriteOperationsRemainder)
                {
                    (testSpecifications[i].NumWriteOperationsTotal)++;
                }
                if (i < numOutstandingWriteOperationsRemainder)
                {
                    (testSpecifications[i].NumOutstandingWriteOperations)++;
                }
                if (i < numReadOperationsRemainder)
                {
                    (testSpecifications[i].NumReadOperationsTotal)++;
                }
                if (i < numOutstandingReadOperationsRemainder)
                {
                    (testSpecifications[i].NumOutstandingReadOperations)++;
                }
                if (i < numItemsRemainder)
                {
                    (testSpecifications[i].NumItems)++;
                }
            }

            return testSpecifications;
        }
    }
}