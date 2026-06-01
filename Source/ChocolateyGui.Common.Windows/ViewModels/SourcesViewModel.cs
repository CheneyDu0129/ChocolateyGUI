// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Chocolatey" file="SourcesViewModel.cs">
//   Copyright 2017 - Present Chocolatey Software, LLC
//   Copyright 2014 - 2017 Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Caliburn.Micro;
using ChocolateyGui.Common.Models;
using ChocolateyGui.Common.Models.Messages;
using ChocolateyGui.Common.Services;
using ChocolateyGui.Common.ViewModels;
using ChocolateyGui.Common.Windows.Services;

namespace ChocolateyGui.Common.Windows.ViewModels
{
    public class SourcesViewModel : Conductor<ISourceViewModelBase>.Collection.OneActive, IHandleWithTask<SourcesUpdatedMessage>
    {
        private readonly IChocolateyService _packageService;
        private readonly CreateRemove _remoteSourceVmFactory;
        private readonly IConfigService _configService;
        private readonly IImageService _imageService;
        private readonly IVersionService _versionService;
        private readonly Func<string, LocalSourceViewModel> _localSourceVmFactory;
        private bool _firstLoad = true;
        private int _selectedSourceModeIndex;
        private ISourceViewModelBase _selectedBrowseSource;
        private bool _isSynchronizingSelection;
        private IDisposable _activeItemSubscription;

        public SourcesViewModel(
            IChocolateyService packageService,
            IConfigService configService,
            IImageService imageService,
            IEventAggregator eventAggregator,
            IVersionService versionService,
            Func<string, LocalSourceViewModel> localSourceVmFactory,
            CreateRemove remoteSourceVmFactory)
        {
            _packageService = packageService;
            _configService = configService;
            _imageService = imageService;
            _versionService = versionService;
            _remoteSourceVmFactory = remoteSourceVmFactory;
            _localSourceVmFactory = localSourceVmFactory;

            if (localSourceVmFactory == null)
            {
                throw new ArgumentNullException(nameof(localSourceVmFactory));
            }

            if (remoteSourceVmFactory == null)
            {
                throw new ArgumentNullException(nameof(remoteSourceVmFactory));
            }

            eventAggregator.Subscribe(this);
        }

        public delegate RemoteSourceViewModel CreateRemove(ChocolateySource source);

        public ImageSource PrimaryApplicationImageSource
        {
            get { return _imageService.PrimaryApplicationImage; }
        }

        public ImageSource SecondaryApplicationImageSource
        {
            get { return _imageService.SecondaryApplicationImage; }
        }

        public string DisplayVersion
        {
            get { return _versionService.DisplayVersion; }
        }

        public IEnumerable<ISourceViewModelBase> BrowseSources
        {
            get { return Items.Where(vm => !(vm is LocalSourceViewModel) && !(vm is SourceSeparatorViewModel)); }
        }

        public bool HasMultipleBrowseSources
        {
            get { return BrowseSources.Skip(1).Any(); }
        }

        public int SelectedSourceModeIndex
        {
            get { return _selectedSourceModeIndex; }
            set
            {
                if (_selectedSourceModeIndex == value)
                {
                    return;
                }

                _selectedSourceModeIndex = value;
                NotifyOfPropertyChange(nameof(SelectedSourceModeIndex));
                NotifyOfPropertyChange(nameof(IsBrowseProductsSelected));
                NotifyOfPropertyChange(nameof(IsInstalledSelected));
                ActivateSourceMode();
            }
        }

        public bool IsBrowseProductsSelected
        {
            get { return SelectedSourceModeIndex == 0; }
            set
            {
                if (value)
                {
                    SelectedSourceModeIndex = 0;
                }
            }
        }

        public bool IsInstalledSelected
        {
            get { return SelectedSourceModeIndex == 1; }
            set
            {
                if (value)
                {
                    SelectedSourceModeIndex = 1;
                }
            }
        }

        public ISourceViewModelBase SelectedBrowseSource
        {
            get { return _selectedBrowseSource; }
            set
            {
                if (value == null || value is LocalSourceViewModel || value is SourceSeparatorViewModel)
                {
                    return;
                }

                if (ReferenceEquals(_selectedBrowseSource, value))
                {
                    return;
                }

                _selectedBrowseSource = value;
                NotifyOfPropertyChange(nameof(SelectedBrowseSource));

                var remoteSource = value as RemoteSourceViewModel;
                if (remoteSource != null)
                {
                    remoteSource.LoadPackages(false);
                }

                if (!_isSynchronizingSelection && IsBrowseProductsSelected)
                {
                    ActivateItem(value);
                }
            }
        }

        public bool IsActiveSourceOnline
        {
            get
            {
                var remoteSource = ActiveItem as RemoteSourceViewModel;
                return remoteSource == null || remoteSource.IsOnline;
            }
        }

