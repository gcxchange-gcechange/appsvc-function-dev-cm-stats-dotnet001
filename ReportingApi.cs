using appsvc_function_dev_cm_stats_dotnet001.Configuration;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Google.Analytics.Data.V1Beta;
using Google.Protobuf.Collections;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace appsvc_function_dev_cm_stats_dotnet001
{
    public class ReportingApi
    {
        private readonly BetaAnalyticsDataClient analyticsDataClient;
        private ILogger _logger;

        private class JsonCredentials
        {
            public string type;
            public string project_id;
            public string private_key_id;
            public string private_key;
            public string client_email;
            public string client_id;
            public string auth_uri;
            public string token_uri;
            public string auth_provider_x509_cert_url;
            public string client_x509_cert_url;
            public string universe_domain;
        }

        /// <summary>
        /// Intializes and returns Analytics Reporting Service Instance using the parameters stored in key file
        /// </summary>
        /// <returns>AnalyticsReportingService</returns>   
        private BetaAnalyticsDataClient GetAnalyticsClient()
        {
            Config config = new Config();

            JsonCredentials credentials = new JsonCredentials();

            credentials.type = config.type;
            credentials.project_id = config.project_id;
            credentials.private_key_id = config.private_key_id;
            credentials.client_email = config.client_email;
            credentials.client_id = config.client_id;
            credentials.auth_uri = config.auth_uri;
            credentials.token_uri = config.token_uri;
            credentials.auth_provider_x509_cert_url = config.auth_provider_x509_cert_url;
            credentials.client_x509_cert_url = config.client_x509_cert_url;
            credentials.universe_domain = config.universe_domain;

            try
            {
                SecretClientOptions options = new SecretClientOptions()
                {
                    Retry =
                    {
                        Delay= TimeSpan.FromSeconds(2),
                        MaxDelay = TimeSpan.FromSeconds(16),
                        MaxRetries = 5,
                        Mode = RetryMode.Exponential
                    }
                };
                var client = new SecretClient(new Uri(config.KeyVaultUrl), new DefaultAzureCredential(), options);

                KeyVaultSecret secret = client.GetSecret(config.PrivateKeySecretName);
                credentials.private_key = secret.Value;
            }
            catch (Exception e)
            {
                _logger.LogError("Error accessing the KeyVault!!");
                _logger.LogError(e.Message);
                _logger.LogError(e.StackTrace);
            }

            return new BetaAnalyticsDataClientBuilder
            {
                JsonCredentials = Regex.Unescape(JsonConvert.SerializeObject(credentials))
            }.Build();
        }

        public ReportingApi(ILogger logger)
        {
            _logger = logger;
                
            try {
                this.analyticsDataClient = GetAnalyticsClient();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error with GetAnalyticsClient: {e.Message}");
            }
        }

        /// <summary>
        /// Create date range based on entries in configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns>DateRange</returns>
        private DateRange GetDateRangeFromConfiguration(ReportConfiguration config)
        {
            var strStartDateFromConfig = config.DateConfiguration.StartDate;
            var strEndDateFromConfig = config.DateConfiguration.EndDate;
            var strNumberOfDaysFromConfig = config.DateConfiguration.NumberOfDays;

            DateTime.TryParse(strStartDateFromConfig, out DateTime reportStartDate);
            DateTime.TryParse(strEndDateFromConfig, out DateTime reportEndDate);
            int.TryParse(strNumberOfDaysFromConfig, out int numberOfDays);

            //Set start and end date for report using number of days
            var startDate = DateTime.Now.AddDays(-numberOfDays);
            var endDate = numberOfDays == 0 ? DateTime.Now : DateTime.Now.AddDays(-1);

            if (reportStartDate != DateTime.MinValue && reportEndDate != DateTime.MinValue && reportStartDate <= reportEndDate)
            {
                startDate = reportStartDate;
                endDate = reportEndDate;
            }

            return new DateRange
            {
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd")
            };
        }

        /// <summary>
        /// Get all reports configured in App.config
        /// </summary>
        /// <returns></returns>
        public async Task GenerateReport(string propertyId)
        {
            var reportResponse = new RunReportResponse();

            try
            {
                _logger.LogInformation("Processing Property Id: " + propertyId);
                var config = ReportConfiguration.GetConfig();

                foreach (var item in config.Reports)
                {
                    if (item is Report report)
                    {
                        var stopwatch = new Stopwatch();
                        _logger.LogInformation("Started fetching report: " + report.Name);
                        // Create the Metrics and dimensions object based on configuration.
                        var metrics = new RepeatedField<Metric> { report.Metrics.Split(',').Select(m => new Metric { Name = m }) };
                        var dimensions = new RepeatedField<Dimension> { report.Dimensions.Split(',').Select(d => new Dimension { Name = d }) };

                        var filter = new FilterExpression();
                        Filter.Types.StringFilter stringFilter = new Filter.Types.StringFilter();

                        _logger.LogInformation($"report.Filter: {report.Filter}");

                        stringFilter.Value = report.Filter;
                        filter.Filter = new Filter { FieldName = "eventName", StringFilter = stringFilter };

                        DateRange homeHomeOnTheRange = GetDateRangeFromConfiguration(config);

                        _logger.LogInformation($"homeHomeOnTheRange.StartDate: {homeHomeOnTheRange.StartDate}");
                        _logger.LogInformation($"homeHomeOnTheRange.EndDate: {homeHomeOnTheRange.EndDate}");

                        var reportRequest = new RunReportRequest
                        {
                            Property = "properties/" + propertyId,
                            DateRanges = { homeHomeOnTheRange },
                            Metrics = { metrics },
                            Dimensions = { dimensions },
                            DimensionFilter = filter
                        };

                        stopwatch.Start();
                        reportResponse = analyticsDataClient.RunReport(reportRequest);
                        stopwatch.Stop();
                        
                        _logger.LogInformation("Finished fetching report: " + report.Name);
                        _logger.LogInformation(string.Format("Time elapsed: {0:hh\\:mm\\:ss}", stopwatch.Elapsed));

                        //new ReportingService().SaveReportToDisk(report.Name, propertyId, reportResponse, logger);
                        await new ReportingService().SaveReportToStorageContainerAsync(report.Name, propertyId, reportResponse, _logger);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in fetching reports: " + ex);
            }
        }
    }
}