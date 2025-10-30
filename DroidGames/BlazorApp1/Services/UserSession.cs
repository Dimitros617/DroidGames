using BlazorApp1.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BlazorApp1.Services;

public class UserSession : IDisposable
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private User? _currentUser;
    private bool _isInitialized = false;
    private bool _disposed = false;
    private TaskCompletionSource<bool>? _initializationTcs;

    public UserSession(ProtectedSessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public User? CurrentUser => _currentUser;

    public bool IsAuthenticated => _currentUser != null;

    public string? UserId => _currentUser?.Id;

    public string? Username => _currentUser?.Username;

    public UserRole Role => _currentUser?.Role ?? UserRole.Public;

    public string? TeamId => _currentUser?.TeamId;

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        // Pokud už probíhá inicializace, počkej na ni
        if (_initializationTcs != null && !_initializationTcs.Task.IsCompleted)
        {
            await _initializationTcs.Task;
            return;
        }

        // Pokud už jsme inicializováni a máme uživatele, jen notifikuj
        if (_isInitialized && _currentUser != null)
        {
            NotifyStateChanged();
            return;
        }

        // Pokud jsme inicializováni ale nemáme uživatele, zkus znovu načíst
        if (_isInitialized && _currentUser == null)
        {
            _isInitialized = false; // Reset flag pro opětovné načtení
        }

        _initializationTcs = new TaskCompletionSource<bool>();

        try
        {
            var result = await _sessionStorage.GetAsync<User>("currentUser");
            if (result.Success && result.Value != null)
            {
                _currentUser = result.Value;
                NotifyStateChanged(); // Notifikuj komponenty o obnoveném uživateli
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("prerendering"))
        {
            // Během prerendering nemůžeme použít JS interop, to je v pořádku
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserSession] Error loading from storage: {ex.Message}");
        }
        
        _isInitialized = true;
        _initializationTcs.SetResult(true);
    }

    public async Task SetUserAsync(User user)
    {
        _currentUser = user;
        
        try
        {
            await _sessionStorage.SetAsync("currentUser", user);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("prerendering"))
        {
            // Během prerendering nemůžeme použít JS interop
            Console.WriteLine("[UserSession] Cannot save to storage during prerendering");
        }
        
        Console.WriteLine($"[UserSession] User set: {user.Username} (Role: {user.Role})");
        NotifyStateChanged();
    }

    public async Task ClearUserAsync()
    {
        _currentUser = null;
        _isInitialized = false; // Reset flag aby se při příštím InitializeAsync načetlo znovu
        
        try
        {
            await _sessionStorage.DeleteAsync("currentUser");
            Console.WriteLine("[UserSession] User cleared (logout) - storage deleted");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("prerendering"))
        {
            // Během prerendering nemůžeme použít JS interop
            Console.WriteLine("[UserSession] Cannot clear storage during prerendering");
        }
        
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

    // IDisposable implementation - cleanup resources when circuit is disposed
    public void Dispose()
    {
        if (!_disposed)
        {
            _currentUser = null;
            _isInitialized = false;
            
            // Unsubscribe all event handlers to prevent memory leaks
            if (OnChange != null)
            {
                foreach (var d in OnChange.GetInvocationList())
                {
                    OnChange -= (Action)d;
                }
            }
            
            _disposed = true;
            Console.WriteLine("[UserSession] Disposed - cleaned up resources");
        }
    }
}
