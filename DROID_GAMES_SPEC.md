# 🤖 DROID GAMES - Kompletní Technická Specifikace
## Management System pro Robotickou Soutěž

**Verze:** 1.0  
**Datum:** 2025-10-27  
**Organizace:** Centrum Robotiky Plzeň  
**Pro:** Droid Games 2026

---

## 📋 Obsah

1. [Přehled projektu](#přehled-projektu)
2. [Technický stack](#technický-stack)
3. [Architektura systému](#architektura-systému)
4. [Datové modely](#datové-modely)
5. [Role a oprávnění](#role-a-oprávnění)
6. [Komponenty a stránky](#komponenty-a-stránky)
7. [Interaktivní mapa](#interaktivní-mapa)
8. [Achievement systém](#achievement-systém)
9. [WebSocket komunikace](#websocket-komunikace)
10. [API specifikace](#api-specifikace)
11. [Implementační plán](#implementační-plán)
12. [Deployment](#deployment)

---

## 🎯 Přehled projektu

### Cíl
Vytvoření kompletního real-time managementu systému pro robotickou soutěž Droid Games, který:
- Automatizuje bodování a zápis výsledků
- Umožňuje real-time spolupráci rozhodčích
- Poskytuje gamifikaci a statistiky pro týmy
- Zlepšuje komunikaci mezi moderátorem a režií
- Generuje diplomy a analytické reporty

### Klíčové vlastnosti
✅ Real-time synchronizace přes WebSockets  
✅ Multi-role systém (6 rolí)  
✅ Interaktivní mapa pro rozhodčí  
✅ Achievement systém pro týmy  
✅ Heatmapy a statistiky  
✅ API pro ESP32 hardware  
✅ Export diplom do PDF  
✅ Bez databáze (JSON persistence)  

---

## 🛠️ Technický stack

### Backend
```yaml
Framework: ASP.NET Core 8.0
Language: C# 12
Web Framework: Blazor Server
Real-time: SignalR
PDF Generation: QuestPDF
JSON Storage: System.Text.Json
Authentication: ASP.NET Core Identity (simplified)
```

### Frontend
```yaml
UI Framework: Blazor Server Components
Styling: Tailwind CSS / Bootstrap 5
Charts: ApexCharts.NET
Icons: Font Awesome 6
Animations: Animate.css
Maps: Custom SVG + JavaScript
```

### Infrastructure
```yaml
Container: Docker
Orchestration: docker-compose
Reverse Proxy: nginx (volitelné)
File Storage: Volume mounts
```

### Hardware API
```yaml
Device: ESP32
Protocol: HTTP REST
Format: JSON
Authentication: API Key
```

---

## 🏗️ Architektura systému

### High-Level Overview

```
┌─────────────────────────────────────────────────────────┐
│                     NGINX (optional)                     │
│              SSL Termination / Load Balancer             │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              Blazor Server Application                   │
│  ┌─────────────────────────────────────────────────┐   │
│  │              SignalR Hub (WebSockets)            │   │
│  │  • ScoreboardHub                                 │   │
│  │  • TimerHub                                      │   │
│  │  • NotificationHub                               │   │
│  │  • ProductionHub                                 │   │
│  └─────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────┐   │
│  │              Services Layer                      │   │
│  │  • TeamService                                   │   │
│  │  • ScoreService                                  │   │
│  │  • MapService                                    │   │
│  │  • AchievementService                            │   │
│  │  • ReminderService                               │   │
│  │  • DiplomaService                                │   │
│  │  • HeatmapService                                │   │
│  │  • QuizService                                   │   │
│  └─────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────┐   │
│  │          JSON Storage Layer                      │   │
│  │  • FileBasedRepository<T>                        │   │
│  │  • AtomicWriter                                  │   │
│  │  • BackupService (každých 5 min)                │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                     │
                     │ REST API
                     ▼
┌─────────────────────────────────────────────────────────┐
│               ESP32 Hardware Timer                       │
│               (START/STOP/RESET buttons)                 │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                   Client Devices                         │
│  • Public Display (projektor)                            │
│  • Referee Tablets (3x)                                  │
│  • Head Referee Laptop                                   │
│  • Team Phones                                           │
│  • Production Display                                    │
└─────────────────────────────────────────────────────────┘
```

### Folder Structure

```
DroidGames/
├── DroidGames.Server/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Dockerfile
│   ├── Components/
│   │   ├── Pages/
│   │   │   ├── Public/
│   │   │   │   ├── Index.razor              # Public leaderboard
│   │   │   │   └── Maps.razor               # Public maps
│   │   │   ├── Team/
│   │   │   │   ├── Dashboard.razor          # Team dashboard
│   │   │   │   ├── Statistics.razor         # Detailed stats
│   │   │   │   ├── Achievements.razor       # Achievements page
│   │   │   │   └── Quiz.razor               # Mini quiz game
│   │   │   ├── Referee/
│   │   │   │   ├── Scoring.razor            # Interactive scoring
│   │   │   │   └── MapView.razor            # Map for reference
│   │   │   ├── HeadReferee/
│   │   │   │   ├── Control.razor            # Main control panel
│   │   │   │   ├── Approval.razor           # Score approval
│   │   │   │   ├── Timer.razor              # Timer control
│   │   │   │   ├── FunFacts.razor           # Fun facts manager
│   │   │   │   ├── Reminders.razor          # Reminder system
│   │   │   │   └── Production.razor         # Production comm panel
│   │   │   ├── Production/
│   │   │   │   └── Director.razor           # Production director view
│   │   │   ├── Admin/
│   │   │   │   ├── Teams.razor              # Team management
│   │   │   │   ├── Rounds.razor             # Round generation
│   │   │   │   ├── Maps.razor               # Map configuration
│   │   │   │   ├── Settings.razor           # System settings
│   │   │   │   └── Diplomas.razor           # Diploma generator
│   │   │   └── Auth/
│   │   │       ├── Login.razor
│   │   │       └── Logout.razor
│   │   └── Shared/
│   │       ├── MainLayout.razor
│   │       ├── NavMenu.razor
│   │       ├── InteractiveMap.razor         # Reusable map component
│   │       ├── Leaderboard.razor
│   │       ├── TeamCard.razor
│   │       ├── AchievementBadge.razor
│   │       └── NotificationToast.razor
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── ITeamService.cs
│   │   │   ├── IScoreService.cs
│   │   │   ├── IMapService.cs
│   │   │   ├── IAchievementService.cs
│   │   │   ├── IReminderService.cs
│   │   │   ├── IDiplomaService.cs
│   │   │   ├── IHeatmapService.cs
│   │   │   └── IQuizService.cs
│   │   └── Implementations/
│   │       ├── TeamService.cs
│   │       ├── ScoreService.cs
│   │       ├── MapService.cs
│   │       ├── AchievementService.cs
│   │       ├── ReminderService.cs
│   │       ├── DiplomaService.cs
│   │       ├── HeatmapService.cs
│   │       └── QuizService.cs
│   ├── Hubs/
│   │   ├── ScoreboardHub.cs
│   │   ├── TimerHub.cs
│   │   ├── NotificationHub.cs
│   │   └── ProductionHub.cs
│   ├── Controllers/
│   │   └── Api/
│   │       ├── TimerController.cs           # ESP32 API
│   │       └── HardwareAuthController.cs
│   ├── Models/
│   │   ├── Team.cs
│   │   ├── Round.cs
│   │   ├── Score.cs
│   │   ├── Map.cs
│   │   ├── MapAction.cs
│   │   ├── Achievement.cs
│   │   ├── Reminder.cs
│   │   ├── FunFact.cs
│   │   ├── QuizQuestion.cs
│   │   └── User.cs
│   ├── Data/
│   │   ├── IRepository.cs
│   │   ├── JsonRepository.cs
│   │   └── BackupService.cs
│   └── wwwroot/
│       ├── css/
│       │   └── app.css
│       ├── js/
│       │   ├── interactiveMap.js
│       │   └── notifications.js
│       └── img/
│           └── brand/
├── data/                                     # Mounted volume
│   ├── teams.json
│   ├── rounds.json
│   ├── scores.json
│   ├── maps.json
│   ├── achievements.json
│   ├── settings.json
│   ├── funfacts.json
│   ├── reminders.json
│   ├── quiz.json
│   └── backups/
│       └── [timestamped backups]
├── docker-compose.yml
└── README.md
```

---

## 📊 Datové modely

### 1. Team Model

```csharp
public class Team
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new(); // Max 3
    public string PinCode { get; set; } = string.Empty; // 4-digit PIN
    public string? RobotPhotoUrl { get; set; }
    public string? RobotDescription { get; set; }
    
    // Rounds participation
    public List<RoundParticipation> Rounds { get; set; } = new();
    
    // Achievements
    public List<string> UnlockedAchievements { get; set; } = new();
    
    // Statistics
    public int TotalScore { get; set; }
    public int CurrentPosition { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RoundParticipation
{
    public int RoundNumber { get; set; } // 1-5
    public string OpponentTeamId { get; set; } = string.Empty;
    public string MapConfigurationId { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Duration { get; set; } // seconds
    
    // Scoring (approved after 3 referees agree)
    public Dictionary<string, RefereeScore> RefereeScores { get; set; } = new();
    public bool IsApproved { get; set; }
    public int? FinalScore { get; set; }
    
    // Actions recorded during the round
    public List<MapAction> Actions { get; set; } = new();
}

public class RefereeScore
{
    public string RefereeId { get; set; } = string.Empty;
    public Dictionary<string, int> ScoreBreakdown { get; set; } = new();
    // Example:
    // "crystal_touch": 5,
    // "crystal_move": 3,
    // "carefulness_bonus": 2,
    // "sulfur_disruption": -1,
    // etc.
    
    public int TotalScore { get; set; }
    public DateTime SubmittedAt { get; set; }
}
```

### 2. Map Models

```csharp
public class MapConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty; // "Konfigurace A"
    public int RoundNumber { get; set; } // 1-5
    public bool IsPublished { get; set; } // Visible to teams
    
    // Grid layout (4.5 x 6 blocks per side)
    public List<MapBlock> LeftSide { get; set; } = new();
    public List<MapBlock> RightSide { get; set; } = new();
    public List<MapBlock> CenterLine { get; set; } = new(); // Shared
    
    // Statistics
    public double AverageScore { get; set; }
    public int TimesPlayed { get; set; }
}

public class MapBlock
{
    public int X { get; set; } // Grid position
    public int Y { get; set; }
    public MapBlockType Type { get; set; }
    
    // For heatmap data
    public int TouchCount { get; set; }
    public int MoveCount { get; set; }
    public int DisruptionCount { get; set; }
}

public enum MapBlockType
{
    Empty,
    Rock,           // Gray, immovable
    BlueCrystal,    // Blue, movable, goal
    YellowSulfur    // Yellow, movable, penalty
}

public class MapAction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; }
    public int ElapsedSeconds { get; set; } // From start of round
    
    public MapActionType ActionType { get; set; }
    public int BlockX { get; set; }
    public int BlockY { get; set; }
    public int? TargetX { get; set; } // For moves
    public int? TargetY { get; set; }
    
    public string RefereeId { get; set; } = string.Empty;
}

public enum MapActionType
{
    CrystalTouch,           // +1 point
    CrystalValidMove,       // +1 point (140-280mm)
    CrystalExcessiveMove,   // -2 points (>280mm)
    SulfurDisruption,       // -1 point (>140mm)
    OpponentCompensation    // +2 points
}
```

### 3. Achievement Model

```csharp
public class Achievement
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty; // Emoji or icon class
    public AchievementRarity Rarity { get; set; }
    public bool IsHidden { get; set; } // Visible only after unlock
    
    // Unlock condition (evaluated by AchievementService)
    public AchievementCondition Condition { get; set; } = new();
}

public enum AchievementRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public class AchievementCondition
{
    public AchievementConditionType Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public enum AchievementConditionType
{
    FirstPoints,              // first_blood
    TouchAllCrystals,         // crystal_hunter
    NoSulfurDisruption,       // gentle_giant
    PerfectRound,             // perfect_round
    WinAllRounds,             // unbeatable
    CooperativeBonus,         // cooperative_master
    ConsistentScores,         // consistency
    PositionImprovement,      // comeback_king
    FastCompletion,           // speed_demon
    ExactScore,               // lucky_seven
    MinimalMoves,             // minimalist
    Custom                    // For complex logic
}
```

### 4. Quiz Model

```csharp
public class QuizQuestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new(); // 4 options
    public int CorrectAnswerIndex { get; set; } // 0-3
    public string Explanation { get; set; } = string.Empty;
    public QuizDifficulty Difficulty { get; set; }
    public List<string> Tags { get; set; } = new(); // "rules", "physics", etc.
}

public enum QuizDifficulty
{
    Easy,
    Medium,
    Hard
}

public class QuizSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public List<QuizAnswer> Answers { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class QuizAnswer
{
    public string QuestionId { get; set; } = string.Empty;
    public int SelectedAnswerIndex { get; set; }
    public bool IsCorrect { get; set; }
    public int TimeToAnswerMs { get; set; }
}
```

### 5. Reminder Model

```csharp
public class Reminder
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = string.Empty;
    public int IntervalMinutes { get; set; } // How often to remind
    public DateTime? LastTriggered { get; set; }
    public DateTime? NextDue { get; set; }
    public bool IsActive { get; set; } = true;
    public ReminderPriority Priority { get; set; }
}

public enum ReminderPriority
{
    Low,
    Medium,
    High,
    Critical
}
```

### 6. Fun Fact Model

```csharp
public class FunFact
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "physics", "history", etc.
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
}
```

### 7. Settings Model

```csharp
public class CompetitionSettings
{
    public int CurrentRound { get; set; } = 1;
    public CompetitionStatus Status { get; set; } = CompetitionStatus.NotStarted;
    public int RoundDurationSeconds { get; set; } = 90;
    public int PreparationDurationSeconds { get; set; } = 60;
    public int TotalRounds { get; set; } = 5;
    public int MaxTeams { get; set; } = 16;
    public int MaxTeamsPerSchool { get; set; } = 3;
    
    // Timer state
    public DateTime? TimerStartedAt { get; set; }
    public int TimerRemainingSeconds { get; set; } = 90;
    public TimerStatus TimerStatus { get; set; } = TimerStatus.Stopped;
    
    // Current match
    public string? CurrentTeamAId { get; set; }
    public string? CurrentTeamBId { get; set; }
}

public enum CompetitionStatus
{
    NotStarted,
    InProgress,
    Paused,
    Finished
}

public enum TimerStatus
{
    Stopped,
    Running,
    Paused
}
```

### 8. User Model (Simplified Auth)

```csharp
public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    
    // For teams
    public string? TeamId { get; set; }
}

public enum UserRole
{
    Public,
    Team,
    Referee,
    HeadReferee,
    Production,
    Admin
}
```

---

## 🔐 Role a oprávnění

### Permission Matrix

| Feature | Public | Team | Referee | HeadRef | Production | Admin |
|---------|--------|------|---------|---------|------------|-------|
| View leaderboard | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| View published maps | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Team dashboard | ❌ | ✅ | ❌ | ❌ | ❌ | ✅ |
| View own statistics | ❌ | ✅ | ❌ | ❌ | ❌ | ✅ |
| Play quiz | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ |
| Submit scores | ❌ | ❌ | ✅ | ✅ | ❌ | ✅ |
| View all maps | ❌ | ❌ | ✅ | ✅ | ❌ | ✅ |
| Approve scores | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |
| Control timer | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |
| Manage fun facts | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |
| Manage reminders | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |
| Production comm | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| Receive notifications | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Manage teams | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Generate rounds | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Configure maps | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Export diplomas | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

---

## 🎨 Komponenty a stránky

### PUBLIC Pages

#### 1. Index.razor (Live Leaderboard)

**URL:** `/`

**Features:**
- Real-time updated leaderboard
- Animated position changes
- Current round indicator
- Team avatars/logos
- Confetti for top 3

**Layout:**
```
┌─────────────────────────────────────────┐
│  🏆 DROID GAMES 2026 - LIVE VÝSLEDKY   │
│  Kolo 3 z 5                             │
├─────────────────────────────────────────┤
│  1. 🥇 MegaBots (↑2)     89 bodů 🔥🔥  │
│  2. 🥈 RoboWarriors (↓1) 87 bodů ⚡⚡  │
│  3. 🥉 CodeMasters (↑3)  84 bodů 🚀🚀  │
│  4.    TechKids (↓2)     78 bodů ⭐    │
│  ...                                    │
│                                         │
│  📊 Statistiky:                         │
│  • Průměr: 72 bodů                     │
│  • Nejlepší kolo: #3 (prům. 15.2)     │
│                                         │
│  ⏱️ Aktuální zápas:                     │
│  Robotici vs CyberKids                  │
│  ⏱️ 00:45                               │
└─────────────────────────────────────────┘
```

**SignalR Subscriptions:**
- `ScoreboardHub.OnLeaderboardUpdated`
- `TimerHub.OnTimerTick`

#### 2. Maps.razor (Public Maps)

**URL:** `/maps`

**Features:**
- Grid view of all published maps
- Basic statistics per map
- No interactive elements

---

### TEAM Pages

#### 1. Team/Dashboard.razor

**URL:** `/team/dashboard`

**Authentication:** Required (PIN code)

**Features:**
- Current position and total score
- Progress bar to top 3
- Next round notification
- Achievement badges (visible ones)
- Quick stats

**SignalR Subscriptions:**
- `NotificationHub.OnTeamNotification` (you're next!)
- `ScoreboardHub.OnLeaderboardUpdated`

#### 2. Team/Statistics.razor

**URL:** `/team/statistics`

**Features:**
- Detailed breakdown by round
- Comparison with average
- Comparison with winner
- Static replay (list of actions)
- Strengths & weaknesses analysis

#### 3. Team/Achievements.razor

**URL:** `/team/achievements`

**Features:**
- Grid of unlocked achievements
- Progress bars for in-progress ones
- Rarity colors
- "???" for hidden/locked achievements

#### 4. Team/Quiz.razor

**URL:** `/team/quiz`

**Features:**
- Multiple choice questions about rules
- Timer (15 seconds per question)
- Immediate feedback
- Leaderboard of quiz scores

---

### REFEREE Pages

#### 1. Referee/Scoring.razor

**URL:** `/referee/scoring`

**Authentication:** Required

**Features:**
- Displays current teams
- Interactive map with blocks
- Quick action buttons
- Running log of recorded actions
- Auto-calculated score
- Submit button

**Layout:**
```
┌─────────────────────────────────────────┐
│  📝 Hodnocení - Kolo 3                  │
│  Robotici (levá strana)                 │
│  ⏱️ Zbývá: 00:45                        │
├─────────────────────────────────────────┤
│  [Interactive Map Component]            │
│  (User clicks on blocks)                │
│                                         │
│  Režim: [ 👆 Dotyk krystalu ]          │
│         [ ➡️ Posun objektu ]            │
│         [ ⚠️ Narušení síry ]            │
│         [ 💥 Přílišný posun ]           │
│                                         │
│  ─────────────────────────────────────  │
│  ✅ Záznam akcí:                        │
│  1. ✓ Dotyk - Krystal B2 (0:12)        │
│  2. ✓ Posun - B2→B3 (+1 bod)           │
│  3. ⚠️ Narušení - Síra C2 (-1 bod)     │
│                                         │
│  📊 Součet bodů: 3                      │
│                                         │
│  [Odeslat hodnocení]                    │
└─────────────────────────────────────────┘
```

**SignalR:**
- `ScoreboardHub.SendRefereeScore` (real-time to HeadRef)

---

### HEAD REFEREE Pages

#### 1. HeadReferee/Control.razor

**URL:** `/headref/control`

**Features:**
- Main control center
- Quick access to all sub-pages
- Current status overview
- Timer control (if not on separate page)

#### 2. HeadReferee/Approval.razor

**URL:** `/headref/approval`

**Features:**
- 3-column view of referee scores
- Side-by-side comparison
- Highlight differences
- Approve button (enabled only when all 3 match)
- "Next Team" button

**Layout:**
```
┌──────────────────────────────────────────────────┐
│  ✅ Schválení hodnocení - Kolo 3                │
│  Robotici vs CyberKids                          │
├──────────────────────────────────────────────────┤
│  Rozhodčí 1  │ Rozhodčí 2  │ Rozhodčí 3  │ Akce │
│──────────────┼──────────────┼──────────────┼──────│
│  Dotyky:   5 │ Dotyky:   5 │ Dotyky:   5 │      │
│  Posuny:   3 │ Posuny:   3 │ Posuny:   3 │      │
│  Narušení:-1 │ Narušení:-1 │ Narušení:-1 │      │
│  ──────────  │  ──────────  │  ──────────  │      │
│  Celkem:   7 │ Celkem:   7 │ Celkem:   7 │ ✅   │
│              │              │              │      │
│  🟢 SHODA    │ 🟢 SHODA    │ 🟢 SHODA    │      │
│                                                   │
│  [✅ Schválit hodnocení]  [➡️ Další tým]        │
└──────────────────────────────────────────────────┘

// If disagreement:
│  Rozhodčí 1  │ Rozhodčí 2  │ Rozhodčí 3  │      │
│──────────────┼──────────────┼──────────────┼──────│
│  Celkem:   7 │ Celkem:   8 │ Celkem:   7 │ 🔴   │
│              │              │              │      │
│  🟢          │ 🔴 NESOUHLASÍ│ 🟢          │      │
│                                                   │
│  ⚠️ Rozhodčí 2, prosím přepočítej               │
│  [❌ Nemohu schválit]                            │
```

**SignalR:**
- Receives from `ScoreboardHub` (all 3 referee inputs)
- Sends approval to `ScoreboardHub`

#### 3. HeadReferee/Timer.razor

**URL:** `/headref/timer`

**Features:**
- Large countdown display
- START / STOP / RESET buttons
- Current teams displayed
- Next teams preview

**Layout:**
```
┌─────────────────────────────────────┐
│  ⏱️ ČASOMÍRA                        │
├─────────────────────────────────────┤
│                                     │
│  Aktuální:                          │
│  🤖 Robotici vs CyberKids          │
│                                     │
│       ╔═══════════════╗            │
│       ║   01:23       ║            │
│       ╚═══════════════╝            │
│                                     │
│  [ ▶️ START ]  [ ⏸️ STOP ]         │
│  [ 🔄 RESET ]                       │
│                                     │
│  ─────────────────────────────────  │
│  Připraví se:                       │
│  🤖 MegaBots vs TechKids           │
└─────────────────────────────────────┘
```

**SignalR:**
- `TimerHub.StartTimer`
- `TimerHub.StopTimer`
- `TimerHub.ResetTimer`
- Broadcasts to all clients

#### 4. HeadReferee/FunFacts.razor

**URL:** `/headref/funfacts`

**Features:**
- Card grid of all fun facts
- Mark as "used" with one click
- Filter by category
- Reset all button

**Layout:**
```
┌─────────────────────────────────────┐
│  🎉 FUN FAKTY - Moderátorská pomoc │
│  [🔄 Reset všechny] [🔍 Filter]    │
├─────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐│
│  │ 💡 Fyzika    │  │ 🤖 Historie  ││
│  │ Rychlost...  │  │ Slovo robot..││
│  │              │  │              ││
│  │ [✅ Použito] │  │ [⚪ Použít]  ││
│  └──────────────┘  └──────────────┘│
│  ┌──────────────┐  ┌──────────────┐│
│  │ ...          │  │ ...          ││
└─────────────────────────────────────┘
```

#### 5. HeadReferee/Reminders.razor

**URL:** `/headref/reminders`

**Features:**
- List of configured reminders
- Shows countdown to next trigger
- "Acknowledge" button (resets timer)
- Toast notification when due

**Layout:**
```
┌─────────────────────────────────────┐
│  ⏰ PŘIPOMÍNKY                      │
├─────────────────────────────────────┤
│  📢 Jmenuj sponzory                │
│     Každých: 5 minut               │
│     Další za: ⏱️ 02:34             │
│     [✅ Hotovo]                     │
│  ─────────────────────────────────  │
│  📣 Zmiň Centrum Robotiky          │
│     Každých: 10 minut              │
│     Další za: ⏱️ 07:12             │
│     [✅ Hotovo]                     │
│  ─────────────────────────────────  │
│  [+ Přidat připomínku]             │
└─────────────────────────────────────┘
```

#### 6. HeadReferee/Production.razor

**URL:** `/headref/production`

**Features:**
- Grid of camera angles (tiles)
- Grid of infographic overlays
- Click to send notification to Production
- Status indicator (delivered/acknowledged)

**Layout:**
```
┌─────────────────────────────────────┐
│  📹 KOMUNIKACE S REŽIÍ              │
├─────────────────────────────────────┤
│  Kamery:                            │
│  ┌────┐ ┌────┐ ┌────┐ ┌────┐      │
│  │ 📷1│ │ 📷2│ │ 📷3│ │ 📷4│      │
│  │Dráha│ │Publika│ │Detail│ │Wide│ │
│  └────┘ └────┘ └────┘ └────┘      │
│                                     │
│  Infografiky:                       │
│  ┌────┐ ┌────┐ ┌────┐             │
│  │📊  │ │🏆  │ │🗺️  │             │
│  │Body│ │Pořadí││Mapa│             │
│  └────┘ └────┘ └────┘             │
└─────────────────────────────────────┘
```

**SignalR:**
- `ProductionHub.RequestCamera(cameraId)`
- `ProductionHub.RequestGraphic(graphicId)`

---

### PRODUCTION Page

#### 1. Production/Director.razor

**URL:** `/production/director`

**Features:**
- Same grid layout as HeadRef
- Tiles blink/pulse when HeadRef requests
- Acknowledge button

**SignalR:**
- Receives from `ProductionHub`
- Sends acknowledgment

---

### ADMIN Pages

#### 1. Admin/Teams.razor

**URL:** `/admin/teams`

**Features:**
- Import teams from JSON
- Manual add/edit/delete
- Generate PIN codes
- Assign to rounds

#### 2. Admin/Rounds.razor

**URL:** `/admin/rounds`

**Features:**
- Generate random pairings for next round
- View schedule
- Assign map configurations
- Set timing (estimated start times)

#### 3. Admin/Maps.razor

**URL:** `/admin/maps`

**Features:**
- Create/edit map configurations
- Visual editor (drag & drop blocks)
- Publish/unpublish toggle
- Preview heatmaps

#### 4. Admin/Settings.razor

**URL:** `/admin/settings`

**Features:**
- Competition-wide settings
- Timer duration
- Number of rounds
- Hardware API key

#### 5. Admin/Diplomas.razor

**URL:** `/admin/diplomas`

**Features:**
- HTML template editor
- Data preview
- "Generate All" button
- Download ZIP

---

## 🗺️ Interaktivní mapa

### Component: InteractiveMap.razor

**Props:**
```csharp
@code {
    [Parameter] public MapConfiguration Map { get; set; }
    [Parameter] public bool IsInteractive { get; set; } = false;
    [Parameter] public EventCallback<MapAction> OnActionRecorded { get; set; }
    [Parameter] public MapActionType? CurrentActionMode { get; set; }
    [Parameter] public List<MapAction>? ExistingActions { get; set; }
    [Parameter] public bool ShowHeatmap { get; set; } = false;
}
```

### Visual Representation

**Grid Layout:**
- SVG canvas 1200x800px
- Each block = 80x80px square
- Left side: 9 blocks (4.5x2 rows)
- Center: 2 blocks (shared)
- Right side: 9 blocks (4.5x2 rows)

**Color Coding:**
- 🟦 Blue Crystal: `#3B82F6`
- 🟨 Yellow Sulfur: `#FBBF24`
- ⬛ Rock: `#6B7280`
- ⬜ Empty: `#F3F4F6`

**Interactions (when IsInteractive=true):**
1. User selects action mode via buttons
2. Cursor changes (pointer, crosshair, etc.)
3. User clicks on block
4. If action is "Move":
   - First click: source block (highlights)
   - Second click: target block (draws arrow)
5. Action recorded, sent via callback

**Heatmap Mode:**
- Overlay semi-transparent gradient
- Red (hot) = most touches/moves
- Blue (cold) = no interaction
- Uses Existing Actions data

### Implementation Details

**interactiveMap.js:**
```javascript
window.interactiveMap = {
    initialize: function(svgElementId, blocks, isInteractive) {
        // Draw SVG blocks
        // Attach click handlers if interactive
    },
    
    setActionMode: function(mode) {
        // Change cursor, internal state
    },
    
    recordAction: function(blockX, blockY, targetX, targetY) {
        // Call back to Blazor via DotNetObjectReference
    },
    
    highlightBlock: function(blockX, blockY, color) {
        // Visual feedback
    },
    
    drawHeatmap: function(heatmapData) {
        // Overlay gradient based on counts
    }
};
```

---

## 🏆 Achievement systém

### Service: AchievementService.cs

**Methods:**
```csharp
public interface IAchievementService
{
    Task<List<Achievement>> GetAllAchievements();
    Task<List<Achievement>> GetUnlockedForTeam(string teamId);
    Task CheckAndUnlockAchievements(string teamId);
    Task<Achievement> GetAchievementById(string achievementId);
}
```

### Unlock Logic

**Triggered after:**
- Round score approved
- Real-time during scoring (for some achievements)

**Conditions Evaluation:**

```csharp
public async Task CheckAndUnlockAchievements(string teamId)
{
    var team = await _teamService.GetTeamById(teamId);
    var allAchievements = await GetAllAchievements();
    
    foreach (var achievement in allAchievements)
    {
        if (team.UnlockedAchievements.Contains(achievement.Id))
            continue; // Already unlocked
        
        bool shouldUnlock = achievement.Condition.Type switch
        {
            AchievementConditionType.FirstPoints => 
                team.Rounds.Any(r => r.FinalScore > 0),
            
            AchievementConditionType.TouchAllCrystals => 
                EvaluateTouchAllCrystals(team),
            
            AchievementConditionType.NoSulfurDisruption =>
                EvaluateNoSulfur(team),
            
            AchievementConditionType.PerfectRound =>
                EvaluatePerfectRound(team),
            
            AchievementConditionType.WinAllRounds =>
                team.Rounds.Count == 5 && 
                team.Rounds.All(r => IsWinningScore(r)),
            
            // ... more conditions
            
            _ => false
        };
        
        if (shouldUnlock)
        {
            team.UnlockedAchievements.Add(achievement.Id);
            await _teamService.UpdateTeam(team);
            
            // Send notification
            await _notificationHub.Clients
                .Group($"team_{teamId}")
                .SendAsync("OnAchievementUnlocked", achievement);
        }
    }
}
```

### Notification

When unlocked:
- Toast notification in app
- Badge animation (confetti)
- Sound effect (optional)

---

## 🔌 WebSocket komunikace

### Hubs

#### 1. ScoreboardHub.cs

**Methods:**
```csharp
public class ScoreboardHub : Hub
{
    // Referee sends score
    public async Task SubmitRefereeScore(
        string teamId, 
        int roundNumber, 
        RefereeScore score)
    {
        // Save to database
        // Notify HeadReferee
        await Clients
            .Group("headreferee")
            .SendAsync("OnRefereeScoreReceived", teamId, score);
    }
    
    // HeadReferee approves
    public async Task ApproveScore(
        string teamId, 
        int roundNumber, 
        int finalScore)
    {
        // Update team
        // Recalculate leaderboard
        // Broadcast to all
        await Clients.All.SendAsync("OnLeaderboardUpdated", leaderboard);
        
        // Check achievements
        await _achievementService.CheckAndUnlockAchievements(teamId);
    }
    
    // Public subscription
    public async Task SubscribeToLeaderboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "public");
    }
}
```

#### 2. TimerHub.cs

**Methods:**
```csharp
public class TimerHub : Hub
{
    private readonly ITimerService _timerService;
    
    public async Task StartTimer()
    {
        await _timerService.Start();
        await BroadcastTimerUpdate();
    }
    
    public async Task StopTimer()
    {
        await _timerService.Stop();
        await BroadcastTimerUpdate();
    }
    
    public async Task ResetTimer()
    {
        await _timerService.Reset();
        await BroadcastTimerUpdate();
    }
    
    private async Task BroadcastTimerUpdate()
    {
        var state = await _timerService.GetState();
        await Clients.All.SendAsync("OnTimerUpdate", state);
    }
    
    // Background service ticks every second
    public async Task OnTimerTick(int remainingSeconds)
    {
        await Clients.All.SendAsync("OnTimerTick", remainingSeconds);
    }
}
```

#### 3. NotificationHub.cs

**Methods:**
```csharp
public class NotificationHub : Hub
{
    public async Task SubscribeTeam(string teamId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"team_{teamId}");
    }
    
    // Sent when team is 2 positions away
    public async Task SendTeamPreparationNotice(string teamId, int minutesUntil)
    {
        await Clients
            .Group($"team_{teamId}")
            .SendAsync("OnPreparationNotice", minutesUntil);
    }
    
    // Achievement unlocked
    public async Task SendAchievementNotification(string teamId, Achievement achievement)
    {
        await Clients
            .Group($"team_{teamId}")
            .SendAsync("OnAchievementUnlocked", achievement);
    }
}
```

#### 4. ProductionHub.cs

**Methods:**
```csharp
public class ProductionHub : Hub
{
    // HeadRef requests camera/graphic
    public async Task RequestCamera(int cameraId)
    {
        await Clients
            .Group("production")
            .SendAsync("OnCameraRequested", cameraId);
    }
    
    public async Task RequestGraphic(string graphicId)
    {
        await Clients
            .Group("production")
            .SendAsync("OnGraphicRequested", graphicId);
    }
    
    // Production acknowledges
    public async Task AcknowledgeRequest(string requestType, string requestId)
    {
        await Clients
            .Group("headreferee")
            .SendAsync("OnRequestAcknowledged", requestType, requestId);
    }
}
```

---

## 🔗 API specifikace

### REST API pro ESP32

**Base URL:** `https://your-server.com/api/hardware`

**Authentication:** API Key in header
```
X-Api-Key: your-secret-key-here
```

#### Endpoints

**1. Start Timer**
```http
POST /api/hardware/timer/start
Content-Type: application/json
X-Api-Key: {API_KEY}

Response 200:
{
  "success": true,
  "remainingSeconds": 90,
  "startedAt": "2026-02-15T10:30:00Z"
}
```

**2. Stop Timer**
```http
POST /api/hardware/timer/stop
X-Api-Key: {API_KEY}

Response 200:
{
  "success": true,
  "stoppedAt": "2026-02-15T10:31:23Z",
  "elapsedSeconds": 83
}
```

**3. Reset Timer**
```http
POST /api/hardware/timer/reset
X-Api-Key: {API_KEY}

Response 200:
{
  "success": true,
  "remainingSeconds": 90
}
```

**4. Get Timer Status**
```http
GET /api/hardware/timer/status
X-Api-Key: {API_KEY}

Response 200:
{
  "status": "running", // "stopped", "running", "paused"
  "remainingSeconds": 67,
  "startedAt": "2026-02-15T10:30:00Z"
}
```

### ESP32 Example Code

```cpp
#include <WiFi.h>
#include <HTTPClient.h>

const char* ssid = "YOUR_WIFI";
const char* password = "YOUR_PASSWORD";
const char* apiUrl = "https://your-server.com/api/hardware/timer";
const char* apiKey = "your-secret-key";

void setup() {
  Serial.begin(115200);
  WiFi.begin(ssid, password);
  
  pinMode(START_BUTTON_PIN, INPUT_PULLUP);
  pinMode(STOP_BUTTON_PIN, INPUT_PULLUP);
  pinMode(RESET_BUTTON_PIN, INPUT_PULLUP);
}

void loop() {
  if (digitalRead(START_BUTTON_PIN) == LOW) {
    sendTimerCommand("start");
    delay(500); // debounce
  }
  
  if (digitalRead(STOP_BUTTON_PIN) == LOW) {
    sendTimerCommand("stop");
    delay(500);
  }
  
  if (digitalRead(RESET_BUTTON_PIN) == LOW) {
    sendTimerCommand("reset");
    delay(500);
  }
}

void sendTimerCommand(String command) {
  HTTPClient http;
  String url = String(apiUrl) + "/" + command;
  
  http.begin(url);
  http.addHeader("X-Api-Key", apiKey);
  http.addHeader("Content-Type", "application/json");
  
  int httpCode = http.POST("");
  
  if (httpCode == 200) {
    Serial.println("Command sent: " + command);
  } else {
    Serial.println("Error: " + String(httpCode));
  }
  
  http.end();
}
```

---

## 📋 Implementační plán

### Fáze 1: Základní infrastruktura (1 týden)

**Week 1:**
- ✅ Setup projektu (Blazor Server)
- ✅ Docker configuration
- ✅ JSON repository pattern
- ✅ Basic models (Team, Round, Score)
- ✅ Authentication system (simplified)
- ✅ Role-based authorization
- ✅ SignalR hubs (basic setup)

**Deliverables:**
- Funkční login
- Role switching
- Empty pages for each role

---

### Fáze 2: Bodovací systém (2 týdny)

**Week 2:**
- ✅ ScoreService implementation
- ✅ Referee scoring page (without map)
- ✅ HeadReferee approval page
- ✅ Real-time score submission via SignalR
- ✅ Score validation logic

**Week 3:**
- ✅ Interaktivní mapa - backend
- ✅ Interaktivní mapa - frontend (SVG)
- ✅ MapAction recording
- ✅ Integration s Referee scoring page
- ✅ Testing scoring workflow

**Deliverables:**
- 3 rozhodčí mohou nezávisle hodnotit
- Hlavní rozhodčí schvaluje jen když se shodují
- Data se ukládají do JSON

---

### Fáze 3: Public & Team Features (2 týdny)

**Week 4:**
- ✅ Public leaderboard s real-time updates
- ✅ Team dashboard
- ✅ Team statistics page
- ✅ Notifikační systém (základní)

**Week 5:**
- ✅ Achievement systém - backend
- ✅ Achievement unlock logic
- ✅ Achievement display v Team UI
- ✅ Toast notifications

**Deliverables:**
- Týmy vidí live výsledky
- Týmy vidí své statistiky
- Achievementy se odemykají automaticky

---

### Fáze 4: Timer & Admin (1.5 týdne)

**Week 6:**
- ✅ Timer service (background worker)
- ✅ Timer UI (HeadReferee)
- ✅ Timer API pro ESP32
- ✅ Admin: Team management
- ✅ Admin: Round generation

**Week 6.5:**
- ✅ Admin: Map configuration
- ✅ Admin: Settings page
- ✅ Map publishing toggle

**Deliverables:**
- Timer funguje a broadcastuje
- ESP32 může ovládat timer
- Admin může spravovat týmy a kola

---

### Fáze 5: Moderátor & Režie (1 týden)

**Week 7:**
- ✅ Fun Facts manager
- ✅ Reminder system
- ✅ Production communication panel
- ✅ ProductionHub implementation
- ✅ Camera/graphic request notifications

**Deliverables:**
- Moderátor má pomocníky (fun facts, reminders)
- Komunikace s režií funguje

---

### Fáze 6: Gamifikace & Analytika (1.5 týdne)

**Week 8:**
- ✅ Heatmap service
- ✅ Heatmap visualization
- ✅ Quiz system (questions, sessions)
- ✅ Quiz UI (Team page)

**Week 8.5:**
- ✅ Leaderboard animations
- ✅ Confetti effects
- ✅ Stats graphs (ApexCharts)

**Deliverables:**
- Heatmapy zobrazují data
- Kvíz je hratelný
- UI je plné animací

---

### Fáze 7: Export & Polish (1 týden)

**Week 9:**
- ✅ Diploma PDF generation (QuestPDF)
- ✅ Diploma template design
- ✅ Bulk export (ZIP)
- ✅ Overall UI/UX polish
- ✅ Mobile responsiveness

**Deliverables:**
- Diplomy se generují automaticky
- Celá aplikace vypadá profesionálně

---

### Fáze 8: Testing & Deployment (1 týden)

**Week 10:**
- ✅ Integration testing
- ✅ Load testing (SignalR connections)
- ✅ Bug fixes
- ✅ Docker deployment testing
- ✅ Documentation
- ✅ Training materials for organizers

**Deliverables:**
- Stabilní aplikace
- Deployment guide
- User manual

---

## 🚀 Deployment

### Docker Setup

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  droidgames:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:80"
      - "5001:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      - DataPath=/app/data
      - HardwareApiKey=${HARDWARE_API_KEY}
    volumes:
      - ./data:/app/data
      - ./backups:/app/backups
    restart: unless-stopped
    networks:
      - droidgames-network

networks:
  droidgames-network:
    driver: bridge
```

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DroidGames.Server/DroidGames.Server.csproj", "DroidGames.Server/"]
RUN dotnet restore "DroidGames.Server/DroidGames.Server.csproj"
COPY . .
WORKDIR "/src/DroidGames.Server"
RUN dotnet build "DroidGames.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DroidGames.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create data directory
RUN mkdir -p /app/data /app/backups

ENTRYPOINT ["dotnet", "DroidGames.Server.dll"]
```

### Environment Variables

**.env file:**
```bash
HARDWARE_API_KEY=your-secure-api-key-here-change-me
ADMIN_PASSWORD=admin123  # Change in production!
JWT_SECRET=your-jwt-secret-key
```

### Startup Commands

```bash
# Build
docker-compose build

# Start
docker-compose up -d

# Logs
docker-compose logs -f droidgames

# Stop
docker-compose down

# Backup data
docker exec droidgames tar czf /app/backups/backup-$(date +%Y%m%d-%H%M%S).tar.gz /app/data
```

---

## 📊 Performance Considerations

### Expected Load
- **Users:** ~50 concurrent (16 teams × 3 members + organizers + public)
- **SignalR Connections:** ~50
- **Peak Load:** During score updates, all clients refresh

### Optimizations
1. **SignalR Groups:** Reduce broadcast overhead
2. **JSON Caching:** Cache frequently read files in memory
3. **Debouncing:** Throttle rapid updates (e.g., timer ticks)
4. **Lazy Loading:** Load heavy data (heatmaps) on demand

---

## 🔒 Security Considerations

### Authentication
- Simple password-based (no email verification needed)
- PIN codes for teams (4 digits)
- API key for hardware (in environment variable)

### Authorization
- Role-based middleware
- SignalR group restrictions
- API endpoint authorization attributes

### Data Protection
- No PII stored beyond competition duration
- GDPR compliance (participants informed)
- Backups encrypted (optional)

---

## 🧪 Testing Strategy

### Unit Tests
- Services (scoring logic, achievement conditions)
- Repositories (JSON read/write)
- Utilities (heatmap calculations)

### Integration Tests
- SignalR hubs (mock clients)
- API endpoints (ESP32 simulation)
- End-to-end scoring workflow

### Manual Testing
- Referee workflow (3 devices)
- Team notifications
- Production communication
- Timer synchronization

---

## 📝 Development Notes

### Key Technologies
```csharp
// Package References
<PackageReference Include="QuestPDF" Version="2024.7.3" />
<PackageReference Include="ApexCharts.Blazor" Version="3.4.0" />
<PackageReference Include="Blazored.Toast" Version="4.2.1" />
```

### Code Style
- Use C# 12 features (primary constructors, collection expressions)
- Async/await everywhere
- Dependency Injection for all services
- Repository pattern for data access

### Conventions
- Files: PascalCase.cs
- Methods: PascalCase
- Parameters: camelCase
- Private fields: _camelCase

---

## 🎉 Go Live Checklist

**1 Week Before:**
- [ ] Deploy to staging
- [ ] Test all workflows
- [ ] Prepare training for organizers
- [ ] Generate sample data

**1 Day Before:**
- [ ] Import real teams
- [ ] Generate round 1 pairings
- [ ] Reveal map 1 (60 min before)
- [ ] Test ESP32 connection
- [ ] Backup clean state

**Day Of:**
- [ ] Monitor logs
- [ ] Backup every hour
- [ ] Have rollback plan ready

---

## 📞 Support

**Issues:**
- GitHub Issues
- Email: support@centrumrobotiky.eu

**Documentation:**
- User Manual: `/docs/USER_MANUAL.md`
- API Docs: `/docs/API.md`
- Deployment Guide: `/docs/DEPLOYMENT.md`

---

**End of Specification**

**Total Estimated Development Time:** 10-12 weeks  
**Team Size:** 1-2 developers  
**Ready for Droid Games 2026!** 🚀🤖
