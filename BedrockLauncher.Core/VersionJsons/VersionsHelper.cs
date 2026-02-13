using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BedrockLauncher.Core.SoureGenerate;

namespace BedrockLauncher.Core.VersionJsons;

public static class VersionsHelper
{
	private static readonly object _lock = new object();
	public static string GetNextVersion(Version currentVersion)
	{
		lock (_lock)
		{

			long ticks = DateTime.Now.Ticks;
			int seed = (int)(ticks & 0xFFFFFFFF) ^ (int)(ticks >> 32) ^ Environment.TickCount;
			Random rand = new Random(seed);
			int sevenDigitNumber = rand.Next(1_000_000, 10_000_000);
			string sevenDigitStr = sevenDigitNumber.ToString();


			int[] positions = Enumerable.Range(0, 7).OrderBy(x => rand.Next()).Take(2).ToArray();
			Array.Sort(positions);
			int buildSuffix = int.Parse(sevenDigitStr[positions[0]].ToString() + sevenDigitStr[positions[1]]);


			string originalBuildStr = currentVersion.Build.ToString();

			string paddedBuild = originalBuildStr.PadRight(5, '0');
			string buildPrefix = paddedBuild.Substring(0, 3);


			string newBuildStr = buildPrefix + buildSuffix.ToString().PadLeft(2, '0');
			int newBuild = int.Parse(newBuildStr);

			if (newBuild > 65535) newBuild = newBuild % 65535;
			if (newBuild == 0) newBuild = 1;


			var remainingIndices = Enumerable.Range(0, 7).Except(positions).OrderBy(i => i).ToArray();
			string revisionStr = new string(remainingIndices.Select(i => sevenDigitStr[i]).ToArray());
			int revision = int.Parse(revisionStr);

			if (revision > 65535) revision = revision % 65535;
			if (revision == 0) revision = 1;


			return $"{currentVersion.Major}.{currentVersion.Minor}.{newBuild}.{revision}";
		}
	}
	/// <summary>
	///     Asynchronously retrieves and deserializes a build database from the specified HTTP address(e.g. mcappx).
	/// </summary>
	/// <param name="httpAddress">
	///     The URI of the HTTP endpoint from which to retrieve the build database. Must be a valid,
	///     accessible address.
	/// </param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the deserialized build database.</returns>
	/// <exception cref="BedrockCoreException">Thrown if an error occurs while retrieving or deserializing the build database.</exception>
	public static async Task<BuildDatabase?> GetBuildDatabaseAsync(string httpAddress,
		CancellationToken cancellationToken = new())
	{
		try
		{
			using (var client = new HttpClient())
			{
				// Add UserAgent.
				client.DefaultRequestHeaders.UserAgent.ParseAdd("mcappx_developer");

				var response = await client.GetAsync(
					httpAddress,
					HttpCompletionOption.ResponseHeadersRead,
					cancellationToken);

				response.EnsureSuccessStatusCode();

				await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

				var builds = await JsonSerializer.DeserializeAsync<BuildDatabase>(
					stream,
					BuildDatabaseContext.Default.BuildDatabase,
					cancellationToken);

				return builds;
			}
		}
		catch
		{
			throw new BedrockCoreException("Get BuildDataBase Error");
		}
	}
}