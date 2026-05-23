using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Management.Deployment;

namespace BedrockLauncher.Core.UwpRegister
{
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
        ///     Registers an unpacked appx package for the current user in developer mode (no admin required).
        /// </summary>
        /// <param name="config">Deployment configuration options</param>
        /// <returns>Deployment result containing operation status</returns>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PackageManager))]
        public static async Task<DeploymentResult> RegisterAppxAsync(DeploymentOptionsConfig config)
        {
            ValidateConfig(config);

            var packageUri = GetPackageUri(config.PackagePath);
            var packageFolderPath = GetPackageFolderPath(packageUri);

            RemoveSignatureFile(packageFolderPath);
            
            var manager = new PackageManager();
            var asyncOperation = manager.RegisterPackageAsync(
                packageUri,
                null,
                ToCurrentUserDevelopmentOptions(config.DeploymentOptions));

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

            var packageUri = GetPackageUri(config.PackagePath);
            var packageFolderPath = GetPackageFolderPath(packageUri);

            RemoveSignatureFile(packageFolderPath);

            var packageManager = new PackageManager();
            var asyncOperation = packageManager.AddPackageAsync(
                packageUri,
                null,
                ToCurrentUserDevelopmentOptions(config.DeploymentOptions));

            return await ExecuteWithTimeout(asyncOperation, config);
        }

        private static Uri GetPackageUri(string packagePath)
        {
            if (Uri.TryCreate(packagePath, UriKind.Absolute, out var uri) && uri.IsFile)
            {
                return uri;
            }

            return new Uri(Path.GetFullPath(packagePath));
        }

        private static string GetPackageFolderPath(Uri packageUri)
        {
            var localPath = packageUri.LocalPath;
            return File.Exists(localPath)
                ? Path.GetDirectoryName(localPath) ?? localPath
                : localPath;
        }

        private static DeploymentOptions ToCurrentUserDevelopmentOptions(DeploymentOptions options)
        {
            return options | DeploymentOptions.DevelopmentMode;
        }

        /// <summary>
        ///     Removes the signature file from the package folder
        /// </summary>
        /// <param name="packageFolderPath">Path to the unpacked package folder</param>
        private static void RemoveSignatureFile(string packageFolderPath)
        {
            try
            {
                string signaturePath = Path.Combine(packageFolderPath, "AppxSignature.p7x");
                if (File.Exists(signaturePath))
                {
                    File.Delete(signaturePath);
                }
            }
            catch (Exception ex)
            {
                // 记录错误但继续执行
                Console.WriteLine($"Error removing signature file: {ex.Message}");
            }
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
        
        public static bool CheckForPackageVersion(string packageName, string version)
        {
            PackageManager packageManager = new PackageManager();

            foreach (var package in packageManager.FindPackagesForUser(string.Empty))
            {
                if (package.Id.Name == packageName)
                {
                    var currentVersion = $"{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build}.{package.Id.Version.Revision}";

                    if (currentVersion == version)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        public static bool IsPackageInstalled(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentException("Package name cannot be null or empty", nameof(packageName));

            PackageManager packageManager = new PackageManager();

            foreach (var package in packageManager.FindPackagesForUser(string.Empty))
            {
                if (package.Id.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        
        private static void ValidateConfig(DeploymentOptionsConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.PackagePath))
                throw new ArgumentException("Package path cannot be null or empty", nameof(config));

            if (Uri.TryCreate(config.PackagePath, UriKind.Absolute, out var uri) && uri.IsFile)
                return;

            if (!Path.IsPathFullyQualified(config.PackagePath))
                throw new ArgumentException("Package path must be an absolute file path or file URI", nameof(config));
        }
    }
}