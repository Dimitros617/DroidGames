# PDF Template Folder

Tato složka obsahuje šablony pro generování PDF diplomů.

## Požadovaný soubor

Umístěte váš PDF template z Adobe Illustratoru do této složky s názvem:

```
diploma-template.pdf
```

## Jak to funguje

DiplomaService používá QuestPDF pro overlay textu na váš template:

1. **Template jako pozadí** - Váš PDF bude použit jako pozadí
2. **Dynamický text** - Na template se přidají tyto informace:
   - Jméno člena týmu
   - Název týmu
   - Škola
   - Umístění (1., 2., 3. místo atd.)
   - Celkové body

## Pozice textových polí

Aktuální pozice jsou nastaveny v `DiplomaService.cs`:

```csharp
// Jméno člena - 200px od vrchu, střed
// Název týmu - 220px od vrchu, střed  
// Škola - 230px od vrchu, střed
// Umístění - 270px od vrchu, střed
// Body - 290px od vrchu, střed
```

Po přidání vašeho template můžete tyto pozice upravit podle layoutu.

## Fallback režim

Pokud `diploma-template.pdf` neexistuje, DiplomaService vygeneruje diplom od nuly s moderním designem.

## Použití

Diplomy lze generovat z Admin stránky (připravuje se):
- **Jednotlivé týmy** - ZIP se všemi diplomy pro jeden tým
- **Všechny týmy** - ZIP se všemi diplomy pro všechny týmy
