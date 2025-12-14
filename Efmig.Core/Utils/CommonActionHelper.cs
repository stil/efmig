namespace Efmig.Core.Utils;

public class CommonActionOptions
{
    public required string ActionName { get; set; }
    public required string[] DotnetEfArgs { get; set; }
    public required Action<string>? DataCallback { get; set; }
}