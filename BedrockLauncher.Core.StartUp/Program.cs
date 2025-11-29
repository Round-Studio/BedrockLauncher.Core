using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.DependsComplete;
using BedrockLauncher.Core.GdkDecode;
using BedrockLauncher.Core.SoureGenerate;
using BedrockLauncher.Core.VersionJsons;

namespace BedrockLauncher.Core.StartUp
{
	internal class Program
	{
		static void PrintDownloadProgress(DownloadProgress progress)
		{
			Console.WriteLine("下载进度信息:");
			Console.WriteLine($"进度: {progress.Progress:F1}%");
			Console.WriteLine($"已完成: {FormatBytes(progress.FinishedBytes)} / {FormatBytes(progress.TotalBytes)}");
			Console.WriteLine($"速度: {progress.Speed:F1} KB/s");
			Console.WriteLine($"阶段: {GetStageDescription(progress.Phase)}");

			// 绘制进度条（仅在下载阶段显示）
			if (progress.Phase == DownloadStage.Downloading)
			{
				DrawProgressBar(progress.Progress);
			}
		}

		static string FormatBytes(long bytes)
		{
			string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
			int counter = 0;
			double number = bytes;

			while (number >= 1024 && counter < suffixes.Length - 1)
			{
				number /= 1024;
				counter++;
			}

			return $"{number:F1} {suffixes[counter]}";
		}

		static string GetStageDescription(DownloadStage stage)
		{
			return stage switch
			{
				DownloadStage.Downloading => "下载中",
				DownloadStage.Merging => "合并中",
				DownloadStage.Merged => "已完成",
				_ => "未知"
			};
		}

