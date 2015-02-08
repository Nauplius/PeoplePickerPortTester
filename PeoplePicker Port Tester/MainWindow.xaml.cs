using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace PeoplePicker_Port_Tester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int SocketTimeOut = 200;
        private DateTime dateTime;

        public MainWindow()
        {
            InitializeComponent();
            rbtnFindAll.IsChecked = true;
        }

        private void Connect(object sender, RoutedEventArgs e)
        {
            dateTime = DateTime.Now.ToLocalTime();
            string ldapFilter = txtLDAPFilter.Text;
            string ldapPath = txtLDAPPath.Text;
            string userName = txtUserName.Text;
            SecureString password = txtPassword.SecurePassword;
            int ldapPort = Convert.ToInt32(txtLDAPPort.Text);
            int ldapsPort = Convert.ToInt32(txtLDAPSPort.Text);
            bool findOne = rbtnFindAll.IsChecked != true;
            string dnsName = ParseLdapPathToDnsName(ldapPath);

            //Clear textbox in case of n+1 runs
            tbDns.Clear();
            tbLdap.Clear();
            tbPorts.Clear();

            var br1 = new BackgroundWorker();
            br1.DoWork += (o, args) => TestDns(dnsName);
            br1.RunWorkerAsync(1000);
            
            var br2 = new BackgroundWorker();
            br2.DoWork += (o, args) => TestPorts(dnsName, ldapPort, ldapsPort);
            br2.RunWorkerAsync(1000);
 

            var br3 = new BackgroundWorker();
            br3.DoWork += (o, args) => TestLookup(ldapPath, ldapFilter, userName, password, findOne);
            br3.RunWorkerAsync(1000);
        }

        private void TestDns(string dnsName)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    tbDns.Background = new SolidColorBrush(Colors.LightGoldenrodYellow);
                }));
            try
            {
                IPAddress[] IPs = Dns.GetHostAddresses(dnsName);

                foreach (IPAddress ip in IPs)
                {
                    IPAddress ip1 = ip;
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        var hostName = Dns.GetHostEntry(ip1);
                        tbDns.Text = tbDns.Text + string.Format("{0} [{1}] \r\n", ip1, hostName.HostName);// + ip1 + "\r\n";
                        }));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        tbDns.Text = tbDns.Text + ex.Message + "\r\n";
                    }));
            }
            finally
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        tbDns.Background = new SolidColorBrush(Colors.Azure);
                    }));
            }
        }

        private void TestPorts(string dnsName, int ldapPort, int ldapsPort)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    tbPorts.Background = new SolidColorBrush(Colors.LightGoldenrodYellow);
                }));
            int[] tcpPorts =
                {
                    ldapPort, ldapsPort, 135, 137, 138, 139,
                    3268, 3269, 53, 88, 445, 749, 750
                };

            int[] udpPorts =
                {
                    53, 88, 135, 137, 138, ldapPort, 445, 749
                };

            try
            {
                foreach(int tport in tcpPorts)
                {
                    try
                    {
                        string opt = "";

                        if (tport == 749 || tport == 750)
                        {
                            opt = "[Opt]";
                        }

                        Socket socket = null;
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, false);
                        IAsyncResult result = socket.BeginConnect(dnsName, tport, null, null);
                        bool connected = result.AsyncWaitHandle.WaitOne(SocketTimeOut, true);
                        if (connected)
                        {
                            int tport1 = tport;
                            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                            {
                                tbPorts.Text = tbPorts.Text + string.Format("{0}TCP/{1} connected \r\n", opt, tport1);// "Connected to tcp/" + tport1 + "\r\n";
                            }));
                            socket.Close();
                        }
                        else
                        {
                            int tport1 = tport;
                            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                            {
                                tbPorts.Text = tbPorts.Text + string.Format("{0}TCP/{1} connection failed \r\n", opt, tport1); ;
                            }));
                            socket.Close();
                        }
                    }
                    catch (Exception exception)
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            tbPorts.Text = tbPorts.Text +
                                           string.Format("TCP/{0} encountered exception: {1} \r\n", tport,
                                               exception.Message);
                        }));
                    }
                }
            }
            catch (Exception exception)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    tbPorts.Text = tbPorts.Text + exception.Message + "\r\n";
                }));
            }


            try
            {
                Byte[] bytes = Encoding.ASCII.GetBytes("1234567890");
                var remoteIPEP = new IPEndPoint(IPAddress.Any, 0);
                Byte[] receiveData;
                foreach (int uport in udpPorts)
                {
                    string opt = "";

                    if (uport == 0)
                    {
                        opt = "[Opt]";
                    }

                    var udpClient = new UdpClient(uport, AddressFamily.InterNetwork);

                    Socket socket = udpClient.Client;
                    socket.ReceiveTimeout = 1000;
                    udpClient.Connect(dnsName, uport);
                    udpClient.Send(bytes, bytes.Length);

                    try
                    {
                        receiveData = udpClient.Receive(ref remoteIPEP);

                        if (receiveData != null)
                        {
                            int uport1 = uport;
                            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                            {
                                tbPorts.Text = tbPorts.Text + string.Format("{0}UDP/{1} connected \r\n", opt, uport1);
                            }));

                            socket.Close();
                            Thread.Sleep(1000);
                        }
                    }
                    catch (Exception exception)
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                            {
                                tbPorts.Text = tbPorts.Text + string.Format("{0}UDP/{1} encountered exception: {2} \r\n", opt, uport, exception.Message);
                            }));
                        socket.Close();
                    }
                    finally
                    {
                        udpClient.Close();
                    }
                }
            }
            catch (Exception exception)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        tbPorts.Text = tbPorts.Text + exception.Message + "\r\n";
                    }));
            }
            finally
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        tbPorts.Background = new SolidColorBrush(Colors.Azure);
                    }));
            }
        }

        private string ParseLdapPathToDnsName(string ldapPath)
        {
            string[] split = ldapPath.Split(',');

            string dnsName = String.Join(".", (from pair in split select pair.Split('=') 
                                                   into keyValue where keyValue[0].ToUpper() == ("DC") || 
                                                   keyValue[0].ToUpper() == "LDAP://DC" 
                                               select keyValue[1]).ToArray());
            return dnsName;
        }

        private void TestLookup(string ldapPath, string ldapFilter, string userName, SecureString password, bool findOne)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    tbLdap.Background = new SolidColorBrush(Colors.LightGoldenrodYellow);
                }));
            var de = new DirectoryEntry(ldapPath.ToUpper());

            if(userName == String.Empty)
            {
                de.AuthenticationType = AuthenticationTypes.Secure;              
            }
            else
            {
                IntPtr bstr = Marshal.SecureStringToBSTR(password);
                de.Username = userName;
                de.Password = Marshal.PtrToStringBSTR(bstr);
                Marshal.FreeBSTR(bstr);
                de.AuthenticationType = AuthenticationTypes.Secure;
            }

            var ds = new DirectorySearcher(de) {Filter = ldapFilter, SearchScope = SearchScope.Subtree, PageSize = 1000};

            try
            {
                if (findOne)
                {
                    SearchResult sr = ds.FindOne();
                    de = sr.GetDirectoryEntry();
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            tbLdap.Text = tbLdap.Text + de.Name + Environment.NewLine +
                                             de.Path + "\r\n";
                        }));
                }
                else
                {
                    SearchResultCollection rsc = ds.FindAll();
                    foreach (SearchResult sr in rsc)
                    {
                        SearchResult sr1 = sr;
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                            {
                                tbLdap.Text = tbLdap.Text + sr1.Path + "\r\n";
                            }));
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        tbLdap.Text = tbLdap.Text + ex.Message + "\r\n";
                    }));
            }
            finally
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    tbLdap.Background = new SolidColorBrush(Colors.Azure);
                }));
            }
        }

        public void OnClick(object sender, RoutedEventArgs e)
        {
            var nTextBox = (TextBox) sender;
            if (nTextBox.Text == "LDAP://CN=Users,DC=contoso,DC=com")
            {
                nTextBox.Text = "";   
            }
        }

        public void SaveOutputClick(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog {DefaultExt = ".txt", Filter = "Text Documents (*.txt)|*.txt"};

            var result = dlg.ShowDialog();

            if (result != true) return;
            var filename = dlg.FileName;
            using (var streamWriter = new StreamWriter(filename))
            {
                streamWriter.Write(dateTime + "\r\n");
                streamWriter.Write("Parameters used: \r\n");
                streamWriter.Write("LDAP Filter: {0} \r\n", txtLDAPFilter.Text);
                streamWriter.Write("LDAP Path: {0} \r\n", txtLDAPPath.Text);
                streamWriter.Write("LDAP Port: {0} \r\n", txtLDAPPort.Text);
                streamWriter.Write("LDAPS Port: {0} \r\n", txtLDAPSPort.Text);
                streamWriter.Write("Username: {0} \r\n", txtUserName.Text);
                streamWriter.WriteLine("================\r\n");
                streamWriter.Write("DNS Test: \r\n{0} \r\n", tbDns.Text);
                streamWriter.WriteLine("================\r\n");
                streamWriter.Write("Port Test: \r\n{0} \r\n", tbPorts.Text);
                streamWriter.WriteLine("================\r\n");
                streamWriter.Write("LDAP Test: \r\n{0} \r\n", tbLdap.Text);
            }
        }

        public void AboutClick(object sender, RoutedEventArgs e)
        {
            var aboutDialog = new AboutDialogBox();
            aboutDialog.ShowDialog();
        }

        public void ExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}