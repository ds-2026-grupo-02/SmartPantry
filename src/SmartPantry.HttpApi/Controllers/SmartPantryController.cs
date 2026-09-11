using SmartPantry.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace SmartPantry.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class SmartPantryController : AbpControllerBase
{
    protected SmartPantryController()
    {
        LocalizationResource = typeof(SmartPantryResource);
    }
}
