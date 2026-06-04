using FluentAssertions;
using RPG_ESI07.Application.Responses;

namespace RPG_ESI07.Tests.Application.Responses;

public class PaginatedResponseTests
{
    [Fact]
    public void Create_InitializesPropertiesCorrectly()
    {
        var items = new List<string> { "A", "B" };
        var pageNumber = 2;
        var pageSize = 10;
        var totalCount = 15;

        var response = PaginatedResponse<string>.Create(items, pageNumber, pageSize, totalCount);

        response.Items.Should().BeEquivalentTo(items);
        response.PageNumber.Should().Be(pageNumber);
        response.PageSize.Should().Be(pageSize);
        response.TotalCount.Should().Be(totalCount);
    }

    [Theory]
    [InlineData(10, 5, 2)] // Division exacte
    [InlineData(11, 5, 3)] // Reste positif, nécessite une page supplémentaire
    [InlineData(0, 5, 0)]  // Aucun élément
    [InlineData(4, 5, 1)]  // Moins d'éléments qu'une page complète
    public void TotalPages_CalculatesCorrectly(int totalCount, int pageSize, int expectedTotalPages)
    {
        var response = new PaginatedResponse<object>
        {
            PageSize = pageSize,
            TotalCount = totalCount
        };

        response.TotalPages.Should().Be(expectedTotalPages);
    }
}