using CliWrap;

namespace Efmig.Core.Abstractions;

public interface ICli
{
    Task RunCommand(ICommandConfiguration command);
}