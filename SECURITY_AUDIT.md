# 🔒 Bezpečnostní audit WebSocket / SignalR - DroidGames

## ✅ NALEZENÉ PROBLÉMY A DOPORUČENÍ

### 🔴 KRITICKÉ - Nutné opravit

#### 1. **SignalR Timeouty - CHYBÍ**
**Problém:** SignalR nemá nakonfigurované timeouty pro odpojené klienty.

**Riziko:**
- Zombie connections (klient spadne, server drží spojení)
- Memory leaky při výpadku sítě
- Vyčerpání server resources

**Doporučení:**
```csharp
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment(); // Pouze v Development!
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30); // Klient musí pingovat každých 30s
    options.HandshakeTimeout = TimeSpan.FromSeconds(15); // Max čas na handshake
    options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Server pinguje klienta
    options.MaximumParallelInvocationsPerClient = 1; // Prevence flooding
    options.MaximumReceiveMessageSize = 32 * 1024; // 32 KB limit (bezpečnost)
    options.StreamBufferCapacity = 10; // Limit stream bufferu
});
```

#### 2. **UserSession - Žádné čištění při Circuit disposal**
**Problém:** Když WebSocket spojení spadne, UserSession v paměti zůstává.

**Riziko:**
- Memory leak při mnoha reconnectech
- Staré session data v paměti

**Doporučení:** Implementovat `IDisposable` v UserSession:
```csharp
public class UserSession : IDisposable
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private User? _currentUser;
    private bool _isInitialized = false;
    private bool _disposed = false;

    // ... existing code ...

    public void Dispose()
    {
        if (!_disposed)
        {
            _currentUser = null;
            _isInitialized = false;
            OnChange = null; // Odpojit event handlery
            _disposed = true;
            Console.WriteLine("[UserSession] Disposed - cleaned up resources");
        }
    }
}
```

#### 3. **Blazor Circuit Lifecycle - Není monitorován**
**Problém:** Nemáme handler pro odpojení klienta (Circuit disposal).

**Riziko:**
- Neví se, kdy klient odešel
- Nelze uvolnit resources specifické pro daného uživatele

**Doporučení:** Přidat Circuit Handler:
```csharp
public class CircuitHandlerService : CircuitHandler
{
    private readonly ILogger<CircuitHandlerService> _logger;

    public CircuitHandlerService(ILogger<CircuitHandlerService> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Circuit {circuit.Id} connected");
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogWarning($"Circuit {circuit.Id} disconnected");
        // Zde můžete:
        // - Uvolnit resources
        // - Odhlásit uživatele z aktivních sessions
        // - Vyčistit cache
        return Task.CompletedTask;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Circuit {circuit.Id} opened");
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Circuit {circuit.Id} closed");
        return Task.CompletedTask;
    }
}

// V Program.cs:
builder.Services.AddScoped<CircuitHandler, CircuitHandlerService>();
```

---

### 🟡 STŘEDNÍ PRIORITA - Doporučené

#### 4. **EnableDetailedErrors = true v Production**
**Problém:** `EnableDetailedErrors = true` odhaluje stacktrace útočníkům.

**Riziko:**
- Information disclosure
- Útočník vidí strukturu kódu

**Doporučení:**
```csharp
options.EnableDetailedErrors = builder.Environment.IsDevelopment();
```

#### 5. **MaximumReceiveMessageSize = null - Neomezené**
**Problém:** Klient může poslat neomezeně velkou zprávu.

**Riziko:**
- DoS útok (odeslání obřích zpráv)
- Memory exhaustion
- Crash serveru

**Doporučení:**
```csharp
options.MaximumReceiveMessageSize = 32 * 1024; // 32 KB je rozumný limit pro vaši aplikaci
```

#### 6. **CORS AllowAnyOrigin - Nebezpečné**
**Problém:** ESP32 API má `AllowAnyOrigin()` - kdokoli může volat API.

**Riziko:**
- Cross-Origin attacks
- Neautorizovaný přístup k API

**Doporučení:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ESP32Policy", policy =>
    {
        policy.WithOrigins(
            "http://192.168.1.100", // IP vašeho ESP32
            "http://localhost:5109"  // Pro testování
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials(); // Pro autentizaci
    });
});
```

#### 7. **ProtectedSessionStorage - Session Fixation**
**Problém:** Session se nepřegeneruje po přihlášení.

**Riziko:**
- Session fixation attack
- Útočník může předat session ID oběti

**Doporučení:** Po přihlášení regenerovat session:
```csharp
public async Task SetUserAsync(User user)
{
    // Nejprve smaž starou session
    await _sessionStorage.DeleteAsync("currentUser");
    
    // Nastav novou
    _currentUser = user;
    await _sessionStorage.SetAsync("currentUser", user);
    
    Console.WriteLine($"[UserSession] New session created for: {user.Username}");
    NotifyStateChanged();
}
```

---

### 🟢 NÍZKÁ PRIORITA - Nice to have

#### 8. **Žádné rate limiting**
**Doporučení:** Přidat rate limiting pro API a SignalR:
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100; // 100 requestů za minutu
    });
});
```

