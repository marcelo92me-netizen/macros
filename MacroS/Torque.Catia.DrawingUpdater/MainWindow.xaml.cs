using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;
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
    public ObservableCollection<HistoryItem> UpdateHistory { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        LoadHistory();
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

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateForm()) return;
        var files = ListaArquivos.Items.Cast<DrawingFile>().Select(f => f.FullPath).ToArray();
        if (files.Length == 0) { Warn("Adicione pelo menos um arquivo CATDrawing à lista."); return; }
        var reportPath = Path.Combine(_selectedFolder.Length > 0 ? _selectedFolder : Path.GetDirectoryName(files[0])!, $"Relatorio_Atualizacao_Drawings_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
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
                    if (report.Contains("FIM DO PROCESSAMENTO"))
                    {
                        UpdateHistoryFromReport(report);
                        Resultado.Text = "Processamento encerrado. Relatório: " + reportPath;
                        System.Windows.MessageBox.Show("A macro terminou.\n\n" + GetSummary(report) + "\n\nRelatório completo:\n" + reportPath,
                            "Atualização de Drawings", MessageBoxButton.OK, report.Contains("[ERRO]") ? MessageBoxImage.Warning : MessageBoxImage.Information);
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
            DataProjeto.SelectedDate!.Value.ToString("dd/MM/yyyy"), Solicitante.Text.Trim(), Art.Text.Trim(),
            reportPath, files.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
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
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(Descricao.Text)) missing.Add("Descrição do projeto");
        if (string.IsNullOrWhiteSpace(Codigo.Text)) missing.Add("Código do projeto");
        if (string.IsNullOrWhiteSpace(Versao.Text)) missing.Add("Versão");
        if (DataProjeto.SelectedDate is null) missing.Add("Data do projeto");
        if (string.IsNullOrWhiteSpace(Solicitante.Text)) missing.Add("Solicitante");
        if (missing.Count == 0) return true;
        Warn("Preencha os campos obrigatórios: " + string.Join(", ", missing)); return false;
    }

    private static string GetSummary(string report) => string.Join(Environment.NewLine, report.Split('\n').Where(l => l.Contains("Encontrados:") || l.Contains("Atualizados:") || l.Contains("Com erro:")));
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

    private void UpdateHistoryFromReport(string report)
    {
        var time = DateTime.Now;
        foreach (var line in report.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("Início: ", StringComparison.OrdinalIgnoreCase) && DateTime.TryParse(line[8..].Trim(), out var parsed)) time = parsed;
        }
        foreach (var line in report.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.StartsWith("[OK] ", StringComparison.Ordinal)) continue;
            var fullPath = line[5..].Trim();
            for (int i = UpdateHistory.Count - 1; i >= 0; i--)
                if (string.Equals(UpdateHistory[i].FullPath, fullPath, StringComparison.OrdinalIgnoreCase)) UpdateHistory.RemoveAt(i);
            UpdateHistory.Insert(0, new HistoryItem(Path.GetFileName(fullPath), fullPath, time.ToString("dd/MM/yyyy HH:mm")));
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
    private void Warn(string message) => System.Windows.MessageBox.Show(message, "Atualizador de Drawings", MessageBoxButton.OK, MessageBoxImage.Warning);
    private sealed record DrawingFile(string Name, string FullPath);
    public sealed record HistoryItem(string FileName, string FullPath, string UpdatedAt);
}
