//-----------------------------------------------------------------------------------------------
// <copyright file="Account.cs" company="Erast Korolev">
//     Created in 2018, just under by MIT license. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------------------------------

namespace MailRuCloudClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Exceptions;
    using MailRuCloudClient.Data;
    using MailRuCloudClient.Data.Rates;
    using MailRuCloudClient.Static;
    using MailRuCloudClient.Static.Helpers;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Defines the MAILRU account.
    /// </summary>
    public class Account
    {
        /// <summary>
        /// Cookie container for account.
        /// </summary>
        private CookieContainer cookies = new CookieContainer();

        /// <summary>
        /// Initializes a new instance of the <see cref="Account" /> class.
        /// </summary>
        public Account()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Account" /> class.
        /// </summary>
        /// <param name="email">Login as email.</param>
        /// <param name="password">Password related with this login.</param>
        public Account(string email, string password)
        {
            Debug.Assert(!string.IsNullOrEmpty(email), "Is null or empty.");
            Debug.Assert(!string.IsNullOrEmpty(password), "Is null or empty.");

            this.Email = email;
            this.Password = password;
        }

        /// <summary>
        /// Gets or sets login as email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets the list of activated tariffs for account. Relogin the account to recalculation connected tariffs.
        /// </summary>
        public List<Rate> ActivatedTariffs { get; private set; }

        /// <summary>
        /// Gets a value indicating whether is turn on upload size limit on 2GB for account.
        /// </summary>
        public bool Has2GBUploadSizeLimit { get => !this.ActivatedTariffs.Any(x => x.Id != "ZERO"); }

        /// <summary>
        /// Gets authorization token.
        /// </summary>
        internal string AuthToken { get; private set; }

        /// <summary>
        /// Gets or sets the account cookies.
        /// </summary>
        internal CookieContainer Cookies
        {
            get
            {
                return this.cookies;
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                this.cookies = value;
            }
        }

        /// <summary>
        /// Gets or sets the HTTP client.
        /// </summary>
        internal HttpClient HttpClient { get; set; }

        /// <summary>
        /// Login in cloud server.
        /// </summary>
        /// <exception cref="AuthorizeException">Authorization exception.</exception>
        /// <returns>True or false result of operation.</returns>
        public /*async Task<bool>*/ bool Login()
        {
            //await 
            this.CheckAuthorization(true);
            HttpClientInitializer.Create(this, Urls.BaseMailRuAuth);
            var content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Login", this.Email),
                new KeyValuePair<string, string>("Domain", "mail.ru"),
                new KeyValuePair<string, string>("Password", this.Password)
            });

            //var responseMessage = await this.HttpClient.PostAsync(Urls.Auth, content);
            Task<HttpResponseMessage> responseMessageT1 = 
                this.HttpClient.PostAsync(Urls.Auth, content);

            HttpResponseMessage responseMessage1 = responseMessageT1.Result;
            if (!responseMessage1.IsSuccessStatusCode)
            {
                return false;
            }

            Task<HttpResponseMessage> responseMessageT2 = /*await*/ 
                this.HttpClient.GetAsync(Urls.EnsureSdc);

            HttpResponseMessage responseMessage2 = responseMessageT2.Result;
            if (!responseMessage2.IsSuccessStatusCode)
            {
                return false;
            }

            HttpClientInitializer.Create(this, Urls.BaseMailRuCloud);

            Task<string> responseT = /*await*/ this.HttpClient.GetStringAsync(Urls.AuthToken);

            string responseStr = responseT.Result;

            this.AuthToken = responseStr.Deserialize<AuthToken>().Token;

            Debug.WriteLine("[i] AuthToken = " + this.AuthToken);

            
            /*Task<List<Rate>>*/
            List<Rate> rates = /* await */ this.GetRates();              
            
            this.ActivatedTariffs = rates.Where(x => x.IsActive).ToList();

            Debug.WriteLine("[i] Active Tariffs: " + rates.Where(x => x.IsActive));
            
            return true;
        }

        /// <summary>
        /// Check the client current authorization. 
        /// Do not call this method always before any request, by default it's enabled already.
        /// </summary>
        /// <returns>True - if client is in the system now.</returns>
        public /*async Task<bool>*/ bool CheckAuthorization()
        {
            try
            {
                //await 
                this.CheckAuthorization(false);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get disk usage for account.
        /// </summary>
        /// <returns>Returns Total/Free/Used size.</returns>
        public /*async Task<DiskUsage>*/ DiskUsage GetDiskUsage()
        {
            return /*await*/ this.GetDiskUsageInternal(true);
        }

        /// <summary>
        /// Check authorization options.
        /// </summary>
        /// <param name="baseCheckout">If true will check only Login and Password.</param>
        /// <returns>The simple task.</returns>
        internal /*async Task*/ bool CheckAuthorization(bool baseCheckout)
        {
            if (string.IsNullOrEmpty(this.Email))
            {
                //throw new NotAuthorizedException("Is not defined.", nameof(this.Login));
                return false;
            }

            if (string.IsNullOrEmpty(this.Password))
            {
                //throw new NotAuthorizedException("Is not defined.", nameof(this.Password));
                return false;
            }

            if (!baseCheckout)
            {
                if (this.Cookies == null || this.Cookies.Count == 0)
                {
                    //throw new NotAuthorizedException("Missing cookies.");
                    return false;
                }

                if (string.IsNullOrEmpty(this.AuthToken))
                {
                    throw new NotAuthorizedException("Missing authorization token.");
                    return false;
                }

                //await 
                this.GetDiskUsageInternal(false);
                
            }
            return true;
        }

        /// <summary>
        /// Get disk usage for account.
        /// </summary>
        /// <param name="checkAuthorization">When true, will be checked the authorization to MAILRU.</param>
        /// <returns>Returns Total/Free/Used size.</returns>
        internal /*async Task<DiskUsage>*/ DiskUsage GetDiskUsageInternal(bool checkAuthorization)
        {
            if (checkAuthorization)
            {
                //await 
                this.CheckAuthorization(false);
            }

            var responseMsgT = /*await*/ this.HttpClient.GetAsync(Urls.DiskSpace, this.Email, this.AuthToken);
            //responseMsgT.Wait();
            HttpResponseMessage responseMsg = responseMsgT.Result;
            if (!responseMsg.IsSuccessStatusCode)
            {
                Debug.WriteLine("The client is not authorized.");
                //throw new NotAuthorizedException("The client is not authorized.");
                return null;
            }

            var responseStrT = /*await*/ responseMsg.Content.ReadAsStringAsync();
            string responseStr = responseStrT.Result;
            var responseParsed = responseStr.Deserialize<JToken>();
            return new DiskUsage
            {
                Total = new Size((long)responseParsed["bytes_total"] * 1024L * 1024L),
                Used = new Size((long)responseParsed["bytes_used"] * 1024L * 1024L)
            };
        }

        /// <summary>
        /// Get the activated tariffs.
        /// </summary>
        /// <returns>The tariffs list.</returns>
        private /*async Task<List<Rate>>*/ List<Rate> GetRates()
        {
            //await 
            this.CheckAuthorization(false);

            var responseStrT = /*await*/ this.HttpClient.GetStringAsync(
                Urls.Rates, this.Email, this.AuthToken);
            string responseStr = responseStrT.Result;
            return responseStr.Deserialize<Rates>().Items.ToList();
        }
    }
}
