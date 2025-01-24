using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Newtonsoft.Json.Linq;
using SysConfig = System.Configuration;
using System.Globalization;

namespace appsvc_function_dev_cm_stats_dotnet001
{
    public class RetrieveAnalyticsData
    {
        private class Event {
            public string EventName;
            public string EventDate;
            public string JobOpportunityId;
            public string EventCount;
        }

        private List<Event> _ClickEvents = new();

        private readonly ILogger<RetrieveAnalyticsData> _logger;

        public RetrieveAnalyticsData(ILogger<RetrieveAnalyticsData> logger)
        {
            _logger = logger;
        }

        [Function("RetrieveAnalyticsData")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, ExecutionContext context)
        {
            _logger.LogInformation("RetrieveAnalyticsData received a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string reportName = data["reportName"];
            DateTime rightNow = DateTime.Now;

            _logger.LogInformation($"reportName = {reportName}");
            _logger.LogInformation($"rightNow = {rightNow}");

            return GetEventReport(reportName, rightNow, context);
        }

        private IActionResult GetEventReport(string reportName, DateTime reportDate, ExecutionContext context)
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).AddEnvironmentVariables().Build();
            string connectionString = config["AzureWebJobsStorage"];
            string containerName = config["containerName"];

            string fileName = $"GA4Report_{reportName}_{SysConfig.ConfigurationManager.AppSettings["PropertyId"].Trim()}_{reportDate.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture)}.json";

            _logger.LogInformation($"fileName = {fileName}");

            try
            {
                var Getdata = GetBlob(connectionString, containerName, fileName, context);
                var obj = JsonConvert.DeserializeObject<dynamic>(Getdata);

                _ClickEvents = ((JArray)obj).ToObject<List<Event>>();
            }
            catch (Exception e) {
                _logger.LogError("!! Error fetching data !!");
                _logger.LogError(e.Message);
                _logger.LogError(e.StackTrace);
            }

            _logger.LogInformation($"For report {reportName} stored on {reportDate.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture)}:");
            _logger.LogInformation(" ");

            var sortedEvents = _ClickEvents.OrderBy(o => o.EventDate).ThenBy(o => o.JobOpportunityId);

            foreach (var e in sortedEvents)
            {
                _logger.LogInformation($"Job Opportunity Id {e.JobOpportunityId} was clicked {e.EventCount} times on {e.EventDate} as triggered by event {e.EventName}.");
            }

            _logger.LogInformation(" ");
            _logger.LogInformation("RetrieveAnalyticsData processed a request. GetJobApplicationClicks");

            return new OkObjectResult(JsonConvert.SerializeObject(sortedEvents));
        }

        private string GetBlob(string connectionString, string containerName, string fileName, ExecutionContext executionContext)
        {
            string contents = string.Empty;

            try
            {
                BlobClient blobClient = new BlobClient(connectionString, containerName, fileName);
                contents = blobClient.DownloadContentAsync().Result.Value.Content.ToString();
            }
            catch (Exception e)
            {
                _logger.LogError("!! Exception !!");
                _logger.LogError(e.Message);
                _logger.LogError("!! StackTrace !!");
                _logger.LogError(e.StackTrace);
            }

            return contents;
        }
    }
}