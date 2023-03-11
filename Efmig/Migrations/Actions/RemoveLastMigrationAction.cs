using System.Threading.Tasks;

namespace Efmig.Migrations.Actions;

public class RemoveLastMigrationAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        ctx.LogInfo("Started operation: remove last migration.\r\n");
        
        var targetDir = await HelperProjectInitializer.CreateHelperProject(ctx.ConfigurationProfile);



        ;

        targetDir.Delete(true);

        ctx.LogInfo("Finished operation.");
    }
}