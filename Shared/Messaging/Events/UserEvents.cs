namespace EgitimPlatform.Shared.Messaging.Events;

public class UserRegisteredEvent : IntegrationEvent
{
    public UserRegisteredEvent(string userId, string email, string firstName, string lastName)
    {
        UserId = userId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }

    public string UserId { get; private set; }
    public string Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
}

public class UserProfileUpdatedEvent : IntegrationEvent
{
    public UserProfileUpdatedEvent(string userId, string email, string firstName, string lastName)
    {
        UserId = userId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }

    public string UserId { get; private set; }
    public string Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
}

public class UserEmailVerifiedEvent : IntegrationEvent
{
    public UserEmailVerifiedEvent(string userId, string email)
    {
        UserId = userId;
        Email = email;
    }

    public string UserId { get; private set; }
    public string Email { get; private set; }
}

public class UserPasswordChangedEvent : IntegrationEvent
{
    public UserPasswordChangedEvent(string userId, string email)
    {
        UserId = userId;
        Email = email;
    }

    public string UserId { get; private set; }
    public string Email { get; private set; }
}

public class UserAccountLockedEvent : IntegrationEvent
{
    public UserAccountLockedEvent(string userId, string email, string reason, DateTime lockedUntil)
    {
        UserId = userId;
        Email = email;
        Reason = reason;
        LockedUntil = lockedUntil;
    }

    public string UserId { get; private set; }
    public string Email { get; private set; }
    public string Reason { get; private set; }
    public DateTime LockedUntil { get; private set; }
}

public class UserLoginEvent : IntegrationEvent
{
    public UserLoginEvent(string userId, string email, string ipAddress, string userAgent)
    {
        UserId = userId;
        Email = email;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public string UserId { get; private set; }
    public string Email { get; private set; }
    public string IpAddress { get; private set; }
    public string UserAgent { get; private set; }
}

public class UserLogoutEvent : IntegrationEvent
{
    public UserLogoutEvent(string userId, string email)
    {
        UserId = userId;
        Email = email;
    }

    public string UserId { get; private set; }
    public string Email { get; private set; }
}