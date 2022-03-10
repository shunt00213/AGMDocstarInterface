using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    public static class DSUser
    {
        public static JArray GetAll(ServerConnectionInformation sci)
        {
            var url = WebHelper.GetServerUrl(sci, "User", "GetAll", true);
            var respString = WebHelper.ExecutePost(url, null, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return (JArray)resp["Result"];
        }
        public static Dictionary<string, string> GetIntegratedUsers(JArray users, string authProviderId)
        {
            var ia = new Dictionary<string, string>();
            foreach (var u in users)
            {
                if (u["ConnectionId"].Type != JTokenType.Null && u["DistinguishedName"].Type != JTokenType.Null && u["ConnectionId"].ToString().Equals(authProviderId, StringComparison.CurrentCultureIgnoreCase))
                {
                    ia.Add(u["DistinguishedName"].ToString(), u["Username"].ToString());
                }
            }
            return ia;
        }
    }
}
