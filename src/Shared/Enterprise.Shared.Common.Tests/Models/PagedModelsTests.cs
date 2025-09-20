using Enterprise.Shared.Common.Enums;
using Enterprise.Shared.Common.Models;
using FluentAssertions;

namespace Enterprise.Shared.Common.Tests.Models;

[TestFixture]
public class PagedModelsTests
{
    #region PagedRequest Tests

    [Test]
    public void PagedRequest_DefaultValues_AreCorrect()
    {
        // Act
        var request = new PagedRequest();

        // Assert
        request.Page.Should().Be(1);
        request.PageSize.Should().Be(10);
        request.SortDirection.Should().Be(SortDirection.Ascending);
        request.SortBy.Should().BeNull();
        request.Search.Should().BeNull();
        request.Filters.Should().BeEmpty();
        request.Skip.Should().Be(0);
        request.Take.Should().Be(10);
    }

    [Test]
    public void Skip_CalculatedCorrectly()
    {
        // Arrange
        var request = new PagedRequest { Page = 3, PageSize = 20 };

        // Act & Assert
        request.Skip.Should().Be(40); // (3-1) * 20
    }

    [Test]
    public void Take_ReturnsPageSize()
    {
        // Arrange
        var request = new PagedRequest { PageSize = 25 };

        // Act & Assert
        request.Take.Should().Be(25);
    }

    [Test]
    public void Normalize_FixesInvalidPageValues()
    {
        // Arrange
        var request = new PagedRequest 
        { 
            Page = 0, 
            PageSize = 0 
        };

        // Act
        request.Normalize();

        // Assert
        request.Page.Should().Be(1);
        request.PageSize.Should().Be(10);
    }

    [Test]
    public void Normalize_FixesTooLargePageSize()
    {
        // Arrange
        var request = new PagedRequest { PageSize = 150 };

        // Act
        request.Normalize();

        // Assert
        request.PageSize.Should().Be(100);
    }

    [Test]
    public void Normalize_TrimsAndClearsEmptySearch()
    {
        // Arrange
        var request = new PagedRequest 
        { 
            Search = "   ",
            SortBy = " field "
        };

        // Act
        request.Normalize();

        // Assert
        request.Search.Should().BeNull();
        request.SortBy.Should().Be("field");
    }

    [Test]
    public void AddFilter_AddsFilterCorrectly()
    {
        // Arrange
        var request = new PagedRequest();

        // Act
        request.AddFilter("status", "active");

        // Assert
        request.Filters.Should().ContainKey("status");
        request.Filters["status"].Should().Be("active");
    }

    [Test]
    public void RemoveFilter_RemovesFilterCorrectly()
    {
        // Arrange
        var request = new PagedRequest();
        request.AddFilter("status", "active");

        // Act
        request.RemoveFilter("status");

        // Assert
        request.Filters.Should().NotContainKey("status");
    }

    [Test]
    public void GetFilter_ReturnsCorrectValue()
    {
        // Arrange
        var request = new PagedRequest();
        request.AddFilter("count", 5);

        // Act
        var value = request.GetFilter<int>("count");

        // Assert
        value.Should().Be(5);
    }

    [Test]
    public void GetFilter_WithNonExistentKey_ReturnsDefaultValue()
    {
        // Arrange
        var request = new PagedRequest();

        // Act
        var value = request.GetFilter<string>("nonexistent", "default");

        // Assert
        value.Should().Be("default");
    }

    [Test]
    public void HasFilter_WithExistingFilter_ReturnsTrue()
    {
        // Arrange
        var request = new PagedRequest();
        request.AddFilter("key", "value");

        // Act & Assert
        request.HasFilter("key").Should().BeTrue();
        request.HasFilter("nonexistent").Should().BeFalse();
    }

    [Test]
    public void WithPage_CreatesNewInstanceWithDifferentPage()
    {
        // Arrange
        var request = new PagedRequest { Page = 1, PageSize = 20 };

        // Act
        var newRequest = request.WithPage(3);

        // Assert
        newRequest.Should().NotBeSameAs(request);
        newRequest.Page.Should().Be(3);
        newRequest.PageSize.Should().Be(20);
    }

    [Test]
    public void WithPageSize_CreatesNewInstanceWithDifferentPageSize()
    {
        // Arrange
        var request = new PagedRequest { Page = 2, PageSize = 10 };

        // Act
        var newRequest = request.WithPageSize(50);

        // Assert
        newRequest.Should().NotBeSameAs(request);
        newRequest.Page.Should().Be(2);
        newRequest.PageSize.Should().Be(50);
    }

    #endregion

    #region PagedResult Tests

    [Test]
    public void PagedResult_DefaultValues_AreCorrect()
    {
        // Act
        var result = new PagedResult<string>();

        // Assert
        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(0);
        result.PageSize.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Test]
    public void TotalPages_CalculatedCorrectly()
    {
        // Test cases: (totalCount, pageSize, expectedPages)
        var testCases = new[]
        {
            (0, 10, 0),
            (5, 10, 1),
            (10, 10, 1),
            (11, 10, 2),
            (50, 10, 5),
            (51, 10, 6)
        };

        foreach (var (totalCount, pageSize, expectedPages) in testCases)
        {
            // Arrange
            var result = new PagedResult<string>
            {
                TotalCount = totalCount,
                PageSize = pageSize
            };

            // Act & Assert
            result.TotalPages.Should().Be(expectedPages, 
                $"TotalCount: {totalCount}, PageSize: {pageSize} should result in {expectedPages} pages");
        }
    }

