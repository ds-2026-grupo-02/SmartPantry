using SmartPantry.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace SmartPantry.Permissions;

public class SmartPantryPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var smartPantryGroup = context.AddGroup(SmartPantryPermissions.GroupName, L("Permission:SmartPantry"));

        var metricasPermission = smartPantryGroup.AddPermission(
            SmartPantryPermissions.Metricas.Default,
            L("Permission:Metricas")
        );

        metricasPermission.AddChild(
            SmartPantryPermissions.Metricas.VerCatalogoExterno,
            L("Permission:Metricas.CatalogoExterno")
        );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<SmartPantryResource>(name);
    }
}
