using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace UPCOR.Core
{
    public class AdManager
    {

        protected const string M_AD = @"LDAP://db1.upcor.se/";
        protected const string M_DOMAIN = "SAFE4";
        protected const string M_ROOT_PATH = "OU=Kommuner,OU=Upcor,DC=safe4,DC=se";
        protected const string M_USER = "administrator@safe4.se";
        protected const string M_ADMIN = "administrator";
        protected string M_PASSWORD = "";
        protected string m_StorePath = "";

        public string STORE_PATH {
            get { return m_StorePath; }
        }

        public string ROOT_PATH {
            get { return M_ROOT_PATH; }
        }

        public AdManager(string countyName, string orgName) {
            M_PASSWORD = Properties.Resources.M_PASSWORD;
            if (!String.IsNullOrEmpty(orgName))
                m_StorePath = String.Format("OU={0},OU={1},{2}", orgName, countyName, M_ROOT_PATH);
            else
                m_StorePath = String.Format("OU={0},{1}", countyName, M_ROOT_PATH);

            string rootPath = String.IsNullOrEmpty(orgName) ? M_ROOT_PATH : String.Format("OU={0},{1}", countyName, M_ROOT_PATH);

            if (!EntryExist(rootPath, String.IsNullOrEmpty(orgName) ? countyName : orgName, "organizationalUnit")) {
                using (DirectoryEntry deBase = new DirectoryEntry(M_AD + rootPath, M_USER, M_PASSWORD, AuthenticationTypes.None)) {
                    using (DirectoryEntry deChild = deBase.Children.Add(String.Format("OU={0}", String.IsNullOrEmpty(orgName) ? countyName : orgName), "organizationalUnit")) {
                        try {
                            deChild.CommitChanges();
                        }
                        finally {
                            deChild.Close();
                        }
                    }
                    deBase.Close();
                }
            }
        }

        public AdManager(string countyName):this(countyName, null) {            
        }

        public static string[] UserProperties {
            get {
                return new string[] { "name", "givenName", "sAMAccountName", "sn", "mail", "objectClass", "distinguishedName" };
            }
        }

        public static string[] GroupProperties {
            get {
                return new string[] { "cn", "name", "objectClass", "distinguishedName" };
            }
        }

        public static string[] OrganizationProperties {
            get {
                return new string[] { "ou", "name", "objectClass", "distinguishedName" };
            }
        }

        public KeyValuePair<String, String>[] Search(string[] properties, string filter) {
            return Search(m_StorePath, properties, filter);
        }

        public KeyValuePair<String, String>[] Search(string path, string[] properties, string filter) {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            using (DirectoryEntry deRoot = new DirectoryEntry(M_AD + path, M_USER, M_PASSWORD, AuthenticationTypes.Secure)) {
                using (DirectorySearcher search = new DirectorySearcher(deRoot, filter)) {
                    SearchResultCollection src = search.FindAll();
                    if (src != null) {
                        foreach (SearchResult sr in src) {
                            DirectoryEntry de = sr.GetDirectoryEntry();
                            KeyValuePair<string, string> dict = new KeyValuePair<string, string>();
                            foreach (string prop in properties) {
                                string val = "";
                                if (de.Properties.Contains(prop)) {
                                    if (de.Properties[prop].Value.GetType() == typeof(Object[])) {
                                        val = String.Join(",", (Object[])de.Properties[prop].Value);
                                    }
                                    else {
                                        val = de.Properties.Contains(prop) ? de.Properties[prop].Value.ToString() : String.Empty;
                                    }
                                }
                                dict = new KeyValuePair<string, string>(prop, val);
                                result.Add(dict);
                            }
                            de.Close();
                        }
                    }
                    src.Dispose();
                }
                deRoot.Close();
            }
            return result.ToArray();
        }

        /*
         * 
         * */
        public bool EntryExist(string path, string name, string objectClass) {
            bool retVal = false;
            using (DirectoryEntry deRoot = new DirectoryEntry(M_AD + path, M_USER, M_PASSWORD, AuthenticationTypes.Secure)) {
                string filter = String.Format("(&(objectClass={0})(ou={1}))", objectClass, name);
                using (DirectorySearcher search = new DirectorySearcher(deRoot, filter)) {
                    SearchResultCollection src = search.FindAll();
                    if (src.Count != 0)
                        retVal = true;

                    src.Dispose();
                }
                deRoot.Close();
            }
            return retVal;
        }

        public PrincipalContext GetPrincipalContext(string path) {
            PrincipalContext context = new PrincipalContext(ContextType.Domain,
                                                        M_DOMAIN,
                                                        path,
                                                        ContextOptions.Sealing | ContextOptions.Negotiate,
                                                        M_USER,
                                                        M_PASSWORD);

            return context;
        }

    }
}
