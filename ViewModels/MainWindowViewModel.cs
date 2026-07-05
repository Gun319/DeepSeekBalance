using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeepSeekBalance.Models;
using DeepSeekBalance.Services;

namespace DeepSeekBalance.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IConfigService _cfg;
    private readonly IDeepSeekApiService _api;
    private readonly IPlatformApiService _plat;

    // ============ Navigation ============
    [ObservableProperty] private bool _isDashboard = true;
    [ObservableProperty] private bool _isSettings;
    [ObservableProperty] private bool _isDetail;
    [ObservableProperty] private string _detailModelName = string.Empty;
    [ObservableProperty] private string _detailModelKey = string.Empty;
    [ObservableProperty] private bool _detailIsFlash;

    // ============ Config ============
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private bool _isApiKeySaved;
    [ObservableProperty] private string _usageToken = string.Empty;
    [ObservableProperty] private bool _isUsageTokenSaved;

    // ============ Balance ============
    [ObservableProperty] private bool _isAvailable;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isUsageLoading;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private string _lastUpdated = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _balanceAmount = "0.00";
    [ObservableProperty] private string _balanceCurrencySymbol = "¥";
    [ObservableProperty] private string _balanceStatusText = "—";
    [ObservableProperty] private bool _balanceStatusOff;
    [ObservableProperty] private string _balanceError = string.Empty;
    [ObservableProperty] private bool _balanceHasError;
    [ObservableProperty] private string _todayCost = "—";
    [ObservableProperty] private string _monthCost = "—";

    // ============ Usage Models ============
    [ObservableProperty] private long _flashTotalTokens;
    [ObservableProperty] private double _flashCost;
    [ObservableProperty] private long _flashHit;
    [ObservableProperty] private long _flashMiss;
    [ObservableProperty] private long _flashCacheHitRatePercent;
    [ObservableProperty] private long _flashRequestCount;
    [ObservableProperty] private long _flashResponse;

    [ObservableProperty] private long _proTotalTokens;
    [ObservableProperty] private double _proCost;
    [ObservableProperty] private long _proHit;
    [ObservableProperty] private long _proMiss;
    [ObservableProperty] private long _proCacheHitRatePercent;
    [ObservableProperty] private long _proRequestCount;
    [ObservableProperty] private long _proResponse;

    [ObservableProperty] private long _maxModelTokens = 1;

    // ============ Chart ============
    public ObservableCollection<ChartBar> ChartBars { get; } = [];
    [ObservableProperty] private string _chartHitRate = "";
    [ObservableProperty] private string _chartTotal = "";
    [ObservableProperty] private bool _showChart;

    // ============ Detail Chart ============
    public ObservableCollection<ChartBar> DetailBars { get; } = [];
    [ObservableProperty] private string _detailTotal = "";
    [ObservableProperty] private string _detailCost = "";
    [ObservableProperty] private long _detailRequestCount;
    [ObservableProperty] private string _detailDateRange = "";

    // ============ Auto Refresh ============
    [ObservableProperty] private int _refreshSecs = 60;
    [ObservableProperty] private bool _autoRefresh;
    private Timer? _timer;

    // ============ Theme ============
    [ObservableProperty] private bool _isDark = true;

    // ============ Cached data ============
    private List<DailyUsage> _allDays = [];
    private List<ModelSummary> _allModels = [];

    public MainWindowViewModel(IConfigService cfg, IDeepSeekApiService api, IPlatformApiService plat)
    {
        _cfg = cfg; _api = api; _plat = plat;
        var key = _cfg.LoadApiKey();
        if (!string.IsNullOrEmpty(key)) { ApiKey = key; IsApiKeySaved = true; }
        var tok = _cfg.LoadUsageToken();
        if (!string.IsNullOrEmpty(tok)) { UsageToken = tok; IsUsageTokenSaved = true; }
        RefreshSecs = _cfg.LoadRefreshIntervalSeconds();
    }

    // ============ Nav ============
    [RelayCommand] private void GoDashboard() { IsDashboard = true; IsSettings = IsDetail = false; }
    [RelayCommand] private void GoSettings() { IsSettings = true; IsDashboard = IsDetail = false; }
    [RelayCommand]
    private void GoDetail(string key)
    {
        IsDetail = true; IsDashboard = IsSettings = false;
        DetailIsFlash = key == "flash";
        DetailModelKey = key;
        DetailModelName = DetailIsFlash ? "V4 Flash" : "V4 Pro";
        BuildDetailChart();
    }

    // ============ Config ============
    [RelayCommand] private void SaveApiKey() { if (string.IsNullOrWhiteSpace(ApiKey)) return; _cfg.SaveApiKey(ApiKey.Trim()); IsApiKeySaved = true; }
    [RelayCommand] private void ClearApiKey() { ApiKey = string.Empty; IsApiKeySaved = false; _cfg.SaveApiKey(string.Empty); }
    [RelayCommand] private void SaveUsageToken() { if (string.IsNullOrWhiteSpace(UsageToken)) return; _cfg.SaveUsageToken(UsageToken.Trim()); IsUsageTokenSaved = true; }
    [RelayCommand] private void ClearUsageToken() { UsageToken = string.Empty; IsUsageTokenSaved = false; _cfg.SaveUsageToken(string.Empty); }
    [RelayCommand] private void SetRefresh(int secs) { RefreshSecs = secs; _cfg.SaveRefreshIntervalSeconds(secs); }

    // ============ Theme ============
    [RelayCommand] private void ToggleTheme() { IsDark = !IsDark; }

    // ============ Refresh ============
    partial void OnAutoRefreshChanged(bool value) { if (value) StartTimer(); else StopTimer(); }
    partial void OnRefreshSecsChanged(int value) { if (AutoRefresh) { StopTimer(); StartTimer(); } }
    private void StartTimer() { StopTimer(); _timer = new Timer(async _ => await DoRefresh(), null, TimeSpan.FromSeconds(RefreshSecs), TimeSpan.FromSeconds(RefreshSecs)); }
    private void StopTimer() { _timer?.Dispose(); _timer = null; }
    public void DisposeTimer() => StopTimer();

    [RelayCommand] private async Task Refresh() => await DoRefresh();

    private async Task DoRefresh()
    {
        var key = ApiKey?.Trim();
        if (string.IsNullOrEmpty(key)) { BalanceError = "未配置 API Key"; BalanceHasError = true; return; }

        IsLoading = true;
        BalanceHasError = false;
        BalanceError = string.Empty;

        try
        {
            var resp = await _api.GetBalanceAsync(key);
            if (resp == null) { BalanceError = "返回数据为空"; BalanceHasError = true; return; }

            IsAvailable = resp.IsAvailable;
            BalanceStatusText = resp.IsAvailable ? "可用" : "余额不足";
            BalanceStatusOff = !resp.IsAvailable;

            var info = resp.BalanceInfos.FirstOrDefault();
            if (info != null)
            {
                BalanceCurrencySymbol = info.Currency == "USD" ? "$" : "¥";
                BalanceAmount = $"{BalanceCurrencySymbol}{info.TotalBalance}";
            }

            LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await DoRefreshUsage();
        }
        catch (HttpRequestException ex) { BalanceError = ex.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "API Key 无效或已过期" : $"请求失败: {ex.Message}"; BalanceHasError = true; }
        catch (TaskCanceledException) { BalanceError = "请求超时，请检查网络连接"; BalanceHasError = true; }
        catch (Exception ex) { BalanceError = ex.Message; BalanceHasError = true; }
        finally { IsLoading = false; }
    }

    private async Task DoRefreshUsage()
    {
        var token = _cfg.LoadUsageToken();
        if (string.IsNullOrEmpty(token)) { ShowChart = false; return; }

        IsUsageLoading = true;
        try
        {
            var now = DateTime.Now;
            var (models, days, monthCost) = await _plat.GetUsageAsync(token, now.Month, now.Year);
            var start = now.AddDays(-6);
            if (start.Month != now.Month)
            {
                try { var (_, pd, _) = await _plat.GetUsageAsync(token, start.Month, start.Year); days.InsertRange(0, pd); } catch { }
            }

            _allModels = models;
            _allDays = days;

            MonthCost = $"¥{monthCost:F2}";
            var today = days.FirstOrDefault(d => d.Date == now.ToString("yyyy-MM-dd"));
            TodayCost = today != null ? $"¥{today.TotalCost:F2}" : "¥0.00";

            var flash = models.FirstOrDefault(m => m.Key == "flash");
            if (flash != null)
            {
                FlashTotalTokens = flash.TotalTokens; FlashCost = flash.Cost;
                FlashHit = flash.CacheHitTokens; FlashMiss = flash.CacheMissTokens; FlashResponse = flash.ResponseTokens;
                FlashRequestCount = flash.RequestCount;
                FlashCacheHitRatePercent = FlashHit + FlashMiss > 0 ? (long)((double)FlashHit / (FlashHit + FlashMiss) * 100) : 0;
            }
            var pro = models.FirstOrDefault(m => m.Key == "pro");
            if (pro != null)
            {
                ProTotalTokens = pro.TotalTokens; ProCost = pro.Cost;
                ProHit = pro.CacheHitTokens; ProMiss = pro.CacheMissTokens; ProResponse = pro.ResponseTokens;
                ProRequestCount = pro.RequestCount;
                ProCacheHitRatePercent = ProHit + ProMiss > 0 ? (long)((double)ProHit / (ProHit + ProMiss) * 100) : 0;
            }
            MaxModelTokens = Math.Max(1, Math.Max(FlashTotalTokens, ProTotalTokens));

            BuildMainChart(days, now);
            BuildDetailChart();
            ShowChart = true;
        }
        catch { ShowChart = false; }
        finally { IsUsageLoading = false; }
    }

    private void BuildMainChart(List<DailyUsage> days, DateTime now)
    {
        ChartBars.Clear();
        var recent = Recent7Days(days, now);
        long max = Math.Max(recent.Max(d => d.TotalTokens), 1);
        long sumH = 0, sumM = 0, sumT = 0;

        foreach (var d in recent)
        {
            var hit = d.FlashCacheHit + d.ProCacheHit;
            var miss = d.FlashCacheMiss + d.ProCacheMiss;
            var resp = d.FlashResponse + d.ProResponse;
            var total = hit + miss + resp;
            sumH += hit; sumM += miss; sumT += total;

            ChartBars.Add(MakeBar(d.Date, hit, miss, resp, max));
        }

        var rate = sumH + sumM > 0 ? (int)((double)sumH / (sumH + sumM) * 100) : 0;
        ChartHitRate = $"命中率 {rate}%";
        ChartTotal = $"合计 {FmtShort(sumT)}";
    }

    private void BuildDetailChart()
    {
        DetailBars.Clear();
        if (_allDays.Count == 0) return;

        var flash = _allModels.FirstOrDefault(m => m.Key == DetailModelKey);
        if (flash != null)
        {
            DetailTotal = FmtShort(flash.TotalTokens);
            DetailCost = $"¥{flash.Cost:F2}";
            DetailRequestCount = flash.RequestCount;
        }

        var recent = Recent7Days(_allDays, DateTime.Now);
        if (recent.Count > 0) DetailDateRange = $"{FmtDate(recent[0].Date)} - {FmtDate(recent[^1].Date)}";
        long max = Math.Max(recent.Max(d =>
        {
            var (h, m, r) = DetailIsFlash ? (d.FlashCacheHit, d.FlashCacheMiss, d.FlashResponse) : (d.ProCacheHit, d.ProCacheMiss, d.ProResponse);
            return h + m + r;
        }), 1);

        foreach (var d in recent)
        {
            var (h, m, r) = DetailIsFlash ? (d.FlashCacheHit, d.FlashCacheMiss, d.FlashResponse) : (d.ProCacheHit, d.ProCacheMiss, d.ProResponse);
            DetailBars.Add(MakeBar(d.Date, h, m, r, max));
        }
    }

    private static ChartBar MakeBar(string date, long hit, long miss, long resp, long maxTotal)
    {
        var total = hit + miss + resp;
        var pct = maxTotal > 0 ? (double)total / maxTotal : 0;
        var barH = Math.Max(4, pct * 100);
        double seg(double v) => total > 0 ? (v / (double)total) * barH : 0;

        return new ChartBar
        {
            Date = date,
            ShortDate = FmtDate(date),
            Total = total,
            BarHeight = barH,
            HitSegH = seg(hit),
            MissSegH = seg(miss),
            RespSegH = seg(resp),
            LabelText = total > 0 ? FmtShort(total) : ""
        };
    }

    private static List<DailyUsage> Recent7Days(List<DailyUsage> days, DateTime now)
    {
        var end = now.ToString("yyyy-MM-dd");
        var all = days.Where(d => string.Compare(d.Date, end) <= 0).OrderBy(d => d.Date).ToList();
        return all.TakeLast(7).ToList();
    }

    private static string FmtDate(string date) => DateTime.TryParse(date, out var d) ? $"{d.Month}/{d.Day}" : date;
    private static string FmtShort(long n) => n >= 100_000_000 ? $"{(double)n / 1_000_000:F0}M" : n >= 1_000_000 ? $"{(double)n / 1_000_000:F1}M" : n >= 1_000 ? $"{(double)n / 1_000:F1}K" : n.ToString();
}

public sealed class ChartBar
{
    public string Date { get; set; } = "";
    public string ShortDate { get; set; } = "";
    public long Total { get; set; }
    public string LabelText { get; set; } = "";
    public double BarHeight { get; set; }
    public double HitSegH { get; set; }
    public double MissSegH { get; set; }
    public double RespSegH { get; set; }
}
