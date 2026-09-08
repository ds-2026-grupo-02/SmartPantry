using SmartPantry.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace SmartPantry.Permissions;

public class SmartPantryPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(SmartPantryPermissions.GroupName);

        //Define your own permissions here. Example:
        //myGroup.AddPermission(SmartPantryPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<SmartPantryResource>(name);
    }
}
