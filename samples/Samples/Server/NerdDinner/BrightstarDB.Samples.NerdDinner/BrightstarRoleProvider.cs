using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using BrightstarDB.Samples.NerdDinner.Models;

namespace BrightstarDB.Samples.NerdDinner
{
    public class BrightstarRoleProvider : RoleProvider
    {
        #region Initialization

        private string _applicationName;

        private static string GetConfigValue(string configValue, string defaultValue)
        {
            if (string.IsNullOrEmpty(configValue))
                return defaultValue;

            return configValue;
        }

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            if (config == null) throw new ArgumentNullException("config");

            if (string.IsNullOrEmpty(name)) name = "NerdDinnerRoleProvider";

            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Nerd Dinner Membership Provider");
            }
            base.Initialize(name, config);
            _applicationName = GetConfigValue(config["applicationName"],
                          System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
        }

        #endregion

        /// <summary>
        /// Gets a list of the roles that a specified user is in for the configured applicationName.
        /// </summary>
        /// <returns>
        /// A string array containing the names of all the roles that the specified user is in for the configured applicationName.
        /// </returns>
        /// <param name="username">The user to return a list of roles for.</param>
        public override string[] GetRolesForUser(string username)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException("username");
            //create a new BrightstarDB context using the values in Web.config
            var context = new NerdDinnerContext();
            //find a match for the username
            var login = context.NerdDinnerLogins.Where(l => l.Username.Equals(username)).FirstOrDefault();
            if (login == null) return null;
            //return the Roles collection
            return login.Roles.ToArray();
        }

        /// <summary>
        /// Adds the specified user names to the specified roles for the configured applicationName.
        /// </summary>
        /// <param name="usernames">A string array of user names to be added to the specified roles. </param><param name="roleNames">A string array of the role names to add the specified user names to.</param>
        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            //create a new BrightstarDB context using the values in Web.config
            var context = new NerdDinnerContext();
            foreach (var username in usernames)
            {
                //find the match for the username
                var login = context.NerdDinnerLogins.Where(l => l.Username.Equals(username)).FirstOrDefault();
                if (login == null) continue;
                foreach (var role in roleNames)
                {
                    //if the Roles collection of the login does not already contain the role, then add it
                    if (login.Roles.Contains(role)) continue;
                    login.Roles.Add(role);
                }
            }
            context.SaveChanges();
        }

        /// <summary>
        /// Removes the specified user names from the specified roles for the configured applicationName.
        /// </summary>
        /// <param name="usernames">A string array of user names to be removed from the specified roles. </param><param name="roleNames">A string array of role names to remove the specified user names from.</param>
        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            //create a new BrightstarDB context using the values in Web.config
            var context = new NerdDinnerContext();
            foreach (var username in usernames)
            {
                //find the match for the username
                var login = context.NerdDinnerLogins.Where(l => l.Username.Equals(username)).FirstOrDefault();
                if (login == null) continue;
                foreach (var role in roleNames)
                {
                    //if the Roles collection of the login contains the role, then remove it
                    if (!login.Roles.Contains(role)) continue;
                    login.Roles.Remove(role);
                }
            }
            context.SaveChanges();
        }

        /// <summary>
        /// Gets a value indicating whether the specified user is in the specified role for the configured applicationName.
        /// </summary>
        /// <returns>
        /// true if the specified user is in the specified role for the configured applicationName; otherwise, false.
        /// </returns>
        /// <param name="username">The username to search for.</param>
        /// <param name="roleName">The role to search in.</param>
        public override bool IsUserInRole(string username, string roleName)
        {
            try
            {
                //create a new BrightstarDB context using the values in Web.config
                var context = new NerdDinnerContext();
                //find a match for the username
                var login = context.NerdDinnerLogins.Where(l => l.Username.Equals(username)).FirstOrDefault();
                if (login == null || login.IsLockedOut || !login.IsActivated)
                {
                    // no match or inactive automatically returns false
                    return false;
                }
                //if the Roles collection of the login contains the role we are checking for, return true
                return login.Roles.Contains(roleName.ToLower());
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a list of users in the specified role for the configured applicationName.
        /// </summary>
        /// <returns>
        /// A string array containing the names of all the users who are members of the specified role for the configured applicationName.
        /// </returns>
        /// <param name="roleName">The name of the role to get the list of users for.</param>
        public override string[] GetUsersInRole(string roleName)
        {
            if (string.IsNullOrEmpty(roleName)) throw new ArgumentNullException("roleName");
            //create a new BrightstarDB context using the values in Web.config
            var context = new NerdDinnerContext();
            //search for all logins who have the supplied roleName in their Roles collection
            var usersInRole = context.NerdDinnerLogins.Where(l => l.Roles.Contains(roleName.ToLower())).Select(l => l.Username).ToList();
            return usersInRole.ToArray();
        }

        /// <summary>
        /// Gets a value indicating whether the specified role name already exists in the role data source for the configured applicationName.
        /// </summary>
        /// <returns>
        /// true if the role name already exists in the data source for the configured applicationName; otherwise, false.
        /// </returns>
        /// <param name="roleName">The name of the role to search for in the data source.</param>
        public override bool RoleExists(string roleName)
        {
            //for the purpose of the sample the roles are hard coded
            return roleName.Equals("admin") || roleName.Equals("editor") || roleName.Equals("standard");
        }

        /// <summary>
        /// Gets a list of all the roles for the configured applicationName.
        /// </summary>
        /// <returns>
        /// A string array containing the names of all the roles stored in the data source for the configured applicationName.
        /// </returns>
        public override string[] GetAllRoles()
        {
            //for the purpose of the sample the roles are hard coded
            return new string[] { "admin", "editor", "standard" };
        }

        /// <summary>
        /// Gets an array of user names in a role where the user name contains the specified user name to match.
        /// </summary>
        /// <returns>
        /// A string array containing the names of all the users where the user name matches <paramref name="usernameToMatch"/> and the user is a member of the specified role.
        /// </returns>
        /// <param name="roleName">The role to search in.</param><param name="usernameToMatch">The user name to search for.</param>
        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            if (string.IsNullOrEmpty(roleName)) throw new ArgumentNullException("roleName");
            if (string.IsNullOrEmpty(usernameToMatch)) throw new ArgumentNullException("usernameToMatch");

            var allUsersInRole = GetUsersInRole(roleName);
            if (allUsersInRole == null || allUsersInRole.Count() < 1) return new string[] { "" };
            var match = (from u in allUsersInRole where u.Equals(usernameToMatch) select u);
            return match.ToArray();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the name of the application to store and retrieve role information for.
        /// </summary>
        /// <returns>
        /// The name of the application to store and retrieve role information for.
        /// </returns>
        public override string ApplicationName
        {
            get { return _applicationName; }
            set { _applicationName = value; }
        }

        #endregion

        #region Not Implemented Methods

        /// <summary>
        /// Adds a new role to the data source for the configured applicationName.
        /// </summary>
        /// <param name="roleName">The name of the role to create.</param>
        public override void CreateRole(string roleName)
        {
            //for the purpose of the sample the roles are hard coded
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes a role from the data source for the configured applicationName.
        /// </summary>
        /// <returns>
        /// true if the role was successfully deleted; otherwise, false.
        /// </returns>
        /// <param name="roleName">The name of the role to delete.</param><param name="throwOnPopulatedRole">If true, throw an exception if <paramref name="roleName"/> has one or more members and do not delete <paramref name="roleName"/>.</param>
        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            //for the purpose of the sample the roles are hard coded
            throw new NotImplementedException();
        }

        #endregion
    }
}