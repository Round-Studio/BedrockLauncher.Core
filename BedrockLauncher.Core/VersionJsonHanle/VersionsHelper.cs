using System.Text.Json;
using BedrockLauncher.Core.SoureGenerate;

namespace BedrockLauncher.Core.VersionJsonHanle;

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
	public static async Task<BuildDatabase> GetBuildDatabaseAsync(string httpAddress,
		CancellationToken cancellationToken = new())
	{
		try
		{
			using (var client = new HttpClient())
			{
				var data = await client.GetStringAsync(httpAddress, cancellationToken);
				var builds = JsonSerializer.Deserialize(data, BuildDatabaseContext.Default.BuildDatabase);
				return builds;
			}
		}
		catch
		{
			throw new BedrockCoreException("Get BuildDataBase Error");
		}
	}
}