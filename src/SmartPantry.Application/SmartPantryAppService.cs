using SmartPantry.Localization;
using Volo.Abp.Application.Services;

namespace SmartPantry;

/* Inherit your application services from this class.
 */
public abstract class SmartPantryAppService : ApplicationService
{
    protected SmartPantryAppService()
    {
        LocalizationResource = typeof(SmartPantryResource);
    }
}
