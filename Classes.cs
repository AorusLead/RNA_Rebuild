using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;

namespace RNA_Rebuild
{
	[DataContract]
	public class SuperFolder
	{
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public List<SuperFolder> SubFolders { get; set; } = new List<SuperFolder>();
		[DataMember]
		public List<SuperFile> Files { get; set; } = new List<SuperFile>();
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
	public class SuperFileDirectoryInfo
	{
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public string FullName { get; set; }
		[DataMember]
		public bool IsFolder { get; set; }
		[DataMember]
		public bool HaveSub { get; set; }
		[DataMember]
		public Bitmap Icon { get; set; }
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

	[DataContract]
	public class SuperProcess
	{
		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public Bitmap Icon { get; set; }
	}

	[DataContract]
	public class SuperImage
	{
		[DataMember]
		public byte[] Content { get; set; }
	}

	public class Client
	{
		public string PcName { get; set; }
		public IPAddress IP { get; set; }
		public DateTime ConnectionTime { get; set; }
		public ICallbackService Callback { get; set; }
	}

}
