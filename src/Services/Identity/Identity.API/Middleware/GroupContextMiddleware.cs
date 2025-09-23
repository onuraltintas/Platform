using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Identity.API.Middleware;

public class GroupContextMiddleware
{
    private readonly RequestDelegate _next;
    private const string GroupIdHeaderName = "X-Group-Id";

    public GroupContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to resolve GroupId from header -> query -> route values
        string? groupId = null;

        if (context.Request.Headers.TryGetValue(GroupIdHeaderName, out var headerValues))
        {
            groupId = headerValues.FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(groupId))
        {
            groupId = context.Request.Query["groupId"].FirstOrDefault()
                   ?? context.Request.Query["GroupId"].FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(groupId))
        {
            if (context.Request.RouteValues.TryGetValue("groupId", out var routeGroup) && routeGroup is string s1)
            {
                groupId = s1;
            }
            else if (context.Request.RouteValues.TryGetValue("GroupId", out var routeGroup2) && routeGroup2 is string s2)
            {
                groupId = s2;
            }
        }

        // If we have a valid Guid, attach as a claim for downstream authorization handlers
        if (!string.IsNullOrWhiteSpace(groupId) && Guid.TryParse(groupId, out _))
        {
            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim("GroupId", groupId));

            // Attach alongside existing identities
            context.User = new ClaimsPrincipal(new[] { context.User.Identity as ClaimsIdentity, identity }
                .Where(ci => ci != null)!
                .Cast<ClaimsIdentity>());
        }

        await _next(context);
    }
}

