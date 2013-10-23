using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VerifySiteAttributes
{
    public partial class Form1 : Form
    {
        List<string> Tags = new List<string>();
        List<string> Sites = new List<string>();
        protected List<Tuple<string, string>> Attributes = 
            new List<Tuple<string, string>>();
        string FileSites = "";
        string FileTags = "";

        WebBrowser Web = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void bClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (tbTags.Text == "" || tbSites.Text == "")
            {
                MessageBox.Show("Введите имена файлов для парсинга. ", 
                                "Информационное сообщение", 
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Initialize();

            try
            {
                progressBar1.Visible = true;
                var rs = new XmlReaderConfig();
                var ra = new XmlReaderConfig();
                rs.GetParameters(FileSites, Sites, "site");
                ra.GetParameters(FileTags, Tags, "tag");

                ClearWebBrowser();
                using (Web = new WebBrowser())
                {
                    SetWebBrowserOptions();
                    foreach (var s in Sites)
                    {
                        string content = "";
                        content += "\n\n\nСайт " + s + ":\n";
 
                        Attributes.Clear();
                        
                        LoadSite(s);
                        GetAttributes("META", "NAME", "CONTENT");

                        foreach (var t in Tags)
                        {
                            string text = "";
                            if (FindAttribute(t, ref text))
                            {
                                content += "\n * " + t + " = " + "\t" + text;
                            }
                        }

                        rtbReport.Text += content;
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("Возникла ошибка:  \n" + err.Message,
                                "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar1.Visible = false;
            }
        }

        private void Initialize()
        {
            rtbReport.Clear();
            Tags.Clear();
            Sites.Clear();
            FileSites = tbSites.Text;
            FileTags = tbTags.Text;
            Attributes.Clear();
        }

        private void btDll_Click(object sender, EventArgs e)
        {
            if (ofdAttributes.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    tbTags.Text = ofdAttributes.FileName;
                }
                catch (Exception ex)
                {
                    tbTags.Text = "";
                    MessageBox.Show("Hе возможно считать файл: " + ex.Message, 
                                    "Ошибка", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            Refresh();
        }

        private void btNUnit_Click(object sender, EventArgs e)
        {
            if (ofdSites.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    tbSites.Text = ofdSites.FileName;
                }
                catch (Exception ex)
                {
                    tbSites.Text = "";
                    MessageBox.Show("Hе возможно считать файл: " + ex.Message,
                                    "Ошибка",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            Refresh();
        }

        protected bool Navigate(String address)
        {
            if (String.IsNullOrEmpty(address)) return false;
            if (address.Equals("about:blank")) return false;
            if (!address.StartsWith("http://") &&
                !address.StartsWith("https://"))
            {
                address = "http://" + address;
            }

            try
            {
                Web.Navigate(new Uri(address));
                return true;
            }
            catch (System.UriFormatException err)
            {
                return false;
            }
            catch (Exception err)
            {
                return false;
            }
        }

        protected void GetAttributes(string head = "META", string name = "NAME", string content = "CONTENT")
        {
            Attributes.Clear();

            var elems = Web.Document.GetElementsByTagName(head);
            try
            {
                if (elems != null)
                {
                    foreach (HtmlElement elem in elems)
                    {
                        String nameStr = elem.GetAttribute(name);
                        if (nameStr != null && nameStr.Length != 0)
                        {
                            String contentStr = elem.GetAttribute(content);
                            Attributes.Add(new Tuple<string, string>(nameStr, contentStr));
                        }
                    }
                }
                else
                {
                    throw(new Exception("Не удалось считать страницу указанног осайта."));
                }
            }
            catch (Exception err)
            {
                throw(new Exception("Ошибка во время париснга документа. \n" + err.Message));
            }
        }

        private void SetWebBrowserOptions()
        {
            Web.ScriptErrorsSuppressed = true;
            Web.Visible = false;
        }

        protected void ClearWebBrowser()
        {
            if (Web != null)
                Web.Dispose();
            Web = null;
        }

        protected void LoadSite(string url)
        {
            if (Navigate(url) != true)
            {
                throw(new Exception("Не корректный url."));
            }

            while (Web.ReadyState != WebBrowserReadyState.Complete)
                Application.DoEvents();
        }

        protected bool FindAttribute(string tag, ref string text)
        {
            bool flag = false;
            text = "";
            string tmp = text;

            if (tag != "")
            {
                Parallel.ForEach(Attributes,
                    (curValue, loopstate) =>
                    {
                        if (tag.ToLower() == curValue.Item1.ToLower())
                        {
                            loopstate.Stop();
                            tmp = curValue.Item2;
                            flag = true;
                            return;
                        }
                    });
            }

            text = tmp;
            return flag;
        }
    }
}
