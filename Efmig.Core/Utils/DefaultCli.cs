using CliWrap;
using Efmig.Core.Abstractions;

namespace Efmig.Core.Utils;

public class DefaultCli : ICli
{
    public async Task RunCommand(ICommandConfiguration command)
    {
        if (command is Command cmd)
        {
            await cmd.ExecuteAsync();
        }
        else
        {
            throw new Exception("Command is not a CliWrap command.");
        }
    }
}