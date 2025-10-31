# WebSocket / SignalR Implementace

## Přehled

Aplikace používá SignalR (WebSocket) pro real-time komunikaci mezi serverem a klienty.

## Huby (Hubs)

### 1. CompetitionHub (`/hubs/competition`)

Nový hub pro události soutěže.

**Server → Client události:**

| Událost | Parametry | Popis |
|---------|-----------|-------|
| `OnRoundChanged` | `int newRound` | Změna aktuálního kola |
| `OnCompetitionStatusChanged` | `string newStatus` | Změna stavu soutěže (NotStarted, InProgress, Paused, Finished) |
| `OnCurrentTeamsChanged` | `string? teamAId, string? teamBId` | Změna aktuálních týmů (Team A, Team B) |
| `OnNextTeamsChanged` | `string? teamAId, string? teamBId` | Změna dalších týmů |
| `OnRoundOrderChanged` | `int roundNumber` | Změna pořadí týmů v kole |
| `OnLeaderboardUpdated` | - | Aktualizace žebříčku |
| `OnYourTurn` | `string teamName, int position` | Notifikace pro tým že je na řadě |

**Client → Server metody:**

- `SubscribeAsPublic()` - Přihlásit se jako veřejný klient
- `SubscribeToTeam(string teamId)` - Přihlásit se k notifikacím týmu
- `UnsubscribeFromTeam(string teamId)` - Odhlásit se od notifikací týmu

### 2. ScoreboardHub (`/scoreboardHub`)

Hub pro žebříček a bodování.

### 3. TimerHub (`/hubs/timer`)

Hub pro časovač.

### 4. NotificationHub (`/hubs/notifications`)

Hub pro notifikace.

### 5. ProductionHub (`/hubs/production`)

Hub pro produkční komunikaci.

## Implementace na straně klienta (Home.razor)

### Inicializace

```csharp
private HubConnection? _hubConnection;

protected override async Task OnInitializedAsync()
{
    await InitializeSignalR();
}

private async Task InitializeSignalR()
{
    _hubConnection = new HubConnectionBuilder()
        .WithUrl(Navigation.ToAbsoluteUri("/hubs/competition"))
        .WithAutomaticReconnect()
        .Build();
    
    // Registrace event handlerů
    _hubConnection.On<int>("OnRoundChanged", async (newRound) => {
        // Handler logika
    });
    
    await _hubConnection.StartAsync();
    await _hubConnection.SendAsync("SubscribeAsPublic");
}
```

### Event handlery

```csharp
// Změna kola
_hubConnection.On<int>("OnRoundChanged", async (newRound) =>
{
    await InvokeAsync(async () =>
    {
        if (_settings != null) _settings.CurrentRound = newRound;
        await LoadData();
        StateHasChanged();
    });
});

// Jste na řadě (pro přihlášený tým)
_hubConnection.On<string, int>("OnYourTurn", async (teamName, position) =>
{
    await InvokeAsync(() =>
    {
        _yourTurnTeamName = teamName;
        _yourTurnPosition = position;
        _showYourTurnModal = true;
        StateHasChanged();
    });
});
```

### Dispose

```csharp
public async ValueTask DisposeAsync()
{
    if (_hubConnection != null)
    {
        await _hubConnection.DisposeAsync();
    }
}
```

## Implementace na straně serveru

### Control.razor (HeadRef)

```csharp
@inject IHubContext<CompetitionHub> CompetitionHubContext

private async Task SaveSettings()
{
    // Detekce změn
    var roundChanged = oldSettings.CurrentRound != _settings.CurrentRound;
    var statusChanged = oldSettings.Status != _settings.Status;
    
    // Uložení
    await SettingsRepository.UpdateSingletonAsync(_settings);
    
    // Poslat notifikace
    if (roundChanged)
    {
        await CompetitionHubContext.Clients.All.SendAsync("OnRoundChanged", _settings.CurrentRound);
    }
    
    if (statusChanged)
    {
        await CompetitionHubContext.Clients.All.SendAsync("OnCompetitionStatusChanged", _settings.Status.ToString());
    }
    
    // Notifikovat týmy
    if (currentTeamAChanged && !string.IsNullOrEmpty(_settings.CurrentTeamAId))
    {
        await CompetitionHubContext.Clients.Group($"team_{_settings.CurrentTeamAId}")
            .SendAsync("OnYourTurn", teamA.Name, 1);
    }
}
```

