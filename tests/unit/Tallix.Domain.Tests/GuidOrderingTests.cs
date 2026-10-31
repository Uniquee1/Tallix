namespace Tallix.Domain.Tests;

public class GuidOrderingTests
{
    // The ledger rules break ties by id. On the server that is Guid order, on the phone it is
    // a comparison of the id's text. They must agree on which id comes first.
    [Fact]
    public void GuidOrderMatchesTheOrdinalOrderOfTheText()
    {
        var random = new Random(12345);
        var bytes = new byte[16];

        for (var i = 0; i < 20_000; i++)
        {
            random.NextBytes(bytes);
            var a = new Guid(bytes);
            random.NextBytes(bytes);
            var b = new Guid(bytes);

            var byGuid = Math.Sign(a.CompareTo(b));
            var byText = Math.Sign(string.CompareOrdinal(a.ToString(), b.ToString()));

            Assert.Equal(byText, byGuid);
        }
    }
}