using Microsoft.EntityFrameworkCore;
using SmartPantry.PantryItems;
using SmartPantry.Productos;
using SmartPantry.Warnings; // Importante para ExpirationWarning
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace SmartPantry.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class SmartPantryDbContext :
    AbpDbContext<SmartPantryDbContext>,
    ITenantManagementDbContext,
    IIdentityDbContext
{
    /* Identity */
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    /* Tenant Management */
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    /* Entidades Propias del Dominio */
    public DbSet<Producto> Productos { get; set; }
    public DbSet<PantryItem> PantryItems { get; set; } // Agregado si ya tienes la entidad de ítems de despensa
    public DbSet<ExpirationWarning> ExpirationWarnings { get; set; } // Propiedad DbSet requerida por EF Core

    public SmartPantryDbContext(DbContextOptions<SmartPantryDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Configuración de módulos ABP */
        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureTenantManagement();
        builder.ConfigureBlobStoring();

        /* Mapeo de Entidades */
        builder.Entity<Producto>(b =>
        {
            b.ToTable("Productos");
            b.ConfigureByConvention();

            b.Property(x => x.Nombre)
             .IsRequired()
             .HasMaxLength(ProductoConsts.MaxNombreLength);

            b.Property(x => x.CodigoBarras)
             .IsRequired()
             .HasMaxLength(ProductoConsts.MaxCodigoBarrasLength);
        });

        builder.Entity<PantryItem>(b =>
        {
            b.ToTable("AppPantryItems");
            b.ConfigureByConvention();

            b.Property(x => x.Unidad)
             .IsRequired()
             .HasMaxLength(32);
        });

        builder.Entity<ExpirationWarning>(b =>
        {
            b.ToTable("AppExpirationWarnings");
            b.ConfigureByConvention();

            b.Property(x => x.WarningType)
             .IsRequired()
             .HasMaxLength(64);

            // Índice único compuesto para asegurar la idempotencia requerida en el TP08
            b.HasIndex(x => new { x.PantryItemId, x.WarningType })
             .IsUnique();
        });
    }
}