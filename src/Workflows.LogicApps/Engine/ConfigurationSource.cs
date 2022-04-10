using Microsoft.WindowsAzure.ResourceStack.Common.Services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Xylab.Workflows.LogicApps.Engine
{
    public class EdgeFlowConfigurationSource : AzureConfigurationManager
    {
        private readonly Dictionary<string, string> _configuration;

        public EdgeFlowConfigurationSource(Dictionary<string, string> configurations)
        {
            _configuration = configurations;
        }

        public static EdgeFlowConfigurationSource CreateDefault()
        {
            const string Fqfn = "Xylab.Workflows.LogicApps.Engine.AzureConfig.json";
            using Stream stream = typeof(EdgeFlowConfigurationSource).Assembly.GetManifestResourceStream(Fqfn)!;
            using StreamReader reader = new(stream);
            string json = reader.ReadToEnd();
            return new(JsonConvert.DeserializeObject<Dictionary<string, string>>(json));
        }

        public void SetAzureStorageAccountCredentials(string connectionString)
        {
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
