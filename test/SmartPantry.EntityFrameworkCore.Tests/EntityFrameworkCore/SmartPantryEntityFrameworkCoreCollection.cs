using Xunit;

namespace SmartPantry.EntityFrameworkCore;

[CollectionDefinition(SmartPantryTestConsts.CollectionDefinitionName)]
public class SmartPantryEntityFrameworkCoreCollection : ICollectionFixture<SmartPantryEntityFrameworkCoreFixture>
{

}
