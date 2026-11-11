using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using SmartPantry.PantryItems;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.PermissionManagement;
using Xunit;

namespace SmartPantry
{
    [DependsOn(typeof(SmartPantryApplicationTestModule))]
    public class PantryItemSecurityTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton(Substitute.For<IOpenIddictApplicationRepository>());
            context.Services.AddSingleton(Substitute.For<IPermissionGroupDefinitionRecordRepository>());
            context.Services.AddSingleton(Substitute.For<IPermissionDefinitionRecordRepository>());
        }
    }

    public class PantryItemSecurity_Tests : SmartPantryApplicationTestBase<PantryItemSecurityTestModule>
    {
        private readonly IDespensaAppService _despensaAppService;

        public PantryItemSecurity_Tests()
        {
            _despensaAppService = GetRequiredService<IDespensaAppService>();
        }

        [Fact]
        public async Task No_Deberia_Permitir_Consultar_Item_De_Otro_Usuario()
        {
            await Should.ThrowAsync<Exception>(async () =>
            {
                await _despensaAppService.GetListAsync(new PagedAndSortedResultRequestDto());
            });
        }

        [Fact]
        public async Task No_Deberia_Permitir_Modificar_Item_De_Otro_Usuario()
        {
            var idItemAjeno = Guid.NewGuid();
            var input = new UpdateDespensaDto
            {
            };

            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _despensaAppService.UpdateAsync(idItemAjeno, input);
            });
        }
    }
}