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
	#region Delegates

	public delegate SuperFolder GetSuperFolderDelegate();
	public delegate SuperImage GetImageDelegate();
	public delegate SuperFile GetSuperFileDelegate();
	public delegate SuperFileDirectoryInfo[] GetFileInfoDelegate(string path);
	public delegate SuperProcess[] GetProcessesDelegate();
	public delegate byte[] GetByteArrayDelegate();
	public delegate string[] GetStringsDelegate();
	public delegate string[] GetStringsWithStringDelegate(string str);
	public delegate bool BoolDelegate();

	#endregion

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

		public void ShutdownPCs(string PC_Name)
		{
			Thread t = new Thread(() => ShuttingDown(PC_Name));
			t.Start();
		}

		public void ShuttingDown(string PC_Name)
		{
			if (PC_Name == null)
			{
				foreach (var client in UsingClients)
				{
					try
					{
						GoLog(client.Value?.PcName, "Shutdown");
						client.Value?.Callback?.ShutDown();
						UpdateAdminClients();
						Clients.Remove(client.Key);
					}
					catch { }
				}
				UsingClients.Clear();
			}
			else
			{
				try
				{
					GoLog(Clients[PC_Name]?.PcName, "Get processes");
				Clients[PC_Name]?.Callback?.ShutDown();
				RemoveClient(Clients[PC_Name]);
				}
				catch { }
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

		public void RebootPCs(string PC_Name)
		{
			new Thread(() => Rebooting(Clients[PC_Name])).Start();
		}

		public void Rebooting(Client Client)
		{
			if (Client == null)
			{
				try
				{
					foreach (var client in UsingClients)
					{
						GoLog(client.Value?.PcName, "Reboot");
						client.Value?.Callback?.Reboot();
						Clients.Remove(client.Key);
					}
					UsingClients.Clear();
				}
				catch { }
			}
			else
			{
				try 
				{
				GoLog(Client.PcName, "Reboot");
				Client.Callback?.Reboot();
				RemoveClient(Client);
				}
				catch { }
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

		public void SendMessages(string PC_Name, string mes)
		{
		    Thread t = null;
			if (PC_Name == null)
				t = new Thread(() => SendingMessages(UsingClients.Values.ToList(), mes));
			else
				t = new Thread(() => SendMessageToClient(Clients[PC_Name], mes));
			t.Start();
		}

		public void SendingMessages(List<Client> Clients, string text)
		{
			foreach (Client Client in Clients)
			{
				try
				{
					GoLog(Client?.PcName, "Send message:\n" + text);
					Client?.Callback?.SendMessage(text);
				}
				catch { }
			}
		}

		public void SendMessageToClient(Client Client, string text)
		{
			try
			{
				Client?.Callback?.SendMessage(text);
			}
			catch { }
		}

		public SuperImage GetScreenShot(string Client)
		{
			var d = Dispatcher.CurrentDispatcher.BeginInvoke(new GetImageDelegate(Clients[Client].Callback.GetScreenshot));
			d.Wait();
			return d.Result as SuperImage;
		}

		public void CloseClientProcess(string PC_Name, int ProcessId)
		{
			GoLog(Clients[PC_Name]?.PcName, "Close process - " + ProcessId);
			new Thread(()=>Clients[PC_Name]?.Callback?.CloseProcess(ProcessId)).Start();
		}

		public SuperFileDirectoryInfo[] GetFiles(string PC_Name, string path)
		{
			GoLog(Clients[PC_Name]?.PcName, "Get files - " + path);
			return GettingFiles(Clients[PC_Name], path);
		}

		public SuperFileDirectoryInfo[] GettingFiles(Client Client, string path)
		{
			return new GetFileInfoDelegate(Client.Callback.GetFiles).Invoke(path);
		}

		public SuperFileDirectoryInfo[] GetDirectories(string PC_Name, string path)
		{
			GoLog(Clients[PC_Name]?.PcName, "Get directories - " + path);
			return Task.Run(() => GettingDirectories(Clients[PC_Name], path)).Result;
		}

		public SuperFileDirectoryInfo[] GettingDirectories(Client Client, string path)
		{
			return new GetFileInfoDelegate(Client.Callback.GetDirectories).Invoke(path);
		}

		public void RemoveFile(Client Client, string path)
		{
			new Thread(()=>Client.Callback?.RemoveFile(path)).Start();
		}

		public string[] FindFiles(Client Client, string mask)
		{
			GoLog(Client?.PcName, "Find files - " + mask);
			return new GetStringsWithStringDelegate(Client.Callback.FindFiles).Invoke(mask);
		}

		public SuperFile TakeFile(Client Client, string path)
		{
			GoLog(Client?.PcName, "Take file - " + path);
			return Client?.Callback?.TakeFile(path);
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
			new Thread(() => TryLog(PC_Name, Action)).Start();
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
			else
			{
				try
				{
					if (new BoolDelegate(Clients[NewPcName].Callback.Ping).Invoke()) return false;
				}
				catch 
				{
					Clients[NewPcName].Callback = OperationContext.Current.GetCallbackChannel<ICallbackService>();
					return true;
				}
				return false;
			}
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
				try
				{
					File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\mail.dat");
				}
				catch { }
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
					return true;
				}
				catch
				{
					return false;
				}
				finally
				{
					sr?.Close();
					fs?.Close();
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
			new Thread(() => DisconnectingClients(PC_Name)).Start();
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

        public string[] GetClientDrives(string PC_Name)
        {
			return new GetStringsDelegate(Clients[PC_Name].Callback.GetDrives).Invoke();
        }

        public SuperFileDirectoryInfo[] GetClientDirectories(string PC_Name, string path)
        {
			return new GetFileInfoDelegate(Clients[PC_Name].Callback.GetDirectories).Invoke(path);
        }

        public SuperFileDirectoryInfo[] GetClientFiles(string PC_Name, string path)
        {
			return new GetFileInfoDelegate(Clients[PC_Name].Callback.GetFiles).Invoke(path);
        }
        public SuperProcess[] GetClientProcesses(string client)
        {
			return new GetProcessesDelegate(Clients[client].Callback.GetProcesses).Invoke();
        }

		public void DisconnectPCs(string PC_Name)
		{
			new Thread(() => DisconnectingPCs(PC_Name)).Start();
		}

		private void DisconnectingPCs(string PC_Name)
		{
			if (PC_Name == null)
			{
				foreach (Client Client in UsingClients.Values)
				{
					Client.Callback.Disconnect();
					Clients.Remove(Client.PcName);
				}
				UsingClients.Clear();
			}
			else
			{
				Clients[PC_Name].Callback.Disconnect();
				UsingClients.Remove(PC_Name);
				Clients.Remove(PC_Name);
			}
		}
	}
}