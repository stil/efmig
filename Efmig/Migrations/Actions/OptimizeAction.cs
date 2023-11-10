using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Efmig.Migrations.Actions;

public class OptimizeAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        var stringBuilder = new StringBuilder();

        await CommonActionHelper.RunDotnetEfTool(ctx, new CommonActionOptions
        {
            ActionName = "optmize dbcontext",
            DataCallback = line => { stringBuilder.AppendLine(line); },
            DotnetEfArgs = new[]
            {
                "dbcontext",
                "optimize"
            }
        });

    }
}