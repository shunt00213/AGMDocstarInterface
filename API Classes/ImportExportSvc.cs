using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AGMDocstarInterface
{
    public static class ImportExportSvc
    {
        const string MANIFEST_NAMESPACE = "http://schemas.datacontract.org/2004/07/Astria.Framework.DataContracts.Import";
        const string ARRAY_NAMESPACE = "http://schemas.microsoft.com/2003/10/Serialization/Arrays";
        const string CONTENTTYPE = "ContentType";
        const string ROLE = "Role";
        const string SECURITYCLASS = "SecurityClass";
        const string FIELDMETA = "FieldMeta";
        const string FIELDGROUPS = "FieldGroups";
        const string FOLDER = "Folder";
        internal static void ExportToCSVAndUpdate(ServerConnectionInformation sci, JToken searchResult, Dictionary<string, string> fields)
        {
            ExportToCSV(sci, searchResult);
            var downloadDate = DateTime.Now;

            //Update Documents marking them as linked:
            var results = (JArray)searchResult["Results"];
            var count = results.Count;
            var slimUpdates = new List<dynamic>(count);
            for (int i = 0; i < count; i++)
            {
                var dfs = Search.GetDynamicFields(searchResult["Results"][i]); //Dictionary of all fields that are indexed that are not part of a standard search result.
                slimUpdates.Add(new
                {
                    DocumentId = searchResult["Results"][i]["Id"].ToString(),
                    VersionId = dfs["versionId"][0],
                    ModifiedTicks = dfs["modifiedTicks"][0],
                    CustomFieldValues = new[]
                    {
                        new
                        {
                            CustomFieldMetaId = fields["erplinked"],
                            CustomFieldName = "ERPLinked",
                            TypeCode = "3",
                            BoolValue = new bool?(true),
                            DateTimeValue = new DateTime?()
                        },
                        new
                        {
                            CustomFieldMetaId = fields["erplinkeddate"],
                            CustomFieldName = "ERPLinkedDate",
                            TypeCode = "16",
                            BoolValue = new bool?(),
                            DateTimeValue = new DateTime?(downloadDate)
                        }
                    }
                });
            }
            var url = WebHelper.GetServerUrl(sci, "Document", "UpdateManySlimEx", false);
            var json = JsonConvert.SerializeObject(new
            {
                OnlyModifiedCustomFields = true,
                DocumentSlimUpdates = slimUpdates
            }, new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.MicrosoftDateFormat });
            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());
        }

        public static void ExportToCSV(ServerConnectionInformation sci, JToken searchResult, string[] fields = null)
        {
            //If selected columns is not provided then the user defined columns for their search result display will be used.
            var columns = "";//Explicitly set columns to be returned here, otherwise the logged in user's settings will be used (as defined in the search grid in DocStar).
            if (fields != null)
            {
                var cols = fields.Select(r => $"\"{r}\":{{}}");
                columns = $"{{ {String.Join(",", cols)} }}";
            }
            var json = JsonConvert.SerializeObject(new
            {
                SelectedColumns = columns, 
                SearchRequest = searchResult["Request"] //This request is used to re-execute on the server to get all the results unpaged.
            });
            var url = WebHelper.GetServerUrl(sci, "ImportExport", "ExportSelectedToCSV", false);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);

            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            var fileId = $"DownloadTemp\\{resp["Result"]}";
            var dlUrl = DownloadFile.GenerateDownloadURL(sci, fileId);
            var localFile = DownloadFile.Download(sci, dlUrl, fileId);
            Console.WriteLine("CSV Created: " + localFile);
        }

        internal static void ManifestImport(ServerConnectionInformation sci, DSDoc[] documents, bool svrSideFileCopy)
        {
            var parentDir = Path.Combine(Path.GetTempPath(), "EclipseImports", Guid.NewGuid().ToString());
            Directory.CreateDirectory(parentDir);
            var entitiesToBeCreated = ProcessDocs(documents, parentDir, svrSideFileCopy);
            var rm = CreateRoleManifest(entitiesToBeCreated, parentDir);
            var scm = CreateSecurityClassManifest(entitiesToBeCreated, parentDir);
            var cfm = CreateFieldMetaManifest(entitiesToBeCreated, parentDir);
            var cfgm = CreateFieldGroupManifest(entitiesToBeCreated, parentDir);
            var fm = CreateFolderManifest(entitiesToBeCreated, parentDir);
            var ctm = CreateContentTypeManifest(entitiesToBeCreated, parentDir);
            var dm = CreateDocumentManifest(documents, entitiesToBeCreated, parentDir, svrSideFileCopy);
            var masterManifest = CreateXEl("StandardBatch");
            masterManifest.Add(ctm, cfgm, cfm, dm, fm, rm, scm); // Order in the XML is important.

            var manifestName = "import.manifest";
            File.WriteAllText(Path.Combine(parentDir, manifestName), masterManifest.ToString());
            var zipPath = Path.Combine(Path.GetTempPath(), "EclipseImports", $"{Guid.NewGuid()}.zip");
            ZipHandler.Zip(parentDir, zipPath);

            var fileId = UploadFile.Upload(sci, zipPath);
            Directory.Delete(parentDir, true);
            var importJob = ImportAndWait(sci, fileId);
        }
        private static Dictionary<string, object> ProcessDocs(DSDoc[] docs, string parentDir, bool svrSideFileCopy)
        {
            var cts = new Dictionary<string, DSContentType>();
            var scs = new Dictionary<string, DSSecurityClass>();
            var roles = new Dictionary<string, int>();
            var fldrs = new Dictionary<string, DSFldr>();
            var cfs = new Dictionary<string, DSCustomField>();
            var cfgs = new Dictionary<string, HashSet<DSCustomField>>();
            var id = 0;
            for (int i = 0; i < docs.Length; i++)
            {
                var d = docs[i];
                if (svrSideFileCopy)
                    d.FilePath = Path.GetFullPath(d.FilePath); //Fully Qualify Path.
                else
                {
                    Directory.CreateDirectory(Path.Combine(parentDir, i.ToString())); //Copy file to a relative directory and update the FilePath.
                    var relPath = $"{i}\\{Path.GetFileName(d.FilePath)}";
                    File.Copy(d.FilePath, Path.Combine(parentDir, relPath));
                    d.FilePath = relPath;
                }
                if (!String.IsNullOrWhiteSpace(d.SecurityClassGroupName) && !roles.ContainsKey(d.SecurityClassGroupName))
                    roles.Add(d.SecurityClassGroupName, id++);
                if (!String.IsNullOrWhiteSpace(d.SecurityClassName) && !scs.ContainsKey(d.SecurityClassName))
                    scs.Add(d.SecurityClassName, new DSSecurityClass
                    {
                        Id = id++,
                        Name = d.SecurityClassName,
                        RoleId = String.IsNullOrWhiteSpace(d.SecurityClassGroupName) ? null : new int?(roles[d.SecurityClassGroupName])
                    });
                if (!cts.ContainsKey(d.ContentTypeName))
                    cts.Add(d.ContentTypeName, new DSContentType
                    {
                        Id = id++,
                        Name = d.ContentTypeName,
                        SecurityClassId = scs[d.SecurityClassName].Id
                    });
                if (!String.IsNullOrEmpty(d.FolderPath) && !fldrs.ContainsKey(d.FolderPath))
                    fldrs.Add(d.FolderPath, new DSFldr
                    {
                        Id = id++,
                        Name = d.FolderPath,
                        SecurityClassId = scs[d.SecurityClassName].Id
                    });
                if (d.CustomFields != null)
                {
                    foreach (var cf in d.CustomFields)
                    {
                        if (!cfs.ContainsKey(cf.Name))
                        {
                            cfs.Add(cf.Name, new DSCustomField
                            {
                                Id = id++,
                                Name = cf.Name,
                                TypeCode = cf.TypeCode
                            });
                        }
                        if (!String.IsNullOrWhiteSpace(cf.CustomFieldGroupName))
                        {
                            if (!cfgs.ContainsKey(cf.CustomFieldGroupName))
                                cfgs.Add(cf.CustomFieldGroupName, new HashSet<DSCustomField>());
                            var field = cfs[cf.Name];
                            if (!cfgs[cf.CustomFieldGroupName].Contains(field))
                                cfgs[cf.CustomFieldGroupName].Add(field);
                        }

                    }
                }
            }
            return new Dictionary<string, object>
            {
                { CONTENTTYPE, cts },
                { SECURITYCLASS, scs },
                { ROLE, roles },
                { FOLDER, fldrs },
                { FIELDMETA, cfs },
                { FIELDGROUPS, cfgs }
            };
        }
        private static XElement CreateRoleManifest(Dictionary<string, object> etbc, string parentDir)
        {
            var entities = (Dictionary<string, int>)etbc[ROLE];
            var manifestItems = new List<XElement>();
            foreach (var e in entities)
            {
                manifestItems.Add(CreateXEl("RoleManifestItem",
                        CreateXEl("Id", e.Value),
                        CreateXEl("Name", e.Key)
                    ));
            }
            var manifest = CreateXEl("RoleManifest", CreateXEl("Roles", manifestItems));
            var manifestName = "Role.manifest";
            File.WriteAllText(Path.Combine(parentDir, manifestName), manifest.ToString());

            return CreateXEl("RoleManifest", manifestName);
        }
        private static XElement CreateFolderManifest(Dictionary<string, object> etbc, string parentDir)
        {
            var createdPaths = new Dictionary<string, int>();
            var entities = (Dictionary<string, DSFldr>)etbc[FOLDER];
            var manifestItems = new List<XElement>();
            var id = 0;
            foreach (var e in entities)
            {
                string parentId = null;
                var currentPath = "";
                var parts = e.Key.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    currentPath += p + "\\";
                    if (createdPaths.ContainsKey(currentPath))
                    {
                        parentId = createdPaths[currentPath].ToString();
                        continue;
                    }
                    id++;
                    manifestItems.Add(CreateXEl("FolderManifestItem",
                            CreateXEl("Id", id),
                            CreateXEl("Name", p),
                            CreateXEl("ParentId", parentId),
                            CreateXEl("SecurityClassId", e.Value.SecurityClassId)
                        ));
                    parentId = id.ToString();
                    createdPaths.Add(currentPath, id);
                }
                e.Value.Id = id;
            }
            var manifest = CreateXEl("FolderManifest", CreateXEl("Folders", manifestItems));
            var manifestName = "Folder.manifest";
            File.WriteAllText(Path.Combine(parentDir, manifestName), manifest.ToString());

            return CreateXEl("FolderManifest", manifestName);
        }
        private static XElement CreateFieldMetaManifest(Dictionary<string, object> etbc, string parentDir)
        {
            var entities = (Dictionary<string, DSCustomField>)etbc[FIELDMETA];
            var manifestItems = new List<XElement>();
            foreach (var e in entities)
            {
                manifestItems.Add(CreateXEl("CustomFieldManifestItem",
                        CreateXEl("Id", e.Value.Id),
                        CreateXEl("Name", e.Value.Name),
                        CreateXEl("NonIndexed", false),
                        CreateXEl("Type", e.Value.TypeCode.ToString())
                        )
                    );
            }
            var manifest = CreateXEl("CustomFieldManifest", CreateXEl("CustomFields", manifestItems));
            var manifestName = "CustomField.manifest";
            File.WriteAllText(Path.Combine(parentDir, manifestName), manifest.ToString());

            return CreateXEl("CustomFieldManifest", manifestName);
        }
        private static XElement CreateFieldGroupManifest(Dictionary<string, object> etbc, string parentDir)
        {
            var entities = (Dictionary<string, HashSet<DSCustomField>>)etbc[FIELDGROUPS];
            var manifestItems = new List<XElement>();
            foreach (var e in entities)
            {
                var templates = new List<XElement>();
                var i = 0;
                foreach (var t in e.Value)
                {
                    templates.Add(CreateXEl("CustomFieldGroupTemplateManifestItem",
                        CreateXEl("CustomFieldId", t.Id),
                        CreateXEl("Order", i)
                        )
                    );
                    i++;
                }
                manifestItems.Add(CreateXEl("CustomFieldGroupManifestItem",
                    CreateXEl("Id", e.Key),
                    CreateXEl("Name", e.Key),
                    CreateXEl("Templates", templates)
                    )
                );
            }
            var manifest = CreateXEl("CustomFieldGroupManifest", CreateXEl("CustomFieldGroups", manifestItems));
            var manifestName = "CustomFieldGroup.manifest";
            File.WriteAllText(Path.Combine(parentDir, manifestName), manifest.ToString());

            return CreateXEl("CustomFieldGroupManifest", manifestName);
        }
        private static XElement CreateSecurityClassManifest(Dictionary<string, object> etbc, string parentDir)
        {
            var entities = (Dictionary<string, DSSecurityClass>)etbc[SECURITYCLASS];
            var manifestItems = new List<XElement>();
            foreach (var e in entities)
            {
                manifestItems.Add(CreateXEl("SecurityClassManifestItem",
                        CreateXEl("Id", e.Value.Id),
                        CreateXEl("Name", e.Value.Name),
                        CreateXEl("RolePermissions",
                            CreateXEl("PermissionManifestItem",
                                CreateXEl("Id", e.Value.RoleId),
                                CreateXEl("Permission", "Full")
                            )
                        )
                    ));
            }
            var manifest = CreateXEl("SecurityClassManifest", CreateXEl("SecurityClasses", manifestItems));
            var manifestName = "SecurityClass.manifest";
            File.WriteAllText(Path.Combine(parentDir, manifestName), manifest.ToString());

            return CreateXEl("SecurityClassManifest", manifestName);
        }
        private static XElement CreateContentTypeManifest(Dictionary<string, object> etbc, string parentDir)
        {
            var entities = (Dictionary<string, DSContentType>)etbc[CONTENTTYPE];
            var manifestItems = new List<XElement>();
            foreach (var e in entities)
            {
                manifestItems.Add(
                    CreateXEl("ContentTypeManifestItem",
                        CreateXEl("DefaultSecurityClassId", e.Value.SecurityClassId),
                        //CreateXEl("DisplayMask", displayMask),
                        CreateXEl("Id", e.Value.Id),
                        CreateXEl("Name", e.Value.Name),
                        CreateXEl("SecurityClassId", e.Value.SecurityClassId)
                    ));
            }
            var manifest = CreateXEl("ContentTypeManifest", CreateXEl("ContentTypes", manifestItems));
            var manifestName = "ContentType.manifest";
            File.WriteAllText(Path.Combine(parentDir, manifestName), manifest.ToString());

            return CreateXEl("ContentTypeManifest", manifestName);
        }
        private static XElement CreateDocumentManifest(DSDoc[] documents, Dictionary<string, object> etbc, string parentDir, bool svrSideFileCopy)
        {
            var arrayNamespace = XNamespace.Get(ARRAY_NAMESPACE);
            var folderLookup = (Dictionary<string, DSFldr>)etbc[FOLDER];
            var securityLookup = (Dictionary<string, DSSecurityClass>)etbc[SECURITYCLASS];
            var contentTypeLookup = (Dictionary<string, DSContentType>)etbc[CONTENTTYPE];
            var dmis = new XElement[documents.Length];
            for (int i = 0; i < documents.Length; i++)
            {
                var d = documents[i];
                var customFields = CreateXEl("CustomFields");
                if (d.CustomFields != null)
                {
                    foreach (var c in d.CustomFields)
                    {
                        var cfmi = CreateXEl("CustomFieldManifestItem",
                            CreateXEl("CustomFieldGroupName", c.CustomFieldGroupName),
                            CreateXEl("Name", c.Name),
                            c.SetId.HasValue ? CreateXEl("SetId", c.SetId) : null,
                            CreateXEl("Value", c.Value)
                        );
                        customFields.Add(cfmi);
                    }
                }
                dmis[i] = CreateXEl("DocumentManifestItem",
                    CreateXEl("ContentItems",
                        CreateXEl("ContentManifestItem", CreateXEl("Path", d.FilePath))
                    ),
                    CreateXEl("ContentTypeId", contentTypeLookup[d.ContentTypeName].Id),
                    customFields,
                    CreateXEl("FolderIds",
                        new XAttribute(XNamespace.Xmlns + "d4p1", ARRAY_NAMESPACE),
                        String.IsNullOrWhiteSpace(d.FolderPath) ? null : new XElement(XName.Get("string", ARRAY_NAMESPACE), folderLookup[d.FolderPath].Id)),
                    CreateXEl("SecurityClassId", securityLookup[d.SecurityClassName].Id),
                    CreateXEl("Title", d.Title)
                );
            }

            var manifest = CreateXEl("DocumentManifest", CreateXEl("CopyFiles", svrSideFileCopy), CreateXEl("Documents", dmis));
            var manifestName = "DocumentManifest.manifest";
            File.WriteAllText(Path.Combine(parentDir, manifestName), manifest.ToString());

            return CreateXEl("DocumentManifest", manifestName);
        }
        private static JObject ImportAndWait(ServerConnectionInformation sci, string fileId)
        {
            var importEntitiesPackage = new
            {
                MachineId = Environment.MachineName,
                MachineName = Environment.MachineName,
                OverwriteExisting = false,
                ZipFile = fileId,
                //ForceOutOfProcess = true //Required for Server file copy, it is likely to occur with non-server copy as ZIPs > 10mb go out of process.
            };
            var json = JsonConvert.SerializeObject(importEntitiesPackage);
            var url = WebHelper.GetServerUrl(sci, "ImportExport", "ImportEntities", false);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            var importJobId = resp["Result"]["Id"].ToString();
            var status = int.Parse(resp["Result"]["Status"].ToString());
            //Optionally you can wait for the import to complete. It is possible that an error could occur after the import has begun.

            const int complete = 134217728;
            const int completeWithError = 134217729;
            const int failed = 134217730;
            while ((status & complete) != complete)
            {
                Thread.Sleep(5000); //Check every 5 seconds, this can be lowered if needed.
                url = WebHelper.GetServerUrl(sci, "ImportExport", "CheckImportStatus", false);
                respString = WebHelper.ExecutePost(url, $"\"{importJobId}\"", sci.Token);
                resp = (JObject)JsonConvert.DeserializeObject(respString);
                error = resp["Error"];
                if (error.HasValues)
                    throw new Exception(error["Message"].ToString());

                status = int.Parse(resp["Result"]["JobStatus"].ToString());
                Console.WriteLine($"Importing: {resp["Result"]["PercentDone"]}%");
            }

            url = WebHelper.GetServerUrl(sci, "ImportExport", "GetImportJob", false);
            respString = WebHelper.ExecutePost(url, $"\"{importJobId}\"", sci.Token);
            resp = (JObject)JsonConvert.DeserializeObject(respString);
            error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());


            switch (status)
            {
                case completeWithError:
                    throw new Exception($"A partial import was completed, the following failure was logged: {resp["Result"]["Failures"]}");
                case failed:
                    throw new Exception($"Import Failed, the following failure was logged: {resp["Result"]["Exception"]}");
                default:
                    Console.WriteLine("Import Complete");
                    break;
            }
            return resp;
        }
        private static XElement CreateXEl(string nodeName, params object[] content)
        {
            return new XElement(XName.Get(nodeName, MANIFEST_NAMESPACE), content);
        }
    }
    public class DSDoc
    {
        public string Title { get; set; }
        public string FilePath { get; set; }
        public string FolderPath { get; set; }
        public string ContentTypeName { get; set; }
        public string SecurityClassName { get; set; }
        public string SecurityClassGroupName { get; set; }
        public List<DSCustomField> CustomFields { get; set; }
    }
    public class DSCustomField
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public CFTypeCode TypeCode { get; set; }
        public string Value { get; set; }
        public String CustomFieldGroupName { get; set; }
        public Guid? SetId { get; set; }
    }
    public class DSContentType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SecurityClassId { get; set; }
    }
    public class DSSecurityClass
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? RoleId { get; set; }
    }
    public class DSFldr
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SecurityClassId { get; set; }
    }
}
