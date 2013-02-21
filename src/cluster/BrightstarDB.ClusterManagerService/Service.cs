using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using BrightstarDB.ClusterManager;

namespace BrightstarDB.ClusterManagerService
{
    public partial class Service : ServiceBase
    {

        private ServiceHost _serviceHost;

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // todo: add event registration on error
            var serviceHostFactory = new ClusterManagerServiceHostFactory();
            _serviceHost = serviceHostFactory.CreateServiceHost();
            _serviceHost.Open();
        }

        protected override void OnStop()
        {
            _serviceHost.Close();
        }
    }
}
