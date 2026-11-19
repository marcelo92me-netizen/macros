using System.Windows;
using System.Windows.Controls;

namespace Torque.Catia.DrawingUpdater;

public partial class SettingsWindow : Window
{
    private static readonly string[] Defaults = ["DESC_PROJETO", "CODIGO_PROJETO", "VERSAO_PROJETO", "DATA_PROJETO", "SOLICITANTE_PROJETO", "ART_PROJETO", "VERSAO_ART", "DATA_ART", "EMPRESA_ART"];
    private string[] _customNames = (string[])Defaults.Clone();
    private bool _wasCustom;
    public bool UseCustomFields => ModoPersonalizado.IsChecked == true;
    public string[] FieldNames => UseCustomFields
        ? FieldBoxes().Select(x => x.TextBox.Text.Trim()).ToArray()
        : (string[])_customNames.Clone();

    public SettingsWindow(bool useCustom, string[] customNames)
    {
        InitializeComponent();
        ModoPersonalizado.IsChecked = useCustom;
        ModoPadrao.IsChecked = !useCustom;
        var names = customNames.Length == Defaults.Length ? customNames : Defaults;
        _customNames = (string[])names.Clone();
        _wasCustom = useCustom;
        int i = 0;
        foreach (var field in FieldBoxes()) field.TextBox.Text = names[i++];
        ModoPadrao.Checked += ModeChanged;
        ModoPersonalizado.Checked += ModeChanged;
        RefreshMode();
    }

    private IEnumerable<(System.Windows.Controls.TextBox TextBox, string Default)> FieldBoxes()
    {
        yield return (CampoDesc, Defaults[0]);
        yield return (CampoCodigo, Defaults[1]);
        yield return (CampoVersao, Defaults[2]);
        yield return (CampoData, Defaults[3]);
        yield return (CampoSolicitante, Defaults[4]);
        yield return (CampoArt, Defaults[5]);
        yield return (CampoVersaoArt, Defaults[6]);
        yield return (CampoDataArt, Defaults[7]);
        yield return (CampoEmpresaArt, Defaults[8]);
    }

    private void ModeChanged(object sender, RoutedEventArgs e)
    {
        bool custom = UseCustomFields;
        if (_wasCustom && !custom) _customNames = FieldBoxes().Select(x => x.TextBox.Text.Trim()).ToArray();
        else if (!_wasCustom && custom)
        {
            int i = 0;
            foreach (var field in FieldBoxes()) field.TextBox.Text = _customNames[i++];
        }
        _wasCustom = custom;
        RefreshMode();
    }

    private void RefreshMode()
    {
        bool custom = UseCustomFields;
        int i = 0;
        foreach (var field in FieldBoxes())
        {
            if (!custom) field.TextBox.Text = field.Default;
            field.TextBox.IsReadOnly = !custom;
            field.TextBox.Background = custom ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.WhiteSmoke;
            i++;
        }
        DicaConfiguracao.Text = custom
            ? "Edite os Feature Names usados para localizar cada campo nos Drawings."
            : "Exibindo os nomes padrão definidos no desenvolvimento; eles serão usados nesta execução.";
    }

    private void Salvar_Click(object sender, RoutedEventArgs e)
    {
        if (UseCustomFields)
        {
            var names = FieldNames;
            if (names.Any(string.IsNullOrWhiteSpace))
            {
                System.Windows.MessageBox.Show("Preencha o Feature Name de todos os campos.", "Configurações", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
            {
                System.Windows.MessageBox.Show("Cada campo precisa ter um Feature Name diferente.", "Configurações", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        DialogResult = true;
    }
}
