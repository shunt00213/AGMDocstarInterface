using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    static class Search
    {
        /// <summary>
        /// Finds the first 25 documents in a supplied folder path.
        /// </summary>
        public static JToken FolderSearch(ServerConnectionInformation sci, string folderPath)
        {
            var folderId = DSFolder.GetFolderIdByPath(sci, folderPath);
            if (!folderId.HasValue)
                throw new Exception("No folder found at the path: " + folderPath);

            var url = WebHelper.GetServerUrl(sci, "Search", "Search", false);
            var searchObj = new
            {
                IncludeDocuments = true,
                IncludeFolders = false,
                IncludeInboxes = false,
                MaxRows = 25,
                Start = 0,
                FolderId = folderId
            };

            var json = JsonConvert.SerializeObject(searchObj);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);

            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return resp["Result"];
        }
        /// <summary>
        /// Returns the first 25 documents found in the system.
        /// </summary>
        public static JToken BlankDocumentSearch(ServerConnectionInformation sci)
        {
            return TextSearch(sci, "*");
        }
        /// <summary>
        /// Test search will search across all fields by default.
        /// You can specify a field in the text criteria by using a : (ex InvoiceNum:123456)
        /// </summary>
        public static JToken TextSearch(ServerConnectionInformation sci, string textCriteria, int max = 8000, int start = 0)
        {
            return TextAndFolderSearch(sci, null, textCriteria, max, start);
        }
        /// <summary>
        /// Test search will search across all fields by default.
        /// If a folder Id is specified the results returned will have to be contained within that folder. (NOTE: IncludeSubfolders property can be set to true to include documents in subfolders of the provided folder)
        /// You can specify a field in the text criteria by using a : (ex InvoiceNum:123456)
        /// </summary>
        /// <returns></returns>
        public static JToken TextAndFolderSearch(ServerConnectionInformation sci, Guid? folderId, string textCriteria, int max = 8000, int start = 0)
        {
            Guid[] includedFolders = folderId.HasValue ? new[] { folderId.Value } : null;
            var url = WebHelper.GetServerUrl(sci, "Search", "Search", false);
            //Simple text based search, Fielded search can be added as well.
            var searchObj = new
            {
                IncludeDocuments = true,
                IncludeFolders = false,
                IncludeInboxes = false,
                IncludeSubFolders = false,
                IncludedFolderIds = includedFolders,
                MaxRows = max,
                Start = start,
                TextCriteria = textCriteria,
                SortBy = "created",
                SortOrder = "asc",
                DocumentRetrieveLimit = 10000
            };

            var json = JsonConvert.SerializeObject(searchObj);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);

            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return resp["Result"];
        }
        /// <summary>
        /// Simply converts the Dynamic Fields element into a dictionary.
        /// Dynamic fields contain non-standard search fields.
        /// </summary>
        public static Dictionary<string, List<string>> GetDynamicFields(JToken searchResult)
        {
            var dfDict = new Dictionary<string, List<string>>();
            var dfs = (JArray)searchResult["DynamicFields"];
            foreach (JToken df in dfs)
            {
                var key = df["Key"].ToString();
                dfDict.Add(key, new List<string>());
                foreach (JToken v in (JArray)df["Value"])
                {
                    dfDict[key].Add(v.ToString());
                }
            }
            return dfDict;
        }
    }
}
