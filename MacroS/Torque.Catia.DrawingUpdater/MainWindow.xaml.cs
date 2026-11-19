using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Torque.Catia.DrawingUpdater;

public partial class MainWindow : Window
{
    private string _selectedFolder = "";
    private const string EnvFolder = @"C:\ProgramData\DassaultSystemes\CATEnv";
    private const string InstallRoot = @"C:\Program Files\Dassault Systemes";
    private static string JobPath => Path.Combine(Path.GetTempPath(), "TorqueDrawingUpdater_job.txt");
    private static string HistoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Torque", "DrawingUpdater", "history.json");
    private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Torque", "DrawingUpdater", "settings.json");
    private static readonly string[] DefaultFieldNames = ["DESC_PROJETO", "CODIGO_PROJETO", "VERSAO_PROJETO", "DATA_PROJETO", "SOLICITANTE_PROJETO", "ART_PROJETO", "VERSAO_ART", "DATA_ART", "EMPRESA_ART"];
    private AppSettings _settings = new();
    public ObservableCollection<HistoryItem> UpdateHistory { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        LoadSettings();
        LoadHistory();
        RefreshInputBadges();
    }

    private void Campo_TextChanged(object sender, TextChangedEventArgs e) => RefreshInputBadges();
    private void DataProjeto_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => RefreshInputBadges();

    private void RefreshInputBadges()
    {
        SetInputBadge(BadgeDescricao, BadgeDescricaoTexto, !string.IsNullOrWhiteSpace(Descricao.Text));
        SetInputBadge(BadgeCodigo, BadgeCodigoTexto, !string.IsNullOrWhiteSpace(Codigo.Text));
        SetInputBadge(BadgeVersao, BadgeVersaoTexto, !string.IsNullOrWhiteSpace(Versao.Text));
        SetInputBadge(BadgeData, BadgeDataTexto, DataProjeto.SelectedDate.HasValue);
        SetInputBadge(BadgeSolicitante, BadgeSolicitanteTexto, !string.IsNullOrWhiteSpace(Solicitante.Text));
        SetInputBadge(BadgeArt, BadgeArtTexto, !string.IsNullOrWhiteSpace(Art.Text));
    }

