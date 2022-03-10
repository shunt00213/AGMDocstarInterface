using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AGMDocstarInterface
{
    static class DownloadFile
    {
        /// <summary>
        /// Downloads a file as a PDF via the GetFile generic handler.
        /// If large file support (100+ mb) is required then our FileTransfer Service should be used.
        /// </summary>
        public static string Execute(ServerConnectionInformation sci, string versionId, bool download)
        {
            try
            {
                var fileId = PrepForSend(sci, new string[] { versionId });
                var url = GenerateDownloadURL(sci, fileId);
                if (download)
                    return Download(sci, url, fileId);

                return url;
            }
            catch (Exception ex)
            {
                Console.WriteLine("DownloadFile: {0}", ex);
                return null;
            }
        }

        internal static string DownloadAllInSearchResult(ServerConnectionInformation sci, JToken searchResult)
        {
            var results = (JArray)searchResult["Results"];
            var count = results.Count;
            var versionIds = new string [count];
            for (int i = 0; i < count; i++)
            {
                var dfs = Search.GetDynamicFields(results[i]); //Dictionary of all fields that are indexed that are not part of a standard search result.
                versionIds[i] = dfs["versionId"][0];
            }
            var fileId = PrepForSend(sci, versionIds);
            var url = GenerateDownloadURL(sci, fileId);
            return Download(sci, url, fileId);
        }

        public static string GenerateDownloadURL(ServerConnectionInformation sci, string fileId)
        {
            var args = new[] {
                String.Format("path={0}", HttpUtility.UrlEncode(fileId)),
                "inline=true",
                String.Format("{0}={1}", Constants.SOURCEHEADER, Constants.SERVICE_SOURCE),
                String.Format("{0}={1}", Constants.TOKENHEADER, HttpUtility.UrlEncode(sci.Token).Replace("=", "%3D")) };

            //Dispite its name DownloadZipToBrowser will download the prepapared file in its native form.
            //If prep for send resulted in a zip file (which it will if a document has multiple content items or your requested multiple documents)
            //the you will recieve a zip file.
            var url = String.Format(Constants.GETFILEURL, sci.ServerUrl, "DownloadZipToBrowser", String.Join("&", args));
            return url;
        }
        public static string GenerateNativeDownloadUrl(ServerConnectionInformation sci, string documentId, string versionId, string contentItemId)
        {
            var args = new[] {
                "inline=true",
                $"documentId={documentId}",
                $"versionId={versionId}",
                $"contentItemId={contentItemId}",
                String.Format("{0}={1}", Constants.SOURCEHEADER, Constants.SERVICE_SOURCE),
                String.Format("{0}={1}", Constants.TOKENHEADER, HttpUtility.UrlEncode(sci.Token).Replace("=", "%3D")) };

            //Dispite its name DownloadZipToBrowser will download the prepapared file in its native form.
            //If prep for send resulted in a zip file (which it will if a document has multiple content items or your requested multiple documents)
            //the you will recieve a zip file.
            var url = String.Format(Constants.GETFILEURL, sci.ServerUrl, "GetNativeDownload", String.Join("&", args));
            return url;

        }
        /// <summary>
        /// Downloads a file that has been prepped for download
        /// </summary>
        public static string Download(ServerConnectionInformation sci, string url, string fileId)
        {
            
            var webClient = new WebClient();
            var localFileName = String.Format("{0}{1}", DateTime.Now.Ticks, Path.GetExtension(fileId));
            Console.WriteLine($"Downloading {localFileName} from {url}");
            webClient.DownloadFile(url, localFileName);
            return localFileName;
        }
        /// <summary>
        /// Sets a document up for Export/Download/Email/Print
        /// NOTE: To prep a document for a non-native export it must be imaged. If it is not imaged the prep call will thrown an exception stating the file could not be converted.
        /// </summary>
        private static string PrepForSend(ServerConnectionInformation sci, string[] versionIds)
        {
            var url = WebHelper.GetServerUrl(sci, "Document", "PrepForSend", false);

            var pfsp = new
            {
                //DocumentIds = new[] { documentId }, //If used the current version is downloaded
                VersionIds = versionIds, //multiple documents can be downloaded at a time.
                SendOptions = new
                {
                    ActionType = 256, //ActionType.Downloaded, //For auditing, Downloaded (256), emailed (131072), printed (262144)
                    ExportType = 0, //Native (0), Pdf (1), and Tiff (4) available
                    CombineOutput = false //This only is applicable when the output type is PDF or Tiff and multiple content items are included in the download request.
                    //PageSelection = "1,5,8-10"
                }
            };

            var json = JsonConvert.SerializeObject(pfsp);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);

            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return resp["Result"].ToString();
        }
    }
}
