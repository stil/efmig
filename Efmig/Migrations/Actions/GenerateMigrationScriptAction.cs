using System.Threading.Tasks;

namespace Efmig.Migrations.Actions;

public class GenerateMigrationScriptAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        ctx.LogInfo("Started operation: generate migration script.\r\n");
        
        var targetDir = await HelperProjectInitializer.CreateHelperProject(ctx.ConfigurationProfile);



        ;

        targetDir.Delete(true);

        ctx.LogInfo("Finished operation.");
    }
}