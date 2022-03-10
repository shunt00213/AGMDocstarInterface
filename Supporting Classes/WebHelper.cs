using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    static class WebHelper
    {
        public const string HOST_SERVICE_URL = "{0}HostingV2/{1}.svc/rest{2}/{3}";
        public const string INSTANCE_SERVICE_URL = "{0}AstriaV2/{1}.svc/rest{2}/{3}";
        public static string ExecutePost(string url, string json, string token = null, bool bypassOverrideErrors = false)
        {
            WebRequest wr = WebRequest.Create(url);
            wr.Method = "POST";
            wr.ContentType = "application/json";
            if (!String.IsNullOrEmpty(token))
            {
                wr.Headers.Add(Constants.TOKENHEADER, token);
            }
            if (bypassOverrideErrors)
            {
                wr.Headers.Add(Constants.OPTIONSHEADER, "2");
            }
            //This header will identify this call as a 'Service' so it doesn't use the concurrent licensing model.
            wr.Headers.Add(Constants.SOURCEHEADER, Constants.SERVICE_SOURCE);
            byte[] postBytes = json == null ? new byte[0] : Encoding.UTF8.GetBytes(json);
            wr.ContentLength = postBytes.Length;
            wr.Timeout = (int)TimeSpan.FromSeconds(300).TotalMilliseconds;
            var requestStream = wr.GetRequestStream();
            requestStream.Write(postBytes, 0, postBytes.Length);
            requestStream.Close();

            
            var response = (HttpWebResponse)wr.GetResponse();
            string respString;
            using (var rdr = new StreamReader(response.GetResponseStream()))
            {
                respString = rdr.ReadToEnd();
            }

            return respString;
        }
        public static string GetServerUrl(ServerConnectionInformation sci, string service, string method, bool hostService)
        {
            if (String.IsNullOrWhiteSpace(sci.ServerUrl))
                GetUrlsFromWebServerData(sci);
            var ssl = sci.ServerUrl.ToLower().StartsWith("https");
            var baseAddress = hostService ? HOST_SERVICE_URL : INSTANCE_SERVICE_URL;
            return String.Format(baseAddress, sci.ServerUrl, service, ssl ? "ssl" : "", method);
        }

        public static void GetUrlsFromWebServerData(ServerConnectionInformation sci)
        {
            var responseString = "";

            using (var wclient = new WebClient())
            {
                if (!String.IsNullOrWhiteSpace(sci.Token))
                {
                    wclient.Headers.Add(Constants.TOKENHEADER, sci.Token);
                }
                if (!sci.WebUrl.EndsWith("/"))
                    sci.WebUrl += "/";
                responseString = wclient.DownloadString($"{sci.WebUrl}BasicInfo/GetWebServerData");
                var resp = (JObject)JsonConvert.DeserializeObject(responseString);
                var proxy = false;
                bool.TryParse(resp["Proxy"].ToString(), out proxy);
                sci.Proxy = proxy;

                var isToken = !String.IsNullOrWhiteSpace(sci.Token);
                var error = resp["Exception"];
                if ((!proxy || isToken) && error.HasValues)
                    throw new Exception(error["Message"].ToString());

                if (!proxy || isToken) //If the URL we have is the proxy, we don't want to update the weburl and server url. Once logged in and we have a token we will recieve the web url for the zone we will be communicating with.
                {
                    sci.WebUrl = resp["ZoneUrl"].ToString();
                    sci.ServerUrl = resp["ServerUrl"].ToString();
                }
                if (!sci.WebUrl.EndsWith("/"))
                    sci.WebUrl += "/";
                if (sci.ServerUrl != null && !sci.ServerUrl.EndsWith("/"))
                    sci.ServerUrl += "/";
            }

        }
    }
}
