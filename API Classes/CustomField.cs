using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    public static class CustomField
    {
        public static void EnsureFieldsExist(ServerConnectionInformation sci, List<KeyValuePair<string, CFTypeCode>> fields)
        {
            var existingFields = CollectionGets.CustomFieldMetas(sci);
            foreach (var f in fields)
            {
                if (existingFields.ContainsKey(f.Key.ToLower()))
                    continue;
                Create(sci, f.Key, f.Value);
            }
        }
        public static JObject Create(ServerConnectionInformation sci, string fieldName, CFTypeCode fieldType)
        {
            var url = WebHelper.GetServerUrl(sci, "CustomField", "SetCustomField", false);
            var json = JsonConvert.SerializeObject(new
            {
                Name = fieldName,
                Type = (int)fieldType
            });

            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return (JObject)resp["Result"];
        }
        public static JObject GetGroupDefinition(ServerConnectionInformation sci, string groupId)
        {
            var url = WebHelper.GetServerUrl(sci, "CustomField", "GetGroup", false);
            var respString = WebHelper.ExecutePost(url, $"\"{groupId}\"", sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            var groups = new Dictionary<string, string>();
            return (JObject)resp["Result"];
        }
        public static Dictionary<string, string> GetTemplateDict(JObject groupDef)
        {
            var templateDict = new Dictionary<string, string>();
            var templates = (JArray)groupDef["CustomFieldGroupTemplates"];
            foreach (var t in templates)
            {
                templateDict.Add(t["CustomFieldMetaId"].ToString(), t["Id"].ToString());
            }
            return templateDict;
        }
        public static void AddRow(List<dynamic> cfs, Dictionary<string, string> groupTemplates, dynamic[] rowValues)
        {
            var setId = Constants.NewSeq(); //Sequential Guid, controls default order of each line.
            foreach (var row in rowValues)
            {
                if (groupTemplates.ContainsKey(row.CustomFieldMetaId))
                {
                    cfs.Add(new
                    {
                        row.CustomFieldMetaId,
                        row.CustomFieldName,
                        row.TypeCode,
                        row.BoolValue,
                        row.DateTimeValue,
                        row.IntValue,
                        row.StringValue,
                        row.DecimalValue,
                        CustomFieldGroupTemplateId = groupTemplates[row.CustomFieldMetaId], //filled out when making a field part of a group
                        SetId = setId
                    });
                }
                else
                    cfs.Add(row); //Not part of the group.
            }

        }
        public static void EnsureFieldGroupsExist(ServerConnectionInformation sci, Dictionary<string, string[]> groups)
        {
            var existingGroups = CollectionGets.CustomFieldGroups(sci);
            var existingFields = CollectionGets.CustomFieldMetas(sci);
            foreach (var g in groups)
            {
                if (existingGroups.ContainsKey(g.Key.ToLower()))
                    continue;

                CreateGroup(sci, g.Key, g.Value, existingGroups, existingFields);
            }
        }

        private static void CreateGroup(ServerConnectionInformation sci, string groupName, string[] fieldNames, Dictionary<string, string> existingGroups = null, Dictionary<string, string> existingFields = null)
        {
            if (existingFields == null)
                existingFields = CollectionGets.CustomFieldMetas(sci);
            if (existingGroups == null)
                existingGroups = CollectionGets.CustomFieldGroups(sci);

            if (existingGroups.ContainsKey(groupName.ToLower()))
                return;

            var templates = new List<dynamic>();
            var i = 0;
            foreach (var f in fieldNames)
            {
                if (!existingFields.ContainsKey(f.ToLower()))
                {
                        Console.WriteLine($"The field {f} was not found and will not be added to group {groupName}");
                        continue;
                }
                templates.Add(new { CustomFieldMetaId = existingFields[f.ToLower()], Order = i });
                i++;
            }


            var url = WebHelper.GetServerUrl(sci, "CustomField", "CreateGroup", false);
            var json = JsonConvert.SerializeObject(new
            {
                CustomFieldGroup = new { Name = groupName },
                CustomFieldGroupTemplates = templates
            });

            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());
        }
    }
}
