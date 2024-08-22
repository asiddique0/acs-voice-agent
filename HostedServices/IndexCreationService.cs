namespace LumenicBackend.HostedServices
{
    public class IndexCreationService : IHostedService
    {
        private readonly IRedisConnectionProvider _provider;
        public IndexCreationService(IRedisConnectionProvider provider)
        {
            _provider = provider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _provider.Connection.CreateIndexAsync(typeof(CustomOperationContext));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
