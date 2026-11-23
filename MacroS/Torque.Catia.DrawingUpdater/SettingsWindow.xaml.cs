using System.Windows;
using System.Windows.Controls;

namespace Torque.Catia.DrawingUpdater;

public partial class SettingsWindow : Window
{
    public const string DefaultProfileName = "Padrão";
    private readonly string[] _defaults;
    private bool _loading;
    public List<FieldProfile> Profiles { get; }
    public string SelectedProfile => Convert.ToString(ProfileSelector.SelectedItem) ?? DefaultProfileName;
    public string[] SelectedFieldNames => SelectedProfile == DefaultProfileName
        ? (string[])_defaults.Clone()
        : ReadFieldNames();

    public SettingsWindow(string selectedProfile, IEnumerable<FieldProfile> profiles, string[] defaults)
    {
        InitializeComponent();
        _defaults = (string[])defaults.Clone();
        Profiles = profiles.Select(p => new FieldProfile(p.Name, p.FieldNames)).ToList();
        _loading = true;
        ProfileSelector.Items.Add(DefaultProfileName);
        foreach (var profile in Profiles) ProfileSelector.Items.Add(profile.Name);
        ProfileSelector.SelectedItem = ProfileSelector.Items.Contains(selectedProfile) ? selectedProfile : DefaultProfileName;
        _loading = false;
        LoadSelectedProfile();
    }

    private IEnumerable<(System.Windows.Controls.TextBox TextBox, string Default)> FieldBoxes()
    {
        yield return (CampoDesc, _defaults[0]);
        yield return (CampoCodigo, _defaults[1]);
        yield return (CampoVersao, _defaults[2]);
        yield return (CampoData, _defaults[3]);
        yield return (CampoSolicitante, _defaults[4]);
        yield return (CampoArt, _defaults[5]);
        yield return (CampoVersaoArt, _defaults[6]);
        yield return (CampoDataArt, _defaults[7]);
        yield return (CampoEmpresaArt, _defaults[8]);
    }

    private string[] ReadFieldNames() => FieldBoxes().Select(x => x.TextBox.Text.Trim()).ToArray();

    private void ProfileSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loading) LoadSelectedProfile();
    }

    private void LoadSelectedProfile()
    {
        var selected = SelectedProfile;
        var profile = Profiles.FirstOrDefault(p => p.Name == selected);
        var values = profile?.FieldNames ?? _defaults;
        int i = 0;
        foreach (var field in FieldBoxes())
        {
            field.TextBox.Text = values.Length == _defaults.Length ? values[i] : field.Default;
            field.TextBox.IsReadOnly = selected == DefaultProfileName;
            field.TextBox.Background = selected == DefaultProfileName ? System.Windows.Media.Brushes.WhiteSmoke : System.Windows.Media.Brushes.White;
            i++;
        }
        ProfileHint.Text = selected == DefaultProfileName
            ? "Este perfil é fixo e usa os nomes definidos originalmente na ferramenta."
            : "Edite os nomes deste perfil e salve para atualizar ou crie outro perfil com um novo nome.";
        ExcluirPerfilButton.IsEnabled = selected != DefaultProfileName;
    }

    private void Salvar_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedProfile != DefaultProfileName)
        {
            if (!ValidateFieldNames()) return;
            var profile = Profiles.First(p => p.Name == SelectedProfile);
            profile.FieldNames = ReadFieldNames();
        }
        DialogResult = true;
    }

    private void CriarPerfil_Click(object sender, RoutedEventArgs e)
    {
        var profileName = NewProfileName.Text.Trim();
        if (string.IsNullOrWhiteSpace(profileName))
        {
            System.Windows.MessageBox.Show("Digite um nome para o novo perfil.", "Novo perfil", MessageBoxButton.OK, MessageBoxImage.Warning);
            NewProfileName.Focus();
            System.Windows.Input.Keyboard.Focus(NewProfileName);
            return;
        }
        if (profileName.Equals(DefaultProfileName, StringComparison.OrdinalIgnoreCase) ||
            Profiles.Any(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase)))
        {
            System.Windows.MessageBox.Show("Já existe um perfil com esse nome. Escolha outro nome.", "Novo perfil", MessageBoxButton.OK, MessageBoxImage.Warning);
            NewProfileName.Focus();
            NewProfileName.SelectAll();
            return;
        }
        if (!ValidateFieldNames()) return;
        var profile = new FieldProfile(profileName, ReadFieldNames());
        Profiles.Add(profile);
        _loading = true;
        ProfileSelector.Items.Add(profile.Name);
        ProfileSelector.SelectedItem = profile.Name;
        _loading = false;
        LoadSelectedProfile();
        NewProfileName.Clear();
        DialogResult = true;
    }

    private void ExcluirPerfil_Click(object sender, RoutedEventArgs e)
    {
        var selected = SelectedProfile;
        if (selected == DefaultProfileName) return;
        var answer = System.Windows.MessageBox.Show(
            $"Excluir o perfil \"{selected}\"? Essa ação não pode ser desfeita.",
            "Excluir perfil", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes) return;

        Profiles.RemoveAll(p => p.Name.Equals(selected, StringComparison.OrdinalIgnoreCase));
        _loading = true;
        ProfileSelector.Items.Remove(selected);
        ProfileSelector.SelectedItem = DefaultProfileName;
        _loading = false;
        LoadSelectedProfile();
    }

    private bool ValidateFieldNames()
    {
        var names = ReadFieldNames();
        if (names.Any(string.IsNullOrWhiteSpace))
        {
            System.Windows.MessageBox.Show("Preencha o Feature Name de todos os campos.", "Configurações", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
        {
            System.Windows.MessageBox.Show("Cada campo precisa ter um Feature Name diferente.", "Configurações", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }
}
