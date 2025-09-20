using Identity.Core.DTOs;
using Identity.Core.Entities;
using Identity.Core.Interfaces;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using AutoMapper;
using Xunit;
using Enterprise.Shared.Caching.Interfaces;
using ApplicationGroupService = Identity.Application.Services.GroupService;

namespace Identity.Tests.Services;

public class GroupServiceTests : IDisposable
{
    private readonly IdentityDbContext _context;
    private readonly IGroupRepository _groupRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ICacheService> _mockCache;
    private readonly Mock<ILogger<ApplicationGroupService>> _mockLogger;
    private readonly Mock<ILogger<GroupRepository>> _mockRepoLogger;
    private readonly ApplicationGroupService _groupService;

    public GroupServiceTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new IdentityDbContext(options);

        // Setup mocks
        _mockMapper = new Mock<IMapper>();
        _mockCache = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<ApplicationGroupService>>();
        _mockRepoLogger = new Mock<ILogger<GroupRepository>>();

        _groupRepository = new GroupRepository(_context, _mockRepoLogger.Object);

        // Setup AutoMapper mappings
        SetupMapperMocks();

        _groupService = new ApplicationGroupService(
            _groupRepository,
            _mockMapper.Object,
            _mockLogger.Object,
            _mockCache.Object);
    }

    private void SetupMapperMocks()
    {
        _mockMapper.Setup(m => m.Map<GroupDto>(It.IsAny<Group>()))
            .Returns((Group source) => new GroupDto
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Type = source.Type.ToString(),
                IsActive = source.IsActive,
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy
            });

        _mockMapper.Setup(m => m.Map<Group>(It.IsAny<CreateGroupRequest>()))
            .Returns((CreateGroupRequest source) => new Group
            {
                Id = Guid.NewGuid(),
                Name = source.Name,
                Description = source.Description ?? string.Empty,
                Type = source.Type,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        _mockMapper.Setup(m => m.Map<GroupMemberDto>(It.IsAny<ApplicationUser>()))
            .Returns((ApplicationUser source) => new GroupMemberDto
            {
                UserId = source.Id,
                UserName = source.UserName ?? source.Email,
                Email = source.Email,
                FirstName = source.FirstName ?? string.Empty,
                LastName = source.LastName ?? string.Empty
            });
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateGroup_WhenValidRequest()
    {
        // Arrange
        var request = new CreateGroupRequest
        {
            Name = "Test Group",
            Description = "Test Description",
            Type = GroupType.Team
        };
        var createdBy = "admin-user-id";

        // Act
        var result = await _groupService.CreateAsync(request, createdBy);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Test Group", result.Value.Name);
        Assert.Equal("Test Description", result.Value.Description);

        // Verify group exists in database
        var groupInDb = await _context.Groups.FirstOrDefaultAsync(g => g.Name == "Test Group");
        Assert.NotNull(groupInDb);
        Assert.Equal(createdBy, groupInDb.CreatedBy);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenGroupNameExists()
    {
        // Arrange
        var existingGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Existing Group",
            Description = "Existing",
            Type = GroupType.Team,
            CreatedBy = "user1",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(existingGroup);
        await _context.SaveChangesAsync();

        var request = new CreateGroupRequest
        {
            Name = "Existing Group",
            Description = "New Description",
            Type = GroupType.Department
        };

        // Act
        var result = await _groupService.CreateAsync(request, "user2");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Bu isimde bir grup zaten mevcut", result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnGroup_WhenExists()
    {
        // Arrange
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Description = "Test Description",
            Type = GroupType.Team,
            CreatedBy = "user1",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Act
        var result = await _groupService.GetByIdAsync(group.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(group.Id, result.Value.Id);
        Assert.Equal("Test Group", result.Value.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldFail_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _groupService.GetByIdAsync(nonExistentId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Grup bulunamadı", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateGroup_WhenValidRequest()
    {
        // Arrange
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            Description = "Original Description",
            Type = GroupType.Team,
            CreatedBy = "user1",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateGroupRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Type = GroupType.Department
        };

        // Act
        var result = await _groupService.UpdateAsync(group.Id, updateRequest, "user1");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", result.Value.Name);
        Assert.Equal("Updated Description", result.Value.Description);

        // Verify in database
        var updatedGroup = await _context.Groups.FindAsync(group.Id);
        Assert.NotNull(updatedGroup);
        Assert.Equal("Updated Name", updatedGroup.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldMarkAsDeleted_WhenExists()
    {
        // Arrange
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "To Delete",
            Description = "Will be deleted",
            Type = GroupType.Team,
            CreatedBy = "user1",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Act
        var result = await _groupService.DeleteAsync(group.Id, "user1");

        // Assert
        Assert.True(result.IsSuccess);

        // Verify soft delete
        var deletedGroup = await _context.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == group.Id);
        Assert.NotNull(deletedGroup);
        Assert.True(deletedGroup.IsDeleted);
        Assert.NotNull(deletedGroup.DeletedAt);
    }

    [Fact]
    public async Task AddUserToGroupAsync_ShouldAddUser_WhenValid()
    {
        // Arrange
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Description = "Test",
            Type = GroupType.Team,
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(group);

        var user = new ApplicationUser
        {
            Id = "user123",
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _groupService.AddUserToGroupAsync(group.Id, "user123", UserGroupRole.Member, "admin");

        // Assert
        Assert.True(result.IsSuccess);

        // Verify user group relationship
        var userGroup = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.GroupId == group.Id && ug.UserId == "user123");
        Assert.NotNull(userGroup);
        Assert.Equal(UserGroupRole.Member, userGroup.Role);
    }

    [Fact]
    public async Task RemoveUserFromGroupAsync_ShouldRemoveUser_WhenExists()
    {
        // Arrange
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Description = "Test",
            Type = GroupType.Team,
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(group);

        var userGroup = new UserGroup
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = "user123",
            Role = UserGroupRole.Member,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.UserGroups.Add(userGroup);
        await _context.SaveChangesAsync();

        // Act
        var result = await _groupService.RemoveUserFromGroupAsync(group.Id, "user123");

        // Assert
        Assert.True(result.IsSuccess);

        // Verify user removed
        var removedUserGroup = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.GroupId == group.Id && ug.UserId == "user123" && ug.IsActive);
        Assert.Null(removedUserGroup);
    }

    [Fact]
    public async Task IsUserInGroupAsync_ShouldReturnTrue_WhenUserIsMember()
    {
        // Arrange
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Description = "Test",
            Type = GroupType.Team,
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(group);

        var userGroup = new UserGroup
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = "user123",
            Role = UserGroupRole.Member,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.UserGroups.Add(userGroup);
        await _context.SaveChangesAsync();

        // Act
        var result = await _groupService.IsUserInGroupAsync("user123", group.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task GetUserRoleInGroupAsync_ShouldReturnRole_WhenUserIsMember()
    {
        // Arrange
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Description = "Test",
            Type = GroupType.Team,
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Groups.Add(group);

        var userGroup = new UserGroup
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = "user123",
            Role = UserGroupRole.Admin,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.UserGroups.Add(userGroup);
        await _context.SaveChangesAsync();

        // Act
        var result = await _groupService.GetUserRoleInGroupAsync("user123", group.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(UserGroupRole.Admin, result.Value);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 5)]
    [InlineData(1, 20)]
    public async Task GetGroupsAsync_ShouldReturnPagedResults(int page, int pageSize)
    {
        // Arrange
        var groups = Enumerable.Range(1, 15).Select(i => new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Group {i}",
            Description = $"Description {i}",
            Type = GroupType.Team,
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        }).ToList();

        _context.Groups.AddRange(groups);
        await _context.SaveChangesAsync();

        // Act
        var result = await _groupService.GetGroupsAsync(page, pageSize);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value.TotalCount);
        Assert.Equal(page, result.Value.CurrentPage);
        Assert.Equal(pageSize, result.Value.PageSize);

        var expectedItemCount = Math.Min(pageSize, Math.Max(0, 15 - (page - 1) * pageSize));
        Assert.Equal(expectedItemCount, result.Value.Data.Count());
    }

    [Fact]
    public async Task GetGroupsAsync_ShouldFilterByType_WhenTypeProvided()
    {
        // Arrange
        var teams = Enumerable.Range(1, 5).Select(i => new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Team {i}",
            Description = $"Team Description {i}",
            Type = GroupType.Team,
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        var departments = Enumerable.Range(1, 3).Select(i => new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Department {i}",
            Description = $"Department Description {i}",
            Type = GroupType.Department,
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        _context.Groups.AddRange(teams);
        _context.Groups.AddRange(departments);
        await _context.SaveChangesAsync();

        // Act
        var result = await _groupService.GetGroupsAsync(1, 20, null, GroupType.Team);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalCount);
        Assert.All(result.Value.Data, g => Assert.Equal("Team", g.Type));
    }

    [Fact]
    public async Task GetGroupsAsync_ShouldFilterBySearch_WhenSearchProvided()
    {
        // Arrange
        var groups = new[]
        {
            new Group { Id = Guid.NewGuid(), Name = "Development Team", Description = "Dev team", Type = GroupType.Team, CreatedBy = "admin", CreatedAt = DateTime.UtcNow, IsActive = true },
            new Group { Id = Guid.NewGuid(), Name = "QA Team", Description = "Quality assurance", Type = GroupType.Team, CreatedBy = "admin", CreatedAt = DateTime.UtcNow, IsActive = true },
            new Group { Id = Guid.NewGuid(), Name = "Marketing", Description = "Marketing department", Type = GroupType.Department, CreatedBy = "admin", CreatedAt = DateTime.UtcNow, IsActive = true }
        };

        _context.Groups.AddRange(groups);
        await _context.SaveChangesAsync();

        // Act
        var result = await _groupService.GetGroupsAsync(1, 20, "Team");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.All(result.Value.Data, g => Assert.Contains("Team", g.Name));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}