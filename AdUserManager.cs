using Microsoft.Office.Server.UserProfiles;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UPCOR.Core
{
    public class AdUserManager : AdManager
    {
        public AdUserManager(string countyName, string orgName)
            : base(countyName, orgName) {
        }

        public UserPrincipal Get(string userName) {
            PrincipalContext context = GetPrincipalContext("DC = safe4, DC = se");
            UserPrincipal user = UserPrincipal.FindByIdentity(context, userName);
            return user;
        }

        /*
         * Check if user exist 
         * */
        public bool Exist(string userName) {
            return Get(userName) == null ? false : true;
        }

        private User f_get_user(ClientContext ctx, string domain, string userName) {
            string wAccName = String.Format("i:0#.w|{0}\\{1}", domain, userName);
            string accName = String.Format("{0}\\{1}", domain, userName);
            User u;
            try {
                u = ctx.Web.SiteUsers.GetByLoginName(wAccName);
                ctx.Load(u.Groups, gs => gs.Include(g => g.Id));
                ctx.ExecuteQuery();
            }
            catch {
                try {
                    u = ctx.Web.SiteUsers.GetByLoginName(accName);
                    ctx.Load(u.Groups, gs => gs.Include(g => g.Id));
                    ctx.ExecuteQuery();
                }
                catch (Exception x) {
                    return null;
                }
            }
            return u;
        }

        private GroupCollection f_get_groups(ClientContext ctx) {
            GroupCollection groups = ctx.Web.SiteGroups;
            ctx.Load(groups,
                gs => gs.Include(
                    g => g.Title,
                    g => g.Id));
            ctx.ExecuteQuery();
            return groups;
        }

        /*
         * 
         * */
        public int[] Groups(string siteUrl, string domain, string userName, out string err) {
            err = "";
            ClientContext ctx = new ClientContext(siteUrl);
            ctx.Credentials = new NetworkCredential(M_ADMIN, M_PASSWORD, M_DOMAIN);
            List<int> ints = new List<int>();
            User u = f_get_user(ctx, domain, userName);
            if (u != null) {
                foreach (Group g in u.Groups) {
                    ints.Add(g.Id);
                }
            }
            return ints.ToArray();
        }

        /*
         * Remove user from siteGroups
         * */
        public void RemoveFromGroups(string siteUrl, string domain, string userName, int[] groupIds, out string err) {
            err = "";
            ClientContext ctx = new ClientContext(siteUrl);
            ctx.Credentials = new NetworkCredential(M_ADMIN, M_PASSWORD, M_DOMAIN);
            User u = f_get_user(ctx, domain, userName);
            if (u == null) return;
            try {
                foreach (Group g in f_get_groups(ctx)) {
                    if (groupIds.Contains(g.Id))
                        g.Users.Remove(u);
                }
                ctx.ExecuteQuery();
            }
            catch (Exception ex) {
                err = ex.Message;
            }
        }

        /*
         * Add user to siteGroups
         * */
        public void AddToGroups(string siteUrl, string domain, string userName, int[] groupIds, out string err) {
            err = "";
            ClientContext ctx = new ClientContext(siteUrl);
            ctx.Credentials = new NetworkCredential(M_ADMIN, M_PASSWORD, M_DOMAIN);
            User u = f_get_user(ctx, domain, userName);
            if (u == null) return;
            try {
                foreach (Group g in f_get_groups(ctx)) {
                    if (groupIds.Contains(g.Id))
                        g.Users.AddUser(u);
                }
                ctx.ExecuteQuery();
            }
            catch (Exception ex) {
                err = ex.Message;
            }
        }

        /*
         * Set password
         * */
        public void SetPassword(string userName, string newPassword, out string err) {
            err = "";
            try {
                UserPrincipal user = Get(userName);
                if (user != null) {
                    user.SetPassword(newPassword);
                }
                else {
                    err = String.Format("Användaren: {0} finns inte.", userName);
                }
            }
            catch (Exception ex) {
                err = ex.Message;
            }
        }

        /*
         * Enable user account
         * */
        public void Enable(string userName, out string err) {
            err = "";
            try {
                UserPrincipal user = Get(userName);
                if (user != null) {
                    user.Enabled = false;
                    user.Save();
                }
                else {
                    err = String.Format("Användaren: {0} finns inte.", userName);
                }
            }
            catch (Exception ex) {
                err = ex.Message;
            }
        }

        /*
         * Disable user account
         * */
        public void Disable(string userName, out string err) {
            err = "";
            try {
                UserPrincipal user = Get(userName);
                if (user != null) {
                    user.Enabled = false;
                    user.Save();
                }
                else {
                    err = String.Format("Användaren: {0} finns inte.", userName);
                }
            }
            catch (Exception ex) {
                err = ex.Message;
            }
        }

        /*
         * Expire password
         * */
        public void ExpirePassword(string userName, out string err) {
            err = "";
            try {
                UserPrincipal user = Get(userName);
                if (user != null) {
                    user.ExpirePasswordNow();
                    user.Save();
                }
                else {
                    err = String.Format("Användaren: {0} finns inte.", userName);
                }
            }
            catch (Exception ex) {
                err = ex.Message;
            }
        }

        /*
         * Update user
         * */
        public void Update(string userName, string password, string givenName, string surName, string email, out string err) {
            string err2 = "";
            err = "";
            UserPrincipal user = this.Get(userName);
            if (user != null) {
                try {
                    user.Name = givenName + " " + surName;
                    user.GivenName = givenName;
                    user.Surname = surName;
                    user.EmailAddress = email;
                    user.Save();
                }
                catch (Exception ex) {
                    err = ex.Message;
                }
                finally {
                    if (!String.IsNullOrEmpty(password)) {
                        this.SetPassword(userName, password, out err2);
                    }
                    err = err2 + ";;" + err;
                }
                return;
            }
            err = String.Format("Användaren: {0} finns inte.", userName);
        }

        /*
         * Create User
         * */
        public void Create(string siteUrl, string domain, string ouName, string userName, string password, string givenName, string surName, string email, int[] groupIds, out string err) {
            err = "";
            if (!Exist(userName)) {
                using (PrincipalContext context = GetPrincipalContext(this.STORE_PATH)) {
                    UserPrincipal user = new UserPrincipal(context, userName, password, true);
                    //User Log on Name
                    try {
                        user.Name = givenName + " " + surName;
                        user.UserPrincipalName = userName;
                        user.GivenName = givenName;
                        user.Surname = surName;
                        user.EmailAddress = email;
                        user.Save();
                    }
                    catch (Exception ex) {
                        err = ex.Message;
                    }
                }
            }
            if (groupIds == null)
                return;

            if (groupIds.Length == 0)
                return;

            try {
                ClientContext ctx = new ClientContext(siteUrl);
                ctx.Credentials = new NetworkCredential(M_ADMIN, M_PASSWORD, M_DOMAIN);
                GroupCollection groups = ctx.Web.SiteGroups;
                ctx.Load(groups, gs => gs.Include(g => g.Id));
                ctx.ExecuteQuery();

                UserCreationInformation uci = new UserCreationInformation();
                uci.Email = email;
                uci.LoginName = domain + "\\" + userName;
                uci.Title = givenName + " " + surName;

                foreach (Group g in groups) {
                    if (groupIds.Contains(g.Id))
                        g.Users.Add(uci);
                }
                ctx.ExecuteQuery();
            }
            catch (Exception ex) {
                err = ex.Message;
            }

        }
    }
}
