using BlazorApp1.Models;

namespace BlazorApp1.Services;

public class UserSession
{
    private User? _currentUser;

    public User? CurrentUser => _currentUser;

    public bool IsAuthenticated => _currentUser != null;

    public string? UserId => _currentUser?.Id;

    public string? Username => _currentUser?.Username;

    public UserRole Role => _currentUser?.Role ?? UserRole.Public;

    public string? TeamId => _currentUser?.TeamId;

    public event Action? OnChange;

    public void SetUser(User user)
    {
        _currentUser = user;
        Console.WriteLine($"[UserSession] User set: {user.Username} (Role: {user.Role})");
        NotifyStateChanged();
    }

    public void ClearUser()
    {
        _currentUser = null;
        Console.WriteLine("[UserSession] User cleared (logout)");
        NotifyStateChanged();
    }
    
    public bool HasRole(UserRole role)
    {
        return _currentUser?.Role == role;
    }
    
    public bool HasAnyRole(params UserRole[] roles)
    {
        return _currentUser != null && roles.Contains(_currentUser.Role);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
