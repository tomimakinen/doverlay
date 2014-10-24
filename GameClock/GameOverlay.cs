/*
 * Created by SharpDevelop.
 * User: tomi.makinen
 * Date: 3.9.2014
 * Time: 13:24
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Timers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Capture;
using EasyHook;
using Capture.Interface;
using Capture.Hook;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;
using System.Linq;

namespace GameClock
{
    class UnManaged
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern uint SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
            int nWidth, int nHeight, IntPtr hObjectSource,
            int nXSrc, int nYSrc, int dwRop);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
            int nHeight);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hDC);
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    }
  /// <summary>
  /// Overlay time in DX applications
  /// </summary>
  public class GameOverlay
  {
    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    public System.Timers.Timer updateTimer= new System.Timers.Timer(5000);
    public System.Timers.Timer updateTimeTimer = new System.Timers.Timer(5000);
    uint previousProcessID = 0;
    bool timerrunning = false;
    CaptureProcess _captureProcess = null;
    Settings settings = new Settings();
    string configurationFileName;
      // joskus ehkä jotain iracingiin. 
    // mittareita: http://www.codeproject.com/Articles/16082/Analog-and-LED-Meter
    // sdk https://github.com/robgray/iRacingSdkWrapper, http://members.iracing.com/jforum/posts/list/1474031.page
    

    public GameOverlay()
    {
       // Timer
       //updateTimer = new System.Timers.Timer(5000);
       updateTimer.Elapsed +=  OnTimer;
       updateTimeTimer.Elapsed += OnTimeTimer;
       updateTimer.Enabled = true;

       var process = Process.GetCurrentProcess();
       var path = Path.GetDirectoryName(process.MainModule.FileName);
       configurationFileName = Path.Combine(path, "doverlay_configuration.xml");
       Debug.WriteLine(configurationFileName);
       LoadSettings();
    }

    static uint GetActiveProcessID()
    {
        IntPtr hwnd = GetForegroundWindow();
        uint pid;
        GetWindowThreadProcessId(hwnd, out pid);
        return pid;
        // Process p = Process.GetProcessById((int)pid);
        // return p.MainModule.FileName;
    }

    private void OnTimeTimer(Object source, ElapsedEventArgs e)
    {
        if (_captureProcess != null) _captureProcess.ShowConstantText(DateTime.Now.ToString("HH:mm"));

        Bitmap bitmap = new Bitmap("c:\\tmp\\2.png");
        
        ImageConverter converter = new ImageConverter();
        byte[] overlayByteArray = (byte[])converter.ConvertTo(bitmap, typeof(byte[]));
 
        /*
        var proc = Process.GetProcessesByName("MiniCube");
          if (proc[0] != null)
          {
              if(proc[0].MainWindowHandle != null)
              {
                  var captureWindowHandle = proc[0].MainWindowHandle;
                  var img = CaptureWindow(captureWindowHandle) as Bitmap;
                  var Image = img.Clone(new Rectangle(10, 10, 300, 300), System.Drawing.Imaging.PixelFormat.DontCare);
                  var resized = new Bitmap(Image, new Size(100, 100));
                  ImageConverter conv = new ImageConverter();
                  
                  byte[] overlayByteArray = (byte[])conv.ConvertTo(img, typeof(byte[]));
                  resized.Save("c:\\tmp\\test.png", System.Drawing.Imaging.ImageFormat.Png);
        
              }
          }
         */
    }

    private void OnTimer(Object source, ElapsedEventArgs e)
    {
        if (timerrunning) return;

        timerrunning = true;
        var processid = GetActiveProcessID();
        //var filename = Path.GetFileNameWithoutExtension(Process.GetProcessById((int)processid).MainModule.FileName);

        if (previousProcessID != processid)
        {
            if (previousProcessID != 0)
            {
                previousProcessID = 0;
                if (_captureProcess != null)
                {
                    _captureProcess.CaptureInterface.Disconnect();
                    _captureProcess.Dispose();
                    _captureProcess = null;
                    //updateTimeTimer.Enabled = false;
                }
            }
            var process = Process.GetCurrentProcess();
            if (processid == process.Id)
            {
                timerrunning = false;
                return;
            }

            var activeProcess = Process.GetProcessById((int)processid);
            IntPtr h;
            try { h = process.Handle; }
            catch { timerrunning = false; return; } //if access denied..
            if (activeProcess.MainWindowHandle != IntPtr.Zero && process.ProcessName.Contains("WDExpress")==false && process.ProcessName.Contains("explorer")==false)
            {
                previousProcessID = processid;

                CaptureConfig cc = new CaptureConfig()
                {
                    Direct3DVersion = Direct3DVersion.Direct3D9,
                    ShowOverlay = true,
             /*       CaptureProcessName = "MiniCube",
                    CaptureSpeed = 20,
                    CaptureRect = new Rectangle(100,100,500,500),
                    CaptureOverlayPos = new Point(100,100),
                    CaptureOverlaySize = new Size(100, 100)
               */ };
                var foundSettings = settings.Where(x => x.ProcessName.Equals(activeProcess.ProcessName));
                if (foundSettings.Count() == 0 || foundSettings.ElementAt(0).Enabled == false)
                {
                    // no config
                    //Debug.WriteLine("not active: " + activeProcess.ProcessName);
                    if (foundSettings.Count() == 0)
                    {// create entry to config file
                        var p = new ProcessSettings(activeProcess.ProcessName);
                        settings.Add(p);
                        SaveSettings();
                    }
                    timerrunning = false;
                    return ;
                }
                var processSettings = foundSettings.ElementAt(0);
                if(processSettings.EnableCaptureProcess == true)
                {
                    cc.CaptureProcessName = processSettings.CaptureProcessName;
                    cc.CaptureRect = new Rectangle(processSettings.CapturePoint, processSettings.CaptureSize);
                    cc.CaptureOverlayPos = processSettings.OverlayPoint;
                    cc.CaptureOverlaySize = processSettings.OverlaySize;
                    cc.ShowTime = processSettings.ShowTime;
                    cc.CaptureTransparentColor = processSettings.OverlayTransparentColor;
                }
                ra
                if(string.IsNullOrEmpty(cc.CaptureProcessName)== false)
                {
                    var p = Process.GetProcessesByName(cc.CaptureProcessName);
                    if (p.Length > 0 ) cc.CaptureWindowHandle = p[0].MainWindowHandle;
                }

                var captureInterface = new CaptureInterface();
             //   cc.Direct3DVersion = captureInterface.GetProcessDirectXVersion(process);

                if (cc.Direct3DVersion != Direct3DVersion.Unknown)
                {
                    captureInterface.RemoteMessage += captureInterface_RemoteMessage;
                    try
                    {
                        _captureProcess = new CaptureProcess(activeProcess, cc, captureInterface);
                        updateTimeTimer.Enabled = true;
                    }
                    catch (Exception ex)
                    {
                        _captureProcess = null;
                        Console.WriteLine(ex.Message);
                        if (ex.InnerException != null)
                            Console.WriteLine(ex.InnerException.Message);
                    }
                }
                else
                {
                    try
                    {
                        foreach (ProcessModule m in activeProcess.Modules)
                            Debug.WriteLine(m.FileName);
                    }
                    catch { }
                }
            }
            //Debug.WriteLine(filename);
        }
        timerrunning = false;
    }

    void captureInterface_RemoteMessage(MessageReceivedEventArgs message)
    {
        Debug.WriteLine(message);
    }
    
    public void Exit()
    {
      updateTimer.Enabled = false;

      if (previousProcessID != 0)
      {
          previousProcessID = 0;
          if (_captureProcess != null)
          {
              _captureProcess.Dispose();
              _captureProcess = null;
          }
      }
    }
    public static Image CaptureWindow(IntPtr handle)
    {
        // get the hDC of the target window
        IntPtr hdcSrc = UnManaged.GetWindowDC(handle);
        // get the size
        UnManaged.Rect windowRect = new UnManaged.Rect();
        UnManaged.GetWindowRect(handle, ref windowRect);
        int width = windowRect.right - windowRect.left;
        int height = windowRect.bottom - windowRect.top;
        // create a device context we can copy to
        IntPtr hdcDest = UnManaged.CreateCompatibleDC(hdcSrc);
        // create a bitmap we can copy it to,
        // using GetDeviceCaps to get the width/height
        IntPtr hBitmap = UnManaged.CreateCompatibleBitmap(hdcSrc, width, height);
        // select the bitmap object
        IntPtr hOld = UnManaged.SelectObject(hdcDest, hBitmap);
        // bitblt over
        UnManaged.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, UnManaged.SRCCOPY);
        // restore selection
        UnManaged.SelectObject(hdcDest, hOld);
        // clean up 
        UnManaged.DeleteDC(hdcDest);
        UnManaged.ReleaseDC(handle, hdcSrc);

        // get a .NET image object for it
        Image img = Image.FromHbitmap(hBitmap);
        // free up the Bitmap object
        UnManaged.DeleteObject(hBitmap);

        return img;
    }

    private void SaveSettings()
    {
        FileStream fs = new FileStream(configurationFileName, FileMode.Create);
        new XmlSerializer(typeof(Settings)).Serialize(fs, settings);
        fs.Close();
    }

    public void LoadSettings()
    {
        if (System.IO.File.Exists(configurationFileName) == false)
        {
            var p = new ProcessSettings("nw");
            p.Enabled = true;
            p.ShowTime = true;
            p.EnableCaptureProcess = true;
            p.CaptureProcessName = "MiniCube";
            p.CapturePoint = new Point(100, 100);
            p.CaptureSize = new Size(500, 500);
            p.CaptureSpeed = 20;
            p.OverlayPoint = new Point(10, 300);
            p.OverlaySize = new Size(100, 100);
            p.OverlayTransparentColor = "black";
            settings.Add(p);
            //var writer = new StringWriter();


            FileStream fs = new FileStream(configurationFileName, FileMode.Create);
            new XmlSerializer(typeof(Settings)).Serialize(fs, settings);
            fs.Close();
        }
        else
        {
            XmlSerializer x = new XmlSerializer(typeof(Settings));
            FileStream fs = new FileStream(configurationFileName, FileMode.Open);
            settings = (Settings)x.Deserialize(fs);
            fs.Close();
          //  Debug.WriteLine("Config read from file");
        }
    }
  }
}
