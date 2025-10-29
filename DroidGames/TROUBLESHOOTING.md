# 🔧 Řešení problémů - Droid Games

## ✅ VYŘEŠENO: Aplikace se nekonečně načítala

### Problém
Aplikace se po spuštění nekonečně načítala a nezobrazila žádný obsah. Pokud byly ze stránky odstraněny všechny DI závislosti, fungovala správně.

### Příčina
**Deadlock v Blazor Server kvůli asynchronnímu volání v konstruktoru `JsonRepository`**

V původním kódu `JsonRepository.cs`:
```csharp
public JsonRepository(string dataDirectory, string fileName)
{
    // ...
    LoadAsync().Wait(); // ❌ DEADLOCK!
}
```

Volání `.Wait()` na asynchronní metodu v Blazor Server způsobuje deadlock, protože:
1. Konstruktor se volá v synchronním kontextu při inicializaci DI kontejneru
2. `.Wait()` blokuje aktuální vlákno
3. Blazor Server využívá synchronizační kontext, který nesmí být blokován
4. Výsledek: aplikace "zamrzne" při inicializaci services

### Řešení

**Změna na synchronní načítání souborů v konstruktoru:**

```csharp
public JsonRepository(string dataDirectory, string fileName)
{
    // ...
    try
    {
        LoadSync(); // ✅ Synchronní načítání
        Console.WriteLine($"[DEBUG] JsonRepository<{typeof(T).Name}> initial load complete. Items: {_cache.Count}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] JsonRepository<{typeof(T).Name}> failed to load: {ex}");
        throw;
    }
}

private void LoadSync()
{
    Console.WriteLine($"[DEBUG] JsonRepository<{typeof(T).Name}> LoadSync start");
    if (File.Exists(_filePath))
    {
        var json = File.ReadAllText(_filePath); // Synchronní čtení
        _cache = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
    }
    else
    {
        _cache = new List<T>();
        var json = JsonSerializer.Serialize(_cache, _jsonOptions);
        File.WriteAllText(_filePath, json); // Synchronní zápis
    }
}
```

### Další vylepšení

1. **Debug logging** - přidány Console.WriteLine() pro diagnostiku
2. **Lepší error handling** - try-catch bloky s detailním logováním
3. **Home.razor vylepšení** - přidán UI pro zobrazení chyb a debug logu
4. **Async warnings fix** - opraveny metody v `QuizService`

### Jak testovat opravu

1. Spusťte aplikaci:
```powershell
dotnet run --project ".\BlazorApp1\BlazorApp1.csproj"
```

2. Zkontrolujte výstup v konzoli:
```
[DEBUG] JsonRepository<Team> ctor start. Path: ...
[DEBUG] JsonRepository<Team> LoadSync start
[DEBUG] JsonRepository<Team> reading ...
[DEBUG] JsonRepository<Team> loaded 3 items
[DEBUG] TeamService created
```

3. Otevřete `http://localhost:5109` v prohlížeči
4. Stránka by se měla načíst okamžitě a zobrazit týmy

## 🚀 Obecná doporučení

### Blazor Server + DI + I/O operace

**❌ NEPOUŽÍVEJTE:**
```csharp
public MyService()
{
    SomeAsyncMethod().Wait();  // Deadlock!
    SomeAsyncMethod().Result;  // Deadlock!
    Task.Run(() => SomeAsyncMethod()).Wait(); // Stále může způsobit problémy
}
```

**✅ POUŽIJTE:**
```csharp
// Možnost 1: Synchronní I/O v konstruktoru (pro malé soubory)
public MyService()
{
    var data = File.ReadAllText(path);
}

// Možnost 2: Lazy initialization
private List<T>? _cache;
public async Task<List<T>> GetDataAsync()
{
    if (_cache == null)
    {
        _cache = await LoadDataAsync();
    }
    return _cache;
}

// Možnost 3: Factory pattern
public interface IMyServiceFactory
{
    Task<IMyService> CreateAsync();
}
```

### Debug tips

1. **Vždy přidejte logging do konstruktorů služeb**
2. **Používejte Console.WriteLine pro okamžitý feedback**
3. **Kontrolujte Browser Console (F12) pro client-side chyby**
4. **V Blazor Server zkontrolujte SignalR connection status**

## 📝 Známé problémy

### Warnings o chybějících await
Některé metody v `QuizService` mají warning CS1998. To je OK pokud metody jen vracejí hodnoty přes `Task.FromResult()`.

### HTTPS redirect warning
```
Failed to determine the https port for redirect.
```
To je normální ve vývojovém režimu. V produkci nakonfigurujte HTTPS v `launchSettings.json`.

## 🔍 Další diagnostika

Pokud stále máte problémy:

1. **Zkontrolujte browser console** (F12):
   - Hledejte SignalR chyby
   - Zkontrolujte network tab pro failed requests

2. **Zkontrolujte Application Output** ve Visual Studio nebo terminálu:
   - Červené [ERROR] zprávy
   - Stack traces

3. **Testujte komponenty postupně**:
   - Začněte s prázdnou stránkou
   - Postupně přidávejte @inject direktivy
   - Najděte která služba způsobuje problém

4. **Zkontrolujte JSON soubory**:
   - Jsou validní JSON?
   - Mají správnou strukturu podle modelů?
   - Existují na správné cestě?

## 📚 Reference

- [Blazor Server Dependency Injection](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection)
- [Async/Await Best Practices](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
- [Blazor Error Handling](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors)
