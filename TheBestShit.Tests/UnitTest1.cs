using iteration1.voting;

namespace TheBestShit.Tests;

public class UnitTest1
{
    [Theory]
    [InlineData(1000, 0)]
    [InlineData(100000, 0)]
    [InlineData(20, 3)]
    public void Test1(ulong up, ulong down)
    {
        var score = ConfidenceRankingAlgorithm.Confidence(up, down); 
        Assert.Fail($"{score}");
    }
}