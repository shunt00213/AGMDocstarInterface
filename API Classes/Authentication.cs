using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    static class Authentication
    {
        /// <summary>
        /// Authenticates a users credentials.
        /// This method will work for both Hosted and On Premise.
        /// If your integration is only for on premise you can simplify this method by only using the code within the on premise branch.
        /// </summary>
        public static void Login(ServerConnectionInformation sci, string password)
        {
            sci.Token = null; //ensure this is null. GetUrlsFromWebServerData may return different data depending on the presents of a token.
            WebHelper.GetUrlsFromWebServerData(sci);
            var reqparm = new NameValueCollection();
            reqparm.Add("userName", sci.UserName);
            reqparm.Add("password", password);
            reqparm.Add("rememberMe", false.ToString());
            reqparm.Add("proxyAccount", false.ToString());

            if (sci.Proxy) //Authentication Proxy, Hosted
            {
                using (var wc = new WebClient())
                {
                    var loginUri = $"{sci.WebUrl}Proxy/APILogin";
                    //This header will identify this call as a 'Service' so it doesn't use the concurrent licensing model.
                    wc.Headers.Add(Constants.SOURCEHEADER, Constants.SERVICE_SOURCE);
                    var responseBytes = wc.UploadValues(loginUri, "POST", reqparm);
                    var responseString = Encoding.UTF8.GetString(responseBytes);
                    var dummyLoginObj = new { Result = new[] { new { WebUrl = "", DisplayValue = "", LogInPackageSR = new { Error = new { Message = "" }, Result = new { Token = "" } } } }, Error = new { Message = "" } };
                    var loginResponse = JsonConvert.DeserializeAnonymousType(responseString, dummyLoginObj);
                    if (loginResponse.Error != null)
                        throw new Exception(loginResponse.Error.Message);

                    if (loginResponse.Result == null || !loginResponse.Result.Any() || loginResponse.Result[0].LogInPackageSR == null)
                        throw new Exception("No Login Results Returned"); //Should not happen.

                    //It is possible that when you login you have access to multiple zones. For that case you would likely want to prompt the user to choose which one they want to connect to.
                    //For this sample we are going to just select the first.
                    sci.WebUrl = loginResponse.Result[0].WebUrl;
                    sci.Token = loginResponse.Result[0].LogInPackageSR.Result.Token;
                    sci.ServerUrl = null;
                    WebHelper.GetUrlsFromWebServerData(sci);
                }
            }
            else //On Premise
            {
                var url = WebHelper.GetServerUrl(sci, "user", "LogIn", true);
                var json = JsonConvert.SerializeObject(new { Username = sci.UserName, Password = password });
                //Execute Post will identify this call as a 'Service' so it doesn't use the concurrent licensing model.
                var respString = WebHelper.ExecutePost(url, json);
                var resp = (JObject)JsonConvert.DeserializeObject(respString);
                var error = resp["Error"];
                if (error.HasValues)
                    throw new Exception(error["Message"].ToString());

                sci.Token = resp["Result"]["Token"].ToString();
            }
        }
        /// <summary>
        /// Logs a user out and releases their license.
        /// This should be used if you are not using a service based account (See WebHelper.ExecutePost)
        /// </summary>
        /// <param name="sci"></param>
        public static void Logout(ServerConnectionInformation sci)
        {
            if (String.IsNullOrWhiteSpace(sci.Token))
                return; //No token, can't logout.

            var url = WebHelper.GetServerUrl(sci, "user", "LogOut", true);
            var respString = WebHelper.ExecutePost(url, null, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            sci.Token = null;
        }
        /// <summary>
        /// Validates a token by attempting to get the current tokens user information.
        /// </summary>
        public static bool IsTokenValid(ServerConnectionInformation sci)
        {
            if (String.IsNullOrWhiteSpace(sci.Token))
                return false;

            var url = WebHelper.GetServerUrl(sci, "user", "GetCurrent", true);
            var respString = WebHelper.ExecutePost(url, null, sci.Token);
            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            return !error.HasValues;
        }
        /// <summary>
        /// Uses Login method above if a token has not been saved to disk or the token on disk is bad.
        /// This is a simple example of how to us a single login for server side processing of user requests.
        /// </summary>
        public static void GetToken(ServerConnectionInformation sci, string password)
        {
            var uri = new Uri(sci.WebUrl);
            var file = $"{sci.UserName}-{uri.Host}.txt";
            if (File.Exists(file))
            {
                sci.Token = File.ReadAllText(file);
                var valid = IsTokenValid(sci); //test if token is valid.
                if (valid)
                    return; //early exit we have a good token.
                else
                    sci.Token = null;
            }
            Login(sci, password);
            File.WriteAllText(file, sci.Token);
        }
        /// <summary>
        /// SSO Login Example
        /// Untested to date
        /// </summary>
        public static void SSOLogin(ServerConnectionInformation sci)
        {
            var iaUri = "";
            var wsd = LoadWebServerData(sci.WebUrl);
            if (wsd.Proxy) //If we connected to a proxy then we will have an exception but it just that you cannot get all the data without a token, login and make the call again.
                iaUri = $"{sci.WebUrl}/IntegratedAuthentication.ashx";
            else
            {
                if (!String.IsNullOrWhiteSpace(wsd.Exception))
                    throw new Exception(wsd.Exception);
                iaUri = $"{wsd.ServerUrl}/IntegratedAuthentication.ashx";
            }
            var wc = new CookieAwareWebClient { UseDefaultCredentials = true };
            wc.Headers.Add(Constants.SOURCEHEADER, Constants.SERVICE_SOURCE);

            /******************************************************************************** 
             * DO NOT CHECK THIS IN UNCOMMENTED!                                            *
             * This is code to test SSO on a machine that is not joined to a domain.        *
             * Since the URL are specified it is unlikely that event if this codes gets out *
             * that it will cause any real issue (credentials only used of domain matches). *
             *******************************************************************************/
            //CredentialCache cc = new CredentialCache();
            //var nc = new NetworkCredential("username", "password", "domain");
            //cc.Add(new Uri("http://dev.docstar.com/"), "NTLM", nc);
            //cc.Add(new Uri("https://dev.docstar.com/"), "NTLM", nc);
            //cc.Add(new Uri("http://proxy.docstar.com/"), "NTLM", nc);
            //cc.Add(new Uri("https://proxy.docstar.com/"), "NTLM", nc);
            //wc.Credentials = cc;
            /******************************************************************************** 
             * DO NOT CHECK THIS IN UNCOMMENTED!                                            *
             *******************************************************************************/


            var html = wc.DownloadString(iaUri);
            if (String.IsNullOrWhiteSpace(html))
                throw new Exception("No SSO Response");
            var rex = new Regex(@"<script(.*)>([^<]*)<\/script>", RegexOptions.IgnoreCase); //JSON embedded in first Script tag in the response. 
            var json = rex.Match(html).Groups[2].Value; //2ed group, first group is the attributes of the script tag.

            if (String.IsNullOrWhiteSpace(json))
                throw new Exception("No Value found in SSO Response");

            var dummyLoginObj = new { Result = new { Token = "" }, Error = new { Message = "" } };
            var sr = JsonConvert.DeserializeAnonymousType(json.Trim(), dummyLoginObj);
            if (sr.Result == null && sr.Error == null)
            {
                throw new Exception("No User or Error message found");
            }
            if (sr.Error != null)
            {
                throw new Exception(sr.Error.Message);
            }
            if (sr.Error == null && sr.Result != null && !String.IsNullOrWhiteSpace(sr.Result.Token))
            {
                wsd = LoadWebServerData(sci.WebUrl, sr.Result.Token); //Reload data with token, so we have CurrentServerUrl context.
                sci.WebUrl = wsd.ZoneUrl;
                sci.Token = sr.Result.Token;
                sci.ServerUrl = null;
                WebHelper.GetUrlsFromWebServerData(sci);
            }
        }
        /// <summary>
        /// Gets the webserver data based on the web url. If a token is provided and the web server is a member of a proxy group the call will be forwarded 
        /// to the proxy server and MoveInProgress will be filled out properly (False in all other cases).
        /// </summary>
        public static WebServerData LoadWebServerData(string webUrl, string? token = null)
        {
            var result = new WebServerData();
            var responseString = "";
            using (var wclient = new WebClient())
            {
                if (!String.IsNullOrWhiteSpace(token))
                {
                    wclient.Headers.Add(Constants.TOKENHEADER, token);
                }
                responseString = wclient.DownloadString($"{webUrl}/BasicInfo/GetWebServerData");
                result = JsonConvert.DeserializeObject<WebServerData>(responseString);
            }
            return result;
        }
    }
}