#### 9. **Žádné connection tracking**
**Doporučení:** Tracovat aktivní spojení:
```csharp
public class ConnectionTracker
{
    private readonly ConcurrentDictionary<string, DateTime> _connections = new();

    public void AddConnection(string connectionId)
    {
        _connections.TryAdd(connectionId, DateTime.UtcNow);
    }

    public void RemoveConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public int ActiveConnections => _connections.Count;
}
```

#### 10. **Žádné log sanitization**
**Problém:** Logy obsahují plné User objekty (možná hesla?).

**Doporučení:**
```csharp
Console.WriteLine($"[UserSession] User set: {user.Username} (Role: {user.Role})");
// NIKDY nelogovat: user.Password, user.Email, atd.
```

---

## 📋 IMPLEMENTAČNÍ CHECKLIST

### Okamžitě (před produkcí):
- [ ] Nakonfigurovat SignalR timeouty
- [ ] Přidat Circuit Handler pro cleanup
- [ ] Implementovat UserSession.Dispose()
- [ ] Změnit EnableDetailedErrors na Development only
- [ ] Nastavit MaximumReceiveMessageSize limit
- [ ] Opravit CORS na konkrétní IP
- [ ] Session regeneration po loginu

### Před nasazením (týden):
- [ ] Přidat rate limiting
- [ ] Connection tracking
- [ ] Log sanitization audit
- [ ] Security testing (penetration test)

### Monitoring (dlouhodobě):
- [ ] Sledovat počet aktivních circuits
- [ ] Alert při Memory leak
- [ ] Log všechny failed connections
- [ ] Dashboard s WebSocket metrikami

---

## 🧪 TESTOVACÍ SCÉNÁŘE

### Test 1: Zombie Connection
```
1. Otevři aplikaci v prohlížeči
2. Přihlaš se
3. Zavři prohlížeč BEZ odhlášení (hard close)
4. Počkej 60 sekund
5. OČEKÁVÁNÍ: Server by měl detekovat timeout a uzavřít circuit
```

### Test 2: Reconnect po výpadku sítě
```
1. Otevři aplikaci
2. Přihlaš se
3. Vypni Wi-Fi na 30 sekund
4. Zapni Wi-Fi
5. OČEKÁVÁNÍ: Blazor by se měl automaticky reconnectnout
```

### Test 3: Multiple tabs
```
1. Otevři aplikaci ve 2 tabech
2. Přihlaš se v tabu 1
3. Refresh tabu 2
4. OČEKÁVÁNÍ: Tab 2 by měl vidět přihlášeného uživatele (díky ProtectedSessionStorage)
```

### Test 4: Memory leak test
```
1. Otevři aplikaci
2. Refreshuj stránku 100x rychle za sebou (F5)
3. OČEKÁVÁNÍ: Memory by neměla růst lineárně
```

---

## 📊 SOUČASNÝ STAV

| Kategorie | Stav | Poznámka |
|-----------|------|----------|
| **WebSocket Timeouts** | ❌ CHYBÍ | Výchozí hodnoty nejsou bezpečné |
| **Circuit Lifecycle** | ❌ CHYBÍ | Žádný cleanup handler |
| **Session Cleanup** | ⚠️ ČÁSTEČNÉ | UserSession není IDisposable |
| **CORS Security** | ❌ NEBEZPEČNÉ | AllowAnyOrigin |
| **Message Size Limit** | ❌ NEOMEZENÉ | DoS riziko |
| **Error Disclosure** | ❌ NEBEZPEČNÉ | Detaily v production |
| **Rate Limiting** | ❌ CHYBÍ | Flooding možný |
| **Connection Tracking** | ❌ CHYBÍ | Nevidíme aktivní sessions |

---

## 🎯 PRIORITNÍ AKCE (DO 24 HODIN)

1. **Přidat SignalR timeouty** ← NEJVYŠŠÍ PRIORITA
2. **Circuit Handler pro cleanup**
3. **Opravit CORS politiku**
4. **Limitovat message size**
5. **Development-only detailed errors**

Tyto změny zajistí základní bezpečnost a stabilitu WebSocket spojení.
