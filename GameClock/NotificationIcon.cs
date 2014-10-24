/*
 * Created by SharpDevelop.
 * User: tomi.makinen
 * Date: 3.9.2014
 * Time: 9:29
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Timers;

namespace GameClock
{
  public sealed class NotificationIcon
  {
    private NotifyIcon notifyIcon;
    private ContextMenu notificationMenu;
    private GameOverlay overlay = new GameOverlay();
    
    
    #region Initialize icon and menu
    public NotificationIcon()
    {
      notifyIcon = new NotifyIcon();
      notificationMenu = new ContextMenu(InitializeMenu());
      
       
      notifyIcon.DoubleClick += IconDoubleClick;
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NotificationIcon));
      notifyIcon.Icon = (Icon)resources.GetObject("$this.Icon");
      Bitmap bmp = new Bitmap("Doverico.png");
      notifyIcon.Icon = Icon.FromHandle(bmp.GetHicon());
      notifyIcon.ContextMenu = notificationMenu;
    }
    
    private MenuItem[] InitializeMenu()
    {
      MenuItem[] menu = new MenuItem[] {
        new MenuItem("Reload Configuration", reloadClick),
        new MenuItem("About", menuAboutClick),
        new MenuItem("Exit", menuExitClick)
      };
      return menu;
    }
    #endregion
    
    #region Main - Program entry point
    /// <summary>Program entry point.</summary>
    /// <param name="args">Command Line Arguments</param>
    [STAThread]
    public static void Main(string[] args)
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      
      bool isFirstInstance;
      // Please use a unique name for the mutex to prevent conflicts with other programs
      using (Mutex mtx = new Mutex(true, "GameClock", out isFirstInstance)) {
        if (isFirstInstance) {
          NotificationIcon notificationIcon = new NotificationIcon();
          notificationIcon.notifyIcon.Visible = true;
          Application.Run();
          notificationIcon.notifyIcon.Dispose();
        } else {
          // The application is already running
          // TODO: Display message box or change focus to existing application instance
        }
      } // releases the Mutex
    }
    #endregion
    
    #region Event Handlers
    private void menuAboutClick(object sender, EventArgs e)
    {
      MessageBox.Show("DirectX overlay application by McIne \nThis application id based on Direct3DHook, which uses EasyHook and SharpDX");
    }
    
    private void menuExitClick(object sender, EventArgs e)
    {
      overlay.Exit();
      Application.Exit();
    }
    
    private void IconDoubleClick(object sender, EventArgs e)
    {
      MessageBox.Show("The icon was double clicked");
    }

    private void reloadClick(object sender, EventArgs e)
    {
      overlay.LoadSettings();
      MessageBox.Show("Configuration loaded");
    }
    #endregion
   
  }
}
