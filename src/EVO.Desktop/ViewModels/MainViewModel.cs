using System.Collections.ObjectModel;
using EVO.Collectors.CPU;
using EVO.Collectors.GPU;
using EVO.Collectors.Memory;
using EVO.Collectors.Network;
using EVO.Collectors.Processes;
using EVO.Collectors.Windows;
using EVO.Core.Contracts;
using EVO.Core.Correlation;
using EVO.Core.Reports;
using EVO.Desktop.Commands;

namespace EVO.Desktop.ViewModels;

/// <summary>
/// ViewModel del dashboard: ejecuta el stack M01→M14 y expone el informe M15
/// al usuario. No decide causas — M14 produce causas probables; aquí se muestran.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private string _status = "Presiona RECOLECTAR para ejecutar el análisis.";
    private string _reportJson = "(sin informe todavía)";
    private string _cpuUsage = "—";
    private string _ramAvailable = "—";
    private string _healthEvents = "—";
    private string _gpuAdapters = "—";
    private string _networkBytes = "—";
    private string _findingsSummary = "—";
    private string _lastRun = "—";
    private bool _isRunning = false;

    private readonly ObservableCollection<FindingRow> _findings = new();

    public MainViewModel()
    {
        RunAnalysisCommand = new DelegateCommand(RunAnalysisAsync);
    }

    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public string ReportJson { get => _reportJson; private set => SetProperty(ref _reportJson, value); }
    public string CpuUsage { get => _cpuUsage; private set => SetProperty(ref _cpuUsage, value); }
    public string RamAvailable { get => _ramAvailable; private set => SetProperty(ref _ramAvailable, value); }
    public string HealthEvents { get => _healthEvents; private set => SetProperty(ref _healthEvents, value); }
    public string GpuAdapters { get => _gpuAdapters; private set => SetProperty(ref _gpuAdapters, value); }
    public string NetworkBytes { get => _networkBytes; private set => SetProperty(ref _networkBytes, value); }
    public string FindingsSummary { get => _findingsSummary; private set => SetProperty(ref _findingsSummary, value); }
    public string LastRun { get => _lastRun; private set => SetProperty(ref _lastRun, value); }
    public bool IsRunning { get => _isRunning; private set => SetProperty(ref _isRunning, value); }

    public ObservableCollection<FindingRow> Findings => _findings;
    public DelegateCommand RunAnalysisCommand { get; }

    private async Task RunAnalysisAsync()
    {
        if (IsRunning)
            return;

        IsRunning = true;
        Status = "Recolectando… (M02 / M03 / M05 / M06 / M11 / M12)";
        LastRun = "—";
        Findings.Clear();

        try
        {
            var cpu = await new CpuAnalyzerCollector().CollectAsync();
            var ram = await new RamAnalyzerCollector().CollectAsync();
            var gpu = await new GpuAnalyzerCollector().CollectAsync();
            var proc = await new ProcessAnalyzerCollector().CollectAsync();
            var network = await new NetworkAnalyzerCollector().CollectAsync();
            var health = await new WindowsHealthCollector().CollectAsync();

            CpuUsage = ExtractMetric(cpu, "cpu.usage_percent", "%");
            RamAvailable = ExtractMetric(ram, "ram.available_percent", "%");
            GpuAdapters = ExtractMetric(gpu, "gpu.adapters", "");
            NetworkBytes = ExtractMetric(network, "network.bytes_sent_mb_sec", "MB/s enviados");
            HealthEvents = ExtractMetric(health, "health.events_24h", "eventos 24h");

            // M14 — correlación de evidencias
            var sources = new ICollector[]
            {
                new CpuAnalyzerCollector(),
                new RamAnalyzerCollector(),
                new ProcessAnalyzerCollector(),
                new WindowsHealthCollector(),
            };
            var engine = new BottleneckEngineCollector(sources);
            var engineResult = await engine.CollectAsync();

            foreach (var finding in engineResult.Findings)
            {
                Findings.Add(new FindingRow(
                    finding.Category,
                    finding.Title,
                    finding.Confidence.ToString(),
                    finding.Severity.ToString()));
            }

            var corroborated = engineResult.Findings.Count(f => f.Confidence == EVO.Core.Findings.FindingConfidence.Probable);
            FindingsSummary = engineResult.Findings.Count == 0
                ? "Sin hallazgos correlacionados — equipo en rango esperado (instantánea)"
                : $"{engineResult.Findings.Count} hallazgo(s) — {corroborated} corroborado(s)";

            var report = ReportBuilder.Build(engineResult);
            ReportJson = report.ToJson();

            Status = "Recolección completada";
            LastRun = DateTimeOffset.Now.ToString("HH:mm:ss");
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    private static string ExtractMetric(CollectResult result, string name, string unit)
    {
        var metric = result.Metrics.FirstOrDefault(m => m.Name == name);
        if (metric is null)
            return "—";
        return string.IsNullOrEmpty(unit)
            ? $"{metric.Value:F0}"
            : $"{metric.Value:F1} {unit}";
    }
}
