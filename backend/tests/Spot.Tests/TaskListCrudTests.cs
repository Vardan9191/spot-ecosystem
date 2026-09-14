using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spot.Api.Controllers;
using Spot.Api.DTOs;
using Spot.Infrastructure.Persistence;
using Spot.Infrastructure.Repositories;
using Xunit;

namespace Spot.Tests;

public class TaskListCrudTests
{
    private static SpotDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SpotDbContext>()
            .UseInMemoryDatabase(databaseName: $"SpotTasksTest_{Guid.NewGuid()}")
            .Options;

        var context = new SpotDbContext(options);
        DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
        return context;
    }

    [Fact]
    public async Task TaskLists_CreateAndRetrieve_ShouldSucceed()
    {
        using var context = CreateInMemoryContext();
        var repo = new TaskListRepository(context);
        var controller = new TaskListsController(repo);

        var newUserId = Guid.NewGuid();
        var request = new CreateTaskListRequest(
            UserId: newUserId,
            Title: "Holiday Party Supplies",
            CategoryId: DbSeeder.CatGroceryId,
            Latitude: 40.7145,
            Longitude: -74.0080,
            RadiusMeters: 200,
            IsActive: true
        );

        // Create
        var createResponse = await controller.Create(request);
        var createdResult = createResponse.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var createdList = createdResult.Value.Should().BeOfType<TaskListDto>().Subject;

        createdList.Title.Should().Be("Holiday Party Supplies");
        createdList.Latitude.Should().Be(40.7145);
        createdList.Longitude.Should().Be(-74.0080);
        createdList.RadiusMeters.Should().Be(200);

        // Retrieve by User ID
        var getResponse = await controller.GetByUserId(newUserId);
        var okResult = getResponse.Result.Should().BeOfType<OkObjectResult>().Subject;
        var userLists = okResult.Value.Should().BeOfType<List<TaskListDto>>().Subject;

        userLists.Should().HaveCount(1);
        userLists.First().Id.Should().Be(createdList.Id);
    }

    [Fact]
    public async Task TaskItems_AddToggleDelete_ShouldWorkCorrectly()
    {
        using var context = CreateInMemoryContext();
        var repo = new TaskListRepository(context);
        var itemsController = new TaskItemsController(repo);

        Guid listId = DbSeeder.TestListId;

        // 1. Add item
        var addResponse = await itemsController.AddItem(listId, new CreateTaskItemRequest("Dark Chocolate 85%", "3 bars"));
        var createdResult = addResponse.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var addedItem = createdResult.Value.Should().BeOfType<TaskItemDto>().Subject;

        addedItem.Title.Should().Be("Dark Chocolate 85%");
        addedItem.Quantity.Should().Be("3 bars");
        addedItem.IsCompleted.Should().BeFalse();

        // 2. Toggle completion
        var toggleResponse = await itemsController.ToggleItem(listId, addedItem.Id);
        var toggleResult = toggleResponse.Result.Should().BeOfType<OkObjectResult>().Subject;
        var toggledItem = toggleResult.Value.Should().BeOfType<TaskItemDto>().Subject;

        toggledItem.IsCompleted.Should().BeTrue();

        // 3. Delete item
        var deleteResponse = await itemsController.DeleteItem(listId, addedItem.Id);
        deleteResponse.Should().BeOfType<NoContentResult>();
    }
}
