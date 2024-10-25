using Google.Analytics.Data.V1Beta;
using Newtonsoft.Json;
using System.Configuration;
using System.Globalization;
using System.Reflection;

namespace appsvc_function_dev_cm_stats_dotnet001
{
    public class ReportingService
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public void SaveReportToDisk(string reportName, string propertyId, RunReportResponse reportsResponse)
        {
            try
            {
                if (reportsResponse != null)
                {
                    Logger.Info("Generating extract file...");

                    var outputDirectory = ConfigurationManager.AppSettings["OutputDirectory"];
                    Directory.CreateDirectory(outputDirectory); //Create directory if it doesn't exist

                    var delimiter = ConfigurationManager.AppSettings["Delimiter"];

                    var fileName = string.Format("GA4Report_{0}_{1}_{2}.json", reportName, propertyId, DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.CurrentCulture));

                    File.WriteAllText(string.Format(@"{0}\{1}", outputDirectory, fileName), JsonConvert.SerializeObject(reportsResponse));
                    Logger.Info("Finished geneating extract file...");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in geneating extract file: " + ex);
            }
        }

    }
}