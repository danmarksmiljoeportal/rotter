using Newtonsoft.Json;
using Rotte.WsTrust;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WsFed.WpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            UpdateState();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var partnerUrl = ConfigurationManager.AppSettings["PartnerUrl"];
            cbPartners.ItemsSource = await PartnerConfiguration.ReadAsync(partnerUrl);
        }

        private void cbPartners_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedValue = cbPartners.SelectedValue as Partnerorg;
            if (selectedValue == null)
            {
                txtIdentity.Text = string.Empty;
                txtWindowsEndpoint.Text = string.Empty;
            }
            else
            {
                txtIdentity.Text = selectedValue.Identity;
                txtWindowsEndpoint.Text = selectedValue.WindowsEndpoint;
            }

            UpdateState();
        }

        private void UpdateState()
        {
            btnLoginUP.IsEnabled = !string.IsNullOrEmpty(txtUsername.Text)
                && !string.IsNullOrEmpty(txtPassword.Password);
            btnLoginPO.IsEnabled = !string.IsNullOrEmpty(txtIdentity.Text)
               && !string.IsNullOrEmpty(txtWindowsEndpoint.Text);
        }

        private void btnLoginWithUsernamePassword_Click(object sender, RoutedEventArgs e)
        {
            txtLogs.Text = string.Empty;
            txtLogs.AppendText("Login with Username/Password: ");
            txtLogs.AppendText(Environment.NewLine);
            try
            {
                var timeout = TimeSpan.FromMinutes(5);

                var securityToken = WsFactory.GetIdentifyUserToken(
                    txtUsername.Text, txtPassword.Password);

                var rotteWS = WsFactory.GetRotteWS(securityToken, timeout);

                var claims = rotteWS.GetActAsClaims();

                txtLogs.AppendText("GetActAsClaims response: ");
                txtLogs.AppendText(Environment.NewLine);
                txtLogs.AppendText(JsonConvert.SerializeObject(claims, Formatting.Indented));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                txtLogs.AppendText("Error: ");
                txtLogs.AppendText(Environment.NewLine);
                txtLogs.AppendText(ex.ToString());
            }
        }

        private void btnLoginWithWindowsAuth_Click(object sender, RoutedEventArgs e)
        {
            txtLogs.Text = string.Empty;
            txtLogs.AppendText("Login with Windows Auth(ADFS): ");
            txtLogs.AppendText(Environment.NewLine);
            try
            {
                var timeout = TimeSpan.FromMinutes(5);

                var localWindowsToken = WsFactory.GetLocalWindowsToken(
                    txtWindowsEndpoint.Text, txtIdentity.Text);

                var securityToken = WsFactory.ExchangeLocalWindowsTokenToIdentifyToken(
                    txtWindowsEndpoint.Text, txtIdentity.Text,
                    localWindowsToken);

                var rotteWS = WsFactory.GetRotteWS(securityToken, timeout);

                var claims = rotteWS.GetActAsClaims();

                txtLogs.AppendText("GetActAsClaims response: ");
                txtLogs.AppendText(Environment.NewLine);
                txtLogs.AppendText(JsonConvert.SerializeObject(claims, Formatting.Indented));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                txtLogs.AppendText("Error: ");
                txtLogs.AppendText(Environment.NewLine);
                txtLogs.AppendText(ex.ToString());
            }
        }
    }
}
