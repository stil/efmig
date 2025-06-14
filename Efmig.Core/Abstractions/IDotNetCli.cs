using Efmig.Core.Actions;
using Efmig.Core.Utils;

namespace Efmig.Core.Abstractions;

public interface IDotNetCli
{
    Task RunDotnetEfTool(ActionContext ctx, CommonActionOptions commonActionOptions);
}