		static void DrawProgressBar(double progress)
		{
			const int totalBlocks = 20;
			int filledBlocks = (int)(progress / 100 * totalBlocks);

			Console.Write("进度条: [");
			Console.Write(new string('█', filledBlocks));
			Console.Write(new string('░', totalBlocks - filledBlocks));
			Console.WriteLine($"] {progress:F1}%");
		}
		static void Main(string[] args)
		{
			//var buildDatabaseAsync = VersionsHelper.GetBuildDatabaseAsync("https://data.mcappx.com/v2/bedrock.json").Result;
			//var bedrockCore = new BedrockCore();
			//bedrockCore.Options.IsCheckMD5 = true;
			//var gameOnlinePackageOptions = new GameOnlinePackageOptions()
			//{
			//	SaveFilePath = Path.GetFullPath("./a.appx"),
			//	BuildInfo = buildDatabaseAsync.Builds["1.21.101"],
			//	DownloadProgress = (new Progress<DownloadProgress>((progress =>
			//	{
			//		PrintDownloadProgress(progress);
			//	}))),
			//	MaxRetryTimes = 2000
			//};
			//bedrockCore.GetGamePackage(gameOnlinePackageOptions).Wait();
			//Console.WriteLine(VersionHelper111.GetUri("14d05069-3d90-457b-a8e9-9381a5055705"));
			//var cikKey = new CikKey(File.ReadAllBytes(@"D:\Windows11\Download\1f49d63f-8bf5-1f8d-ed7e-dbd89477dad9.cik"));
			//var msiXvdDecoder = new MsiXVDDecoder(cikKey);
			//var msiXvdStream = new MsiXVDStream(@"D:\Windows11\Download\Microsoft.MinecraftWindowsBeta_1.21.13028.0_x64__8wekyb3d8bbwe.msixvc",msiXvdDecoder);
			//foreach (var encryptionKey in msiXvdStream.EncryptionKeys)
			//{
			//	Console.WriteLine(encryptionKey);
			//}

			//msiXvdStream.ExtractTaskAsync(Path.GetFullPath("./Test4"),new Progress<DecompressProgress>((progress =>
			//{
			//	// 计算百分比
			//	double percentage = progress.TotalCount > 0
			//		? (double)progress.CurrentCount / progress.TotalCount * 100
			//		: 0;

			//	// 创建进度条
			//	int barWidth = 50;
			//	int progressBars = progress.TotalCount > 0
			//		? (int)((double)progress.CurrentCount / progress.TotalCount * barWidth)
			//		: 0;

			//	string progressBar = new string('█', progressBars) +
			//	                     new string('░', barWidth - progressBars);

			//	// 打印进度信息
			//	Console.Write($"\r[{progressBar}] {percentage:F1}% | " +
			//	              $"{progress.CurrentCount:N0}/{progress.TotalCount:N0} | " +
			//	              $"{progress.FileName}");

			//	// 如果完成，换行
			//	if (progress.CurrentCount >= progress.TotalCount)
			//	{
			//		Console.WriteLine();
			//	}
			//})));
			//Console.ReadLine();
			//PrintBuildInfo(buildDatabaseAsync);
			//var downloader = new MultiThreadDownloader();
			//string url = "http://assets1.xboxlive.cn/12/66b02bc1-c4f1-4986-a183-c23e00cccecb/98bd2335-9b01-4e4c-bd05-ccc01614078b/1.21.12021.0.e5cfeb9c-2eaa-4959-8a49-e82cde29702a/Microsoft.MinecraftWindowsBeta_1.21.12021.0_x64__8wekyb3d8bbwe.msixvc";
			//var cts = new CancellationTokenSource();
			//var progress = new Progress<DownloadProgress>(p =>
			//{
			//	string stage = p.Phase switch
			//	{
			//		DownloadStage.Downloading => "Downloading",
			//		DownloadStage.Merging => "Merging",
			//		DownloadStage.Merged => "Merged",
			//		_ => "Unknown"
			//	};
			//	double completedMB = p.FinishedBytes / 1024.0 / 1024.0;
			//	double totalMB = p.TotalBytes / 1024.0 / 1024.0;
			//	double speedMB = p.Speed / 1024.0 / 1024.0;
			//	Console.WriteLine($"{stage} Progress: {(p.Progress * 100):F2}% " +
			//	                  $"Done: {completedMB:F2}MB/{totalMB:F2}MB " +
			//	                  $"Speed: {speedMB:F2} MB/s");
			//});

			//downloader.DownloadFileAsync(
			//	url,
			//	"a.a",
			//	threadCount: 4,
			//	progress: progress,
			//	cancellationToken: cts.Token,
			//	maxRetry: 5,
			//	retryDelayMs: 3000
			//);

			//while (true)
			//{
			//	var readLine = Console.ReadLine();
			//	if (readLine == "stop")
			//	{
			//		cts.Cancel();
			//	}

			//	if (readLine == "exit")
			//	{
			//		return;
			//	}
			//}
		}
		static void PrintBuildInfo(BuildDatabase buildData)
		{
			Console.WriteLine($"数据库创建时间: {buildData.CreationTime:yyyy-MM-dd HH:mm:ss}");
			Console.WriteLine($"包含 {buildData.Builds.Count} 个版本");
			Console.WriteLine();

			foreach (var (version, buildInfo) in buildData.Builds)
			{
				Console.WriteLine($"版本: {version}");
				Console.WriteLine($"  类型: {buildInfo.Type}");
				Console.WriteLine($"  构建类型: {buildInfo.BuildType}");
				Console.WriteLine($"  内部ID: {buildInfo.ID}");
				Console.WriteLine($"  发布日期: {buildInfo.Date}");
				Console.WriteLine($"  变体数量: {buildInfo.Variations.Count}");

				foreach (var variation in buildInfo.Variations)
				{
					Console.WriteLine($"    架构: {variation.Arch}");
					Console.WriteLine($"    归档状态: {variation.ArchivalStatus}");
					Console.WriteLine($"    系统要求: {variation.OSBuild}");
					Console.WriteLine($"    MD5: {variation.MD5}");
					Console.WriteLine($"    元数据: {string.Join(", ", variation.MetaData)}");
				}
				Console.WriteLine();
			}
			
		}
	}
}
