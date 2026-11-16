using Core.Reports;

namespace Core.Tests;

public class ReportServiceTests
{
    // 3) один тест на кількох наборах даних
    [Theory]
    [InlineData(10, 2)]
    [InlineData(1, 1)]
    [InlineData(0, 0)]
    public void LowStock_CountsOnlyBelowThreshold(int threshold, int expected) =>
        Assert.Equal(expected, ReportService.LowStock(TestData.Items(), threshold).Count);

    // 4) детермінований топ-N: у SKU-MID і SKU-TIE залишок однаковий, порядок вирішує SKU
    [Fact]
    public void TopByQuantity_IsDeterministic() =>
        Assert.Equal(
            new[] { "SKU-BIG", "SKU-MID", "SKU-TIE" },
            ReportService.TopByQuantity(TestData.Items(), 3).Select(l => l.Sku));

    [Fact]
    public void ByUnit_GroupsAndSums()
    {
        IReadOnlyList<GroupSummary> groups = ReportService.ByUnit(TestData.Items());

        Assert.Equal(new GroupSummary("шт", 2, 550), groups[0]);
        Assert.Equal(3, groups.Count);
    }

    [Fact]
    public void Totals_OnEmptySource_ReturnsZeros_InsteadOfThrowing() =>
        Assert.Equal((0, 0, 0d, 0), ReportService.Totals([]));

    [Fact]
    public void Totals_ComputesSumAverageAndMax()
    {
        var totals = ReportService.Totals(TestData.Items());

        Assert.Equal(5, totals.Items);
        Assert.Equal(605, totals.Total);
        Assert.Equal(121d, totals.Average);
        Assert.Equal(500, totals.Max);
    }
}
