using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    class Role
    {
        internal static JObject Create(ServerConnectionInformation sci, string roleName)
        {
            var url = WebHelper.GetServerUrl(sci, "Security", "CreateRole", false);
            var json = JsonConvert.SerializeObject(new
            {
                Name = roleName
            });

            var respString = WebHelper.ExecutePost(url, json, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());
            
            return (JObject)resp["Result"];
        }
    }
}
