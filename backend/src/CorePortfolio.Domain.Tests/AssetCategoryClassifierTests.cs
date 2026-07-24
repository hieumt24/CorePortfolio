using CorePortfolio.Domain.Accounting;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public class AssetCategoryClassifierTests
{
    [Theory]
    [InlineData("Crypto")]
    [InlineData("Tiền điện tử")]
    [InlineData("Tiền mã hóa")]
    public void IsCrypto_MatchesEnglishAndVietnameseNames(string categoryName)
    {
        Assert.True(AssetCategoryClassifier.IsCrypto(categoryName));
    }

    [Theory]
    [InlineData("Stock")]
    [InlineData("Cổ phiếu")]
    [InlineData("Chứng khoán")]
    public void IsStock_MatchesEnglishAndVietnameseNames(string categoryName)
    {
        Assert.True(AssetCategoryClassifier.IsStock(categoryName));
    }

    [Theory]
    [InlineData("Fund")]
    [InlineData("Chứng chỉ quỹ")]
    [InlineData("CCQ / ETF")]
    public void IsFund_MatchesEnglishAndVietnameseNames(string categoryName)
    {
        Assert.True(AssetCategoryClassifier.IsFund(categoryName));
    }

    [Fact]
    public void IsStock_DoesNotMatchFundNameContainingSecurities()
    {
        Assert.False(AssetCategoryClassifier.IsStock("Quỹ đầu tư chứng khoán"));
    }
}
