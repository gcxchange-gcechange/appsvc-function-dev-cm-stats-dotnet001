using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
        //public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        public async Task Run([TimerTrigger("0 6 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("StoreAnalyticsData received a request.");

            try
            {
                Config config = new Config();
                await new ReportingApi(_logger).GenerateReport(config.PropertyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }


            _logger.LogInformation("StoreAnalyticsData processed a request.");

            //return new OkResult();
        }
    }
}