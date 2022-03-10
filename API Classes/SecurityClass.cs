using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    public static class SecurityClass
    {
        internal static object EnsureSecurityClassExists(ServerConnectionInformation sci, string securityClass)
        {
            var existing = CollectionGets.SecurityClasses(sci);
            if (existing.ContainsKey(securityClass.ToLower()))
                return existing[securityClass.ToLower()];

            
            var url = WebHelper.GetServerUrl(sci, "Security", "CreateSecurityClass", false);
            var sc = new { Name = securityClass };
            var json = JsonConvert.SerializeObject(sc);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return resp["Result"]["Id"].ToString();
        }
    }
}
