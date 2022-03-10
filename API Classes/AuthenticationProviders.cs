using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    public static class AuthenticationProviders
    {
        /// <summary>
        /// Returns all users in a specific LDAP node
        /// </summary>
        /// <param name="sci">authorization</param>
        /// <param name="authProviderId">LDAP authentication provider id to target the query</param>
        /// <param name="subtree">if true, returns users in "node" and in all sub-items in the LDAP true; if false, performs a "one level" query, finding users only directly under the node</param>
        /// <param name="node">the distinguished name of a container or OU, or blank to query the LDAP root (ex: OU=Schenectady,OU=User Accounts,DC=americas,DC=epicor,DC=net). You can use the GetLDAPContainers api to list OUs</param>
        /// <returns>Collection of LDAP users</returns>
        public static JArray GetLDAPUsers(ServerConnectionInformation sci, string authProviderId, bool subtree, string node)
        {
            var url = WebHelper.GetServerUrl(sci, "AuthenticationProvider", "GetLDAPUsers", true);
            var ldapGetQuery = new
            {
                ConnectionId = authProviderId,
                Subtree = subtree,
                Node = node
            };
            var json = JsonConvert.SerializeObject(ldapGetQuery);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);

            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return (JArray)resp["Result"];
        }
        /// <summary>
        /// Creates user accounts based on data retrieved from GetLDAPUsers.
        /// </summary>
        /// <param name="sci">authorization</param>
        /// <param name="authProviderId">LDAP authentication provider id the users are a member of</param>
        /// <param name="ldapUsers">List of users to be imported</param>
        /// <returns>DocStar user object representing the users imported</returns>
        public static JArray ImportLDAPUsers(ServerConnectionInformation sci, string authProviderId, List<dynamic> ldapUsers)
        {
            var url = WebHelper.GetServerUrl(sci, "AuthenticationProvider", "ImportLDAPUsers", true);
            var pkg = new
            {
                ConnectionId = authProviderId,
                Users = ldapUsers,
                ReadOnly = false //Identifies the user as a ReadOnly user, in this mode the highest level of access they can have is Read | Export regardless of membership.
            };
            var json = JsonConvert.SerializeObject(pkg);
            var respString = WebHelper.ExecutePost(url, json, sci.Token);

            var resp = (JObject)JsonConvert.DeserializeObject(respString);
            var error = resp["Error"];
            if (error.HasValues)
                throw new Exception(error["Message"].ToString());

            return (JArray)resp["Result"];
        }
    }
}
