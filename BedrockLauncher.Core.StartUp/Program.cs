using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Management.Deployment;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.DependsComplete;
using BedrockLauncher.Core.GdkDecode;
using BedrockLauncher.Core.SoureGenerate;
using BedrockLauncher.Core.Utils;
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
			var bedrockCore = new BedrockCore();
			var options = new LocalGamePackageOptions()
			{
				FileFullPath = Path.GetFullPath(@"C:\Users\Administrator\AppData\Roaming\RoundStudio\BedrockBoot\Bedrock_Data\Appx\1.21.12020.appx"),
				Type = MinecraftBuildTypeVersion.UWP,
				GameTypeVersion = MinecraftGameTypeVersion.Preview,
				ExtractionProgress = (new Progress<DecompressProgress>((progress =>
				{
					double percentage = progress.TotalCount > 0
						? (double)progress.CurrentCount / progress.TotalCount * 100
						: 0;

					// 创建进度条
					int barWidth = 50;
					int progressBars = progress.TotalCount > 0
						? (int)((double)progress.CurrentCount / progress.TotalCount * barWidth)
						: 0;

					string progressBar = new string('█', progressBars) +
										 new string('░', barWidth - progressBars);

					// 打印进度信息
					Console.Write($"\r[{progressBar}] {percentage:F1}% | " +
								  $"{progress.CurrentCount:N0}/{progress.TotalCount:N0} | " +
								  $"{progress.FileName}");

					// 如果完成，换行
					if (progress.CurrentCount >= progress.TotalCount)
					{
						Console.WriteLine();
					}
				}))),
				InstallDstFolder = Path.GetFullPath("./rr4")
			};
			options.DeployProgress = new Progress<DeploymentProgress>((progress =>
			{
				Console.WriteLine(progress.percentage + progress.state.ToString());
			}));
			var installResult = bedrockCore.InstallPackageAsync(options).Result;
			Console.WriteLine(installResult.DeploymentResult?.IsRegistered);
			Console.WriteLine(installResult.DeploymentResult?.ErrorText);
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
