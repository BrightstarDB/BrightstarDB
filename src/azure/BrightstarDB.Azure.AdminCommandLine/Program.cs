using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using BrightstarDB.Azure.AdminCommandLine.Commands;

namespace BrightstarDB.Azure.AdminCommandLine
{
    class Program
    {

        static void Main(string[] args)
        {
            var cmd = FindCommand(args[0]);
            try
            {
                cmd.Execute(args);
            } catch(WebException wex)
            {
                Console.WriteLine("Error returned by server: {0}", wex.Status);
                if (wex.Response != null)
                {
                    using (var reader = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        var responseContent = reader.ReadToEnd();
                        Console.WriteLine(responseContent);
                    }
                }
            }
        }

        private static ICommand FindCommand(string cmdName)
        {
            switch(cmdName.ToLowerInvariant())
            {
                case "authenticate":
                    return new AuthCmd();
                case "setadmin":
                    return new SetAdminCmd();
                default:
                    return null;
            }
        }
    }
}