    private static void SetInputBadge(System.Windows.Controls.Border badge, System.Windows.Controls.TextBlock text, bool hasValue)
    {
        badge.Background = hasValue ? System.Windows.Media.Brushes.Honeydew : System.Windows.Media.Brushes.Gainsboro;
        text.Text = hasValue ? "Será atualizado" : "Ignorado";
        text.Foreground = hasValue
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 101, 52))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
    }

    private void ConfigurarCampos_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings.UseCustomFields, _settings.CustomFieldNames) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        _settings.UseCustomFields = dialog.UseCustomFields;
        _settings.CustomFieldNames = dialog.FieldNames;
        SaveSettings();
    }

    private void EscolherPasta_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Selecione a pasta raiz dos arquivos CATDrawing", UseDescriptionForTitle = true, ShowNewFolderButton = false };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) LoadFolder(dialog.SelectedPath);
    }

    private void AdicionarArquivos_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Adicionar arquivos CATDrawing", Filter = "CATIA Drawings (*.CATDrawing)|*.CATDrawing", Multiselect = true };
        if (dialog.ShowDialog() == true) AddFiles(dialog.FileNames);
    }

    private void LoadFolder(string folder)
    {
        _selectedFolder = folder;
        Pasta.Text = folder;
        ListaArquivos.Items.Clear();
        AddFiles(Directory.GetFiles(folder, "*.CATDrawing", SearchOption.AllDirectories));
        ResultCount();
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        var existing = ListaArquivos.Items.Cast<DrawingFile>().Select(f => f.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
            if (existing.Add(path)) ListaArquivos.Items.Add(new DrawingFile(Path.GetFileName(path), path));
        ResultCount();
    }

    private void RemoverArquivos_Click(object sender, RoutedEventArgs e)
    {
        var selected = ListaArquivos.SelectedItems.Cast<object>().ToArray();
        foreach (var item in selected) ListaArquivos.Items.Remove(item);
        ResultCount();
    }

    private void LimparArquivos_Click(object sender, RoutedEventArgs e) { ListaArquivos.Items.Clear(); ResultCount(); }
    private void ResultCount() => Contagem.Text = $"{ListaArquivos.Items.Count} arquivo(s) na lista";

    private void LoadSettings()
    {
        try { if (File.Exists(SettingsPath)) _settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings(); }
        catch { _settings = new AppSettings(); }
        if (_settings.CustomFieldNames is null || _settings.CustomFieldNames.Length != DefaultFieldNames.Length)
            _settings.CustomFieldNames = (string[])DefaultFieldNames.Clone();
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { Resultado.Text = "Não foi possível salvar as configurações: " + ex.Message; }
    }

    private string[] GetCurrentFieldNames() => _settings.UseCustomFields
        ? _settings.CustomFieldNames.Select(n => n.Trim()).ToArray()
        : (string[])DefaultFieldNames.Clone();

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateForm()) return;
        var files = ListaArquivos.Items.Cast<DrawingFile>().Select(f => f.FullPath).ToArray();
        if (files.Length == 0) { Warn("Adicione pelo menos um arquivo CATDrawing à lista."); return; }
        var reportPath = Path.Combine(_selectedFolder.Length > 0 ? _selectedFolder : Path.GetDirectoryName(files[0])!, $"Relatorio_Atualizacao_Drawings_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        foreach (var item in ListaArquivos.Items.Cast<DrawingFile>()) { item.Status = "Na fila"; item.Details = "Aguardando execução"; }
        try
        {
            WriteJob(files, reportPath);
            var (cnext, environment) = FindCatia();
            string macro = Path.Combine(AppContext.BaseDirectory, "ProcessarDrawings.CATScript");
            if (!File.Exists(macro)) throw new FileNotFoundException("CATScript de processamento não encontrado junto à aplicação.", macro);
            var start = new ProcessStartInfo(cnext) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(cnext)! };
            start.ArgumentList.Add("-env"); start.ArgumentList.Add(environment);
            start.ArgumentList.Add("-direnv"); start.ArgumentList.Add(EnvFolder);
            start.ArgumentList.Add("-macro"); start.ArgumentList.Add(macro);
            Atualizar.IsEnabled = false;
            Resultado.Text = "Iniciando o CATIA e aguardando a macro…";
            using var launched = Process.Start(start) ?? throw new InvalidOperationException("Não foi possível iniciar o CATIA.");
            for (int i = 0; i < 1800; i++)
            {
                await Task.Delay(1000);
                if (File.Exists(reportPath))
                {
                    string report = File.ReadAllText(reportPath);
                    UpdateFileResults(report);
                    if (report.Contains("FIM DO PROCESSAMENTO"))
                    {
                        UpdateHistoryFromReport(report, reportPath);
                        Resultado.Text = "Processamento encerrado. Relatório: " + reportPath;
                        System.Windows.MessageBox.Show("A macro terminou.\n\n" + GetSummary(report) + "\n\nRelatório completo:\n" + reportPath,
                            "Atualização de Drawings", MessageBoxButton.OK, report.Contains("[ERRO]") || report.Contains("[ATENÇÃO]") ? MessageBoxImage.Warning : MessageBoxImage.Information);
                        return;
                    }
                }
                if (launched.HasExited && !File.Exists(reportPath)) throw new InvalidOperationException($"O CATIA encerrou antes de criar o relatório (código {launched.ExitCode}). Consulte o ambiente CATIA: {environment}.");
                Resultado.Text = $"CATIA processando a lista · {i + 1}s";
            }
            Warn("A macro ainda não concluiu após 30 minutos. Verifique a janela do CATIA e o arquivo de relatório.");
        }
        catch (Exception ex) { Warn("Não foi possível executar a atualização: " + ex.Message); }
        finally { Atualizar.IsEnabled = true; }
    }

    private void WriteJob(string[] files, string reportPath)
    {
        var lines = new List<string>
        {
            Descricao.Text.Trim(), Codigo.Text.Trim(), Versao.Text.Trim(),
            DataProjeto.SelectedDate?.ToString("dd/MM/yyyy") ?? "", Solicitante.Text.Trim(), Art.Text.Trim(),
            reportPath
        };
        lines.AddRange(GetCurrentFieldNames());
        lines.Add(files.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        lines.AddRange(files);
        File.WriteAllLines(JobPath, lines, System.Text.Encoding.Unicode);
    }

    private static (string Cnext, string Environment) FindCatia()
    {
        if (!Directory.Exists(EnvFolder) || !Directory.Exists(InstallRoot)) throw new DirectoryNotFoundException("Instalação CATIA ou pasta de ambientes CATEnv não localizada.");
        var envs = Directory.GetFiles(EnvFolder, "CATIA*.txt");
        foreach (var envFile in envs.OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var environment = Path.GetFileNameWithoutExtension(envFile);
            var version = environment.Split('.').LastOrDefault();
            if (string.IsNullOrWhiteSpace(version)) continue;
            var cnext = Path.Combine(InstallRoot, version, "win_b64", "code", "bin", "CNEXT.exe");
            if (File.Exists(cnext)) return (cnext, environment);
        }
        throw new FileNotFoundException("Não foi encontrado CNEXT.exe correspondente aos ambientes CATIA em " + EnvFolder);
    }

    private bool ValidateForm()
    {
        bool hasAnyValue = !string.IsNullOrWhiteSpace(Descricao.Text) ||
            !string.IsNullOrWhiteSpace(Codigo.Text) || !string.IsNullOrWhiteSpace(Versao.Text) ||
            DataProjeto.SelectedDate.HasValue || !string.IsNullOrWhiteSpace(Solicitante.Text) ||
            !string.IsNullOrWhiteSpace(Art.Text);
        if (!hasAnyValue)
        {
            Warn("Preencha pelo menos uma informação para atualizar.");
            return false;
        }
        if (_settings.UseCustomFields)
        {
            var names = GetCurrentFieldNames();
            if (names.Any(string.IsNullOrWhiteSpace))
            {
                Warn("Preencha todos os Feature Names personalizados.");
                return false;
            }
            if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
            {
                Warn("Cada campo personalizado precisa ter um Feature Name diferente.");
                return false;
            }
        }
        return true;
    }

    private static string GetSummary(string report) => string.Join(Environment.NewLine, report.Split('\n').Where(l => l.Contains("Encontrados:") || l.Contains("Sucesso:") || l.Contains("Atenção:") || l.Contains("Com erro:")));

    private void UpdateFileResults(string report)
    {
        DrawingFile? current = null;
        var details = new List<string>();
        void Flush()
        {
            if (current is null) return;
            current.Details = details.Count == 0 ? "Sem detalhes reportados" : string.Join(" · ", details);
            current = null;
            details.Clear();
        }
        foreach (var rawLine in report.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            string? status = line.StartsWith("[SUCESSO] ", StringComparison.Ordinal) ? "Sucesso" :
                line.StartsWith("[ATENÇÃO] ", StringComparison.Ordinal) ? "Atenção" :
                line.StartsWith("[ERRO] ", StringComparison.Ordinal) ? "Erro" : null;
            if (status is not null)
            {
                Flush();
                var path = line[(status == "Sucesso" ? 10 : status == "Atenção" ? 10 : 7)..].Trim();
                current = ListaArquivos.Items.Cast<DrawingFile>().FirstOrDefault(f => string.Equals(f.FullPath, path, StringComparison.OrdinalIgnoreCase));
                if (current is not null) current.Status = status;
                continue;
            }
            if (current is not null && (line.StartsWith("Campos atualizados:", StringComparison.Ordinal) ||
                line.StartsWith("Campos com erro:", StringComparison.Ordinal) ||
                line.StartsWith("Campos obrigatórios ausentes:", StringComparison.Ordinal) ||
                line.StartsWith("Campos opcionais ausentes:", StringComparison.Ordinal) ||
                line.StartsWith("Erro de execução:", StringComparison.Ordinal))) details.Add(line);
        }
        Flush();
    }
    private void LoadHistory()
    {
        try
        {
            if (File.Exists(HistoryPath))
                foreach (var item in JsonSerializer.Deserialize<List<HistoryItem>>(File.ReadAllText(HistoryPath)) ?? []) UpdateHistory.Add(item);
        }
        catch { UpdateHistory.Clear(); }
        HistoricoVazio.Visibility = UpdateHistory.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void HistoricoLista_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HistoricoLista.SelectedItem is not HistoryItem item) return;
        new HistoryDetailsWindow(item) { Owner = this }.ShowDialog();
        HistoricoLista.SelectedItem = null;
    }

    private void UpdateHistoryFromReport(string report, string reportPath)
    {
        var time = DateTime.Now;
        foreach (var line in report.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            if (line.StartsWith("Início: ", StringComparison.OrdinalIgnoreCase) && DateTime.TryParse(line[8..].Trim(), out var parsed)) time = parsed;

        foreach (var record in ParseHistoryItems(report, time, reportPath))
        {
            for (int i = UpdateHistory.Count - 1; i >= 0; i--)
                if (string.Equals(UpdateHistory[i].FullPath, record.FullPath, StringComparison.OrdinalIgnoreCase)) UpdateHistory.RemoveAt(i);
            UpdateHistory.Insert(0, record);
        }
        while (UpdateHistory.Count > 20) UpdateHistory.RemoveAt(UpdateHistory.Count - 1);
        HistoricoVazio.Visibility = UpdateHistory.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(HistoryPath)!);
            File.WriteAllText(HistoryPath, JsonSerializer.Serialize(UpdateHistory.ToList(), new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { Resultado.Text = "Histórico exibido, mas não foi possível salvá-lo: " + ex.Message; }
    }

    private static List<HistoryItem> ParseHistoryItems(string report, DateTime time, string reportPath)
    {
        var results = new List<HistoryItem>();
        HistoryItem? current = null;
        var notUpdated = new List<string>();
        void Flush()
        {
            if (current is null) return;
            current.FieldsNotUpdated = notUpdated.Count == 0 ? "Nenhum campo pendente ou com erro." : string.Join("\n", notUpdated);
            results.Add(current);
            current = null;
            notUpdated.Clear();
        }
        foreach (var raw in report.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            string? marker = line.StartsWith("[SUCESSO] ", StringComparison.Ordinal) ? "[SUCESSO] " :
                line.StartsWith("[ATENÇÃO] ", StringComparison.Ordinal) ? "[ATENÇÃO] " :
                line.StartsWith("[ERRO] ", StringComparison.Ordinal) ? "[ERRO] " : null;
            if (marker is not null)
            {
                Flush();
                var path = line[marker.Length..].Trim();
                var status = marker.StartsWith("[SUCESSO", StringComparison.Ordinal) ? "Sucesso" :
                    marker.StartsWith("[ATENÇÃO", StringComparison.Ordinal) ? "Atenção" : "Erro";
                current = new HistoryItem(Path.GetFileName(path), path, time.ToString("dd/MM/yyyy HH:mm"), status, "Nenhum campo informado.", "Nenhum campo pendente ou com erro.", reportPath);
                continue;
            }
            if (current is null) continue;
            if (line.StartsWith("Campos atualizados:", StringComparison.Ordinal)) current.FieldsUpdated = line["Campos atualizados:".Length..].Trim();
            else if (line.StartsWith("Campos preenchidos antes do erro:", StringComparison.Ordinal)) current.FieldsUpdated = line["Campos preenchidos antes do erro:".Length..].Trim();
            else if (line.StartsWith("Campos com erro:", StringComparison.Ordinal) ||
                     line.StartsWith("Campos obrigatórios ausentes:", StringComparison.Ordinal) ||
                     line.StartsWith("Campos opcionais ausentes:", StringComparison.Ordinal) ||
                     line.StartsWith("Erro de execução:", StringComparison.Ordinal)) notUpdated.Add(line);
        }
        Flush();
        return results;
    }
    private void Warn(string message) => System.Windows.MessageBox.Show(message, "Atualizador de Drawings", MessageBoxButton.OK, MessageBoxImage.Warning);
    private sealed class DrawingFile : INotifyPropertyChanged
    {
        private string _status = "Na fila";
        private string _details = "Aguardando execução";
        public DrawingFile(string name, string fullPath) { Name = name; FullPath = fullPath; }
        public string Name { get; }
        public string FullPath { get; }
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
        public string Details { get => _details; set { _details = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public sealed class HistoryItem
    {
        public HistoryItem() { }
        public HistoryItem(string fileName, string fullPath, string updatedAt, string status, string fieldsUpdated, string fieldsNotUpdated, string reportPath)
        {
            FileName = fileName; FullPath = fullPath; UpdatedAt = updatedAt; Status = status;
            FieldsUpdated = fieldsUpdated; FieldsNotUpdated = fieldsNotUpdated; ReportPath = reportPath;
        }
        public string FileName { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string UpdatedAt { get; set; } = "";
        public string Status { get; set; } = "Sucesso";
        public string FieldsUpdated { get; set; } = "Nenhum campo informado.";
        public string FieldsNotUpdated { get; set; } = "Nenhum campo pendente ou com erro.";
        public string ReportPath { get; set; } = "";
    }
    private sealed class AppSettings
    {
        public AppSettings() { }
        public bool UseCustomFields { get; set; }
        public string[] CustomFieldNames { get; set; } = (string[])DefaultFieldNames.Clone();
    }
}
