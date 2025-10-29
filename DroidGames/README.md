# 🤖 DroidGames - Management System pro Robotickou Soutěž

Kompletní real-time management systém pro Droid Games 2026.

## ✨ Hlavní Funkce

- ✅ Real-time live leaderboard
- ✅ Multi-role systém (Public, Team, Referee, HeadReferee, Production, Admin)
- ✅ SignalR real-time komunikace
- ✅ JSON persistence (bez databáze)
- ✅ API pro ESP32 hardware timer
- ✅ Achievement systém
- ✅ Gamifikovaný moderní design s barvami z loga
- ✅ Auto-backup každých 5 minut

## 🚀 Rychlý Start

### Požadavky

- .NET 8.0 SDK
- Visual Studio 2022 nebo VS Code

### Spuštění

1. **Naklonujte/otevřete projekt:**
```bash
cd DroidGames/BlazorApp1
```

2. **Obnovte NuGet balíčky:**
```bash
dotnet restore
```

3. **Spusťte aplikaci:**
```bash
dotnet run
```

4. **Otevřete prohlížeč:**
```
https://localhost:5001
```

## 📁 Struktura Dat

Aplikace automaticky vytvoří složku `data/` s těmito soubory:

- `teams.json` - Týmy a jejich výsledky
- `maps.json` - Konfigurace herních map
- `achievements.json` - Úspěchy
- `quiz.json` - Kvízové otázky
- `reminders.json` - Připomínky pro moderátora
- `funfacts.json` - Fun facts
- `users.json` - Uživatelé systému
- `settings.json` - Nastavení soutěže
- `backups/` - Automatické zálohy každých 5 minut

## 🎯 Inicializace Dat

### Vytvoření prvních týmů

Spusťte aplikaci a soubory se vytvoří automaticky. Pro přidání testovacích dat můžete upravit JSON soubory v `data/` složce.

Příklad `data/teams.json`:
```json
[
  {
    "Id": "team-1",
    "Name": "RoboMasters",
    "School": "ZŠ Plzeň",
    "Members": ["Jan Novák", "Eva Svobodová", "Petr Dvořák"],
    "PinCode": "1234",
    "Rounds": [],
    "UnlockedAchievements": [],
    "TotalScore": 0,
    "CurrentPosition": 1,
    "CreatedAt": "2026-01-15T10:00:00Z"
  }
]
```

### Vytvoření admin účtu

Upravte `data/users.json`:
```json
[
  {
    "Id": "admin-1",
    "Username": "admin",
    "PasswordHash": "YWRtaW4xMjM=",
    "Role": "Admin"
  }
]
```

Přihlášení: `admin` / `admin123`

## 🎨 Design

Aplikace používá barvy z DroidGames loga:
- **Primární modrá:** #0066ff
- **Oranžová:** #ff6600
- **Tyrkysová:** #00d4ff
- **Zlatá:** #ffd700

Design je plně gamifikovaný s animacemi a moderním dark theme.

## 🔌 ESP32 API

API pro hardwarový timer je dostupné na:

```
POST /api/hardware/timer/start
POST /api/hardware/timer/stop
POST /api/hardware/timer/reset
GET  /api/hardware/timer/status
```

**Autentizace:** Přidejte header `X-Api-Key` s hodnotou z `settings.json`

### Příklad ESP32 kódu

```cpp
#include <WiFi.h>
#include <HTTPClient.h>

const char* apiUrl = "https://your-server.com/api/hardware/timer";
const char* apiKey = "your-api-key";

void startTimer() {
  HTTPClient http;
  http.begin(String(apiUrl) + "/start");
  http.addHeader("X-Api-Key", apiKey);
  int httpCode = http.POST("");
  http.end();
}
```

## 📱 Role a Přístupy

| Role | Popis | Přístup |
|------|-------|---------|
| **Public** | Veřejnost | Live leaderboard |
| **Team** | Týmy | Dashboard, statistiky, achievementy, kvíz |
| **Referee** | Rozhodčí | Bodování, interaktivní mapa |
| **HeadReferee** | Hlavní rozhodčí | Schvalování bodů, timer, režie |
| **Production** | Režie | Komunikace s moderátorem |
| **Admin** | Administrátor | Správa týmů, map, nastavení |

## 🏗️ Architektura

```
Blazor Server (ASP.NET Core 8)
├── SignalR Hubs (Real-time)
├── Services (Business Logic)
├── JSON Repository (Data Persistence)
├── Background Services (Timer, Backup)
└── REST API (ESP32 Hardware)
```

## 🎮 Gamifikace

### Achievementy
- **First Blood** - První body v soutěži
- **Crystal Hunter** - Dotek všech krystalů
- **Perfect Round** - Perfektní kolo bez chyb
- **Unbeatable** - Výhra všech kol
- A mnoho dalších...

### Animace
- Confetti pro top 3 týmy
- Pulzující timer při posledních vteřinách
- Smooth transitions v leaderboardu
- Unlock animace pro achievementy

## 📊 Monitorování

Aplikace automaticky loguje všechny důležité události do konzole:
- Změny skóre
- Timer události
- ESP32 požadavky
- Chyby a varování

## 🔧 Konfigurace

### appsettings.json

```json
{
  "DataPath": "data",
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## 🐛 Troubleshooting

### Port už je použit
```bash
dotnet run --urls "https://localhost:5002;http://localhost:5003"
```

### Data se neuložila
- Zkontrolujte oprávnění k zápisu do složky `data/`
- Podívejte se do `data/backups/` na poslední zálohu

### SignalR nefunguje
- Zkontrolujte firewall
- Ověřte, že používáte HTTPS v production

## 📝 Další Vývoj

Pro rozšíření aplikace:

1. **Interaktivní mapa** - Implementovat SVG mapu s drag & drop
2. **Diplomy** - Přidat QuestPDF pro generování PDF
3. **Admin UI** - Vytvořit kompletní admin rozhraní
4. **Quiz systém** - Dobudovat gamifikovaný kvíz
5. **Heatmapy** - Vizualizace nejčastěji používaných pozic

## 📧 Podpora

Pro otázky a podporu kontaktujte:
- Email: support@centrumrobotiky.eu
- Web: https://centrumrobotiky.eu

## 📜 Licence

© 2026 Centrum Robotiky Plzeň
Vytvořeno pro Droid Games 2026

---

**Ready for Droid Games 2026!** 🚀🤖
