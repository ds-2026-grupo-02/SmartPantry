using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SmartPantry.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class SmartPantryDbContextFactory : IDesignTimeDbContextFactory<SmartPantryDbContext>
{
    public SmartPantryDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        SmartPantryEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<SmartPantryDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new SmartPantryDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../SmartPantry.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
