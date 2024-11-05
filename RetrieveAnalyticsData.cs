using Google.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json.Linq;


namespace appsvc_function_dev_cm_stats_dotnet001
{
    public class RetrieveAnalyticsData
    {
        // DimensionValues.Count = 3
        // ValueOtherName = 2024102410
        // ValueOtherName = click
        // ValueOtherName = mailto:(redacted)?ID=234567
        // 
        // MetricValues.Count = 1
        // ValueOtherName = 1






        private class EventRow
        {
            public List<MyValue> DimensionValues;
            //public List<MyValue> MetricValues;
        }

        private class MyValue
        {

            [JsonProperty(PropertyName = "Value")]
            public string Value;
            //public bool HasValue;
            //public int OneValueCase;
        }

        private class GoogleAnalyticsEvent {
            public string DateHour;
            public string EventName;
            public string LinkUrl;

            public GoogleAnalyticsEvent(string dateHour, string eventName, string linkUrl)
            {
                DateHour = dateHour;
                EventName = eventName;
                LinkUrl = linkUrl;
            }
        }

        private List<GoogleAnalyticsEvent> _EventList;

        private List<EventRow> events = new();

        private readonly ILogger<RetrieveAnalyticsData> _logger;

        public RetrieveAnalyticsData(ILogger<RetrieveAnalyticsData> logger)
        {
            _logger = logger;
        }

        [Function("RetrieveAnalyticsData")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, ExecutionContext context)
        {
            _logger.LogInformation("RetrieveAnalyticsData received a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).AddEnvironmentVariables().Build();
            string connectionString = config["AzureWebJobsStorage"];
            string containerName = config["containerName"];
            string fileName = "GA4Report_Events_453422813_20241029163737.json";

            var Getdata = GetBlob(connectionString, containerName, fileName, context);

            var obj = JsonConvert.DeserializeObject<dynamic>(Getdata);
            events = ((JArray)obj.Rows).ToObject<List<EventRow>>();
            _logger.LogWarning($"events.Count = {events.Count}");

            _logger.LogInformation(" ");


            _EventList = new List<GoogleAnalyticsEvent>();

            foreach (var e in events) {
                //_logger.LogWarning($"e.DimensionValues.Count = {e.DimensionValues.Count}");
                //foreach (var d in e.DimensionValues) {
                //    _logger.LogWarning($"d.Value = {d.Value}");
                //    //_logger.LogWarning($"d.HasValue = {d.HasValue}");
                //    //_logger.LogWarning($"d.OneValueCase = {d.OneValueCase}");
                //}
                //_logger.LogInformation("------------------------------------------------------------");
                //_logger.LogWarning($"e.MetricValues.Count = {e.MetricValues.Count}");
                //foreach (var m in e.MetricValues)
                //{
                //    _logger.LogWarning($"d.ValueOtherName = {m.ValueOtherName}");
                //    //_logger.LogWarning($"d.HasValue = {m.HasValue}");
                //    //_logger.LogWarning($"d.OneValueCase = {m.OneValueCase}");
                //}

                _EventList.Add(new GoogleAnalyticsEvent(e.DimensionValues[0].Value, e.DimensionValues[1].Value, e.DimensionValues[2].Value));

                //_logger.LogInformation(" ");
            }


            foreach (var e in _EventList)
            {
                if (e.LinkUrl.StartsWith("mailto:"))
                {
                    _logger.LogInformation($"e.DateHour = {e.DateHour}");
                    _logger.LogInformation($"e.EventName = {e.EventName}");
                    _logger.LogInformation($"e.LinkUrl = {e.LinkUrl}");
                }

            }

















            _logger.LogInformation("RetrieveAnalyticsData processed a request.");

            return new OkResult();
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