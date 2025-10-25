namespace iteration1.voting;

public static class HotRankingAlgorithm
{
    private static DateTime Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    
    private static double Score(ulong ups, ulong downs) => ups - downs;

    private static double EpochSeconds(DateTime date)
    {
        var td = date - Epoch;
        return td.TotalSeconds;
    }
    
    public static double IsHot(ulong ups, ulong downs, DateTime date)
    {
        double score = Score(ups, downs);
        double order = Math.Log(Math.Max(Math.Abs(score), 1), 10);
        int sign = score switch
        {
            > 0 => 1,
            < 0 => -1,
            _ => 0
        };
        double seconds = EpochSeconds(date) - 1134028003;
        return Math.Round(sign * order + seconds / 45000, 7);
    }
}

public static class ConfidenceRankingAlgorithm
{
    private static double ConfidenceInternal(ulong ups, ulong downs)
    {
        var n = ups + downs;

        if (n == 0)
        {
            return 0;
        }
        
        var z =  1.281551565545; // 80% confidence
        var p = (double)ups / n;
        
        var left = p + 1 / (2*n)*z*z;
        var right = z * Math.Sqrt(p*(1-p)/n + z*z/(4*n*n));
        var under = 1 + 1 /n*z*z;

        return (left - right) / under;
    }

    public static double Confidence(ulong ups, ulong downs)
    {
        if (ups + downs == 0)
        {
            return 0;
        }
        
        return ConfidenceInternal(ups, downs);
    }
}