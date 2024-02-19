using System.CommandLine;
using System.CommandLine.Invocation;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var inputPathArg = new Argument<string>("inputPath", "The path to the .unitypackage file to extract.");
var outputDirArg = new Argument<string>("outputDir", () => ".", "The directory to extract to. A new directory with the same name as the input file (excluding the extension) will be created inside the specified directory.");
var rootCommand = new RootCommand(description: "Extracts the contents of a .unitypackage file into a new directory.")
{
    inputPathArg, outputDirArg
};
rootCommand.SetHandler((InvocationContext context) =>
{
    var extractor = new Extractor
    {
        InputPath = context.ParseResult.GetValueForArgument(inputPathArg),
        OutputDir = context.ParseResult.GetValueForArgument(outputDirArg)
    };
    try
    {
        extractor.Extract();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine();
        Console.Error.WriteLine($"ERROR: {ex.Message}");
        Console.ResetColor();
        context.ExitCode = 1;
    }
});
return rootCommand.Invoke(args);