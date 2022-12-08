namespace UniqueRegionNamesPatcher.Extensions
{
    public static class NumberExtensions
    {
        public static decimal Scale(this decimal n, decimal oldMin, decimal oldMax, decimal newMin, decimal newMax)
        {
            return newMin + (n - oldMin) * (newMax - newMin) / (oldMax - oldMin);
        }
    }
}
