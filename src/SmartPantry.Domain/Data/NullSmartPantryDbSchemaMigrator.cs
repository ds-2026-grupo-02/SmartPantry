using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SmartPantry.Data;

/* This is used if database provider does't define
 * ISmartPantryDbSchemaMigrator implementation.
 */
public class NullSmartPantryDbSchemaMigrator : ISmartPantryDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
