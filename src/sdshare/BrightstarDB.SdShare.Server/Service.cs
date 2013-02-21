/*
Copyright Networked Planet Ltd 2004-12
contact@networkedplanet.com

------------------------------------------------------------------------

This code is made available to Hafslund ASA to be used as seen fit on
internal projects only. 

Any changes made to this code should be reported to Networked Planet Ltd.  
  
This code is made available as is, in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  

------------------------------------------------------------------------
*/

using System.ServiceProcess;

namespace BrightstarDB.SDShare.Server
{
    public partial class Service : ServiceBase
    {
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Program.StartService();
        }

        protected override void OnStop()
        {
        }
    }
}
