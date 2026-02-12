using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BedrockLauncher.Core.SoureGenerate;

namespace BedrockLauncher.Core.VersionJsons;

public static class VersionsHelper
{
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