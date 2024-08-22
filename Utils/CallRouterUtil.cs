namespace LumenicBackend.Utils
{
    public static class CallRouterUtil
    {
        private static readonly Random Random = new Random();

        public static bool DetermineIfTransferCall(double transferProbability)
        {
            return Random.NextDouble() <= transferProbability;
        }
    }
}
