using BlazorApp1.Models;

namespace BlazorApp1.Services;

public class UserSession
{
    public User? CurrentUser { get; set; }
    public bool IsAuthenticated => CurrentUser != null;
    
    public bool HasRole(UserRole role)
    {
        return CurrentUser?.Role == role;
    }
    
    public bool HasAnyRole(params UserRole[] roles)
    {
        return CurrentUser != null && roles.Contains(CurrentUser.Role);
    }
}
