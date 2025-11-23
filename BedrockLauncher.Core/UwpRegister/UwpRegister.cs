using System.Diagnostics.CodeAnalysis;
using Windows.Foundation;
using Windows.Management.Deployment;

namespace BedrockLauncher.Core.UwpRegister;

public class DeploymentOptionsConfig
{
	/// <summary>
	///     Gets or sets the file system path to the package.
	/// </summary>
	public string PackagePath { get; set; } = string.Empty;

	/// <summary>
	///     Gets or sets the options used to configure deployment behavior.
	/// </summary>
	public DeploymentOptions DeploymentOptions { get; set; }

	/// <summary>
	///     Gets or sets the cancellation token that is used to observe cancellation requests for the associated operation.
	/// </summary>
	/// <remarks>
	///     Assign a cancellation token to enable cooperative cancellation of the operation. If no token is
	///     provided, the operation cannot be cancelled through this property.
	/// </remarks>
	public CancellationToken CancellationToken { get; set; } = default;

	/// <summary>
	///     Progress callback
	/// </summary>
	public IProgress<DeploymentProgress>? ProgressCallback { get; set; }

	/// <summary>
	///     Gets or sets the maximum duration to wait before the operation times out.
	/// </summary>
	public TimeSpan? Timeout { get; set; }
}

public class UwpRegister
{
	/// <summary>
	///     Registers an appx package with flexible configuration
	/// </summary>
	/// <param name="config">Deployment configuration options</param>
	/// <returns>Deployment result containing operation status</returns>
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PackageManager))]
	public async Task<DeploymentResult> RegisterAppxAsync(DeploymentOptionsConfig config)
	{
		ValidateConfig(config);
		var manager = new PackageManager();
		var asyncOperation = manager.RegisterPackageAsync(
			new Uri(config.PackagePath),
			null,
			config.DeploymentOptions | DeploymentOptions.DevelopmentMode);

		return await ExecuteWithTimeout(asyncOperation, config);
	}

	/// <summary>
	///     Adds a framework appx package with flexible configuration
	/// </summary>
	/// <param name="config">Deployment configuration options</param>
	/// <returns>Deployment result containing operation status</returns>
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PackageManager))]
	public static async Task<DeploymentResult> AddAppxAsync(DeploymentOptionsConfig config)
	{
		ValidateConfig(config);

		var packageManager = new PackageManager();
		var asyncOperation = packageManager.AddPackageAsync(
			new Uri(config.PackagePath),
			null,
			config.DeploymentOptions);

		return await ExecuteWithTimeout(asyncOperation, config);
	}

	private static async Task<DeploymentResult> ExecuteWithTimeout(
		IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> asyncOperation,
		DeploymentOptionsConfig config)
	{
		if (config.Timeout.HasValue)
		{
			using var timeoutCts = new CancellationTokenSource(config.Timeout.Value);
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
				config.CancellationToken, timeoutCts.Token);

			return await asyncOperation.AsTask(linkedCts.Token, config.ProgressCallback);
		}

		return await asyncOperation.AsTask(config.CancellationToken, config.ProgressCallback);
	}

	private static void ValidateConfig(DeploymentOptionsConfig config)
	{
		if (string.IsNullOrWhiteSpace(config.PackagePath))
			throw new ArgumentException("Package path cannot be null or empty", nameof(config));

		if (!Uri.TryCreate(config.PackagePath, UriKind.Absolute, out _))
			throw new ArgumentException("Package path must be a valid absolute URI", nameof(config));
	}
}