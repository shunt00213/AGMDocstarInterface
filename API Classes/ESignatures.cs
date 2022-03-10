using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    public static class ESignatures
    {
        public static void UpdateStatus(ServerConnectionInformation sci, string versionId, JObject settings)
        {
            var docPkg = DSDocument.GetDocument(sci, versionId, true);
            if (docPkg == null)
                return;

            var sigPkgs = docPkg["SigPackages"];
            if (sigPkgs == null)
                return;

            var envelopIds = new List<string>();
            foreach (JObject sig in (JArray)sigPkgs)
            {
                envelopIds.Add(sig["ProviderId"].ToString());
            }
            if (!envelopIds.Any())
                return;

            var args = new
            {
                ProviderIds = envelopIds
            };
            
            var url = WebHelper.GetServerUrl(sci, "ESign", "UpdateStatus", false);
            var json = JsonConvert.SerializeObject(args);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());
        }
        public static JObject GetSettings(ServerConnectionInformation sci)
        {
            var url = WebHelper.GetServerUrl(sci, "ESign", "GetDocuSignSettings", false);
            var respString = WebHelper.ExecutePost(url, null, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return (JObject)resp["Result"];
        }
    }
}
