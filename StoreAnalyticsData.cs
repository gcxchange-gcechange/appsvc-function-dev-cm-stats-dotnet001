using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Configuration;

namespace appsvc_function_dev_cm_stats_dotnet001
{
    public class StoreAnalyticsData
    {
        private readonly ILogger<StoreAnalyticsData> _logger;

        public StoreAnalyticsData(ILogger<StoreAnalyticsData> logger)
        {
            _logger = logger;
        }

        [Function("StoreAnalyticsData")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        //public async Task Run([TimerTrigger("0 6 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("StoreAnalyticsData received a request.");

            try
            {
                var propertyId = ConfigurationManager.AppSettings["PropertyId"].Trim();
                await new ReportingApi().GenerateReport(propertyId, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            _logger.LogInformation("StoreAnalyticsData processed a request.");

            return new OkResult();
        }
    }
}