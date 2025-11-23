using System.Net;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using BedrockLauncher.Core.GdkDecode;
using BedrockLauncher.Core.SoureGenerate;
using BedrockLauncher.Core.VersionJsonHanle;

namespace BedrockLauncher.Core.StartUp
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var buildDatabaseAsync = VersionsHelper.GetBuildDatabaseAsync("https://data.mcappx.com/v2/bedrock.json").Result;
			var msiXvdStream = new MsiXVDStream("E:\\XvdStreaming\\MICROSOFT.MINECRAFTUWP_1.21.12302.0_x64__8wekyb3d8bbwe.msixvc", new CikKey());
			msiXvdStream.ParseFile();
			Console.WriteLine(msiXvdStream.Header.Volumes.HasFlag(MsiXVDVolumeAttributes.EncryptionDisabled));
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
