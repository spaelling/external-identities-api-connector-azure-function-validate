using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sample.ExternalIdentities
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .Build();

            host.Run();
        }
    }
}
