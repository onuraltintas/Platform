using Identity.API.Controllers;
using Identity.Core.DTOs;
using Identity.Core.Entities;
using Identity.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using Enterprise.Shared.Common.Models;

namespace Identity.Tests.Controllers;

public class GroupsControllerUnitTests
{
    private readonly Mock<IGroupService> _mockGroupService;
    private readonly Mock<ILogger<GroupsController>> _mockLogger;
    private readonly GroupsController _controller;

    public GroupsControllerUnitTests()
    {
        _mockGroupService = new Mock<IGroupService>();
        _mockLogger = new Mock<ILogger<GroupsController>>();
        _controller = new GroupsController(_mockGroupService.Object, _mockLogger.Object);

        // Setup controller context with fake user
        SetupControllerContext();
    }

    private void SetupControllerContext()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Email, "test@example.com")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetGroups_ShouldReturnOk_WhenServiceSucceeds()
    {
        // Arrange
        var request = new GetGroupsRequest { Page = 1, PageSize = 10 };
        var serviceResult = Result<PagedResult<GroupDto>>.Success(new PagedResult<GroupDto>
        {
            Data = new[]
            {
                new GroupDto { Id = Guid.NewGuid(), Name = "Test Group", Type = "Team" }
            },
            TotalCount = 1,
            CurrentPage = 1,
            PageSize = 10
        });

        _mockGroupService.Setup(s => s.GetGroupsAsync(request.Page, request.PageSize, request.Search, request.Type))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.GetGroups(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResult<GroupDto>>(okResult.Value);
        Assert.Single(pagedResult.Data);
        Assert.Equal("Test Group", pagedResult.Data.First().Name);
    }

    [Fact]
    public async Task GetGroups_ShouldReturnBadRequest_WhenServiceFails()
    {
        // Arrange
        var request = new GetGroupsRequest { Page = 1, PageSize = 10 };
        var serviceResult = Result<PagedResult<GroupDto>>.Failure("Service error");

        _mockGroupService.Setup(s => s.GetGroupsAsync(request.Page, request.PageSize, request.Search, request.Type))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.GetGroups(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Service error", badRequestResult.Value);
    }

    [Fact]
    public async Task GetGroup_ShouldReturnOk_WhenGroupExists()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var groupDto = new GroupDto { Id = groupId, Name = "Test Group", Type = "Team" };
        var getResult = Result<GroupDto>.Success(groupDto);
        var accessResult = Result<bool>.Success(true);

        _mockGroupService.Setup(s => s.GetByIdAsync(groupId))
            .ReturnsAsync(getResult);
        _mockGroupService.Setup(s => s.CanUserAccessGroupAsync("test-user-id", groupId))
            .ReturnsAsync(accessResult);

        // Act
        var result = await _controller.GetGroup(groupId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedGroup = Assert.IsType<GroupDto>(okResult.Value);
        Assert.Equal(groupId, returnedGroup.Id);
        Assert.Equal("Test Group", returnedGroup.Name);
    }

    [Fact]
    public async Task GetGroup_ShouldReturnNotFound_WhenGroupDoesNotExist()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var getResult = Result<GroupDto>.Failure("Group not found");

        _mockGroupService.Setup(s => s.GetByIdAsync(groupId))
            .ReturnsAsync(getResult);

        // Act
        var result = await _controller.GetGroup(groupId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Group not found", notFoundResult.Value);
    }

    [Fact]
    public async Task GetGroup_ShouldReturnForbid_WhenUserCannotAccess()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var groupDto = new GroupDto { Id = groupId, Name = "Test Group", Type = "Team" };
        var getResult = Result<GroupDto>.Success(groupDto);
        var accessResult = Result<bool>.Success(false);

        _mockGroupService.Setup(s => s.GetByIdAsync(groupId))
            .ReturnsAsync(getResult);
        _mockGroupService.Setup(s => s.CanUserAccessGroupAsync("test-user-id", groupId))
            .ReturnsAsync(accessResult);

        // Act
        var result = await _controller.GetGroup(groupId);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreateGroup_ShouldReturnCreated_WhenValidRequest()
    {
        // Arrange
        var request = new CreateGroupRequest
        {
            Name = "New Group",
            Description = "New Description",
            Type = GroupType.Team
        };

        var createdGroup = new GroupDto
        {
            Id = Guid.NewGuid(),
            Name = "New Group",
            Description = "New Description",
            Type = "Team"
        };

        var createResult = Result<GroupDto>.Success(createdGroup);
        var addUserResult = Result<bool>.Success(true);

        _mockGroupService.Setup(s => s.CreateAsync(request, "test-user-id"))
            .ReturnsAsync(createResult);
        _mockGroupService.Setup(s => s.AddUserToGroupAsync(createdGroup.Id, "test-user-id", UserGroupRole.Owner, "test-user-id"))
            .ReturnsAsync(addUserResult);

        // Act
        var result = await _controller.CreateGroup(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(GroupsController.GetGroup), createdResult.ActionName);

        var returnedGroup = Assert.IsType<GroupDto>(createdResult.Value);
        Assert.Equal("New Group", returnedGroup.Name);

        // Verify that user was added as owner
        _mockGroupService.Verify(s => s.AddUserToGroupAsync(
            createdGroup.Id,
            "test-user-id",
            UserGroupRole.Owner,
            "test-user-id"), Times.Once);
    }

    [Fact]
    public async Task CreateGroup_ShouldReturnBadRequest_WhenServiceFails()
    {
        // Arrange
        var request = new CreateGroupRequest
        {
            Name = "New Group",
            Description = "New Description",
            Type = GroupType.Team
        };

        var createResult = Result<GroupDto>.Failure("Creation failed");

        _mockGroupService.Setup(s => s.CreateAsync(request, "test-user-id"))
            .ReturnsAsync(createResult);

        // Act
        var result = await _controller.CreateGroup(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Creation failed", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateGroup_ShouldReturnOk_WhenUserIsAdmin()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var request = new UpdateGroupRequest
        {
            Name = "Updated Group",
            Description = "Updated Description",
            Type = GroupType.Department
        };

        var roleResult = Result<UserGroupRole?>.Success(UserGroupRole.Admin);
        var updateResult = Result<GroupDto>.Success(new GroupDto
        {
            Id = groupId,
            Name = "Updated Group",
            Description = "Updated Description",
            Type = "Department"
        });

        _mockGroupService.Setup(s => s.GetUserRoleInGroupAsync("test-user-id", groupId))
            .ReturnsAsync(roleResult);
        _mockGroupService.Setup(s => s.UpdateAsync(groupId, request, "test-user-id"))
            .ReturnsAsync(updateResult);

        // Act
        var result = await _controller.UpdateGroup(groupId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedGroup = Assert.IsType<GroupDto>(okResult.Value);
        Assert.Equal("Updated Group", updatedGroup.Name);
    }

    [Fact]
    public async Task UpdateGroup_ShouldReturnForbid_WhenUserIsNotAdminOrOwner()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var request = new UpdateGroupRequest
        {
            Name = "Updated Group",
            Description = "Updated Description",
            Type = GroupType.Department
        };

        var roleResult = Result<UserGroupRole?>.Success(UserGroupRole.Member);

        _mockGroupService.Setup(s => s.GetUserRoleInGroupAsync("test-user-id", groupId))
            .ReturnsAsync(roleResult);

        // Act
        var result = await _controller.UpdateGroup(groupId, request);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteGroup_ShouldReturnNoContent_WhenUserIsOwner()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var roleResult = Result<UserGroupRole?>.Success(UserGroupRole.Owner);
        var deleteResult = Result<bool>.Success(true);

        _mockGroupService.Setup(s => s.GetUserRoleInGroupAsync("test-user-id", groupId))
            .ReturnsAsync(roleResult);
        _mockGroupService.Setup(s => s.DeleteAsync(groupId, "test-user-id"))
            .ReturnsAsync(deleteResult);

        // Act
        var result = await _controller.DeleteGroup(groupId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteGroup_ShouldReturnForbid_WhenUserIsNotOwner()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var roleResult = Result<UserGroupRole?>.Success(UserGroupRole.Admin);

        _mockGroupService.Setup(s => s.GetUserRoleInGroupAsync("test-user-id", groupId))
            .ReturnsAsync(roleResult);

        // Act
        var result = await _controller.DeleteGroup(groupId);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetMyGroups_ShouldReturnOk_WhenServiceSucceeds()
    {
        // Arrange
        var groups = new[]
        {
            new GroupDto { Id = Guid.NewGuid(), Name = "My Group 1", Type = "Team" },
            new GroupDto { Id = Guid.NewGuid(), Name = "My Group 2", Type = "Department" }
        };

        var serviceResult = Result<IEnumerable<GroupDto>>.Success(groups);

        _mockGroupService.Setup(s => s.GetUserGroupsAsync("test-user-id"))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.GetMyGroups();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedGroups = Assert.IsAssignableFrom<IEnumerable<GroupDto>>(okResult.Value);
        Assert.Equal(2, returnedGroups.Count());
    }

    [Theory]
    [InlineData(UserGroupRole.Admin)]
    [InlineData(UserGroupRole.Owner)]
    public async Task AddMemberToGroup_ShouldReturnCreated_WhenUserCanManageGroup(UserGroupRole userRole)
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var request = new AddGroupMemberRequest
        {
            UserId = "new-member-id",
            Role = UserGroupRole.Member
        };

        var roleResult = Result<UserGroupRole?>.Success(userRole);
        var addResult = Result<bool>.Success(true);

        _mockGroupService.Setup(s => s.GetUserRoleInGroupAsync("test-user-id", groupId))
            .ReturnsAsync(roleResult);
        _mockGroupService.Setup(s => s.AddUserToGroupAsync(groupId, request.UserId, request.Role, "test-user-id"))
            .ReturnsAsync(addResult);

        // Act
        var result = await _controller.AddMemberToGroup(groupId, request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(GroupsController.GetGroupMembers), createdResult.ActionName);
    }

    [Fact]
    public async Task AddMemberToGroup_ShouldReturnForbid_WhenUserCannotManageGroup()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var request = new AddGroupMemberRequest
        {
            UserId = "new-member-id",
            Role = UserGroupRole.Member
        };

        var roleResult = Result<UserGroupRole?>.Success(UserGroupRole.Member);

        _mockGroupService.Setup(s => s.GetUserRoleInGroupAsync("test-user-id", groupId))
            .ReturnsAsync(roleResult);

        // Act
        var result = await _controller.AddMemberToGroup(groupId, request);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }
}