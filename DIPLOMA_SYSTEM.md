# Diploma System - Implementační dokumentace

## 📜 Přehled

Systém pro generování PDF diplomů pro týmy a jejich členy s podporou vlastních PDF šablon.

## ✅ Implementováno

### 1. Backend služby

**DiplomaService.cs**
- `GenerateDiplomaAsync(teamId, memberName, position)` - Generuje jeden diplom
- `GenerateBulkDiplomasAsync(teamId)` - Generuje ZIP pro všechny členy týmu
- `GenerateAllTeamsDiplomasAsync()` - Generuje ZIP pro všechny týmy

### 2. Frontend komponenty

**Admin/Diplomas.razor**
- Výběr týmu
- Zobrazení detailů týmu a seznamu členů
- Stahování diplomů pro vybraný tým nebo všechny týmy
- Kontrola existence PDF template
- Status notifikace

**Shared/AdminNav.razor**
- Navigace pro admin sekci
- Tabs: Týmy, Diplomy, Mapy

### 3. Utility funkce

**wwwroot/js/utilities.js**
- `downloadFile(filename, base64Data)` - Stahování souborů
- `copyToClipboard(text)` - Kopírování do schránky
- `showToast(message, type, duration)` - Toast notifikace

### 4. Template podpora

**wwwroot/templates/**
- Složka pro PDF šablonu z Adobe Illustratoru
- README.md s instrukcemi
- Fallback: pokud šablona neexistuje, generuje se výchozí design

## 🎨 Vlastnosti diplomu

### Výchozí design (bez template)
- **Layout**: A4 na šířku
- **Header**: DROID GAMES 2026
- **Obsah**:
  - Jméno člena (velké, zvýrazněné)
  - Název týmu
  - Škola
  - Umístění (barevně odlišené)
  - Celkové body
- **Footer**: Centrum Robotiky Plzeň + datum

### S PDF template
- Template jako pozadí
- Overlay textu na specifikované pozice
- Přizpůsobitelné pozice v kódu

### Barevné schéma umístění
- 🥇 **1. místo**: Zlatá (#FFD700)
- 🥈 **2. místo**: Stříbrná (#C0C0C0)
- 🥉 **3. místo**: Bronzová (#CD7F32)
- Ostatní: Tmavě šedá (#34495e)

## 📦 Export formáty

### Jednotlivý tým
```
Diplomy_NazevTymu.zip
  ├── NazevTymu_JmenoClena1.pdf
  ├── NazevTymu_JmenoClena2.pdf
  └── ...
```

### Všechny týmy
```
Diplomy_Vsechny_Tymy_20260315.zip
  ├── 1_Tým1_Člen1.pdf
  ├── 1_Tým1_Člen2.pdf
  ├── 2_Tým2_Člen1.pdf
  └── ...
```

## 🔧 Konfigurace

### Přidání PDF template

1. Export PDF z Adobe Illustratoru (A4 landscape)
2. Uložit jako `diploma-template.pdf`
3. Nahrát do `wwwroot/templates/`
4. Upravit pozice textu v `DiplomaService.cs` (metoda `GenerateFromTemplateAsync`)

### Úprava pozic textu

```csharp
// V DiplomaService.cs - GenerateFromTemplateAsync
column.Item().PaddingTop(200).AlignCenter()  // Změnit 200 na jinou hodnotu
    .Text(memberName)
    .FontSize(36)  // Velikost písma
    .FontColor("#2c3e50");  // Barva textu
```

## 🚀 Použití

### Admin stránka
1. Přihlásit se jako Admin
2. Navigace → Admin → Diplomy
3. Vybrat tým
4. Kliknout "Stáhnout diplomy pro vybraný tým"
5. Nebo "Stáhnout diplomy pro všechny týmy"

### API volání (pro budoucí integraci)
```csharp
// Jeden diplom
var pdf = await diplomaService.GenerateDiplomaAsync(teamId, "Jan Novák", 1);

// Všechny členy týmu
var zip = await diplomaService.GenerateBulkDiplomasAsync(teamId);

// Všechny týmy
var allZip = await diplomaService.GenerateAllTeamsDiplomasAsync();
```

## 📋 Závislosti

- **QuestPDF 2025.7.3** - PDF generování
- **System.IO.Compression** - ZIP archivy

## ⚙️ Technické detaily

### QuestPDF Konfigurace
```csharp
QuestPDF.Settings.License = LicenseType.Community;
```

### Template cesta
```csharp
_templatePath = Path.Combine(_environment.WebRootPath, "templates", "diploma-template.pdf");
```

### Bezpečnost souborů
- Automatické ošetření speciálních znaků v názvech souborů
- Replace mezer, lomítek, backslashů

## 🔜 Možná vylepšení

1. **Preview diplom** - Náhled před stažením
2. **Hromadný email** - Poslat diplomy emailem
3. **Vlastní pozice** - UI pro nastavení pozic textu
4. **Více template** - Různé šablony pro různá umístění
5. **Digitální podpis** - PDF signing
6. **Watermark** - Přidání loga/razítka
7. **QR kód** - Ověření autenticity

## 📝 Poznámky

- Diplomy se generují on-demand (nevytvářejí se předem)
- Každý diplom obsahuje aktuální data z databáze
- ZIP soubory se generují v paměti (ne na disku)
- Podpora pro neomezený počet členů týmu

## 🐛 Známé limitace

1. PDF template musí být obrázek (zatím není podpora pro overlay na existující PDF s textem)
2. Pozice textu jsou hardcoded (nutné upravit v kódu)
3. Chybí preview funkce
4. Žádná validace PDF template formátu

## 👥 Team.Members

Model `Team` již obsahuje pole `Members`:
```csharp
public List<string> Members { get; set; } = new();
```

Data se ukládají do `teams.json` - je potřeba přidat členy přes Admin/Teams stránku.
