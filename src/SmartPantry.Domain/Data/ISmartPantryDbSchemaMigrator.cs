using System.Threading.Tasks;

namespace SmartPantry.Data;

public interface ISmartPantryDbSchemaMigrator
{
    Task MigrateAsync();
}
