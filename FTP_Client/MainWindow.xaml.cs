using Microsoft.Win32;
using System.IO;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace FTP_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _ip;
        private string _login;
        private string _password;

        public MainWindow()
        {
            InitializeComponent();


        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (TextBox_Ip.Text.Length < 7) return;
            _ip = TextBox_Ip.Text;
            _login = TextBox_Login.Text;
            _password = TextBox_Password.Text;

            var item = new TreeViewFileItem();
            LoadFtpStructure(_ip, item);
            TreeView_Dir.ItemsSource = item.Items;
        }

        private void LoadFtpStructure(string ip, TreeViewFileItem parent, string pathArg = "")
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($@"ftp://{_ip}{pathArg}");
            request.Credentials = new NetworkCredential(_login, _password);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());



            while (!reader.EndOfStream)
            {
                string path = reader.ReadLine();
                string[] pathSplit = path.Split(' ');
                string filename = pathSplit.Last();

                if (filename.Contains('.'))
                {
                    //TreeView_Dir.Items.Add(pathSplit.Last());
                    parent.Items.Add(new TreeViewFileItem() { Filename = filename, Tag = parent });
                }
                else
                {
                    parent.Items.Add(new TreeViewFileItem() { Filename = filename, Tag = parent });
                    LoadFtpStructure(ip, parent.Items[parent.Items.Count - 1], pathArg + '/' + filename);
                }
            }
        }
        private string GetSelectedPath() 
        {
            var selItem = (TreeViewFileItem)TreeView_Dir.SelectedItem;

            if (!selItem.Filename.Contains('.')) selItem = (TreeViewFileItem)selItem.Tag;


            List<string> path = new List<string>();
            while (selItem != null)
            {
                path.Add(selItem.Filename);
                selItem = (TreeViewFileItem)selItem.Tag;
            }
            path.Reverse();
            string resultPath = String.Join('/', path);
            return resultPath;
        }
        private void Button_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (TreeView_Dir.SelectedItem == null) return;
            var selItem = (TreeViewFileItem)TreeView_Dir.SelectedItem;
            if (!selItem.Filename.Contains('.')) return;

            string resultPath = GetSelectedPath();


            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($@"ftp://{_ip}/{resultPath}");
            request.Credentials = new NetworkCredential(_login, _password);
            request.Method = WebRequestMethods.Ftp.DeleteFile;

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            if (response.StatusCode != FtpStatusCode.FileActionOK) MessageBox.Show(response.StatusDescription, Enum.GetName(typeof(FtpStatusCode), response.StatusCode));
            Button_Refresh_Click(sender, e);
        }

        private void Button_Refresh_Click(object sender, RoutedEventArgs e)
        {
            var item = new TreeViewFileItem();
            LoadFtpStructure(_ip, item);
            TreeView_Dir.ItemsSource = item.Items;
        }

        private void Button_Upload_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.ShowDialog();
            FtpWebRequest request = null;

            string filename = dialog.FileName.Split('\\').Last();


            if (TreeView_Dir.SelectedItem != null)
            {
                if (dialog.CheckFileExists)
                {
                    request = (FtpWebRequest)WebRequest.Create($@"ftp://{_ip}/{GetSelectedPath()}/{filename}");
                }
            }
            else
            {
                request = (FtpWebRequest)WebRequest.Create($@"ftp://{_ip}/{filename}");
            }
            request.Credentials = new NetworkCredential(_login, _password);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            FileStream fs = new FileStream(dialog.FileName, FileMode.Open);
            byte[] buff = new byte[fs.Length];
            fs.Read(buff, 0, buff.Length);
            fs.Close();

            request.ContentLength = buff.Length;
            Stream stream = request.GetRequestStream();
            stream.Write(buff, 0, buff.Length);
            stream.Close();

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            if (response.StatusCode != FtpStatusCode.FileActionOK) MessageBox.Show(response.StatusDescription, Enum.GetName(typeof(FtpStatusCode), response.StatusCode));
            Button_Refresh_Click(sender, e);
        }

        private void Button_Download_Click(object sender, RoutedEventArgs e)
        {
            var selItem = (TreeViewFileItem)TreeView_Dir.SelectedItem;
            var dirPath = GetSelectedPath();
            var filename = dirPath.Split('/').Last();

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($@"ftp://{_ip}/{dirPath}");
            request.Credentials = new NetworkCredential(_login, _password);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            using (Stream ftpStream = response.GetResponseStream())
            using (Stream fileStream = File.Create("" + filename))
            {
                ftpStream.CopyTo(fileStream);
            }

            if (response.StatusCode != FtpStatusCode.FileActionOK) MessageBox.Show(response.StatusDescription, Enum.GetName(typeof(FtpStatusCode), response.StatusCode));
            Button_Refresh_Click(sender, e);
        }
    }

    public class TreeViewFileItem
    {
        public string Filename { get; set; }
        public List<TreeViewFileItem> Items { get; set; } = new List<TreeViewFileItem>();

        public object Tag { get; set; }
    }
}
