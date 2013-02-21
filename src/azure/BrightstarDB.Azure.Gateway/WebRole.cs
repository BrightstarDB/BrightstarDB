using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BrightstarDB.Azure.Common;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace BrightstarDB.Azure.Gateway
{
    public class WebRole : RoleEntryPoint
    {
#if DEBUG
        private const int BasePeriod = 2;
#else
        private const int BasePeriod = 15;
#endif
        private TimeSpan _monitorPeriod;
        private Timer _monitorTimer;

        public override bool OnStart()
        {
            try
            {
                base.OnStart();
                _monitorPeriod = TimeSpan.FromMinutes(BasePeriod * RoleEnvironment.CurrentRoleInstance.Role.Instances.Count);
                var rnd = new Random();
                var start = rnd.Next((int) _monitorPeriod.TotalMinutes);
                    // Randomize time to first invocation to prevent all gateways from synchronizing on the same time
                _monitorTimer = new Timer(MonitorCallback, null, TimeSpan.FromMinutes(start), _monitorPeriod);
                RoleStartupHelper.StartDiagnostics();
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Gateway WebRole OnStart failed: " + ex);
                EventLog.WriteEntry("AzureEventLogs",
                                    "OnStart failed for Gateway role: Exception:" + ex.GetType().FullName +
                                    ", Message:" + ex.Message,
                                    EventLogEntryType.Error, 0);
                Trace.TraceError("Sleeping for long enough to allow errors to flush");
                Thread.Sleep(TimeSpan.FromMinutes(1.5));
                Trace.TraceError("Aborting role startup");
                return false;
            }
        }


        public override void OnStop()
        {
            Trace.TraceInformation("Gateway: OnStop invoked.");
            base.OnStop();
            _monitorTimer.Dispose();
            Trace.TraceInformation("Gateway: OnStop completed cleanup");
        }

        public void MonitorCallback(object state)
        {
            if (AccountsRepositoryFactory.GetAccountsRepository() == null)
            {
                //AccountsRepositoryFactory.Initialize(new NullAccountsCache());
                Trace.TraceWarning("AccountsRepositoryFactory has not been initialized yet.");
                return;
            }

            try
            {
                DateTime startTime = DateTime.UtcNow;
                var storeSizes = new Dictionary<string, int>();

                foreach (var store in BrightstarCluster.Instance.GetStores())
                {
                    if (BrightstarCluster.Instance.GetLastModifiedDate(store).Subtract(startTime) <= _monitorPeriod)
                    {

                        int storeSize = BrightstarCluster.Instance.GetStoreSize(store);
                        storeSizes[store] = storeSize;
                    }
                }
                if (storeSizes.Count > 0)
                {
                    var repo = AccountsRepositoryFactory.GetAccountsRepository();
                    if (repo != null)
                    {
                        repo.UpdateStoreSizes(storeSizes);
                    }
                }
                var newPeriod = TimeSpan.FromMinutes(BasePeriod*RoleEnvironment.CurrentRoleInstance.Role.Instances.Count);
                if (!newPeriod.Equals(_monitorPeriod))
                {
                    // Indicates that we have more or fewer roles instances so need to adjust the timer frequency accordingly
                    _monitorTimer.Change(newPeriod, newPeriod);
                    _monitorPeriod = newPeriod;
                }
            }
            catch (Exception ex)
            {
                // Log exception in monitor loop
                Trace.TraceError("Gateway: Exception caught in monitor loop: {0}", ex);
            }
        }
    }
}
