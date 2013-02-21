using System;
using System.Diagnostics;
using System.Reflection;

namespace BrightstarDB.Polaris.ViewModel
{
    public class AboutViewModel
    {
        public string Heading { get; private set; }
        public string Info { get; private set; }
        public string Acknowledgements { get { return Strings.Acknowledgements; } }

        public AboutViewModel()
        {
            Heading =  Strings.AboutHeading;
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Info = String.Format(Strings.AboutInfo, assemblyName.Version, fvi.ProductVersion);
        }
    }
}
