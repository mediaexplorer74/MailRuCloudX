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
        private const string Login = "myrauthorization@mail.ru"; // test login
        private const string Password = "190190aA"; // test pwd
        private Account account = new Account(Login, Password);

        public MainPage()
        {
            InitializeComponent();            
        }


        public void A1LoginTest()
        {
            account.Login();
            Debug.WriteLine("[i] account.AuthToken = " + account.AuthToken);
        }

        private void Btn1_Clicked(object sender, EventArgs e)
        {
            A1LoginTest();
        }
    }
}
