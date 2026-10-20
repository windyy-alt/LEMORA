using System;
using System.IO;

namespace LEMORA.Services
{
    public static class ImageStorageService
    {
        private static readonly string ImageFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LEMORA", "Images");

        public static string SavePhoto(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("The selected photo was not found.", sourcePath);

            string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" &&
                extension != ".png" && extension != ".bmp")
                throw new InvalidOperationException("Use a JPG, PNG, or BMP image.");

            if (new FileInfo(sourcePath).Length > 10 * 1024 * 1024)
                throw new InvalidOperationException("The photo must be 10 MB or smaller.");

            Directory.CreateDirectory(ImageFolder);

            string fullSourcePath = Path.GetFullPath(sourcePath);
            string? sourceDirectory = Path.GetDirectoryName(fullSourcePath);
            if (string.Equals(sourceDirectory, Path.GetFullPath(ImageFolder),
                              StringComparison.OrdinalIgnoreCase))
                return fullSourcePath;

            string fileName = $"{Guid.NewGuid():N}{extension}";
            string destination = Path.Combine(ImageFolder, fileName);
            File.Copy(fullSourcePath, destination);
            return destination;
        }
    }
}