### RoundOrderManagement.razor

```csharp
@inject IHubContext<CompetitionHub> CompetitionHubContext

private async Task ConfirmOrder()
{
    foreach (var order in _roundOrder)
    {
        order.IsConfirmed = true;
        order.IsPublic = true;
        await RoundOrderRepository.UpdateAsync(order);
    }
    
    // Poslat notifikaci o změně pořadí
    await CompetitionHubContext.Clients.All.SendAsync("OnRoundOrderChanged", _currentRound);
    
    // Notifikovat první tým
    var firstTeam = _teams.FirstOrDefault(t => t.Id == firstOrder.TeamId);
    if (firstTeam != null)
    {
        await CompetitionHubContext.Clients.Group($"team_{firstTeam.Id}")
            .SendAsync("OnYourTurn", firstTeam.Name, 1);
    }
}
```

## UI Komponenty

### Modal "Jste na řadě"

```html
@if (_showYourTurnModal)
{
    <div class="your-turn-modal-overlay" @onclick="CloseYourTurnModal">
        <div class="your-turn-modal" @onclick:stopPropagation="true">
            <div class="modal-icon">
                <span class="material-symbols-outlined">notifications_active</span>
            </div>
            <h2>🎯 JSTE NA ŘADĚ!</h2>
            <div class="team-name-large">@_yourTurnTeamName</div>
            <p class="position-info">
                Připravte se na <strong>Stůl @(_yourTurnPosition == 1 ? "A" : "B")</strong>
            </p>
            <button class="btn-confirm" @onclick="CloseYourTurnModal">
                Rozumím
            </button>
        </div>
    </div>
}
```

## Flow událostí

### 1. Změna kola v Control.razor

1. Admin změní kolo v `/headref/control`
2. Klikne "Uložit nastavení"
3. Server pošle `OnRoundChanged` event všem klientům
4. Home.razor přijme event a reload data
5. UI se aktualizuje s novým kolem

### 2. Losování a potvrzení pořadí

1. Admin v `/admin/round-order` klikne "Zamíchat"
2. Provede se animace míchání
3. Admin klikne "Potvrdit a zamknout"
4. Server pošle `OnRoundOrderChanged` event
5. Všechny Home.razor instance reload pořadí
6. První tým dostane `OnYourTurn` notifikaci
7. Zobrazí se modal "JSTE NA ŘADĚ!"

### 3. Změna aktuálních týmů

1. Admin v `/headref/control` vybere týmy
2. Klikne "Uložit nastavení"
3. Server pošle `OnCurrentTeamsChanged` event
4. Home.razor aktualizuje zobrazení současných týmů
5. Týmy dostanou `OnYourTurn` notifikaci

## Testování

### Otevřít více instancí prohlížeče

1. Otevřít http://localhost:5109 ve dvou oknech
2. V jednom přihlásit jako admin
3. Ve druhém zůstat jako veřejný uživatel
4. Změnit kolo v admin panelu
5. Druhé okno by mělo okamžitě zobrazit nové kolo

### Test notifikace týmu

1. Otevřít Home v jednom okně (přihlášený jako team-1)
2. V druhém okně přihlásit jako admin
3. Jít na `/headref/control`
4. Nastavit team-1 jako Current Team A
5. Uložit nastavení
6. První okno by mělo zobrazit modal "JSTE NA ŘADĚ!"

## Debugging

Console logy pro sledování:

```
[Home] SignalR connection established
[Home] Subscribed to team notifications for team team-1
[Control] Round changed to 2, sending notification
[Home] Round changed to 2
[Control] Notified team Team Alpha that it's their turn (position 1)
[Home] Your turn notification: Team Alpha, position 1
[RoundOrderManagement] Sending OnRoundOrderChanged notification for round 1
[Home] Round order changed for round 1
```

## Bezpečnost

- Všechny notifikace jsou broadcast (Clients.All) nebo group-based (Clients.Group)
- Žádná autorizace na úrovni SignalR není nutná (vše je public read)
- Writable operace (změna nastavení) jsou chráněny na úrovni Razor stránek (@AuthorizeView)

## Výkon

- Automatické reconnect při výpadku spojení
- Broadcast události jsou efektivní (jeden send → N klientů)
- Group-based notifikace pro týmy (pouze relevantní klienti)
