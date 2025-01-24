using Google.Analytics.Data.V1Beta;
using Newtonsoft.Json;
using System.Globalization;
using System.Reflection;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace appsvc_function_dev_cm_stats_dotnet001
{
    public class ReportingService
    {
        private class Event
        {
            public List<MyValue> DimensionValues;
            public List<MyValue> MetricValues;

            public string EventName
            {
                get { return DimensionValues[0].Value; }
            }

            public string EventDate
            {
                get { return DimensionValues[1].Value; }
            }

            public string JobOpportunityId
            {
                get { return DimensionValues[2].Value; }
            }

            public string EventCount
            {
                get { return MetricValues[0].Value; }
            }
        }

        private class MyValue
        {
            [JsonProperty(PropertyName = "Value")]
            public string Value;
        }

        private List<Event> _ClickEvents = new();

        public class IgnorePropertiesResolver : DefaultContractResolver
        {
            private readonly HashSet<string> _ignoreProps;
            public IgnorePropertiesResolver(params string[] propNamesToIgnore)
            {
                _ignoreProps = new HashSet<string>(propNamesToIgnore);
            }
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);
                if (_ignoreProps.Contains(property.PropertyName))
                {
                    property.ShouldSerialize = _ => false;
                }
                return property;
            }
        }

        public void SaveReportToDisk(string reportName, string propertyId, RunReportResponse reportsResponse, ILogger logger)
        {
            try
            {
                if (reportsResponse != null)
                {
                    logger.LogInformation("Generating extract file...");

                    var outputDirectory = System.Configuration.ConfigurationManager.AppSettings["OutputDirectory"];
                    Directory.CreateDirectory(outputDirectory); //Create directory if it doesn't exist

                    var delimiter = System.Configuration.ConfigurationManager.AppSettings["Delimiter"];

                    var fileName = string.Format("GA4Report_{0}_{1}_{2}.json", reportName, propertyId, DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.CurrentCulture));

                    File.WriteAllText(string.Format(@"{0}\{1}", outputDirectory, fileName), JsonConvert.SerializeObject(reportsResponse));
                    logger.LogInformation("Finished generating extract file");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error in generating extract file: " + ex);
            }
        }

        public async Task SaveReportToStorageContainerAsync(string reportName, string propertyId, RunReportResponse reportsResponse, ILogger logger)
        {
            try
            {
                if (reportsResponse != null)
                {
                    logger.LogInformation("Saving extract file to storage container...");

                    var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).AddEnvironmentVariables().Build();
                    string connectionString = config["AzureWebJobsStorage"];
                    string containerName = config["containerName"];

                    var fileName = string.Format("GA4Report_{0}_{1}_{2}.json", reportName, propertyId, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture));
                    logger.LogWarning($"fileName = {fileName}");

                    BlobClient blobClient = new BlobClient(connectionString, containerName, fileName);

                    string json = JsonConvert.SerializeObject(reportsResponse);

                    var obj = JsonConvert.DeserializeObject<dynamic>(json);
                    _ClickEvents = ((JArray)obj.Rows).ToObject<List<Event>>();
                    logger.LogWarning($"_ClickEvents.Count = {_ClickEvents.Count}");

                    json = JsonConvert.SerializeObject(_ClickEvents, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new IgnorePropertiesResolver("DimensionValues", "MetricValues") });

                    using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
                    {
                        StreamWriter writer = new StreamWriter(ms);
                        writer.Write(json);
                        ms.Position = 0;
                        await blobClient.UploadAsync(ms, true); // overwrite existing = true
                    }

                    BlobHttpHeaders headers = new BlobHttpHeaders();
                    headers.ContentType = "application/json";
                    await blobClient.SetHttpHeadersAsync(headers);

                    logger.LogInformation("Finished saving extract file to storage container");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error in generating extract file: " + ex);
            }
        }
    }
}