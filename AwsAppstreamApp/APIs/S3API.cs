using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;

namespace AWSAppstreamApp.APIs
{
    //<copyright file="S3API.cs" company="WoAx-IT Wolfgang Axamit KG">
    // WoAx-IT Wolfgang Axamit KG. All rights reserved.
    // </copyright>  
    public static class S3API
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
        private static AmazonS3Client GetAmazonS3Client()
        {
            var vAmazonS3Client = new AmazonS3Client(AccessKeyID, AccessKeySecret, RegionEndpoint.EUCentral1);
            return vAmazonS3Client;
        }
        public static async Task<List<Session>> GetObject(DateTime pStart, DateTime pEnd,
            Action<string> pMethod, string pBucket, string pAppDataFolder)
        {
            List<Session> vSessions = new List<Session>();
            var vBucket = pBucket;
            var vClient = GetAmazonS3Client();
            string currentFileName = null;
            var vkeys = GenerateKeysForS3(pStart, pEnd);

            if (vkeys != null)
                foreach (var vkey in vkeys)
                {
                    try
                    {
                        GetObjectResponse objectResponse = await vClient.GetObjectAsync(new GetObjectRequest()
                        {
                            BucketName = vBucket,
                            Key = vkey,
                        });

                        currentFileName = vkey.Replace('/', '-');

                        if (!Directory.Exists(pAppDataFolder))
                        {
                            Directory.CreateDirectory(pAppDataFolder);
                        }

                        var path = Path.Combine(pAppDataFolder, currentFileName);

                        var sessions = await SaveToFile(objectResponse, path);
                        if (sessions != null) vSessions.AddRange(sessions);

                        pMethod?.Invoke("File created: " + path);
                    }
                    catch (Exception e)
                    {
                        pMethod?.Invoke(e.Message + " : " + currentFileName);
                    }
                }

            return vSessions;
        }

        private static async Task<List<Session>> SaveToFile(GetObjectResponse pObjects, string pFilePath)
        {
            List<Session> vSessions = new List<Session>();
            
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (StreamReader reader = new StreamReader(pObjects.ResponseStream, Encoding.UTF8, true, 1024))
                {
                    var separator = new[] { ',' };
                    string currentLine;

                    bool first = true;
                    while ((currentLine = reader.ReadLine()) != null)
                    {
                        if (first)
                        {
                            first = false;
                            continue;
                        }
                        string[] temp = currentLine.Split(separator, StringSplitOptions.None);
                        
                        Session s = new Session();
                        s.user_session_id = temp[0];
                        s.aws_account_id = temp[1];
                        s.region = temp[2];
                        s.session_start_time = temp[3];
                        s.session_end_time = temp[4];
                        s.session_duration_in_seconds = temp[5];
                        s.user_id = temp[6];
                        s.user_arn = temp[7];
                        s.authentication_type = temp[8];
                        s.authentication_type_user_id = temp[9];
                        s.fleet_name = temp[10];
                        s.stack_name = temp[11];
                        s.instance_type = temp[12];
                        s.eni_private_ip_address = temp[13];
                        s.connected_at_least_once = temp[14];
                        s.client_ip_addresses = temp[15];
                        s.google_drive_enabled = temp[16];
                        s.one_drive_enabled = temp[17];
                        s.home_folders_storage_location = temp[18];
                        s.user_settings_clipboard_copy_from_local_device = temp[19];
                        s.user_settings_clipboard_copy_to_local_device = temp[20];
                        s.user_settings_file_upload = temp[21];
                        s.user_settings_file_download = temp[22];
                        s.user_settings_printing_to_local_device = temp[23];
                        s.application_settings_enabled = temp[24];
                        s.domain_joined = temp[25];
                        s.max_session_duration = temp[26];
                        s.session_type = temp[27];
                        s.stream_view = temp[28];
                        s.streaming_experience_settings_protocol = temp[29];

                        vSessions.Add(s);
                    }
                    
                    using (Stream responseStream = pObjects.ResponseStream)
                    {
                        await responseStream.CopyToAsync(memoryStream);
                    }

                    memoryStream.Position = 0;

                    using (FileStream file = new FileStream(
                               pFilePath,
                               FileMode.Create,
                               System.IO.FileAccess.Write))
                        await memoryStream.CopyToAsync(file);
                }
            }

            return vSessions;
        }

        //private static string GenerateCodeFor(string pClassName, string[] pProps)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.AppendLine($"public class {pClassName}" +"{");
        //    if (pProps != null)
        //        foreach (var prop in pProps)
        //        {
        //            sb.AppendLine($"public string {prop}" + " { get; set; }");
        //        }

        //    sb.AppendLine("}");
        //    return sb.ToString();
        //}

        private static List<string> GenerateKeysForS3(DateTime pStart, DateTime pEnd)
        {
            //"sessions/schedule=DAILY/year=2023/month=01/day=05/daily-session-report-2023-01-05.csv"

            List<string> vResultSet = new List<string>();
            DateTime currentDay = new DateTime(pStart.Year, pStart.Month, pStart.Day);
            while (currentDay <= pEnd)
            {
                var fileName = $"sessions/schedule=DAILY/year={currentDay.Year}/month={currentDay.Month:00}/day={currentDay.Day:00}/daily-session-report-{currentDay.Year}-{currentDay.Month:00}-{currentDay.Day:00}.csv";
                vResultSet.Add(fileName);
                currentDay = currentDay.AddDays(1);
            }

            return vResultSet;
        }
    }

    public class Session
    {
        public string user_session_id { get; set; }
        public string aws_account_id { get; set; }
        public string region { get; set; }
        public string session_start_time { get; set; }
        public string session_end_time { get; set; }
        public string session_duration_in_seconds { get; set; }
        public string user_id { get; set; }
        public string user_arn { get; set; }
        public string authentication_type { get; set; }
        public string authentication_type_user_id { get; set; }
        public string fleet_name { get; set; }
        public string stack_name { get; set; }
        public string instance_type { get; set; }
        public string eni_private_ip_address { get; set; }
        public string connected_at_least_once { get; set; }
        public string client_ip_addresses { get; set; }
        public string google_drive_enabled { get; set; }
        public string one_drive_enabled { get; set; }
        public string home_folders_storage_location { get; set; }
        public string user_settings_clipboard_copy_from_local_device { get; set; }
        public string user_settings_clipboard_copy_to_local_device { get; set; }
        public string user_settings_file_upload { get; set; }
        public string user_settings_file_download { get; set; }
        public string user_settings_printing_to_local_device { get; set; }
        public string application_settings_enabled { get; set; }
        public string domain_joined { get; set; }
        public string max_session_duration { get; set; }
        public string session_type { get; set; }
        public string stream_view { get; set; }
        public string streaming_experience_settings_protocol { get; set; }
    }

}
