using Newtonsoft.Json.Linq;
using System.IO;
namespace AGMDocstarInterface
{
    public class Docstar
    {
        ServerConnectionInformation sci;
        Dictionary<string,string> fields;
        public Docstar()
        {
            sci = new()
            {
                WebUrl = "https://docstarapp01.main.agmcontainer.com/Eclipseweb",
                UserName = $"{Environment.UserName}@agmcontainer.com"
            };
            Authentication.SSOLogin(sci);
            fields = CollectionGets.CustomFieldMetas(sci);
        }
        //This returns a string that refers to the downloaded file, and can be then manipulated with file.IO to be what it needs to be.
        public string DownloadDoc(Dictionary<string, string> searchValues, string folderPath)
        {
            string searchString = BuildSearchString(searchValues);
            var folderId = DSFolder.GetFolderIdByPath(sci, folderPath);
            var searchResult = Search.TextAndFolderSearch(sci, folderId ,searchString);
            //var results = (JArray)searchResult;
            var results = (JArray)searchResult["Results"];
            var dfs = Search.GetDynamicFields(results[0]);
            return DownloadFile.Execute(sci, dfs["versionId"][0], true);
            //return DownloadFile.DownloadAllInSearchResult(sci, searchResult);

            //return new Document("","","");
        }

        public bool DocExists(Dictionary<string, string> searchValues, string folderPath)
        {
            string searchString = BuildSearchString(searchValues);
            var folderId = DSFolder.GetFolderIdByPath(sci, folderPath);
            var searchResult = Search.TextAndFolderSearch(sci, folderId, searchString);
            //var results = (JArray)searchResult;
            var results = (JArray)searchResult["Results"];
            if(results.Count > 0)
            {
                return true;
            }
            return false;
        }
        
        //KVP input is key:name of field, value: value of field.
        public void SearchAndUpdate(Dictionary<string, string> searchValues, string fieldName, object value)
        {
            string searchString = BuildSearchString(searchValues);
            var searchResult = Search.TextSearch(sci, searchString);

            var results = (JArray)searchResult["Results"];
            var length = results.Count;
            var slimUpdates = new List<dynamic>(length);
            
            for (int i = 0; i < length; i++)
            {
                var dfs = Search.GetDynamicFields(results[i]); //Dictionary of all fields that are indexed that are not part of a standard search result.

                slimUpdates.Add(new
                {
                    DocumentId = results[i]["Id"].ToString(),   //Unique Identifier for the Document
                    VersionId = dfs["versionId"][0],            //Unique Identifier for the Version of the Document
                    ModifiedTicks = dfs["modifiedTicks"][0],    //Identifier for the last time the version we retrieved was modified, this is for detection and in some cases possible auto resolution of conflicting updates.
                    CustomFieldValues = GetCustomFieldValues(fieldName, value)  //New Custom Field Value(s), this update specifies to leave other fields as is if not specified in this object. This allows you to update targeted fields.
                    
                });
            }
            DSDocument.UpdateManySlim(sci, slimUpdates);
        }
        private string BuildSearchString(Dictionary<string, string> values)
        {
            string searchString = $"{values.First().Key}:{values.First().Value}";
            for(int i = 1; i < values.Count; i++)
            {
                searchString += $" AND {values.ElementAt(i).Key}:{values.ElementAt(i).Value}";
            }
            return searchString;
        }
        private dynamic GetCustomFieldValues(string fieldName, object value)
        {
            return value.GetType().Name switch
            {
                "String" => new[]
                    {
                        new
                        {
                            CustomFieldMetaId = fields[fieldName.ToLower()],
                            CustomFieldName = fieldName,
                            TypeCode = (int)CFTypeCode.String,
                            StringValue = value.ToString()
                        }
                    },
                "Int32" => new[]
                    {
                        new
                        {
                            CustomFieldMetaId = fields[fieldName.ToLower()],
                            CustomFieldName = fieldName,
                            TypeCode = (int)CFTypeCode.Int32,
                            Int32Value = (int)value
                        }
                    },
                _ => new[] { new { } },
            };
        }
    }
    public class Document
    {
        string DocID;
        string versionID;
        string modifiedTicks;
        Dictionary<string, string> fields;

        public Document() { }
        public Document(string dID, string vID, string modTicks)
        {
            DocID = dID;
            versionID = vID;
            modifiedTicks = modTicks;
        }
    }
}