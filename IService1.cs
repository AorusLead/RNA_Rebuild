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
		SuperImage GetScreenshot();



		[OperationContract(IsOneWay = true)]
		void SendMessage(string mes);




		[OperationContract(IsOneWay = true)]
		void Reboot();

		[OperationContract(IsOneWay = true)]
		void ShutDown();

		[OperationContract(IsOneWay = true)]
		void Disconnect();




		[OperationContract]
		SuperProcess[] GetProcesses();

		[OperationContract(IsOneWay = true)]
		void CloseProcess(int ProcessId);




		[OperationContract]
		string[] GetDrives();

		[OperationContract]
		SuperFileDirectoryInfo[] GetFiles(string path);

		[OperationContract]
		SuperFileDirectoryInfo[] GetDirectories(string path);

		[OperationContract(IsOneWay = true)]
		void RemoveFile(string path);

		[OperationContract]
		string[] FindFiles(string mask);

		[OperationContract]
		SuperFile TakeFile(string path);



		[OperationContract(IsOneWay = true)]
		void UpdateClients(Dictionary<string, Client> UsingClients, Dictionary<string, Client> Clients);

		[OperationContract(IsOneWay = true)]
		void Add_Client(Client cl);

		[OperationContract(IsOneWay = true)]
		void Remove_Client(Client cl);

		[OperationContract]
		bool Ping();
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
		SuperImage GetScreenShot(string client);


		[OperationContract]
		void SetSMTPClient(string server, int port, string address, string password, bool ssl, string reciever);

		[OperationContract]
		void ChangeSMTPLogging(bool value);


		[OperationContract]
		void ChangeTXTLogging(bool value);

		[OperationContract]
		bool CheckMail();




		[OperationContract]
		void ShutdownPCs(string PC_Name);

		[OperationContract]
		void RebootPCs(string PC_Name);




		[OperationContract]
		void DisconnectPCs(string PC_Name);

		[OperationContract]
		string[] GetClientDrives(string PC_Name);

		[OperationContract]
		SuperFileDirectoryInfo[] GetClientDirectories(string PC_Name, string path);

		[OperationContract]
		SuperFileDirectoryInfo[] GetClientFiles(string PC_Name, string path);

		[OperationContract]
		SuperProcess[] GetClientProcesses(string client);

		[OperationContract]
		void CloseClientProcess(string PC_Name, int ProcessId);
	}
}