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
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("StoreAnalyticsData received a request.");

            try
            {
                var propertyId = ConfigurationManager.AppSettings["PropertyId"].Trim();
                new ReportingApi().GenerateReport(propertyId, _logger);
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