using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using BrightstarDB.ClusterNodeService;
using BrightstarDB.Service;

namespace BrightstarDB.NodeServerRunner
{
    public partial class Service1 : ServiceBase
    {
        private ServiceHost _serviceHost;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var serviceHostFactory = new BrightstarServiceHostFactory();
            var service = new BrightstarNodeService();
            _serviceHost = serviceHostFactory.CreateServiceHost(service, Program.StopNode);
            _serviceHost.Open();
        }

        protected override void OnStop()
        {
            _serviceHost.Close();
        }
    }
}
