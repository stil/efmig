using Efmig.Core.Actions;
using Efmig.Core.Utils;

namespace Efmig.Core.Abstractions;

public interface IDotNetEfTool
{
    Task RunDotnetEfTool(ActionContext ctx, CommonActionOptions commonActionOptions);
}