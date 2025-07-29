using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Core.Native
{
    public class ZipProgress
    {
        public int TotalFiles { get; set; }
        public int CompletedFiles { get; set; }
        public string CurrentFileName { get; set; } = string.Empty;
        public double Percentage => TotalFiles == 0 ? 0 : (double)CompletedFiles / TotalFiles * 100;

        public override string ToString()
        {
            return $"解压进度: {CompletedFiles}/{TotalFiles} ({Percentage:F1}%) - {CurrentFileName}";
        }
    }

    public static class ZipExtractor
    {
        public static void ExtractWithProgress(string zipPath, string extractPath, IProgress<ZipProgress> progress)
        {
            if (!File.Exists(zipPath))
                throw new FileNotFoundException("ZIP 文件不存在", zipPath);

            // 确保目标目录存在
            Directory.CreateDirectory(extractPath);

            // 先统计文件总数
            int totalFiles;
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                totalFiles = archive.Entries.Count;
            }

            var zipProgress = new ZipProgress { TotalFiles = totalFiles };

            // 重新打开进行解压
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!string.IsNullOrEmpty(entry.Name)) // 忽略目录条目
                    {
                        var destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));

                        // 防止路径遍历攻击
                        if (!destinationPath.StartsWith(extractPath, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("ZIP 文件包含潜在的路径遍历攻击。");

                        var destinationDir = Path.GetDirectoryName(destinationPath);
                        if (!string.IsNullOrEmpty(destinationDir))
                            Directory.CreateDirectory(destinationDir);

                        using (var sourceStream = entry.Open())
                        using (var targetStream = File.Create(destinationPath))
                        {
                            sourceStream.CopyTo(targetStream);
                        }
                    }

                    // 更新进度
                    zipProgress.CompletedFiles++;
                    zipProgress.CurrentFileName = entry.FullName;
                    progress?.Report(zipProgress);
                }
            }
        }
    }
}
