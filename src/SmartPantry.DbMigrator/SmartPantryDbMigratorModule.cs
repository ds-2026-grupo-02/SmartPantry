using SmartPantry.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace SmartPantry.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(SmartPantryEntityFrameworkCoreModule),
    typeof(SmartPantryApplicationContractsModule)
)]
public class SmartPantryDbMigratorModule : AbpModule
{
}
