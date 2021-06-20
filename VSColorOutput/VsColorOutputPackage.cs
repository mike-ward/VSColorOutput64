using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using VSColorOutput.State;
using Task = System.Threading.Tasks.Task;

namespace VSColorOutput
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(VSColorOutputPackage.PackageGuidString)]
    [ProvideOptionPage(typeof(VsColorOutputOptionsDialog), VsColorOutputOptionsDialog.Category, VsColorOutputOptionsDialog.SubCategory, 1000, 1001, true)]
    [ProvideProfile(typeof(VsColorOutputOptionsDialog), VsColorOutputOptionsDialog.Category, VsColorOutputOptionsDialog.SubCategory, 1000, 1001, true)]
    [InstalledProductRegistration("VSColorOutput64", "Color output for build and debug windows - https://mike-ward.net/vscoloroutput", "2021.3")]
    public sealed class VSColorOutputPackage : AsyncPackage
    {
        public const string PackageGuidString = "65dd734b-180a-4c67-b245-56de889637e1";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }
    }
}