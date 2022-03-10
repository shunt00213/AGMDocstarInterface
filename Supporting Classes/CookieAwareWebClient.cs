using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AGMDocstarInterface
{
    public class CookieAwareWebClient : WebClient
    {
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);
        public CookieAwareWebClient()
            : this(new CookieContainer())
        { }
        public CookieAwareWebClient(CookieContainer c)
        {
            this.CookieContainer = c;
        }
        public CookieContainer CookieContainer { get; set; }
        public string EncryptedToken { get; set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            request.Timeout = (int)RequestTimeout.TotalMilliseconds;
            HttpWebRequest webRequest = request as HttpWebRequest;
            if (webRequest != null)
            {
                webRequest.CookieContainer = CookieContainer;
            }
            return request;
        }

        public void AddCookies(HttpCookieCollection cookieCollection, string domain)
        {
            if (cookieCollection == null)
                return;

            for (int j = 0; j < cookieCollection.Count; j++)
            {
                HttpCookie hc = cookieCollection.Get(j);
                Cookie c = new Cookie();

                // Convert between the System.Net.Cookie to a System.Web.HttpCookie...
                c.Domain = domain;
                c.Expires = hc.Expires;
                c.Name = hc.Name;
                c.Path = hc.Path;
                c.Secure = hc.Secure;
                c.Value = hc.Value;

                CookieContainer.Add(c);
            }
        }
    }
}
