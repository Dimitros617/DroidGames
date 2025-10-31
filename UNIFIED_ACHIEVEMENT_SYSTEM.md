# Unified Achievement System

## Přehled

Systém achievementů byl sjednocen tak, aby všechny achievementy (jak z kvízu, tak z hodnocení rozhodčích) byly definovány na jednom místě a správně se zobrazovaly na stránce `/team/achievements`.

## Struktura

### 1. Definice achievementů (`achievements.json`)

Všechny achievementy jsou nyní definovány v souboru `/DroidGames/BlazorApp1/Data/achievements.json`:

- **13 Quiz achievementů** - Získávané za odpovědi na otázky v kvízu
- **10 Evaluation achievementů** - Získávané za výkony v jednotlivých kolech (hodnocení rozhodčích)

**Celkem: 23 achievementů**

### 2. Typy podmínek (`AchievementConditionType.cs`)

Byly přidány nové typy podmínek pro evaluation achievementy:

```csharp
// Evaluation-based achievements (from referee scoring)
FirstCrystalTouch,          // První dotyk krystalu v celé soutěži
FirstSulfurHit,             // První narušení síry v celé soutěži
ThreeCrystalsInRow,         // 3 krystaly dotknuté za sebou
AllCrystalsTouched,         // Dotknutí všech krystalů v kole
AllSulfursCleared,          // Odsunuti všech sír v kole
PerfectRun,                 // Perfektní jízda - pouze krystaly
SpeedDemon,                 // Rychlé dokončení kola
MinimalMovesEfficient,      // Vysoké skóre s malým počtem tahů
NoSulfurDamage,             // Žádné narušení síry v kole
CrystalMaster,              // Více než 10 krystalů v jednom kole
```

### 3. Jednotný zdroj dat

Všechny stránky nyní načítají odemčené achievementy z **jednotného zdroje** - `TeamAchievement` repository:

- **Achievements.razor** (`/team/achievements`) - Zobrazuje všechny achievementy a stav jejich odemčení
- **Dashboard.razor** (`/team/dashboard`) - Zobrazuje počet odemčených achievementů
- **TeamEdit.razor** (`/admin/teams/{id}/edit`) - Admin stránka pro úpravu týmu

### 4. Evaluace achievementů

#### Quiz achievementy
Vyhodnocovány službou `AchievementService.CheckQuizAchievementsAsync()` při zodpovězení otázky.

#### Evaluation achievementy
Vyhodnocovány službou `AchievementEvaluationService.EvaluateRoundAchievementsAsync()` při schválení hodnocení rozhodčích (po dokončení kola).

Logika vyhodnocení zůstala zachována v `AchievementEvaluationService` a obsahuje:

- `CheckFirstCrystalTouch()` - První dotyk krystalu v soutěži
- `CheckFirstSulfurHit()` - První narušení síry v soutěži
- `CheckThreeCrystalsInRow()` - 3 krystaly za sebou
- `CheckAllCrystalsTouched()` - Všechny krystaly v kole
- `CheckAllSulfursCleared()` - Všechny síry v kole
- `CheckPerfectRun()` - Perfektní jízda bez síry
- `CheckSpeedDemon()` - Rychlé dokončení
- `CheckMinimalMoves()` - Efektivní strategie
- `CheckNoSulfurDamage()` - Bez poškození
- `CheckCrystalMaster()` - Více než 10 krystalů

### 5. Tok dat

```
1. Rozhodčí schvalují hodnocení kola
   ↓
2. ScoreFinalizationService.FinalizeApprovedScoreAsync()
   ↓
3. AchievementEvaluationService.EvaluateRoundAchievementsAsync()
   ↓
4. Kontrola jednotlivých achievement podmínek
   ↓
5. Uložení do TeamAchievement repository
   ↓
6. Notifikace týmu přes WebSocket
   ↓
7. Zobrazení na /team/achievements
```

## Výhody sjednoceného systému

1. ✅ **Všechny achievementy na jednom místě** - `/team/achievements` zobrazuje quiz i evaluation achievementy
2. ✅ **Jednotný zdroj dat** - `TeamAchievement` repository je single source of truth
3. ✅ **Zachovaná logika** - Evaluation logika zůstala beze změny v `AchievementEvaluationService`
4. ✅ **Konzistentní zobrazení** - Všechny stránky používají stejný zdroj dat
5. ✅ **Snadná údržba** - Nové achievementy se přidávají do `achievements.json`

## Testování

Pro ověření správné funkce systému:

1. **Quiz achievementy**: Zodpovězte otázky v kvízu a zkontrolujte odemknutí na `/team/achievements`
2. **Evaluation achievementy**: Dokončete kolo a nechte rozhodčí schválit hodnocení
3. **Zobrazení**: Zkontrolujte, že všechny odemčené achievementy se zobrazují na stránce

## Migrace starých dat

Pokud existují staré achievementy v `Team.UnlockedAchievements`, tyto již nejsou používány. Systém nyní používá pouze `TeamAchievement` repository.
