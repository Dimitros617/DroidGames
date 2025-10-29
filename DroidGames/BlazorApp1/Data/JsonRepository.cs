using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorApp1.Data;

public class JsonRepository<T> : IRepository<T> where T : class
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<T> _cache = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonRepository(string dataDirectory, string fileName)
    {
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, fileName);
        Console.WriteLine($"[DEBUG] JsonRepository<{typeof(T).Name}> ctor start. Path: {_filePath}");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
        
        // FIXED: Synchronous file loading to avoid deadlock in Blazor Server
        try
        {
            LoadSync();
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
            Console.WriteLine($"[DEBUG] JsonRepository<{typeof(T).Name}> reading {_filePath}");
            var json = File.ReadAllText(_filePath);
            _cache = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
            Console.WriteLine($"[DEBUG] JsonRepository<{typeof(T).Name}> loaded {_cache.Count} items");
        }
        else
        {
            _cache = new List<T>();
            Console.WriteLine($"[DEBUG] JsonRepository<{typeof(T).Name}> file missing, creating new store");
            var json = JsonSerializer.Serialize(_cache, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }
    }

    private async Task LoadAsync()
    {
        Console.WriteLine($"[DEBUG] JsonRepository<{typeof(T).Name}> LoadAsync start");
        await _lock.WaitAsync();
        try
        {
            if (File.Exists(_filePath))
            {
                Console.WriteLine($"[DEBUG] JsonRepository<{typeof(T).Name}> reading {_filePath}");
                var json = await File.ReadAllTextAsync(_filePath);
                _cache = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
                Console.WriteLine($"[DEBUG] JsonRepository<{typeof(T).Name}> loaded {_cache.Count} items");
            }
            else
            {
                _cache = new List<T>();
                Console.WriteLine($"[DEBUG] JsonRepository<{typeof(T).Name}> file missing, creating new store");
                await SaveAsync();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<T>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return new List<T>(_cache);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null) return null;

            return _cache.FirstOrDefault(item =>
            {
                var itemId = idProperty.GetValue(item)?.ToString();
                return itemId == id;
            });
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<T> AddAsync(T entity)
    {
        await _lock.WaitAsync();
        try
        {
            _cache.Add(entity);
            await SaveAsync();
            return entity;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<T> UpdateAsync(T entity)
    {
        await _lock.WaitAsync();
        try
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null) throw new InvalidOperationException("Entity must have Id property");

            var entityId = idProperty.GetValue(entity)?.ToString();
            var index = _cache.FindIndex(item =>
            {
                var itemId = idProperty.GetValue(item)?.ToString();
                return itemId == entityId;
            });

            if (index >= 0)
            {
                _cache[index] = entity;
                await SaveAsync();
            }

            return entity;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null) return false;

            var removed = _cache.RemoveAll(item =>
            {
                var itemId = idProperty.GetValue(item)?.ToString();
                return itemId == id;
            });

            if (removed > 0)
            {
                await SaveAsync();
                return true;
            }

            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync()
    {
        Console.WriteLine($"[DEBUG] JsonRepository<{typeof(T).Name}> SaveAsync writing {_cache.Count} items to {_filePath}");
        var json = JsonSerializer.Serialize(_cache, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
