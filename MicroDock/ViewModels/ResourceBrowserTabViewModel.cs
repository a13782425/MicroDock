using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using FluentAvalonia.Styling;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;

namespace MicroDock.ViewModels;

/// <summary>
/// 资源浏览器视图模型
/// </summary>
public class ResourceBrowserTabViewModel : ReactiveObject
{
    private string _searchText = string.Empty;
    private string _selectedType = "全部";
    private ObservableCollection<ResourceItemModel> _filteredResources = new();
    public ResourceBrowserTabViewModel()
    {
        FilteredResources = new ObservableCollection<ResourceItemModel>();
        ResourceTypes = new ObservableCollection<string> { "全部" };
        RefreshCommand = ReactiveCommand.Create(LoadResources);
        // 搜索和筛选响应
        this.WhenAnyValue(x => x.SearchText, x => x.SelectedType)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ApplyFilter());
        // 初始加载
        LoadResources();
    }
    //public ObservableCollection<ResourceItemModel> AllResources { get; }

    private Dictionary<string, ResourceItemModel> _resourceDict = new();

    public ObservableCollection<ResourceItemModel> FilteredResources
    {
        get => _filteredResources;
        set => this.RaiseAndSetIfChanged(ref _filteredResources, value);
    }
    public ObservableCollection<string> ResourceTypes { get; }
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }
    public string SelectedType
    {
        get => _selectedType;
        set => this.RaiseAndSetIfChanged(ref _selectedType, value);
    }
    public int TotalCount => FilteredResources.Count;
    public ICommand RefreshCommand { get; }

    private void LoadResources()
    {
        _resourceDict.Clear();
        ResourceTypes.Clear();
        ResourceTypes.Add("全部");
        var types = new HashSet<string>();
        if (Application.Current?.Resources != null)
        {
            // 主资源
            foreach (var style in Application.Current.Styles)
            {
                if (style is FluentTheme baseTheme)
                {
                    if (!baseTheme.Resources.ThemeDictionaries.Any())
                        continue;
                    KeyValuePair<Avalonia.Styling.ThemeVariant, IThemeVariantProvider> theme = baseTheme.Resources.ThemeDictionaries.FirstOrDefault();
                    if (theme.Value is ResourceDictionary resourceDict)
                    {
                        foreach (var key in resourceDict.Keys)
                        {
                            if (baseTheme.TryGetResource(key, null, out var value))
                            {
                                AddOrUpdateResource(key, value, "Base", types);
                            }
                        }
                    }
                }
                else if (style is FluentAvaloniaTheme faTheme)
                {
                    if (!faTheme.Resources.ThemeDictionaries.Any())
                        continue;
                    KeyValuePair<Avalonia.Styling.ThemeVariant, IThemeVariantProvider> theme = faTheme.Resources.ThemeDictionaries.FirstOrDefault();
                    if (theme.Value is ResourceDictionary resourceDict)
                    {
                        foreach (var key in resourceDict.Keys)
                        {
                            if (faTheme.TryGetResource(key, null, out var value))
                            {
                                AddOrUpdateResource(key, value, "FA", types);
                            }
                        }
                    }
                }
            }
        }
        // 添加类型筛选选项
        foreach (var type in types.OrderBy(t => t))
        {
            ResourceTypes.Add(type);
        }
        ApplyFilter();
        this.RaisePropertyChanged(nameof(TotalCount));
    }


    /// <summary>
    /// 添加或更新资源（后者覆盖前者，并标记为已覆盖）
    /// </summary>
    private void AddOrUpdateResource(object key, object? value, string source, HashSet<string> types)
    {
        var keyStr = key.ToString() ?? "";
        var item = CreateResourceItem(key, value, source);
        if (item == null) return;
        if (_resourceDict.TryGetValue(keyStr, out var existing))
        {
            if (existing.Value != value)
            {
                // 已存在，标记为被覆盖
                item.IsOverridden = true;
                item.OverriddenFrom = existing.Source;
            }
        }
        _resourceDict[keyStr] = item;
        types.Add(item.TypeName);
    }

    private ResourceItemModel? CreateResourceItem(object key, object? value, string source)
    {
        if (value == null) return null;
        var item = new ResourceItemModel
        {
            Key = key.ToString() ?? "",
            Value = value,
            TypeName = GetFriendlyTypeName(value.GetType()),
            Source = source,
        };
        // 处理显示值
        switch (value)
        {
            case SolidColorBrush brush:
                item.DisplayValue = brush.Color.ToString();
                item.PreviewBrush = brush;
                break;
            case Color color:
                item.DisplayValue = color.ToString();
                item.PreviewBrush = new SolidColorBrush(color);
                break;
            case FontFamily font:
                item.DisplayValue = font.Name;
                break;
            case double d:
                item.DisplayValue = d.ToString("F2");
                break;
            case Thickness t:
                item.DisplayValue = $"{t.Left},{t.Top},{t.Right},{t.Bottom}";
                break;
            case CornerRadius cr:
                item.DisplayValue = $"{cr.TopLeft},{cr.TopRight},{cr.BottomRight},{cr.BottomLeft}";
                break;
            default:
                item.DisplayValue = value.GetType().Name;
                break;
        }
        return item;
    }
    private string GetFriendlyTypeName(Type type)
    {
        var name = type.Name;
        // 移除泛型标记
        var index = name.IndexOf('`');
        if (index > 0) name = name.Substring(0, index);
        return name;
    }
    private void ApplyFilter()
    {
        var query = _resourceDict.Values.AsEnumerable();
        // 类型筛选
        if (!string.IsNullOrEmpty(SelectedType) && SelectedType != "全部")
        {
            query = query.Where(r => r.TypeName == SelectedType);
        }
        // 搜索筛选
        if (!string.IsNullOrEmpty(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            query = query.Where(r =>
                r.Key.ToLowerInvariant().Contains(search) ||
                r.DisplayValue.ToLowerInvariant().Contains(search));
        }
        FilteredResources = new ObservableCollection<ResourceItemModel>(query.OrderBy(r => r.Key));
        this.RaisePropertyChanged(nameof(TotalCount));
    }
}
/// <summary>
/// 资源项模型
/// </summary>
public class ResourceItemModel
{
    public string Key { get; set; } = "";
    public object? Value { get; set; }
    public string TypeName { get; set; } = "";
    public string Source { get; set; } = "";
    public string DisplayValue { get; set; } = "";
    public IBrush? PreviewBrush { get; set; }
    public bool IsOverridden { get; set; }
    public string? OverriddenFrom { get; set; }
    public bool HasPreviewBrush => PreviewBrush != null;

    // 显示覆盖提示
    public string SourceDisplay => IsOverridden
        ? $"{Source} (覆盖自 {OverriddenFrom})"
        : Source;
}
