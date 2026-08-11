using BookStore.Application.Features.Backup.Services;

namespace BookStore.Infrastructure.Backup.Services;

public sealed class DiskSpaceService : IDiskSpaceService
{
    public bool HasEnoughSpace(string folderPath, long requiredBytes) => GetAvailableBytes(folderPath) >= requiredBytes;

    public long GetAvailableBytes(string folderPath)
    {
        Directory.CreateDirectory(folderPath);
        var root = Path.GetPathRoot(Path.GetFullPath(folderPath));
        if (string.IsNullOrWhiteSpace(root))
        {
            return 0;
        }

        return new DriveInfo(root).AvailableFreeSpace;
    }
}
