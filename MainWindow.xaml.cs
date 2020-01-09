using ColumnExtracter.Parser;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Windows;

namespace ColumnExtracter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnExtract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(this.tbFilePath.Text))
                {
                    throw new Exception("Enter FileName.");
                }
                string sqlText = RemovePackage(File.ReadAllText(tbFilePath.Text));

                ParseResult result = DBParser.ExtractColumns(sqlText);
                this.txtResults.Text = string.Join(Environment.NewLine, result.Columnlist);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string RemovePackage(string str)
        {
            string[] lines = str.Split('\n');
            bool isPackage = false;
            foreach (string line in lines)
            {
                if (line.ToLower().Contains("package"))
                {
                    isPackage = true;
                    break;
                }
            }

            StringBuilder sb = new StringBuilder();
            bool foundPackageHeader = false;
            if (isPackage)
            {
                foreach (string line in lines)
                {
                    if (foundPackageHeader) { sb.Append(line); }
                    if (line.ToLower().Contains("package")) { foundPackageHeader = true; sb.Clear(); }                    
                }

                //find last index of END
                string modified = sb.ToString();
                int lastEndIndex = modified.LastIndexOf("end", StringComparison.CurrentCultureIgnoreCase);
                string updated = modified.Substring(0, lastEndIndex);

                //find first procedure at start
                int firstProcedureIndex = updated.IndexOf("procedure", StringComparison.CurrentCultureIgnoreCase);

                return updated.Substring(firstProcedureIndex, updated.Length - firstProcedureIndex);
            }

            return str; //do nothing
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                bool? result = ofd.ShowDialog(this);
                if (result.HasValue && result.Value)
                {
                    this.tbFilePath.Text = ofd.FileName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
