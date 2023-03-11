using System.Threading.Tasks;

namespace Efmig.Migrations.Actions;

public interface IAction
{
    public Task ExecuteAsync(ActionContext ctx);
}