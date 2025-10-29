using BlazorApp1.Models;

namespace BlazorApp1.Services;

public interface ITeamService
{
    Task<List<Team>> GetAllTeamsAsync();
    Task<Team?> GetTeamByIdAsync(string id);
    Task<Team?> GetTeamByPinAsync(string pin);
    Task<Team> AddTeamAsync(Team team);
    Task<Team> UpdateTeamAsync(Team team);
    Task<bool> DeleteTeamAsync(string id);
    Task<List<Team>> GetLeaderboardAsync();
    Task UpdateTeamScoresAsync();
}
