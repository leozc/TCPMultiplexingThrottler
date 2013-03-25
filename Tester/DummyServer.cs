using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace multiplexingThrottler
{
   
    /**
     * A highly inefficient TCP server.
     * Good for testing :)
     */
    public class DummyServer
    {
        private int port = 0;
        public TcpListener Server = null;
        public ManualResetEvent socketAccepted = new ManualResetEvent(false);
        public ManualResetEvent socketCompleted = new ManualResetEvent(false);
        List<Byte> buffer = new List<byte>(); 
        MemoryStream ms = new MemoryStream();
        public DummyServer(int port)
        {
            
            this.port = port;
        }
        public Byte[] GetBytes()
        {
            return ms.ToArray();
        }
        /**
         * Single Cycle Listener
         */
        public void Start()
        {
            TcpListener server = new TcpListener(IPAddress.Any, port);
            try
            {
                // we set our IP address as server's address, and we also set the port: 9999

                server.Start(); // this will start the server
                socketAccepted.Set();
                ASCIIEncoding encoder = new ASCIIEncoding();
                //we use this to transform the message(string) into a byte array, so we can send it


                TcpClient client = server.AcceptTcpClient(); //if a connection exists, the server will accept it

                NetworkStream ns = client.GetStream(); //we use a networkstream to send the message to the client
                while (client.Connected) //while the client is connected, we check for incoming messages
                {
                    var msg = new byte[1024]; //the messages arrive as byte array
                    int r = ns.Read(msg, 0, msg.Length); //we read the message send by the client
                    if (r == 0)
                        break;
                    for (int i = 0; i < r;i++ )
                        ms.WriteByte(msg[i]);
                    var msgNew = new byte[r];
                    Array.Copy(msg, msgNew, r);
                   // System.Diagnostics.Debug.WriteLine(encoder.GetString(msgNew)); //now , we write the message as string
                }
                socketCompleted.Set();
                System.Diagnostics.Debug.WriteLine("LISTENER CLOSED");
            }
            finally
            {
                server.Stop();
            }

        }
    }
}
