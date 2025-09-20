using Identity.API;
using Identity.Core.DTOs;
using Identity.Core.Entities;
using Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Identity.Tests.Controllers;

public class GroupsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IdentityDbContext _context;
    private readonly IServiceScope _scope;

    public GroupsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove the app's database context registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Add a database context using an in-memory database for testing
                services.AddDbContext<IdentityDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"InMemoryDb_{Guid.NewGuid()}");
                });

                // Ensure the database is created
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                context.Database.EnsureCreated();
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    }

    [Fact]
    public async Task GetGroups_ShouldReturnOk_WhenNoGroupsExist()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/groups");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedGroupResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task CreateGroup_ShouldReturnCreated_WhenValidRequest()
    {
        // Arrange
        await AuthenticateAsync();

        var request = new CreateGroupRequest
        {
            Name = "Test Group",
            Description = "Test Description",
            Type = GroupType.Team
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/groups", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GroupDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("Test Group", result.Name);
        Assert.Equal("Test Description", result.Description);

        // Verify in database
        var groupInDb = await _context.Groups.FirstOrDefaultAsync(g => g.Name == "Test Group");
        Assert.NotNull(groupInDb);
    }

    [Fact]
    public async Task CreateGroup_ShouldReturnBadRequest_WhenInvalidRequest()
    {
        // Arrange
        await AuthenticateAsync();

        var request = new CreateGroupRequest
        {
            Name = "", // Invalid empty name
            Description = "Test Description",
            Type = GroupType.Team
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/groups", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetGroup_ShouldReturnOk_WhenGroupExists()
    {
        // Arrange
        await AuthenticateAsync();

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Existing Group",
            Description = "Existing Description",
            Type = GroupType.Team,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/groups/{group.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GroupDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(group.Id, result.Id);
        Assert.Equal("Existing Group", result.Name);
    }

    [Fact]
    public async Task GetGroup_ShouldReturnNotFound_WhenGroupDoesNotExist()
    {
        // Arrange
        await AuthenticateAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/groups/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGroup_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        await AuthenticateAsync();

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            Description = "Original Description",
            Type = GroupType.Team,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(group);

        // Add user to group as Owner to allow updates
        var userGroup = new UserGroup
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = "test-user",
            Role = UserGroupRole.Owner,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.UserGroups.Add(userGroup);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateGroupRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Type = GroupType.Department
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/groups/{group.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GroupDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Updated Description", result.Description);
    }

    [Fact]
    public async Task DeleteGroup_ShouldReturnNoContent_WhenUserIsOwner()
    {
        // Arrange
        await AuthenticateAsync();

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "To Delete",
            Description = "Will be deleted",
            Type = GroupType.Team,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(group);

        // Add user as Owner
        var userGroup = new UserGroup
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = "test-user",
            Role = UserGroupRole.Owner,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.UserGroups.Add(userGroup);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/groups/{group.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify soft delete
        var deletedGroup = await _context.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == group.Id);
        Assert.NotNull(deletedGroup);
        Assert.True(deletedGroup.IsDeleted);
    }

    [Fact]
    public async Task GetGroupMembers_ShouldReturnOk_WhenUserIsMember()
    {
        // Arrange
        await AuthenticateAsync();

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Description = "Test",
            Type = GroupType.Team,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(group);

        var user = new ApplicationUser
        {
            Id = "test-user",
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        _context.Users.Add(user);

        var userGroup = new UserGroup
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = "test-user",
            Role = UserGroupRole.Member,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.UserGroups.Add(userGroup);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/groups/{group.Id}/members");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedMemberResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("test-user", result.Data.First().UserId);
    }

    [Fact]
    public async Task AddMemberToGroup_ShouldReturnCreated_WhenUserIsAdmin()
    {
        // Arrange
        await AuthenticateAsync();

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Description = "Test",
            Type = GroupType.Team,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(group);

        // Add current user as Admin
        var adminUserGroup = new UserGroup
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = "test-user",
            Role = UserGroupRole.Admin,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.UserGroups.Add(adminUserGroup);

        // Add target user
        var targetUser = new ApplicationUser
        {
            Id = "target-user",
            UserName = "targetuser",
            Email = "target@example.com",
            FirstName = "Target",
            LastName = "User"
        };
        _context.Users.Add(targetUser);
        await _context.SaveChangesAsync();

        var request = new AddGroupMemberRequest
        {
            UserId = "target-user",
            Role = UserGroupRole.Member
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/groups/{group.Id}/members", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify member was added
        var memberAdded = await _context.UserGroups.AnyAsync(ug =>
            ug.GroupId == group.Id &&
            ug.UserId == "target-user" &&
            ug.Role == UserGroupRole.Member &&
            ug.IsActive);
        Assert.True(memberAdded);
    }

    [Fact]
    public async Task RemoveMemberFromGroup_ShouldReturnNoContent_WhenUserIsAdmin()
    {
        // Arrange
        await AuthenticateAsync();

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Description = "Test",
            Type = GroupType.Team,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(group);

        // Add current user as Admin
        var adminUserGroup = new UserGroup
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = "test-user",
            Role = UserGroupRole.Admin,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.UserGroups.Add(adminUserGroup);

        // Add target user as Member
        var memberUserGroup = new UserGroup
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = "target-user",
            Role = UserGroupRole.Member,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.UserGroups.Add(memberUserGroup);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/groups/{group.Id}/members/target-user");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify member was removed
        var memberExists = await _context.UserGroups.AnyAsync(ug =>
            ug.GroupId == group.Id &&
            ug.UserId == "target-user" &&
            ug.IsActive);
        Assert.False(memberExists);
    }

    [Fact]
    public async Task GetMyGroups_ShouldReturnUserGroups_WhenAuthenticated()
    {
        // Arrange
        await AuthenticateAsync();

        var group1 = new Group
        {
            Id = Guid.NewGuid(),
            Name = "User Group 1",
            Description = "First group",
            Type = GroupType.Team,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var group2 = new Group
        {
            Id = Guid.NewGuid(),
            Name = "User Group 2",
            Description = "Second group",
            Type = GroupType.Department,
            CreatedBy = "other-user",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Groups.AddRange(group1, group2);

        var userGroup1 = new UserGroup
        {
            Id = Guid.NewGuid(),
            GroupId = group1.Id,
            UserId = "test-user",
            Role = UserGroupRole.Owner,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        var userGroup2 = new UserGroup
        {
            Id = Guid.NewGuid(),
            GroupId = group2.Id,
            UserId = "test-user",
            Role = UserGroupRole.Member,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.UserGroups.AddRange(userGroup1, userGroup2);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/groups/my-groups");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GroupDto[]>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Contains(result, g => g.Name == "User Group 1");
        Assert.Contains(result, g => g.Name == "User Group 2");
    }

    private async Task AuthenticateAsync()
    {
        // Create a test user
        var user = new ApplicationUser
        {
            Id = "test-user",
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // For integration tests, we'll simulate authentication by adding the Authorization header
        // In a real scenario, you would obtain a JWT token through the auth endpoint
        var token = "Bearer fake-jwt-token-for-testing";
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "fake-jwt-token-for-testing");

        // Add user ID claim header (simulate what JWT middleware would do)
        _client.DefaultRequestHeaders.Add("X-User-ID", "test-user");
    }

    public void Dispose()
    {
        _scope.Dispose();
        _context.Dispose();
    }
}

// Response DTOs for deserialization
public class PagedGroupResponse
{
    public IEnumerable<GroupDto> Data { get; set; } = new List<GroupDto>();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

public class PagedMemberResponse
{
    public IEnumerable<GroupMemberDto> Data { get; set; } = new List<GroupMemberDto>();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}