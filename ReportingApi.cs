﻿using appsvc_function_dev_cm_stats_dotnet001.Configuration;
using Google.Analytics.Data.V1Beta;
using Google.Protobuf.Collections;
//using Grpc.Core.Logging;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace appsvc_function_dev_cm_stats_dotnet001
{
    public class ReportingApi
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly BetaAnalyticsDataClient analyticsDataClient;
        /// <summary>
        /// Intializes and returns Analytics Reporting Service Instance using the parameters stored in key file
        /// </summary>
        /// <returns>AnalyticsReportingService</returns>   
        private BetaAnalyticsDataClient GetAnalyticsClient()
        {
            return new BetaAnalyticsDataClientBuilder
            {
                CredentialsPath = ConfigurationManager.AppSettings["KeyFileName"]
            }.Build();
        }
        public ReportingApi()
        {
            this.analyticsDataClient = GetAnalyticsClient();
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

            //Use start and end date from config if specified else keep the existing values
            if (reportStartDate != DateTime.MinValue && reportEndDate != DateTime.MinValue &&
                reportStartDate <= reportEndDate)
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
        public void GenerateReport(string propertyId, ILogger logger)
        {
            var reportResponse = new RunReportResponse();
            try
            {
                logger.LogInformation("Processing Property Id: " + propertyId);
                var config = ReportConfiguration.GetConfig();

                foreach (var item in config.Reports)
                {
                    if (item is Report report)
                    {
                        var stopwatch = new Stopwatch();
                        logger.LogInformation("Started fetching report: " + report.Name);
                        // Create the Metrics and dimensions object based on configuration.
                        var metrics = new RepeatedField<Metric> { report.Metrics.Split(',').Select(m => new Metric { Name = m }) };
                        var dimensions = new RepeatedField<Dimension> { report.Dimensions.Split(',').Select(d => new Dimension { Name = d }) };
                        var reportRequest = new RunReportRequest
                        {
                            Property = "properties/" + propertyId,
                            DateRanges = { GetDateRangeFromConfiguration(config) },
                            Metrics = { metrics },
                            Dimensions = { dimensions }
                        };
                        stopwatch.Start();
                        reportResponse = analyticsDataClient.RunReport(reportRequest);
                        stopwatch.Stop();
                        logger.LogInformation("Finished fetching report: " + report.Name);
                        logger.LogInformation(string.Format("Time elapsed: {0:hh\\:mm\\:ss}", stopwatch.Elapsed));
                        new ReportingService().SaveReportToDisk(report.Name, propertyId, reportResponse);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error in fetching reports: " + ex);
            }
        }

    }
}
