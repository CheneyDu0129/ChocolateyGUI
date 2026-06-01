// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Chocolatey" file="PackageViewModel.cs">
//   Copyright 2017 - Present Chocolatey Software, LLC
//   Copyright 2014 - 2017 Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Caliburn.Micro;
using chocolatey;
using ChocolateyGui.Common.Base;
using ChocolateyGui.Common.Constants;
using ChocolateyGui.Common.Models;
using ChocolateyGui.Common.Models.Messages;
using ChocolateyGui.Common.Properties;
using ChocolateyGui.Common.Services;
using ChocolateyGui.Common.ViewModels.Items;
using ChocolateyGui.Common.Windows.Services;
using ChocolateyGui.Common.Windows.Views;
using MahApps.Metro.Controls.Dialogs;
using NuGet.Versioning;
using Action = System.Action;
using MemoryCache = System.Runtime.Caching.MemoryCache;
using Application = System.Windows.Application;

namespace ChocolateyGui.Common.Windows.ViewModels.Items
{
    [DebuggerDisplay("Id = {Id}, Version = {Version}")]
    public class PackageViewModel :
        ObservableBase,
        IPackageViewModel,
        IHandle<PackageOutdatedMessage>,
        IHandle<FeatureModifiedMessage>,
        IHandle<PackageUpgradedMessage>,
        IHandle<PackageUninstalledMessage>,
        IHandle<PackageInstalledMessage>,
        IHandle<PackagePinnedMessage>,
        IHandle<PackageUnpinnedMessage>
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.ForContext<PackageViewModel>();

        private readonly MemoryCache _cache = MemoryCache.Default;

        private readonly IChocolateyService _chocolateyService;
        private readonly IEventAggregator _eventAggregator;

        private readonly IMapper _mapper;
        private readonly IDialogService _dialogService;
        private readonly IProgressService _progressService;

        private readonly IChocolateyGuiCacheService _chocolateyGuiCacheService;
        private readonly IConfigService _configService;
        private readonly IAllowedCommandsService _allowedCommandsService;
        private readonly IPackageArgumentsService _packageArgumentsService;
        private readonly IPersistenceService _persistenceService;

        private string[] _authors;

        private string _copyright;

        private DateTime _created;

        private string _dependencies;

        private string _description;

        private long _downloadCount;

        private string _galleryDetailsUrl;

        private string _iconUrl = string.Empty;

        private string _id;

        private bool _isOutdated;

        private bool _isInstalled;

        private bool _isPinned;

        private bool _isPrerelease;

        private string _language;

        private DateTime _lastUpdated;

        private NuGetVersion _latestVersion;

        private NuGetVersion _remoteVersion;

        private string _licenseUrl = string.Empty;

        private string[] _owners;

        private string _packageHash;

        private string _packageHashAlgorithm;

        private long _packageSize;

        private string _projectUrl = string.Empty;

        private DateTimeOffset _published;

        private string _releaseNotes;

        private string _reportAbuseUrl = string.Empty;

        private string _requireLicenseAcceptance;

        private Uri _source;

        private Models.ChocolateySource _chocolateySource;

        private string _summary;

        private string _tags;

        private string _title;

        private NuGetVersion _version;

        private long _versionDownloadCount;
        private Utilities.NotifyTaskCompletion<ObservableCollection<string>> _availableVersions;
        private string _selectedVersion;
        private bool _availableVersionsLoaded;
        private bool _availableVersionsLoading;
        private bool _includePreRelease = true;
        private bool _isSourceAvailable = true;
        private string _sourceRefreshError;
        private bool _isRefreshingSelectedVersionDetails;

        public PackageViewModel(
            IChocolateyService chocolateyService,
            IEventAggregator eventAggregator,
            IMapper mapper,
            IDialogService dialogService,
            IProgressService progressService,
            IChocolateyGuiCacheService chocolateyGuiCacheService,
            IConfigService configService,
            IAllowedCommandsService allowedCommandsService,
            IPackageArgumentsService packageArgumentsService,
            IPersistenceService persistenceService)
        {
            _chocolateyService = chocolateyService;
            _eventAggregator = eventAggregator;
            _mapper = mapper;
            _dialogService = dialogService;
            _progressService = progressService;
            eventAggregator?.Subscribe(this);
            _chocolateyGuiCacheService = chocolateyGuiCacheService;
            _configService = configService;
            _allowedCommandsService = allowedCommandsService;
            _packageArgumentsService = packageArgumentsService;
            _persistenceService = persistenceService;
        }

        public DateTime Created
        {
            get { return _created; }
            set { SetPropertyValue(ref _created, value); }
        }

        public DateTime LastUpdated
        {
            get { return _lastUpdated; }
            set { SetPropertyValue(ref _lastUpdated, value); }
        }

