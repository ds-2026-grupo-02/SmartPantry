using Volo.Abp.Modularity;

namespace SmartPantry;

public abstract class SmartPantryApplicationTestBase<TStartupModule> : SmartPantryTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
