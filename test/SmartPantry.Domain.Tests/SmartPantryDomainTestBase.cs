using Volo.Abp.Modularity;

namespace SmartPantry;

/* Inherit from this class for your domain layer tests. */
public abstract class SmartPantryDomainTestBase<TStartupModule> : SmartPantryTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