        public string[] Authors
        {
            get { return _authors; }
            set { SetPropertyValue(ref _authors, value); }
        }

        public bool CanInstall => !IsInstalled;

        public bool IsInstallAllowed => _allowedCommandsService.IsInstallCommandAllowed && IsSourceAvailable;

        public bool CanReinstall => IsInstalled && !IsPinned;

        public bool IsReinstallAllowed => _allowedCommandsService.IsInstallCommandAllowed && IsSourceAvailable;

        public bool CanUninstall => IsInstalled && !IsPinned;

        public bool IsUninstallAllowed => _allowedCommandsService.IsUninstallCommandAllowed;

        public bool CanUpdate => IsInstalled && !IsPinned && IsOutdated;

        public bool IsUpgradeAllowed => _allowedCommandsService.IsUpgradeCommandAllowed;

        public bool CanPin => !IsPinned && IsInstalled;

        public bool IsPinAllowed => _allowedCommandsService.IsPinCommandAllowed;

        public bool CanUnpin => IsPinned && IsInstalled;

        public bool IsUnpinAllowed => _allowedCommandsService.IsPinCommandAllowed;

        public string Copyright
        {
            get { return _copyright; }
            set { SetPropertyValue(ref _copyright, value); }
        }

        public string Dependencies
        {
            get { return _dependencies; }
            set { SetPropertyValue(ref _dependencies, value); }
        }

        public string Description
        {
            get { return _description; }
            set { SetPropertyValue(ref _description, value); }
        }

        public long DownloadCount
        {
            get { return _downloadCount; }
            set { SetPropertyValue(ref _downloadCount, value); }
        }

        public string GalleryDetailsUrl
        {
            get { return _galleryDetailsUrl; }
            set { SetPropertyValue(ref _galleryDetailsUrl, value); }
        }

        public string IconUrl
        {
            get { return _iconUrl; }
            set { SetPropertyValue(ref _iconUrl, value); }
        }

        public string Id
        {
            get { return _id; }
            set { SetPropertyValue(ref _id, value); }
        }

        public string LowerCaseId
        {
            get { return Id.ToLowerInvariant(); }
        }

        public bool IsInstalled
        {
            get
            {
                return _isInstalled;
            }

            set
            {
                if (SetPropertyValue(ref _isInstalled, value))
                {
                    NotifyPropertyChanged(nameof(CanUpdate));
                    NotifyPropertyChanged(nameof(InstalledBadgeText));
                }
            }
        }

        public bool IsPinned
        {
            get
            {
                return _isPinned;
            }

            set
            {
                if (SetPropertyValue(ref _isPinned, value))
                {
                    NotifyPropertyChanged(nameof(CanUpdate));
                }
            }
        }

        public bool IsOutdated
        {
            get
            {
                return _isOutdated;
            }

            set
            {
                if (SetPropertyValue(ref _isOutdated, value))
                {
                    NotifyPropertyChanged(nameof(CanUpdate));
                }
            }
        }

        public bool IsPrerelease
        {
            get { return _isPrerelease; }
            set { SetPropertyValue(ref _isPrerelease, value); }
        }

        public string Language
        {
            get { return _language; }
            set { SetPropertyValue(ref _language, value); }
        }

        public NuGetVersion LatestVersion
        {
            get { return _latestVersion; }
            set { SetPropertyValue(ref _latestVersion, value); }
        }

        public NuGetVersion RemoteVersion
        {
            get { return _remoteVersion; }
            set { SetPropertyValue(ref _remoteVersion, value); }
        }

        public string LicenseUrl
        {
            get { return _licenseUrl; }
            set { SetPropertyValue(ref _licenseUrl, value); }
        }

        public string[] Owners
        {
            get { return _owners; }
            set { SetPropertyValue(ref _owners, value); }
        }

        public string PackageHash
        {
            get { return _packageHash; }
            set { SetPropertyValue(ref _packageHash, value); }
        }

        public string PackageHashAlgorithm
        {
            get { return _packageHashAlgorithm; }
            set { SetPropertyValue(ref _packageHashAlgorithm, value); }
        }

        public long PackageSize
        {
            get { return _packageSize; }
            set { SetPropertyValue(ref _packageSize, value); }
        }

        public string ProjectUrl
        {
            get { return _projectUrl; }
            set { SetPropertyValue(ref _projectUrl, value); }
        }

        public DateTimeOffset Published
        {
            get { return _published; }
            set { SetPropertyValue(ref _published, value); }
        }

        public string ReleaseNotes
        {
            get { return _releaseNotes; }
            set { SetPropertyValue(ref _releaseNotes, value); }
        }

        public string ReportAbuseUrl
        {
            get { return _reportAbuseUrl; }
            set { SetPropertyValue(ref _reportAbuseUrl, value); }
        }

