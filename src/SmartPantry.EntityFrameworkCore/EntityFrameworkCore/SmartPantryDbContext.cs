using Microsoft.EntityFrameworkCore;
using SmartPantry.Despensa;
using SmartPantry.Productos;
using SmartPantry.Advertencias; // Importante para ExpirationWarning
using SmartPantry.CatalogoExterno;
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
    public DbSet<NotaPrivadaDespensa> NotasPrivadasDespensa { get; set; }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<ItemDespensa> ItemsDespensa { get; set; }
    public DbSet<RegistroLlamadaExterna> RegistrosLlamadasExternas { get; set; }
    public DbSet<AdvertenciaVencimiento> AdvertenciasVencimiento { get; set; }

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

        //SmartPantry.Despensa ya está importado arriba,
        //así que NotaPrivadaDespensa e ItemDespensa se resuelven directo).
        builder.Entity<NotaPrivadaDespensa>(b =>
        {
            b.ToTable(SmartPantryConsts.DbTablePrefix + "NotasPrivadasDespensa", SmartPantryConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Contenido)
             .IsRequired()
             .HasMaxLength(NotaPrivadaDespensa.MaxContenidoLength);

            b.HasOne<ItemDespensa>()
             .WithMany()
             .HasForeignKey(x => x.ItemDespensaId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Producto>(b =>
        {
            b.ToTable(SmartPantryConsts.DbTablePrefix + "Productos", SmartPantryConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.NombreEs)
             .HasMaxLength(ProductoConsts.MaxNombreLength);

            b.Property(x => x.NombreEn)
             .HasMaxLength(ProductoConsts.MaxNombreLength);

            b.Property(x => x.Cantidad)
             .HasMaxLength(ProductoConsts.MaxCantidadLength);

            b.Property(x => x.CodigoBarras)
             .IsRequired()
             .HasMaxLength(ProductoConsts.MaxCodigoBarrasLength);

            b.Property(x => x.Marca)
             .HasMaxLength(ProductoConsts.MaxMarcaLength);

            b.Property(x => x.ImagenUrl)
             .HasMaxLength(ProductoConsts.MaxImagenUrlLength);

            b.Property(x => x.Categorias)
             .HasMaxLength(ProductoConsts.MaxCategoriasLength);

            b.Property(x => x.Ingredientes)
             .HasMaxLength(ProductoConsts.MaxIngredientesLength);

            b.Property(x => x.Alergenos)
             .HasMaxLength(ProductoConsts.MaxAlergenosLength);

            b.Property(x => x.NutriScore)
             .HasMaxLength(ProductoConsts.MaxNutriScoreLength);

            b.HasIndex(x => x.CodigoBarras)
             .IsUnique();
        });

        builder.Entity<ItemDespensa>(b =>
        {
            b.ToTable(SmartPantryConsts.DbTablePrefix + "ItemsDespensa", SmartPantryConsts.DbSchema);
            b.ConfigureByConvention();
        });

        builder.Entity<AdvertenciaVencimiento>(b =>
        {
            b.ToTable(SmartPantryConsts.DbTablePrefix + "AdvertenciasVencimiento", SmartPantryConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.TipoAdvertencia)
             .IsRequired()
             .HasMaxLength(64);

            // Índice único compuesto para asegurar la idempotencia requerida en el TP08
            b.HasIndex(x => new { x.ItemDespensaId, x.TipoAdvertencia })
             .IsUnique();
        });

        builder.Entity<RegistroLlamadaExterna>(b =>
        {
            b.ToTable(SmartPantryConsts.DbTablePrefix + "RegistrosLlamadasExternas", SmartPantryConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.TipoOperacion)
             .IsRequired();

            b.Property(x => x.FechaHoraUtc)
             .IsRequired();

            b.Property(x => x.DuracionMs)
             .IsRequired();

            b.Property(x => x.Resultado)
             .IsRequired();

            b.HasIndex(x => x.FechaHoraUtc);
            b.HasIndex(x => new { x.FechaHoraUtc, x.Resultado });
        });
    }
}
