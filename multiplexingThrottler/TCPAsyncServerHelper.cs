using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace multiplexingThrottler
{
    //shamelessly copy/insprited from http://stackoverflow.com/questions/6023264/high-performance-tcp-server-in-c-sharp
    class ClientContext
    {
        public TcpClient Client;
        public Stream Stream;
        public byte[] Buffer = new byte[8];
        public MemoryStream Message = new MemoryStream();
    }

    class DummyServer
    {
        private int port = 0;
        public TcpListener Server = null;
        public DummyServer(int port)
        {
            this.port = port;
        }
        public void GetListener()
        {
            TcpListener server = new TcpListener(IPAddress.Any, port);
            // we set our IP address as server's address, and we also set the port: 9999

            server.Start();  // this will start the server

            ASCIIEncoding encoder = new ASCIIEncoding();
            //we use this to transform the message(string) into a byte array, so we can send it


            TcpClient client = server.AcceptTcpClient();  //if a connection exists, the server will accept it

            NetworkStream ns = client.GetStream(); //we use a networkstream to send the message to the client
            while (client.Connected)  //while the client is connected, we check for incoming messages
            {
                byte[] msg = new byte[1024];     //the messages arrive as byte array
                int r = ns.Read(msg, 0, msg.Length);   //we read the message send by the client

                Console.WriteLine(encoder.GetString(msg)); //now , we write the message as string
            }

        }
    }
}