        public string RequireLicenseAcceptance
        {
            get { return _requireLicenseAcceptance; }
            set { SetPropertyValue(ref _requireLicenseAcceptance, value); }
        }

        public Uri Source
        {
            get { return _source; }
            set { SetPropertyValue(ref _source, value); }
        }

        public Models.ChocolateySource ChocolateySource
        {
            get { return _chocolateySource; }
            set { SetPropertyValue(ref _chocolateySource, value); }
        }

        public string Summary
        {
            get { return _summary; }
            set { SetPropertyValue(ref _summary, value); }
        }

        public string Tags
        {
            get { return _tags; }
            set { SetPropertyValue(ref _tags, value); }
        }

        public string Title
        {
            get { return string.IsNullOrWhiteSpace(_title) ? Id : _title; }
            set { SetPropertyValue(ref _title, value); }
        }

        public NuGetVersion Version
        {
            get { return _version; }
            set
            {
                if (SetPropertyValue(ref _version, value) && string.IsNullOrWhiteSpace(_selectedVersion) && value != null)
                {
                    SelectedVersion = value.ToNormalizedStringChecked();
                }

                NotifyPropertyChanged(nameof(InstalledBadgeText));
            }
        }

        public Utilities.NotifyTaskCompletion<ObservableCollection<string>> AvailableVersions
        {
            get { return _availableVersions; }
            set { SetPropertyValue(ref _availableVersions, value); }
        }

        public string SelectedVersion
        {
            get { return _selectedVersion; }
            set
            {
                if (SetPropertyValue(ref _selectedVersion, value))
                {
                    NotifyPropertyChanged(nameof(InstalledBadgeText));
                    RefreshSelectedVersionDetails();
                }
            }
        }

        public string InstalledBadgeText
        {
            get
            {
                var installedText = L(nameof(Resources.PackagesView_Installed));
                var installedVersion = Version?.ToNormalizedStringChecked();

                if (!IsInstalled || string.IsNullOrWhiteSpace(installedVersion) || string.IsNullOrWhiteSpace(SelectedVersion))
                {
                    return installedText;
                }

                if (string.Equals(SelectedVersion, installedVersion, StringComparison.OrdinalIgnoreCase))
                {
                    return installedText;
                }

                return $"{installedText}: {installedVersion}";
            }
        }

        public bool IncludePreRelease
        {
            get { return _includePreRelease; }
            set
            {
                if (SetPropertyValue(ref _includePreRelease, value))
                {
                    EnsureAvailableVersionsLoaded(true);
                }
            }
        }

        public bool IsSourceAvailable
        {
            get { return _isSourceAvailable; }
            private set
            {
                if (SetPropertyValue(ref _isSourceAvailable, value))
                {
                    NotifyPropertyChanged(nameof(IsInstallAllowed));
                    NotifyPropertyChanged(nameof(IsReinstallAllowed));
                }
            }
        }

        public string SourceRefreshError
        {
            get { return _sourceRefreshError; }
            private set { SetPropertyValue(ref _sourceRefreshError, value); }
        }

        public bool IsRefreshingSelectedVersionDetails
        {
            get { return _isRefreshingSelectedVersionDetails; }
            private set { SetPropertyValue(ref _isRefreshingSelectedVersionDetails, value); }
        }

        public long VersionDownloadCount
        {
            get { return _versionDownloadCount; }
            set { SetPropertyValue(ref _versionDownloadCount, value); }
        }

        public bool IsDownloadCountAvailable
        {
            get
            {
                return DownloadCount != -1 && !(_configService.GetEffectiveConfiguration().HidePackageDownloadCount ?? false);
            }
        }

        public bool IsPackageSizeAvailable
        {
            get { return PackageSize != -1; }
        }

