using Auth.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace Auth.Infrastructure.Tests.Services;

public class CacheServiceTests : IDisposable
{
    private readonly CacheService _service;
    private readonly IMemoryCache _memoryCache;

    public CacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _service = new CacheService(_memoryCache);
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    [Fact]
    public async Task SetAndGet_ShouldReturnValue()
    {
        var value = new TestDto { Id = 1, Name = "test" };

        await _service.SetAsync("key1", value);
        var result = await _service.GetAsync<TestDto>("key1");

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("test");
    }

    [Fact]
    public async Task Get_NonExistentKey_ShouldReturnNull()
    {
        var result = await _service.GetAsync<string>("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Remove_ShouldClearValue()
    {
        await _service.SetAsync("key2", "value");
        await _service.RemoveAsync("key2");

        var result = await _service.GetAsync<string>("key2");
        result.Should().BeNull();
    }

    [Fact]
    public async Task Exists_ShouldReturnTrue_WhenKeyExists()
    {
        await _service.SetAsync("key3", "value");

        var exists = await _service.ExistsAsync("key3");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Exists_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        var exists = await _service.ExistsAsync("nonexistent");
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task SetWithExpiration_ShouldExpire()
    {
        await _service.SetAsync("exp-key", "value", TimeSpan.FromMilliseconds(1));
        await Task.Delay(50);

        var result = await _service.GetAsync<string>("exp-key");
        result.Should().BeNull();
    }

    private class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
