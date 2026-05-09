using Auth.Domain.Common;
using Auth.Domain.Events;
using FluentAssertions;

namespace Auth.Domain.Tests;

public class BaseEntityTests
{
    private class TestEntity : BaseEntity
    {
        public TestEntity() { }
        public TestEntity(Guid id) : base(id) { }
    }

    [Fact]
    public void CreateEntity_ShouldGenerateNewId()
    {
        var entity = new TestEntity();

        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void CreateEntity_WithId_ShouldUseGivenId()
    {
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);

        entity.Id.Should().Be(id);
    }

    [Fact]
    public void DomainEvents_ShouldBeCollectible()
    {
        var entity = new TestEntity();
        var domainEvent = new UserCreatedEvent(Guid.NewGuid(), "test@test.com");

        entity.AddDomainEvent(domainEvent);

        entity.DomainEvents.Should().Contain(domainEvent);

        entity.RemoveDomainEvent(domainEvent);
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAll()
    {
        var entity = new TestEntity();
        entity.AddDomainEvent(new UserCreatedEvent(Guid.NewGuid(), "a@b.com"));
        entity.AddDomainEvent(new UserCreatedEvent(Guid.NewGuid(), "c@d.com"));

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty();
    }
}
