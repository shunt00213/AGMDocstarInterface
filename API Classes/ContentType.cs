using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AGMDocstarInterface
{
    public class ContentType
    {
        public static JObject Get(ServerConnectionInformation sci, string contentTypeId)
        {
            var url = WebHelper.GetServerUrl(sci, "ContentType", "Get", false);
            var respString = WebHelper.ExecutePost(url, $"\"{contentTypeId}\"", sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return (JObject)resp["Result"];
        }
        internal static void EmitDefaultFields(ServerConnectionInformation sci, JObject contentType)
        {
            //There are 2 collections that define a content types default fields:
            //1) DefaultCustomFields - These are header level fields.
            //2) DefaultCustomFieldGroups -- These are the default line items for the group. 
            //  **This is a new feature, most existing customers will not have content types that define these yet. In this case you should offer a selection from all groups. See CollectionGets.CustomFieldGroups

            var cfms = CollectionGets.CustomFieldMetas(sci).ToDictionary(k => k.Value, v => v.Key); //Inverse the key and value so we can emit the name of the field.
            Console.WriteLine($"Content Type: {contentType["Name"]}:");

            //Default Fields:
            Console.WriteLine("   Default Fields:");
            foreach (JObject df in (JArray)contentType["DefaultCustomFields"])
            {
                var fieldName = cfms[df["CustomFieldMetaID"].ToString()];
                Console.WriteLine($"     {fieldName}");
            }

            //Default Field Groups (Line Items):
            Console.WriteLine("   Default Field Groups:");
            foreach (JObject df in (JArray)contentType["DefaultCustomFieldGroups"])
            {
                var groupDef = CustomField.GetGroupDefinition(sci, df["CustomFieldGroupId"].ToString());
                var groupName = groupDef["CustomFieldGroup"]["Name"];
                Console.WriteLine($"     {groupName}");
                //Display each field in the group (Columns):
                foreach (var column in (JArray)groupDef["CustomFieldGroupTemplates"])
                {
                    var fieldName = cfms[column["CustomFieldMetaId"].ToString()];
                    Console.WriteLine($"         {fieldName}");
                }
            }

        }
        /// <summary>
        /// Will check for the existence of a content type, if it exists nothing is changed.
        /// If it does not it will be created, if the security class passed does not exist it to will be created.
        /// All groups and fields must be created prior to executing this method.
        /// </summary>
        public static string EnsureExists(ServerConnectionInformation sci, string contentTypeName, string securityClass, string[] defaultFields, string[] defaultFieldGroups)
        {
            var existingContentTypes = CollectionGets.ContentTypes(sci);
            if (existingContentTypes.ContainsKey(contentTypeName.ToLower()))
                return existingContentTypes[contentTypeName.ToLower()]; //Already Exists, we could update the default fields but that is not the intent of this method at this time.

            var existingFields = CollectionGets.CustomFieldMetas(sci);
            var existingGroups = CollectionGets.CustomFieldGroups(sci);

            var dfs = new List<dynamic>();
            if (defaultFields != null)
            {
                foreach (var item in defaultFields)
                {
                    if (!existingFields.ContainsKey(item.ToLower()))
                    {
                        Console.WriteLine($"The field {item} was not found and will be skipped as a default field to {contentTypeName}");
                        continue;
                    }
                    dfs.Add(new { CustomFieldMetaID = existingFields[item.ToLower()] });
                }
            }

            var dfgs = new List<dynamic>();
            if (defaultFieldGroups != null)
            {
                foreach (var item in defaultFieldGroups)
                {
                    if (!existingGroups.ContainsKey(item.ToLower()))
                    {
                        Console.WriteLine($"The field group {item} was not found and will not be added as a default field group to {contentTypeName}");
                        continue;
                    }
                    dfgs.Add(new { CustomFieldGroupId = existingGroups[item.ToLower()]});
                }
            }

            var scId = SecurityClass.EnsureSecurityClassExists(sci, securityClass);
            var ct = new
            {
                Name = contentTypeName,
                SecurityClassId = scId,
                DefaultSecurityClassId = scId, //DefaultSecurityClassId = the security class of a document when imported with this content type and no overriding security class is specified.
                DefaultCustomFields = dfs,
                DefaultCustomFieldGroups = dfgs
            };
            
            var url = WebHelper.GetServerUrl(sci, "ContentType", "Create", false);
            var json = JsonConvert.SerializeObject(ct);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return resp["Result"]["Id"].ToString();
        }
    }
}