        public async Task ShowArguments()
        {
            // TODO: Add legacy handling for packages installed prior to v2.0.0.
            var decryptedArguments = _packageArgumentsService.DecryptPackageArgumentsFile(Id, Version.ToNormalizedStringChecked()).ToList();

            if (decryptedArguments.Count == 0)
            {
                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.PackageViewModel_ArgumentsForPackageFormat), Title),
                    L(nameof(Resources.PackageViewModel_NoArgumentsAvailableForPackage)));
            }
            else
            {
                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.PackageViewModel_ArgumentsForPackageFormat), Title),
                    string.Join(Environment.NewLine, decryptedArguments));
            }
        }

        public async Task Install()
        {
            if (!IsSourceAvailable)
            {
                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.RemoteSourceViewModel_FailedToLoad)),
                    SourceRefreshError ?? L(nameof(Resources.RemoteSourceViewModel_UnableToConnectToFeed), Source));
                return;
            }

            await InstallPackage(GetInstallVersion());
        }

        public async Task InstallAdvanced()
        {
            if (!IsSourceAvailable)
            {
                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.RemoteSourceViewModel_FailedToLoad)),
                    SourceRefreshError ?? L(nameof(Resources.RemoteSourceViewModel_UnableToConnectToFeed), Source));
                return;
            }

            var dataContext = new AdvancedInstallViewModel(_chocolateyService, _persistenceService, Id, Version, Source);

            var result = await _dialogService.ShowChildWindowAsync<AdvancedInstallViewModel, AdvancedInstallViewModel>(
                L(nameof(Resources.AdvancedChocolateyDialog_Title_Install)),
                new AdvancedInstallView { DataContext = dataContext },
                dataContext);

            // null means that the Cancel button was clicked
            if (result != null)
            {
                if (string.Equals(result.SelectedVersion, Resources.AdvancedChocolateyDialog_LatestVersion, StringComparison.OrdinalIgnoreCase))
                {
                    result.SelectedVersion = null;
                }

                var advancedOptions = _mapper.Map<AdvancedInstall>(result);

                await InstallPackage(result.SelectedVersion, advancedOptions);
            }
        }

        public async Task Reinstall()
        {
            if (IsSelfPackageUpgrade())
            {
                await TriggerSelfReinstall();
                return;
            }

            if (!IsSourceAvailable)
            {
                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.RemoteSourceViewModel_FailedToLoad)),
                    SourceRefreshError ?? L(nameof(Resources.RemoteSourceViewModel_UnableToConnectToFeed), Source));
                return;
            }

            try
            {
                var confirmationResult = MessageDialogResult.Affirmative;
                if (!_configService.GetEffectiveConfiguration().SkipModalDialogConfirmation.GetValueOrDefault(false))
                {
                    confirmationResult = await _dialogService.ShowConfirmationMessageAsync(
                        L(nameof(Resources.Dialog_AreYouSureTitle)),
                        L(nameof(Resources.Dialog_AreYouSureReinstallMessage), Id));
                }

                if (confirmationResult == MessageDialogResult.Affirmative)
                {
                    var reinstallVersion = GetInstallVersion();

                    using (await StartProgressDialog(L(nameof(Resources.PackageViewModel_ReinstallingPackage)), L(nameof(Resources.PackageViewModel_ReinstallingPackage)), Id))
                    {
                        await _chocolateyService.InstallPackage(Id, reinstallVersion, Source, true);

                        // We need to clear out all files, as there may be incorrect information cached for
                        // all the sources.
                        _chocolateyGuiCacheService.PurgeOutdatedPackages(source: null, includePrerelease: false);
                        await _eventAggregator.PublishOnUIThreadAsync(new PackageInstalledMessage(Id, Version, ChocolateySource));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ran into an error while reinstalling {Id}, version {Version}.", Id, Version);
                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.PackageViewModel_FailedToReinstall)),
                    L(nameof(Resources.PackageViewModel_RanIntoInstallError), Id, ex.Message));
            }
        }

        public async Task Uninstall()
        {
            try
            {
                var confirmationResult = MessageDialogResult.Affirmative;
                if (!_configService.GetEffectiveConfiguration().SkipModalDialogConfirmation.GetValueOrDefault(false))
                {
                    confirmationResult = await _dialogService.ShowConfirmationMessageAsync(
                    L(nameof(Resources.Dialog_AreYouSureTitle)),
                    L(nameof(Resources.Dialog_AreYouSureUninstallMessage), Id));
                }

                if (confirmationResult == MessageDialogResult.Affirmative)
                {
                    if (IsSelfPackageUpgrade())
                    {
                        await TriggerSelfUninstall();
                        return;
                    }

                    using (await StartProgressDialog(L(nameof(Resources.PackageViewModel_UninstallingPackage)), L(nameof(Resources.PackageViewModel_UninstallingPackage)), Id))
                    {
                        var result = await _chocolateyService.UninstallPackage(Id, Version.ToNormalizedStringChecked(), true);

                        if (!result.Successful)
                        {
                            var exceptionMessage = result.Exception == null
                                ? string.Empty
                                : L(nameof(Resources.ChocolateyRemotePackageService_ExceptionFormat), result.Exception);

                            var message = L(
                                nameof(Resources.ChocolateyRemotePackageService_UninstallFailedMessage),
                                Id,
                                Version,
                                string.Join("\n", result.Messages),
                                exceptionMessage);

                            await _dialogService.ShowMessageAsync(
                                L(nameof(Resources.ChocolateyRemotePackageService_UninstallFailedTitle)),
                                message);

                            Logger.Warning(result.Exception, "Failed to uninstall {Package}, version {Version}. Errors: {Errors}", Id, Version, result.Messages);

                            return;
                        }

                        IsInstalled = false;
                        await _eventAggregator.PublishOnUIThreadAsync(new PackageUninstalledMessage(Id, Version, ChocolateySource));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ran into an error while uninstalling {Id}, version {Version}.", Id, Version);

                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.PackageViewModel_FailedToUninstall)),
                    L(nameof(Resources.PackageViewModel_RanIntoUninstallError), Id, ex.Message));
            }
        }

        public async Task Update()
        {
            try
            {
                if (IsSelfPackageUpgrade())
                {
                    await TriggerSelfUpgrade();
                    return;
                }

                using (await StartProgressDialog(L(nameof(Resources.PackageViewModel_UpdatingPackage)), L(nameof(Resources.PackageViewModel_UpdatingPackage)), Id))
                {
                    var result = await _chocolateyService.UpdatePackage(Id, LatestVersion.ToNormalizedStringChecked(), Source);

                    if (!result.Successful)
                    {
                        var exceptionMessage = result.Exception == null
                            ? string.Empty
                            : L(nameof(Resources.ChocolateyRemotePackageService_ExceptionFormat), result.Exception);

                        var message = L(
                            nameof(Resources.ChocolateyRemotePackageService_UpdateFailedMessage),
                            Id,
                            string.Join("\n", result.Messages),
                            exceptionMessage);

                        await _dialogService.ShowMessageAsync(
                            L(nameof(Resources.ChocolateyRemotePackageService_UpdateFailedTitle)),
                            message);

                        Logger.Warning(result.Exception, "Failed to update {Package}. Errors: {Errors}", Id, result.Messages);

                        return;
                    }

                    IsOutdated = false;
                    Version = LatestVersion;

                    // We need to clear out all files, as there may be incorrect information cached for 
                    // all the sources.
                    _chocolateyGuiCacheService.PurgeOutdatedPackages(source: null, includePrerelease: false);
                    await _eventAggregator.PublishOnUIThreadAsync(new PackageUpgradedMessage(Id, Version, source: ChocolateySource));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ran into an error while updating {Id}, version {Version}.", Id, Version);

                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.PackageViewModel_FailedToUpdate)),
                    L(nameof(Resources.PackageViewModel_RanIntoUpdateError), Id, ex.Message));
            }
        }

        private bool IsSelfPackageUpgrade()
        {
            return !string.IsNullOrWhiteSpace(BrandingConstants.PackageId)
                && string.Equals(Id, BrandingConstants.PackageId, StringComparison.OrdinalIgnoreCase);
        }

        private async Task TriggerSelfUpgrade()
        {
            try
            {
                using (await StartProgressDialog(L(nameof(Resources.PackageViewModel_UpdatingPackage)), L(nameof(Resources.PackageViewModel_UpdatingPackage)), Id))
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = string.Format("-NoProfile -ExecutionPolicy Bypass -Command \"choco upgrade '{0}' -y\"", BrandingConstants.PackageId),
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    var process = Process.Start(processInfo);
                    if (process == null)
                    {
                        throw new InvalidOperationException("Failed to start self-upgrade process.");
                    }

                    await _dialogService.ShowMessageAsync(
                        L(nameof(Resources.PackageViewModel_UpdatingPackage)),
                        "Upgrade started. The app will close now. Reopen it when done.");
                }

                Application.Current?.Dispatcher?.Invoke(() => Application.Current.Shutdown());
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Self-upgrade for {Package} was cancelled or failed to start.", BrandingConstants.PackageId);
                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.PackageViewModel_FailedToUpdate)),
                    L(nameof(Resources.PackageViewModel_RanIntoUpdateError), BrandingConstants.PackageId, ex.Message));
            }
        }

        private async Task TriggerSelfReinstall()
        {
            try
            {
                using (await StartProgressDialog(L(nameof(Resources.PackageViewModel_ReinstallingPackage)), L(nameof(Resources.PackageViewModel_ReinstallingPackage)), Id))
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = string.Format("-NoProfile -ExecutionPolicy Bypass -Command \"choco upgrade '{0}' -y --force\"", BrandingConstants.PackageId),
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    var process = Process.Start(processInfo);
                    if (process == null)
                    {
                        throw new InvalidOperationException("Failed to start self-reinstall process.");
                    }

                    await _dialogService.ShowMessageAsync(
                        L(nameof(Resources.PackageViewModel_ReinstallingPackage)),
                        "Reinstall started. The app will close now. Reopen it when done.");
                }

                Application.Current?.Dispatcher?.Invoke(() => Application.Current.Shutdown());
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Self-reinstall for {Package} was cancelled or failed to start.", BrandingConstants.PackageId);
                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.PackageViewModel_FailedToReinstall)),
                    L(nameof(Resources.PackageViewModel_RanIntoInstallError), BrandingConstants.PackageId, ex.Message));
            }
        }

        private async Task TriggerSelfUninstall()
        {
            try
            {
                using (await StartProgressDialog(L(nameof(Resources.PackageViewModel_UninstallingPackage)), L(nameof(Resources.PackageViewModel_UninstallingPackage)), Id))
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = string.Format("-NoProfile -ExecutionPolicy Bypass -Command \"choco uninstall '{0}' -y\"", BrandingConstants.PackageId),
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    var process = Process.Start(processInfo);
                    if (process == null)
                    {
                        throw new InvalidOperationException("Failed to start self-uninstall process.");
                    }

                    await _dialogService.ShowMessageAsync(
                        L(nameof(Resources.PackageViewModel_UninstallingPackage)),
                        "Uninstall started. The app will close now."
                    );
                }

                Application.Current?.Dispatcher?.Invoke(() => Application.Current.Shutdown());
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Self-uninstall for {Package} was cancelled or failed to start.", BrandingConstants.PackageId);
                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.PackageViewModel_FailedToUninstall)),
                    L(nameof(Resources.PackageViewModel_RanIntoUninstallError), BrandingConstants.PackageId, ex.Message));
            }
        }

        public async Task Pin()
        {
            try
            {
                using (await StartProgressDialog(L(nameof(Resources.PackageViewModel_PinningPackage)), L(nameof(Resources.PackageViewModel_PinningPackage)), Id))
                {
                    var result = await _chocolateyService.PinPackage(Id, Version.ToNormalizedStringChecked());

                    if (!result.Successful)
                    {
                        var exceptionMessage = result.Exception == null
                            ? string.Empty
                            : L(nameof(Resources.ChocolateyRemotePackageService_ExceptionFormat), result.Exception);

                        var message = L(
                            nameof(Resources.ChocolateyRemotePackageService_PinFailedMessage),
                            Id,
                            Version,
                            string.Join("\n", result.Messages),
                            exceptionMessage);

                        await _dialogService.ShowMessageAsync(
                            L(nameof(Resources.ChocolateyRemotePackageService_PinFailedTitle)),
                            message);

                        Logger.Warning(result.Exception, "Failed to pin {Package}, version {Version}. Errors: {Errors}", Id, Version, result.Messages);

                        return;
                    }

                    IsPinned = true;
                    await _eventAggregator.PublishOnUIThreadAsync(new PackagePinnedMessage(Id, Version, source: ChocolateySource));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ran into an error while pinning {Id}, version {Version}.", Id, Version);

                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.PackageViewModel_FailedToPin)),
                    L(nameof(Resources.PackageViewModel_RanIntoPinningError), Id, ex.Message));
            }
        }

        public async Task Unpin()
        {
            try
            {
                using (await StartProgressDialog(L(nameof(Resources.PackageViewModel_UnpinningPackage)), L(nameof(Resources.PackageViewModel_UnpinningPackage)), Id))
                {
                    var result = await _chocolateyService.UnpinPackage(Id, Version.ToNormalizedStringChecked());

                    if (!result.Successful)
                    {
                        var exceptionMessage = result.Exception == null
                            ? string.Empty
                            : L(nameof(Resources.ChocolateyRemotePackageService_ExceptionFormat), result.Exception);

                        var message = L(
                            nameof(Resources.ChocolateyRemotePackageService_UnpinFailedMessage),
                            Id,
                            Version,
                            string.Join("\n", result.Messages),
                            exceptionMessage);

                        await _dialogService.ShowMessageAsync(
                            L(nameof(Resources.ChocolateyRemotePackageService_UninstallFailedTitle)),
                            message);

                        Logger.Warning(result.Exception, "Failed to unpin {Package}, version {Version}. Errors: {Errors}", Id, Version, result.Messages);

                        return;
                    }

                    IsPinned = false;
                    await _eventAggregator.PublishOnUIThreadAsync(new PackageUnpinnedMessage(Id, Version, source: ChocolateySource));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ran into an error while unpinning {Id}, version {Version}.", Id, Version);

                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.PackageViewModel_FailedToUnpin)),
                    L(nameof(Resources.PackageViewModel_RanIntoUnpinError), Id, ex.Message));
            }
        }