    [Test]
    public void HasNextPage_CalculatedCorrectly()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Page = 2,
            TotalCount = 25,
            PageSize = 10 // Total 3 pages
        };

        // Act & Assert
        result.HasNextPage.Should().BeTrue(); // Page 2 of 3

        result.Page = 3;
        result.HasNextPage.Should().BeFalse(); // Page 3 of 3 (last page)
    }

    [Test]
    public void HasPreviousPage_CalculatedCorrectly()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Page = 1,
            TotalCount = 25,
            PageSize = 10
        };

        // Act & Assert
        result.HasPreviousPage.Should().BeFalse(); // First page

        result.Page = 2;
        result.HasPreviousPage.Should().BeTrue(); // Not first page
    }

    [Test]
    public void NextPage_CalculatedCorrectly()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Page = 2,
            TotalCount = 30,
            PageSize = 10 // Total 3 pages
        };

        // Act & Assert
        result.NextPage.Should().Be(3); // Has next page

        result.Page = 3; // Last page
        result.NextPage.Should().BeNull(); // No next page
    }

    [Test]
    public void PreviousPage_CalculatedCorrectly()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Page = 2,
            TotalCount = 30,
            PageSize = 10
        };

        // Act & Assert
        result.PreviousPage.Should().Be(1); // Has previous page

        result.Page = 1; // First page
        result.PreviousPage.Should().BeNull(); // No previous page
    }

    [Test]
    public void StartItem_And_EndItem_CalculatedCorrectly()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Data = new List<string> { "item1", "item2", "item3", "item4", "item5" }, // 5 items on page 3
            Page = 3,
            PageSize = 10,
            TotalCount = 25
        };

        // Act & Assert
        result.StartItem.Should().Be(21); // (3-1)*10 + 1
        result.EndItem.Should().Be(25); // Min(3*10, 25)
    }

    [Test]
    public void StartItem_And_EndItem_WithEmptyResult()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 0
        };

        // Act & Assert
        result.StartItem.Should().Be(0);
        result.EndItem.Should().Be(0);
    }

    [Test]
    public void Create_StaticMethod_CreatesCorrectInstance()
    {
        // Arrange
        var data = new List<string> { "item1", "item2", "item3" };

        // Act
        var result = PagedResult<string>.Create(data, 100, 2, 10);

        // Assert
        result.Data.Should().BeEquivalentTo(data);
        result.TotalCount.Should().Be(100);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
    }

    [Test]
    public void Empty_StaticMethod_CreatesEmptyInstance()
    {
        // Act
        var result = PagedResult<string>.Empty(3, 20);

        // Assert
        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(3);
        result.PageSize.Should().Be(20);
        result.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Map_TransformsDataCorrectly()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3 };
        var result = PagedResult<int>.Create(data, 100, 1, 10);

        // Act
        var mappedResult = result.Map(x => x.ToString());

        // Assert
        mappedResult.Data.Should().BeEquivalentTo(new[] { "1", "2", "3" });
        mappedResult.TotalCount.Should().Be(100);
        mappedResult.Page.Should().Be(1);
        mappedResult.PageSize.Should().Be(10);
    }

    [Test]
    public void Filter_FiltersDataCorrectly()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5 };
        var result = PagedResult<int>.Create(data, 100, 1, 10);

        // Act
        var filteredResult = result.Filter(x => x % 2 == 0);

        // Assert
        filteredResult.Data.Should().BeEquivalentTo(new[] { 2, 4 });
        filteredResult.TotalCount.Should().Be(2); // Updated to filtered count
    }

    [Test]
    public void GetPageInfo_ReturnsCorrectInfo()
    {
        // Arrange
        var data = new List<string> { "a", "b", "c" };
        var result = PagedResult<string>.Create(data, 25, 2, 10);

        // Act
        var pageInfo = result.GetPageInfo();

        // Assert
        pageInfo.Should().Be("Showing 11-13 of 25 items (Page 2 of 3)");
    }

    [Test]
    public void GetPageInfo_WithEmptyResult_ReturnsNoItemsMessage()
    {
        // Arrange
        var result = PagedResult<string>.Empty();

        // Act
        var pageInfo = result.GetPageInfo();

        // Assert
        pageInfo.Should().Be("No items");
    }

    [Test]
    public void IsFirstPage_And_IsLastPage_Properties()
    {
        // Arrange
        var result = new PagedResult<string>
        {
            Page = 1,
            TotalCount = 30,
            PageSize = 10 // Total 3 pages
        };

        // Act & Assert
        result.IsFirstPage.Should().BeTrue();
        result.IsLastPage.Should().BeFalse();

        result.Page = 2;
        result.IsFirstPage.Should().BeFalse();
        result.IsLastPage.Should().BeFalse();

        result.Page = 3;
        result.IsFirstPage.Should().BeFalse();
        result.IsLastPage.Should().BeTrue();
    }

    [Test]
    public void ImplicitConversion_ToList_ReturnsData()
    {
        // Arrange
        var data = new List<string> { "a", "b", "c" };
        var result = PagedResult<string>.Create(data, 100, 1, 10);

        // Act
        List<string> list = result;

        // Assert
        list.Should().BeEquivalentTo(data);
    }

    [Test]
    public void ToList_ReturnsDataCopy()
    {
        // Arrange
        var data = new List<string> { "a", "b", "c" };
        var result = PagedResult<string>.Create(data, 100, 1, 10);

        // Act
        var list = result.ToList();

        // Assert
        list.Should().BeEquivalentTo(data);
        list.Should().NotBeSameAs(result.Data); // Should be a copy
    }

    #endregion
}