using System.Configuration;

namespace appsvc_function_dev_cm_stats_dotnet001.Configuration
{
    public class DateConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("startDate", IsRequired = true)]
        public string StartDate
        {
            get
            {
                return this["startDate"] as string;
            }
        }

        [ConfigurationProperty("endDate", IsRequired = true)]
        public string EndDate
        {
            get
            {
                return this["endDate"] as string;
            }
        }
        [ConfigurationProperty("numberOfDays", IsRequired = true)]
        public string NumberOfDays
        {
            get
            {
                return this["numberOfDays"] as string;
            }
        }
    }
}
