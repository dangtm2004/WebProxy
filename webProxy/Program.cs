using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebProxy
{
	class Program
	{

		//DECLARATION OF VARIABLES
		static int counter = 20;
		static Byte[][] cache = new Byte[counter][];
		static string[] cacheIndex = new string[counter];
		string data;
		byte[] recvBytes = new byte[4096];
		byte[] buffer;

		static void Main(string[] args)
		{
			//DECLARATION OF VARIABLES
			Socket client = null;
			//GIVES USER INPUT FOR CHOOSING PORT NUMBER
			Console.Write("ENTER PORT TO USE:   ");
			int port = 8080;
			string temp = Console.ReadLine();
			if (isDigitsOnly(temp) && temp != "")
			{
				port = int.Parse(temp);
			}
			else
			{//IF PORT NUMBER PROVIDED IS NOT A NUMBER, OR USER JUST PRESSED ENTER
				port = 8080;
			}

			//ACCEPT NEW CONNECTIONS OF ANY IP ADDRESS of the port that is given and start
			//welcomeSocket = new TcpListener(IPAddress.Any, port);
			IPHostEntry iphostINfo = Dns.GetHostEntry("localhost");
			IPAddress ipadr = iphostINfo.AddressList[0];
			Console.Write(iphostINfo.AddressList[0].ToString());
			IPEndPoint localEnd = new IPEndPoint(ipadr, port);
			try
			{
				Socket welcomeSocket = new Socket(ipadr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				welcomeSocket.Bind(localEnd);
				welcomeSocket.Listen(1);
				Console.Clear(); //Clear console of the ENTER PORT TO USE PART
				while (true)
				{
					Program p = new Program();

					Console.WriteLine("----------------------------------------------------------------------------------------------");
					Console.Write("\t\tWEB PROXY SERVER IS LISTENING");

					bool noCaching = false;
						//Client connects to the socket
						client = welcomeSocket.Accept();
						//Declare a temporary buffer to be of size of the read in client
						p.buffer = new byte[client.Available];
						ClearCurrentConsoleLine();
						//This is the function that deals with client
						p.clientHandler(ref client);
						p = null;
						//Put the console to sleep, I realized after a bit of trial and error, that the website sometimes still decides to connect Socket sucks but I have to use it, even though Socket is disconnected.

				}
			}
			catch(Exception e)
			{
				Console.WriteLine(e.Message);
			}
			
		}

		static bool isDigitsOnly(string str)
		{//This is for checking port inputs and checks whether all digits 
			foreach (char c in str)
			{
				if (c < '0' || c > '9')
					return false;
			}

			return true;
		}

		public int findInCache(string header)   //Cache finding Logic
		{
			for (int i = 0; i < counter; i++)
			{	//if the index is not null and the index is equal to header then return index
				if (cacheIndex[i] != null && cacheIndex[i] == header)
					return i;
			}
			return -1;   //else return -1 which basically means none found
		}

		public void cachingHandler(byte[] data, string header)
		{
			for (int i = 0; i < counter; i++)
			{
				if (cache[i] == null)
				{
					Console.WriteLine("[WRITE FILE INTO CACHE]:" + "cache/" + header);
					cacheIndex[i] = header;
					cache[i] = data;
					return;
				}
			}
			Console.WriteLine("CACHE IS FULL");
			return;
		}

		//DEALS WITH CLIENT SOCKET MAINLY
		public void clientHandler(ref Socket client)
		{   //DECLARATION OF VARIABLE
			string sUrl = "";
			try
			{	//DECLARATION OF VARIABLES
				int index1 = 0, index2 = 0;
				Console.WriteLine("MESSAGE RECEIVED FROM CLIENT:");
				if (client.Connected)
				{
					int bytes = client.Receive(buffer, client.Available, 0);
					if (bytes == 0)
					{
						Console.WriteLine("COULD NOT RETRIEVE ANY DATA");
						return;
					}

					//PARSING LOGIC FOR GETTING ALL THE HEADER STUFF, AS WELL AS GETTING THE URL BY ITSELF
					string mesgClient = Encoding.ASCII.GetString(buffer);
					data = (String)mesgClient;
					//FIND INDEX OF TO SPLIT HEADER
					index1 = data.IndexOf(' ');
					index2 = data.IndexOf(' ', index1 + 1);

					if (index1 == -1 || index2 == -1) throw new IOException();
					//Split said header even further to extract url
					string part1 = data.Substring(index1 + 1, index2 - index1);
					int index3 = part1.IndexOf('/');
					int index4 = part1.IndexOf(' ');
					if (index4 > 1)
						sUrl = part1.Substring(index3 + 1, index4 - 1);
					Console.WriteLine(data);
					Console.WriteLine("\nEND OF MESSAGE RECEIVED FROM CLIENT");
					Console.WriteLine("\n[PARSE MESSAGE HEADER]:");
					Console.Write("METHOD = ");
					string datas = data;
					if (new System.Text.RegularExpressions.Regex("^GET").IsMatch(datas))
						Console.Write("GET, ");

					else if (new System.Text.RegularExpressions.Regex("^POST").IsMatch(datas))
						Console.Write("POST, ");
					part1 = part1.Substring(1, part1.Length - 2);
					string Header = new string(part1);

					Console.Write("DESTADDRESS = " + part1 + ", ");
					index3 = index2 + 4;
					part1 = data.Substring(index2, 9);
					Console.WriteLine("HTTPVERSION = " + part1);
					Console.WriteLine();
					//END OF PARSING LOGIC
					bool noCaching = false;
					//CACHING LOGIC
					Console.Write("\n[LOOK UP IN THE CACHE]: ");
					int temp = findInCache(Header);
					if (temp == -1)
					{//IF NO CACHE FOUND, ACCESS SERVER TO GET DATA AND AFTER SAVE DATA ONTO CACHE
						Console.WriteLine("NOT FOUND, BUILD REQUEST TO SEND TO ORIGINAL SERVER");
						serverHandler(ref client, sUrl,ref noCaching);
						client.Send(recvBytes);    //send bytes received from server to client, (show on browser)
						if(!noCaching)
							cachingHandler(recvBytes, Header);
					}
					else
					{
						//DATA FOUND IN CACHE SO SEND THAT STRAIGHT TO CLIENT SOCKET
						Console.WriteLine("FOUND IN THE CACHE: FILE = cache/" + cacheIndex[temp]);
						client.Send(cache[temp], cache[temp].Length, 0);
					}

					client.Close();
				}
				else{
					Console.WriteLine("CLIENT FAILED TO CONNECT!");
				}
			}
			catch (Exception e)  //ERROR CATCHING
			{
				Console.WriteLine(e.Message);
			}
		}

		//DEALS WITH SERVER SOCKET MAINLY
		public void serverHandler(ref Socket client, string url,ref bool noCaching)
		{
			//Parsing information needed to pass onto server and show on Console
			//adding http on front so Socket can connect to the website, using Uri to build and using that to get port and Host names
			int index = data.IndexOf("\r");
			string surl = data.Substring(index);
			url = "http://" + url;
			surl = "GET " + url + " HTTP/1.1"+surl;
			var path = new Uri(url);
			Console.WriteLine("[PARSE REQUEST HEADER] HOSTNAME IS" + path.Host);
			Console.WriteLine("[PARSE REQUEST HEADER] URL IS" + path.LocalPath);
			Console.WriteLine("\nREQUEST MESSAGE SENT TO ORIGINAL SERVER");
			Console.WriteLine(surl);

			var ssurl = Encoding.ASCII.GetBytes(surl);
			Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			server.Connect(path.Host, path.Port);
			server.Send(ssurl);         //Send Uri to server Socket
			server.Receive(recvBytes);  //Receive data from server socket

			Console.WriteLine("\nEND OF MESSAGE SENT TO ORIGINAL SERVER");
			

			//Parse Response from Server to Proxy
			Console.WriteLine("RESPONSE HEADER FROM PROXY TO CLIENT:");
			string datas = Encoding.ASCII.GetString(recvBytes);
			int index2 = datas.IndexOf('<'), index1 = 0;
			if (index2 > index1)
			{
				string dataServ = datas.Substring(index1, index2 - index1);
				Console.WriteLine(dataServ);
				Console.WriteLine("\nEND OF HEADER");
			}
			if (new System.Text.RegularExpressions.Regex("^200").IsMatch(datas))
			{
				noCaching = true;
			}
			//Shutdown socket SEND/RECEIVE
			server.Shutdown(SocketShutdown.Both);
			//Close socket Server and set to null
			server.Close();
		}

		public static void ClearCurrentConsoleLine()
		{
			int currentLineCursor = Console.CursorTop;
			Console.SetCursorPosition(0, Console.CursorTop);
			Console.Write(new string(' ', Console.WindowWidth));
			Console.SetCursorPosition(0, currentLineCursor);
		}

	}
}