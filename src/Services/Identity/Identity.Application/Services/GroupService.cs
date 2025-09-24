using Identity.Core.Interfaces;
using Identity.Core.DTOs;
using Identity.Core.Entities;
using Enterprise.Shared.Common.Models;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Enterprise.Shared.Caching.Interfaces;

namespace Identity.Application.Services;

public class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GroupService> _logger;
    private readonly ICacheService _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    public GroupService(
        IGroupRepository groupRepository,
        IMapper mapper,
        ILogger<GroupService> logger,
        ICacheService cache)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<GroupDto>> GetByIdAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"group:{groupId}";

            // Use the shared caching service's GetOrSetAsync for better performance
            var groupDto = await _cache.GetOrSetAsync(cacheKey, async () =>
            {
                var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
                if (group == null)
                {
                    throw new InvalidOperationException("Grup bulunamadı");
                }

                var dto = _mapper.Map<GroupDto>(group);
                dto.CurrentUserCount = await _groupRepository.GetMemberCountAsync(groupId, cancellationToken);
                return dto;
            }, _cacheExpiration, cancellationToken);

            if (groupDto == null)
            {
                return Result<GroupDto>.Failure("Grup bulunamadı");
            }
            return Result<GroupDto>.Success(groupDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group by id {GroupId}", groupId);
            return Result<GroupDto>.Failure("Grup getirilemedi");
        }
    }

    public async Task<Result<GroupDto>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var group = await _groupRepository.GetByNameAsync(name, cancellationToken);
            if (group == null)
            {
                return Result<GroupDto>.Failure("Grup bulunamadı");
            }

            var groupDto = _mapper.Map<GroupDto>(group);
            groupDto.CurrentUserCount = await _groupRepository.GetMemberCountAsync(group.Id, cancellationToken);

            return Result<GroupDto>.Success(groupDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group by name {Name}", name);
            return Result<GroupDto>.Failure("Grup getirilemedi");
        }
    }

    public async Task<Result<PagedResult<GroupDto>>> GetGroupsAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        GroupType? type = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _groupRepository.GetGroupsAsync(page, pageSize, search, type, cancellationToken);

            var groupDtos = new List<GroupDto>();
            foreach (var group in result.Data)
            {
                var dto = _mapper.Map<GroupDto>(group);
                dto.CurrentUserCount = await _groupRepository.GetMemberCountAsync(group.Id, cancellationToken);
                groupDtos.Add(dto);
            }

            var pagedResult = new PagedResult<GroupDto>
            {
                Data = groupDtos,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };

            return Result<PagedResult<GroupDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups");
            return Result<PagedResult<GroupDto>>.Failure("Gruplar getirilemedi");
        }
    }

    public async Task<Result<GroupDto>> CreateAsync(CreateGroupRequest request, string createdBy, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<GroupDto>.Failure("Grup adı gereklidir");
            }

            // Check if name is already taken
            if (await _groupRepository.IsNameTakenAsync(request.Name, null, cancellationToken))
            {
                return Result<GroupDto>.Failure("Bu grup adı zaten kullanılmaktadır");
            }

            // Create group
            var group = _mapper.Map<Group>(request);
            group.Id = Guid.NewGuid();
            group.CreatedBy = createdBy;
            group.CreatedAt = DateTime.UtcNow;
            group.IsActive = true;

            var createdGroup = await _groupRepository.CreateAsync(group, cancellationToken);

            var groupDto = _mapper.Map<GroupDto>(createdGroup);
            groupDto.CurrentUserCount = 0;

            // Clear cache
            await _cache.RemoveAsync($"group:{createdGroup.Id}", cancellationToken);

            _logger.LogInformation("Group created successfully: {GroupId} by {CreatedBy}", createdGroup.Id, createdBy);
            return Result<GroupDto>.Success(groupDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group {Name}", request.Name);
            return Result<GroupDto>.Failure("Grup oluşturulamadı");
        }
    }

    public async Task<Result<GroupDto>> UpdateAsync(Guid groupId, UpdateGroupRequest request, string modifiedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
            if (group == null)
            {
                return Result<GroupDto>.Failure("Grup bulunamadı");
            }

            // Validate name uniqueness if changed
            if (group.Name != request.Name && await _groupRepository.IsNameTakenAsync(request.Name, groupId, cancellationToken))
            {
                return Result<GroupDto>.Failure("Bu grup adı zaten kullanılmaktadır");
            }

            // Update group
            group.Name = request.Name;
            group.Description = request.Description;
            group.Website = request.Website;
            group.ContactEmail = request.ContactEmail;
            group.ContactPhone = request.ContactPhone;
            group.MaxUsers = request.MaxUsers;
            group.SubscriptionPlan = request.SubscriptionPlan;
            group.LogoUrl = request.LogoUrl;
            group.LastModifiedBy = modifiedBy;
            group.LastModifiedAt = DateTime.UtcNow;

            var updatedGroup = await _groupRepository.UpdateAsync(group, cancellationToken);

            var groupDto = _mapper.Map<GroupDto>(updatedGroup);
            groupDto.CurrentUserCount = await _groupRepository.GetMemberCountAsync(groupId, cancellationToken);

            // Clear cache
            await _cache.RemoveAsync($"group:{groupId}", cancellationToken);

            return Result<GroupDto>.Success(groupDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId}", groupId);
            return Result<GroupDto>.Failure("Grup güncellenemedi");
        }
    }

    public async Task<Result<bool>> DeleteAsync(Guid groupId, string deletedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
            if (group == null)
            {
                return Result<bool>.Failure("Grup bulunamadı");
            }

            // Check if group has members
            var memberCount = await _groupRepository.GetMemberCountAsync(groupId, cancellationToken);
            if (memberCount > 0)
            {
                return Result<bool>.Failure("Üyeleri olan grup silinemez. Önce tüm üyeleri gruptan çıkarın");
            }

            var success = await _groupRepository.DeleteAsync(groupId, cancellationToken);
            if (!success)
            {
                return Result<bool>.Failure("Grup silinemedi");
            }

            // Clear cache
            await _cache.RemoveAsync($"group:{groupId}", cancellationToken);

            _logger.LogInformation("Group deleted: {GroupId} by {DeletedBy}", groupId, deletedBy);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId}", groupId);
            return Result<bool>.Failure("Grup silinemedi");
        }
    }

    public async Task<Result<bool>> AddUserToGroupAsync(
        Guid groupId,
        string userId,
        UserGroupRole role = UserGroupRole.Member,
        string? invitedBy = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if group exists
            if (!await _groupRepository.ExistsAsync(groupId, cancellationToken))
            {
                return Result<bool>.Failure("Grup bulunamadı");
            }

            // Check if user is already in group
            var existingRelation = await _groupRepository.GetUserGroupRelationAsync(userId, groupId, cancellationToken);
            if (existingRelation != null)
            {
                return Result<bool>.Failure("Kullanıcı zaten bu grupda yer alıyor");
            }

            // Check group member limit
            var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
            var currentMemberCount = await _groupRepository.GetMemberCountAsync(groupId, cancellationToken);

            if (group != null && currentMemberCount >= group.MaxUsers)
            {
                return Result<bool>.Failure("Grup üye limiti aşıldı");
            }

            var userGroup = new UserGroup
            {
                UserId = userId,
                GroupId = groupId,
                Role = role,
                InvitedBy = invitedBy,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _groupRepository.AddUserToGroupAsync(userGroup, cancellationToken);

            // Clear cache
            await _cache.RemoveAsync($"group:{groupId}", cancellationToken);
            await _cache.RemoveAsync($"user_groups:{userId}", cancellationToken);

            _logger.LogInformation("User {UserId} added to group {GroupId} with role {Role}", userId, groupId, role);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to group {GroupId}", userId, groupId);
            return Result<bool>.Failure("Kullanıcı gruba eklenemedi");
        }
    }

    public async Task<Result<bool>> RemoveUserFromGroupAsync(Guid groupId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _groupRepository.RemoveUserFromGroupAsync(userId, groupId, cancellationToken);
            if (!success)
            {
                return Result<bool>.Failure("Kullanıcı gruptan çıkarılamadı");
            }

            // Clear cache
            await _cache.RemoveAsync($"group:{groupId}", cancellationToken);
            await _cache.RemoveAsync($"user_groups:{userId}", cancellationToken);

            _logger.LogInformation("User {UserId} removed from group {GroupId}", userId, groupId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from group {GroupId}", userId, groupId);
            return Result<bool>.Failure("Kullanıcı gruptan çıkarılamadı");
        }
    }

    public async Task<Result<bool>> UpdateUserRoleInGroupAsync(Guid groupId, string userId, UserGroupRole role, CancellationToken cancellationToken = default)
    {
        try
        {
            var userGroup = await _groupRepository.GetUserGroupRelationAsync(userId, groupId, cancellationToken);
            if (userGroup == null)
            {
                return Result<bool>.Failure("Kullanıcı bu grupda yer almıyor");
            }

            userGroup.Role = role;

            // Update through context (assuming EF Core change tracking)
            await _groupRepository.UpdateAsync(userGroup.Group, cancellationToken);

            // Clear cache
            await _cache.RemoveAsync($"group:{groupId}", cancellationToken);
            await _cache.RemoveAsync($"user_groups:{userId}", cancellationToken);

            _logger.LogInformation("User {UserId} role updated to {Role} in group {GroupId}", userId, role, groupId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId} role in group {GroupId}", userId, groupId);
            return Result<bool>.Failure("Kullanıcı rolü güncellenemedi");
        }
    }

    public async Task<Result<PagedResult<GroupMemberDto>>> GetGroupMembersAsync(
        Guid groupId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _groupRepository.GetGroupMembersAsync(groupId, page, pageSize, cancellationToken);

            var memberDtos = result.Data.Select(user => _mapper.Map<GroupMemberDto>(user)).ToList();

            var pagedResult = new PagedResult<GroupMemberDto>
            {
                Data = memberDtos,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };

            return Result<PagedResult<GroupMemberDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group members for {GroupId}", groupId);
            return Result<PagedResult<GroupMemberDto>>.Failure("Grup üyeleri getirilemedi");
        }
    }

    public async Task<Result<IEnumerable<GroupDto>>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"user_groups:{userId}";

            var groupDtos = await _cache.GetOrSetAsync<List<GroupDto>>(cacheKey, async () =>
            {
                var groups = await _groupRepository.GetUserGroupsAsync(userId, cancellationToken);
                var dtos = new List<GroupDto>();

                foreach (var group in groups)
                {
                    var dto = _mapper.Map<GroupDto>(group);
                    dto.CurrentUserCount = await _groupRepository.GetMemberCountAsync(group.Id, cancellationToken);
                    dtos.Add(dto);
                }

                return dtos;
            }, _cacheExpiration, cancellationToken);

            return Result<IEnumerable<GroupDto>>.Success(groupDtos ?? new List<GroupDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user groups for {UserId}", userId);
            return Result<IEnumerable<GroupDto>>.Failure("Kullanıcı grupları getirilemedi");
        }
    }

    public async Task<Result<bool>> IsUserInGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var relation = await _groupRepository.GetUserGroupRelationAsync(userId, groupId, cancellationToken);
            return Result<bool>.Success(relation != null && relation.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} is in group {GroupId}", userId, groupId);
            return Result<bool>.Success(false);
        }
    }

    public async Task<Result<UserGroupRole?>> GetUserRoleInGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var relation = await _groupRepository.GetUserGroupRelationAsync(userId, groupId, cancellationToken);
            var role = relation?.IsActive == true ? relation.Role : (UserGroupRole?)null;
            return Result<UserGroupRole?>.Success(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId} role in group {GroupId}", userId, groupId);
            return Result<UserGroupRole?>.Success(null);
        }
    }

    // Placeholder implementations for remaining methods
    public Task<Result<string>> GenerateInvitationCodeAsync(Guid groupId, UserGroupRole role, DateTime? expiresAt = null, CancellationToken cancellationToken = default)
    {
        // TODO: Implement invitation system
        return Task.FromResult(Result<string>.Failure("Davet sistemi henüz implementedilmemiştir"));
    }

    public Task<Result<GroupDto>> JoinByInvitationCodeAsync(string userId, string invitationCode, CancellationToken cancellationToken = default)
    {
        // TODO: Implement invitation system
        return Task.FromResult(Result<GroupDto>.Failure("Davet sistemi henüz implementedilmemiştir"));
    }

    public async Task<Result<bool>> CanUserAccessGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic implementation: check if user is member of the group
            var isUserInGroup = await IsUserInGroupAsync(userId, groupId, cancellationToken);
            return isUserInGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user {UserId} access to group {GroupId}", userId, groupId);
            return Result<bool>.Success(false);
        }
    }

    // New Group Invitation System Methods
    public async Task<Result<GroupInvitationDto>> CreateInvitationAsync(Guid groupId, CreateGroupInvitationRequest request, string createdBy, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify group exists
            var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
            if (group == null)
            {
                return Result<GroupInvitationDto>.Failure("Grup bulunamadı");
            }

            // Create invitation
            var invitation = new GroupInvitation
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                InvitationCode = GenerateSecureCode(),
                Role = request.Role,
                ExpiresAt = DateTime.UtcNow.AddHours(request.ExpirationHours),
                MaxUses = request.MaxUses,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Note = request.Note,
                AllowedEmails = request.AllowedEmails != null ? System.Text.Json.JsonSerializer.Serialize(request.AllowedEmails) : null,
                AllowedDomains = request.AllowedDomains != null ? System.Text.Json.JsonSerializer.Serialize(request.AllowedDomains) : null
            };

            // TODO: Save invitation to database
            // await _groupRepository.CreateInvitationAsync(invitation, cancellationToken);

            var dto = new GroupInvitationDto
            {
                Id = invitation.Id,
                GroupId = invitation.GroupId,
                GroupName = group.Name,
                InvitationCode = invitation.InvitationCode,
                InvitationLink = $"/groups/join/{invitation.InvitationCode}",
                Role = invitation.Role,
                ExpiresAt = invitation.ExpiresAt,
                MaxUses = invitation.MaxUses,
                UsedCount = 0,
                IsActive = invitation.IsActive,
                CreatedBy = invitation.CreatedBy,
                CreatedAt = invitation.CreatedAt,
                Note = invitation.Note,
                AllowedEmails = request.AllowedEmails,
                AllowedDomains = request.AllowedDomains
            };

            _logger.LogInformation("Group invitation created for group {GroupId} by {CreatedBy}", groupId, createdBy);
            return Result<GroupInvitationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invitation for group {GroupId}", groupId);
            return Result<GroupInvitationDto>.Failure("Davet oluşturulamadı");
        }
    }

    public async Task<Result<GroupDto>> JoinByInvitationAsync(string userId, string invitationCode, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement invitation validation and group joining
            // 1. Find invitation by code
            // 2. Validate expiration and usage limits
            // 3. Check allowed emails/domains
            // 4. Add user to group
            // 5. Update invitation usage

            _logger.LogWarning("JoinByInvitationAsync not fully implemented");
            return Result<GroupDto>.Failure("Davet sistemi henüz tam implement edilmemiştir");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining group by invitation {Code}", invitationCode);
            return Result<GroupDto>.Failure("Gruba katılınamadı");
        }
    }

    public async Task<Result<bool>> RevokeInvitationAsync(Guid invitationId, string revokedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement invitation revocation
            _logger.LogWarning("RevokeInvitationAsync not fully implemented");
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking invitation {InvitationId}", invitationId);
            return Result<bool>.Failure("Davet iptal edilemedi");
        }
    }

    public async Task<Result<IEnumerable<GroupInvitationDto>>> GetGroupInvitationsAsync(Guid groupId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement fetching group invitations
            _logger.LogWarning("GetGroupInvitationsAsync not fully implemented");
            return Result<IEnumerable<GroupInvitationDto>>.Success(new List<GroupInvitationDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invitations for group {GroupId}", groupId);
            return Result<IEnumerable<GroupInvitationDto>>.Failure("Davetler getirilemedi");
        }
    }

    public async Task<Result<GroupInvitationDto>> GetInvitationByCodeAsync(string invitationCode, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement fetching invitation by code
            _logger.LogWarning("GetInvitationByCodeAsync not fully implemented");
            return Result<GroupInvitationDto>.Failure("Davet bulunamadı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invitation by code {Code}", invitationCode);
            return Result<GroupInvitationDto>.Failure("Davet getirilemedi");
        }
    }

    public async Task<Result<GroupStatisticsDto>> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = "group:statistics";

            var statistics = await _cache.GetOrSetAsync(cacheKey, async () =>
            {
                // Get all groups for statistics
                var allGroupsResult = await _groupRepository.GetGroupsAsync(1, int.MaxValue, null, null, cancellationToken);

                if (allGroupsResult == null || allGroupsResult.Data == null)
                {
                    return new GroupStatisticsDto
                    {
                        TotalGroups = 0,
                        SystemGroups = 0,
                        CustomGroups = 0,
                        TotalMembers = 0,
                        AverageMembersPerGroup = 0,
                        EmptyGroups = 0
                    };
                }

                var groups = allGroupsResult.Data.ToList();
                var totalGroups = groups.Count;
                var systemGroups = groups.Count(g => g.Name.StartsWith("System_") || g.Name.StartsWith("Role_"));
                var customGroups = totalGroups - systemGroups;

                // Count members and find largest group
                var totalMembers = 0;
                var largestGroupSize = 0;
                Group? largestGroup = null;
                var emptyGroups = 0;

                foreach (var group in groups)
                {
                    var memberCount = await _groupRepository.GetMemberCountAsync(group.Id, cancellationToken);
                    totalMembers += memberCount;

                    if (memberCount == 0)
                    {
                        emptyGroups++;
                    }

                    if (memberCount > largestGroupSize)
                    {
                        largestGroupSize = memberCount;
                        largestGroup = group;
                    }
                }

                var averageMembersPerGroup = totalGroups > 0 ? (double)totalMembers / totalGroups : 0;

                return new GroupStatisticsDto
                {
                    TotalGroups = totalGroups,
                    SystemGroups = systemGroups,
                    CustomGroups = customGroups,
                    TotalMembers = totalMembers,
                    AverageMembersPerGroup = Math.Round(averageMembersPerGroup, 2),
                    LargestGroup = largestGroup != null ? _mapper.Map<GroupDto>(largestGroup) : null,
                    EmptyGroups = emptyGroups
                };
            }, TimeSpan.FromMinutes(5), cancellationToken);

            return Result<GroupStatisticsDto>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group statistics");

            // Return default statistics on error
            return Result<GroupStatisticsDto>.Success(new GroupStatisticsDto
            {
                TotalGroups = 0,
                SystemGroups = 0,
                CustomGroups = 0,
                TotalMembers = 0,
                AverageMembersPerGroup = 0,
                EmptyGroups = 0
            });
        }
    }

    private string GenerateSecureCode(int length = 16)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}