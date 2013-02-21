using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace website.installation
{
    public partial class _default : System.Web.UI.Page
    {
        private const int CurrentMajorVer = 1;
        private const int CurrentMinorVer = 1;

        public bool NewInstallation { get; private set; }
        public bool Upgrade { get; private set; }
        public bool Uninstalled { get; private set; }
        public bool CurrentVersion { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            string verNum = Request["install"];
            NewInstallation = !String.IsNullOrEmpty(verNum);
            if (!NewInstallation)
            {
                verNum = Request["upgrade"];
                Upgrade = !String.IsNullOrEmpty(verNum);
                if (!Upgrade)
                {
                    verNum = Request["uninstall"];
                    Uninstalled = !String.IsNullOrEmpty(verNum);
                }
            }
            int maj, min;
            GetVersionNumber(verNum, out maj, out min);
            CurrentVersion = !(maj < CurrentMajorVer || (maj == CurrentMajorVer && min < CurrentMinorVer));
        }

        private void GetVersionNumber(string verNum, out int maj, out int min)
        {
            maj = CurrentMajorVer;
            min = CurrentMinorVer;
            try
            {
                var parts = verNum.Split('.');
                if (parts.Length > 2)
                {
                    maj = Int32.Parse(parts[0]);
                    min = Int32.Parse(parts[1]);
                }
            }
            catch (Exception)
            {

            }
        }
    }
}