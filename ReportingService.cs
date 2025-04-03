using Google.Analytics.Data.V1Beta;
using Newtonsoft.Json;
using System.Globalization;
using System.Reflection;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Serialization;
using static appsvc_function_dev_cm_stats_dotnet001.Common;

namespace appsvc_function_dev_cm_stats_dotnet001
{
    public class ReportingService
    {
        private class Event
        {
            public List<MyValue> DimensionValues;
            public List<MyValue> MetricValues;

            public string EventName
            {
                get { return DimensionValues[0].Value; }
            }

            public string EventDate
            {
                get { return DimensionValues[1].Value; }
            }

            public string JobOpportunityId
            {
                get { return DimensionValues[2].Value; }
            }

            public string EventCount
            {
                get { return MetricValues[0].Value; }
            }

            public string JobTitleEn;
            public string JobTitleFr;
        }

        private class MyValue
        {
            [JsonProperty(PropertyName = "Value")]
            public string Value;
        }

        private List<Event> ClickEvents = new();

        public class IgnorePropertiesResolver : DefaultContractResolver
        {
            private readonly HashSet<string> _ignoreProps;
            public IgnorePropertiesResolver(params string[] propNamesToIgnore)
            {
                _ignoreProps = new HashSet<string>(propNamesToIgnore);
            }
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);
                if (_ignoreProps.Contains(property.PropertyName))
                {
                    property.ShouldSerialize = _ => false;
                }
                return property;
            }
        }

        public void SaveReportToDisk(string reportName, string propertyId, RunReportResponse reportsResponse, ILogger logger)
        {
            try
            {
                if (reportsResponse != null)
                {
                    logger.LogInformation("Generating extract file...");

                    var outputDirectory = "C:\\GAReports";
                    Directory.CreateDirectory(outputDirectory); //Create directory if it doesn't exist

                    var delimiter = System.Configuration.ConfigurationManager.AppSettings["Delimiter"];

                    var fileName = string.Format("GA4Report_{0}_{1}_{2}.json", reportName, propertyId, DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.CurrentCulture));

                    File.WriteAllText(string.Format(@"{0}\{1}", outputDirectory, fileName), JsonConvert.SerializeObject(reportsResponse));
                    logger.LogInformation("Finished generating extract file");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error in generating extract file: " + ex);
            }
        }
      
        public async Task SaveReportToStorageContainerAsync(string reportName, string propertyId, RunReportResponse reportsResponse, ILogger logger)
        {
            try
            {
                if (reportsResponse != null)
                {
                    logger.LogInformation("Saving extract file to storage container...");

                    Config config = new Config();

                    string json = JsonConvert.SerializeObject(reportsResponse);

                    var obj = JsonConvert.DeserializeObject<dynamic>(json);
                    ClickEvents = ((JArray)obj.Rows).ToObject<List<Event>>();
                    logger.LogWarning($"_ClickEvents.Count = {ClickEvents.Count}");

                    //json = JsonConvert.SerializeObject(ClickEvents, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new IgnorePropertiesResolver("DimensionValues", "MetricValues") });
                    json = GetEventDetails(ClickEvents, logger).Result;

                    await SaveToBlob(json, config.AzureWebJobsStorage, config.ContainerName, GetFileName(reportName), logger);

                    json = GetPopularSkills(ClickEvents, logger).Result;
                    await SaveToBlob(json, config.AzureWebJobsStorage, config.ContainerName, GetFileName(string.Concat(reportName, "_Skills")), logger);

                    logger.LogInformation("Finished saving extract file to storage container");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error in generating extract file: " + ex);
            }
        }

        private class EventStuff
        {
            public string EventName;
            public List<TopTenWithSkills> topTenList;

            public EventStuff()
            {
                topTenList = new();
            }
        }

        private class TopTenWithSkills
        {
            public string JobTitleEn;
            public string JobTitleFr;
            public int ClickCount;
            public List<string> SkillsEn;
            public List<string> SkillsFr;

            public TopTenWithSkills ()
            {
                SkillsEn = new List<string> ();
                SkillsFr = new List<string>();
            }
        }

        private class LookUp
        {
            public string LookupId;
            public string LookupValue;
        }

        private async Task<string> GetPopularSkills(List<Event> ClickEvents, ILogger logger)
        {
            Dictionary<string, int> clickTotal = new();
            int count;
            int itemId;
            bool result;
            GraphServiceClient client = Auth.GetClient(logger);
            Config config = new Config();

            EventStuff eventStuff = new EventStuff();

            foreach (var clickEvent in ClickEvents) {
                result = int.TryParse(clickEvent.JobOpportunityId, out itemId);

                if (result) {
                    if (clickTotal.TryGetValue(clickEvent.JobOpportunityId, out count))
                        clickTotal[clickEvent.JobOpportunityId] = count + int.Parse(clickEvent.EventCount);
                    else
                        clickTotal.Add(clickEvent.JobOpportunityId, int.Parse(clickEvent.EventCount));
                }

                eventStuff.EventName = clickEvent.EventName;
            }

            int FoundCount = 0;
            var sortedDict = (from entry in clickTotal orderby entry.Value descending select entry); //.Take(10);

            foreach (var k in sortedDict)
            {
                if (FoundCount == 10) break;

                TopTenWithSkills topTen = new TopTenWithSkills();
                topTen.ClickCount = k.Value;

                try
                {
                    var item = await client.Sites[config.SiteId].Lists[config.ListId].Items[k.Key].GetAsync((requestConfiguration) =>
                    {
                        requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                        requestConfiguration.QueryParameters.Expand = new string[] { $"fields($select=JobTitleEn,JobTitleFr,Skills,{config.SkillsNameFr})" };
                    });

                    FoundCount += 1;

                    topTen.JobTitleEn = item.Fields.AdditionalData["JobTitleEn"].ToString();
                    topTen.JobTitleFr = item.Fields.AdditionalData["JobTitleFr"].ToString();

                    var skills = (UntypedArray)item.Fields.AdditionalData["Skills"];
                    var skillsArray = skills.GetValue().ToArray();
                    List<LookUp> Skills = new();

                    for (int i = 0; i < skillsArray.Length; i++)
                    {
                        var serializedString = KiotaJsonSerializer.SerializeAsStringAsync(skillsArray[i]);
                        var obj = JsonConvert.DeserializeObject<dynamic>(serializedString.Result);
                        Skills.Add(obj.ToObject<LookUp>());
                    }

                    foreach (var lookup in Skills)
                    {
                        topTen.SkillsEn.Add(lookup.LookupValue.ToString());
                    }

                    skills = (UntypedArray)item.Fields.AdditionalData[config.SkillsNameFr];
                    skillsArray = skills.GetValue().ToArray();
                    Skills = new();

                    for (int i = 0; i < skillsArray.Length; i++)
                    {
                        var serializedString = KiotaJsonSerializer.SerializeAsStringAsync(skillsArray[i]);
                        var obj = JsonConvert.DeserializeObject<dynamic>(serializedString.Result);
                        Skills.Add(obj.ToObject<LookUp>());
                    }

                    foreach (var lookup in Skills)
                    {
                        topTen.SkillsFr.Add(lookup.LookupValue.ToString());
                    }

                    eventStuff.topTenList.Add(topTen);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Exception: {ex.Message} ID: {k.Key}");
                }
            }

            return JsonConvert.SerializeObject(eventStuff);
        }

        private async Task<string> GetEventDetails(List<Event> ClickEvents, ILogger logger)
        {
            Dictionary<string, int> clickTotal = new();
            int itemId;
            bool result;
            GraphServiceClient client = Auth.GetClient(logger);
            Config config = new Config();

            foreach (var clickEvent in ClickEvents)
            {
                clickEvent.JobTitleEn = "JobTitleEn";
                clickEvent.JobTitleFr = "JobTitleFr";

                logger.LogWarning($"clickEvent.JobOpportunityId: {clickEvent.JobOpportunityId}");

                result = int.TryParse(clickEvent.JobOpportunityId, out itemId);

                logger.LogWarning($"result: {result}");

                if (result)
                {

                    logger.LogWarning($"itemId: {itemId}");

                    try
                    {
                        var item = await client.Sites[config.SiteId].Lists[config.ListId].Items[itemId.ToString()].GetAsync((requestConfiguration) =>
                        {
                            requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                            requestConfiguration.QueryParameters.Expand = new string[] { "fields($select=JobTitleEn,JobTitleFr)" };
                        });

                        clickEvent.JobTitleEn = item.Fields.AdditionalData["JobTitleEn"].ToString();
                        clickEvent.JobTitleFr = item.Fields.AdditionalData["JobTitleFr"].ToString();

                        logger.LogWarning($"clickEvent.JobTitleEn: {clickEvent.JobTitleEn}");


                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Error getting event details for ID: {itemId}");
                        logger.LogError($"Message: {e.Message}");
                        logger.LogError($"StackTrace: {e.StackTrace}");
                    }
                }
            }

            return JsonConvert.SerializeObject(ClickEvents, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new IgnorePropertiesResolver("DimensionValues", "MetricValues") });
        }

        private async Task SaveToBlob(string json, string connectionString, string containerName, string fileName, ILogger logger)
        {
            logger.LogWarning($"fileName = {fileName}");

            BlobClient blobClient = new BlobClient(connectionString, containerName, fileName);

            using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            {
                StreamWriter writer = new StreamWriter(ms);
                writer.Write(json);
                ms.Position = 0;
                await blobClient.UploadAsync(ms, true); // overwrite existing = true
            }

            BlobHttpHeaders headers = new BlobHttpHeaders();
            headers.ContentType = "application/json";
            await blobClient.SetHttpHeadersAsync(headers);
        }
    }
}