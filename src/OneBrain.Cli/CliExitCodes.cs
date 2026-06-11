using OneBrain.Core.Recipes;

namespace OneBrain.Cli;

public static class CliExitCodes
{
    public const int Success = 0;
    public const int Failure = 1;

    public static int FromRecipeResult(RecipeRunResult result)
    {
        return result.Success ? Success : Failure;
    }
}
