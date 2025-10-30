using BlazorApp1.Data;
using BlazorApp1.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO.Compression;

namespace BlazorApp1.Services;

public class DiplomaService : IDiplomaService
{
    private readonly IRepository<Team> _teamRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly string _templatePath;

    public DiplomaService(IRepository<Team> teamRepository, IWebHostEnvironment environment)
    {
        _teamRepository = teamRepository;
        _environment = environment;
        _templatePath = Path.Combine(_environment.WebRootPath, "templates", "diploma-template.pdf");
        
        // Configure QuestPDF license (community license)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateDiplomaAsync(string teamId, string memberName, int position)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null)
            throw new ArgumentException($"Team {teamId} not found");

        // Pokud existuje template PDF, použijeme ho jako základ
        if (File.Exists(_templatePath))
        {
            return await GenerateFromTemplateAsync(team, memberName, position);
        }
        else
        {
            // Fallback - vygenerujeme PDF od nuly
            return await GenerateFromScratchAsync(team, memberName, position);
        }
    }

    public async Task<byte[]> GenerateBulkDiplomasAsync(string teamId)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null)
            throw new ArgumentException($"Team {teamId} not found");

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            int position = team.CurrentPosition;

            foreach (var member in team.Members)
            {
                var diplomaPdf = await GenerateDiplomaAsync(teamId, member, position);
                var safeFileName = $"{team.Name}_{member}.pdf"
                    .Replace(" ", "_")
                    .Replace("/", "_")
                    .Replace("\\", "_");
                    
                var entry = archive.CreateEntry(safeFileName);
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(diplomaPdf);
            }
        }

        return memoryStream.ToArray();
    }

    public async Task<byte[]> GenerateAllTeamsDiplomasAsync()
    {
        var teams = await _teamRepository.GetAllAsync();
        
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var team in teams.OrderBy(t => t.CurrentPosition))
            {
                foreach (var member in team.Members)
                {
                    var diplomaPdf = await GenerateDiplomaAsync(team.Id, member, team.CurrentPosition);
                    var safeFileName = $"{team.CurrentPosition}_{team.Name}_{member}.pdf"
                        .Replace(" ", "_")
                        .Replace("/", "_")
                        .Replace("\\", "_");
                        
                    var entry = archive.CreateEntry(safeFileName);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(diplomaPdf);
                }
            }
        }

        return memoryStream.ToArray();
    }

    private async Task<byte[]> GenerateFromTemplateAsync(Team team, string memberName, int position)
    {
        // Načteme template PDF
        var templateBytes = await File.ReadAllBytesAsync(_templatePath);

        // Vytvoříme nový PDF s overlay textem
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(0);

                // Vložíme template jako pozadí
                page.Content().Layers(layers =>
                {
                    // Layer 1: Template PDF jako obrázek (budeme potřebovat převést)
                    // TODO: Pro skutečné použití s template PDF bychom potřebovali SkiaSharp nebo ImageSharp
                    
                    // Layer 2: Text overlay
                    layers.Layer().Column(column =>
                    {
                        column.Spacing(20);

                        // Jméno člena - uprostřed nahoře
                        column.Item().AlignCenter().PaddingTop(200).Text(memberName)
                            .FontSize(36)
                            .Bold()
                            .FontColor("#2c3e50");

                        // Název týmu
                        column.Item().AlignCenter().PaddingTop(20).Text(team.Name)
                            .FontSize(28)
                            .SemiBold()
                            .FontColor("#34495e");

                        // Škola
                        column.Item().AlignCenter().PaddingTop(10).Text(team.School)
                            .FontSize(20)
                            .FontColor("#7f8c8d");

                        // Umístění
                        column.Item().AlignCenter().PaddingTop(40).Text($"{position}. místo")
                            .FontSize(32)
                            .Bold()
                            .FontColor(GetPositionColor(position));

                        // Body
                        column.Item().AlignCenter().PaddingTop(20).Text($"{team.TotalScore} bodů")
                            .FontSize(24)
                            .FontColor("#95a5a6");
                    });
                });
            });
        }).GeneratePdf();
    }

    private async Task<byte[]> GenerateFromScratchAsync(Team team, string memberName, int position)
    {
        return await Task.Run(() => Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(40);

                page.Header().AlignCenter().Column(column =>
                {
                    column.Item().PaddingBottom(20).Text("DROID GAMES 2026")
                        .FontSize(48)
                        .Bold()
                        .FontColor("#2c3e50");
                    
                    column.Item().Text("Diplom")
                        .FontSize(32)
                        .SemiBold()
                        .FontColor("#34495e");
                });

                page.Content().PaddingVertical(60).Column(column =>
                {
                    column.Spacing(30);

                    // Jméno účastníka
                    column.Item().Border(1).BorderColor("#bdc3c7").Background("#ecf0f1")
                        .Padding(30).AlignCenter().Text(memberName)
                        .FontSize(40)
                        .Bold()
                        .FontColor("#2c3e50");

                    // Tým
                    column.Item().AlignCenter().Column(inner =>
                    {
                        inner.Item().Text("člen týmu")
                            .FontSize(18)
                            .FontColor("#7f8c8d");
                        
                        inner.Item().PaddingTop(10).Text(team.Name)
                            .FontSize(32)
                            .SemiBold()
                            .FontColor("#34495e");
                    });

                    // Škola
                    column.Item().AlignCenter().Text(team.School)
                        .FontSize(22)
                        .FontColor("#95a5a6");

                    // Umístění
                    column.Item().AlignCenter().PaddingTop(40)
                        .Border(2)
                        .BorderColor(GetPositionColor(position))
                        .Background(GetPositionBackgroundColor(position))
                        .Padding(20)
                        .Column(inner =>
                        {
                            inner.Item().Text("Umístění")
                                .FontSize(20)
                                .FontColor("#7f8c8d");
                            
                            inner.Item().PaddingTop(10).Text(GetPositionText(position))
                                .FontSize(48)
                                .Bold()
                                .FontColor(GetPositionColor(position));
                            
                            inner.Item().PaddingTop(10).Text($"{team.TotalScore} bodů")
                                .FontSize(28)
                                .FontColor("#95a5a6");
                        });
                });

                page.Footer().AlignCenter().Column(column =>
                {
                    column.Item().Text($"Centrum Robotiky Plzeň")
                        .FontSize(16)
                        .FontColor("#95a5a6");
                    
                    column.Item().PaddingTop(5).Text($"{DateTime.Now:dd. MMMM yyyy}")
                        .FontSize(14)
                        .FontColor("#bdc3c7");
                });
            });
        }).GeneratePdf());
    }

    private string GetPositionText(int position)
    {
        return position switch
        {
            1 => "🥇 1. místo",
            2 => "🥈 2. místo",
            3 => "🥉 3. místo",
            _ => $"{position}. místo"
        };
    }

    private string GetPositionColor(int position)
    {
        return position switch
        {
            1 => "#FFD700", // Zlatá
            2 => "#C0C0C0", // Stříbrná
            3 => "#CD7F32", // Bronzová
            _ => "#34495e"
        };
    }

    private string GetPositionBackgroundColor(int position)
    {
        return position switch
        {
            1 => "#FFF9E6",
            2 => "#F5F5F5",
            3 => "#FFF0E6",
            _ => "#ecf0f1"
        };
    }
}
