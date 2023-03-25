using MailRuCloudClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MailRuCloudApi
{
    public partial class MainPage : ContentPage
    {
        public const string Login = "myrauthorization@mail.ru"; // test login
        public const string Password = "190190aA"; // test pwd

        private static Account account = null;
        private static CloudClient client = null;

        private const string TestFolderName = "new folder"; // In Cloud
        private const string TestFolderPath = "/";// + TestFolderName; // In Cloud
       
        private int prevUploadProgressPercentage = -1;
        private int prevDownloadProgressPercentage = -1;
        private bool hasChangedFolderContentAfterUploading = false;

        public MainPage()
        {
            InitializeComponent();            
        }

        // Btn1_Clicked
        private void Btn1_Clicked(object sender, EventArgs e)
        {
            //await 
            CreateFolderTest();
        }//Btn1_Clicked


        // CreateFolderTest
        public /*async Task*/ bool CreateFolderTest()
        {
            //await 
            CheckAuthorization();
            
            var newFolderName = /*Guid.NewGuid().ToString()*/"newfoldertest";
            var result = /*await*/ client.CreateFolder(TestFolderPath
                /*+ "/new folders test/"*/ + newFolderName);

            string res = "";
            try
            {
                res = result.FullPath.Split(new[] { '/' }).Last();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] CreateFolderTest exception: " + ex.Message);
            }

            Debug.WriteLine(newFolderName + " | " + res);
            Debug.WriteLine(TestFolderPath + "newfoldertest | " + result.FullPath);

            return true;
        }//CreateFolderTest


        // CheckAuthorization
        private async Task CheckAuthorization()
        {
            if (account == null)
            {
                account = new Account(Login, Password);
                bool outp = /*await*/ account.Login();

                Debug.WriteLine(outp == true ? "true" : "false");

                client = new CloudClient(account);
            }
        }//CheckAuthorization

    }//class end

}//namespace end
