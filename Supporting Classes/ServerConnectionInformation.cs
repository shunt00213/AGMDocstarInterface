using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    public class ServerConnectionInformation
    {
        #region Filled Out By API Developer
        /// <summary>
        /// Url to the DocStar server root directory
        /// IE: https://my.docstar.com/EclipseWeb
        /// </summary>
        public string WebUrl { get; set; }
        /// <summary>
        /// UserName of the user all operations will be performed under.
        /// </summary>
        public string UserName { get; set; }
        #endregion


        #region Filled out during the login process
        /// <summary>
        /// The URL to the root DocStar Server API.
        /// This is discovered using the WebUrl above.
        /// </summary>
        public string ServerUrl { get; set; }
        /// <summary>
        /// Authentication token - Provides access to secured API methods (which are pretty much all of them except Login and Logout).
        /// This is filled out as part of the authentication process.
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// Flag indicating if the Urls present are pointing to an authentication proxy or not.
        /// </summary>
        public bool Proxy { get; set; }
        #endregion
    }
}
