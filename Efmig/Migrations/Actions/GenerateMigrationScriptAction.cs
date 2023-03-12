using System.Text;
using System.Threading.Tasks;
using Efmig.ViewModels;
using Efmig.Views;

namespace Efmig.Migrations.Actions;

public class GenerateMigrationScriptAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        var stringBuilder = new StringBuilder();

        await CommonActionHelper.RunDotnetEfTool(ctx, new CommonActionOptions
        {
            ActionName = "remove last migration",
            DataCallback = line => { stringBuilder.AppendLine(line); },
            DotnetEfArgs = new[]
            {
                "migrations",
                "script"
            }
        });
        var scriptWindow = new MigrationScriptWindow();
        var scriptViewModel = new MigrationScriptViewModel
        {
            Script = stringBuilder.ToString()
        };
        scriptWindow.DataContext = scriptViewModel;

        scriptWindow.Show();
    }
}