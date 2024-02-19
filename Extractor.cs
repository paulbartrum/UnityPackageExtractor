using System.Diagnostics.CodeAnalysis;
using System.Formats.Tar;
using System.IO.Compression;

/// <summary>
/// Extracts files from a .unitypackage file.
/// </summary>
public class Extractor
{
    /// <summary>
    /// The path to the .unitypackage file.
    /// </summary>
    public required string InputPath { get; init; }

    /// <summary>
    /// The directory to output to.
    /// </summary>
    public required string OutputDir { get; init; }

    /// <summary>
    /// Extracts a <i>.unitypackage</i> archive.
    /// </summary>
    public void Extract()
    {
        if (!InputPath.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The input file must end with .unitypackage");

        // Add the input file name to the output path.
        var outputDir = Path.Combine(Path.GetFullPath(OutputDir), Path.GetFileNameWithoutExtension(InputPath));
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);
        outputDir += Path.DirectorySeparatorChar;   // This is so SafeCombine can check the root correctly.

        Console.Write($"Extracting '{InputPath}' to '{outputDir}'... ");

        using var fileStream = File.OpenRead(InputPath);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var tarReader = new TarReader(gzipStream);
        using (var progress = new ProgressBar())
        {
            while (true)
            {
                // Read from the TAR file.
                var entry = tarReader.GetNextEntry();
                if (entry == null)
                    break;

                // Extract it.
                if (entry.EntryType == TarEntryType.RegularFile)
                    ProcessTarEntry(entry, outputDir);

                // Report progress.
                progress.Report((double)fileStream.Position / fileStream.Length);
            }
        }

        Console.WriteLine("done.");
    }

    private readonly Dictionary<string, string?> _guidToAssetPath = [];

    /// <summary>
    /// Processes a single file in the .unitypackage file.
    /// </summary>
    /// <param name="entry"> The TAR entry to process. </param>
    /// <param name="outputDir"> The output directory. </param>
    private void ProcessTarEntry(TarEntry entry, string outputDir)
    {
        string fileName = Path.GetFileName(entry.Name);
        string guid = VerifyNonNull(Path.GetDirectoryName(entry.Name));
        if (fileName == "asset")
        {
            string outputFilePath;
            if (_guidToAssetPath.TryGetValue(guid, out var assetPath))
            {
                // If we previously encountered a 'pathname' use that.
                VerifyNonNull(assetPath);
                outputFilePath = SafeCombine(outputDir, assetPath);
                CreatePathDirectoriesIfNecessary(outputFilePath);
            }
            else
            {
                // Extract the file and call it '<guid>'.
                _guidToAssetPath[guid] = null;
                outputFilePath = SafeCombine(outputDir, guid);
            }

            // Extract the file.
            entry.ExtractToFile(outputFilePath, overwrite: false);   
        }
        else if (fileName == "pathname")
        {
            VerifyNonNull(entry.DataStream);

            // [ascii path] e.g. Assets/Footstep Sounds/Water and Mud/Water Running 1_10.wav
            // ASCII line feed (0xA)
            // 00
            string assetPath = VerifyNonNull(new StreamReader(entry.DataStream, System.Text.Encoding.ASCII).ReadLine());
            if (_guidToAssetPath.TryGetValue(guid, out var existingAssetPath))
            {
                if (existingAssetPath != null)
                    throw new FormatException("The format of the file is invalid; is this a valid Unity package file?");
                assetPath = SafeCombine(outputDir, assetPath);
                CreatePathDirectoriesIfNecessary(assetPath);
                File.Move(SafeCombine(outputDir, guid), assetPath);
            }
            _guidToAssetPath[guid] = assetPath;
        }
    }

    /// <summary>
    /// Throws an exception if the value is null.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"> The value to check for null. </param>
    /// <returns></returns>
    private static T VerifyNonNull<T>([NotNull] T? value)
    {
        if (value == null)
            throw new FormatException("The format of the file is invalid; is this a valid Unity package file?");
        return value;
    }

    /// <summary>
    /// Acts like Path.Combine but checks the resulting path starts with <paramref name="rootDir"/>.
    /// </summary>
    /// <param name="rootDir"> The root directory. </param>
    /// <param name="relativePath"> The relative path to append. </param>
    /// <returns> The combined file path. </returns>
    private static string SafeCombine(string rootDir, string relativePath)
    {
        var result = Path.Combine(rootDir, relativePath);
        if (!result.StartsWith(rootDir, StringComparison.Ordinal))
            throw new InvalidOperationException($"Invalid path '{result}'; it should start with '{rootDir}'.");
        return result;
    }

    private readonly HashSet<string> _createdDirectories = [];

    /// <summary>
    /// Creates any directories in the given path, if they don't already exist.
    /// </summary>
    /// <param name="path"> The file path. </param>
    private void CreatePathDirectoriesIfNecessary(string path)
    {
        // Get the directory from the path.
        var dir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir))
            return;

        // Fast cache check.
        if (_createdDirectories.Contains(dir))
            return;

        // Create it if it doesn't exist.
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Add to cache.
        _createdDirectories.Add(dir);
    }
}