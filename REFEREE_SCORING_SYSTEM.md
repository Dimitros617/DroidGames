# Referee Scoring System - Dokumentace

## 📋 Přehled

Interaktivní systém pro bodování týmů rozhodčími během soutěžních kol pomocí klikací mapy.

## ✅ Implementováno

### 1. Hlavní stránka `/referee/scoring`

**Funkce:**
- Zobrazení aktuálního kola a soupeřících týmů
- Výběr týmu pro bodování
- Interaktivní mapa 6x9 bloků
- Real-time výpočet skóre
- Odesílání hodnocení

### 2. Komponenty

**Referee/Scoring.razor**
- Kompletní UI pro bodování
- Správa stavu (completed blocks, score breakdown)
- Integration se službami (TeamService, MapService, ScoreService)

**Scoring.razor.css**
- Moderní responzivní design
- Animace a přechody
- Barevné schéma podle typu bloku

## 🎮 Jak používat

### Přihlášení
1. Přihlásit se jako Rozhodčí (role: Referee)
2. Navigace → Bodování

### Bodování týmu
1. **Vybrat tým** - Kliknout na tlačítko s názvem týmu
2. **Klikat na bloky** - Kliknout na bloky, které tým splnil
   - První klik = přidá body (zelený rámeček)
   - Druhý klik = odebere body
3. **Kontrola skóre** - Aktuální skóre se zobrazuje nahoře
4. **Odeslat** - Tlačítko "Odeslat hodnocení"

## 🗺️ Typy bloků a bodování

### Kameny 🪨 (Rock)
- **Barva**: Šedá
- **Body**: +5 bodů
- **Popis**: Základní překážka

### Modré krystaly 💎 (BlueCrystal)
- **Barva**: Modrá
- **Body**: +10 bodů
- **Popis**: Střední hodnota

### Žlutá síra 🟡 (YellowSulfur)
- **Barva**: Žlutá
- **Body**: +15 bodů
- **Popis**: Nejvyšší hodnota

### Prázdné pole (Empty)
- **Barva**: Šedá
- **Body**: 0 bodů
- **Popis**: Nelze kliknout

## 📊 Funkce

### Interaktivní mapa
```razor
<div class="map-grid">
    @for (int y = 0; y < 6; y++)
    {
        @for (int x = 0; x < 9; x++)
        {
            <div class="map-cell" @onclick="() => OnBlockClick(block, x, y)">
                <!-- Block content -->
            </div>
        }
    }
</div>
```

### Výpočet skóre
```csharp
private void OnBlockClick(MapBlock? block, int x, int y)
{
    var key = $"{x},{y}";
    
    if (_completedBlocks.Contains(key))
    {
        // Remove score
        _completedBlocks.Remove(key);
        var blockName = GetBlockName(block);
        _scoreBreakdown[blockName] -= GetBlockPoints(block);
    }
    else
    {
        // Add score
        _completedBlocks.Add(key);
        var blockName = GetBlockName(block);
        _scoreBreakdown[blockName] += GetBlockPoints(block);
    }
    
    _currentScore = _scoreBreakdown.Values.Sum();
}
```

### Odesílání hodnocení
```csharp
private async Task SubmitScore()
{
    var refereeScore = new RefereeScore
    {
        RefereeId = UserSession.UserId,
        ScoreBreakdown = new Dictionary<string, int>(_scoreBreakdown),
        TotalScore = _currentScore,
        SubmittedAt = DateTime.UtcNow
    };
    
    await ScoreService.SubmitRefereeScoreAsync(
        _selectedTeamId, 
        _currentRound.RoundNumber, 
        refereeScore
    );
}
```

## 🎨 UI Komponenty

### Header
- Název kola
- Zobrazení soupeřících týmů
- Timer (připraveno pro budoucí integraci)

### Team Selection
- Velká tlačítka pro výběr týmu
- Zvýraznění aktivního výběru
- Material Icons

### Interaktivní mapa
- Grid 6 řádků × 9 sloupců
- Každá buňka 80×80px (responsive)
- Hover efekt
- Completion badge (zelený checkmark)

### Score Summary
- Rozpad bodů podle typu
- Celkové skóre
- Real-time update

### Submit Button
- Velké tlačítko s ikonou
- Disabled stav (0 bodů)
- Loading spinner

## 🔧 Technické detaily

### State Management
```csharp
private Dictionary<string, int> _scoreBreakdown = new();
private HashSet<string> _completedBlocks = new();
private int _currentScore = 0;
```

### Block Identification
- Koordináty: `"x,y"` (např. "3,2")
- HashSet pro rychlé vyhledávání

### Responsive Design
- Desktop: 80×80px buňky
- Tablet: 70×70px buňky
- Mobile: 50×50px buňky

## 📱 Responzivita

### Desktop (> 1200px)
- Plná velikost mapy
- Všechny funkce
- Optimální UX

### Tablet (768px - 1200px)
- Menší buňky mapy
- Zachovaná funkčnost

### Mobile (< 768px)
- Minimální buňky
- Vertikální layout
- Scrollable mapa

## 🚀 Budoucí vylepšení

1. **Live Timer Integration**
   - Napojení na TimerService
   - Real-time odpočet
   - Auto-submit při vypršení času

2. **Multiple Referees**
   - Zobrazení hodnocení od všech rozhodčích
   - Agregace skóre
   - Konflikt řešení

3. **Undo/Redo**
   - Historie akcí
   - Zpět/Vpřed tlačítka

4. **Photo Evidence**
   - Upload foto důkazů
   - Připojení k blokům
   - Gallery view

5. **Voice Commands**
   - Hlasové ovládání
   - "Přidat kámen na 3,2"

6. **Offline Mode**
   - Local storage
   - Sync po připojení

7. **Analytics**
   - Časová analýza
   - Heatmapa nejčastěji kliknutých bloků
   - Průměrné skóre

## 🐛 Známá omezení

1. **Mock data** - Zatím používá mock týmy a kola
2. **Žádná validace** - Nelze ověřit, zda rozhodčí kliká správně
3. **No undo** - Nelze vrátit zpět jednotlivé akce
4. **Single referee view** - Nevidí hodnocení jiných rozhodčích

## 🔐 Autorizace

- RequiredRole: `UserRole.Referee`
- AuthGuard komponenta
- Automatická redirect při nedostatečných právech

## 📊 Data flow

```
User clicks block
    ↓
OnBlockClick()
    ↓
Toggle _completedBlocks
    ↓
Update _scoreBreakdown
    ↓
Recalculate _currentScore
    ↓
StateHasChanged()
    ↓
UI update
```

## 🎯 Klíčové metriky

- **Rychlost bodování**: < 1 minuta na tým
- **Přesnost**: Real-time výpočet
- **UX**: Intuitivní klikání
- **Responzivita**: Okamžitá reakce

## 📝 Poznámky

- Mapa je immutable - bloky nelze přesouvat
- Skóre je temporary dokud není odesláno
- Toast notifikace pro feedback
- Automatický reset po odeslání
