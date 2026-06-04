using FluentAssertions;
using RPG_ESI07.Application.Specifications;

namespace RPG_ESI07.Tests.Application.Specifications;

public class SpecificationTests
{
    private class TestEntity
    { public int Id { get; set; } }

    private class TestSpecification : Specification<TestEntity>
    {
        public TestSpecification()
        {
            Criteria = x => x.Id > 0;
            AddInclude(x => x.Id);
            AddInclude("NavigationProperty");
            ApplyPaging(10, 20);
        }
    }

    [Fact]
    public void Specification_ConfiguresPropertiesCorrectly()
    {
        var spec = new TestSpecification();

        spec.Criteria.Should().NotBeNull();
        spec.Includes.Should().HaveCount(1);
        spec.IncludeStrings.Should().ContainSingle().Which.Should().Be("NavigationProperty");
        spec.IsPagingEnabled.Should().BeTrue();
        spec.Skip.Should().Be(10);
        spec.Take.Should().Be(20);
    }
}