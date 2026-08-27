using RepairShop.IntegrationTests;

namespace RepairShop.IntegrationTests;

[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>;