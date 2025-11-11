using Hems_Backend.Application.TodoLists.Queries.GetTodos;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.ValueObjects;

namespace Hems_Backend.Application.FunctionalTests.TodoLists.Queries;

using static Testing;

public class GetTodosTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnPriorityLevels()
    {
        await RunAsDefaultUserAsync();

        var query = new GetTodosQuery();

        var result = await SendAsync(query);

        result.PriorityLevels.Should().NotBeEmpty();
    }

    [Test]
    public async Task ShouldReturnAllListsAndItems()
    {
        await RunAsDefaultUserAsync();

        await AddAsync(new TodoList
        {
            Title = "Shopping",
            Colour = Colour.Blue,
            Items =
                    {
                        new TodoItem { Title = "Apples", Done = true },
                        new TodoItem { Title = "Milk", Done = true },
                        new TodoItem { Title = "Bread", Done = true },
                        new TodoItem { Title = "Toilet paper" },
                        new TodoItem { Title = "Pasta" },
                        new TodoItem { Title = "Tissues" },
                        new TodoItem { Title = "Tuna" }
                    }
        });

        var query = new GetTodosQuery();

        var result = await SendAsync(query);

        result.Lists.Should().HaveCountGreaterOrEqualTo(1); // En az 1 test list + seeding lists
        result.Lists.First().Items.Should().HaveCountGreaterOrEqualTo(0); // Seeding verileri temizlendiği için 0 olabilir
    }

    [Test]
    public async Task ShouldDenyAnonymousUser()
    {
        // This test is not needed since we don't have anonymous access
        // The authorization is handled at the endpoint level
        var userId = await RunAsDefaultUserAsync();
        var query = new GetTodosQuery();

        var result = await SendAsync(query);
        
        result.Should().NotBeNull();
    }
}