        public virtual async Task LoadSources()
        {
            var oldItems = Items.Skip(1).Cast<ISourceViewModelBase>().ToList();
            var previousBrowseSource = _selectedBrowseSource as RemoteSourceViewModel;

            var sources = await _packageService.GetSources();
            var vms = new List<ISourceViewModelBase>();

            if (_configService.GetEffectiveConfiguration().ShowAggregatedSourceView ?? false)
            {
                vms.Add(_remoteSourceVmFactory(new ChocolateyAggregatedSources()));
                vms.Add(new SourceSeparatorViewModel());
            }

            foreach (var source in sources.Where(s => !s.Disabled && !IsHiddenSource(s)).OrderBy(s => s.Priority))
            {
                vms.Add(_remoteSourceVmFactory(source));
            }

            await Execute.OnUIThreadAsync(
                () =>
                    {
                        Items.RemoveRange(oldItems);
                        Items.AddRange(vms);

                        var browseSources = BrowseSources.ToList();
                        var defaultBrowseSource = browseSources
                            .OfType<RemoteSourceViewModel>()
                            .FirstOrDefault(vm => string.Equals(vm.Source?.Id, "local", StringComparison.OrdinalIgnoreCase))
                            ?? browseSources.FirstOrDefault();

                        _selectedBrowseSource = previousBrowseSource == null
                            ? defaultBrowseSource
                            : browseSources.OfType<RemoteSourceViewModel>().FirstOrDefault(vm => vm.Source.Id == previousBrowseSource.Source.Id)
                              ?? defaultBrowseSource;

                        NotifyBrowseProperties();

                        var localSource = Items.OfType<LocalSourceViewModel>().FirstOrDefault();
                        if (IsInstalledSelected && localSource != null)
                        {
                            ActivateItem(localSource);
                        }
                        else if (_selectedBrowseSource != null)
                        {
                            ActivateItem(_selectedBrowseSource);
                        }
                        else if (localSource != null)
                        {
                            ActivateItem(localSource);
                        }
                    });
        }

        private static bool IsHiddenSource(ChocolateySource source)
        {
            if (source == null)
            {
                return false;
            }

            if (string.Equals(source.Id, "chocolatey", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(source.Value)
                && source.Value.IndexOf("community.chocolatey.org", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public async Task Handle(SourcesUpdatedMessage message)
        {
            await LoadSources();
        }

        protected override void OnViewReady(object view)
        {
            Observable.FromEventPattern<PropertyChangedEventArgs>(this, nameof(PropertyChanged))
                .Where(p => p.EventArgs.PropertyName == nameof(ActiveItem))
                .Subscribe(
                    p =>
                    {
                        DisplayName = $"Source - {ActiveItem?.DisplayName}";
                        UpdateSelectionFromActiveItem();
                        HookActiveItemOnlineStatusNotifications();
                        NotifyOfPropertyChange(nameof(IsActiveSourceOnline));
                    });

            if (_firstLoad)
            {
                Items.Add(_localSourceVmFactory("[Resources_ThisPC]"));
                NotifyBrowseProperties();

                _ = LoadSources();
                _firstLoad = false;
            }
        }

        private void HookActiveItemOnlineStatusNotifications()
        {
            _activeItemSubscription?.Dispose();

            var activeRemoteSource = ActiveItem as RemoteSourceViewModel;
            if (activeRemoteSource == null)
            {
                return;
            }

            _activeItemSubscription = Observable.FromEventPattern<PropertyChangedEventArgs>(activeRemoteSource, nameof(activeRemoteSource.PropertyChanged))
                .Where(p => p.EventArgs.PropertyName == nameof(RemoteSourceViewModel.IsOnline))
                .Subscribe(_ => NotifyOfPropertyChange(nameof(IsActiveSourceOnline)));
        }

        private void ActivateSourceMode()
        {
            if (_isSynchronizingSelection)
            {
                return;
            }

            if (IsInstalledSelected)
            {
                var localSource = Items.OfType<LocalSourceViewModel>().FirstOrDefault();
                if (localSource != null && !ReferenceEquals(ActiveItem, localSource))
                {
                    ActivateItem(localSource);
                }

                return;
            }

            var browseSource = SelectedBrowseSource ?? BrowseSources.FirstOrDefault();
            if (browseSource != null && !ReferenceEquals(ActiveItem, browseSource))
            {
                ActivateItem(browseSource);
            }
        }

        private void UpdateSelectionFromActiveItem()
        {
            _isSynchronizingSelection = true;

            if (ActiveItem is LocalSourceViewModel)
            {
                _selectedSourceModeIndex = 1;
            }
            else if (ActiveItem != null && !(ActiveItem is SourceSeparatorViewModel))
            {
                _selectedSourceModeIndex = 0;
                _selectedBrowseSource = ActiveItem;
            }

            NotifyOfPropertyChange(nameof(SelectedSourceModeIndex));
            NotifyOfPropertyChange(nameof(IsBrowseProductsSelected));
            NotifyOfPropertyChange(nameof(IsInstalledSelected));
            NotifyOfPropertyChange(nameof(SelectedBrowseSource));

            _isSynchronizingSelection = false;
        }

        private void NotifyBrowseProperties()
        {
            NotifyOfPropertyChange(nameof(BrowseSources));
            NotifyOfPropertyChange(nameof(HasMultipleBrowseSources));
            NotifyOfPropertyChange(nameof(SelectedBrowseSource));
            NotifyOfPropertyChange(nameof(IsActiveSourceOnline));
        }

        private class SourcesComparer : IEqualityComparer<RemoteSourceViewModel>
        {
            public bool Equals(RemoteSourceViewModel x, RemoteSourceViewModel y)
            {
                return x.Source.Equals(y.Source);
            }

            public int GetHashCode(RemoteSourceViewModel obj)
            {
                return obj.Source.GetHashCode();
            }
        }
    }
}