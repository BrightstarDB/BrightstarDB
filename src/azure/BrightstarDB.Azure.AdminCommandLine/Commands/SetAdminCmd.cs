using System;
using System.Collections.Generic;
using System.Net;

namespace BrightstarDB.Azure.AdminCommandLine.Commands
{
    class SetAdminCmd : ICommand
    {
        #region Implementation of ICommand

        public string Name
        {
            get { return "SetAdmin"; }
        }

        public void Execute(params string[] args)
        {
            if (args.Length != 2)
            {
                Usage();
                return;
            }

            var client = new RestClient();
            var testPath = String.Format("services/account/{0}", args[1]);
            var response = client.AuthenticatedGet(testPath);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var actionPath = String.Format("services/account/{0}/SetAdminFlag", args[1]);
                response = client.AuthenticatedPost(actionPath, new Dictionary<string, string> {{"isAdmin", "true"}});
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine("OK");
                }
                else
                {
                    Console.Error.WriteLine("Request failed: {0} - {1}", response.StatusCode, response.StatusDescription);
                }
            } else
            {
                Console.Error.WriteLine("Failed to retrieve metadata for account {0}", args[1]);
            }
        }

        private void Usage()
        {
            Console.Error.WriteLine("Usage: bsadmin setadmin [account id]");
        }

        #endregion
    }
}
