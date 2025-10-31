# Real-time Aktualizace - Testovací Scénáře

Aplikace běží na: **http://localhost:5109**

## Implementace

Real-time aktualizace používají **stejný pattern jako ScoreNotificationService**:

### Pattern (konzistentní napříč celou aplikací):

```csharp
// 1. Interface definuje async events
public interface ICompetitionNotificationService
{
    Task NotifyXAsync(...);
    event Func<..., Task>? OnX;
}

// 2. Implementace volá event + SignalR hub
public async Task NotifyXAsync(...)
{
    _logger.LogInformation("...");
    
    // Notify via SignalR Hub (pro vzdálené klienty)
    await _hubContext.Clients.All.SendAsync("OnX", ...);
    
    // Trigger local event (pro komponenty na stejném serveru)
    if (OnX != null)
    {
        await OnX.Invoke(...);
    }
}

// 3. Komponenty subscribují events
protected override async Task OnInitializedAsync()
{
    CompetitionNotificationService.OnX += HandleX;
    await LoadData();
}

private async Task HandleX(...)
{
    await InvokeAsync(async () =>
    {
        // Update state
        await LoadData();
        StateHasChanged();
    });
}

// 4. Dispose unsubscribe
public void Dispose()
{
    CompetitionNotificationService.OnX -= HandleX;
}
```

## Testovací Scénář 1: Změna Kola

### Kroky:
1. **Otevři 2 okna prohlížeče**:
   - Okno A: `http://localhost:5109/` (Home - veřejný žebříček)
   - Okno B: `http://localhost:5109/headref/control` (Admin control panel)

2. **Přihlas se v okně B**:
   - Username: `admin`
   - Password: `admin123`

3. **V okně B změň kolo**:
   - Změň "Aktuální kolo" z 1 na 2
   - Klikni "Uložit nastavení"

### Očekávaný výsledek:
- ✅ V okně A (Home) se **okamžitě** změní badge "Kolo X z Y" **BEZ nutnosti F5**
- ✅ V konzoli okna A uvidíš: `[Home] Round changed to 2`
- ✅ V debug logu (pro adminy) uvidíš: `Kolo změněno na 2`
- ✅ V terminálu serveru uvidíš:
  ```
  info: BlazorApp1.Services.CompetitionNotificationService[0]
        [CompetitionNotificationService] Round changed to 2
  ```

---

## Testovací Scénář 2: Změna Stavu Soutěže

### Kroky:
1. **Stejné 2 okna** jako výše
2. **V okně B změň stav**:
   - Změň "Stav soutěže" z "Příprava" na "Probíhá"
   - Klikni "Uložit nastavení"

### Očekávaný výsledek:
- ✅ V okně A se **okamžitě** změní badge "Stav: Příprava" → "Stav: Probíhá" **BEZ F5**
- ✅ V konzoli okna A: `[Home] Competition status changed to InProgress`
- ✅ V debug logu: `Stav soutěže změněn na InProgress`

---

## Testovací Scénář 3: Změna Current Teams

### Kroky:
1. **Stejné 2 okna**
2. **V okně B nastav current teams**:
   - Current Team A: Vyber nějaký tým (např. "Roboti")
   - Current Team B: Vyber jiný tým (např. "Mechanici")
   - Klikni "Uložit nastavení"

### Očekávaný výsledek:
- ✅ V okně A se **okamžitě** aktualizuje "Aktuální zápas" sekce **BEZ F5**
- ✅ Zobrazí se názvy týmů a školy
- ✅ V konzoli: `[Home] Current teams changed`
- ✅ Server log:
  ```
  info: BlazorApp1.Services.CompetitionNotificationService[0]
        [CompetitionNotificationService] Current teams changed: A=team-1, B=team-2
  ```

---

## Testovací Scénář 4: Modal "JSTE NA ŘADĚ!"

### Příprava:
1. **Vytvoř 3 okna**:
   - Okno A: `http://localhost:5109/` (Home jako veřejnost)
   - Okno B: `http://localhost:5109/headref/control` (Admin)
   - **Okno C: `http://localhost:5109/login` → přihlas se jako tým**
     - PIN: `1234` (nebo jiný PIN týmu z teams.json)
     - Po přihlášení přejdi na `http://localhost:5109/`

### Kroky:
1. **V okně B (Admin) nastav current team**:
   - Current Team A: Vyber **TÝM, KTERÝ JE PŘIHLÁŠEN v okně C**
   - Klikni "Uložit nastavení"

