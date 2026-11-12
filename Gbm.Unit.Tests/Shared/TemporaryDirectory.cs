namespace Gbm.Unit.Tests.Shared;

public sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"gbm-tests-{Guid.NewGuid():N}");
    public TemporaryDirectory() => Directory.CreateDirectory(Path);
    public string File(string name) => System.IO.Path.Combine(Path, name);
    public void Dispose()
    {
        var full = System.IO.Path.GetFullPath(Path);
        var parent = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath()).TrimEnd(System.IO.Path.DirectorySeparatorChar);
        if (System.IO.Path.GetDirectoryName(full) != parent || !System.IO.Path.GetFileName(full).StartsWith("gbm-tests-", StringComparison.Ordinal))
            throw new InvalidOperationException("Unexpected temporary directory.");
        if (!Directory.Exists(full)) return;
        foreach (var file in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
            System.IO.File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(full, recursive: true);
    }
}
