using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartPantry.Data;
using Volo.Abp.DependencyInjection;

namespace SmartPantry.EntityFrameworkCore;

public class EntityFrameworkCoreSmartPantryDbSchemaMigrator
    : ISmartPantryDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreSmartPantryDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the SmartPantryDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<SmartPantryDbContext>()
            .Database
            .MigrateAsync();
    }
}
