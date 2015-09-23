using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace RemoteScreenCapture
{
    class RemoteScreenCapture
    {
        Socket tcpSocket;
        long imageLastId = 0;
        Thread mediaChannelProcessorThread;
        Thread pictureChangerThread;
        Thread windowManagerThread;

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



        public void processMediaChannel()
        {
            byte[] media = new byte[600000];
            FileStream mediaStream = null;
            int mediaSize = 0;

          

            String[] files = Directory.EnumerateFiles(".\\Captures\\", "*.jpeg").ToArray<String>();

            foreach (String file in files) {

                File.Delete(file);          
            
            }

            while (true)
            {
                try
                {
                    mediaSize = tcpSocket.Receive(media);
                }
                catch (Exception e) {
                    System.Console.WriteLine(e.Message);
                    pictureChangerThread.Abort();
                    windowManagerThread.Abort();
                    pictureChangerThread = null;
                    windowManagerThread = null;
                    mediaChannelProcessorThread = null;
                    return;
                }

                 try
                {
                    mediaStream = new FileStream(".\\Captures\\" + Convert.ToString(imageLastId) + ".jpeg",FileMode.Create);
                    mediaStream.Write(media , 0 , mediaSize);
                    mediaStream.Dispose();
                    imageLastId++;
                    
                }

                catch (IOException exception) { System.Console.WriteLine(exception.Message); continue; }

            }
        }



        public void pictureChanger(Object arg)
        {

            String[] files;
            PictureBox box = (PictureBox)arg;
            String oldPath;
            while (true)
            {
                try
                {
                    files = (Directory.EnumerateFiles(".\\Captures\\", "*.jpeg").OrderBy(f => Convert.ToInt16((f.Replace(".jpeg", String.Empty)).Replace(".\\Captures\\", String.Empty)))).ToArray<String>();
                }
                catch (Exception e) {
                    System.Console.WriteLine(e.Message);
                    continue;
                }
                    
                    
                 foreach (String st in files)
                {

                    try
                    {
                        oldPath = box.ImageLocation;
                        try {
                            Bitmap bm = new Bitmap(st);
                            bm.Dispose();
                        }
                        catch (Exception e) {
                            try
                            {
                                File.Delete(st);
                            }
                            catch (Exception ein) { continue; }
                            continue;
                        
                        }
                        box.ImageLocation = st;
                        box.Update();
                        Thread.Sleep(500);

                        if (oldPath != null)
                          File.Delete(oldPath);

                    }
                    catch (IOException exception) { continue; }
                    catch (ArgumentException exception) { continue; }

                }


            }

        }

        public void windowManager()
        {

            Form form = new Form();
            form.Name = "CSF";     
            PictureBox box = new PictureBox();
            Point location = new Point(Screen.PrimaryScreen.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Height);
            form.WindowState = FormWindowState.Maximized;
            form.DesktopLocation = location;
            box.Size = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            box.SizeMode = PictureBoxSizeMode.StretchImage;
            form.Controls.Add(box);
            box.Visible = true;
            String[] files = null;
            try{
                
                files = Directory.EnumerateFiles(".\\Captures\\", "*.jpeg").ToArray<String>();
             }
                catch (Exception e) {
                    System.Console.WriteLine(e.Message);
                 
                }
                    
            if(files != null)
            foreach (String st in files) {
                try
                {
                    File.Delete(st);
                }
                catch (Exception e) {
                    System.Console.WriteLine(e.Message);
                    continue;
                }
            }

            pictureChangerThread = new Thread(pictureChanger);

            try
            {
                pictureChangerThread.Start(box);
            }
            catch (Exception e) {

                System.Console.WriteLine(e.Message);
                return;
            
            }
                form.ShowDialog();

        }



        static void Main(string[] args)
        {

            RemoteScreenCapture rScreenCapture = new RemoteScreenCapture();
            TcpListener listener = rScreenCapture.prepareReceiver(args[0], Convert.ToInt16(args[1]));
            if (listener == null)
            {
                System.Console.ReadKey();
                Environment.Exit(1);
            
            }
 
            rScreenCapture.receiveConnection(listener);
            byte[] loggedKeys = new byte[20000];
            while (true)
            {


                System.Console.WriteLine("Welcome to the remote capture module.");
                System.Console.WriteLine("start");
                System.Console.WriteLine("stop");
                String command = System.Console.ReadLine();

                if (command.Equals("start")){
                
                    if((rScreenCapture.mediaChannelProcessorThread != null) || (rScreenCapture.windowManagerThread != null)){
                        System.Console.WriteLine("Already started!");
                        continue;
                    }

                    rScreenCapture.mediaChannelProcessorThread = new Thread(rScreenCapture.processMediaChannel);
                    rScreenCapture.windowManagerThread = new Thread(rScreenCapture.windowManager);

                    try
                    {
                        rScreenCapture.mediaChannelProcessorThread.Start();
                        rScreenCapture.windowManagerThread.Start();
                    }
                    catch (Exception e) {

                        System.Console.WriteLine(e.Message);
                        continue;
                    
                    }
                }

                try
                {
                    rScreenCapture.tcpSocket.Send(Encoding.ASCII.GetBytes("3 " + command));
                }catch(Exception e){

                    System.Console.WriteLine(e.Message);
                    System.Console.WriteLine("Lost connection. Please wait...");
                    rScreenCapture.receiveConnection(listener);
                    continue;
                }

                if(command.Equals("stop")){
                    if(rScreenCapture.tcpSocket.Connected)
                     rScreenCapture.tcpSocket.Close();
                     Environment.Exit(0);


                }


            }


        }
    }
}


