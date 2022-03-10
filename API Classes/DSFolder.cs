using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    public static class DSFolder
    {
        /// <summary>
        /// Given a folder path the folder id at the end of that path is returned.
        /// </summary>
        public static Guid? GetFolderIdByPath(ServerConnectionInformation sci, string folderPath)
        {
            var url = WebHelper.GetServerUrl(sci, "Folder", "GetByPathSlim", false);
            var getFolderByPathPackage = new
            {
                Path = folderPath,
                Split = '\\'
            };
            var json = JsonConvert.SerializeObject(getFolderByPathPackage);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);

            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            if (resp["Result"].HasValues)
                return new Guid(resp["Result"]["Id"].ToString());
            return null;
        }
        /// <summary>
        /// Creates a folder hierarchy
        /// </summary>
        public static Guid CreateFolderByPath(ServerConnectionInformation sci, string folderPath)
        {
            //Quick Exit, we have the full path.
            var folderId = GetFolderIdByPath(sci, folderPath);
            if (folderId.HasValue)
                return folderId.Value;

            var scId = CollectionGets.SecurityClasses(sci).First().Value; //For demo purposes we are just using the first security class found.
            var toBeCreated = new List<string>();
            var folders = folderPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            while (!folderId.HasValue && folders.Length > 0)
            {
                toBeCreated.Insert(0, folders.Last());
                folders = folders.Take(folders.Length - 1).ToArray();
                folderId = GetFolderIdByPath(sci, $"\\{String.Join("\\", folders)}");
            }
            foreach (var item in toBeCreated)
            {
                folderId = CreateFolder(sci, item, folderId, scId);
            }
            return folderId.Value;
        }

        private static Guid CreateFolder(ServerConnectionInformation sci, string folderName, Guid? parentId, string scId)
        {
            var url = WebHelper.GetServerUrl(sci, "Folder", "Create", false);
            var createFolderPkg = new
            {
                Folders = new []
                {
                    new
                    {
                        Title = folderName,
                        Parent = parentId,
                        SecurityClassId = scId
                    }
                }
            };
            var json = JsonConvert.SerializeObject(createFolderPkg);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);

            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            if (resp["Result"].HasValues)
                return new Guid(resp["Result"][0]["Id"].ToString());

            throw new Exception("No error and no result, please contact support");
        }
    }
}