### Očekávaný výsledek:
- ✅ V okně C (přihlášený tým) se **okamžitě zobrazí modal**:
  ```
  🎯 JSTE NA ŘADĚ!
  [Název týmu]
  Připravte se na Stůl A
  [Tlačítko: Rozumím]
  ```
- ✅ V okně A (veřejnost) **NEZOBRAZÍ se modal** (modal je jen pro přihlášený tým)
- ✅ V konzoli okna C: `[Home] Your turn notification: [TeamName], position 1`
- ✅ Server log:
  ```
  info: BlazorApp1.Services.CompetitionNotificationService[0]
        [CompetitionNotificationService] Your turn: TeamName (teamId=team-x), position=1
  ```

---

## Testovací Scénář 5: Leaderboard Update (po dokončení round)

### Kroky:
1. **2 okna**: Home (A) a Admin (B)
2. **V okně B**: Jdi na Scoring stránku, schval nějaké skóre
3. **Po schválení** by měl FinalScoreService zavolat NotifyLeaderboardUpdatedAsync

### Očekávaný výsledek:
- ✅ V okně A se **okamžitě** aktualizuje žebříček **BEZ F5**
- ✅ Pozice týmů se přeřadí
- ✅ Zobrazí se animace změny pozice (↑↓)

---

## Debugging Tips

### Konzole prohlížeče (F12):
```javascript
// Měly by se zobrazovat logy:
[Home] Round changed to X
[Home] Competition status changed to X
[Home] Current teams changed: A=..., B=...
```

### Server Terminal:
```
info: BlazorApp1.Services.CompetitionNotificationService[0]
      [CompetitionNotificationService] Round changed to X
```

### Admin Debug Log (na Home stránce když jsi přihlášen jako admin):
- Na konci stránky uvidíš "Admin Debug Log" s timestampy všech událostí

---

## Co dělat když to nefunguje:

### 1. Zkontroluj Console (F12):
- Jsou tam chyby?
- Zobrazují se logy `[Home] ...`?

### 2. Zkontroluj Server Terminal:
- Zobrazují se `[CompetitionNotificationService] ...` logy?

### 3. Hard Refresh:
- Ctrl + F5 (vynucený reload)
- Zkus zavřít a otevřít nové okno

### 4. Zkontroluj Circuit:
```
info: BlazorApp1.Services.CircuitHandlerService[0]
      [Circuit ...] Circuit opened (new session started)
info: BlazorApp1.Services.CircuitHandlerService[0]
      [Circuit ...] Connection established
```
- Pokud vidíš "Connection lost", reload stránku

### 5. Restart aplikace:
```powershell
# Zastavit
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue

# Spustit znovu
cd DroidGames\BlazorApp1
dotnet run
```

---

## Implementované Events:

| Event | Kdy se volá | Co se aktualizuje |
|-------|-------------|-------------------|
| `OnRoundChanged` | Změna čísla kola | Badge "Kolo X z Y" |
| `OnCompetitionStatusChanged` | Změna stavu soutěže | Badge stavu (Příprava/Probíhá/...) |
| `OnCurrentTeamsChanged` | Změna current teams | Sekce "Aktuální zápas" |
| `OnNextTeamsChanged` | Změna next teams | Sekce "Následující:" |
| `OnRoundOrderChanged` | Potvrzení pořadí kola | Reload celých dat (včetně pořadí) |
| `OnLeaderboardUpdated` | Po schválení score | Žebříček se přeřadí |
| `OnYourTurn` | Tým je nastaven jako current | Modal "JSTE NA ŘADĚ!" |

---

## Architektura (stejná jako ScoreNotificationService):

```
Control.razor (Admin)
    ↓ SaveSettings()
    ↓ await CompetitionNotificationService.NotifyRoundChangedAsync(2)
    ↓
CompetitionNotificationService
    ↓ 1. Raise C# event: OnRoundChanged?.Invoke(2)
    ↓ 2. SignalR broadcast: _hubContext.Clients.All.SendAsync("OnRoundChanged", 2)
    ↓
Home.razor (subscribed to OnRoundChanged event)
    ↓ HandleRoundChanged(2)
    ↓ InvokeAsync(() => { _settings.CurrentRound = 2; StateHasChanged(); })
    ↓
UI se aktualizuje OKAMŽITĚ ✅
```

**Klíčové výhody tohoto patternu:**
- ✅ Konzistentní napříč celou aplikací
- ✅ Funguje v Blazor Server bez SignalR.Client
- ✅ Podporuje jak lokální (same circuit) tak vzdálené klienty
- ✅ Testovatelné (můžeš mockovat ICompetitionNotificationService)
- ✅ Loose coupling (komponenty jsou oddělené přes events)
