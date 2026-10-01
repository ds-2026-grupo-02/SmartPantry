using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SmartPantry.Productos;
using Volo.Abp.Modularity;

namespace SmartPantry;

[DependsOn(
    typeof(SmartPantryApplicationModule),
    typeof(SmartPantryDomainTestModule)
)]
public class SmartPantryApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton(Substitute.For<IExternalProductCatalogClient>());
    }
}
