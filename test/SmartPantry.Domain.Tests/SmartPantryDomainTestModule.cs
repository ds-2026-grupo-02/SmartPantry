using Volo.Abp.Modularity;

namespace SmartPantry;

[DependsOn(
    typeof(SmartPantryDomainModule),
    typeof(SmartPantryTestBaseModule)
)]
public class SmartPantryDomainTestModule : AbpModule
{

}
