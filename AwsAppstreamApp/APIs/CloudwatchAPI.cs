using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace AWSAppstreamApp.APIs
{
    //<copyright file="CloudWatchAPI.cs" company="WoAx-IT Wolfgang Axamit KG">
    // WoAx-IT Wolfgang Axamit KG. All rights reserved.
    // </copyright>  
    public static class CloudWatchAPI
    {
        private static bool initHappened;
        private static string AccessKeyID;
        private static string AccessKeySecret;
        public static void Init(string pAccessKeyId, string pAccessKeySecret)
        {
            AccessKeyID = pAccessKeyId;
            AccessKeySecret = pAccessKeySecret;
            initHappened = true;
        }
        private static AmazonCloudWatchClient GetAmazonCloudWatchClient()
        {
            var vCloudwatch = new AmazonCloudWatchClient(AccessKeyID, AccessKeySecret, RegionEndpoint.EUCentral1);
            return vCloudwatch;
        }
        public static async Task<List<Metric>> GetMetrics()
        {
            List<Metric> vList = new List<Metric>();

            var vClient = GetAmazonCloudWatchClient();
            var vMetrics = await vClient.ListMetricsAsync(new ListMetricsRequest() { });

            if (vMetrics != null)
            {
                var vAppstreamMetrics = vMetrics.Metrics.Where(x => x.Namespace == @"AWS/AppStream").ToList();
                var vS3Metrics = vMetrics.Metrics.Where(x => x.Namespace == @"AWS/S3").ToList();
                vList.AddRange(vAppstreamMetrics);
                vList.AddRange(vS3Metrics);
            }
            return vList;
        }

        public static async Task<List<MetricDataResult>> GetDataForMetricsAsync(List<Metric> pMetrics,
            Period pSelectedPeriod, string pSelectedStat, DateTime pStartDt, DateTime pEndDt)
        {
            //https://docs.aws.amazon.com/AmazonCloudWatch/latest/APIReference/API_GetMetricData.html
            //Amazon CloudWatch retains metric data as follows:
            //Data points with a period of less than 60 seconds are available for 3 hours.These data points are high - resolution metrics and are available only for custom metrics that have been defined with a StorageResolution of 1.
            //Data points with a period of 60 seconds(1 - minute) are available for 15 days.
            //Data points with a period of 300 seconds(5 - minute) are available for 63 days.
            //Data points with a period of 3600 seconds(1 hour) are available for 455 days(15 months).

            List<MetricDataResult> vResultList = new List<MetricDataResult>();
            List<MetricDataResult> vPartialResult = new List<MetricDataResult>();
            var vClient = GetAmazonCloudWatchClient();

            var vRequest = new GetMetricDataRequest()
            {
                StartTimeUtc = pStartDt.ToUniversalTime(),
                EndTimeUtc = pEndDt.ToUniversalTime(),
                MetricDataQueries = new List<MetricDataQuery>()
            };
            if (pMetrics != null)
                for (var vIndex = 0; vIndex < pMetrics.Count; vIndex++)
                {
                    var vMetric = pMetrics[vIndex];
                    var vMdq = new MetricDataQuery()
                    {
                        //Id = "id" + "_" + vIndex + vMetric.MetricName,
                        Id = "id" + "_" + vIndex + vMetric.MetricName,
                        //https://docs.aws.amazon.com/AmazonCloudWatch/latest/APIReference/API_MetricStat.html
                        MetricStat = new MetricStat()
                        {
                            Metric = vMetric,
                            Stat = pSelectedStat,
                            Period = pSelectedPeriod.Value
                        }
                    };

                    vRequest.MetricDataQueries.Add(vMdq);
                }

            string nextToken;
            do
            {
                GetMetricDataResponse vData = await vClient.GetMetricDataAsync(vRequest);
                nextToken = vData.NextToken;
                vRequest.NextToken = nextToken;
                if (vData.MetricDataResults != null)
                {
                    vPartialResult.AddRange(vData.MetricDataResults);
                }

            } while (!String.IsNullOrEmpty(nextToken));

            var groupedRes = vPartialResult.GroupBy(x => x.Id);
            foreach (var vGroupedRe in groupedRes)
            {
                MetricDataResult vVnewMetricDataResult = new MetricDataResult()
                {
                    Id = vGroupedRe.Key,
                    Label = vGroupedRe.First()?.Label,
                    Messages = vGroupedRe.SelectMany(x => x.Messages).ToList(),
                    StatusCode = vGroupedRe.Last()?.StatusCode,
                    Timestamps = vGroupedRe.SelectMany(x => x.Timestamps).ToList(),
                    Values = vGroupedRe.SelectMany(x => x.Values).ToList(),
                };
                vResultList.Add(vVnewMetricDataResult);
            }

            return vResultList;
        }
    }
}
