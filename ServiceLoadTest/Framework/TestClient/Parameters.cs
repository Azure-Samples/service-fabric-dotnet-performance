// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace ServiceLoadTestClient
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    internal class Parameters
    {
        internal static Dictionary<string, Id> ParameterNames = new Dictionary<string, Id>()
        {
            {
                "ClusterAddress",
                Id.ClusterAddress
            },
            {
                "ClientConnectionEndpoint",
                Id.ClientConnectionPort
            },
            {
                "ReverseProxyEndpoint",
                Id.ReverseProxyPort
            },
            {
                "ServerCertificateThumbprint",
                Id.ServerCertificateThumbprint
            },
            {
                "ClientCertificateThumbprint",
                Id.ClientCertificateThumbprint
            },
            {
                "NumWriteOperationsTotal",
                Id.NumWriteOperationsTotal
            },
            {
                "NumOutstandingWriteOperations",
                Id.NumOutstandingWriteOperations
            },
            {
                "NumReadOperationsTotal",
                Id.NumReadOperationsTotal
            },
            {
                "NumOutstandingReadOperations",
                Id.NumOutstandingReadOperations
            },
            {
                "OperationDataSizeInBytes",
                Id.OperationDataSizeInBytes
            },
            {
                "NumItems",
                Id.NumItems
            },
            {
                "NumClients",
                Id.NumClients
            },
            {
                "TargetServiceType",
                Id.TargetServiceType
            },
        };

        private static Dictionary<Id, ParameterInfo> ParameterInformation = new Dictionary<Id, ParameterInfo>()
        {
            {
                Id.ClusterAddress,
                new ParameterInfo()
                {
                    Description = "Address of the cluster. e.g. mycluster.westus.cloudapp.azure.com",
                    Type = typeof(string)
                }
            },
            {
                Id.ClientConnectionPort,
                new ParameterInfo()
                {
                    Description = "Endpoint used by clients to connect to the cluster to perform management and query operations.",
                    Type = typeof(int)
                }
            },
            {
                Id.ReverseProxyPort,
                new ParameterInfo()
                {
                    Description = "Endpoint used by clients to connect to the reverse proxy.",
                    Type = typeof(int)
                }
            },
            {
                Id.ServerCertificateThumbprint,
                new ParameterInfo()
                {
                    Description = "Thumbprint of server certificate",
                    Type = typeof(string)
                }
            },
            {
                Id.ClientCertificateThumbprint,
                new ParameterInfo()
                {
                    Description = "Thumbprint of client certificate",
                    Type = typeof(string)
                }
            },
            {
                Id.NumWriteOperationsTotal,
                new ParameterInfo()
                {
                    Description = "Total number of write operations to be performed on the service.",
                    Type = typeof(int)
                }
            },
            {
                Id.NumOutstandingWriteOperations,
                new ParameterInfo()
                {
                    Description = "Number of write operations sent to the service at any given time.",
                    Type = typeof(int)
                }
            },
            {
                Id.NumReadOperationsTotal,
                new ParameterInfo()
                {
                    Description = "Total number of read operations to be performed on the service.",
                    Type = typeof(int)
                }
            },
            {
                Id.NumOutstandingReadOperations,
                new ParameterInfo()
                {
                    Description = "Number of read operations sent to the service at any given time.",
                    Type = typeof(int)
                }
            },
            {
                Id.OperationDataSizeInBytes,
                new ParameterInfo()
                {
                    Description = "Size in bytes of the data associated with each operation (i.e. read or write) performed on the service.",
                    Type = typeof(int)
                }
            },
            {
                Id.NumItems,
                new ParameterInfo()
                {
                    Description = "Number of items (e.g. number of rows in a table) that the operations are distributed across in the service.",
                    Type = typeof(int)
                }
            },
            {
                Id.NumClients,
                new ParameterInfo()
                {
                    Description = "Number of clients used to perform the operations on the service.",
                    Type = typeof(int)
                }
            },
            {
                Id.TargetServiceType,
                new ParameterInfo()
                {
                    Description =
                        String.Format(
                            "Target service type on which the operations should be performed. Supported values are: {0}",
                            String.Join(",", (TargetService.SupportedTypes.Keys).Select(e => e.ToString()))),
                    Type = typeof(TargetService.Types)
                }
            },
        };

        internal Parameters()
        {
            this.ParameterValues = new Dictionary<Id, object>();
        }

        internal Dictionary<Id, object> ParameterValues { get; private set; }

        internal void ReadFromConfigFile()
        {
            // Loop through each of the settings in app.config file
            foreach (string paramName in ConfigurationManager.AppSettings.AllKeys)
            {
                // Make sure the parameter is recognized
                if (!ParameterNames.ContainsKey(paramName))
                {
                    string message = String.Format("Parameter {0} specified in the configuration file is not recognized", paramName);
                    throw new ConfigurationErrorsException(message);
                }

                // Get the parameter value convert it to the appropriate type
                string paramValue = ConfigurationManager.AppSettings[paramName];
                Id parameterId = ParameterNames[paramName];
                try
                {
                    if (ParameterInformation[parameterId].Type.IsEnum)
                    {
                        this.ParameterValues[parameterId] = Enum.Parse(ParameterInformation[parameterId].Type, paramValue);
                    }
                    else if (ParameterInformation[parameterId].Type != typeof(string))
                    {
                        this.ParameterValues[parameterId] = Convert.ChangeType(paramValue, ParameterInformation[parameterId].Type);
                    }
                    else
                    {
                        this.ParameterValues[parameterId] = paramValue;
                    }
                }
                catch (Exception e)
                {
                    string message = String.Format(
                        "The value '{0}' specified for parameter '{1}' in the configuration file could not be converted to {2}. Exception encountered while converting: {3}.",
                        paramValue,
                        paramName,
                        ParameterInformation[parameterId].Type,
                        e);
                    throw new ConfigurationErrorsException(message);
                }
            }

            // Make sure values have been specified for all the parameters.
            if (this.ParameterValues.Count != ParameterNames.Count)
            {
                IEnumerable<KeyValuePair<string, Id>> missingParameters = ParameterNames.Where(kvp => !this.ParameterValues.ContainsKey(kvp.Value));
                string message = String.Format(
                    "The list of parameters specified in the configuration file is incomplete. Missing parameters: {0}",
                    String.Join(",", missingParameters.Select(kvp => kvp.Key)));
                throw new ConfigurationErrorsException(message);
            }
        }

        internal void OverrideFromCommandLine(string[] args)
        {
            if ((args.Length == 1) &&
                ((args[0] == "/?") ||
                 (args[0] == "-?")))
            {
                this.PrintUsageAndExit();
            }

            // Loop through each argument specified via the command line.
            foreach (string arg in args)
            {
                // Parse the argument
                string[] argParts = arg.Split(new[] {':'}, 2);
                if (argParts.Length < 2)
                {
                    string message = String.Format("Unable to parse command line argument '{0}' because it does not contain the ':' delimiter.", arg);
                    this.PrintUsageAndThrow(message);
                }
                argParts[0] = argParts[0].Remove(0, 1);

                // Make sure the parameter is recognized
                if (!ParameterNames.ContainsKey(argParts[0]))
                {
                    string message = String.Format("Command line argument {0} is not recognized", argParts[0]);
                    this.PrintUsageAndThrow(message);
                }

                // Get the parameter value convert it to the appropriate type
                Id argumentId = ParameterNames[argParts[0]];
                try
                {
                    if (ParameterInformation[argumentId].Type.IsEnum)
                    {
                        this.ParameterValues[argumentId] = Enum.Parse(ParameterInformation[argumentId].Type, argParts[1]);
                    }
                    else if (ParameterInformation[argumentId].Type != typeof(string))
                    {
                        this.ParameterValues[argumentId] = Convert.ChangeType(argParts[1], ParameterInformation[argumentId].Type);
                    }
                    else
                    {
                        this.ParameterValues[argumentId] = argParts[1];
                    }
                }
                catch (Exception e)
                {
                    string message = String.Format(
                        "The value '{0}' specified for command line argument {1} could not be converted to {2}. Exception encountered while converting: {3}.",
                        argParts[1],
                        argParts[0],
                        ParameterInformation[argumentId].Type,
                        e);
                    this.PrintUsageAndThrow(message);
                }
            }
        }

        private void PrintUsageAndThrow(string message)
        {
            this.PrintUsage();
            throw new ArgumentException(message);
        }

        private void PrintUsageAndExit()
        {
            this.PrintUsage();
            Environment.Exit(0);
        }

        private void PrintUsage()
        {
            StringBuilder usage = new StringBuilder(Process.GetCurrentProcess().MainModule.ModuleName);

            foreach (KeyValuePair<string, Id> argName in ParameterNames)
            {
                usage.AppendLine();
                string argFormat = String.Format(
                    "    [/{0}:<{1}_VALUE>]",
                    argName.Key,
                    ParameterInformation[argName.Value].Type.Name);
                usage.Append(argFormat);
            }

            usage.AppendLine();
            usage.AppendLine();
            usage.AppendLine(
                "All command line arguments are optional. Default values are read from the app.config file. Command line arguments override the default values.");
            usage.AppendLine();

            foreach (KeyValuePair<string, Id> argName in ParameterNames)
            {
                string argDesc = String.Format(
                    "{0} - {1}",
                    argName.Key,
                    ParameterInformation[argName.Value].Description);
                usage.AppendLine(argDesc);
                usage.AppendLine();
            }

            usage.AppendLine();
            usage.AppendLine();

            Console.Write(usage.ToString());
        }

        internal enum Id
        {
            ClusterAddress,
            ClientConnectionPort,
            ReverseProxyPort,
            ServerCertificateThumbprint,
            ClientCertificateThumbprint,
            NumWriteOperationsTotal,
            NumOutstandingWriteOperations,
            NumReadOperationsTotal,
            NumOutstandingReadOperations,
            OperationDataSizeInBytes,
            NumItems,
            NumClients,
            TargetServiceType
        }

        private class ParameterInfo
        {
            internal string Description;
            internal Type Type;
        }
    }
}