using Google.Analytics.Data.V1Beta;
using Newtonsoft.Json;
using System.Configuration;
using System.Globalization;
using System.Reflection;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json.Linq;

namespace appsvc_function_dev_cm_stats_dotnet001
{
    public class ReportingService
    {
        private class EventRow
        {
            List<MyValue> DimensionValues;
            List<MyValue> MetricValues;
        }

        private class MyValue
        {
            string Value;
            bool HasValue;
            int OneValueCase;
        }

        private List<EventRow> events = new();

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

                    //var fileName = string.Format("GA4Report_{0}_{1}_{2}.json", reportName, propertyId, DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.CurrentCulture));
                    var fileName = string.Format("GA4Report_{0}_{1}_{2}.json", reportName, propertyId, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture));
                    logger.LogWarning($"fileName = {fileName}");

                    BlobClient blobClient = new BlobClient(connectionString, containerName, fileName);

                    string json = JsonConvert.SerializeObject(reportsResponse);

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