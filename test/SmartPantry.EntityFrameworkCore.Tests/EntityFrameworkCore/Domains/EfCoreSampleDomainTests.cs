using SmartPantry.Samples;
using Xunit;

namespace SmartPantry.EntityFrameworkCore.Domains;

[Collection(SmartPantryTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<SmartPantryEntityFrameworkCoreTestModule>
{

}
