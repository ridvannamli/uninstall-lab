namespace UninstallLab.Core;

internal static class PathText
{
    /// <summary>Platformdan bağımsız son yol parçası ('\' ve '/' ayırıcıdır).</summary>
    public static string LastSegment(string path)
    {
        var i = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
        return i < 0 ? path : path[(i + 1)..];
    }
}
