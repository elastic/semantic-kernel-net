using System.Threading.Tasks;

namespace Elastic.SemanticKernel.Playground;

internal sealed class Program
{
    public static async Task Main(string[] args)
    {
        _ = args;
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
