using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    public static class DSDocument
    {
        public static void BatchLoadDocuments(ServerConnectionInformation dsSvr)
        {
            //Create a DSDoc object per document to be converted.
            //Any reference to a Content type, Custom Field, Folder, Security Class, or Roles (Security Class Group Name) will be created if it does not already exist.
            //I'd suggest a batch size of 1000 documents or less.
            var doc = new DSDoc
            {
                Title = "Document 1", //Required
                ContentTypeName = "CustomerCapturedDocs", //Required
                FilePath = "13f33pub.pdf", //Required
                SecurityClassName = "Public", //Required
                FolderPath = @"CustomerDocs\Acme", //Optional
                SecurityClassGroupName = "AllUsers", //Required - Security Group will have full access to Security Class if we are creating the security class for the first time.
                CustomFields = new List<DSCustomField> //Optional
                    {
                        new DSCustomField
                        {
                            Name = "CustomerId",
                            TypeCode = CFTypeCode.String,
                            Value = "ERP"
                        },
                        new DSCustomField
                        {
                            Name = "InvoiceDate",
                            TypeCode = CFTypeCode.DateTime,
                            Value = DateTime.Now.ToString()
                        },
                        new DSCustomField
                        {
                            Name = "InvoiceNumber",
                            TypeCode = CFTypeCode.String,
                            Value = "Inv90901"
                        }
                    }
            };
            //With fields other then the ones above to demonstrate that the documents created can all have different metadata.
            var doc2 = new DSDoc
            {
                Title = "Document 2", //Required
                ContentTypeName = "HRDocs", //Required
                FilePath = "CSharpOnGC.pdf", //Required
                SecurityClassName = "HR", //Required
                FolderPath = @"ReleaseNotes\DocStar\19.1", //Optional
                SecurityClassGroupName = "HRPersonnel", //Required - Security Group will have full access to Security Class if we are creating the security class for the first time.
                CustomFields = new List<DSCustomField> //Optional - The fields listed here do not have to be the same as above.
                    {
                        new DSCustomField
                        {
                            Name = "Employee",
                            TypeCode = CFTypeCode.String,
                            Value = "VanDerlofske,Matthew"
                        },
                        new DSCustomField
                        {
                            Name = "Position",
                            TypeCode = CFTypeCode.String,
                            Value = "Manager, Software Development"
                        },
                        new DSCustomField
                        {
                            Name = "Terminated",
                            TypeCode = CFTypeCode.Boolean,
                            Value = "false"
                        },
                        new DSCustomField
                        {
                            Name = "HireDate",
                            TypeCode = CFTypeCode.Date,
                            Value = "2004/03/01"
                        },
                        new DSCustomField
                        {
                            Name = "Manager",
                            TypeCode = CFTypeCode.String,
                            Value = "Bowden, Jeff"
                        }
                    }
            };
            //Minimum information
            var doc3 = new DSDoc
            {
                Title = "Nearly No Metadata", //Required
                ContentTypeName = "Default", //Required
                FilePath = "13f33pub.pdf", //Required
                SecurityClassName = "Public", //Required
                SecurityClassGroupName = "AllUsers", //Required - Security Group will have full access to Security Class if we are creating the security class for the first time.
            };
            //Document with header fields and line items:
            var doc4 = new DSDoc
            {
                Title = "With Group Items", //Required
                ContentTypeName = "CustomerCapturedDocs", //Required
                FilePath = "13f33pub.pdf", //Required
                SecurityClassName = "Public", //Required
                FolderPath = @"CustomerDocs\Acme", //Optional
                SecurityClassGroupName = "AllUsers", //Required - Security Group will have full access to Security Class if we are creating the security class for the first time.
                CustomFields = new List<DSCustomField> //Optional
                    {
                        new DSCustomField
                        {
                            Name = "CustomerId",
                            TypeCode = CFTypeCode.String,
                            Value = "ERP"
                        },
                        new DSCustomField
                        {
                            Name = "InvoiceDate",
                            TypeCode = CFTypeCode.DateTime,
                            Value = DateTime.Now.ToString()
                        },
                        new DSCustomField
                        {
                            Name = "InvoiceNumber",
                            TypeCode = CFTypeCode.String,
                            Value = "Inv90901"
                        }
                    }
            };
            //Add 5 Line Items:
            for (int i = 0; i < 5; i++)
            {
                var setId = Constants.NewSeq(); //A set id identifies multiple fields that belong in the same row in a field group.
                doc4.CustomFields.Add(new DSCustomField
                {
                    CustomFieldGroupName = "APLineItems",
                    Name = "PartNum",
                    TypeCode = CFTypeCode.String,
                    Value = "Bolt15x6x10",
                    SetId = setId
                });
                doc4.CustomFields.Add(new DSCustomField
                {
                    CustomFieldGroupName = "APLineItems",
                    Name = "Quantity",
                    TypeCode = CFTypeCode.Int32,
                    Value = "200",
                    SetId = setId
                });
                doc4.CustomFields.Add(new DSCustomField
                {
                    CustomFieldGroupName = "APLineItems",
                    Name = "UnitPrice",
                    TypeCode = CFTypeCode.Decimal,
                    Value = ".0112",
                    SetId = setId
                });
            }
            //NOTE: This method will also create content types/fields/folders/security classes/roles if necessary. It is part of the manifest import, it maps to existing and if not present creates them.
            //Second Note: This can be made simpler, if we are sure all entities besides the document exist beforehand then the entity reference Id can be used and all other manifests besides the document manifest and the master manifest can be dropped.
            ImportExportSvc.ManifestImport(dsSvr, new[] { doc, doc2, doc3, doc4 }, true);
        }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        public static string GenerateDocumentFromXML(ServerConnectionInformation sci, string contentTypeId, string xmlFileId, string title, string xsltFileId = null, List<dynamic> customFieldValues = null, PageSize ps = PageSize.Letter, bool landscape = false)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        {
            var cfvs = customFieldValues == null ? null : customFieldValues.ToArray();

            var url = WebHelper.GetServerUrl(sci, "Document", "GenerateFromXML", false);
            //Create the basic document creation structure:
            var docCreatePackage = new
            {
                IsDraft = false, //You can create a document as a draft instead of published.
                Document = new { ContentTypeId = contentTypeId },
                Version = new
                {
                    Title = title,
                    Keywords = String.Format("Document Created via Sample API Call On: {0:MMddyyy}", DateTime.Now),
                    Priority = 1, //PriorityLevel.Normal, 0 = Low, 2 = High
                    CustomFieldValues = cfvs //Optional
                },
                XSLTFileId = xsltFileId,
                XMLFileId = xmlFileId,
                //You may want to offer these as options on the Document Type
                PageSize = ps,
                Landscape = landscape
            };
            var dtHndler = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
            };
            var json = JsonConvert.SerializeObject(docCreatePackage, dtHndler);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);

            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return resp["Result"]["Document"]["Id"].ToString();
        }

        /// <summary>
        /// Sample from a search request building document URL's (To page 1 of a Document as an image and to the document directly)
        /// as well as updating the document via an additive method
        /// </summary>
        public static void GenerateImageUrlsAndUpdateAdditiveDocumentFromSearchResult(ServerConnectionInformation sci, JToken searchResult)
        {
            var fields = CollectionGets.CustomFieldMetas(sci);
            var results = (JArray)searchResult["Results"];
            var length = results.Count;
            var slimUpdates = new List<dynamic>(length);
            var varifiedDate = DateTime.Now;
            for (int i = 0; i < length; i++)
            {
                var dfs = Search.GetDynamicFields(results[i]); //Dictionary of all fields that are indexed that are not part of a standard search result.                    
                var token = WebUtility.UrlEncode(sci.Token);
                var versionId = dfs["versionId"][0];

                //Image URL that can be placed in an image tag.
                var imageUrl = $"{sci.ServerUrl}GetFile.ashx?functionName=GetImage&versionId={versionId}&pageNum=1&redacted=true&annotated=false&isNative=false&ds-token={token}";
                //URL allows the user to view a document in the DocStar viewer
                var viewInDocStarURL = $"{sci.WebUrl}#Retrieve/viewByVersionId/{versionId}";
                slimUpdates.Add(new
                {
                    DocumentId = results[i]["Id"].ToString(),
                    VersionId = versionId,
                    ModifiedTicks = dfs["modifiedTicks"][0],
                    CustomFieldValues = new[]
                    {
                            new
                            {
                                CustomFieldMetaId = fields["portalvarified"],
                                CustomFieldName = "PortalVarified",
                                TypeCode = (int)CFTypeCode.Boolean,
                                BoolValue = new bool?(true),
                                DateTimeValue = new DateTime?()
                            },
                            new
                            {
                                CustomFieldMetaId = fields["portalvarifiedon"],
                                CustomFieldName = "PortalVarifiedOn",
                                TypeCode = (int)CFTypeCode.DateTime,
                                BoolValue = new bool?(),
                                DateTimeValue = new DateTime?(varifiedDate)
                            }
                        }
                });
            }

            UpdateManySlim(sci, slimUpdates);
        }
        public static void UpdateManySlim(ServerConnectionInformation sci, List<dynamic> slimUpdates)
        {
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
        /// <summary>
        /// Checks out the specified document, then checks it back in with a new binary for its content.
        /// The original binary is left intact with the prior version and both can be accessed with proper permissions.
        /// </summary>
        public static void CreateNewVersion(ServerConnectionInformation sci, string documentId, string fileId)
        {
            CheckOutDocument(sci, documentId);
            //NOTE: If desired you can make updates to the document version returned in the CheckOutDocument method.
            CheckInDocument(sci, documentId, fileId);
        }
        /// <summary>
        /// Creates a Draft version in the specified document. This draft will track metadata and content changes independent of prior versions.
        /// </summary>
        public static JObject CheckOutDocument(ServerConnectionInformation sci, string documentId)
        {
            var checkOutArgs = new
            {
                DocumentId = documentId,
                VersionComment = "Automated Version checkout via your custom application" //Comment not required
            };

            var url = WebHelper.GetServerUrl(sci, "Document", "CheckOut", false);
            var json = JsonConvert.SerializeObject(checkOutArgs);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return resp;
        }
        /// <summary>
        /// Makes the current documents Draft (CheckOut Version) the published version
        /// NOTE: A check-in does not have to make the draft published, it can simply create a new version entry but still be a draft.
        /// For example, you have 1.0, a check out creates a 1.1 draft version, then a check in with RemainAsDraft = true will create a 1.2 draft version and the 1.1 becomes an archived draft.
        /// This could allow for flows where the prior draft version is deleted, promoted, or just kept as an archive for future review.
        /// </summary>
        public static JObject CheckInDocument(ServerConnectionInformation sci, string documentId, string fileId)
        {
            var checkInArgs = new
            {
                Complete = false, //Applies to Forms
                DocumentId = documentId,
                FileName = fileId,
                //NewDraftOwnerId = "58a64711-7adb-4c19-947d-9b6e665ef1c8", //Only required if you want to change the draft owner.
                RemainAsDraft = false, //If  true the version checked in stays in draft form.
                RestartWorkflow = false, //The new version will restart the current workflow if true
                VersionComment = "Automated Version check-in via your custom application" //Comment, not required
            };
            var url = WebHelper.GetServerUrl(sci, "Document", "CheckIn", false);
            var json = JsonConvert.SerializeObject(checkInArgs);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return resp;
        }

        /// <summary>
        /// Sample from a search request building document URL's (To page 1 of a Document as an image and to the document directly)
        /// as well as updating the document via an update method (adding if not present).
        /// </summary>
        public static void GenerateImageUrlsAndUpdateAllFieldsDocumentFromSearchResult(ServerConnectionInformation sci, JToken searchResult)
        {
            var fields = CollectionGets.CustomFieldMetas(sci);
            var results = (JArray)searchResult["Results"];
            var length = results.Count;
            var varifiedDate = DateTime.Now;
            for (int i = 0; i < length; i++)
            {
                var dfs = Search.GetDynamicFields(results[i]); //Dictionary of all fields that are indexed that are not part of a standard search result.                    
                var token = WebUtility.UrlEncode(sci.Token);
                var versionId = dfs["versionId"][0];

                //Image URL that can be placed in an image tag.
                var imageUrl = $"{sci.ServerUrl}GetFile.ashx?functionName=GetImage&versionId={versionId}&pageNum=1&redacted=true&annotated=false&isNative=false&ds-token={token}";
                //URL allows the user to view a document in the DocStar viewer
                var viewInDocStarURL = $"{sci.WebUrl}#Retrieve/viewByVersionId/{versionId}";
                var docObj = GetDocument(sci, versionId);
                UpdateFields(sci, docObj, new List<Tuple<string, string, CFTypeCode>>
                {
                    new Tuple<string, string, CFTypeCode>(fields["portalvarified"], "true", CFTypeCode.Boolean),
                    new Tuple<string, string, CFTypeCode>(fields["portalvarifiedon"], DateTime.Now.ToString(), CFTypeCode.DateTime),
                });
            }
        }
        public static JObject GetDocument(ServerConnectionInformation sci, string id, bool byVersionId = true)
        {
            var url = WebHelper.GetServerUrl(sci, "Document", byVersionId ? "GetByVersion" : "Get", false);
            var respString = WebHelper.ExecutePost(url, $"\"{id}\"", sci.Token);

            //Keep dates in MSJSON format, we are just going to submit it back to the internal server anyways in the MigrateDocumentToNewSystem sample.
            var dtHndler = new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None
            };

            var resp = (JObject)JsonConvert.DeserializeObject(respString, dtHndler);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return (JObject)resp["Result"];
        }
        public static void UpdateFieldAndFolderInSingleCall(ServerConnectionInformation sci, JObject docPkg, string folderPath, string fieldId, string fieldValue)
        {
            //Slim update can update both folderId's and field values in one transaction.
            var folderId = DSFolder.GetFolderIdByPath(sci, folderPath);
            var version = (JObject)docPkg["Version"];
            var url = WebHelper.GetServerUrl(sci, "Document", "UpdateManySlim", false);
            var documentSlimUpdate = new
            {
                DocumentId = version["DocumentId"].ToString(),
                VersionId = version["Id"].ToString(),
                ModifiedTicks = docPkg["ModifiedTicks"].ToString(),
                FolderIds = new[] { folderId },
                //Title: "",
                //Keywords: "",
                //Priority: "",
                //CutoffDate: "",
                //DueDate: "",
                //InboxId: "",
                //SecurityClassId: "",
                //RecordCategoryId: "",
                //ContentTypeId: "",
                //StartPage: 1,
                //Status: "",
                CustomFieldValues = (JArray)version["CustomFieldValues"]
            };
            var fieldFound = false;
            foreach (JObject cfv in documentSlimUpdate.CustomFieldValues)
            {
                if (cfv["CustomFieldMetaId"].ToString().Equals(fieldId, StringComparison.CurrentCultureIgnoreCase))
                {
                    fieldFound = true;
                    cfv["StringValue"] = fieldValue; //Depending on the field type you will want to set one of these properties: BoolValue, DateTimeValue, DateValue, DecimalValue, IntValue, LongValue, or StringValue. TypeCode can be used to determine this if for your case it is dynamic.
                    break;
                }
            }
            if (!fieldFound)
            {
                //NOTE: My field used in the example is a List type, values for a list type are stored in the String field just as String type custom fields are.
                //Note: List types are a type code of 0 (Object). See type codes at the bottom of this CS file.
                var cfj = String.Format(@"{{
                    CustomFieldMetaId: ""{0}"",
                    StringValue: ""{1}"",
                    TypeCode: 1
                }}", fieldId, fieldValue);
                documentSlimUpdate.CustomFieldValues.Add(JObject.Parse(cfj));
            }

            var json = JsonConvert.SerializeObject(new[] { documentSlimUpdate });
            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());
        }
        public static void MoveToFolder(ServerConnectionInformation sci, string documentId, string folderPath)
        {
            var folderId = DSFolder.GetFolderIdByPath(sci, folderPath);
            var url = WebHelper.GetServerUrl(sci, "Document", "MoveTo", false);
            var moveArgs = new
            {
                DocumentIds = new[] { documentId },
                Type = 1024, //1024 = Folder, 512 = Inbox: Same move call is used for both Folder and Inbox.
                DestinationId = folderId
            };
            var json = JsonConvert.SerializeObject(moveArgs);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

        }
        /// <summary>
        /// Updates a collection of fields on a document, if not present the field is added.
        /// </summary>
        public static void UpdateFields(ServerConnectionInformation sci, JObject docPkg, List<Tuple<string, string, CFTypeCode>> fieldValues)
        {
            var version = (JObject)docPkg["Version"];
            var url = WebHelper.GetServerUrl(sci, "Document", "UpdateManySlim", false);
            var documentSlimUpdate = new
            {
                DocumentId = version["DocumentId"].ToString(),
                VersionId = version["Id"].ToString(),
                ModifiedTicks = docPkg["ModifiedTicks"].ToString(),
                CustomFieldValues = (JArray)version["CustomFieldValues"]
                //FolderIds = new[] { folderId },
                //Title: "",
                //Keywords: "",
                //Priority: "",
                //CutoffDate: "",
                //DueDate: "",
                //InboxId: "",
                //SecurityClassId: "",
                //RecordCategoryId: "",
                //ContentTypeId: "",
                //StartPage: 1,
                //Status: "",
            };
            foreach (var fv in fieldValues)
            {
                var fieldFound = false;
                var key = "StringValue";
                var value = fv.Item2;
                switch (fv.Item3)
                {
                    case CFTypeCode.Boolean:
                        key = "BoolValue";
                        break;
                    case CFTypeCode.Int32:
                        key = "IntValue";
                        break;
                    case CFTypeCode.Int64:
                        key = "LongValue";
                        break;
                    case CFTypeCode.Decimal:
                        key = "DecimalValue";
                        break;
                    case CFTypeCode.DateTime:
                        key = "DateTimeValue";
                        value = JsonConvert.SerializeObject(DateTime.Parse(value), new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.MicrosoftDateFormat });
                        value = value.Substring(2, value.Length - 5) + "/"; //Trim the quotes and extra slashes
                        break;
                    case CFTypeCode.Date:
                        key = "DateValue";
                        value = $"\"{value}\"";
                        break;
                    default:
                        value = $"\"{value}\"";
                        break;
                }
                foreach (JObject cfv in documentSlimUpdate.CustomFieldValues)
                {
                    if (cfv["CustomFieldMetaId"].ToString().Equals(fv.Item1, StringComparison.CurrentCultureIgnoreCase))
                    {
                        fieldFound = true;
                        cfv[key] = value;
                    }
                }
                if (!fieldFound)
                {
                    documentSlimUpdate.CustomFieldValues.Add(JObject.Parse(String.Format(@"{{
                    CustomFieldMetaId: ""{0}"",
                    {3}: {1},
                    TypeCode: {2}
                }}", fv.Item1, value, (int)fv.Item3, key)));
                }
            }
            var json = JsonConvert.SerializeObject(new[] { documentSlimUpdate });
            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());
        }
        public static JObject MigrateDocumentToNewSystem(ServerConnectionInformation sci, JObject docPkg, string[] fileIds, NameIdMaps nim, StringBuilder createMessages)
        {
            var url = WebHelper.GetServerUrl(sci, "Document", "Create", false);
            //Remove Collections we don't want to copy:
            docPkg.Remove("Approvals");
            docPkg.Remove("Folders");
            docPkg.Remove("VersionComments");
            docPkg.Remove("VersionStateInfo");
            docPkg.Remove("Messages");
            docPkg.Remove("WFDocumentDataPackage");
            docPkg.Remove("FormTemplatePackages");
            docPkg.Remove("Packages");
            docPkg.Remove("WorkflowId");
            docPkg.Remove("Inbox");
            docPkg.Remove("RolePermissions");
            docPkg.Remove("UserPermissions");
            //Modify the document for proper import
            var doc = (JObject)docPkg["Document"];
            doc.Remove("Path");
            var name = doc["ContentTypeName"].ToString().ToLower();
            doc["ContentTypeId"] = nim.ContentTypes.ContainsKey(name) ? nim.ContentTypes[name] : nim.ContentTypes.First().Value;
            doc.Remove("ContentTypeName");
            doc.Remove("RecordCategoryId");
            doc.Remove("RecordCategoryName");
            name = doc["SecurityClassName"].ToString().ToLower();
            doc["SecurityClassId"] = nim.SecurityClasses.ContainsKey(name) ? nim.SecurityClasses[name] : nim.SecurityClasses.First().Value;
            doc.Remove("SecurityClassName");
            doc.Remove("ImportJobId");
            //Modify the Version for proper import
            var version = (JObject)docPkg["Version"];
            version.Remove("CreatedBy");
            version.Remove("ApprovalSetId");
            if (version["CustomFieldValues"].HasValues)
            {
                var groupPropsRemoved = false;
                var toBeRemoved = new List<JObject>();
                var customFields = (JArray)version["CustomFieldValues"];
                foreach (JObject cf in customFields)
                {
                    name = cf["CustomFieldName"].ToString().ToLower();
                    //NOT IMPLEMENTED - CUSTOM FIELD GROUPS
                    if (!String.IsNullOrEmpty(cf["CustomFieldGroupId"].ToString()))
                    {
                        cf.Remove("CustomFieldGroupId");
                        cf.Remove("CustomFieldGroupTemplateId");
                        cf.Remove("SetId");
                        cf.Remove("ParentId");
                        cf.Remove("CustomFieldGroupOrder");
                        cf.Remove("CustomFieldGroupName");
                        groupPropsRemoved = true;
                    }
                    if (nim.CustomFieldMetas.ContainsKey(name))
                        cf["CustomFieldMetaId"] = nim.CustomFieldMetas[name];
                    else
                    {
                        if (!toBeRemoved.Any())
                            createMessages.Append("Removed Fields: ");
                        createMessages.Append($"{name}, ");

                        toBeRemoved.Add(cf);
                    }
                }
                foreach (var cf in toBeRemoved)
                    cf.Remove();
                if (groupPropsRemoved)
                {
                    if (toBeRemoved.Any())
                        createMessages.AppendLine();
                    createMessages.AppendLine("Documents with groups found, they may have been imported but their group relationship has been removed");
                }
            }
            //Modify the Content Item collection to have the new file ids
            var cis = (JArray)docPkg["ContentItems"];
            var i = 0;
            foreach (JObject ci in cis)
            {
                ci["FileName"] = fileIds[i++];
                ci["ImageState"] = 0;
                ci.Remove("Authentidate");
                ci.Remove("CreatedBy");
                ci.Remove("ModifiedBy");
                ci.Remove("Hash");
                ci.Remove("FormPartId");
            }
            var saveOptions = (JObject)JsonConvert.DeserializeObject(@"
            {
                ReturnUpdated: true,
                TriggerWorkflow: false,
                IndexOperations: 3,
                AddToDistributedQueue: true,
                DQSortDateOffsetHours: 0,
                DQPriorityOffset: 0,
                UseContentTypeDefaultFolders: false,
                UseContentTypeDefaultInbox: false,
                UseContentTypeDefaultWorkflow: false,
                GenerateContentHashOnCreate: true,
                ReconcileBookmarksOnUpdate: false
            }
            ");
            docPkg.Add("SaveOptions", saveOptions);
            var json = JsonConvert.SerializeObject(docPkg);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);

            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return (JObject)resp["Result"];
        }
        public static void DeleteDocument(ServerConnectionInformation sci, string[] documentIds, bool hardDelete)
        {
            var url = WebHelper.GetServerUrl(sci, "Document", hardDelete ? "HardDelete" : "SoftDelete", false);
            var docIds = new[] { documentIds };
            var json = JsonConvert.SerializeObject(docIds);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());
        }
        public static void DeleteDocumentInSearchResult(ServerConnectionInformation sci, JArray searchResults, bool hardDelete)
        {
            var url = WebHelper.GetServerUrl(sci, "Document", hardDelete ? "HardDelete" : "SoftDelete", false);
            var docIds = new List<string>();

            var count = searchResults.Count;
            var slimUpdates = new List<dynamic>(count);
            for (int i = 0; i < count; i++)
            {
                docIds.Add(searchResults[i]["Id"].ToString());
            }
            var json = JsonConvert.SerializeObject(docIds);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());
        }
        /// <summary>
        /// Creates a Form Document given a Form Template Name
        /// </summary>
        public static Guid CreateFormDocument(ServerConnectionInformation sci, string formTemplateName)
        {
            var formTemplates = CollectionGets.FormTemplates(sci);
            formTemplateName = formTemplateName.ToLower();
            if (!formTemplateName.Contains(formTemplateName))
                throw new Exception($"The form template {formTemplateName} does not exist");

            var ftId = formTemplates[formTemplateName];
            var url = WebHelper.GetServerUrl(sci, "Forms", "CreateDocument", false);
            var formCreateArgs = new
            {
                FormTemplateId = ftId,
                //FolderIds = new [] { "" },
                //InboxId = "",
                //WorkflowId = "",
                //IsTemporary = false //Flag indicating that the form is not to be rendered or added to the index. It is made permanent by executing SubmitTemporaryDocument against the Document Service.
            };

            var json = JsonConvert.SerializeObject(formCreateArgs);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);

            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            //Update the Document to have the meta you want:
            var versionId = resp["Result"]["Version"]["Id"].ToString();
            var docId = resp["Result"]["Document"]["Id"].ToString();
            var ticks = resp["Result"]["ModifiedTicks"].ToString();
            var cfvs = new[]
            {
                //All of these fields must already exist on the server.
                CreateFieldValue(sci, "ReadyToFulfill", boolValue: true),
                CreateFieldValue(sci, "Description", stringValue: "Merchant"),
                CreateFieldValue(sci, "Division", intValue: 12345),
                CreateFieldValue(sci, "ErpUploadDate", datetimeValue: DateTime.Now)
            };
            var slimUpdates = new List<dynamic>
            {
                new
                {
                    DocumentId = docId,
                    VersionId = versionId,
                    ModifiedTicks = ticks,
                    CustomFieldValues = cfvs
                }
            };
            url = WebHelper.GetServerUrl(sci, "Document", "UpdateManySlimEx", false);
            json = JsonConvert.SerializeObject(new
            {
                OnlyModifiedCustomFields = true,
                DocumentSlimUpdates = slimUpdates
            }, new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.MicrosoftDateFormat });
            respString = WebHelper.ExecutePost(url, json, sci.Token);
            resp = (JObject)JsonConvert.DeserializeObject(respString);
            error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return new Guid(versionId);
        }
        /// <summary>
        /// Creates a document based on the uploaded file(s) (See UploadFile.cs).
        /// NOTE: Security to the created document is based on the selected security class, in addition the creating user will have full control over the document regardless of the rights from the security class.
        /// </summary>
        public static JObject CreateDocument(ServerConnectionInformation sci, string[] fileIds, List<dynamic> customFieldValues = null, string contentTypeId = null, string securityClassId = null)
        {
            //The following can be static if you know the ids, or may come from a prompt in your interface.
            var cts = CollectionGets.ContentTypes(sci);
            if (String.IsNullOrEmpty(contentTypeId))
                contentTypeId = cts.First().Value;
            var cfvs = customFieldValues == null ? null : customFieldValues.ToArray();

            var url = WebHelper.GetServerUrl(sci, "Document", "Create", false);
            //Create the basic document creation structure:
            var docCreatePackage = new
            {
                IsDraft = false, //You can create a document as a draft instead of published.
                Document = new { ContentTypeId = contentTypeId, SecurityClassId = securityClassId /*Optional but recommended (otherwise only the user creating the document has access)*/ },
                Version = new
                {
                    Title = String.Format("Created On: {0:MMDDyyy}", DateTime.Now),
                    Keywords = "Document Created via Sample API Call",
                    Priority = 1, //PriorityLevel.Normal, 0 = Low, 2 = High
                    CustomFieldValues = cfvs //Optional
                },
                //WorkflowId = workflowId, //Optionally add to a workflow on creation
                //InboxId = inboxeId, //Optionally add to an inbox on creation
                //FolderIds = new Guid[] { folderId }, //Optionally add to a folder on creation
                SaveOptions = new //Optional but if you want the document imaged you need this to set AddToDistributedQueue = true.
                {
                    AddToDistributedQueue = true,
                    IndexOperations = 1, //IndexOperations.UpdateCreate,
                    TriggerWorkflow = true,
                    UseContentTypeDefaultFolders = true,
                    UseContentTypeDefaultInbox = true,
                    GenerateContentHashOnCreate = true,
                    ReturnUpdated = true
                },
                ContentItems = fileIds.Select(f => new { FileName = f }).ToArray()
            };
            var dtHndler = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
            };
            var json = JsonConvert.SerializeObject(docCreatePackage, dtHndler);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);

            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return (JObject)resp["Result"];
        }
        public static JObject CreateWithGroupCustomFieldValues(ServerConnectionInformation sci, string[] fileIds)
        {
            var cfgs = CollectionGets.CustomFieldGroups(sci);
            //These fields will be standard fields, not grouped at all.
            var cfvs = new List<dynamic>
            {
                //NOTE: All the following field must already exist on the DocStar server.
                CreateFieldValue(sci, "ReadyToFulfill", boolValue: true),
                CreateFieldValue(sci, "Description", stringValue: "Merchant"),
                CreateFieldValue(sci, "Division", intValue: 12345),
                CreateFieldValue(sci, "ErpUploadDate", datetimeValue: DateTime.Now)
            };
            //NOTE: the following assumes you have a group defined (HeaderMiscCharges) with MISC_MiscCode, MISC_Desc, MISC_Percentage, MISC_MiscAmt, MISC_Freq 
            //NOTE: HeaderMiscCharges also contains MISC_Percent, I am not using that field in this case so it is omittied from the AddRow calls.
            var groupDef = CustomField.GetGroupDefinition(sci, cfgs["headermisccharges"]);
            var td = CustomField.GetTemplateDict(groupDef);
            CustomField.AddRow(cfvs, td, new[]
            {
                CreateFieldValue(sci, "MISC_MiscCode", stringValue: "F"),
                CreateFieldValue(sci, "MISC_Desc", stringValue: "Freight"),
                CreateFieldValue(sci, "MISC_MiscAmt", decimalValue: 28.52m),
                CreateFieldValue(sci, "MISC_Freq", listValue: "First")
            });
            CustomField.AddRow(cfvs, td, new[]
            {
                CreateFieldValue(sci, "MISC_MiscCode", stringValue: "T"),
                CreateFieldValue(sci, "MISC_Desc", stringValue: "Taxes"),
                CreateFieldValue(sci, "MISC_MiscAmt", decimalValue: 8.22m),
                CreateFieldValue(sci, "MISC_Freq", listValue: "First")
            });
            CustomField.AddRow(cfvs, td, new[]
            {
                CreateFieldValue(sci, "MISC_MiscCode", stringValue: "H"),
                CreateFieldValue(sci, "MISC_Desc", stringValue: "Handling"),
                CreateFieldValue(sci, "MISC_MiscAmt", decimalValue: 14.10m),
                CreateFieldValue(sci, "MISC_Freq", listValue: "First")
            });
            return CreateDocument(sci, fileIds, cfvs);
        }

        private static Dictionary<string, string> fieldLookup = new Dictionary<string, string>();
        public static dynamic CreateFieldValue(ServerConnectionInformation sci, string fieldName, DateTime? datetimeValue = null,
            bool? boolValue = null, string stringValue = null, int? intValue = null, decimal? decimalValue = null,
            string listValue = null, string dateValue = null, JObject groupDef = null, string setId = null)
        {
            if (fieldLookup.Count == 0)
                fieldLookup = CollectionGets.CustomFieldMetas(sci);

            var tc = datetimeValue.HasValue ? CFTypeCode.DateTime : boolValue.HasValue ? CFTypeCode.Boolean : intValue.HasValue ? CFTypeCode.Int32 : decimalValue.HasValue ? CFTypeCode.Decimal : !String.IsNullOrEmpty(dateValue) ? CFTypeCode.Date : string.IsNullOrEmpty(listValue) ? CFTypeCode.String : CFTypeCode.Object;
            var lowerFieldName = fieldName.ToLower();
            if (!fieldLookup.ContainsKey(lowerFieldName))
            {
                var newField = CustomField.Create(sci, fieldName, tc);
                fieldLookup.Add(lowerFieldName, newField["Id"].ToString());
            }

            string fieldGroupId = null;
            string templateId = null;
            if (groupDef != null)
            {
                foreach (JObject template in (JArray)groupDef["CustomFieldGroupTemplates"])
                {
                    if (template["CustomFieldMetaId"].ToString().Equals(fieldLookup[lowerFieldName]))
                    {
                        templateId = template["Id"].ToString();
                        fieldGroupId = groupDef["CustomFieldGroup"]["Id"].ToString();
                        break;
                    }
                }
            }

            return new
            {
                CustomFieldMetaId = fieldLookup[lowerFieldName],
                CustomFieldName = fieldName,
                TypeCode = (int)tc,
                BoolValue = boolValue,
                DateTimeValue = datetimeValue,
                IntValue = intValue,
                StringValue = stringValue ?? listValue,
                DecimalValue = decimalValue,
                DateValue = dateValue,
                CustomFieldGroupId = fieldGroupId,
                SetId = setId,
                CustomFieldGroupTemplateId = templateId
            };
        }

        public static void PurgeRecycleBin(ServerConnectionInformation sci, DateTime cutoffDate)
        {
            var url = WebHelper.GetServerUrl(sci, "Document", "GetDeleted", false);
            var getObj = new
            {
                MaxRows = 100, //Paged result set, use Start to get page 1 (0), page 2 (100), and so on.
                SortOrder = "asc", //Valid values: asc, desc
                SortedBy = "CreatedOn", //Valid values: ModifiedOn, CreatedOn
                Start = 0
            };
            var json = JsonConvert.SerializeObject(getObj);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            var idsToBeDeleted = new List<string>();
            var results = (JArray)resp["Result"]["Results"];
            foreach (JObject doc in results)
            {
                var dt = doc["CreatedOn"].Value<DateTime>();
                if (dt < cutoffDate)
                    idsToBeDeleted.Add(doc["Id"].ToString());
            }

            if (!idsToBeDeleted.Any())
                return; //early exit.


            url = WebHelper.GetServerUrl(sci, "Document", "HardDelete", false);
            json = JsonConvert.SerializeObject(idsToBeDeleted);
            respString = WebHelper.ExecutePost(url, json, sci.Token);
            resp = (JObject)JsonConvert.DeserializeObject(respString);
            error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

        }
    }
}
