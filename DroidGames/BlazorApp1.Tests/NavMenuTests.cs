using System.Threading.Tasks;
using BlazorApp1.Components.Layout;
using BlazorApp1.Models;
using BlazorApp1.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xunit;

namespace BlazorApp1.Tests;

public class NavMenuTests : TestContext
{
    private readonly UserSession _userSession;

    public NavMenuTests()
    {
        Services.AddLogging();
        Services.AddDataProtection();
        Services.AddSingleton<NavigationManager>(new TestNavigationManager());

        JSInterop.Setup<ProtectedBrowserStorageResult<User>>("Blazor._internal.protectedBrowserStorage.get", _ => true)
            .SetResult(default);
        JSInterop.SetupVoid("Blazor._internal.protectedBrowserStorage.set", _ => true);
        JSInterop.SetupVoid("Blazor._internal.protectedBrowserStorage.delete", _ => true);
        JSInterop.SetupVoid("sessionStorage.setItem", _ => true);
        JSInterop.SetupVoid("sessionStorage.removeItem", _ => true);
        JSInterop.SetupVoid("sessionStorage.getItem", _ => true);
        Services.AddSingleton<UserSession>(sp => new UserSession(
            new ProtectedSessionStorage(
                sp.GetRequiredService<IJSRuntime>(),
                sp.GetRequiredService<IDataProtectionProvider>())));

        _userSession = Services.GetRequiredService<UserSession>();
    }

    [Fact]
    public void NavMenu_hides_private_links_for_public_user()
    {
        // Ověří, že nepřihlášený vidí jen veřejné odkazy a tlačítko přihlášení
        var cut = RenderComponent<NavMenu>();

        Assert.DoesNotContain("Dashboard", cut.Markup);
        Assert.DoesNotContain("Admin", cut.Markup);
        Assert.Contains("Přihlásit se", cut.Markup);
    }

    [Fact]
    public async Task NavMenu_shows_team_links_for_team_user()
    {
        // Ověří, že týmový uživatel vidí dashboard/statistiky/achievementy
        SetUserDirectly(_userSession, new User
        {
            Id = "team-1",
            TeamId = "team-1",
            Username = "TeamUser",
            Role = UserRole.Team
        });

        var cut = RenderComponent<NavMenu>();

        Assert.Contains("Dashboard", cut.Markup);
        Assert.Contains("Statistiky", cut.Markup);
        Assert.Contains("Achievementy", cut.Markup);
        Assert.DoesNotContain("Admin", cut.Markup);
    }

    [Fact]
    public async Task NavMenu_shows_headref_links_for_headref()
    {
        // Ověří, že hlavní rozhodčí vidí všechny „admin“ položky
        SetUserDirectly(_userSession, new User
        {
            Id = "admin-1",
            Username = "admin",
            Role = UserRole.HeadReferee
        });

        var cut = RenderComponent<NavMenu>();

        Assert.Contains("Bodování", cut.Markup);
        Assert.Contains("Schvalování", cut.Markup);
        Assert.Contains("Hlavní rozhodčí", cut.Markup);
        Assert.DoesNotContain("Přihlásit se", cut.Markup);
    }

    private static void SetUserDirectly(UserSession session, User user)
    {
        typeof(UserSession).GetField("_currentUser", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(session, user);
        typeof(UserSession).GetField("_isInitialized", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(session, true);
    }
}
