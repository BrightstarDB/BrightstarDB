using BrightstarDB.Azure.Common;
using BrightstarDB.Azure.Management;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace BrightstarDB.Azure.Gateway
{
    public class AccountsRepositoryFactory
    {
        private static IAccountsRepository _instance;

        public static void Initialize(IAccountsCache cache)
        {
            _instance = new BrightstarAccountsRepository(
                RoleEnvironment.GetConfigurationSettingValue(AzureConstants.SuperUserAccountPropertyName),
                RoleEnvironment.GetConfigurationSettingValue(AzureConstants.SuperUserKeyPropertyName),
                cache
                );
        }

        public static IAccountsRepository GetAccountsRepository()
        {
            return _instance;
        }
    }
}