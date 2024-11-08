using appsvc_function_dev_cm_stats_dotnet001.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
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



        // dateHour = e.DimensionValues[0].Value;
        // eventName = e.DimensionValues[1].Value;
        // linkUrl = e.DimensionValues[2].Value;


        private class EventRow
        {
            public List<MyValue> DimensionValues;
            //public List<MyValue> MetricValues;

            public string DateHour
            {
                get { return DimensionValues[0].Value; }
            }

            public string EventName
            {
                get { return DimensionValues[1].Value; }
            }

            public string LinkUrl
            {
                get { return DimensionValues[2].Value; }
            }

            public string Id
            {
                get { return LinkUrl.Substring(LinkUrl.LastIndexOf("?ID=") + 4); }
            }
        }

        private class MyValue
        {
            [JsonProperty(PropertyName = "Value")]
            public string Value;
        }

        private class Event {
            public string DateHour;
            public string EventName;
            public string LinkUrl;

            public Event(string dateHour, string eventName, string linkUrl)
            {
                DateHour = dateHour;
                EventName = eventName;
                LinkUrl = linkUrl;
            }
        }

        private List<Event> _EventList;

        private List<EventRow> _ClickEvents = new();

        private List<EventRow> _MailToEvents = new();

        private readonly ILogger<RetrieveAnalyticsData> _logger;

        private Dictionary<string, int> _ClicksById; 

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
            string fileName = "GA4Report_Events_453422813_2024-11-08.json";

            var Getdata = GetBlob(connectionString, containerName, fileName, context);
            
            var obj = JsonConvert.DeserializeObject<dynamic>(Getdata);


            // where EventName = "click"

            _ClickEvents = ((JArray)obj.Rows).ToObject<List<EventRow>>();
            _ClickEvents.RemoveAll(x => x.EventName != "click");

            _MailToEvents = new List<EventRow>(_ClickEvents);
            _MailToEvents.RemoveAll(x => x.LinkUrl.IndexOf("mailto:") < 0);

            //_logger.LogWarning($"events.Count = {_ClickEvents.Count}");
            _logger.LogWarning($"events.Count = {_MailToEvents.Count}");

            _logger.LogInformation(" ");
            _logger.LogInformation(JsonConvert.SerializeObject(_MailToEvents));
            _logger.LogInformation(" ");


            _ClicksById = new();

            //foreach (var e in _ClickEvents)
            //{
            //    if (e.LinkUrl.StartsWith("mailto:"))
            //    {
            //        //_logger.LogInformation($"e.DateHour = {e.DateHour}");
            //        //_logger.LogInformation($"e.EventName = {e.EventName}");
            //        //_logger.LogInformation($"e.LinkUrl = {e.LinkUrl}");
            //        //_logger.LogInformation($"e.Id = {e.Id}");

            //        if (_ClicksById.ContainsKey(e.Id))
            //        {
            //            _ClicksById[e.Id] = _ClicksById[e.Id] + 1;
            //        }
            //        else
            //        {
            //            _ClicksById.Add(e.Id, 1);
            //        }
            //    }
            //}

            foreach (var e in _MailToEvents)
            {
                if (_ClicksById.ContainsKey(e.Id))
                {
                    _ClicksById[e.Id] = _ClicksById[e.Id] + 1;
                }
                else
                {
                    _ClicksById.Add(e.Id, 1);
                }
            }

            _logger.LogInformation(" ");

            var config2 = ReportConfiguration.GetConfig();
            var strStartDateFromConfig = config2.DateConfiguration.StartDate;
            var strEndDateFromConfig = config2.DateConfiguration.EndDate;

            _logger.LogInformation($"For the period of time between {strStartDateFromConfig} and {strEndDateFromConfig}:");
            _logger.LogInformation(" ");

            foreach (var c in _ClicksById)
            {
                _logger.LogInformation($"Job Id {c.Key} was clicked {c.Value} times.");
            }

            _logger.LogInformation(" ");

            //_EventList = new List<GoogleAnalyticsEvent>();

            //foreach (var e in events) {
            //    _EventList.Add(new GoogleAnalyticsEvent(e.DateHour, e.EventName, e.LinkUrl));
            //}

            //foreach (var e in _EventList)
            //{
            //    if (e.LinkUrl.StartsWith("mailto:"))
            //    {
            //        _logger.LogInformation($"e.DateHour = {e.DateHour}");
            //        _logger.LogInformation($"e.EventName = {e.EventName}");
            //        _logger.LogInformation($"e.LinkUrl = {e.LinkUrl}");
            //    }

            //}

            _logger.LogInformation("RetrieveAnalyticsData processed a request.");

            return new OkObjectResult(JsonConvert.SerializeObject(_ClickEvents));
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