#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void

        public async void ViewDetails()
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
        {
            EnsureAvailableVersionsLoaded();
            await _eventAggregator.PublishOnUIThreadAsync(new ShowPackageDetailsMessage(this)).ConfigureAwait(false);
        }

        public void Handle(FeatureModifiedMessage message)
        {
            NotifyPropertyChanged(nameof(IsDownloadCountAvailable));
        }

        public void Handle(PackageOutdatedMessage message)
        {
            if (!string.Equals(message.Id, Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (LatestVersion == message.Version)
            {
                return;
            }

            if ((ChocolateySource == null && message.Source == null) || (ChocolateySource == message.Source))
            {
                RemoteVersion = message.Version;

                if (IsInstalled)
                {
                    LatestVersion = message.Version;
                    IsOutdated = true;
                }
                else
                {
                    LatestVersion = null;
                    Version = message.Version;
                }
            }

            return;            
        }

        public void Handle(PackageUpgradedMessage message)
        {
            if (!string.Equals(message.Id, Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (IsInstalled)
            {
                LatestVersion = RemoteVersion;
                Version = message.Version;

                if (LatestVersion <= Version)
                {
                    LatestVersion = null;
                }

                IsOutdated = LatestVersion > Version;
            }
        }

        public void Handle(PackageUninstalledMessage message)
        {
            // If this is the local source, we don't need to do anything, since the package
            // will be removed from the list of packages completely.
            if (ChocolateySource == null)
            {
                return;
            }

            if (!string.Equals(message.Id, Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (LatestVersion != null)
            {
                Version = RemoteVersion;
                LatestVersion = null;
            }

            IsInstalled = false;
        }

        public void Handle(PackageInstalledMessage message)
        {
            if (!string.Equals(message.Id, Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            LatestVersion = RemoteVersion;
            Version = message.Version;

            if (LatestVersion == Version)
            {
                LatestVersion = null;
            }

            IsInstalled = true;

            IsOutdated = LatestVersion > Version;
        }

        public void Handle(PackagePinnedMessage message)
        {
            if (!string.Equals(message.Id, Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            IsPinned = true;
        }

        public void Handle(PackageUnpinnedMessage message)
        {
            if (!string.Equals(message.Id, Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            IsPinned = false;
        }

        private async Task InstallPackage(string version, AdvancedInstall advancedOptions = null)
        {
            try
            {
                using (await StartProgressDialog(L(nameof(Resources.PackageViewModel_InstallingPackage)), L(nameof(Resources.PackageViewModel_InstallingPackage)), Id))
                {
                    var packageInstallResult = await _chocolateyService.InstallPackage(
                        Id,
                        version,
                        Source,
                        false,
                        advancedOptions);

                    if (!packageInstallResult.Successful)
                    {
                        var exceptionMessage = packageInstallResult.Exception == null
                            ? string.Empty
                            : L(nameof(Resources.ChocolateyRemotePackageService_ExceptionFormat), packageInstallResult.Exception);

                        var message = L(
                            nameof(Resources.ChocolateyRemotePackageService_InstallFailedMessage),
                            Id,
                            Version,
                            string.Join("\n", packageInstallResult.Messages),
                            exceptionMessage);

                        await _dialogService.ShowMessageAsync(
                            L(nameof(Resources.ChocolateyRemotePackageService_InstallFailedTitle)),
                            message);

                        Logger.Warning(packageInstallResult.Exception, "Failed to install {Package}, version {Version}. Errors: {Errors}", Id, Version, packageInstallResult.Messages);

                        return;
                    }

                    IsInstalled = true;

                    // We need to clear out all files, as there may be incorrect information cached for 
                    // all the sources.
                    _chocolateyGuiCacheService.PurgeOutdatedPackages(source: null, includePrerelease: false);
                    await _eventAggregator.PublishOnUIThreadAsync(new PackageInstalledMessage(Id, Version, ChocolateySource));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ran into an error while installing {Id}, version {Version}.", Id, Version);

                await _dialogService.ShowMessageAsync(
                    L(nameof(Resources.PackageViewModel_FailedToInstall)),
                    L(nameof(Resources.PackageViewModel_RanIntoInstallError), Id, ex.Message));
            }
        }

        private async Task<IDisposable> StartProgressDialog(string commandString, string initialProgressText, string id = "")
        {
            await _progressService.StartLoading(L(nameof(Resources.PackageViewModel_StartLoadingFormat), commandString, id));
            _progressService.WriteMessage(initialProgressText);
            return new DisposableAction(() => _progressService.StopLoading());
        }

        private class DisposableAction : IDisposable
        {
            private readonly Action _disposeAction;

            public DisposableAction(System.Action disposeAction)
            {
                _disposeAction = disposeAction;
            }

            public void Dispose()
            {
                _disposeAction?.Invoke();
            }
        }

        private string GetInstallVersion()
        {
            if (string.IsNullOrWhiteSpace(SelectedVersion) ||
                string.Equals(SelectedVersion, Resources.AdvancedChocolateyDialog_LatestVersion, StringComparison.OrdinalIgnoreCase))
            {
                return Version.ToNormalizedStringChecked();
            }

            return SelectedVersion;
        }

        private void RefreshSelectedVersionDetails()
        {
#pragma warning disable 4014
            RefreshSelectedVersionDetailsAsync();
#pragma warning restore 4014
        }

        private async Task RefreshSelectedVersionDetailsAsync()
        {
            if (_isRefreshingSelectedVersionDetails || string.IsNullOrWhiteSpace(SelectedVersion))
            {
                return;
            }

            if (string.Equals(SelectedVersion, Resources.AdvancedChocolateyDialog_LatestVersion, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            NuGetVersion selectedVersion;
            if (!NuGetVersion.TryParse(SelectedVersion, out _))
            {
                return;
            }

            IsRefreshingSelectedVersionDetails = true;

            try
            {
                var package = await _chocolateyService.GetByVersionAndIdAsync(Id, SelectedVersion, IncludePreRelease, Source);
                if (package != null)
                {
                    Authors = package.Authors;
                    Copyright = package.Copyright;
                    Dependencies = package.Dependencies;
                    Description = package.Description;
                    DownloadCount = package.DownloadCount;
                    GalleryDetailsUrl = package.GalleryDetailsUrl;
                    IconUrl = package.IconUrl;
                    Language = package.Language;
                    LicenseUrl = package.LicenseUrl;
                    Owners = package.Owners;
                    PackageHash = package.PackageHash;
                    PackageHashAlgorithm = package.PackageHashAlgorithm;
                    PackageSize = package.PackageSize;
                    ProjectUrl = package.ProjectUrl;
                    Published = package.Published;
                    ReleaseNotes = package.ReleaseNotes;
                    ReportAbuseUrl = package.ReportAbuseUrl;
                    RequireLicenseAcceptance = package.RequireLicenseAcceptance;
                    Summary = package.Summary;
                    Tags = package.Tags;
                    Title = package.Title;
                    VersionDownloadCount = package.VersionDownloadCount;
                    IsPrerelease = package.IsPrerelease;
                    NotifyPropertyChanged(nameof(IsDownloadCountAvailable));
                    NotifyPropertyChanged(nameof(IsPackageSizeAvailable));
                }

                IsSourceAvailable = true;
                SourceRefreshError = null;
            }
            catch (Exception ex)
            {
                IsSourceAvailable = false;
                SourceRefreshError = L(nameof(Resources.RemoteSourceViewModel_UnableToConnectToFeed), Source?.ToString() ?? string.Empty);
                Logger.Warning(ex, "Unable to refresh package details for {Id}, version {Version}, from {Source}.", Id, SelectedVersion, Source);
            }
            finally
            {
                IsRefreshingSelectedVersionDetails = false;
            }
        }

        private void EnsureAvailableVersionsLoaded(bool forceReload = false)
        {
            if (_availableVersionsLoading)
            {
                return;
            }

            if (_availableVersionsLoaded && !forceReload)
            {
                return;
            }

            _availableVersionsLoaded = false;
            _availableVersionsLoading = true;
            AvailableVersions = new Utilities.NotifyTaskCompletion<ObservableCollection<string>>(GetAvailableVersionsAsync());
        }

        private async Task<ObservableCollection<string>> GetAvailableVersionsAsync()
        {
            var availableVersions = new ObservableCollection<string>
            {
                Resources.AdvancedChocolateyDialog_LatestVersion
            };

            var currentVersion = Version?.ToNormalizedStringChecked();
            if (!string.IsNullOrWhiteSpace(currentVersion) && !availableVersions.Contains(currentVersion))
            {
                availableVersions.Add(currentVersion);
            }

            var loadedFromSource = false;

            try
            {
                var versions = await _chocolateyService.GetAvailableVersionsForPackageIdAsync(Id, 0, 100, IncludePreRelease, Source);
                foreach (var version in versions)
                {
                    var versionString = version.ToNormalizedStringChecked();
                    if (!availableVersions.Contains(versionString))
                    {
                        availableVersions.Add(versionString);
                    }
                }

                loadedFromSource = true;
                IsSourceAvailable = true;
                SourceRefreshError = null;
            }
            catch (Exception ex)
            {
                IsSourceAvailable = false;
                SourceRefreshError = L(nameof(Resources.RemoteSourceViewModel_UnableToConnectToFeed), Source?.ToString() ?? string.Empty);
                Logger.Warning(ex, "Unable to refresh package versions for {Id} from {Source}.", Id, Source);
            }
            finally
            {
                _availableVersionsLoading = false;
                _availableVersionsLoaded = loadedFromSource;
            }

            if (string.IsNullOrWhiteSpace(SelectedVersion))
            {
                SelectedVersion = currentVersion ?? Resources.AdvancedChocolateyDialog_LatestVersion;
            }

            return availableVersions;
        }
    }
}
