using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;    
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Live;
using System.Collections;
using System.Web.Script.Serialization;
using System.Security.Cryptography;


namespace OneDrive
{
    public partial class Main : Form, IRefreshTokenHandler
    {
        public Main()
        {
            InitializeComponent();
        }
        
        public AuthResult authResult;
        string ClientID = "000000004814AD4C";
        MyBrowser myBrowser;
        LiveAuthClient liveAuthClient;
        LiveConnectClient liveConnectClient;
        private Microsoft.Live.RefreshTokenInfo refreshTokenInfo;
        private ODSecurity ods;
        string[] Scopes = { "wl.signin", "wl.basic", "wl.photos", "wl.share", "wl.skydrive", "wl.skydrive_update", "wl.work_profile" };

        LiveAuthClient AuthClient
        {
            get
            {
                if (this.liveAuthClient == null)
                {
                    this.AuthClient = new LiveAuthClient(ClientID, this);
                }

                return this.liveAuthClient;
            }

            set // удалена обработка ошибок
            {
                this.liveAuthClient = value;
                this.liveConnectClient = null;
            }
        }
        private void CleanupBrowser()
        {
            if (this.myBrowser != null)
            {
                this.myBrowser.Dispose();
                this.myBrowser = null;
            }
        }
        private async void OnAuthCompleted(AuthResult authResult)
        {
            this.CleanupBrowser();
            if (authResult.AuthorizeCode != null)
            {
                try
                {
                    LiveConnectSession session = await this.AuthClient.ExchangeAuthCodeAsync(authResult.AuthorizeCode);
                    this.liveConnectClient = new LiveConnectClient(session);
                    LiveOperationResult meRes = await this.liveConnectClient.GetAsync("me");
                    dynamic meData = meRes.Result;
                    this.nameLabel.Text = meData.name;
                    LiveDownloadOperationResult meImgResult = await this.liveConnectClient.DownloadAsync("me/picture");
                    this.meImage.Image = Image.FromStream(meImgResult.Stream);

                }
                catch(Exception e)
                {
                    MessageBox.Show("Error", e.Message);
                }
                updateTree();
            }
        }
        Task IRefreshTokenHandler.SaveRefreshTokenAsync(RefreshTokenInfo tokenInfo)// хз
        {
            // Note: 
            // 1) In order to receive refresh token, wl.offline_access scope is needed.
            // 2) Alternatively, we can persist the refresh token.
            return Task.Factory.StartNew(() =>
            {
                this.refreshTokenInfo = tokenInfo;
            });
        }
        Task<RefreshTokenInfo> IRefreshTokenHandler.RetrieveRefreshTokenAsync() // хз
        {
            return Task.Factory.StartNew<RefreshTokenInfo>(() =>
            {
                return this.refreshTokenInfo;
            });
        }
        public async void recursiveExplorer(TreeNode currentNode, drive currentDrive)
        {
            try
            {
                driveList localList;
                localList = await getFileInfo(currentDrive);
                if (localList.data.Count() > 0)
                {
                    foreach (drive i in localList.data)
                    {
                        if (i.id.StartsWith("folder"))
                        {
                            // MessageBox.Show(i.id);
                            TreeNode node = currentNode.Nodes.Add(i.id, i.name);
                            this.recursiveExplorer(node, i);
                            node.ImageIndex = node.SelectedImageIndex = 0;
                        }
                        else if (i.id.StartsWith("file"))
                        {
                            //   TreeNode nodes = treeView1.Nodes[currentNode.Index].Nodes.Add(i.name);
                            TreeNode nodes = currentNode.Nodes.Add(i.id, i.name);
                            nodes.ImageIndex = nodes.SelectedImageIndex = 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }    
        public async Task<driveList> getFileInfo(drive thisDrive)
        {
            driveList resultList = null;
            try
            {
                LiveOperationResult result = await this.liveConnectClient.GetAsync(string.Concat(thisDrive.id, "/files"));
                JavaScriptSerializer ser = new JavaScriptSerializer();
                resultList = ser.Deserialize<driveList>(result.RawResult);
                return resultList;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return resultList;
            }


        }
        public async void updateTree()
        {
            try
            {
                LiveOperationResult res = await this.liveConnectClient.GetAsync("me/skydrive/files");

                JavaScriptSerializer ser = new JavaScriptSerializer();
                driveList rootDriveList = ser.Deserialize<driveList>(res.RawResult);
                drive Rootdrive = new drive();
                Rootdrive.name = "root";
                Rootdrive.id = "me/skydrive";

                treeView1.TopNode = new TreeNode();

                recursiveExplorer(this.treeView1.TopNode, Rootdrive);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        private async Task DownloadFile(string path)
                {
                    SaveFileDialog dialog = new SaveFileDialog();
                    Stream stream = null;
                    dialog.RestoreDirectory = true;

                    if (dialog.ShowDialog() != DialogResult.OK)
                    {
                        throw new InvalidOperationException("No file is picked to upload.");
                    }
                    try
                    {
                        if ((stream = dialog.OpenFile()) == null)
                        {
                            throw new Exception("Unable to open the file selected to upload.");
                        }
                        CryptoStream crStream = createCryptoStream(stream, "Decrypt");
                        using (crStream)
                        {
                            LiveDownloadOperationResult result = await this.liveConnectClient.DownloadAsync(path);
                            if (result.Stream != null)
                            {
                                using (result.Stream)
                                {
                                    await result.Stream.CopyToAsync(crStream);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
        private async Task<LiveOperationResult> UploadFile(string path)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                Stream stream = null;
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    throw new InvalidOperationException("No file is picked to upload.");
                }
                try
                {
                    if ((stream = dialog.OpenFile()) == null)
                    {
                        throw new Exception("Unable to open the file selected to upload.");
                    }
                    CryptoStream crStream = createCryptoStream(stream, "Encrypt");

                    using (crStream)
                    {
                        return await this.liveConnectClient.UploadAsync(path, dialog.SafeFileName, crStream, OverwriteOption.DoNotOverwrite);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        private CryptoStream createCryptoStream(Stream originalStream, string mode)
        {
            Rijndael rijAlg = Rijndael.Create();
            rijAlg.Key = new byte[32] { 118, 123, 23, 17, 161, 152, 35, 68, 126, 213, 16, 115, 68, 217, 58, 108, 56, 218, 5, 78, 28, 128, 113, 208, 61, 56, 10, 87, 187, 162, 233, 38 };
            rijAlg.IV = new byte[16] { 33, 241, 14, 16, 103, 18, 14, 248, 4, 54, 18, 5, 60, 76, 16, 191 };
            ICryptoTransform crypt = null;
            CryptoStream crStream = null;
            switch (mode)
            {
                case "Decrypt":
                    crypt = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);
                    crStream = new CryptoStream(originalStream, crypt, CryptoStreamMode.Write);
                    break;
                case "Encrypt":
                    crypt = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);
                    crStream = new CryptoStream(originalStream, crypt, CryptoStreamMode.Read);
                    break;
            }
            return crStream;
        }
        private async void DownloadButton_Click(object sender, System.EventArgs e)
        {
            try
            {
                string adress = this.treeView1.SelectedNode.Name;
                //   MessageBox.Show(adress);
                await this.DownloadFile(string.Concat(adress, "/content"));

                this.textBox1.Text = "Download Completed";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private async void UploadButton_Click(object sender, System.EventArgs e)
        {
            try
            {
                await this.UploadFile(treeView1.SelectedNode.Name);
                textBox1.Text = "Upload completed";
                updateTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void SignInButton_Click(object sender, EventArgs e)
        {
            string startUri = this.AuthClient.GetLoginUrl(Scopes);
            string endUri = "https://login.live.com/oauth20_desktop.srf";
            this.myBrowser = new MyBrowser(startUri, endUri, this.OnAuthCompleted);
            this.myBrowser.Show();
            this.myBrowser.MyBrowser_Load();
        }
        private void SignOutButton_Click(object sender, EventArgs e)
        {
            this.webBrowser1.Navigate(this.AuthClient.GetLogoutUrl());
            this.AuthClient = null;
            meImage.Image = Properties.Resources.noavatar;
            nameLabel.Text = "User";
        }
        private void button4_Click(object sender, EventArgs e)
        {
            ODSecurity _ods = new ODSecurity();
            string password = GetSha384Password(passwordTextbox.Text);
            _ods.SaveODSecurity(password);
            textBox1.Text = "Save completed!";
        }
        private void OpenODSButton_Click(object sender, EventArgs e)
        {
            string password = null;
            if (passwordTextbox.Text != null)
            {
                password = GetSha384Password(passwordTextbox.Text);
            }
            else
            {
                MessageBox.Show("Password is empty!");
                return;
            }
            try
            {
                ods = new ODSecurity().OpenODSecurity(password);
            }
            catch(Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
        private string GetSha384Password(string input)
        {
            System.Security.Cryptography.SHA384 sha1 = new System.Security.Cryptography.SHA384CryptoServiceProvider();
            return Encoding.ASCII.GetString(sha1.ComputeHash(Encoding.ASCII.GetBytes(input)));
        }

    }
}
