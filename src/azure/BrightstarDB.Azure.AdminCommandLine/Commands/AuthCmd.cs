using System;

namespace BrightstarDB.Azure.AdminCommandLine.Commands
{
    internal class AuthCmd : ICommand
    {
        #region Implementation of ICommand

        public string Name
        {
            get { return "Authenticate"; }
        }

        public void Execute(params string[] args)
        {
            if (args.Length != 4)
            {
                Usage();
                return;
            }
            Settings.Default.Endpoint = args[1];
            Settings.Default.SuperUserAccount = args[2];
            Settings.Default.SuperUserKey = args[3];
            Settings.Default.Save();
            Console.WriteLine("Authentication settings saved.");
        }

        #endregion

        private void Usage()
        {
            Console.Error.WriteLine("Usage: bsadmin authenticate [endpoint] [accountid] [accountkey]");
        }
    }
}