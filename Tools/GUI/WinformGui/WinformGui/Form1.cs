using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json.Linq;

namespace WinformGui
{
    public partial class Form1 : Form
    {

        HubConnection connection;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                connection = new HubConnectionBuilder().WithUrl("http://localhost:59010/nethub").WithAutomaticReconnect().Build();
                //  IDisposable disposable = connection.On<string>("ReceiveMessage", ReceiveMessage);


                connection.StartAsync();
            } catch(Exception ex)
            {
                Console.Write(ex.ToString());
            }
        }

        private string rectifyUrl(string url)
        {
            url = url.ToLower();
            if (!url.StartsWith("http://"))
            {
                url = "http://" + url;
            }

            if (!url.EndsWith("/") && !url.EndsWith("connect"))
            {
                url = url + "/";
            }

            if (!url.EndsWith("connect"))
            {
                url = url + "connect";
            }
            return url;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            HttpClient c = new HttpClient();
            StringContent content = new StringContent(@"{
	        ""UserName"": ""j.barnes"",

            ""FirstName"": ""Jeremy"",
	        ""LastName"": ""Barnes"",
	        ""EmailAddress"": ""jeremiah.barnes@outlook.com"",
	        ""Cash"" : 100,
	        ""Gender"": ""Male"",
	        ""Birthdate"": ""12-05-1991"",
	        ""City"": ""Chicago"",
	        ""State"": ""IL"",
	        ""Country"": ""USA"",
	        ""Postcode"": ""60654"",
	        ""Password"": ""password1"",
	        ""Salt"": ""1233456"",
	        ""TokenSelector"": ""boop"",
	        ""TokenValidator"": ""beepborp""


        }", Encoding.UTF8, "application/json");

            connection.InvokeAsync("Connect");

            var x = c.PostAsync("http://localhost:59010/api/user/login", content).Result;
            jwt = JObject.Parse(x.Content.ReadAsStringAsync().Result)["authToken"].ToString();
        }
        static string jwt;
    }
}
