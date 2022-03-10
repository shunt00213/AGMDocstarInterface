using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    public static class CollectionGets
    {
        public static NameIdMaps GetNameIdMap(ServerConnectionInformation sci)
        {
            var nim = new NameIdMaps();
            nim.ContentTypes = ContentTypes(sci);
            nim.SecurityClasses = SecurityClasses(sci);
            nim.CustomFieldMetas = CustomFieldMetas(sci);

            return nim;
        }
        public static Dictionary<string ,string> SecurityClasses(ServerConnectionInformation sci)
        {
            var url = WebHelper.GetServerUrl(sci, "Security", "GetAllSecurityClassesSlim", false);
            var respString = WebHelper.ExecutePost(url, null, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            var securityClasses = new Dictionary<string, string>();
            var scs = (JArray)resp["Result"];
            foreach (var sc in scs)
            {
                securityClasses.Add(sc["Name"].ToString().ToLower(), sc["Id"].ToString());
            }
            return securityClasses;
        }
        public static Dictionary<string, string> ContentTypes(ServerConnectionInformation sci)
        {
            var url = WebHelper.GetServerUrl(sci, "ContentType", "GetContentTypesSlim", false);
            var respString = WebHelper.ExecutePost(url, null, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            var contentTypes = new Dictionary<string, string>();
            var scs = (JArray)resp["Result"];
            foreach (var sc in scs)
            {
                contentTypes.Add(sc["Name"].ToString().ToLower(), sc["Id"].ToString());
            }
            return contentTypes;
        }
        public static Dictionary<string, string> CustomFieldMetas(ServerConnectionInformation sci)
        {
            var url = WebHelper.GetServerUrl(sci, "CustomField", "GetCustomFieldsSlim", false);
            var respString = WebHelper.ExecutePost(url, null, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            var customFields = new Dictionary<string, string>();
            var scs = (JArray)resp["Result"];
            foreach (var sc in scs)
            {
                customFields.Add(sc["Name"].ToString().ToLower(), sc["Id"].ToString());
            }
            return customFields;
        }

        public static Dictionary<string, string> FormTemplates(ServerConnectionInformation sci)
        {
            var url = WebHelper.GetServerUrl(sci, "Forms", "GetTemplatesSlim", false);
            var respString = WebHelper.ExecutePost(url, null, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            var formTemplates = new Dictionary<string, string>();
            var fts = (JArray)resp["Result"];
            foreach (var ft in fts)
            {
                formTemplates.Add(ft["Name"].ToString().ToLower(), ft["FormTemplateId"].ToString());
            }
            return formTemplates;
        }
        public static Dictionary<string, string> CustomFieldGroups(ServerConnectionInformation sci)
        {
            var url = WebHelper.GetServerUrl(sci, "CustomField", "GetGroupsSlim", false);
            var respString = WebHelper.ExecutePost(url, null, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            var groups = new Dictionary<string, string>();
            var gps = (JArray)resp["Result"];
            foreach (var gp in gps)
            {
                groups.Add(gp["Name"].ToString().ToLower(), gp["Id"].ToString());
            }
            return groups;
        }
        internal static Dictionary<string, string> LDAPAuthenticationProviders(ServerConnectionInformation sci)
        {
            var url = WebHelper.GetServerUrl(sci, "AuthenticationProvider", "GetSlimLDAP", true);
            var respString = WebHelper.ExecutePost(url, null, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            var providers = new Dictionary<string, string>();
            var ldps = (JArray)resp["Result"];
            foreach (var ld in ldps)
            {
                providers.Add(ld["Name"].ToString().ToLower(), ld["Id"].ToString());
            }
            return providers;
        }
    }
}
