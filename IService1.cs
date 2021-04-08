using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace RNA_Rebuild
{
    public interface ICallbackService
    {
		[OperationContract]
		SuperFile GetScreenshot();

		[OperationContract(IsOneWay = true)]
		void SendMessage(string mes);

		[OperationContract(IsOneWay = true)]
		void Reboot();

		[OperationContract(IsOneWay = true)]
		void ShutDown();

		[OperationContract(IsOneWay = true)]
		void Disconnect();

		[OperationContract]
		Process[] GetProcesses();

		[OperationContract(IsOneWay = true)]
		void CloseProcess(int ProcessId);

		[OperationContract]
		[FaultContract(typeof(Exception))]
		List<string> GetDrives();

		[OperationContract]
		FileInfo[] GetFiles(string path);

		[OperationContract]
		DirectoryInfo[] GetDirectories(string path);

		[OperationContract]
		bool RemoveFile(string path);

		[OperationContract]
		string[] FindFiles(string mask);

		[OperationContract]
		SuperFile TakeFile(string path);

		[OperationContract(IsOneWay = true)]
		void Add_Client(Client cl);

		[OperationContract(IsOneWay = true)]
		void Remove_Client(Client cl);

		[OperationContract(IsOneWay = true)]
		void UpdateClients(Dictionary<string, Client> UsingClients, Dictionary<string, Client> Clients);
	}

    [ServiceContract(CallbackContract = typeof(ICallbackService))]
    public interface IService1
    {
		[OperationContract]
		bool AddClient(string NewPcName, IPAddress NewIP);

		[OperationContract]
		void AddAdmin(string PC_Name, IPAddress IP);

		[OperationContract]
		void DeleteAdmin(string PC_Name);

		[OperationContract]
		SuperFile GetScreenShot(Client client);

		[OperationContract]
		void DisconnectClient(string PC_Name);

		[OperationContract]
		void DeleteClient(string PCName);

		[OperationContract]
		void SetActiveClients(Dictionary<string, Client> Clients);

		[OperationContract]
		Dictionary<string, Client> GetClients();

		[OperationContract]
		Dictionary<string, Client> GetActiveClients();

		[OperationContract]
		Client GetClient(string PCname);

		[OperationContract]
		void AddUsingClient(string PCname);

		[OperationContract]
        void DeleteUsingClient(string PCname);

		[OperationContract]
		void SetSMTPClient(string server, int port, string address, string password, bool ssl, string reciever);

		[OperationContract]
		void ChangeSMTPLogging(bool value);

		[OperationContract]
		void ChangeTXTLogging(bool value);

		[OperationContract]
		void ShutdownPCs(Client Client = null);

		[OperationContract]
		void RebootPCs(Client Client = null);

		[OperationContract]
		bool CheckMail();

		[OperationContract]
		[FaultContract(typeof(Exception))]
		List<string> GetClientDrives(Client client);

		[OperationContract]
		DirectoryInfo[] GetClientDirectories(Client client, string path);

		[OperationContract]
		FileInfo[] GetClientFiles(Client client, string path);

		[OperationContract]
		List<Process> GetClientProcesses(Client client);
	}

}