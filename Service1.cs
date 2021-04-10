using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;


namespace RNA_Rebuild
{
    public class Client
    {
        public string PcName { get; set; }
        public IPAddress IP { get; set; }
        public DateTime ConnectionTime { get; set; }
        public ICallbackService Callback { get; set; }
    }


	public delegate string GetStr();
	public delegate SuperFile GetScreenDelegate();

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple/*, IncludeExceptionDetailInFaults = true*/)]
    public class Service1 : IService1
    {
        public Dictionary<string, Client> Clients = new Dictionary<string, Client>();
        public Dictionary<string, Client> UsingClients = new Dictionary<string, Client>();
		private Dictionary<string, Client> Admins = new Dictionary<string, Client>();
		private Sender Mail { get; set; } = null;
		public bool TXTLog { get; set; } = false;
		public bool MailLog { get; set; } = false;
        bool InStream { get; set; } = false;
        public Service1()
        {
            try
            {
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\Stream"))
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Stream");
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\Files"))
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Files");
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\Screenshots"))
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Screenshots");
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\Logs"))
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Logs");
            }
            catch { }
        }
        public bool CanSendScreen()
        {
            return !InStream;
        }

		public BitmapImage ToBitmapImage(Bitmap bitmap)
		{
			using (var memory = new MemoryStream())
			{
				bitmap.Save(memory, ImageFormat.Png);
				memory.Position = 0;
				var bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = memory;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				bitmapImage.Freeze();
				return bitmapImage;
			}
		}

		public void ShutdownPCs(Client Client = null)
		{
			Thread t = new Thread(() => ShuttingDown(Client));
			t.Start();
		}

		public void ShuttingDown(Client Client)
		{
			if (Client == null)
			{
				foreach (var client in UsingClients)
				{
					GoLog(client.Value?.PcName, "Shutdown");
					client.Value?.Callback?.ShutDown();
					UpdateAdminClients();
					Clients.Remove(client.Key);
				}
				UsingClients.Clear();
			}
			else
			{
				GoLog(Client.PcName, "Get processes");
				Client?.Callback?.ShutDown();
				RemoveClient(Client);
			}
		}
		private void UpdateAdminClients()
		{
			UsingClients.Clear();
			for (int i = 0; i < Admins.Count; i++)
			{
				try
				{
					Admins?.ElementAt(i).Value?.Callback?.UpdateClients(UsingClients, Clients);
				}
				catch
				{
					Admins.Remove(Admins?.ElementAt(i).Key);
				}
			}
		}

		public void RebootPCs(Client Client = null)
		{
			Thread t = new Thread(() => Rebooting(Client));
			t.Start();
		}

		public void Rebooting(Client Client)
		{
			if (Client == null)
			{
				foreach (var client in UsingClients)
				{
					GoLog(client.Value?.PcName, "Reboot");
					client.Value?.Callback?.Reboot();
					Clients.Remove(client.Key);
				}
				UsingClients.Clear();
			}
			else
			{
				GoLog(Client.PcName, "Reboot");
				Client.Callback?.Reboot();
				RemoveClient(Client);
			}
		}

		private void RemoveClient(Client cl)
		{
			foreach (var Admin in Admins)
			{
				try
				{
					Admin.Value?.Callback?.Remove_Client(cl);
				}
				catch { continue; }
			}
		}

		public void SendMessages(List<Client> Clients, string text)
		{
			Thread t = new Thread(() => SendingMessages(Clients, text));
			t.Start();
		}

		public void SendingMessages(List<Client> Clients, string text)
		{
			foreach (Client Client in Clients)
			{
				GoLog(Client?.PcName, "Send message:\n" + text);
				Client?.Callback?.SendMessage(text);
			}
		}

		public SuperFile GetScreenShot(string Client)
		{
			return new GetScreenDelegate(Clients[Client].Callback.GetScreenshot).Invoke(); ;
		}

		public Process[] GetProcesses(Client Client)
		{
			GoLog(Client?.PcName, "Get processes");
			return Client?.Callback?.GetProcesses();
		}

		public void CloseProcess(Client Client, int id)
		{
			GoLog(Client?.PcName, "Close process - " + id);
			Thread t = new Thread(()=>Client?.Callback?.CloseProcess(id));
			t.Start();
		}

		public FileInfo[] GetFiles(Client Client, string path)
		{
			GoLog(Client?.PcName, "Get files - " + path);
			return Task.Run(() => GettingFiles(Client, path)).Result;
		}

		public FileInfo[] GettingFiles(Client Client, string path)
		{
			return Client?.Callback?.GetFiles(path);
		}

		public DirectoryInfo[] GetDirectories(Client Client, string path)
		{
			GoLog(Client?.PcName, "Get directories - " + path);
			return Task.Run(() => GettingDirectories(Client, path)).Result;
		}

		public DirectoryInfo[] GettingDirectories(Client Client, string path)
		{
			return Client?.Callback?.GetDirectories(path);
		}

		public void RemoveFile(Client Client, string path)
		{
			Thread t = new Thread(()=>Client.Callback?.RemoveFile(path));
			t.Start();
		}

		public string[] FindFiles(Client Client, string mask)
		{
			GoLog(Client?.PcName, "Find files - " + mask);
			return Client?.Callback?.FindFiles(mask);
		}

		public SuperFile TakeFile(Client Client, string path)
		{
			GoLog(Client?.PcName, "Take file - " + path);
			return Task.Run(() => Client?.Callback?.TakeFile(path)).Result;
		}

		public void ChangeUsingClient(string PC_Name, bool value)
        {
			if (Clients.ContainsKey(PC_Name))
            if (UsingClients.ContainsKey(PC_Name) && !value)
                UsingClients.Remove(PC_Name);
            else if (value) UsingClients.Add(PC_Name, Clients[PC_Name]);
        }

		private void GoLog(string PC_Name, string Action)
		{
			Thread t = new Thread(() => TryLog(PC_Name, Action));
			t.Start();
		}

		private void TryLog(string PC_Name, string Action)
		{
			if (TXTLog) TxtLog(PC_Name, Action);
			if (MailLog) SMTPLog(PC_Name, Action);
		}

		private void SMTPLog(string PC_Name, string Action)
		{
			try
			{
				if (Mail == null) return;
				MailMessage mailMsg = new MailMessage();
				mailMsg.From = new MailAddress(Mail.Address);
				mailMsg.To.Add(Mail.Reciever);
				mailMsg.IsBodyHtml = false;
				mailMsg.Subject = "RNA Logs";
				mailMsg.Body = $"{DateTime.Now}: {PC_Name} > {Action}";
				SmtpClient client = new SmtpClient(Mail.ServerAddress, Mail.ServerPort);
				client.Credentials = new NetworkCredential(Mail.Address, Mail.Password);
				client.EnableSsl = Mail.SSL;
				client.Send(mailMsg);
			}
			catch { }
		}
		private void TxtLog(string PC_Name, string Action)
		{
			try
			{
				StreamWriter sw = File.AppendText(Environment.CurrentDirectory + "\\Logs\\" + $"{DateTime.Now.Day}.{DateTime.Now.Month}.{DateTime.Now.Year}");
				sw.WriteLine(DateTime.Now + ": " + PC_Name + " > " + Action);
				sw.Close();
			}
			catch { }
		}

        public void DeleteClient(string PCName)
        {
            if (UsingClients.ContainsKey(PCName)) UsingClients.Remove(PCName);
            if (Clients.ContainsKey(PCName)) Clients.Remove(PCName);
        }

		public bool AddClient(string NewPcName, IPAddress NewIP)
		{
            if (!Clients.ContainsKey(NewPcName))
            {
                try
                {
					Client new_client = new Client
					{
						Callback = OperationContext.Current.GetCallbackChannel<ICallbackService>(),
						ConnectionTime = DateTime.Now,
						IP = NewIP,
						PcName = NewPcName
					};
					Clients.Add( NewPcName, new_client );
					foreach (var Admin in Admins) try { Admin.Value?.Callback?.Add_Client(new_client); } catch { }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

		public Dictionary<string, Client> GetClients()
		{
            return Clients;
		}

		public Dictionary<string, Client> GetActiveClients()
		{
            return UsingClients;
		}

		public Client GetClient(string PCname)
		{
            return Clients[PCname];
		}

		public void AddUsingClient(string PCname)
		{
            if (Clients.ContainsKey(PCname))
                if (!UsingClients.ContainsKey(PCname))
                    UsingClients.Add(PCname, Clients[PCname]);
		}

		public void DeleteUsingClient(string PCname)
        {
            if (Clients.ContainsKey(PCname))
                if (UsingClients.ContainsKey(PCname))
                    UsingClients.Remove(PCname);
        }

		public bool CheckMail()
		{
			if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\mail.dat"))
			{
				File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\mail.dat");
				return false;
			}
			else
			{
				StreamReader sr = null;
				FileStream fs = null;
				try
				{
					fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "\\mail.dat", FileMode.Open, FileAccess.Read);
					sr = new StreamReader(fs);
					Mail = new Sender
					{
						ServerAddress = sr.ReadLine(),
						ServerPort = Convert.ToInt32(sr.ReadLine()),
						Address = sr.ReadLine(),
						Password = sr.ReadLine(),
						Reciever = sr.ReadLine(),
						SSL = Convert.ToBoolean(sr.ReadLine())
					};
					sr.Close();
					fs.Close();
					return true;
				}
				catch
				{
					sr.Close();
					fs.Close(); 
					return false; 
				}
			}
		}

		public void SetSMTPClient(string server, int port, string address, string password, bool ssl, string reciever)
		{
			GoLog("Mail setting - ", address);
			Mail = new Sender 
			{ ServerAddress = server, Password = password, ServerPort = port, 
				SSL = ssl, Address = address, Reciever = reciever };
		}

		public void ChangeSMTPLogging(bool value)
		{
			MailLog = value;
		}

		public void ChangeTXTLogging(bool value)
		{
			TXTLog = value;
		}

		public void SetActiveClients(Dictionary<string, Client> Clients)
		{
			UsingClients = Clients;
		}

		public void AddAdmin(string PC_Name, IPAddress IP)
		{
			if (Admins.ContainsKey(PC_Name)) Admins.Remove(PC_Name);
			Admins.Add(PC_Name, new Client
			{
				PcName = PC_Name,
				Callback = OperationContext.Current.GetCallbackChannel<ICallbackService>(),
				ConnectionTime = DateTime.Now,
				IP = IP
			});
		}

		public void DeleteAdmin(string PC_Name)
		{
			if (Admins.ContainsKey(PC_Name)) Admins.Remove(PC_Name);
		}

		public void DisconnectClient(string PC_Name = null)
		{
			Thread t = new Thread(() => DisconnectingClients(PC_Name));
			t.Start();
		}

		private void DisconnectingClients(string PC_Name = null)
		{
			if (PC_Name == null)
			{
				foreach (var Client in UsingClients)
				{
					Client.Value.Callback.Disconnect();
					Clients.Remove(Client.Key);
				}
				UsingClients.Clear();
			}
			else
			{
				if (Clients.ContainsKey(PC_Name))
				{
					Clients[PC_Name].Callback.Disconnect();
					Clients.Remove(PC_Name);
					if (UsingClients.ContainsKey(PC_Name))
						UsingClients.Remove(PC_Name);
				}
			}
		}

        public List<string> GetClientDrives(Client client)
        {
			try
			{
				return client.Callback.GetDrives();
			}
			catch (Exception ex) { MessageBox.Show(ex.Message); }
			return null;
        }

        public DirectoryInfo[] GetClientDirectories(Client client, string path)
        {
			return client.Callback.GetDirectories(path);
        }

        public FileInfo[] GetClientFiles(Client client, string path)
        {
			return client.Callback.GetFiles(path);
        }

        public List<Process> GetClientProcesses(Client client)
        {
			return client.Callback.GetProcesses().ToList();
        }
    }

    [DataContract]
    public class SuperFile
    {
        [DataMember]
        public byte[] Content { get; set; }
        [DataMember]
        public string Name { get; set; }
	}
	[DataContract]
	public class Sender
	{
		[DataMember]
		public string ServerAddress { get; set; }
		[DataMember]
		public int ServerPort { get; set; }
		[DataMember]
		public string Address { get; set; }
		[DataMember]
		public string Password { get; set; }
		[DataMember]
		public bool SSL { get; set; }
		[DataMember]
		public string Reciever { get; set; }
	}
}