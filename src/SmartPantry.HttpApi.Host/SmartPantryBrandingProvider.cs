using Microsoft.Extensions.Localization;
using SmartPantry.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace SmartPantry;

[Dependency(ReplaceServices = true)]
public class SmartPantryBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<SmartPantryResource> _localizer;

    public SmartPantryBrandingProvider(IStringLocalizer<SmartPantryResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
