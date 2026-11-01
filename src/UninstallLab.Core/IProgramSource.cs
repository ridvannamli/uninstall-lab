namespace UninstallLab.Core;

/// <summary>Yüklü programların kaynağı (registry, test verisi vb.).</summary>
public interface IProgramSource
{
    IReadOnlyList<InstalledProgram> GetPrograms();
}
