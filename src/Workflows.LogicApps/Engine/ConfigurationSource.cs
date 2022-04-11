using Microsoft.WindowsAzure.ResourceStack.Common.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Azure.Workflows.Data.Configuration
{
    public class EdgeFlowConfigurationSource : AzureConfigurationManager
    {
        private readonly Dictionary<string, string> _configuration;

        public Uri EndpointUri { get; set; }

        public string AppDirectoryPath { get; set; }

        public EdgeFlowConfigurationSource(
            Dictionary<string, string> configurations,
            Uri endpointUri,
            string appDirectoryPath)
        {
            _configuration = configurations;
            EndpointUri = endpointUri;
            AppDirectoryPath = appDirectoryPath;
        }

        public static EdgeFlowConfigurationSource CreateDefault(
            Uri endpointUri,
            string appDirectoryPath)
        {
            const string Fqfn = "Xylab.Workflows.LogicApps.Engine.AzureConfig.json";
            using Stream stream = typeof(EdgeFlowConfigurationSource).Assembly.GetManifestResourceStream(Fqfn)!;
            using StreamReader reader = new(stream);
            string json = reader.ReadToEnd();
            return new EdgeFlowConfigurationSource(
                JsonConvert.DeserializeObject<Dictionary<string, string>>(json),
                endpointUri,
                appDirectoryPath);
        }

        public void SetAzureStorageAccountCredentials(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            string[] settingKeys = new[]
            {
                "CloudStorageAccount.Workflows.BillingDataStorage.ConnectionString",
                "CloudStorageAccount.Workflows.RegionalDataStorage.ConnectionString",
                "CloudStorageAccount.Workflows.HydrationDataStorage.ConnectionString",
                "CloudStorageAccount.Workflows.PlatformArtifactsContentStorage.ConnectionString",
                "CloudStorageAccount.Workflows.ScaleUnitsDataStorage.CU00.ConnectionString",
                "CloudStorageAccount.Workflows.PairedRegion.RegionalDataStorage.ConnectionString",
                "CloudStorageAccount.Flow.FunctionAppsRuntimeStorage.ConnectionString",
                "CloudStorageAccount.Flow.FunctionAppsSecretStorage.ConnectionString"
            };

            foreach (string settingKey in settingKeys)
            {
                _configuration[settingKey] = connectionString;
            }
        }

        protected override string? GetConfigurationSettings(string settingName)
        {
            return _configuration.GetValueOrDefault(settingName);
        }
    }
}
