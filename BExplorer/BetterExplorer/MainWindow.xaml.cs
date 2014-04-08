using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Fluent;
using System.Reflection;
using System.IO;
using System.Drawing;
using System.Threading;
using Clipboards = System.Windows.Forms.Clipboard;
using MenuItem = Fluent.MenuItem;
using System.Windows.Shell;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Controls;
using Microsoft.WindowsAPICodePack.Controls.WindowsForms;
using System.Windows.Media.Animation;
using System.Collections.Specialized;
using WindowsHelper;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Threading;
using SevenZip;
using ContextMenu = Fluent.ContextMenu;
using System.Globalization;
using NAppUpdate.Framework;
using BetterExplorerControls;
using Microsoft.WindowsAPICodePack.Shell.FileOperations;
using Microsoft.WindowsAPICodePack.Shell.ExplorerBrowser;
using wyDay.Controls;
using System.Security.Principal;
using Shell32;
using Microsoft.WindowsAPICodePack.Taskbar;
using BetterExplorer.Networks;
using System.Xml.Linq;
using System.Text;
using BetterExplorer.UsbEject;
using LTR.IO;
using LTR.IO.ImDisk;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BetterExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        #region Variables and Constants
        ExplorerBrowser Explorer = new ExplorerBrowser();
        ClipboardMonitor cbm = new ClipboardMonitor();
        ContextMenu cmHistory = new ContextMenu();
        private int LastTabIndex = -1;
        private int BeforeLastTabIndex = -1;
        string StartUpLocation = KnownFolders.Libraries.ParsingName;
        bool IsHFlyoutEnabled;
        bool IsPreviewPaneEnabled;
        bool IsInfoPaneEnabled;
        bool IsNavigationPaneEnabled;
        bool isCheckModeEnabled;
        bool IsExtendedFileOpEnabled;
        bool IsCloseLastTabCloseApp;
        public bool IsrestoreTabs;
        bool IsUpdateCheck;
        bool IsUpdateCheckStartup;
        bool IsConsoleShown;
        int UpdateCheckType;
        public bool isOnLoad;
        System.Windows.Shell.JumpList AppJL = new System.Windows.Shell.JumpList();
        public bool IsCalledFromLoading;
        public bool IsCalledFromViewEnum;
        bool ReadyToChangeLanguage;
        System.Windows.Forms.Timer FocusTimer = new System.Windows.Forms.Timer();
        IntPtr Handle;
        string EditComm = "";
        List<string> Archives = new List<string>(new string[] { ".rar", ".zip", ".7z", ".tar", ".gz", ".xz", ".bz2" });
        List<string> Images = new List<string>(new string[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".wmf" });
        List<string> VirDisks = new List<string>(new string[] { ".iso", ".bin", ".vhd" });
        MenuItem misa;
        MenuItem misd;
        MenuItem misag;
        MenuItem misdg;
        MenuItem misng;
        public static UpdateManager Updater;
        bool inLibrary = false;
        bool inDrive = false;
        //string LastItemSelected = "";
        string SelectedArchive = "";
        //bool KeepFocusOnExplorer = false;
        bool KeepBackstageOpen = false;
        IProgressDialog pd;
        private string ICON_DLLPATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
        bool IsAfterFolderCreate = false;
        bool isGoingBackOrForward;
        bool asFolder = false;
        bool asImage = false;
        bool asArchive = false;
        bool asDrive = false;
        bool asApplication = false;
        bool asLibrary = false;
        bool asVirtualDrive = false;
        bool canlogactions = false;
        string sessionid = DateTime.UtcNow.ToFileTimeUtc().ToString();
        string logdir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BExplorer\\ActionLog\\";
        string satdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\BExplorer_SavedTabs\\";
        string naddir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BExplorer\\NetworkAccounts\\";
        string sstdir;
        List<NavigationLog> reopenabletabs = new List<NavigationLog>();
        bool OverwriteOnRotate = false;
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        BackgroundWorker UpdaterWorker;
        System.Windows.Forms.Timer updateCheckTimer = new System.Windows.Forms.Timer();
        DateTime LastUpdateCheck;
        Int32 UpdateCheckInterval;
        Double CommandPromptWinHeight;
        bool IsViewSelection = false;
        private ShellNotifications.ShellNotifications Notifications = new ShellNotifications.ShellNotifications();
        public List<string> QatItems = new List<string>();
        UIElement curitem = null;
        Boolean IsGlassOnRibonMinimized { get; set; }
        NetworkAccountManager nam = new NetworkAccountManager();
        List<LVItemColor> LVItemsColor { get; set; }
        uint SelectedDriveID = 0;
        string[] InitialTabs;
        #endregion

        #region Properties
        /// <summary>
        /// Gets Previouse Window State
        /// </summary>
        public WindowState PreviouseWindowState { get; private set; }

        /// <summary>
        /// Gets the New context menu
        /// </summary>
        /// <returns>The list of the nwe context menu items</returns>
        private List<string> GetNewContextMenu()
        {
            List<string> TheList = new List<string>();
            RegistryKey reg = Registry.CurrentUser;
            RegistryKey classesrk = reg.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Discardable\PostSetup\ShellNew");
            string[] classes = (string[])classesrk.GetValue("Classes");

            foreach (string item in classes)
            {
                RegistryKey regext = Registry.ClassesRoot;
                RegistryKey extrk = regext.OpenSubKey(item + @"\ShellNew");
                if (extrk != null)
                {
                    string FileName = (string)extrk.GetValue("FileName");
                    TheList.Add(FileName);
                }


            }
            classesrk.Close();
            reg.Close();
            return TheList;
        }

        #endregion

        #region Events

    void InternalRichTextBox_TextChanged(object sender, EventArgs e) {
      ctrlConsole.ScrollToBottom();
    }

    void Explorer_ItemHot(object sender, ExplorerAUItemEventArgs e) {
     // MessageBox.Show(e.Item.ParsingName);
    }

    private void chkIsLastTabCloseApp_Click(object sender, RoutedEventArgs e) {
      this.IsCloseLastTabCloseApp = this.chkIsLastTabCloseApp.IsChecked.Value;
    }

    private void btnConsolePane_Click(object sender, RoutedEventArgs e) {
      if (btnConsolePane.IsChecked.Value) {
        this.IsConsoleShown = true;
        rCommandPrompt.Height = new GridLength(this.CommandPromptWinHeight);
        spCommandPrompt.Height = GridLength.Auto;
        if (!ctrlConsole.IsProcessRunning) {
          ctrlConsole.StartProcess("cmd.exe", Explorer.NavigationLog.CurrentLocation.IsFileSystemObject?String.Format("/K \"{0}\"",Explorer.NavigationLog.CurrentLocation.ParsingName.TrimEnd(new[] { '/', '\\' })):null);
          ctrlConsole.InternalRichTextBox.TextChanged += new EventHandler(InternalRichTextBox_TextChanged);
          ctrlConsole.ClearOutput();
        }
      } else {
        this.IsConsoleShown = false;
        rCommandPrompt.Height = new GridLength(0);
        spCommandPrompt.Height = new GridLength(0);
        if (ctrlConsole.IsProcessRunning)
          ctrlConsole.StopProcess();
      }
    }

    private void ctrlConsole_OnConsoleInput(object sender, ConsoleControl.ConsoleEventArgs args) {

    }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            fmAbout fAbout = new fmAbout(this);
            fAbout.Closed += new EventHandler(fAbout_Closed);
            fAbout.ShowDialog();
        }

        void Explorer_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            switch (e.Effect)
            {
                case System.Windows.Forms.DragDropEffects.All:
                AddToLog(String.Format("The following data was dragged into {0}: {1}", Explorer.NavigationLog.CurrentLocation, e.Data.GetData(DataFormats.FileDrop)));
                    break;
                case System.Windows.Forms.DragDropEffects.Copy:
                    AddToLog(String.Format("The following data was copied into {0}: {1}", Explorer.NavigationLog.CurrentLocation, e.Data.GetData(DataFormats.FileDrop)));
                    break;
                case System.Windows.Forms.DragDropEffects.Link:
                    AddToLog(String.Format("The following data was linked into {0}: {1}", Explorer.NavigationLog.CurrentLocation, e.Data.GetData(DataFormats.FileDrop)));
                    break;
                case System.Windows.Forms.DragDropEffects.Move:
                    AddToLog(String.Format("The following data was moved into {0}: {1}", Explorer.NavigationLog.CurrentLocation, e.Data.GetData(DataFormats.FileDrop)));
                    break;
                case System.Windows.Forms.DragDropEffects.None:
                    AddToLog(String.Format("The following data was dragged into {0}: {1}", Explorer.NavigationLog.CurrentLocation, e.Data.GetData(DataFormats.FileDrop)));
                    break;
                case System.Windows.Forms.DragDropEffects.Scroll:
                    AddToLog(String.Format("The following data was dragged into {0}: {1}", Explorer.NavigationLog.CurrentLocation, e.Data.GetData(DataFormats.FileDrop)));
                    break;
                default:
                    AddToLog(String.Format("The following data was dragged into {0}: {1}", Explorer.NavigationLog.CurrentLocation, e.Data.GetData(DataFormats.FileDrop)));
                    break;
            }
        }



        private void RibbonWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (breadcrumbBarControl1.IsInEditMode)
            {
                breadcrumbBarControl1.ExitEditMode();
            }
        }


        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scviewer = (sender as ScrollViewer);
            scviewer.ScrollToHorizontalOffset(scviewer.HorizontalOffset - e.Delta);
        }

        private void btnBugtracker_Click(object sender, RoutedEventArgs e) {
            Process.Start("http://bugtracker.better-explorer.com");
        }

        private void btnCondSel_DropDownOpened(object sender, EventArgs e)
        {

        }

        private void RibbonWindow_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            //Explorer.Focus();
       //     if (!backstage.IsOpen)
                //Explorer.SetExplorerFocus();
            //  if (breadcrumbBarControl1.IsInEditMode)
            //  {
            //      breadcrumbBarControl1.ExitEditMode();
            //  }
        }

    private void SaveSettings(String openedTabs)
    {
      RegistryKey rk = Registry.CurrentUser;
      RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
      rks.SetValue(@"LastWindowWidth", this.Width);
      rks.SetValue(@"LastWindowHeight", this.Height);
      rks.SetValue(@"LastWindowPosLeft", this.Left);
      rks.SetValue(@"LastWindowPosTop", this.Top);
      if (btnBlue.IsChecked == true)
      {
        rks.SetValue(@"CurrentTheme", "Blue");
      }
      else if (btnSilver.IsChecked == true)
      {
        rks.SetValue(@"CurrentTheme", "Silver");
      }
      else if (btnBlack.IsChecked == true)
      {
        rks.SetValue(@"CurrentTheme", "Black");
      }
      else if (btnGreen.IsChecked == true)
      {
        rks.SetValue(@"CurrentTheme", "Green");
      }
      switch (this.WindowState)
      {
        case System.Windows.WindowState.Maximized:
          rks.SetValue(@"LastWindowState", 2);
          break;
        case System.Windows.WindowState.Minimized:
          rks.SetValue(@"LastWindowState", 1);
          break;
        case System.Windows.WindowState.Normal:
          rks.SetValue(@"LastWindowState", 0);
          break;
        default:
          rks.SetValue(@"LastWindowState", -1);
          break;
      }
      rks.SetValue(@"IsRibonMinimized", TheRibbon.IsMinimized);
      rks.SetValue(@"OpenedTabs", openedTabs);

      if (FlowDirection == System.Windows.FlowDirection.RightToLeft)
      {
        rks.SetValue(@"RTLMode", "true");
      }
      else
      {
        rks.SetValue(@"RTLMode", "false");
      }

      //asFolder = ((int)rks.GetValue(@"AutoSwitchFolderTools", 0) == 1);
      //asArchive = ((int)rks.GetValue(@"AutoSwitchArchiveTools", 1) == 1);
      //asImage = ((int)rks.GetValue(@"AutoSwitchImageTools", 1) == 1);
      //asApplication = ((int)rks.GetValue(@"AutoSwitchApplicationTools", 0) == 1);
      //asLibrary = ((int)rks.GetValue(@"AutoSwitchLibraryTools", 1) == 1);
      //asDrive = ((int)rks.GetValue(@"AutoSwitchDriveTools", 0) == 1);

      rks.SetValue(@"AutoSwitchFolderTools", GetIntegerFromBoolean(asFolder));
      rks.SetValue(@"AutoSwitchArchiveTools", GetIntegerFromBoolean(asArchive));
      rks.SetValue(@"AutoSwitchImageTools", GetIntegerFromBoolean(asImage));
      rks.SetValue(@"AutoSwitchApplicationTools", GetIntegerFromBoolean(asApplication));
      rks.SetValue(@"AutoSwitchLibraryTools", GetIntegerFromBoolean(asLibrary));
      rks.SetValue(@"AutoSwitchDriveTools", GetIntegerFromBoolean(asDrive));
      rks.SetValue(@"AutoSwitchVirtualDriveTools", GetIntegerFromBoolean(asVirtualDrive));
      rks.SetValue(@"IsLastTabCloseApp", GetIntegerFromBoolean(this.IsCloseLastTabCloseApp));
      if (this.IsConsoleShown)
        rks.SetValue(@"CmdWinHeight", rCommandPrompt.ActualHeight, RegistryValueKind.DWord);
      rks.SetValue(@"IsConsoleShown", this.IsConsoleShown ? 1 : 0);

      if (this.TabbaBottom.IsChecked == true)
      {
        rks.SetValue(@"TabBarAlignment", "bottom");
      }
      else
      {
        rks.SetValue(@"TabBarAlignment", "top");
      }

      rks.Close();
    }
    private void RibbonWindow_Closing(object sender, CancelEventArgs e)
        {

      if (App.isStartNewWindows)
      {
        if (r != null)
        {
          r.Close();
        }

      }

      if (this.OwnedWindows.OfType<FileOperationDialog>().Count() > 0) {
        
        if (MessageBox.Show("Are you sure you want to cancel all running file operation tasks?", "", MessageBoxButton.YesNo) == MessageBoxResult.No) {
          e.Cancel = true;
          return;
        }
      }
            //Explorer.automan.Dispose();
            //if (PicturePreview != null)
            //{
            //    PicturePreview.Close(); 
            //}
      //if (ctrlConsole.IsProcessRunning)
      //  ctrlConsole.StopProcess();

            string OpenedTabs = "";
            foreach (ClosableTabItem item in tabControl1.Items)
            {
                OpenedTabs += ";" + item.Path.ParsingName;
            }

      if (this.WindowState != System.Windows.WindowState.Minimized)
        SaveSettings(OpenedTabs);

            SaveHistoryToFile(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\history.txt", breadcrumbBarControl1.HistoryItems.Select(s => s.DisplayName).ToList());
            AddToLog("Session Ended");
      if (!App.isStartNewWindows)
      {
        e.Cancel = true;
        App.isStartMinimized = true;
        this.WindowState = System.Windows.WindowState.Minimized;
        this.Visibility = System.Windows.Visibility.Hidden; 
      }
        }

        private void backstage_IsOpenChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            autoUpdater.Visibility = System.Windows.Visibility.Visible;
            autoUpdater.UpdateLayout();

            if (KeepBackstageOpen == true)
            {
                backstage.IsOpen = true;
                KeepBackstageOpen = false;
            }
        }

        private void RibbonWindow_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsAfterRename = true;
            if (breadcrumbBarControl1.IsInEditMode)
                breadcrumbBarControl1.ExitEditMode();
        }

        private void TheRibbon_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (TheRibbon.IsMinimized)
            {
        if (this.IsGlassOnRibonMinimized)
        {
          System.Windows.Point p =
          ShellVView.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
          this.GlassBorderThickness = new Thickness(8, this.WindowState == System.Windows.WindowState.Maximized ? p.Y : p.Y - 2, 8, 8); ;
        }
                try
                {
                    this.SetBlur(false);
                }
                catch (Exception)
                {


                }
            }
            else
            {
        if (this.IsGlassOnRibonMinimized)
        {
          System.Windows.Point p = backstage.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
          this.GlassBorderThickness = new Thickness(8, p.Y + backstage.ActualHeight + 2, 8, 8);
        }
                try
                {
                    this.SetBlur(true);
                }
                catch (Exception)
                {


                }
            }

        }

        private void TheRibbon_CustomizeQuickAccessToolbar(object sender, EventArgs e)
        {
      OpenCustomizeQAT();
        }

        private static int GetIntegerFromBoolean(bool value)
        {
            if (value == true)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

    private void LoadInitialWindowPositionAndState()
    {
      RegistryKey rk = Registry.CurrentUser;
      RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", false);

      if (rks == null)
      {
        return;
      }

      this.Width = Convert.ToDouble(rks.GetValue(@"LastWindowWidth", "640"));
      this.Height = Convert.ToDouble(rks.GetValue(@"LastWindowHeight", "480"));

      System.Drawing.Point Location = new System.Drawing.Point();
      try
      {
        Location = new System.Drawing.Point(
          Convert.ToInt32(rks.GetValue(@"LastWindowPosLeft", "0")),
          Convert.ToInt32(rks.GetValue(@"LastWindowPosTop", "0")));
      }
      catch
      {

      }
      if (Location != null)
      {
        this.Left = Location.X;
        this.Top = Location.Y;
      }

      switch (Convert.ToInt32(rks.GetValue(@"LastWindowState")))
      {
        case 2:
          this.WindowState = System.Windows.WindowState.Maximized;
          break;
        case 1:
          this.WindowState = System.Windows.WindowState.Minimized;
          break;
        case 0:
          this.WindowState = System.Windows.WindowState.Normal;
          break;
        default:
          this.WindowState = System.Windows.WindowState.Maximized;
          break;
      }

      int isGlassOnRibonMinimized = (int)rks.GetValue(@"RibbonMinimizedGlass", 1);
      this.IsGlassOnRibonMinimized = isGlassOnRibonMinimized == 1;
      chkRibbonMinimizedGlass.IsChecked = this.IsGlassOnRibonMinimized;

      TheRibbon.IsMinimized = Convert.ToBoolean(rks.GetValue(@"IsRibonMinimized", false));

      //CommandPrompt window size
      this.CommandPromptWinHeight = Convert.ToDouble(rks.GetValue(@"CmdWinHeight", 100));
      rCommandPrompt.Height = new GridLength(this.CommandPromptWinHeight);

      if ((int)rks.GetValue(@"IsConsoleShown", 0) == 1)
      {
        rCommandPrompt.Height = new GridLength(this.CommandPromptWinHeight);
        spCommandPrompt.Height = GridLength.Auto;
        if (!ctrlConsole.IsProcessRunning)
        {
          Task.Run(() =>
          {
            ctrlConsole.StartProcess("cmd.exe", null);
          });
          ctrlConsole.InternalRichTextBox.TextChanged += new EventHandler(InternalRichTextBox_TextChanged);
          ctrlConsole.ClearOutput();
        }
      }
      else
      {
        rCommandPrompt.Height = new GridLength(0);
        spCommandPrompt.Height = new GridLength(0);
      }

      rks.Close();
    }

    private void LoadColorCodesFromFile()
    {
      Task.Run(() =>
      {
        string itemColorSettingsLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"BExplorer\itemcolors.cfg");

        if (File.Exists(itemColorSettingsLocation))
        {
          XDocument docs = XDocument.Load(itemColorSettingsLocation);

          this.LVItemsColor = docs.Root.Elements("ItemColorRow")
                             .Select(element => new LVItemColor(element.Elements().ToArray()[0].Value, System.Drawing.Color.FromArgb(Convert.ToInt32(element.Elements().ToArray()[1].Value))))
                             .ToList();

        }
      });
    }

    private void RibbonWindow_Initialized(object sender, EventArgs e)
        {
            AppCommands.RoutedNewTab.InputGestures.Add(new KeyGesture(Key.T, ModifierKeys.Control));
            AppCommands.RoutedEnterInBreadCrumbCombo.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Alt));
            AppCommands.RoutedChangeTab.InputGestures.Add(new KeyGesture(Key.Tab, ModifierKeys.Control));
            AppCommands.RoutedCloseTab.InputGestures.Add(new KeyGesture(Key.W, ModifierKeys.Control));

      LoadInitialWindowPositionAndState();

      LoadColorCodesFromFile();
        }

        /// <summary>
        /// Occures on layout change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            PreviouseWindowState = WindowState;
        }

    private void SetSortingAndGroupingButtons()
    {
      btnSort.Items.Clear();
      btnGroup.Items.Clear();

      WindowsAPI.SORTCOLUMN sc;
      Explorer.GetSortColInfo(out sc);
      WindowsAPI.PROPERTYKEY pkg;
      bool GroupDir;
      Explorer.GetGroupColInfo(out pkg, out GroupDir);

      try
      {
        foreach (Collumns item in Explorer.AvailableVisibleColumns)
        {

          if (item != null)
          {
            MenuItem mi = new MenuItem();
            mi.Header = item.Name;
            mi.Tag = item;
            mi.GroupName = "GR2";
            mi.Focusable = false;
            mi.IsCheckable = true;
            if ((item.pkey.fmtid == sc.propkey.fmtid) && (item.pkey.pid == sc.propkey.pid))
            {
              mi.IsChecked = true;
            }
            else
            {
              mi.IsChecked = false;
            }
            mi.Click += mi_Click;
            btnSort.Items.Add(mi);

            MenuItem mig = new MenuItem();
            mig.Header = item.Name;
            mig.Tag = item;
            mig.GroupName = "GR3";
            mig.Focusable = false;
            mig.IsCheckable = true;
            if ((item.pkey.fmtid == pkg.fmtid) && (item.pkey.pid == pkg.pid))
            {
              mig.IsChecked = true;
            }
            else
            {
              mig.IsChecked = false;
            }
            mig.Click += mig_Click;
            btnGroup.Items.Add(mig);
          }
        }
      }
      catch (Exception ex)
      {
        //FIXME: I disable this message becasue of strange null after filter
        MessageBox.Show("BetterExplorer had an issue loading the visible columns for the current view. You might not be able to sort or group items.", ex.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
      }

      Separator sp = new Separator();
      sp.Focusable = false;
      btnSort.Items.Add(sp);
      misa = new MenuItem();
      misa.Click += misa_Click;
      misa.Focusable = false;
      misa.Header = FindResource("miAscending");
      misa.GroupName = "GR1";
      misa.IsCheckable = true;

      misd = new MenuItem();
      misd.Header = FindResource("miDescending");
      misd.IsCheckable = true;
      misd.Click += misd_Click;
      misd.Focusable = false;
      misd.GroupName = "GR1";
      if (sc.direction == WindowsAPI.SORT.ASCENDING)
      {
        misa.IsChecked = true;
      }
      else
      {
        misd.IsChecked = true;
      }
      btnSort.Items.Add(misa);
      btnSort.Items.Add(misd);
      misng = new MenuItem();
      misng.Header = "(none)";
      misng.Focusable = false;
      misng.GroupName = "GR3";
      misng.IsCheckable = true;
      misng.IsChecked = pkg.fmtid == Guid.Empty;
      misng.Click += misng_Click;
      btnGroup.Items.Add(misng);
      Separator spg = new Separator();
      btnGroup.Items.Add(spg);
      misag = new MenuItem();
      misag.Focusable = false;
      misag.Header = FindResource("miAscending");
      misag.IsCheckable = true;


      misag.GroupName = "GR4";

      misdg = new MenuItem();
      misdg.Focusable = false;
      misdg.Header = FindResource("miDescending");
      misdg.IsCheckable = true;
      misdg.GroupName = "GR4";
      if (GroupDir)
      {
        misag.IsChecked = true;
      }
      else
      {
        misdg.IsChecked = true;
      }

      btnGroup.Items.Add(misag);
      btnGroup.Items.Add(misdg);

    }
    private void SetUpViewGallery()
    {
      if (Explorer.ContentOptions.ThumbnailSize == 256)
      {
        ViewGallery.SelectedIndex = 0;

      }
      if (Explorer.ContentOptions.ThumbnailSize == 96)
      {
        ViewGallery.SelectedIndex = 1;

      }
      if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Icon && Explorer.ContentOptions.ThumbnailSize == 48)
      {
        ViewGallery.SelectedIndex = 2;
        btnSbIcons.IsChecked = true;

      }
      if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.SmallIcon)
      {
        ViewGallery.SelectedIndex = 3;

      }
      else
      {
        btnSbIcons.IsChecked = false;
      }
      if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.List)
      {
        ViewGallery.SelectedIndex = 4;
      }
      if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Details)
      {
        ViewGallery.SelectedIndex = 5;
        btnSbDetails.IsChecked = true;
      }
      else
      {
        btnSbDetails.IsChecked = false;
      }
      if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Tile)
      {
        ViewGallery.SelectedIndex = 6;
        btnSbTiles.IsChecked = true;
      }
      else
      {
        btnSbTiles.IsChecked = false;
      }
      if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Content)
      {
        ViewGallery.SelectedIndex = 7;
      }
    }

    private void SetupColumnsButton(Collumns[] AllAvailColls)
    {
      btnMoreColls.Items.Clear();

      for (int j = 1; j < 10; j++)
      {
        try
        {
          MenuItem mic = new MenuItem();
          mic.Header = AllAvailColls[j].Name;
          mic.Tag = AllAvailColls[j].pkey;
          mic.Click += mic_Click;
          mic.Focusable = false;
          mic.IsCheckable = true;
          foreach (Collumns col in Explorer.AvailableVisibleColumns)
          {
            if (col.Name == AllAvailColls[j].Name)
            {
              mic.IsChecked = true;
            }
          }
          btnMoreColls.Items.Add(mic);
        }
        catch (Exception)
        {

        }


      }

      int ItemsCount = Explorer.GetItemsCount();


      if (ItemsCount == 0)
      {
        sbiItemsCount.Visibility = System.Windows.Visibility.Collapsed;
      }
      else
      {
        sbiItemsCount.Visibility = System.Windows.Visibility.Visible;
      }
      if (ItemsCount == 1)
      {
        sbiItemsCount.Content = "1 item";
      }
      else
      {
        sbiItemsCount.Content = ItemsCount + " items";
      }


      Separator sep = new Separator();
      btnMoreColls.Items.Add(sep);
      MenuItem micm = new MenuItem();
      micm.Header = FindResource("btnMoreColCP");
      micm.Focusable = false;
      micm.Tag = AllAvailColls;
      micm.Click += new RoutedEventHandler(micm_Click);
      btnMoreColls.Items.Add(micm);
    }

    private void Explorer_ViewEnumerationComplete(object sender, EventArgs e)
        {
            IsCalledFromViewEnum = true;
            zoomSlider.Value = Explorer.ContentOptions.ThumbnailSize;
            IsCalledFromViewEnum = false;

      Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)(() =>
      {

        try
        {
          IShellView isv = Explorer.GetShellView();
          Collumns[] AllAvailColls = Explorer.AvailableColumns(true);

          SetupColumnsButton(AllAvailColls);

          SetSortingAndGroupingButtons();
        }
        catch (Exception)
        {

        }
                        
          
            }));

      IsViewSelection = false;
      SetUpViewGallery();
      IsViewSelection = true;
      Explorer.ContentOptions.CheckSelect = this.isCheckModeEnabled;
        }

        void misd_Click(object sender, RoutedEventArgs e)
        {
            foreach (object item in btnSort.Items)
            {
                if (item is MenuItem)
                    if (((MenuItem)item).IsChecked && ((MenuItem)item != (sender as MenuItem)))
                    {
            Explorer.SetSortCollumn(((Collumns)((MenuItem)item).Tag).pkey, WindowsAPI.SORT.DESCENDING);
                    }
            }
        }

        void misa_Click(object sender, RoutedEventArgs e)
        {
            foreach (object item in btnSort.Items)
            {
                if (item is MenuItem)
                    if (((MenuItem)item).IsChecked && ((MenuItem)item != (sender as MenuItem)))
                    {
            Explorer.SetSortCollumn(((Collumns)((MenuItem)item).Tag).pkey, WindowsAPI.SORT.ASCENDING);
                    }
            }
        }

        void timerv_Tick(object sender, EventArgs e)
        {
            DoubleAnimation da = new DoubleAnimation(CurrentProgressValue, CurrentProgressValue + 1, new Duration(new TimeSpan(0, 0, 2)));
            da.FillBehavior = FillBehavior.Stop;
            CurrentProgressValue = CurrentProgressValue + 1;
            (sender as DispatcherTimer).Stop();
        }

        void micm_Click(object sender, RoutedEventArgs e)
        {

      using (MoreColumns fMoreCollumns = new MoreColumns())
      {
        fMoreCollumns.PopulateAvailableColumns((Collumns[])(sender as MenuItem).Tag, Explorer, this.PointToScreen(Mouse.GetPosition(this)));
      }
        }

        void mic_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (sender as MenuItem);
      WindowsAPI.PROPERTYKEY pkey = (WindowsAPI.PROPERTYKEY)mi.Tag;
            Explorer.SetColInView(pkey, !mi.IsChecked);
        }

        [DllImport("shell32.dll")]
        public static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, [Out()] out IntPtr pidl, uint sfgaoIn, [Out()] out uint psfgaoOut);

    private void PreselectItemsIntoExplorerControl()
    {
      var itb = tabControl1.SelectedItem as ClosableTabItem;
      if (itb.SelectedItems != null)
      {
        foreach (var item in itb.SelectedItems)
        {
          try
          {
            Explorer.SelectItem(ShellObject.FromParsingName(item));
          }
          catch (Exception)
          {
            //! Sometimes there can be malformed or invalid string on some systems so we have to catch this error.
          }
        }
        Explorer.SetExplorerFocus();
      }
    }
    private void SetupUIonNavComplete(NavigationCompleteEventArgs e)
    {
      if (e.NewLocation.IsFileSystemObject)
      {
        btnSizeChart.IsEnabled = true;
      }
      else
      {
        btnSizeChart.IsEnabled = false;
      }


      btnAutosizeColls.IsEnabled =
          Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Details ?
            true : false;


      if (e.NewLocation.ParsingName == KnownFolders.RecycleBin.ParsingName)
      {
        if (Explorer.GetItemsCount() > 0)
        {
          miRestoreALLRB.Visibility = System.Windows.Visibility.Visible;
        }
      }
      else
      {
        miRestoreALLRB.Visibility = System.Windows.Visibility.Collapsed;
      }
      bool IsChanged = (Explorer.GetSelectedItemsCount() > 0);
      bool isFuncAvail;
      if (Explorer.SelectedItems.Count == 1)
      {
        isFuncAvail = (Explorer.SelectedItems[0].IsFileSystemObject) ||
          Explorer.NavigationLog.CurrentLocation.ParsingName ==
            KnownFolders.Libraries.ParsingName;
      }
      else
      {
        if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
          !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
          ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
        isFuncAvail = true;
      }
      btnCopy.IsEnabled = IsChanged;
      //btnPathCopy.IsEnabled = IsChanged;
      btnCut.IsEnabled = IsChanged;
      btnRename.IsEnabled = IsChanged;
      btnDelete.IsEnabled = IsChanged && isFuncAvail;
      btnCopyto.IsEnabled = IsChanged;
      btnMoveto.IsEnabled = IsChanged;
      btnSelNone.IsEnabled = IsChanged;
      btnUpLevel.IsEnabled = !(Explorer.NavigationLog.CurrentLocation.Parent == null);
    }

    private void SetUpJumpListOnNavComplete()
    {
      IntPtr pIDL = IntPtr.Zero;

      try
      {
        uint iAttribute;
        SHParseDisplayName(Explorer.NavigationLog.CurrentLocation.ParsingName,
            IntPtr.Zero, out pIDL, (uint)0, out iAttribute);

        WindowsAPI.SHFILEINFO sfi = new WindowsAPI.SHFILEINFO();
        IntPtr Res = IntPtr.Zero;
        if (pIDL != IntPtr.Zero)
        {
          if (!Explorer.NavigationLog.CurrentLocation.IsFileSystemObject)
          {
            Res = WindowsAPI.SHGetFileInfo(pIDL, 0, ref sfi, (uint)Marshal.SizeOf(sfi), WindowsAPI.SHGFI.IconLocation | WindowsAPI.SHGFI.SmallIcon | WindowsAPI.SHGFI.PIDL);
          }

        }

        if (Explorer.NavigationLog.CurrentLocation.IsFileSystemObject)
        {
          WindowsAPI.SHGetFileInfo(Explorer.NavigationLog.CurrentLocation.ParsingName, 0, ref sfi, (uint)Marshal.SizeOf(sfi), (uint)WindowsAPI.SHGFI.IconLocation | (uint)WindowsAPI.SHGFI.SmallIcon);

        }
        JumpTask JTask = new JumpTask();
        JTask.ApplicationPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        JTask.Arguments = String.Format("\"{0}\"", Explorer.NavigationLog.CurrentLocation.ParsingName);
        JTask.Title = Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default);
        JTask.IconResourcePath = sfi.szDisplayName;
        JTask.IconResourceIndex = sfi.iIcon;
        System.Windows.Shell.JumpList.AddToRecentCategory(JTask);
        AppJL.Apply();

      }
      finally
      {

        if (pIDL != IntPtr.Zero)
          Marshal.FreeCoTaskMem(pIDL);
      }
    }

    private void TranslateUI()
    {
      if (Explorer.NavigationLog.CurrentLocation.ParsingName == KnownFolders.Libraries.ParsingName)
      {
        btnCreateFolder.Header = FindResource("btnNewLibraryCP");  //"New Library";
        stNewFolder.Title = FindResource("btnNewLibraryCP").ToString();//"New Library";
        stNewFolder.Text = "Creates a new library in the current folder.";
        stNewFolder.Image = new BitmapImage(new Uri(@"/BetterExplorer;component/Images/newlib32.png", UriKind.Relative));
        btnCreateFolder.LargeIcon = @"..\Images\newlib32.png";
        btnCreateFolder.Icon = @"..\Images\newlib16.png";
      }
      else
      {
        btnCreateFolder.Header = FindResource("btnNewFolderCP");//"New Folder";
        stNewFolder.Title = FindResource("btnNewFolderCP").ToString(); //"New Folder";
        stNewFolder.Text = "Creates a new folder in the current folder";
        stNewFolder.Image = new BitmapImage(new Uri(@"/BetterExplorer;component/Images/folder_new32.png", UriKind.Relative));
        btnCreateFolder.LargeIcon = @"..\Images\folder_new32.png";
        btnCreateFolder.Icon = @"..\Images\folder_new16.png";
      }
    }
    private bool SetUpNewFolderButtons()
    {
      bool isinLibraries = false;
      if (Explorer.NavigationLog.CurrentLocation.Parent != null)
      {
        if (Explorer.NavigationLog.CurrentLocation.Parent.ParsingName ==
              KnownFolders.Libraries.ParsingName)
        {
          isinLibraries = true;
        }
        else
        {
          isinLibraries = false;
        }
      }

      btnCreateFolder.IsEnabled = Explorer.NavigationLog.CurrentLocation.IsFileSystemObject ||
          (Explorer.NavigationLog.CurrentLocation.ParsingName == KnownFolders.Libraries.ParsingName) ||
          (isinLibraries);

      TranslateUI();

      return isinLibraries;
    }
    private void SetUpButtonVisibilityOnNavComplete(bool isinLibraries)
    {
      if (Explorer.NavigationLog.CurrentLocation.ParsingName.Contains(KnownFolders.Libraries.ParsingName) &&
                                    Explorer.NavigationLog.CurrentLocation.ParsingName != KnownFolders.Libraries.ParsingName)
      {
        ctgLibraries.Visibility = System.Windows.Visibility.Visible;
        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
        ctgImage.Visibility = System.Windows.Visibility.Collapsed;
        ctgArchive.Visibility = System.Windows.Visibility.Collapsed;
        ctgVirtualDisk.Visibility = System.Windows.Visibility.Collapsed;
        ctgExe.Visibility = System.Windows.Visibility.Collapsed;
        inLibrary = true;

        //MessageBox.Show("In Library");
        try
        {
          ShellLibrary lib =
              ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
          IsFromSelectionOrNavigation = true;
          chkPinNav.IsChecked = lib.IsPinnedToNavigationPane;
          IsFromSelectionOrNavigation = false;
          foreach (ShellObject item in lib)
          {
            MenuItem miItem = new MenuItem();
            miItem.Header = item.GetDisplayName(DisplayNameType.Default);
            miItem.Tag = item;
            item.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
            item.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
            miItem.Icon = item.Thumbnail.BitmapSource;
            miItem.GroupName = "GRDS1";
            miItem.IsCheckable = true;
            miItem.IsChecked = (item.ParsingName == lib.DefaultSaveFolder);
            miItem.Click += new RoutedEventHandler(miItem_Click);
            btnDefSave.Items.Add(miItem);
          }

          btnDefSave.IsEnabled = !(lib.Count == 0);
          lib.Close();
        }
        catch
        {

        }
      }
      else
      {
        if (!Explorer.NavigationLog.CurrentLocation.ParsingName.ToLowerInvariant().EndsWith("library-ms"))
        {
          btnDefSave.Items.Clear();
          ctgLibraries.Visibility = System.Windows.Visibility.Collapsed;
          inLibrary = false;
        }
        //MessageBox.Show("Not in Library");
      }
      if (Explorer.NavigationLog.CurrentLocation.IsDrive)
      {
        ctgDrive.Visibility = System.Windows.Visibility.Visible;
        inDrive = true;
        //MessageBox.Show("In Drive");
      }
      else
      {
        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
        inDrive = false;
        //MessageBox.Show("Not In Drive");
      }
      if (isinLibraries)
      {
        ctgFolderTools.Visibility = Visibility.Collapsed;
      }
    }
    private void SetUpConsoleWindow(NavigationCompleteEventArgs e)
    {
      try
      {
        if (e.NewLocation.IsFileSystemObject)
        {
          ctrlConsole.WriteInput(String.Format("cd \"{0}\"", e.NewLocation.ParsingName), System.Drawing.Color.Red, false);
        }
        if (ctrlConsole.InternalRichTextBox.Lines.Last().Substring(0, ctrlConsole.InternalRichTextBox.Lines.Last().IndexOf(Char.Parse(@"\")) + 1) != Path.GetPathRoot(e.NewLocation.ParsingName))
        {
          ctrlConsole.WriteInput(Path.GetPathRoot(e.NewLocation.ParsingName).TrimEnd(Char.Parse(@"\")), System.Drawing.Color.Red, false);
        }
      }
      catch (Exception)
      {
        // catch all expetions for illigal path
      }
    }
    private void SetUpBreadcrumbbarOnNavComplete(NavigationCompleteEventArgs e)
    {
      this.breadcrumbBarControl1.LoadDirectory(e.NewLocation);
      this.breadcrumbBarControl1.LastPath = e.NewLocation.ParsingName;
      this.Title = "Better Explorer - " + e.NewLocation.GetDisplayName(DisplayNameType.Default);
      e.NewLocation.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
      e.NewLocation.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
      try
      {
        (tabControl1.SelectedItem as ClosableTabItem).Header = e.NewLocation.GetDisplayName(DisplayNameType.Default);
        (tabControl1.SelectedItem as ClosableTabItem).TabIcon = e.NewLocation.Thumbnail.BitmapSource;
      }
      catch (Exception)
      {

      }

      try
      {

        //ClosableTabItem it = new ClosableTabItem();
        //CreateTabbarRKMenu(it);

        (tabControl1.Items[tabControl1.SelectedIndex] as ClosableTabItem).Path = Explorer.NavigationLog.CurrentLocation;
        if (!isGoingBackOrForward)
        {
          try
          {
            if ((tabControl1.Items[tabControl1.SelectedIndex] as ClosableTabItem).log.ForwardEntries.Count() > 1)
              (tabControl1.Items[tabControl1.SelectedIndex] as ClosableTabItem).log.ClearForwardItems();
          }
          catch (Exception)
          {

          }

          (tabControl1.Items[tabControl1.SelectedIndex] as ClosableTabItem).log.CurrentLocation = Explorer.NavigationLog.CurrentLocation;

        }

        isGoingBackOrForward = false;

        leftNavBut.IsEnabled = (tabControl1.Items[tabControl1.SelectedIndex] as ClosableTabItem).log.CanNavigateBackwards;
        rightNavBut.IsEnabled = (tabControl1.Items[tabControl1.SelectedIndex] as ClosableTabItem).log.CanNavigateForwards;
      }
      catch (Exception)
      {

      }
    }
    async void Explorer_NavigationComplete(object sender, NavigationCompleteEventArgs e)
        {
            try
      {
        int explorerSelectedItemsCount = 0;
        Task.Run(() =>
        {
          Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (ThreadStart)(() =>
          {
            SetUpBreadcrumbbarOnNavComplete(e);

          }));
        });

        Task.Run(() =>
        {
          Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)(() =>
                    {
                      PreselectItemsIntoExplorerControl();

                      ConstructMoveToCopyToMenu();

                      SetupUIonNavComplete(e);

                      SetUpJumpListOnNavComplete();


                      bool isinLibraries = SetUpNewFolderButtons();

                      SetUpButtonVisibilityOnNavComplete(isinLibraries);

                      SetUpConsoleWindow(e);

                      SetupUIOnSelectOrNavigate(Explorer.GetSelectedItemsCount(), true);
                    }
                  ));
        });

        #region StatusBar selected items counter
        await Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)(() =>
        {
          explorerSelectedItemsCount = Explorer.GetSelectedItemsCount();
        }));

        SetUpStatusBarOnSelectOrNavigate(explorerSelectedItemsCount);
        #endregion
            }
            catch (Exception exe)
            {
              ShellObject ne = e.NewLocation;
              bool isinLibraries = false;
              bool itisLibraries = false;
        if (ne != null)
        {

            if (ne.Parent.ParsingName == KnownFolders.Libraries.ParsingName)
            {
                isinLibraries = true;
            }
            else
            {
                isinLibraries = false;
            }

            if (ne.ParsingName == KnownFolders.Libraries.ParsingName)
            {
                itisLibraries = true;
            }
            else
            {
                itisLibraries = false;
            }
        }

                //if (MessageBox.Show("An error occurred while loading a folder. Please report this issue at http://bugtracker.better-explorer.com/. \r\n\r\nHere is some information about the folder being loaded:\r\n\r\nName: " + ne.GetDisplayName(DisplayNameType.Default) + "\r\nLocation: " + ne.ParsingName +
                //    "\r\n\r\nFolder, Drive, or Library: " + GetYesNoFromBoolean(ne.IsFolder) + "\r\nDrive: " + GetYesNoFromBoolean(ne.IsDrive) + "\r\nNetwork Drive: " + GetYesNoFromBoolean(ne.IsNetDrive) + "\r\nRemovable: " + GetYesNoFromBoolean(ne.IsRemovable) +
                //    "\r\nSearch Folder: " + GetYesNoFromBoolean(ne.IsSearchFolder) + "\r\nShared: " + GetYesNoFromBoolean(ne.IsShared) + "\r\nShortcut: " + GetYesNoFromBoolean(ne.IsLink) + "\r\nLibrary: " + GetYesNoFromBoolean(isinLibraries) + "\r\nLibraries Folder: " + GetYesNoFromBoolean(itisLibraries) +
                //    "\r\n\r\n Would you like to see additional information? Click No to try continuing the program.", "Error Occurred on Completing Navigation", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                //{
                    // MessageBox.Show("An error occurred while loading a folder. Please report this issue at http://bugtracker.better-explorer.com/. \r\n\r\nHere is additional information about the error: \r\n\r\n" + exe.Message + "\r\n\r\n" + exe.ToString(), "Additional Error Data", MessageBoxButton.OK, MessageBoxImage.Error);
                //}
            }

        }

        public static string GetYesNoFromBoolean(bool value)
        {
      return value ? "Yes" : "No";
        }

        void miItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            ShellObject SaveLoc = mi.Tag as ShellObject;

            if (Explorer.NavigationLog.CurrentLocation.ParsingName.Contains(KnownFolders.Libraries.ParsingName) &&
                Explorer.NavigationLog.CurrentLocation.ParsingName.EndsWith("library-ms"))
            {
                ShellLibrary lib =
                    ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
                lib.DefaultSaveFolder = SaveLoc.ParsingName;
                lib.Close();

            }
            else if (Explorer.SelectedItems[0].ParsingName.Contains(KnownFolders.Libraries.ParsingName))
            {
                ShellLibrary lib =
                    ShellLibrary.Load(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default), false);
                lib.DefaultSaveFolder = SaveLoc.ParsingName;
                lib.Close();

            }
        }

        void Explorer_ExplorerGotFocus(object sender, EventArgs e)
        {
            if (breadcrumbBarControl1.IsInEditMode)
            {
                breadcrumbBarControl1.ExitEditMode();
            }
      IsAfterRename = false;
      Explorer.IsRenameStarted = false;
            
        }

        void Explorer_LostFocus(object sender, EventArgs e)
        {
            if (!backstage.IsOpen)
                Explorer.SetExplorerFocus();
            IsAfterRename = false;
        }

        void Explorer_KeyUP(object sender, ExplorerKeyUPEventArgs e)
        {

            if (e.Key == 27 || e.Key == 13 || e.Key == 17 || e.Key == 777 || e.Key == 778)
            {
                IsAfterRename = true;
                IsAfterFolderCreate = false;
            } 
        }

        void Explorer_RenameFinished(object sender, EventArgs e)
        {
            IsAfterRename = true;
      IsAfterFolderCreate = false;
      Explorer.IsRenameStarted = false;
            breadcrumbBarControl1.ExitEditMode();
        }

        void Explorer_GotFocus(object sender, EventArgs e)
        {
            
        }

        private bool IsLibW = false;
        void Explorer_ItemsChanged(object sender, EventArgs e)
        {
            int ItemsCount = Explorer.GetItemsCount();
            sbiItemsCount.Content = ItemsCount == 1 ? ItemsCount.ToString() + " item" : ItemsCount.ToString() +
                                            " items";


            if (IsAfterFolderCreate)
            {
                Explorer.DoRename(LastPath.Replace(@"\\", @"\"), IsLibW);
            }


        }

        void Explorer_ExplorerBrowserMouseLeave(object sender, EventArgs e)
        {
            //if (Itmpop != null)
            //{
            //    Itmpop.Visibility = System.Windows.Visibility.Hidden;
            //}
        }

        public static bool IsFocusedItemHost()
        {
            UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;

            return elementWithFocus.IsKeyboardFocused;
        }

        bool IsCancel = false;
        void Explorer_NavigationPending(object sender, NavigationPendingEventArgs e)
        {
            e.Cancel = Explorer.IsCancelNavigation;
        }

        void Explorer_ViewChanged(object sender, ViewChangedEventArgs e)
        {
      IsViewSelection = false;

      if (e.ThumbnailSize == 256)
      {
          ViewGallery.SelectedIndex = 0;

      }
      if (e.ThumbnailSize == 96)
      {
          ViewGallery.SelectedIndex = 1;

      }
      if (e.View == ExplorerBrowserViewMode.Icon && e.ThumbnailSize == 48)
      {
          ViewGallery.SelectedIndex = 2;
          btnSbIcons.IsChecked = true;
      }
      if (e.View == ExplorerBrowserViewMode.SmallIcon)
      {
          ViewGallery.SelectedIndex = 3;
                                     
      }
      else
      {
          btnSbIcons.IsChecked = false;
      }
      if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.List)
      {
          ViewGallery.SelectedIndex = 4;
      }
      if (e.View == ExplorerBrowserViewMode.Details)
      {
          ViewGallery.SelectedIndex = 5;
          btnSbDetails.IsChecked = true;
      }
      else
      {
          btnSbDetails.IsChecked = false;
      }
      if (e.View == ExplorerBrowserViewMode.Tile)
      {
          ViewGallery.SelectedIndex = 6;
          btnSbTiles.IsChecked = true;
      }
      else
      {
          btnSbTiles.IsChecked = false;
      }
      if (e.View == ExplorerBrowserViewMode.Content)
      {
          ViewGallery.SelectedIndex = 7;
      }
      IsCalledFromViewEnum = true;
      zoomSlider.Value = e.ThumbnailSize;
      IsCalledFromViewEnum = false;

      btnAutosizeColls.IsEnabled = e.View == ExplorerBrowserViewMode.Details ? true : false;

      IsViewSelection = true;
        }

        void fsw_Renamed(object sender, RenamedEventArgs e)
        {

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                         (Action)(() =>
                         {
                             foreach (MenuItem item in btnFavorites.Items)
                             {
                                 if ((item.Tag as ShellObject).ParsingName == e.OldFullPath)
                                 {
                                     item.Header = Path.GetFileNameWithoutExtension(e.Name);
                                 }
                             }
                         }));

        }

        void fsw_Deleted(object sender, FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                             (Action)(() =>
                             {
                                 MenuItem ItemForRemove = null;
                                 foreach (MenuItem item in btnFavorites.Items)
                                 {

                                     if (item.Header.ToString() == Path.GetFileNameWithoutExtension(e.Name))
                                     {
                                         ItemForRemove = item;

                                     }

                                 }
                                 btnFavorites.Items.Remove(ItemForRemove);
                             }));
        }

        void fsw_Created(object sender, FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                             (Action)(() =>
                             {
                                 if (Path.GetExtension(e.FullPath).ToLowerInvariant() == ".lnk")
                                 {
                                     ShellObject so = ShellObject.FromParsingName(e.FullPath);
                                     MenuItem mi = new MenuItem();
                                     mi.Header = so.GetDisplayName(DisplayNameType.Default);
                                     mi.Tag = so;
                                     so.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                                     so.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                                     ImageSource icon = so.Thumbnail.BitmapSource;
                                     mi.Icon = icon;
                                     mi.Click += new RoutedEventHandler(mif_Click);
                                     btnFavorites.Items.Add(mi);
                                 }
                             }));
            WindowsAPI.SetFocus(Explorer.SysListViewHandle);
        }


        void ExplorerBrowserControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            //TheStatusBar.InvalidateVisual();
            //TheStatusBar.UpdateLayout();
        }

        void ExplorerBrowserControl_ClientSizeChanged(object sender, EventArgs e)
        {
            //TheStatusBar.InvalidateVisual();
            //TheStatusBar.UpdateLayout();
        }

        bool IsSelectionRized = false;
        //BackgroundWorker bwSelectionChanged = new BackgroundWorker();
        //'Selection change (when an item is selected in a folder)

        async void ExplorerBrowserControl_SelectionChanged(object sender, EventArgs e)
        {
      int explorerSelectedItemsCount = 0;

      await Task.Run(() =>
      {
        //ct.ThrowIfCancellationRequested();
        Dispatcher.BeginInvoke(DispatcherPriority.Input, (ThreadStart)(() =>
        {

          if (!IsSelectionRized)
          {
            try
            {
              IsSelectionRized = true;
              // set up buttons
              SetupUIOnSelectOrNavigate(Explorer.GetSelectedItemsCount());
            }
            catch (Exception)
            {


            }
            IsSelectionRized = false;
          }
        }));
      });

      #region StatusBar selected items counter
      await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
        {
          explorerSelectedItemsCount = Explorer.GetSelectedItemsCount();
        }));

      SetUpStatusBarOnSelectOrNavigate(explorerSelectedItemsCount);
      #endregion

      if (!IsAfterFolderCreate && !backstage.IsOpen && IsAfterRename && !Explorer.IsRenameStarted)
          Explorer.SetExplorerFocus();
        }

    private Boolean SetupEditButton(string item)
    {
      bool isEditAvailable = false;
                                
        #region RegistryBasedCode
        RegistryKey rg = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\" + Path.GetExtension(item) + @"\OpenWithProgids");
        if (rg != null)
        {

          string filetype = rg.GetValueNames()[0];
          rg.Close();
          RegistryKey rgtype = Registry.ClassesRoot.OpenSubKey(filetype + @"\shell\edit\command");
          if (rgtype != null)
          {
            string editcommand = (string)rgtype.GetValue("");

            isEditAvailable = true;
            EditComm = editcommand.Replace("\"", "");
            rgtype.Close();
          }
          else
          {
            //RegistryKey rgtypeopen = Registry.ClassesRoot.OpenSubKey(filetype + @"\shell\open\command");
            //if (rgtypeopen != null)
            //{
            //  string editcommand = (string)rgtypeopen.GetValue("");

            //  isEditAvailable = true;
            //  EditComm = editcommand.Replace("\"", "");
            //  rgtypeopen.Close();
            //}
            //else
            //{
            //  isEditAvailable = false;
            //}
            isEditAvailable = false;
          }
        }
        else
        {
          isEditAvailable = false;
        }
        #endregion
      //try
      //{
      //  Shell32.Shell shell = new Shell();
      //  Shell32.Folder folder = null;
      //  folder = shell.NameSpace(Path.GetDirectoryName(item));

      //  var itemInternal = folder.Items().OfType<Shell32.FolderItem>().ToArray()[0];
      //  if (itemInternal != null)
      //  {
      //    isEditAvailable = itemInternal.Verbs().OfType<FolderItemVerb>().Select(s => s.Name).Contains("&Edit");
      //  }
      //  item = null;
      //  folder = null;
      //  shell = null;
      //}
      //catch (Exception)
      //{
      //  isEditAvailable = false;
      //}
                                
      return isEditAvailable;
    }
    private void SetUpOpenWithButton(ShellObject SelectedItem)
    {
      btnOpenWith.Items.Clear();
      string defapp = "";
        List<string> recommendedPrograms = new List<string>();
          string extension =
          System.IO.Path.GetExtension(SelectedItem.ParsingName);
          recommendedPrograms = Explorer.RecommendedPrograms(extension);

          MenuItem mid = new MenuItem();
          defapp = WindowsAPI.GetAssoc(extension, WindowsAPI.AssocF.Verify,
                  WindowsAPI.AssocStr.Executable);
          if (File.Exists(defapp) && defapp.ToLowerInvariant() != Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll").ToLowerInvariant())
          {
            if (defapp != "" && defapp != "\"%1\"" && extension != "")
            {

              string DefAppName = WindowsAPI.GetAssoc(extension, WindowsAPI.AssocF.Verify,
                                          WindowsAPI.AssocStr.FriendlyAppName);
              try
              {
                ShellObject objd = ShellObject.FromParsingName(defapp);
                mid.Header = DefAppName;
                mid.Tag = defapp;
                mid.Click += new RoutedEventHandler(miow_Click);
                objd.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                objd.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                mid.Icon = objd.Thumbnail.BitmapSource;
                mid.Focusable = false;
                objd.Dispose();
                btnOpenWith.Items.Add(mid);
              }
              catch (Exception)
              {

              }
            }

          }
          if (recommendedPrograms.Count > 0)
          {
            foreach (string item in recommendedPrograms)
            {
              if (item != "CompressedFolder")
              {
                MenuItem mi = new MenuItem();
                string deffappname;
                String ExePath = "";
                ExePath = WindowsAPI.GetAssoc(item, WindowsAPI.AssocF.Verify |
                    WindowsAPI.AssocF.Open_ByExeName, WindowsAPI.AssocStr.Executable);
                deffappname = WindowsAPI.GetAssoc(item, WindowsAPI.AssocF.Verify |
                    WindowsAPI.AssocF.Open_ByExeName, WindowsAPI.AssocStr.FriendlyAppName);
                if (!File.Exists(ExePath))
                {
                  ExePath = WindowsAPI.GetAssoc(item, WindowsAPI.AssocF.Verify,
                      WindowsAPI.AssocStr.Executable);
                  deffappname = WindowsAPI.GetAssoc(item, WindowsAPI.AssocF.Verify, WindowsAPI.AssocStr.FriendlyAppName);
                }

                bool isDuplicate = false;

                foreach (MenuItem mei in btnOpenWith.Items)
                {
                  if ((mei.Tag as string) == ExePath)
                  {
                    isDuplicate = true;
                    //MessageBox.Show(ExePath,"Duplicate Found");
                  }
                }

                if (isDuplicate == false)
                {
                  try
                  {
                    ShellObject obj = ShellObject.FromParsingName(ExePath);
                    mi.Header = deffappname;
                    mi.Tag = ExePath;
                    mi.Click += new RoutedEventHandler(miow_Click);
                    obj.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                    obj.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                    mi.Icon = obj.Thumbnail.BitmapSource;
                    mi.ToolTip = ExePath;
                    mi.Focusable = false;
                    obj.Dispose();
                  }
                  catch (Exception)
                  {

                  }
                
                  if (!String.IsNullOrEmpty(defapp))
                    btnOpenWith.Items.Add(mi);
                              
                }
              }
            }
          }
          btnOpenWith.IsEnabled = btnOpenWith.HasItems;
    }
    private Visibility BooleanToVisibiliy(bool value)
    {
      return value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }
    
    private void SetUPRibbonTabsVisibilityOnSelectOrNavigate(int selectedItemsCount, ShellObject selectedItem)
    {
      #region Archive Contextual Tab
      ctgArchive.Visibility = BooleanToVisibiliy(selectedItemsCount == 1 && Archives.Contains(Path.GetExtension(selectedItem.ParsingName).ToLowerInvariant()));
      if (asArchive == true && ctgArchive.Visibility == System.Windows.Visibility.Visible)
      {
        TheRibbon.SelectedTabItem = ctgArchive.Items[0];
      }  
      #endregion

      #region Drive Contextual Tab
      ctgDrive.Visibility = BooleanToVisibiliy((selectedItemsCount == 1 && ((selectedItem != null && selectedItem.IsDrive) || (selectedItem != null && selectedItem.Parent != null && selectedItem.Parent.IsDrive))) || ((Explorer.NavigationLog.CurrentLocation.IsDrive)));
      if (asDrive == true && ctgDrive.Visibility == System.Windows.Visibility.Visible && (selectedItem != null && selectedItem.IsDrive))
      {
        TheRibbon.SelectedTabItem = ctgDrive.Items[0];
      }  
      #endregion

      #region Application Context Tab
      ctgExe.Visibility = BooleanToVisibiliy(selectedItemsCount == 1 && (Path.GetExtension(selectedItem.ParsingName).ToLowerInvariant() == ".exe" || Path.GetExtension(selectedItem.ParsingName).ToLowerInvariant() == ".msi") && selectedItem.IsFolder == false);
      if (asApplication == true && ctgExe.Visibility == System.Windows.Visibility.Visible)
      {
        TheRibbon.SelectedTabItem = ctgExe.Items[0];
      }  
      #endregion

      #region Folder Tools Context Tab
      ctgFolderTools.Visibility = BooleanToVisibiliy((selectedItemsCount == 1 && ((selectedItem.IsFolder && selectedItem.IsFileSystemObject && !selectedItem.IsDrive && !selectedItem.IsNetDrive))) || (Explorer.NavigationLog.CurrentLocation.IsFolder && Explorer.NavigationLog.CurrentLocation.IsFileSystemObject && !Explorer.NavigationLog.CurrentLocation.IsDrive && !Explorer.NavigationLog.CurrentLocation.IsNetDrive));
      if (asFolder == true && ctgFolderTools.Visibility == System.Windows.Visibility.Visible)
      {
        TheRibbon.SelectedTabItem = ctgFolderTools.Items[0];
      } 
      #endregion

      #region Image Context Tab
      ctgImage.Visibility = BooleanToVisibiliy(selectedItemsCount == 1 && Images.Contains(Path.GetExtension(selectedItem.ParsingName).ToLowerInvariant()) && selectedItem.IsFolder == false);
      if (ctgImage.Visibility == System.Windows.Visibility.Visible)
      {
        Bitmap cvt = new Bitmap(selectedItem.ParsingName);

        imgSizeDisplay.WidthData = cvt.Width.ToString();
        imgSizeDisplay.HeightData = cvt.Height.ToString();

        if (asImage == true)
        {
          TheRibbon.SelectedTabItem = ctgImage.Items[0];
        }
        cvt.Dispose();
      } 
      #endregion

      #region Library Context Tab
      ctgLibraries.Visibility = BooleanToVisibiliy(selectedItemsCount == 1 && (Explorer.NavigationLog.CurrentLocation.Equals(KnownFolders.Libraries) || (selectedItem.Parent != null && selectedItem.Parent.Equals(KnownFolders.Libraries))));
      if (asLibrary == true && ctgLibraries.Visibility == Visibility.Visible)
      {
        TheRibbon.SelectedTabItem = ctgLibraries.Items[0];
      } 
      #endregion

      #region Virtual Disk Context Tab
      ctgVirtualDisk.Visibility = BooleanToVisibiliy(selectedItemsCount == 1 && (Path.GetExtension(selectedItem.ParsingName).ToLowerInvariant() == ".iso") && selectedItem.IsFolder == false);
      if (asVirtualDrive == true && ctgVirtualDisk.Visibility == System.Windows.Visibility.Visible)
      {
          TheRibbon.SelectedTabItem = ctgVirtualDisk.Items[0];
      }
      #endregion

      #region Search Contextual Tab
      ctgSearch.Visibility = BooleanToVisibiliy(Explorer.NavigationLog.CurrentLocation.IsSearchFolder);
      if (ctgSearch.Visibility == System.Windows.Visibility.Visible && !Explorer.NavigationLog.CurrentLocation.IsSearchFolder)
      {
        ctgSearch.Visibility = System.Windows.Visibility.Collapsed;
        TheRibbon.SelectedTabItem = HomeTab;
      } 
      #endregion

    }

    private void SetUpStatusBarOnSelectOrNavigate(int selectedItemsCount)
    {
      spSelItems.Visibility = BooleanToVisibiliy(selectedItemsCount > 0);
      sbiSelItemsCount.Visibility = BooleanToVisibiliy(selectedItemsCount > 0);
      if (selectedItemsCount == 1)
        sbiSelItemsCount.Content = "1 item selected";
      else if (selectedItemsCount > 1)
        sbiSelItemsCount.Content = selectedItemsCount.ToString() + " items selected";
    }
    private async void SetUpButtonsStateOnSelectOrNavigate(int selectedItemsCount, ShellObject selectedItem)
    {
      btnCopy.IsEnabled = selectedItemsCount > 0;
      btnCopyto.IsEnabled = selectedItemsCount > 0;
      btnMoveto.IsEnabled = selectedItemsCount > 0;
      btnCut.IsEnabled = selectedItemsCount > 0;
      btnDelete.IsEnabled = selectedItem != null && selectedItem.IsFileSystemObject;
      btnRename.IsEnabled = selectedItem != null && (selectedItem.IsFileSystemObject || (selectedItem.Parent != null && selectedItem.Parent.Equals(KnownFolders.Libraries)));
      btnProperties3.IsEnabled = selectedItemsCount > 0;
      if (selectedItem != null)
      {
        btnEdit.IsEnabled = SetupEditButton(selectedItem.ParsingName);  
        //await Task.Run(() =>
        //    {

        //      return SetupEditButton(selectedItem.ParsingName);
        //    });
      }
      btnSelAll.IsEnabled = selectedItemsCount != Explorer.GetItemsCount();
      btnSelNone.IsEnabled = selectedItemsCount > 0;
      btnShare.IsEnabled = selectedItemsCount == 1 && selectedItem.IsFolder;
      btnAdvancedSecurity.IsEnabled = selectedItemsCount == 1;
      btnHideSelItems.IsEnabled = Explorer.NavigationLog.CurrentLocation.IsFileSystemObject;
    }
    private void SetupUIOnSelectOrNavigate(int SelItemsCount, bool isNavigate = false)
    {
      btnDefSave.Items.Clear();
      var selectedItem = Explorer.SelectedItems.FirstOrDefault();
      if (selectedItem != null)
      {
        if (!isNavigate)
          SetUpOpenWithButton(selectedItem);
      }

      //Set up ribbon contextual tabs on selection changed
      SetUPRibbonTabsVisibilityOnSelectOrNavigate(SelItemsCount, selectedItem);

      SetUpButtonsStateOnSelectOrNavigate(SelItemsCount, selectedItem);

      //string defapp = "";
      //bool isFuncAvail;
      //bool isEditAvailable;
      //string ext;
      //if (SelItemsCount == 0)
      //{
      //  // WHAT TO DO IF NO ITEMS ARE SELECTED
      //  //MessageBox.Show("No Items Selected");

      //  // hide status bar items
      //  sbiSelItemsCount.Visibility = System.Windows.Visibility.Collapsed;
      //  spSelItems.Visibility = System.Windows.Visibility.Collapsed;

      //  // disable buttons
      //  btnShare.IsEnabled = false;
      //  btnCopy.IsEnabled = false;
      //  btnCut.IsEnabled = false;
      //  btnRename.IsEnabled = false;
      //  btnDelete.IsEnabled = false;
      //  btnCopyto.IsEnabled = false;
      //  btnMoveto.IsEnabled = false;
      //  btnSelNone.IsEnabled = false;
      //  btnOpenWith.IsEnabled = false;
      //  btnEdit.IsEnabled = false;
      //  btnHistory.IsEnabled = false;
      //  btnAdvancedSecurity.IsEnabled = false;
      //  miRestoreRBItems.Visibility = System.Windows.Visibility.Collapsed;
      //  mnuIncludeInLibrary.IsEnabled = false;
      //  btnOpenTray.IsEnabled = false;
      //  btnCloseTray.IsEnabled = false;
      //  btnEjectDevice.IsEnabled = false;
      //  btnUnmountDrive.IsEnabled = false;

      //  // this still has functionality when there are no items selected
      //  btnEasyAccess.IsEnabled = true;

      //  // hide contextual tabs
      //  ctgArchive.Visibility = System.Windows.Visibility.Collapsed;
      //  ctgVirtualDisk.Visibility = System.Windows.Visibility.Collapsed;
      //  ctgExe.Visibility = System.Windows.Visibility.Collapsed;
      //  ctgImage.Visibility = System.Windows.Visibility.Collapsed;

      //  // already hidden (not)
      //  ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;

      //  // if the current viewing location is a Drive, show Drive Tools.
      //  if (inDrive == true)
      //  {
      //    ctgDrive.Visibility = System.Windows.Visibility.Visible;
      //  }
      //  else
      //  {
      //    ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
      //  }

      //  miSelAllByType.IsEnabled = false;
      //  miSelAllByDate.IsEnabled = false;

      //  // if the current viewing location is a Library, show Library Tools.
      //  //if (Explorer.NavigationLog.CurrentLocation.Parent != null)
      //  //{
      //  //    if (Explorer.NavigationLog.CurrentLocation.Parent.ParsingName == KnownFolders.Libraries.ParsingName)
      //  //    {
      //  //        ctgFolderTools.Visibility = Visibility.Collapsed;
      //  //    }
      //  //}

      //  if (Explorer.NavigationLog.CurrentLocation.ParsingName == KnownFolders.Libraries.ParsingName || Explorer.NavigationLog.CurrentLocation.IsDrive)
      //  {
      //    ctgFolderTools.Visibility = Visibility.Collapsed;
      //  }

      //  if (inLibrary == true)
      //  {
      //    ctgFolderTools.Visibility = Visibility.Collapsed;
      //    ctgLibraries.Visibility = System.Windows.Visibility.Visible;
      //    ShellLibrary lib = ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
      //    IsFromSelectionOrNavigation = true;
      //    chkPinNav.IsChecked = lib.IsPinnedToNavigationPane;
      //    IsFromSelectionOrNavigation = false;
      //    foreach (ShellObject item in lib)
      //    {
      //      MenuItem miItem = new MenuItem();
      //      miItem.Header = item.GetDisplayName(DisplayNameType.Default);
      //      miItem.Tag = item;
      //      item.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
      //      item.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
      //      miItem.Icon = item.Thumbnail.BitmapSource;
      //      miItem.GroupName = "GRDS1";
      //      miItem.Click += new RoutedEventHandler(miItem_Click);
      //      miItem.IsCheckable = true;
      //      miItem.IsChecked = (item.ParsingName == lib.DefaultSaveFolder);
      //      btnDefSave.Items.Add(miItem);
      //    }

      //    btnDefSave.IsEnabled = !(lib.Count == 0);
      //    lib.Close();
      //  }
      //  else
      //  {
      //    if (!Explorer.NavigationLog.CurrentLocation.ParsingName.ToLowerInvariant().EndsWith("library-ms"))
      //    {
      //      ctgLibraries.Visibility = System.Windows.Visibility.Collapsed;
      //    }
      //  }

      //}
      //else
      //{
      //  // WHAT TO DO IF ITEMS ARE SELECTED

      //  // show status bar items
      //  sbiSelItemsCount.Visibility = System.Windows.Visibility.Visible;
      //  spSelItems.Visibility = System.Windows.Visibility.Visible;

      //  // enable (most) buttons
      //  btnCopy.IsEnabled = true;
      //  btnCut.IsEnabled = true;
      //  btnRename.IsEnabled = Explorer.SelectedItems.Count() == 1;
      //  btnCopyto.IsEnabled = true;
      //  btnMoveto.IsEnabled = true;
      //  btnSelNone.IsEnabled = true;
      //  btnAdvancedSecurity.IsEnabled = false;
      //  btnDefSave.Items.Clear();

      //  miSelAllByType.IsEnabled = true;
      //  miSelAllByDate.IsEnabled = true;

      //  // Disable in case the selection is not a folder, or there's more than one selected item
      //  mnuIncludeInLibrary.IsEnabled = false;

      //  if (SelItemsCount == 1)
      //  {
      //    // IF ONE ITEM IS SELECTED
      //    ShellObject SelectedItem = Explorer.SelectedItems[0];
      //    //MessageBox.Show("One Item Selected \r\n" + SelectedItem.ParsingName);
      //    if (Explorer.NavigationLog.CurrentLocation.ParsingName == KnownFolders.RecycleBin.ParsingName)
      //    {
      //      miRestoreRBItems.Visibility = System.Windows.Visibility.Visible;
      //    }
      //    // set up status bar
      //    sbiSelItemsCount.Content = "1 item selected";

      //    // set variables

      //    btnEasyAccess.IsEnabled = true;

      //    btnShare.IsEnabled = SelectedItem.IsFolder && SelectedItem.IsFileSystemObject;
      //    btnAdvancedSecurity.IsEnabled = SelectedItem.IsFileSystemObject;
      //    isFuncAvail = (SelectedItem.IsFileSystemObject &&
      //        (Explorer.NavigationLog.CurrentLocation.ParsingName != KnownFolders.Computer.ParsingName));

      //    isEditAvailable = SetupEditButton(SelectedItem);

      //    // set up Open With button
      //    SetUpOpenWithButton(defapp, isFuncAvail, SelectedItem);

      //    // enable buttons
      //    btnDelete.IsEnabled = (isFuncAvail || Explorer.NavigationLog.CurrentLocation.ParsingName ==
      //        KnownFolders.Libraries.ParsingName);
      //    btnOpenWith.IsEnabled = (isFuncAvail &&
      //        System.IO.Path.GetExtension(SelectedItem.ParsingName) != ""
      //        && (btnOpenWith.Items.Count > 0));

      //    btnEdit.IsEnabled = isFuncAvail && isEditAvailable;
      //    btnHistory.IsEnabled = true;

      //    //set up contextual tabs

      //    bool selisfolder = false;
      //    bool selislib = false;

      //    // Folder/Disk Tools
      //    if (SelectedItem.IsFolder && SelectedItem.IsFileSystemObject)
      //    {
      //      // Check for if Disk
      //      if (SelectedItem.IsDrive)
      //      {
      //        // Is Drive
      //        ctgDrive.Visibility = System.Windows.Visibility.Visible;
      //        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;

      //        if (SelectedItem.GetDriveInfo().DriveType == DriveType.CDRom)
      //        {
      //          btnOpenTray.IsEnabled = true;
      //          btnCloseTray.IsEnabled = true;
      //          btnEjectDevice.IsEnabled = false;
      //        }
      //        else if (SelectedItem.GetDriveInfo().DriveType == DriveType.Removable)
      //        {
      //          btnOpenTray.IsEnabled = false;
      //          btnCloseTray.IsEnabled = false;
      //          btnEjectDevice.IsEnabled = true;
      //        }
      //        else
      //        {
      //          btnOpenTray.IsEnabled = false;
      //          btnCloseTray.IsEnabled = false;
      //          btnEjectDevice.IsEnabled = false;
      //        }

      //        try
      //        {
      //          if (GetLettersOfVirtualDrives(false).Contains(GetDriveLetterFromDrivePath(SelectedItem.ParsingName)) == true)
      //          {
      //            btnUnmountDrive.IsEnabled = true;
      //            SelectedDriveID = GetDeviceNumberForDriveLetter(GetDriveLetterFromDrivePath(SelectedItem.ParsingName));
      //          }
      //          else
      //          {
      //            btnUnmountDrive.IsEnabled = false;
      //          }
      //        }
      //        catch (System.DllNotFoundException)
      //        {
      //          btnUnmountDrive.IsEnabled = false;
      //        }

      //      }
      //      else if (!SelectedItem.IsNetDrive)
      //      {
      //        // Is Folder
      //        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
      //        btnOpenTray.IsEnabled = false;
      //        btnCloseTray.IsEnabled = false;
      //        btnEjectDevice.IsEnabled = false;
      //        btnUnmountDrive.IsEnabled = false;
      //        //MessageBox.Show("1");
      //        ctgFolderTools.Visibility = System.Windows.Visibility.Visible;
      //        mnuIncludeInLibrary.IsEnabled = true;
      //        selisfolder = true;
      //      }
      //      else
      //      {
      //        // Is Network Drive
      //        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
      //        btnOpenTray.IsEnabled = false;
      //        btnCloseTray.IsEnabled = false;
      //        btnEjectDevice.IsEnabled = false;
      //        btnUnmountDrive.IsEnabled = false;
      //        if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
      //            !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
      //          ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //      }
      //    }
      //    else
      //    {
      //      // Is not a directory
      //      ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
      //      btnOpenTray.IsEnabled = false;
      //      btnCloseTray.IsEnabled = false;
      //      btnEjectDevice.IsEnabled = false;
      //      btnUnmountDrive.IsEnabled = false;
      //      if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
      //          !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
      //        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //    }

      //    // Library Tools
      //    if (SelectedItem.ParsingName.Contains(KnownFolders.Libraries.ParsingName))
      //    {
      //      ctgLibraries.Visibility = System.Windows.Visibility.Visible;
      //      selislib = true;
      //      ShellLibrary lib =
      //      ShellLibrary.Load(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default), false);
      //      IsFromSelectionOrNavigation = true;
      //      chkPinNav.IsChecked = lib.IsPinnedToNavigationPane;
      //      IsFromSelectionOrNavigation = false;
      //      foreach (ShellObject item in lib)
      //      {
      //        MenuItem miItem = new MenuItem();
      //        miItem.Header = item.GetDisplayName(DisplayNameType.Default);
      //        miItem.Tag = item;
      //        item.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
      //        item.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
      //        miItem.Icon = item.Thumbnail.BitmapSource;
      //        miItem.IsCheckable = true;
      //        miItem.Click += new RoutedEventHandler(miItem_Click);
      //        miItem.IsChecked = (item.ParsingName == lib.DefaultSaveFolder);
      //        btnDefSave.Items.Add(miItem);
      //      }

      //      btnDefSave.IsEnabled = !(lib.Count == 0);
      //      lib.Close();

      //    }
      //    else
      //    {
      //      if (!Explorer.NavigationLog.CurrentLocation.ParsingName.ToLowerInvariant().EndsWith("library-ms"))
      //      {
      //        btnDefSave.Items.Clear();
      //        ctgLibraries.Visibility = System.Windows.Visibility.Collapsed;
      //      }

      //    }

      //    //Application Tools
      //    if ((System.IO.Path.GetExtension(SelectedItem.ParsingName).ToLowerInvariant() == ".exe" ||
      //        System.IO.Path.GetExtension(SelectedItem.ParsingName).ToLowerInvariant() == ".msi") &&
      //        !(SelectedItem.IsFolder))
      //    {
      //      ctgExe.Visibility = System.Windows.Visibility.Visible;
      //      btnPin.IsChecked = WindowsAPI.IsPinnedToTaskbar(Explorer.SelectedItems[0].ParsingName);
      //      if (asApplication == true)
      //      {
      //        TheRibbon.SelectedTabItem = ctgExe.Items[0];
      //      }
      //    }
      //    else
      //    {
      //      ctgExe.Visibility = System.Windows.Visibility.Collapsed;
      //    }

      //    // Archive Tools
      //    if (Archives.Contains(System.IO.Path.GetExtension(SelectedItem.ParsingName).ToLowerInvariant()))
      //    {
      //      ctgArchive.Visibility = System.Windows.Visibility.Visible;
      //      if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
      //          !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
      //        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //      ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //      selisfolder = false;
      //      var sectedFileInfo = new FileInfo(SelectedItem.ParsingName);
      //      txtExtractLocation.Text = sectedFileInfo.DirectoryName;
      //      sectedFileInfo = null;
      //      SelectedArchive = SelectedItem.ParsingName;
      //      if (System.IO.Path.GetExtension(SelectedItem.ParsingName).ToLowerInvariant().EndsWith(".zip"))
      //      {
      //        //btnViewArchive.IsEnabled = true;
      //      }
      //      else
      //      {
      //        //btnViewArchive.IsEnabled = false;
      //      }
      //      if (asArchive == true)
      //      {
      //        TheRibbon.SelectedTabItem = ctgArchive.Items[0];
      //      }
      //    }
      //    else
      //    {
      //      ctgArchive.Visibility = System.Windows.Visibility.Collapsed;
      //    }

      //    // Virtual Disk Tools
      //    if (VirDisks.Contains(System.IO.Path.GetExtension(SelectedItem.ParsingName).ToLowerInvariant()))
      //    {
      //      ctgVirtualDisk.Visibility = System.Windows.Visibility.Visible;
      //    }
      //    else
      //    {
      //      ctgVirtualDisk.Visibility = System.Windows.Visibility.Collapsed;
      //    }

      //    // Image Tools
      //    System.Drawing.Bitmap cvt;
      //    if (//SelItemsCount > 0 &&
      //        Images.Contains(System.IO.Path.GetExtension(SelectedItem.ParsingName).ToLowerInvariant()))
      //    {
      //      cvt = new Bitmap(SelectedItem.ParsingName);
      //      //imgdHeight.Text = cvt.Height.ToString();
      //      //imgdWidth.Text = cvt.Width.ToString();
      //      imgSizeDisplay.WidthData = cvt.Width.ToString();
      //      imgSizeDisplay.HeightData = cvt.Height.ToString();
      //      ctgImage.Visibility = System.Windows.Visibility.Visible;
      //      if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
      //          !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
      //        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //      if (asImage == true)
      //      {
      //        TheRibbon.SelectedTabItem = ctgImage.Items[0];
      //      }
      //      cvt.Dispose();
      //    }
      //    else
      //    {
      //      ctgImage.Visibility = System.Windows.Visibility.Collapsed;
      //    }
      //    //LastItemSelected = SelectedItem.ParsingName;

      //    // Folder/Disk Tools
      //    if (Explorer.NavigationLog.CurrentLocation.IsFolder && Explorer.NavigationLog.CurrentLocation.IsFileSystemObject)
      //    {
      //      // Check for if Disk
      //      if (Explorer.NavigationLog.CurrentLocation.IsDrive)
      //      {
      //        ctgDrive.Visibility = System.Windows.Visibility.Visible;
      //        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //        if (asDrive == true)
      //        {
      //          //TheRibbon.SelectedTabItem = ctgDrive.Items[0];
      //        }
      //      }
      //      else if (!Explorer.NavigationLog.CurrentLocation.IsNetDrive)
      //      {
      //        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
      //        //ctgFolderTools.Visibility = System.Windows.Visibility.Visible;
      //      }
      //      else
      //      {
      //        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
      //        if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
      //            !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
      //          ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //      }
      //    }
      //    else
      //    {
      //      if (!(Explorer.SelectedItems[0].IsDrive && !Explorer.SelectedItems[0].IsNetDrive))
      //      {
      //        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
      //        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //      }

      //    }

      //    if (selisfolder == true && Explorer.SelectedItems[0].IsFolder)
      //    {
      //      //MessageBox.Show("2");
      //      ctgFolderTools.Visibility = System.Windows.Visibility.Visible;
      //      if (asFolder == true)
      //      {
      //        TheRibbon.SelectedTabItem = ctgFolderTools.Items[0];
      //      }
      //    }
      //    else
      //    {
      //      if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
      //          !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
      //        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //      ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //    }



      //    if (selislib == true)
      //    {
      //      ctgLibraries.Visibility = System.Windows.Visibility.Visible;
      //      if (asLibrary == true)
      //      {
      //        TheRibbon.SelectedTabItem = ctgLibraries.Items[0];
      //      }
      //    }

      //  }
      //  else
      //  {
      //    // IF MULTIPLE ITEMS ARE SELECTED
      //    //MessageBox.Show(SelItemsCount.ToString() + " items selected");

      //    // set variables
      //    isFuncAvail = true;
      //    isEditAvailable = false;

      //    // set up status bar
      //    sbiSelItemsCount.Content = SelItemsCount.ToString() + " items selected";

      //    // enable (or disable) buttons
      //    btnDelete.IsEnabled = true;
      //    btnShare.IsEnabled = false;
      //    btnOpenWith.IsEnabled = false;
      //    btnEdit.IsEnabled = false;
      //    btnHistory.IsEnabled = false;
      //    btnEasyAccess.IsEnabled = false;
      //    btnOpenTray.IsEnabled = false;
      //    btnCloseTray.IsEnabled = false;
      //    btnEjectDevice.IsEnabled = false;
      //    btnUnmountDrive.IsEnabled = false;

      //    // hide contextual tabs
      //    ctgImage.Visibility = System.Windows.Visibility.Collapsed;
      //    ctgExe.Visibility = System.Windows.Visibility.Collapsed;
      //    ctgArchive.Visibility = System.Windows.Visibility.Collapsed;
      //    ctgVirtualDisk.Visibility = System.Windows.Visibility.Collapsed;
      //    ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
      //    if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
      //            !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
      //      ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //    ctgLibraries.Visibility = System.Windows.Visibility.Collapsed;

      //    // Folder/Disk Tools
      //    if (Explorer.NavigationLog.CurrentLocation.IsFolder && Explorer.NavigationLog.CurrentLocation.IsFileSystemObject)
      //    {
      //      // Check for if Disk
      //      if (Explorer.NavigationLog.CurrentLocation.IsDrive)
      //      {
      //        ctgDrive.Visibility = System.Windows.Visibility.Visible;
      //        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //      }
      //      else if (!Explorer.NavigationLog.CurrentLocation.IsNetDrive)
      //      {
      //        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
      //        //ctgFolderTools.Visibility = System.Windows.Visibility.Visible;
      //      }
      //      else
      //      {
      //        ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
      //        if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
      //            !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
      //          ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //      }
      //    }
      //    else
      //    {
      //      ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
      //      if (!(Explorer.NavigationLog.CurrentLocation.IsFolder && !Explorer.NavigationLog.CurrentLocation.IsDrive &&
      //            !Explorer.NavigationLog.CurrentLocation.IsSearchFolder))
      //        ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //    }

      //    // if the current viewing location is a Library, show Library Tools.
      //    if (inLibrary == true)
      //    {
      //      ctgFolderTools.Visibility = System.Windows.Visibility.Collapsed;
      //      ctgLibraries.Visibility = System.Windows.Visibility.Visible;
      //      ShellLibrary lib =
      //                  ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
      //      IsFromSelectionOrNavigation = true;
      //      chkPinNav.IsChecked = lib.IsPinnedToNavigationPane;
      //      IsFromSelectionOrNavigation = false;
      //      foreach (ShellObject item in lib)
      //      {
      //        MenuItem miItem = new MenuItem();
      //        miItem.Header = item.GetDisplayName(DisplayNameType.Default);
      //        miItem.Tag = item.ParsingName;
      //        item.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
      //        item.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
      //        miItem.Icon = item.Thumbnail.BitmapSource;
      //        miItem.IsCheckable = true;
      //        miItem.IsChecked = (item.ParsingName == lib.DefaultSaveFolder);
      //        btnDefSave.Items.Add(miItem);
      //      }

      //      btnDefSave.IsEnabled = !(lib.Count == 0);
      //      lib.Close();
      //    }
      //    else
      //    {
      //      if (!Explorer.NavigationLog.CurrentLocation.ParsingName.ToLowerInvariant().EndsWith("library-ms"))
      //      {
      //        btnDefSave.Items.Clear();
      //        ctgLibraries.Visibility = System.Windows.Visibility.Collapsed;
      //      }

      //    }
      //  }

      //}
    }
        bool IsFromSelectionOrNavigation = false;
        
        // background worker code removed. hopefully we don't need it still... Lol.

        void cbm_ClipboardChanged(object sender, ClipboardChangedEventArgs e)
        {
            btnPaste.IsEnabled = e.DataObject.GetDataPresent(DataFormats.FileDrop);
            btnPasetShC.IsEnabled = e.DataObject.GetDataPresent(DataFormats.FileDrop);
        }

        private void TheRibbon_IsCollapsedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                this.edtSearchBox.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                this.edtSearchBox.Visibility = System.Windows.Visibility.Visible;
            }
        }

        #endregion

        #region Conditional Select

        private void miSelAllByType_Click(object sender, RoutedEventArgs e)
        {

            if (Explorer.SelectedItems.Count > 0)
            {
                List<string> flt = new List<string>();

                foreach (ShellObject item in Explorer.SelectedItems)
                {
                    flt.Add(item.Properties.System.ItemTypeText.Value.ToLowerInvariant());
                }

                ShellContainer CurrentLoc = (ShellContainer)Explorer.NavigationLog.CurrentLocation;

                foreach (ShellObject item in CurrentLoc)
                {
                    if (flt.Contains(item.Properties.System.ItemTypeText.Value.ToLowerInvariant()))
                    {
                        Explorer.SelectItem(item);
                    }
                }
            }
            else
            {
                return;
            }

        //    ShellObject FirstSelItem = Explorer.SelectedItems.Count > 0 ? Explorer.SelectedItems[0] : null;

        //    ShellContainer CurrentLoc = (ShellContainer)Explorer.NavigationLog.CurrentLocation;
        //    if (FirstSelItem != null)
        //    {
        //        //string pth = Path.GetExtension(FirstSelItem.ParsingName).ToLowerInvariant();
        //string pth = FirstSelItem.Properties.System.ItemTypeText.Value.ToLowerInvariant();
        //        foreach (ShellObject item in CurrentLoc)
        //        {
        //            //if (Path.GetExtension(item.ParsingName).ToLowerInvariant() == pth)
        //  if (item.Properties.System.ItemTypeText.Value.ToLowerInvariant() == pth)
        //            {
        //                Explorer.SelectItem(item);
        //            }

        //        }
        //    }

            btnCondSel.IsDropDownOpen = false;
            Explorer.Focus();

        }

        private void miSelAllByDate_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.SelectedItems.Count > 0)
            {
                List<DateTime> flt = new List<DateTime>();

                foreach (ShellObject item in Explorer.SelectedItems)
                {
                    if (item.Properties.System.DateCreated.Value.HasValue == true)
                    {
                        flt.Add(item.Properties.System.DateCreated.Value.Value);
                    }
                }

                ShellContainer CurrentLoc = (ShellContainer)Explorer.NavigationLog.CurrentLocation;
                foreach (ShellObject item in CurrentLoc)
                {
                    if (item.Properties.System.DateCreated.Value.HasValue == true)
                    {
                        if (flt.Contains(item.Properties.System.DateCreated.Value.Value))
                        {
                            Explorer.SelectItem(item);
                        }
                    }
                }
            }
            else
            {
                return;
            }

            //ShellObject FirstSelItem = Explorer.SelectedItems.Count > 0 ? Explorer.SelectedItems[0] : null;

            //if (FirstSelItem != null)
            //{
            //    DateTime cd = new FileInfo(FirstSelItem.ParsingName).CreationTimeUtc;

            //    ShellObjectCollection CurrentLoc = ((ShellContainer)Explorer.NavigationLog.CurrentLocation).ToShellObjectCollection();

            //    using (ShellObjectCollection selectable = FilterByCreateDate(CurrentLoc, cd, ConditionalSelectParameters.DateFilterTypes.Equals))
            //    {
            //        foreach (ShellObject item in selectable)
            //        {
            //            Explorer.SelectItem(item);
            //        }
            //    }
            //}

            btnCondSel.IsDropDownOpen = false;
            Explorer.Focus();
        }

        private void btnCondSel_Click(object sender, RoutedEventArgs e)
        {
            btnCondSel.IsDropDownOpen = false;

            ConditionalSelectForm csf = new ConditionalSelectForm();
            csf.ShowDialog();
            if (csf.CancelAction == false)
            {
                ConditionallySelectFiles(csf.csd);
                Explorer.Focus();
            }
            Explorer.Focus();
        }

        private void ConditionallySelectFiles(ConditionalSelectData csd)
        {

            if (csd == null)
            {
                return;
            }

            ShellObjectCollection shells = Explorer.Items;
            ShellObjectCollection l1shells = new ShellObjectCollection();
            ShellObjectCollection l2shells = new ShellObjectCollection();
            ShellObjectCollection l3shells = new ShellObjectCollection();
            ShellObjectCollection l4shells = new ShellObjectCollection();
            ShellObjectCollection l5shells = new ShellObjectCollection();

            Explorer.DeSelectAllItems();

            if (csd.FilterByFileName == true)
            {
                foreach (ShellObject item in shells)
                {
                    if (Directory.Exists(item.ParsingName) == false)
                    {
                        FileInfo data = new FileInfo(item.ParsingName);
                        if (csd.FileNameData.matchCase == true)
                        {
                            switch (csd.FileNameData.filter)
                            {
                                case ConditionalSelectParameters.FileNameFilterTypes.Contains:
                                    if (data.Name.Contains(csd.FileNameData.query) == true)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.StartsWith:
                                    if (data.Name.StartsWith(csd.FileNameData.query) == true)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.EndsWith:
                                    if (data.Name.EndsWith(csd.FileNameData.query) == true)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.Equals:
                                    if (data.Name == csd.FileNameData.query)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.DoesNotContain:
                                    if (data.Name.Contains(csd.FileNameData.query) == false)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.NotEqualTo:
                                    if (data.Name != csd.FileNameData.query)
                                        l1shells.Add(item);
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            switch (csd.FileNameData.filter)
                            {
                                case ConditionalSelectParameters.FileNameFilterTypes.Contains:
                                    if (data.Name.ToLower().Contains(csd.FileNameData.query.ToLower()) == true)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.StartsWith:
                                    if (data.Name.ToLower().StartsWith(csd.FileNameData.query.ToLower()) == true)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.EndsWith:
                                    if (data.Name.ToLower().EndsWith(csd.FileNameData.query.ToLower()) == true)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.Equals:
                                    if (data.Name.ToLower() == csd.FileNameData.query.ToLower())
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.DoesNotContain:
                                    if (data.Name.ToLower().Contains(csd.FileNameData.query.ToLower()) == false)
                                        l1shells.Add(item);
                                    break;
                                case ConditionalSelectParameters.FileNameFilterTypes.NotEqualTo:
                                    if (data.Name.ToLower() != csd.FileNameData.query.ToLower())
                                        l1shells.Add(item);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (ShellObject item in shells)
                {
                    if (Directory.Exists(item.ParsingName) == false)
                    {
                        l1shells.Add(item);
                    }
                }
            }

            if (csd.FilterByFileSize == true)
            {
                foreach (ShellObject item in l1shells)
                {
                    FileInfo data = new FileInfo(item.ParsingName);
                    switch (csd.FileSizeData.filter)
                    {
                        case ConditionalSelectParameters.FileSizeFilterTypes.LargerThan:
                            if (data.Length > csd.FileSizeData.query1)
                                l2shells.Add(item);
                            break;
                        case ConditionalSelectParameters.FileSizeFilterTypes.SmallerThan:
                            if (data.Length < csd.FileSizeData.query1)
                                l2shells.Add(item);
                            break;
                        case ConditionalSelectParameters.FileSizeFilterTypes.Equals:
                            if (data.Length == csd.FileSizeData.query1)
                                l2shells.Add(item);
                            break;
                        case ConditionalSelectParameters.FileSizeFilterTypes.Between:
                            long smallbound;
                            long largebound;
                            if (csd.FileSizeData.query2 > csd.FileSizeData.query1)
                            {
                                smallbound = csd.FileSizeData.query1;
                                largebound = csd.FileSizeData.query2;
                            }
                            else
                            {
                                if (csd.FileSizeData.query2 < csd.FileSizeData.query1)
                                {
                                    smallbound = csd.FileSizeData.query2;
                                    largebound = csd.FileSizeData.query1;
                                }
                                else
                                {
                                    // they are the same, use Equal code

                                    //MessageBox.Show("Could not continue Conditional Select operation. When searching for files that is between a certain size, make sure the upper and lower bounds of the search parameters are not the same.", "BetterExplorer Conditional Select - Filter by File Size", MessageBoxButton.OK, MessageBoxImage.Error);
                                    //return;

                                    if (data.Length == csd.FileSizeData.query1)
                                        l2shells.Add(item);
                                    break;
                                }
                            }

                            if (data.Length > smallbound)
                                if (data.Length < largebound)
                                    l2shells.Add(item);

                            break;
                        case ConditionalSelectParameters.FileSizeFilterTypes.NotEqualTo:
                            if (data.Length != csd.FileSizeData.query1)
                                l2shells.Add(item);
                            break;
                        case ConditionalSelectParameters.FileSizeFilterTypes.NotBetween:
                            long smallbound2;
                            long largebound2;
                            if (csd.FileSizeData.query2 > csd.FileSizeData.query1)
                            {
                                smallbound2 = csd.FileSizeData.query1;
                                largebound2 = csd.FileSizeData.query2;
                            }
                            else
                            {
                                if (csd.FileSizeData.query2 < csd.FileSizeData.query1)
                                {
                                    smallbound2 = csd.FileSizeData.query2;
                                    largebound2 = csd.FileSizeData.query1;
                                }
                                else
                                {
                                    // they are the same, use Unequal code
                                    if (data.Length != csd.FileSizeData.query1)
                                        l2shells.Add(item);
                                    break;
                                    //MessageBox.Show("Could not continue Conditional Select operation. When searching for files that is not between a certain size, make sure the upper and lower bounds of the search parameters are not the same.", "BetterExplorer Conditional Select - Filter by File Size", MessageBoxButton.OK, MessageBoxImage.Error);
                                    //return;
                                }
                            }

                            if (data.Length < smallbound2 || data.Length > largebound2)
                                l2shells.Add(item);

                            break;
                        default:
                            break;
                    }

                }
            }
            else
            {
                foreach (ShellObject item in l1shells)
                {
                    l2shells.Add(item);
                }
            }

            if (csd.FilterByDateCreated == true)
            {
                foreach (ShellObject item in FilterByCreateDate(l2shells, csd.DateCreatedData.queryDate, csd.DateCreatedData.filter))
                {
                    l3shells.Add(item);
                }
            }
            else
            {
                foreach (ShellObject item in l2shells)
                {
                    l3shells.Add(item);
                }
            }

            if (csd.FilterByDateModified == true)
            {
                foreach (ShellObject item in FilterByWriteDate(l3shells, csd.DateModifiedData.queryDate, csd.DateModifiedData.filter))
                {
                    l4shells.Add(item);
                }
            }
            else
            {
                foreach (ShellObject item in l2shells)
                {
                    l4shells.Add(item);
                }
            }

            if (csd.FilterByDateAccessed == true)
            {
                foreach (ShellObject item in FilterByAccessDate(l4shells, csd.DateAccessedData.queryDate, csd.DateAccessedData.filter))
                {
                    l5shells.Add(item);
                }
            }
            else
            {
                foreach (ShellObject item in l4shells)
                {
                    l5shells.Add(item);
                }
            }

            List<ShellObject> sel = new List<ShellObject>();

            foreach (ShellObject item in l5shells)
            {
                // this is where the code should be to select multiple files.
                //
                // However, the Explorer control does not support selecting multiple files.

                Explorer.SelectItem(item);
            }

            Explorer.Focus();

        }

        public ShellObjectCollection FilterByCreateDate(ShellObjectCollection shells, DateTime datetocompare, ConditionalSelectParameters.DateFilterTypes filter)
        {
            ShellObjectCollection outshells = new ShellObjectCollection();

            foreach (ShellObject item in shells)
            {
                FileInfo data = new FileInfo(item.ParsingName);
                switch (filter)
                {
                    case ConditionalSelectParameters.DateFilterTypes.EarlierThan:
                        if (DateTime.Compare(data.CreationTimeUtc.Date, datetocompare.Date) < 0)
                            outshells.Add(item);
                        break;
                    case ConditionalSelectParameters.DateFilterTypes.LaterThan:
                        if (DateTime.Compare(data.CreationTimeUtc.Date, datetocompare.Date) > 0)
                            outshells.Add(item);
                        break;
                    case ConditionalSelectParameters.DateFilterTypes.Equals:
                        if (DateTime.Compare(data.CreationTimeUtc.Date, datetocompare.Date) == 0)
                            outshells.Add(item);
                        break;
                    default:
                        break;
                }
            }

            return outshells;
        }

        public ShellObjectCollection FilterByWriteDate(ShellObjectCollection shells, DateTime datetocompare, ConditionalSelectParameters.DateFilterTypes filter)
        {
            ShellObjectCollection outshells = new ShellObjectCollection();

            foreach (ShellObject item in shells)
            {
                FileInfo data = new FileInfo(item.ParsingName);
                switch (filter)
                {
                    case ConditionalSelectParameters.DateFilterTypes.EarlierThan:
                        if (DateTime.Compare(data.LastWriteTimeUtc.Date, datetocompare) < 0)
                            outshells.Add(item);
                        break;
                    case ConditionalSelectParameters.DateFilterTypes.LaterThan:
                        if (DateTime.Compare(data.LastWriteTimeUtc.Date, datetocompare) > 0)
                            outshells.Add(item);
                        break;
                    case ConditionalSelectParameters.DateFilterTypes.Equals:
                        if (DateTime.Compare(data.LastWriteTimeUtc.Date, datetocompare) == 0)
                            outshells.Add(item);
                        break;
                    default:
                        break;
                }
            }

            return outshells;
        }

        public ShellObjectCollection FilterByAccessDate(ShellObjectCollection shells, DateTime datetocompare, ConditionalSelectParameters.DateFilterTypes filter)
        {
            ShellObjectCollection outshells = new ShellObjectCollection();

            foreach (ShellObject item in shells)
            {
                FileInfo data = new FileInfo(item.ParsingName);
                switch (filter)
                {
                    case ConditionalSelectParameters.DateFilterTypes.EarlierThan:
                        if (DateTime.Compare(data.LastAccessTimeUtc.Date, datetocompare) < 0)
                            outshells.Add(item);
                        break;
                    case ConditionalSelectParameters.DateFilterTypes.LaterThan:
                        if (DateTime.Compare(data.LastAccessTimeUtc.Date, datetocompare) > 0)
                            outshells.Add(item);
                        break;
                    case ConditionalSelectParameters.DateFilterTypes.Equals:
                        if (DateTime.Compare(data.LastAccessTimeUtc.Date, datetocompare) == 0)
                            outshells.Add(item);
                        break;
                    default:
                        break;
                }
            }

            return outshells;
        }

        #endregion

        #region Size Chart

        private void btnFSizeChart_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.SelectedItems.Count > 0)
            {
                if ((Explorer.SelectedItems[0].IsFolder || Explorer.SelectedItems[0].IsDrive) && Explorer.SelectedItems[0].IsFileSystemObject)
                {
                    FolderSizeWindow fsw = new FolderSizeWindow(Explorer.SelectedItems[0].ParsingName);
                    fsw.Owner = this;
                    fsw.Show();
                }
                else
                {
                    if ((Explorer.NavigationLog.CurrentLocation.IsFolder || Explorer.NavigationLog.CurrentLocation.IsDrive)
                            && Explorer.NavigationLog.CurrentLocation.IsFileSystemObject)
                    {
                        FolderSizeWindow fsw = new FolderSizeWindow(Explorer.NavigationLog.CurrentLocation.ParsingName);
                        fsw.Owner = this;
                        fsw.Show();
                    }
                }

            }
            else
            {
                if ((Explorer.NavigationLog.CurrentLocation.IsFolder || Explorer.NavigationLog.CurrentLocation.IsDrive)
                                            && Explorer.NavigationLog.CurrentLocation.IsFileSystemObject)
                {
          FolderSizeWindow fsw = new FolderSizeWindow(Explorer.NavigationLog.CurrentLocation.ParsingName) { Owner = this };
                    fsw.Show();
                }
            }

        }

        private void btnSizeChart_Click(object sender, RoutedEventArgs e)
        {
      FolderSizeWindow fsw = new FolderSizeWindow(Explorer.NavigationLog.CurrentLocation.ParsingName) { Owner = this };
            fsw.Show();
        }

        #endregion

        #region Home Tab

        private void miJunctionpoint_Click(object sender, RoutedEventArgs e)
        {
            StringCollection DropList = System.Windows.Forms.Clipboard.GetFileDropList();
            string PathForDrop = Explorer.NavigationLog.CurrentLocation.ParsingName.Replace(@"\\", @"\");
            foreach (string item in DropList)
            {
                ShellObject o = ShellObject.FromParsingName(item);
                JunctionPointUtils.JunctionPoint.Create(String.Format(@"{0}\{1}", PathForDrop, o.GetDisplayName(DisplayNameType.Default)),
                                                        o.ParsingName, true);
                AddToLog(String.Format(@"Created Junction Point at {0}\{1} linked to {2}", PathForDrop, o.GetDisplayName(DisplayNameType.Default), o.ParsingName));
                o.Dispose();
            }
        }

        private void miCreateSymlink_Click(object sender, RoutedEventArgs e)
        {

            StringCollection DropList = System.Windows.Forms.Clipboard.GetFileDropList();
            string PathForDrop = Explorer.NavigationLog.CurrentLocation.ParsingName.Replace(@"\\", @"\");

            String CurrentexePath = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
            string dir = Path.GetDirectoryName(CurrentexePath);
            string ExePath = Path.Combine(dir, @"BetterExplorerOperations.exe");


            int winhandle = (int)WindowsAPI.getWindowId(null, "BetterExplorerOperations");

            List<ShellObject> items = new List<ShellObject>();

            foreach (string item in DropList)
            {
                ShellObject o = ShellObject.FromParsingName(item);
                items.Add(o);
                AddToLog(String.Format(@"Created Symbolic Link at {0}\\{1} linked to {2}", PathForDrop, o.GetDisplayName(DisplayNameType.Default), o.ParsingName));
            }

            string sources = PathStringCombiner.CombinePaths(items, ";", true);
            string drops = PathStringCombiner.CombinePathsWithSinglePath(PathForDrop + @"\", items, false);


            for (int val = 0; val < items.Count; val++)
            {
                string source = items[val].ParsingName.Replace(@"\\", @"\");
                string drop = String.Format(@"{0}\{1}", PathForDrop, items[val].GetDisplayName(DisplayNameType.Default));
            }

            using (Process proc = new Process())
            {
                var psi = new ProcessStartInfo 
                { 
                    FileName = ExePath, 
                    Verb = "runas", 
                    UseShellExecute = true, 
                    Arguments = String.Format("/env /user:Administrator \"{0}\"", ExePath) 
                };

                proc.StartInfo = psi;
                proc.Start();
                Thread.Sleep(1000);
                int res = WindowsAPI.sendWindowsStringMessage((int)WindowsAPI.getWindowId(null, "BetterExplorerOperations"), 0, 
                    "0x88779", sources, drops, "", "", "");
                proc.WaitForExit();
                if (proc.ExitCode == 1)
                    MessageBox.Show("Error in creating symlink", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {

            Explorer.ShowPropPage(this.Handle, Explorer.SelectedItems[0].ParsingName, 
                WindowsAPI.LoadResourceString(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"twext.dll"),1024,"Previous Versions"));
        }

        private void btnBackstageExit_Click(object sender, RoutedEventArgs e)
        {
      //! We call Shutdown() so to explisit shutdown the app regardless of windows closing cancel flag.
      if (r != null)
        r.Close();

      Application.Current.Shutdown();
        }

        // create new window
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {

            var k = System.Reflection.Assembly.GetExecutingAssembly().Location;
            Process.Start(k, "/nw");
        }


        void miow_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (sender as MenuItem);
            Process.Start(item.Tag.ToString(), String.Format("\"{0}\"", Explorer.SelectedItems[0].ParsingName));
        }

        void mif_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (sender as MenuItem);

      var obj = ShellObjectFactory.Create((item.Tag as ShellObject).PIDL);
      ShellLink lnk = new ShellLink(obj.NativeShellItem as IShellItem2);

      Explorer.Navigate(lnk.TargetShellObject);

      lnk.Dispose();
      obj.Dispose();
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            StringCollection sc = new StringCollection();
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                sc.Add(item.ParsingName);
            }
            System.Windows.Forms.Clipboard.SetFileDropList(sc);
            Explorer.IsMoveClipboardOperation = false;
        }

        private void btnPaste_Click(object sender, RoutedEventArgs e)
        {
      if (!ExplorerBrowser.IsCustomDialogs) {
        FileOperationsData PasteData = new FileOperationsData();
        PasteData.DropList = System.Windows.Forms.Clipboard.GetFileDropList();
        if (Explorer.SelectedItems.Count > 0 & Explorer.SelectedItems.Count < 2) {
          PasteData.PathForDrop = Explorer.SelectedItems[0].ParsingName;

        } else {
          PasteData.PathForDrop = Explorer.NavigationLog.CurrentLocation.ParsingName;
        }
        Thread pasteThread = null;
        if (Explorer.IsMoveClipboardOperation) {
          pasteThread = new Thread(Explorer.DoMove);
          AddToLog(String.Format("The following files have been pasted at {0}: {1}", PasteData.PathForDrop, GetStringsFromCollection(PasteData.DropList)));
        } else {
          pasteThread = new Thread(Explorer.DoCopy);
        }
        pasteThread.SetApartmentState(ApartmentState.STA);
        pasteThread.Start(PasteData);
      } else {
        if (Explorer.IsMoveClipboardOperation) {
          var DestinationLocation = Explorer.SelectedItems.Count == 1 ? Explorer.SelectedItems[0].ParsingName : Explorer.NavigationLog.CurrentLocation.ParsingName; ;
          var SourceItemsCollection = System.Windows.Forms.Clipboard.GetFileDropList().OfType<String>().ToArray();
          var win = Application.Current.MainWindow;

          FileOperation tempWindow = new FileOperation(SourceItemsCollection, DestinationLocation, OperationType.Move);
          FileOperationDialog currentDialog = win.OwnedWindows.OfType<FileOperationDialog>().SingleOrDefault();

          if (currentDialog == null) {
            currentDialog = new FileOperationDialog();
            tempWindow.ParentContents = currentDialog;
            currentDialog.Owner = win;

            tempWindow.Visibility = Visibility.Collapsed;
            currentDialog.Contents.Add(tempWindow);
          } else {
            tempWindow.ParentContents = currentDialog;
            tempWindow.Visibility = Visibility.Collapsed;
            currentDialog.Contents.Add(tempWindow);
          }
        } else {
          var DestinationLocation = Explorer.SelectedItems.Count == 1 ? Explorer.SelectedItems[0].ParsingName : Explorer.NavigationLog.CurrentLocation.ParsingName; ;
          var SourceItemsCollection = System.Windows.Forms.Clipboard.GetFileDropList().OfType<String>().ToArray();
          var win = Application.Current.MainWindow;

          FileOperation tempWindow = new FileOperation(SourceItemsCollection, DestinationLocation, OperationType.Copy);
          FileOperationDialog currentDialog = win.OwnedWindows.OfType<FileOperationDialog>().SingleOrDefault();

          if (currentDialog == null) {
            currentDialog = new FileOperationDialog();
            tempWindow.ParentContents = currentDialog;
            currentDialog.Owner = win;

            tempWindow.Visibility = Visibility.Collapsed;
            currentDialog.Contents.Add(tempWindow);
          } else {
            tempWindow.ParentContents = currentDialog;
            tempWindow.Visibility = Visibility.Collapsed;
            currentDialog.Contents.Add(tempWindow);
          }
        }
      }
        }

        // Delete
        private void SplitButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender, e);
        }

        // Rename
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            IsAfterRename = false;
            Explorer.DoRename();
        }

        private void btnPathCopy_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.SelectedItems.Count > 1)
            {
                string path = null;
                foreach (ShellObject item in Explorer.SelectedItems)
                {
                    //This way i think is better of making multiple line in .Net ;)
                    if (string.IsNullOrEmpty(path))
                    {
                        path = item.ParsingName;
                    }
                    else
                    {
            path = String.Format("{0}\r\n{1}", path, item.ParsingName);
                    }

                }
                Clipboards.SetText(path);
            }
            else if (Explorer.SelectedItems.Count == 1)
            {
                Clipboards.SetText(Explorer.SelectedItems[0].ParsingName);
            }
            else
            {
                Clipboards.SetText(Explorer.NavigationLog.CurrentLocation.ParsingName);
            }

        }

        private string GetStringsFromCollection(StringCollection coll, string separator = " ")
        {
            string path = null;
            foreach (string item in coll)
            {
                if (string.IsNullOrEmpty(path))
                {
                    path = item;
                }
                else
                {
                    path = path + separator + item;
                }
            }
            return path;
        }

        private void btnSelAll_Click(object sender, RoutedEventArgs e)
        {
            Explorer.SelectAllItems();
        }

        private void btnSelNone_Click(object sender, RoutedEventArgs e)
        {
            Explorer.DeSelectAllItems();
        }

        private string ListAllSelectedItems()
        {
            string path = null;
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                //This way i think is better of making multiple line in .Net ;)
                if (string.IsNullOrEmpty(path))
                {
                    path = item.ParsingName;
                }
                else
                {
          path = String.Format("{0} {1}", path, item.ParsingName);
                }

            }
            return path;
        }

        // Delete > Send to Recycle Bin
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            //KeepFocusOnExplorer = true;
            AddToLog(String.Format("The following files have been moved to the Recycle Bin: {0}", ListAllSelectedItems()));
            SetDeleteOperation(true);
        }

        // Delete > Permanently Delete
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            //KeepFocusOnExplorer = true;
            SetDeleteOperation(false);
        }

        string LastPath = "";
        public bool IsCompartibleRename = false;
        public bool IsAfterRename = false;

        // New Folder/Library
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
      Explorer.SetExplorerFocus();
            string path = "";

            bool IsLib = false;
            if (Explorer.NavigationLog.CurrentLocation.ParsingName == KnownFolders.Libraries.ParsingName)
            {
        path = Explorer.CreateNewLibrary(FindResource("btnNewLibraryCP").ToString()).GetDisplayName(DisplayNameType.Default);
                AddToLog("Library created");
                IsLib = true;
            }
            else
            {
                path = Explorer.CreateNewFolder(FindResource("btnNewFolderCP").ToString());
                AddToLog("Folder created in " + Explorer.NavigationLog.CurrentLocation.ParsingName);
            }

            LastPath = path;

            //if (IsCompartibleRename)
            //{
            //	Explorer.RefreshExplorer();

            //}
            //else
            //{
                WindowsAPI.SHChangeNotify(WindowsAPI.HChangeNotifyEventID.SHCNE_MKDIR,
                WindowsAPI.HChangeNotifyFlags.SHCNF_PATHW | WindowsAPI.HChangeNotifyFlags.SHCNF_FLUSHNOWAIT,
                    Marshal.StringToHGlobalAuto(path.Replace(@"\\", @"\")), IntPtr.Zero);
            //}


            IsAfterRename = false;




            IsLibW = IsLib;
            IsAfterFolderCreate = true;

        }


        private void btnProperties_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.SelectedItems.Count > 0)
            {
                Explorer.ShowFileProperties();
            }
            else
            {
                Explorer.ShowFileProperties(Explorer.NavigationLog.CurrentLocation.ParsingName);
            }
            Explorer.Focus();
        }


        [DllImport("user32.dll", SetLastError = true)]
        private static extern int RegisterHotKey(IntPtr hwnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int UnregisterHotKey(IntPtr hwnd, int id);

        //short mHotKeyId = 0;

        private void btnInvSel_Click(object sender, RoutedEventArgs e)
        {
            Explorer.InvertSelection();
            //WindowsAPI.SendMessage((int)Explorer.Handle, 0x0111, 0x7013, 0);
        }

        private void btnPasetShC_Click(object sender, RoutedEventArgs e)
        {

            StringCollection DropList = System.Windows.Forms.Clipboard.GetFileDropList();
            string PathForDrop = Explorer.NavigationLog.CurrentLocation.ParsingName;
            foreach (string item in DropList)
            {
        using (ShellLinkApi shortcut = new ShellLinkApi())
                {
                    ShellObject o = ShellObject.FromParsingName(item);
                    shortcut.Target = item;
                    shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(item);
                    shortcut.Description = o.GetDisplayName(DisplayNameType.Default);
          shortcut.DisplayMode = ShellLinkApi.LinkDisplayMode.edmNormal;
                    shortcut.Save(PathForDrop + "\\" + o.GetDisplayName(DisplayNameType.Default) + ".lnk");
                    AddToLog(String.Format("Shortcut created at {0}\\{1} from source {2}", PathForDrop, o.GetDisplayName(DisplayNameType.Default), item));
                    o.Dispose();
                }
            }

        }

        private void btnctDocuments_Click(object sender, RoutedEventArgs e)
        {
            SetFOperation(KnownFolders.Documents.ParsingName, OperationType.Copy);
        }

        private void btnctDesktop_Click(object sender, RoutedEventArgs e)
        {
            SetFOperation(KnownFolders.Desktop.ParsingName, OperationType.Copy);
        }

        private void btnctDounloads_Click(object sender, RoutedEventArgs e)
        {
            SetFOperation(KnownFolders.Downloads.ParsingName, OperationType.Copy);
        }

        private void btnmtDocuments_Click(object sender, RoutedEventArgs e)
        {
            SetFOperation(KnownFolders.Documents.ParsingName, OperationType.Move);
        }

        private void btnmtDesktop_Click(object sender, RoutedEventArgs e)
        {
            SetFOperation(KnownFolders.Desktop.ParsingName, OperationType.Move);
        }

        private void btnmtDounloads_Click(object sender, RoutedEventArgs e)
        {
            SetFOperation(KnownFolders.Downloads.ParsingName, OperationType.Move);
        }

        private void btnmtOther_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SetFOperation(dlg.FileName, OperationType.Move);
            }
        }

    private void SetFOperation(String FileName,  OperationType opType) {
        if (!ExplorerBrowser.IsCustomDialogs) {
          FileOperationsData dd = new FileOperationsData();
          dd.Shellobjects = Explorer.SelectedItems;
          dd.PathForDrop = FileName;
          Thread CopyThread = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
          CopyThread.SetApartmentState(ApartmentState.STA);
          CopyThread.Start(dd);
          AddToLog(String.Format("The following files have been copied to {0}: {1}", dd.PathForDrop, PathStringCombiner.CombinePaths(dd.Shellobjects.ToList(), " ")));
        } else {
          var DestinationLocation = FileName;
          var SourceItemsCollection = Explorer.SelectedItems.Select(c => c.ParsingName).ToArray();
          var win = Application.Current.MainWindow;

          FileOperation tempWindow = new FileOperation(SourceItemsCollection, DestinationLocation, opType);
          FileOperationDialog currentDialog = win.OwnedWindows.OfType<FileOperationDialog>().SingleOrDefault();

          if (currentDialog == null) {
            currentDialog = new FileOperationDialog();
            tempWindow.ParentContents = currentDialog;
            currentDialog.Owner = win;

            tempWindow.Visibility = Visibility.Collapsed;
            currentDialog.Contents.Add(tempWindow);
          } else {
            tempWindow.ParentContents = currentDialog;
            tempWindow.Visibility = Visibility.Collapsed;
            currentDialog.Contents.Add(tempWindow);
          }
        }
    }
    private void SetFOperation(ShellObject obj, OperationType opType) {
      if (!ExplorerBrowser.IsCustomDialogs) {
        FileOperationsData dd = new FileOperationsData();
        dd.Shellobjects = Explorer.SelectedItems;
        dd.PathForDrop = obj.ParsingName;
        Thread CopyThread = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
        CopyThread.SetApartmentState(ApartmentState.STA);
        CopyThread.Start(dd);
        AddToLog(String.Format("The following files have been copied to {0}: {1}", dd.PathForDrop, PathStringCombiner.CombinePaths(dd.Shellobjects.ToList(), " ")));
      } else {
        var DestinationLocation = obj.ParsingName;
        var SourceItemsCollection = Explorer.SelectedItems.Select(c => c.ParsingName).ToArray();
        var win = Application.Current.MainWindow;

        FileOperation tempWindow = new FileOperation(SourceItemsCollection, DestinationLocation, opType);
        FileOperationDialog currentDialog = win.OwnedWindows.OfType<FileOperationDialog>().SingleOrDefault();

        if (currentDialog == null) {
          currentDialog = new FileOperationDialog();
          tempWindow.ParentContents = currentDialog;
          currentDialog.Owner = win;

          tempWindow.Visibility = Visibility.Collapsed;
          currentDialog.Contents.Add(tempWindow);
        } else {
          tempWindow.ParentContents = currentDialog;
          tempWindow.Visibility = Visibility.Collapsed;
          currentDialog.Contents.Add(tempWindow);
        }
      }
    }
    private void SetDeleteOperation(bool isMoveToRB)
    {
        if (!ExplorerBrowser.IsCustomDialogs)
        {
            if (isMoveToRB)
            {
                Explorer.DeleteToRecycleBin();
            }
            else
            {
              Thread CopyThread = new Thread(ExplorerBrowser.DoDelete);
              CopyThread.SetApartmentState(ApartmentState.STA);
              CopyThread.Start(Explorer.SelectedItems);
            }
        }
        else
        {
                    var sourceItemsCollection = Explorer.SelectedItems.Select(c => c.ParsingName).ToArray();
            HookLibManager.ShowDeleteDialog(sourceItemsCollection, this, isMoveToRB);
        }
    }

    private void btnctOther_Click(object sender, RoutedEventArgs e)
        {
      CommonOpenFileDialog dlg = new CommonOpenFileDialog();
      dlg.IsFolderPicker = true;
      if (dlg.ShowDialog() == CommonFileDialogResult.Ok) {
        SetFOperation(dlg.FileName,OperationType.Copy);
      }
            Explorer.Focus();
        }

        private void btnCopyto_Click(object sender, RoutedEventArgs e)
        {
            btnctOther_Click(sender, e);
        }

        private void btnMoveto_Click(object sender, RoutedEventArgs e)
        {
            btnmtOther_Click(sender, e);
        }

        private void btnCut_Click(object sender, RoutedEventArgs e)
        {
            AddToLog("The following files have been cut: " + PathStringCombiner.CombinePaths(Explorer.SelectedItems.ToList(), " ", false));
            Explorer.DoCut();
        }

        private void btnNewItem_Click(object sender, RoutedEventArgs e)
        {
            WindowsAPI.SHELLSTATE state = new WindowsAPI.SHELLSTATE() { fShowAllObjects = 0 };
            WindowsAPI.SHGetSetSettings(ref state, WindowsAPI.SSF.SSF_SHOWALLOBJECTS, true);
        }

        private void btnOpenWith_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(String.Format("\"{0}\"", Explorer.SelectedItems[0].ParsingName));
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
      Explorer.EditFile(Explorer.SelectedItems[0].ParsingName);
        }

        private void btnFavorites_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.GetSelectedItemsCount() == 1)
            {
        ShellLinkApi link = new ShellLinkApi();
        link.DisplayMode = ShellLinkApi.LinkDisplayMode.edmNormal;
                link.Target = Explorer.SelectedItems[0].ParsingName;
                link.Save(KnownFolders.Links.ParsingName + @"\" +
                    Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default) + ".lnk");
                link.Dispose();
            }

            if (Explorer.GetSelectedItemsCount() == 0)
            {
        ShellLinkApi link = new ShellLinkApi();
        link.DisplayMode = ShellLinkApi.LinkDisplayMode.edmNormal;
                link.Target = Explorer.NavigationLog.CurrentLocation.ParsingName;
                link.Save(KnownFolders.Links.ParsingName + @"\" +
                    Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default) + ".lnk");
                link.Dispose();
            }

        }

        #endregion

        #region Drive Tools / Virtual Disk Tools

        private void btnFormatDrive_Click(object sender, RoutedEventArgs e)
        {
      if (MessageBox.Show("Are you sure you want to do this?", FindResource("btnFormatDriveCP").ToString(), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
      {
        Thread formatDriveThread = new Thread(() =>
        {
          Explorer.FormatDrive(IntPtr.Zero);
        });
        formatDriveThread.Start();
      }
        }

        private void btnCleanDrive_Click(object sender, RoutedEventArgs e)
        {
            Explorer.CleanupDrive();
        }

        private void btnDefragDrive_Click(object sender, RoutedEventArgs e)
        {
            Explorer.DefragDrive();
        }

        [DllImport("winmm.dll")]
        static extern Int32 mciSendString(String command, StringBuilder buffer, Int32 bufferSize, IntPtr hwndCallback);

        private char GetDriveLetterFromDrivePath(string path)
        {
            return path.Substring(0, 1).ToCharArray()[0];
        }

        private void OpenCDTray(char DriveLetter)
        {
            mciSendString(String.Format("open {0}: type CDAudio alias drive{1}", DriveLetter, DriveLetter), null, 0, IntPtr.Zero);
            mciSendString(String.Format("set drive{0} door open", DriveLetter), null, 0, IntPtr.Zero);
        }

        private void CloseCDTray(char DriveLetter)
        {
            mciSendString(String.Format("open {0}: type CDAudio alias drive{1}", DriveLetter, DriveLetter), null, 0, IntPtr.Zero);
            mciSendString(String.Format("set drive{0} door closed", DriveLetter), null, 0, IntPtr.Zero);
        }

        private void btnOpenTray_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.SelectedItems[0].GetDriveInfo() != null)
            {
                if (Explorer.SelectedItems[0].GetDriveInfo().DriveType == DriveType.CDRom)
                {
                    OpenCDTray(GetDriveLetterFromDrivePath(Explorer.SelectedItems[0].ParsingName));
                }
            }
        }

        private void btnCloseTray_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.SelectedItems[0].GetDriveInfo() != null)
            {
                if (Explorer.SelectedItems[0].GetDriveInfo().DriveType == DriveType.CDRom)
                {
                    CloseCDTray(GetDriveLetterFromDrivePath(Explorer.SelectedItems[0].ParsingName));
                }
            }
        }

        private void EjectDisk(char DriveLetter)
        {
            Thread t = new Thread(() =>
            {
                Thread.Sleep(10);
                VolumeDeviceClass vdc = new VolumeDeviceClass();
                foreach (Volume item in vdc.Devices)
                {
                    if (GetDriveLetterFromDrivePath(item.LogicalDrive) == DriveLetter)
                    {
                        var veto = item.Eject(false);
                        if (veto != BetterExplorer.UsbEject.Native.PNP_VETO_TYPE.TypeUnknown)
                        {
                          if (veto == BetterExplorer.UsbEject.Native.PNP_VETO_TYPE.Ok)
                          {
                            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                              (Action)(() =>
                              {
                                this.beNotifyIcon.ShowBalloonTip("Information", String.Format("It is safe to remove {0}", item.LogicalDrive), Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                                var tabsForRemove = tabControl1.Items.OfType<ClosableTabItem>()
                                                               .Where(w => w.Path.IsFileSystemObject && 
                                                                           Path.GetPathRoot(w.Path.ParsingName).ToLowerInvariant() == 
                                                                           String.Format("{0}:\\", DriveLetter).ToLowerInvariant()).ToArray();

                                foreach (ClosableTabItem tab in tabsForRemove)
                                {
                                  CloseTab(tab, false);
                                }
                              }));
                          }
                          else
                          {
                            var message = String.Empty;
                            var obj = ShellObject.FromParsingName(item.LogicalDrive);
                            switch (veto)
                            {
                              case Native.PNP_VETO_TYPE.Ok:
                                break;
                              case Native.PNP_VETO_TYPE.TypeUnknown:
                                break;
                              case Native.PNP_VETO_TYPE.LegacyDevice:
                                break;
                              case Native.PNP_VETO_TYPE.PendingClose:
                                break;
                              case Native.PNP_VETO_TYPE.WindowsApp:
                                break;
                              case Native.PNP_VETO_TYPE.WindowsService:
                                break;
                              case Native.PNP_VETO_TYPE.OutstandingOpen:
                                message = String.Format("The device {0} can not be disconnected becasue is in use!", obj.GetDisplayName(DisplayNameType.Default));
                                break;
                              case Native.PNP_VETO_TYPE.Device:
                                break;
                              case Native.PNP_VETO_TYPE.Driver:
                                break;
                              case Native.PNP_VETO_TYPE.IllegalDeviceRequest:
                                break;
                              case Native.PNP_VETO_TYPE.InsufficientPower:
                                break;
                              case Native.PNP_VETO_TYPE.NonDisableable:
                                message = String.Format("The device {0} does not support disconnecting!", obj.GetDisplayName(DisplayNameType.Default));
                                break;
                              case Native.PNP_VETO_TYPE.LegacyDriver:
                                break;
                              default:
                                break;
                            }
                            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                              (Action)(() =>
                              {
                                this.beNotifyIcon.ShowBalloonTip("Error", message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
                                
                              }));
                            obj.Dispose();
                          }
                        }
                        break;
                    }
                }
            });
            t.Start();
        }

        private void btnEjectDevice_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.SelectedItems[0].GetDriveInfo() != null)
            {
                if (Explorer.SelectedItems[0].GetDriveInfo().DriveType == DriveType.Removable)
                {
                    EjectDisk(GetDriveLetterFromDrivePath(Explorer.SelectedItems[0].ParsingName));
                }
            }
        }

        // Virtual Disk Tools

        private bool CheckImDiskInstalled()
        {
            bool installed = true;

            try
            {
                ImDiskAPI.GetDeviceList();
            }
            catch (System.DllNotFoundException)
            {
                installed = false;
            }

            return installed;
        }

        public void ShowInstallImDiskMessage()
        {
            if (MessageBox.Show("It appears you do not have the ImDisk Virtual Disk Driver installed. This driver is used to power Better Explorer's ISO-mounting features. \n\nWould you like to visit ImDisk's website to install the product? (http://www.ltr-data.se/opencode.html/)", "ImDisk Not Found", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                Process.Start("http://www.ltr-data.se/opencode.html/#ImDisk");
            }
        }

        private void btnAdvMountIso_Click(object sender, RoutedEventArgs e)
        {
            if (CheckImDiskInstalled() == false)
            {
                ShowInstallImDiskMessage();
                return;
            }

            MountIso mi = new MountIso();
            mi.Owner = this;
            mi.ShowDialog();
            if (mi.yep == true)
            {
                string DriveLetter;
                if (mi.chkPreselect.IsChecked == true)
                {
                    DriveLetter = String.Format("{0}:", ImDiskAPI.FindFreeDriveLetter());
                }
                else
                {
                    DriveLetter = String.Format("{0}:", (char)mi.cbbLetter.SelectedItem);
                }

                long size = 0;
                if (mi.chkPresized.IsChecked == false)
                {
                    size = Convert.ToInt64(mi.txtSize.Text);
                }

                ImDiskFlags imflags;

                switch (mi.cbbType.SelectedIndex)
                {
                    case 0:
                    //Hard Drive
                        imflags = ImDiskFlags.DeviceTypeHD;
                        break;
                    case 1:
                    // CD/DVD
                        imflags = ImDiskFlags.DeviceTypeCD;
                        break;
                    case 2:
                    // Floppy Disk
                        imflags = ImDiskFlags.DeviceTypeFD;
                        break;
                    case 3:
                    // Raw Data
                        imflags = ImDiskFlags.DeviceTypeRAW;
                        break;
                    default:
                        imflags = ImDiskFlags.DeviceTypeCD;
                        break;
                }

                switch (mi.cbbAccess.SelectedIndex)
                {
                    case 0:
                        // Access directly
                        imflags |= ImDiskFlags.FileTypeDirect;
                        break;
                    case 1:
                        // Copy to memory
                        imflags |= ImDiskFlags.FileTypeAwe;
                        break;
                    default:
                        imflags |= ImDiskFlags.FileTypeDirect;
                        break;
                }

                if (mi.chkRemovable.IsChecked == true)
                {
                    imflags |= ImDiskFlags.Removable;
                }

                if (mi.chkReadOnly.IsChecked == true)
                {
                    imflags |= ImDiskFlags.ReadOnly;
                }

                ImDiskAPI.CreateDevice(size, 0, 0, 0, 0, imflags, Explorer.SelectedItems[0].ParsingName, false, DriveLetter, IntPtr.Zero);
            }
        }

        private void btnMountIso_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO: add the code for mounting images with ImDisk. look the example below!

                var freeDriveLetter = String.Format("{0}:", ImDiskAPI.FindFreeDriveLetter());
                ImDiskAPI.CreateDevice(0, 0, 0, 0, 0, ImDiskFlags.Auto, Explorer.SelectedItems[0].ParsingName, false, freeDriveLetter, IntPtr.Zero);
            }
            catch (System.DllNotFoundException)
            {
                ShowInstallImDiskMessage();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while trying to mount this file. \n\n" + ex.Message, ex.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnWriteIso_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "isoburn.exe"),
                string.Format("\"{0}\"", Explorer.SelectedItems[0].ParsingName));
        }

        private List<char> GetLettersOfVirtualDrives(bool threaded = true)
        {
            if (threaded == true)
            {
                List<char> drives = new List<char>();
                Thread t = new Thread(() =>
                {
                    Thread.Sleep(10);
                    List<int> list = ImDiskAPI.GetDeviceList();
                    foreach (int item in list)
                    {
                        drives.Add(ImDiskAPI.QueryDevice(Convert.ToUInt32(item)).DriveLetter);
                    }
                });
                t.Start();
                return drives;
            }
            else
            {
                List<char> drives = new List<char>();
                List<int> list = ImDiskAPI.GetDeviceList();
                foreach (int item in list)
                {
                    try
                    {
                        drives.Add(ImDiskAPI.QueryDevice(Convert.ToUInt32(item)).DriveLetter);
                    }
                    catch
                    {

                    }
                }
                return drives;
            }
        }

        private uint GetDeviceNumberForDriveLetter(char letter)
        {
            List<int> list = ImDiskAPI.GetDeviceList();
            foreach (int item in list)
            {
                if (ImDiskAPI.QueryDevice(Convert.ToUInt32(item)).DriveLetter == letter)
                {
                    return Convert.ToUInt32(item);
                }
            }
            return 0;
        }

        private void btnUnmountDrive_Click(object sender, RoutedEventArgs e)
        {
            if (CheckImDiskInstalled() == false)
            {
                ShowInstallImDiskMessage();
                return;
            }

            try
            {
              var currentDeviceInfo = ImDiskAPI.QueryDevice(SelectedDriveID);
              if ((currentDeviceInfo.Flags & ImDiskFlags.DeviceTypeCD) != 0)
              {
                ImDiskAPI.ForceRemoveDevice(SelectedDriveID);
              }
              else
              {
                ImDiskAPI.RemoveDevice(SelectedDriveID);
              }
            }
            catch
            {
                if (MessageBox.Show("The drive could not be removed. Would you like to try to force a removal?", "Remove Drive Failed", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    try
                    {
                        ImDiskAPI.ForceRemoveDevice(SelectedDriveID);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not force remove this drive. Double-check to make sure this drive is a mounted ISO or VHD file.", "Could Not Force Remove Drive", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            Explorer.RefreshExplorer();
            ctgDrive.Visibility = System.Windows.Visibility.Collapsed;
        }

        #endregion

        #region Application Tools

        private void btnRunAsAdmin_Click(object sender, RoutedEventArgs e)
        {
            ExplorerBrowser.RunExeAsAdmin(Explorer.SelectedItems[0].ParsingName);
        }

        private void btnPin_Click(object sender, RoutedEventArgs e)
        {
            WindowsAPI.PinUnpinToTaskbar(Explorer.SelectedItems[0].ParsingName);
        }

        private void btnPinToStart_Click(object sender, RoutedEventArgs e)
        {
            WindowsAPI.PinUnpinToStartMenu(Explorer.SelectedItems[0].ParsingName);
            //if (Explorer.GetSelectedItemsCount() == 1)
            //{
            //    ShellLink link = new ShellLink();
            //    link.DisplayMode = ShellLink.LinkDisplayMode.edmNormal;
            //    link.Target = Explorer.SelectedItems[0].ParsingName;
            //    link.Save(KnownFolders.StartMenu.ParsingName + @"\" +
            //        Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default) + ".lnk");
            //    link.Dispose();
            //}
        }

        private void btnRunAs_Click(object sender, RoutedEventArgs e)
        {
            //RunExeAsUser reu = new RunExeAsUser();
            //reu.ShowDialog();

            //if (reu.dialogresult == true)
            //{
            //    Explorer.RunExeAsAnotherUser(Explorer.SelectedItems[0].ParsingName, reu.textBox1.Text);
            //}
            WindowsAPI.RunProcesssAsUser(Explorer.SelectedItems[0].ParsingName);
        }

        #endregion

        #region Backstage - Information Tab

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            backstage.IsOpen = true;
            //CheckForUpdate();
            
            switch (autoUpdater.UpdateStepOn)
            {
                case UpdateStepOn.Checking:
                case UpdateStepOn.DownloadingUpdate:
                case UpdateStepOn.ExtractingUpdate:
                    autoUpdater.Visibility = System.Windows.Visibility.Visible;
                    autoUpdater.UpdateLayout();
                    autoUpdater.Cancel();
                    break;

                case UpdateStepOn.UpdateReadyToInstall:
                case UpdateStepOn.UpdateAvailable:
                    autoUpdater.Visibility = System.Windows.Visibility.Visible;
                    autoUpdater.UpdateLayout();
                    break;
                case UpdateStepOn.UpdateDownloaded:
                    autoUpdater.Visibility = System.Windows.Visibility.Visible;
                    autoUpdater.UpdateLayout();
                    autoUpdater.InstallNow();
                    break;

                default:

                    autoUpdater.Visibility = System.Windows.Visibility.Visible;
                    autoUpdater.UpdateLayout();
                    autoUpdater.ForceCheckForUpdate(true);
                    break;
            }
            
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            Process.Start("http://better-explorer.com/");

        }

        #endregion

        #region Path to String HelperFunctions / Other HelperFunctions

        private string RemoveExtensionsFromFile(string file, string ext)
        {
            if (file.EndsWith(ext) == true)
            {
                return file.Remove(file.LastIndexOf(ext), ext.Length);
            }
            else
            {
                return file;
            }
        }

        private string GetExtension(string file)
        {
            return file.Substring(file.LastIndexOf("."));
        }

        private string GetUseablePath(string path)
        {
            if (path.StartsWith("::"))
            {
                string lib = path.Substring(0, path.IndexOf("\\"));
                string gp = GetDefaultFolderfromLibrary(lib);
                if (gp == lib)
                {
                    return path;
                }
                else
                {
                    return gp + path.Substring(path.IndexOf("\\") + 1);
                }
            }
            else
            {
                return path;
            }
        }

        private string GetDefaultFolderfromLibrary(string library)
        {
            try
            {
                ShellLibrary lib = ShellLibrary.Load(library, true);
                return lib.DefaultSaveFolder;
            }
            catch
            {
                return library;
            }
        }

        private void OpenCommandPromptHere(string dir)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.WorkingDirectory = dir;
                //p.StartInfo.WorkingDirectory = @"c:\Program Files\";
                //p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                p.StartInfo.UseShellExecute = false; // not sure if this should be false or not

                p.Start();
            }
            catch
            {
                MessageBox.Show("Could not open command prompt in this directory: " + dir);
            }
        }

        #endregion

        #region Updater

        public void CheckForUpdate(bool ShowUpdateUI = true)
        {
            this.UpdaterWorker = new BackgroundWorker();
            this.UpdaterWorker.WorkerSupportsCancellation = true;
            this.UpdaterWorker.WorkerReportsProgress = true;
            this.UpdaterWorker.DoWork += new DoWorkEventHandler(UpdaterWorker_DoWork);

            if (!this.UpdaterWorker.IsBusy)
                this.UpdaterWorker.RunWorkerAsync(ShowUpdateUI);
            else {
                if (ShowUpdateUI)
                    MessageBox.Show("Update in progress! Please wait!");
            }
         // var informalVersion = (Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).FirstOrDefault() as AssemblyInformationalVersionAttribute).InformationalVersion;
        }

        void UpdaterWorker_DoWork(object sender, DoWorkEventArgs e) {
            Updater updater = new Updater("http://update.better-explorer.com/update.xml", 5, this.UpdateCheckType == 1);
            if (updater.LoadUpdateFile()) {
                if ((bool)e.Argument) {
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                                (Action)(() => {
                                                    UpdateWizard updateWizzard = new UpdateWizard(updater);

                                                    updateWizzard.ShowDialog(this.GetWin32Window());
                                                }));
                } else {
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                                (Action)(() => {
                                                    stiUpdate.Content = FindResource("stUpdateAvailableCP").ToString().Replace("VER", updater.AvailableUpdates[0].Version);
                                                    stiUpdate.Foreground = System.Windows.Media.Brushes.Red;
                                                }));
                }
            } else {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                                (Action)(() => {
                                                                stiUpdate.Content = FindResource("stUpdateNotAvailableCP").ToString();
                                                                stiUpdate.Foreground = System.Windows.Media.Brushes.Black;
                                if ((bool)e.Argument)
                                  MessageBox.Show(FindResource("stUpdateNotAvailableCP").ToString());
                                                }));
            }

      RegistryKey rk = Registry.CurrentUser;
      RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
      rks.SetValue(@"LastUpdateCheck", DateTime.Now.ToBinary(), RegistryValueKind.QWord);
      LastUpdateCheck = DateTime.Now;
      rks.Close();
      rk.Close();
        }

        //Code source - Codeplex User Salysle
        //Hope it works...
        private bool IsConnectedToInternet()
        {
            int lngFlags = 0;

            return WindowsAPI.InternetGetConnectedState(lngFlags, 0);
        }

        private void btnAdvancedUpdateSettings_Click(object sender, RoutedEventArgs e)
        {

        }

        void updateCheckTimer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now.Subtract(LastUpdateCheck).Days >= UpdateCheckInterval)
            {
                CheckForUpdate(false);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"CheCkForUpdates", 1);
                IsUpdateCheck = true;
                rks.Close();
                rk.Close();
                updateCheckTimer.Interval = 3600000 * 3;
                updateCheckTimer.Tick += new EventHandler(updateCheckTimer_Tick);
                updateCheckTimer.Start();

                if (DateTime.Now.Subtract(LastUpdateCheck).Days >= UpdateCheckInterval)
                {
                    CheckForUpdate(false);
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"CheckForUpdates", 0);
                IsUpdateCheck = false;
                rks.Close();
                rk.Close();
            }
        }

        private void rbCheckInterval_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            if (rbDaily.IsChecked.Value)
            {
                rks.SetValue(@"CheckInterval", 1);
                UpdateCheckInterval = 1;
            }
            else if (rbMonthly.IsChecked.Value)
            {
                rks.SetValue(@"CheckInterval", 30);
                UpdateCheckInterval = 30;
            }
            else
            {
                rks.SetValue(@"CheckInterval", 7);
                UpdateCheckInterval = 7;
            }
            rks.Close();
            rk.Close();
        }

        private void chkUpdateStartupCheck_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CheckForUpdatesStartup", chkUpdateStartupCheck.IsChecked.Value ? 1 : 0);
            IsUpdateCheckStartup = chkUpdateStartupCheck.IsChecked.Value;
            rks.Close();
            rk.Close();
        }

        private void UpdateTypeCheck_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"UpdateCheckType", rbReleases.IsChecked.Value ? 0 : 1);
            UpdateCheckType = rbReleases.IsChecked.Value ? 0 : 1;
            rks.Close();
            rk.Close();
        }

#endregion

    #region Miscellaneous Helper Functions

    public ShellObject GetShellObjectFromLocation(string Location) {
      ShellObject sho;
      if (Location.IndexOf("::") == 0 && Location.IndexOf(@"\") == -1)
        sho = ShellObject.FromParsingName("shell:" + Location);
      else
        try {
          sho = ShellObject.FromParsingName(Location);
        } catch {
          sho = (ShellObject)KnownFolders.Libraries;
        }
      if (Path.GetExtension(Location).ToLowerInvariant() == ".library-ms") {
        sho = (ShellObject)ShellLibrary.Load(Path.GetFileNameWithoutExtension(Location), true);
      }
      return sho;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(ref Win32Point pt);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Win32Point
    {
        public Int32 X;
        public Int32 Y;
    };
    public static System.Drawing.Point GetMousePosition()
    {
        Win32Point w32Mouse = new Win32Point();
        GetCursorPos(ref w32Mouse);
        return new System.Drawing.Point(w32Mouse.X, w32Mouse.Y);
    }


    List<DependencyObject> hitTestList = null;
    HitTestResultBehavior CollectAllVisuals_Callback(HitTestResult result)
    {
        if (result == null || result.VisualHit == null)
            return HitTestResultBehavior.Stop;

        hitTestList.Add(result.VisualHit);
        return HitTestResultBehavior.Continue;
    }
    public bool Activate(bool restoreIfMinimized)
    {
        if (restoreIfMinimized && WindowState == WindowState.Minimized)
        {
            WindowState = PreviouseWindowState == WindowState.Normal
                                    ? WindowState.Normal : WindowState.Maximized;
        }
        return Activate();
    }

    /// <summary>
    /// Apends the args.
    /// </summary>
    /// <param name="args">The args.</param>
    public void ApendArgs(string[] args)
    {
        //if (args == null) return;
        //Application.Current.Properties["cmd2"] = args[0];
    }

#endregion

    public MainWindow() {

      TaskbarManager.Instance.ApplicationId = "{A8795DFC-A37C-41E1-BC3D-6BBF118E64AD}";

            CommandBinding cbnewtab = new CommandBinding(AppCommands.RoutedNewTab, ERNewTab);
            this.CommandBindings.Add(cbnewtab);
            CommandBinding cbGotoCombo = new CommandBinding(AppCommands.RoutedEnterInBreadCrumbCombo, ERGoToBCCombo);
            this.CommandBindings.Add(cbGotoCombo);
            CommandBinding cbCloseTab = new CommandBinding(AppCommands.RoutedCloseTab, RCloseTab);
            this.CommandBindings.Add(cbCloseTab);
            CommandBinding cbChangeTab= new CommandBinding(AppCommands.RoutedChangeTab, ChangeTab);
            this.CommandBindings.Add(cbChangeTab);
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);

      ExplorerBrowser.SetCustomDialogs(false);//Convert.ToInt32(rks.GetValue(@"IsCustomFO", 0)) == 1);
            ExplorerBrowser.IsOldSysListView = Convert.ToInt32(rks.GetValue(@"IsVistaStyleListView", 1)) == 1;

            // loads current Ribbon color theme
            try
            {
                switch (Convert.ToString(rks.GetValue(@"CurrentTheme", "Blue")))
                {
                    case "Blue":
                        ChangeRibbonTheme("Blue");
                        break;
                    case "Silver":
                        ChangeRibbonTheme("Silver");
                        break;
                    case "Black":
                        ChangeRibbonTheme("Black");
                        break;
                    case "Green":
                        ChangeRibbonTheme("Green");
                        break;
                    default:
                        ChangeRibbonTheme("Blue");
                        break;
                }
        
            }
            catch (Exception ex)
            {
        MessageBox.Show(String.Format("An error occurred while trying to load the theme data from the Registry. \n\r \n\r{0}\n\r \n\rPlease let us know of this issue at http://bugtracker.better-explorer.com/", ex.Message), "RibbonTheme Error - " + ex.ToString());
            }

            // loads current UI language (uses en-US if default)
            try
            {

                string loc;
                if (Convert.ToString(rks.GetValue(@"Locale", ":null:")) == ":null:")
                {
                    // creates value in Registry if value does not exist
                    rks.SetValue(@"Locale", "en-US");
                    loc = "en-US";
                }
                else
                {
                    loc = Convert.ToString(rks.GetValue(@"Locale", ":null:"));
                }

                SelectCulture(loc, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BExplorer\\translation.xaml");

            }
            catch (Exception ex)
            {
        MessageBox.Show(String.Format("An error occurred while trying to load the locale data from the Registry. \n\r \n\r{0}\n\r \n\rPlease let us know of this issue at http://bugtracker.better-explorer.com/", ex.Message), "Locale Load Error - " + ex);
            }

            // gets values from registry to be applied after initialization
            string lohc = Convert.ToString(rks.GetValue(@"Locale", ":null:"));
            double sbw = Convert.ToDouble(rks.GetValue(@"SearchBarWidth", "220"));

            string rtlused = Convert.ToString(rks.GetValue(@"RTLMode", "notset"));

            string tabba = Convert.ToString(rks.GetValue(@"TabBarAlignment", "top"));

            rks.Close();
            rk.Close();


            //Main Initialization routine
      InitializeComponent();

            isOnLoad = true;
            //chkOldSysListView.IsChecked = ExplorerBrowser.IsOldSysListView;
            isOnLoad = false;

            // sets up ComboBox to select the current UI language
            foreach (TranslationComboBoxItem item in this.TranslationComboBox.Items)
            {
                if (item.LocaleCode == lohc)
                {
                    this.TranslationComboBox.SelectedItem = item;
                }
            }

            bool rtlset = false;

            if (rtlused != "notset")
            {
                rtlset = true;
            }
            else
            {
                if ((this.TranslationComboBox.SelectedItem as TranslationComboBoxItem).UsesRTL == true)
                {
                    rtlused = "true";
                }
                else
                {
                    rtlused = "false";
                }
            }

            // sets size of search bar
            this.SearchBarColumn.Width = new GridLength(sbw);

            // store 1st value
            PreviouseWindowState = WindowState;

            // attach to event (used to store prev. win. state)
            LayoutUpdated += Window_LayoutUpdated;

            ShellVView.Child = Explorer;

            // prepares RTL mode
            if (rtlused == "true")
            {
                FlowDirection = System.Windows.FlowDirection.RightToLeft;
            }
            else
            {
                FlowDirection = System.Windows.FlowDirection.LeftToRight;
            }

            if (rtlset == true)
            {
                rtlused = "notset";
            }

            // sets tab bar alignment
            if (tabba == "top")
            {
                TabbaTop.IsChecked = true;
            }
            else if (tabba == "bottom")
            {
                TabbaBottom.IsChecked = true;
            }
            else
            {
                TabbaTop.IsChecked = true;
            }

            // allows user to change language
            ReadyToChangeLanguage = true;
      Task.Run(() =>
          {
            LoadInternalList();
          });
        }

    MessageReceiver r;

    private void SetUpFavoritesMenu()
    {
      Dispatcher.BeginInvoke(DispatcherPriority.Render, (ThreadStart)(() =>
      {
        try
        {
          btnFavorites.Visibility = System.Windows.Visibility.Visible;
          foreach (var item in ((ShellContainer)KnownFolders.Links).Where(w => !w.IsHidden))
          {
            try
            {
              MenuItem mi = new MenuItem();
              mi.Header = item.GetDisplayName(DisplayNameType.Default);
              mi.Tag = item;
              item.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
              item.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
              ImageSource icon = item.Thumbnail.BitmapSource;
              mi.Focusable = false;
              mi.Icon = icon;
              mi.Click += new RoutedEventHandler(mif_Click);
              btnFavorites.Items.Add(mi);
            }
            catch (Exception)
            {
            }
          }
        }
        catch (Exception)
        {

          btnFavorites.Visibility = System.Windows.Visibility.Collapsed;
        }
      }));
    }
    private void InitializeExplorerControl()
    {
      Explorer.LVItemsColorCodes = this.LVItemsColor;
      Explorer.SelectionChanged += ExplorerBrowserControl_SelectionChanged;
      Explorer.NavigationComplete += Explorer_NavigationComplete;
      Explorer.MouseWheel += Explorer_MouseWheel;
      Explorer.ViewEnumerationComplete += Explorer_ViewEnumerationComplete;
      Explorer.NavigationPending += Explorer_NavigationPending;
      Explorer.GotFocus += Explorer_GotFocus;
      Explorer.ExplorerGotFocus += Explorer_ExplorerGotFocus;
      Explorer.RenameFinished += Explorer_RenameFinished;
      Explorer.KeyUP += Explorer_KeyUP;
      Explorer.LostFocus += Explorer_LostFocus;
      Explorer.NavigationOptions.PaneVisibility.Commands = PaneVisibilityState.Hide;
      Explorer.NavigationOptions.PaneVisibility.CommandsOrganize = PaneVisibilityState.Hide;
      Explorer.NavigationOptions.PaneVisibility.CommandsView = PaneVisibilityState.Hide;
      Explorer.ItemsChanged += Explorer_ItemsChanged;
      Explorer.ContentOptions.FullRowSelect = true;
      Explorer.ClientSizeChanged += ExplorerBrowserControl_ClientSizeChanged;
      Explorer.Paint += ExplorerBrowserControl_Paint;
      Explorer.ViewChanged += Explorer_ViewChanged;
      Explorer.ItemHot += Explorer_ItemHot;
      Explorer.ItemMouseMiddleClick += Explorer_ItemMouseMiddleClick;
      Explorer.ExplorerBrowserMouseLeave += Explorer_ExplorerBrowserMouseLeave;
      Explorer.DragDrop += Explorer_DragDrop;

      Explorer.NavigationOptions.PaneVisibility.Preview =
        IsPreviewPaneEnabled ? PaneVisibilityState.Show : PaneVisibilityState.Hide;
      Explorer.NavigationOptions.PaneVisibility.Details =
        IsInfoPaneEnabled ? PaneVisibilityState.Show : PaneVisibilityState.Hide;

      Explorer.NavigationOptions.PaneVisibility.Navigation =
        IsNavigationPaneEnabled ? PaneVisibilityState.Show : PaneVisibilityState.Hide;

      Explorer.ContentOptions.CheckSelect = isCheckModeEnabled;
    }
    protected override void OnSourceInitialized(EventArgs e)
    {
      base.OnSourceInitialized(e);
      Handle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
    }

    private void LoadRegistryRelatedSettings()
    {
      RegistryKey rk = Registry.CurrentUser;
      RegistryKey rks = rk.CreateSubKey(@"Software\BExplorer");

      switch (Convert.ToString(rks.GetValue(@"CurrentTheme", "Blue")))
      {
        case "Blue":
          btnBlue.IsChecked = true;
          break;
        case "Silver":
          btnSilver.IsChecked = true;
          break;
        case "Black":
          btnBlack.IsChecked = true;
          break;
        case "Green":
          btnGreen.IsChecked = true;
          break;
        default:
          btnBlue.IsChecked = true;
          break;
      }

      LastUpdateCheck = DateTime.FromBinary(Convert.ToInt64(rks.GetValue(@"LastUpdateCheck", 0)));

      UpdateCheckInterval = (int)rks.GetValue(@"CheckInterval", 7);

      switch (UpdateCheckInterval)
      {
        case 1:
          rbDaily.IsChecked = true;
          break;
        case 7:
          rbWeekly.IsChecked = true;
          break;
        case 30:
          rbMonthly.IsChecked = true;
          break;
        default:
          break;
      }

      UpdateCheckType = (int)rks.GetValue(@"UpdateCheckType", 1);

      switch (UpdateCheckType)
      {
        case 0:
          rbReleases.IsChecked = true;
          break;
        case 1:
          rbReleasTest.IsChecked = true;
          break;
        default:
          break;

      }


      int HFlyoutEnabled = (int)rks.GetValue(@"HFlyoutEnabled", 0);

      int UpdateCheck = (int)rks.GetValue(@"CheckForUpdates", 1);
      IsUpdateCheck = (UpdateCheck == 1);
      chkUpdateCheck.IsChecked = IsUpdateCheck;

      int UpdateCheckStartup = (int)rks.GetValue(@"CheckForUpdatesStartup", 1);
      IsUpdateCheckStartup = (UpdateCheckStartup == 1);
      chkUpdateStartupCheck.IsChecked = IsUpdateCheckStartup;

      int isConsoleShown = (int)rks.GetValue(@"IsConsoleShown", 0);
      IsConsoleShown = (isConsoleShown == 1);
      btnConsolePane.IsChecked = this.IsConsoleShown;

      IsHFlyoutEnabled = (HFlyoutEnabled == 1);
      chkIsFlyout.IsChecked = IsHFlyoutEnabled;

      int InfoPaneEnabled = (int)rks.GetValue(@"InfoPaneEnabled", 0);

      IsInfoPaneEnabled = (InfoPaneEnabled == 1);
      btnInfoPane.IsChecked = IsInfoPaneEnabled;

      int PreviewPaneEnabled = (int)rks.GetValue(@"PreviewPaneEnabled", 0);

      IsPreviewPaneEnabled = (PreviewPaneEnabled == 1);
      btnPreviewPane.IsChecked = IsPreviewPaneEnabled;

      int NavigationPaneEnabled = (int)rks.GetValue(@"NavigationPaneEnabled", 1);

      IsNavigationPaneEnabled = (NavigationPaneEnabled == 1);
      btnNavigationPane.IsChecked = IsNavigationPaneEnabled;

      int CheckBoxesEnabled = (int)rks.GetValue(@"CheckModeEnabled", 0);

      isCheckModeEnabled = (CheckBoxesEnabled == 1);
      chkShowCheckBoxes.IsChecked = isCheckModeEnabled;

      int ExFileOpEnabled = (int)rks.GetValue(@"FileOpExEnabled", 0);

      IsExtendedFileOpEnabled = (ExFileOpEnabled == 1);
      Explorer.IsExFileOpEnabled = IsExtendedFileOpEnabled;
      chkIsTerraCopyEnabled.IsChecked = IsExtendedFileOpEnabled;

      int cfoEnabled = (int)rks.GetValue(@"IsCustomFO", 0);

      chkIsCFO.IsChecked = cfoEnabled == 1;

      int CompartibleRename = (int)rks.GetValue(@"CompartibleRename", 1);

      IsCompartibleRename = (CompartibleRename == 1);

      //chkIsCompartibleRename.IsChecked = IsCompartibleRename;

      int RestoreTabs = (int)rks.GetValue(@"IsRestoreTabs", 1);

      IsrestoreTabs = (RestoreTabs == 1);

      chkIsRestoreTabs.IsChecked = IsrestoreTabs;

      //if this instance has the /norestore switch, do not load tabs from previous session, even if it is set in the Registry
      if (App.isStartWithStartupTab == true)
      {
        IsrestoreTabs = false;
      }

      int IsLastTabCloseApp = (int)rks.GetValue(@"IsLastTabCloseApp", 1);
      IsCloseLastTabCloseApp = (IsLastTabCloseApp == 1);
      this.chkIsLastTabCloseApp.IsChecked = IsCloseLastTabCloseApp;

      int LogActions = (int)rks.GetValue(@"EnableActionLog", 0);

      canlogactions = (LogActions == 1);
      chkLogHistory.IsChecked = canlogactions;
      if (LogActions == 1)
      {
        chkLogHistory.Visibility = System.Windows.Visibility.Visible;
        ShowLogsBorder.Visibility = System.Windows.Visibility.Visible;
        paddinglbl8.Visibility = System.Windows.Visibility.Visible;
      }

      // load settings for auto-switch to contextual tab
      asFolder = ((int)rks.GetValue(@"AutoSwitchFolderTools", 0) == 1);
      asArchive = ((int)rks.GetValue(@"AutoSwitchArchiveTools", 1) == 1);
      asImage = ((int)rks.GetValue(@"AutoSwitchImageTools", 1) == 1);
      asApplication = ((int)rks.GetValue(@"AutoSwitchApplicationTools", 0) == 1);
      asLibrary = ((int)rks.GetValue(@"AutoSwitchLibraryTools", 1) == 1);
      asDrive = ((int)rks.GetValue(@"AutoSwitchDriveTools", 1) == 1);
      asVirtualDrive = ((int)rks.GetValue(@"AutoSwitchVirtualDriveTools", 0) == 1);


      chkFolder.IsChecked = asFolder;
      chkArchive.IsChecked = asArchive;
      chkImage.IsChecked = asImage;
      chkApp.IsChecked = asApplication;
      chkLibrary.IsChecked = asLibrary;
      chkDrive.IsChecked = asDrive;
      chkVirtualTools.IsChecked = asVirtualDrive;

      // load OverwriteOnImages setting (default is false)
      int oor = (int)rks.GetValue(@"OverwriteImageWhileEditing", 0);
      OverwriteOnRotate = (oor == 1);
      chkOverwriteImages.IsChecked = (oor == 1);

      // load Saved Tabs Directory location (if different from default)
      string tdir = Convert.ToString(rks.GetValue(@"SavedTabsDirectory", satdir));
      txtDefSaveTabs.Text = tdir;
      sstdir = tdir;

      StartUpLocation = rks.GetValue(@"StartUpLoc", KnownFolders.Libraries.ParsingName).ToString();

      if (StartUpLocation == "")
      {
        rks.SetValue(@"StartUpLoc", KnownFolders.Libraries.ParsingName);
        StartUpLocation = KnownFolders.Libraries.ParsingName;
      }
      char[] delimiters = new char[] { ';' };
      string LastOpenedTabs = rks.GetValue(@"OpenedTabs", "").ToString();
      InitialTabs = LastOpenedTabs.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

      try
      {
        RegistryKey rkbe = Registry.ClassesRoot;
        RegistryKey rksbe = rkbe.OpenSubKey(@"Folder\shell\open\command", RegistryKeyPermissionCheck.ReadSubTree);
        bool IsThereDefault = rksbe.GetValue("DelegateExecute", "-1").ToString() == "-1";
        chkIsDefault.IsChecked = IsThereDefault;
        chkIsDefault.IsEnabled = true;
        rksbe.Close();
        rkbe.Close();
      }
      catch (Exception)
      {
        chkIsDefault.IsChecked = false;
        chkIsDefault.IsEnabled = false;
      }

      RegistryKey rkfe = Registry.CurrentUser;
      RegistryKey rksfe = rk.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", RegistryKeyPermissionCheck.ReadSubTree);
      chkTreeExpand.IsChecked = (int)rksfe.GetValue("NavPaneExpandToCurrentFolder", 0) == 1;
      rksfe.Close();
      rkfe.Close();

      rks.Close();
      rk.Close();

      if (StartUpLocation.IndexOf("::") == 0 && StartUpLocation.IndexOf(@"\") == -1)
      {
        btnSetCurrentasStartup.Header =
        ShellObject.FromParsingName("Shell:" + StartUpLocation).GetDisplayName(DisplayNameType.Default);
        btnSetCurrentasStartup.Icon = ShellObject.FromParsingName("Shell:" + StartUpLocation).Thumbnail.BitmapSource;
      }
      else
      {
        try
        {
          btnSetCurrentasStartup.Header =
            ShellObject.FromParsingName(StartUpLocation).GetDisplayName(DisplayNameType.Default);

          btnSetCurrentasStartup.Icon = ShellObject.FromParsingName(StartUpLocation).Thumbnail.BitmapSource;
        }
        catch
        {


        }
      }

      miTabManager.IsEnabled = Directory.Exists(sstdir);

      autoUpdater.DaysBetweenChecks = this.UpdateCheckInterval;
      try
      {
        if (IsUpdateCheck)
        {
          autoUpdater.UpdateType = UpdateType.OnlyCheck;
        }
        else
        {
          autoUpdater.UpdateType = UpdateType.DoNothing;
        }
        if (IsUpdateCheckStartup)
        {
          autoUpdater.ForceCheckForUpdate();
        }
      }
      catch (IOException)
      {
        this.stiUpdate.Content = "Switch to another BetterExplorer window or restart to check for updates.";
        this.btnUpdateCheck.IsEnabled = false;
      }

      if (App.isStartMinimized)
      {
        this.Visibility = System.Windows.Visibility.Hidden;
        this.WindowState = System.Windows.WindowState.Minimized;
        //this.ShowInTaskbar = false;
      }

      if (!TheRibbon.IsMinimized)
      {
        TheRibbon.SelectedTabItem = HomeTab;
        this.TheRibbon.SelectedTabIndex = 0;
      }
    }
    private void InitializeInitialTabs()
    {
      if (InitialTabs.Length == 0 || !IsrestoreTabs)
      {
        ShellObject sho = GetShellObjectFromLocation(StartUpLocation);
        if (tabControl1.Items.OfType<ClosableTabItem>().Count() == 0)
          NewTab(sho, true);
        else
          Explorer.Navigate(sho);
      }
      if (IsrestoreTabs)
      {
        isOnLoad = true;
        int i = 0;
        foreach (string str in InitialTabs)
        {
          try
          {
            i++;
            if (i == InitialTabs.Length)
            {
              NewTab(str, true);
            }
            else
            {
              NewTab(str, false);
            }
            if (i == InitialTabs.Count())
            {
              ShellObject sho = GetShellObjectFromLocation(str);
              Explorer.Navigate(sho);
              (tabControl1.SelectedItem as ClosableTabItem).Path = Explorer.NavigationLog.CurrentLocation;
              (tabControl1.SelectedItem as ClosableTabItem).ToolTip = Explorer.NavigationLog.CurrentLocation.ParsingName;
            }
          }
          catch
          {
            //AddToLog(String.Format("Unable to load {0} into a tab!", str));
            MessageBox.Show("BetterExplorer is unable to load one of the tabs from your last session. Your other tabs are perfectly okay though! \r\n\r\nThis location was unable to be loaded: " + str, "Unable to Create New Tab", MessageBoxButton.OK, MessageBoxImage.Error);
          }

        }
        if (tabControl1.Items.Count == 0)
        {
          NewTab();

          if (StartUpLocation.IndexOf("::") == 0)
          {

            Explorer.Navigate(ShellObject.FromParsingName("shell:" + StartUpLocation));
          }
          else
            Explorer.Navigate(ShellObject.FromParsingName(StartUpLocation.Replace("\"", "")));

          (tabControl1.SelectedItem as ClosableTabItem).Path = Explorer.NavigationLog.CurrentLocation;
          (tabControl1.SelectedItem as ClosableTabItem).ToolTip = Explorer.NavigationLog.CurrentLocation.ParsingName;
        }
        isOnLoad = false;

      }
      breadcrumbBarControl1.ExitEditMode();
      Explorer.SetExplorerFocus();

      if (tabControl1.Items.Count == 1)
        tabControl1.SelectedIndex = 0;
    }
    private void SetsUpJumpList()
    {
      //sets up Jump List
      try
      {
        AppJL.ShowRecentCategory = true;
        AppJL.ShowFrequentCategory = true;
        System.Windows.Shell.JumpList.SetJumpList(Application.Current, AppJL);
        JumpTask newTab = new JumpTask()
        {
          ApplicationPath = Process.GetCurrentProcess().MainModule.FileName,
          Arguments = "t",
          Title = "Open Tab",
          Description = "Opens new tab with default location"
        };

        JumpTask newWindow = new JumpTask()
        {
          ApplicationPath = Process.GetCurrentProcess().MainModule.FileName,
          Arguments = "/nw",
          Title = "New Window",
          Description = "Creates a new window with default location"
        };

        AppJL.JumpItems.Add(newTab);
        AppJL.JumpItems.Add(newWindow);
        AppJL.Apply();
      }
      catch
      {

      }
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
        {
      Task.Run(() =>
        {
          UpdateRecycleBinInfos();
        });

     
      r = new MessageReceiver();
      r.OnMessageReceived += r_OnMessageReceived;
      r.Show();

            bool exitApp = false;

            try
            {
        Task.Run(() =>
        {
          if (WindowsAPI.getOSInfo() == WindowsAPI.OsVersionInfo.Windows8)
          {
            WindowsAPI.SHELLSTATE state = new WindowsAPI.SHELLSTATE();
            WindowsAPI.SHGetSetSettings(ref state, WindowsAPI.SSF.SSF_SHOWSTATUSBAR, false);
            if (state.fShowStatusBar == 1)
            {
              WindowsAPI.SHELLSTATE newState = new WindowsAPI.SHELLSTATE();
              newState.fShowStatusBar = 0;
              WindowsAPI.SHGetSetSettings(ref newState, WindowsAPI.SSF.SSF_SHOWSTATUSBAR, true);
            }
          }
        });

                

                //Sets up FileSystemWatcher for Favorites folder
                try
                {
                    FileSystemWatcher fsw = new FileSystemWatcher(KnownFolders.Links.ParsingName);
          fsw.Created += fsw_Created;
          fsw.Deleted += fsw_Deleted;
          fsw.Renamed += fsw_Renamed;
                    fsw.EnableRaisingEvents = true;
                }
                catch
                {
                }

                //Set up breadcrumb bar drag/drop functionality
                breadcrumbBarControl1.SetDragHandlers(new DragEventHandler(bbi_DragEnter), new DragEventHandler(bbi_DragLeave), new DragEventHandler(bbi_DragOver), new DragEventHandler(bbi_Drop));

        //Set up Favorites Menu
        Task.Run(() =>
        {
          SetUpFavoritesMenu();
        });

        //Set up ListView Color codes 
        Explorer.LVItemsColorCodes = this.LVItemsColor;


        //Load the ShellSettings
        IsCalledFromLoading = true;
                WindowsAPI.SHELLSTATE statef = new WindowsAPI.SHELLSTATE();
        WindowsAPI.SHGetSetSettings(ref statef, WindowsAPI.SSF.SSF_SHOWALLOBJECTS | WindowsAPI.SSF.SSF_SHOWEXTENSIONS, false);
        chkHiddenFiles.IsChecked = (statef.fShowAllObjects == 1);
        chkExtensions.IsChecked = (statef.fShowExtensions == 1);
                IsCalledFromLoading = false;

                isOnLoad = true;

                //'load from Registry
        LoadRegistryRelatedSettings();

        //'set up Explorer control
        InitializeExplorerControl();

                // set up history on breadcrumb bar (currently missing try-catch statement in order to catch error)
                try
                {
          Task.Run(() =>
          {
            breadcrumbBarControl1.ClearHistory();
            breadcrumbBarControl1.HistoryItems = ReadHistoryFromFile(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\history.txt");
          });
                }
                catch (FileNotFoundException)
                {
                    logger.Warn(String.Format("History file not found at location:{0}\\history.txt", Environment.SpecialFolder.LocalApplicationData));
                }

                AddToLog("Session Began");

                isOnLoad = false;
                         
                //'set StartUp location
        if (Application.Current.Properties["cmd"] != null && Application.Current.Properties["cmd"].ToString() != "-minimized")
                {

                    String cmd = Application.Current.Properties["cmd"].ToString();

                    if (cmd.IndexOf("::") == 0)
                    {

                        Explorer.Navigate(ShellObject.FromParsingName("shell:" + cmd));
                    }
                    else
                        Explorer.Navigate(ShellObject.FromParsingName(cmd.Replace("\"", "")));


          NewTab(Explorer.NavigationLog.CurrentLocation,true);
                    
                }
                else
                {
            InitializeInitialTabs();
                }

        SetsUpJumpList();
      
                //Setup Clipboard monitor
                cbm.ClipboardChanged += new EventHandler<ClipboardChangedEventArgs>(cbm_ClipboardChanged);
                    

                if (exitApp)
                {
                    Application.Current.Shutdown();
                    return;
                }

        if (this.IsConsoleShown) {
          ctrlConsole.StartProcess("cmd.exe", null);
          ctrlConsole.InternalRichTextBox.TextChanged += new EventHandler(InternalRichTextBox_TextChanged);
          ctrlConsole.ClearOutput(); 
        }

        //Set up Version Info
                verNumber.Content = "Version " + (Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).FirstOrDefault() as AssemblyInformationalVersionAttribute).InformationalVersion;
                lblArchitecture.Content = WindowsAPI.Is64bitProcess(Process.GetCurrentProcess()) ? "64-bit version" : "32-bit version";

        this.Activate(true);
            }
            catch (Exception exe)
            {
                MessageBox.Show(String.Format("An error occurred while loading the window. Please report this issue at http://bugtracker.better-explorer.com/. \r\n\r\n Here is some information about the error: \r\n\r\n{0}\r\n\r\n{1}", exe.Message, exe), "Error While Loading", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

    void Explorer_MouseWheel(object sender, ExplorerMoiseWheelArgs e)
    {
      if (e.IsOutsideExplorer)
      {
        e.Handled = true;
        var scroll = (ScrollViewer)tabControl1.Template.FindName("svTabBar", tabControl1);
        scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset - e.Delta);
      }
    }

    void r_OnMessageReceived(object sender, EventArgs e)
    {
      Thread t = new Thread(() =>
      {
        Thread.Sleep(1000);
        UpdateRecycleBinInfos();
      });

      t.Start();
    }


    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
    private const int WM_VSCROLL = 277;
    private const int SB_PAGEBOTTOM = 7;

        #region Old Search Code
        private ShellObject BeforeSearchFolder;
        Thread backgroundSearchThread;
        // Helper method to do the search on a background thread
        internal void DoSimpleSearch(object arg)
        {
            SearchCondition searchCondition = SearchConditionFactory.ParseStructuredQuery(arg.ToString());
            ShellSearchFolder searchFolder =
                new ShellSearchFolder(searchCondition, (ShellContainer)BeforeSearchFolder);


            //Dispatcher.Invoke(
            //		System.Windows.Threading.DispatcherPriority.Normal,
            //		new Action(
            //		delegate()
            //		{
                        Explorer.Navigate(searchFolder);

                //	}
                //));

            if (BeforeSearchFolder != null)
            {
                //Thread.Sleep(750);
                //Explorer.RefreshExplorer();
            }


        }


        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Explorer.Navigate((ShellSearchFolder)e.Argument);
        }

        public void DoSearch(string SearchCriteria)
        {
            if (backgroundSearchThread != null)
                backgroundSearchThread.Abort();


            if (Explorer.NavigationLog.CurrentLocation.IsSearchFolder)
            {
                if (BeforeSearchFolder == null)
                {
                    BeforeSearchFolder = Explorer.NavigationLog.CurrentLocation;
                }
            }
            else
            {
                BeforeSearchFolder = Explorer.NavigationLog.CurrentLocation;
            }

            if (SearchCriteria != "")
            {
        DoSimpleSearch(SearchCriteria);
                //backgroundSearchThread = new Thread(new ParameterizedThreadStart(DoSimpleSearch));
                //backgroundSearchThread.IsBackground = true;
                //// ApartmentState.STA is required for COM
                //backgroundSearchThread.SetApartmentState(ApartmentState.STA);
                //backgroundSearchThread.Start(SearchCriteria);

            }
        }
        private void searchTextBox1_Search(object sender, RoutedEventArgs e)
        {

            //DoSearch(searchTextBox1.Text);
            //StatusBar.UpdateLayout();

        }
        int searchcicles = 0;
        //int BeforeSearcCicles = 0;
        int CurrentProgressValue = 0;
        #endregion

        #region Change Ribbon Color (Theme)

        public void ChangeRibbonThemeL(string ThemeName)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, (ThreadStart)(() =>
            {

                Resources.BeginInit();
        Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(String.Format("pack://application:,,,/Fluent;component/Themes/Office2010/{0}.xaml", ThemeName)) });
                Resources.MergedDictionaries.RemoveAt(0);
                Resources.EndInit();
            }));
        }

        private void btnSilver_Click(object sender, RoutedEventArgs e)
        {
            ChangeRibbonTheme("Silver");
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CurrentTheme", "Silver");
            rks.Close();
            KeepBackstageOpen = true;
        }

    public void ChangeRibbonTheme(string ThemeName, bool IsMetro = false)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, (ThreadStart)(() =>
            {
                Application.Current.Resources.BeginInit();
                Application.Current.Resources.MergedDictionaries.RemoveAt(1);
        if (IsMetro) {
          Application.Current.Resources.MergedDictionaries.Insert(1, new ResourceDictionary() { Source = new Uri(String.Format("pack://application:,,,/Fluent;component/Themes/Metro/{0}.xaml", "White")) });
        } else {
          Application.Current.Resources.MergedDictionaries.Insert(1, new ResourceDictionary() { Source = new Uri(String.Format("pack://application:,,,/Fluent;component/Themes/Office2010/{0}.xaml", ThemeName)) });
        }
                Application.Current.Resources.EndInit();
            }));
        }

        private void btnBlue_Click(object sender, RoutedEventArgs e)
        {
            ChangeRibbonTheme("Blue");
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CurrentTheme", "Blue");
            rks.Close();
            KeepBackstageOpen = true;
        }

        private void btnBlack_Click(object sender, RoutedEventArgs e)
        {
            ChangeRibbonTheme("Black");
            btnBlack.IsChecked = true;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CurrentTheme", "Black");
            rks.Close();
            KeepBackstageOpen = true;
        }

        private void btnGreen_Click(object sender, RoutedEventArgs e)
        {
            ChangeRibbonThemeL("Green");
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CurrentTheme", "Green");
            rks.Close();
            KeepBackstageOpen = true;
        }

        #endregion

        #region Archive Commands

        //private void CreateArchive_Show(OutArchiveFormat format) 
        //{
        //    var selectedItems = new List<string>();
        //    foreach(ShellObject item in Explorer.SelectedItems)
        //    {
        //        if (Directory.Exists(item.ParsingName))
        //        {
        //            DirectoryInfo di = new DirectoryInfo(item.ParsingName);
        //            FileInfo[] Files = di.GetFiles("*", SearchOption.AllDirectories);
        //            foreach (FileInfo fi in Files)
        //            {
        //                selectedItems.Add(fi.FullName);
        //            }

        //        }
        //        else
        //        {
        //            selectedItems.Add(item.ParsingName);
        //        }

        //    }
        //    if (selectedItems.Count > 0)
        //    {
        //        try
        //        {
        //            var CAI = new CreateArchive(selectedItems,
        //                                        true,
        //                                        Path.GetDirectoryName(selectedItems[0]),
        //                                        format);

        //            CAI.Show(this.GetWin32Window());


        //        }
        //        catch (Exception exception)
        //        {
        //            var dialog = new TaskDialog();
        //            dialog.StandardButtons = TaskDialogStandardButtons.Ok;
        //            dialog.Text = exception.Message;
        //            dialog.Show();
        //        }
        //    }
        // }

        private void ExtractFiles()
        {
            var selectedItems = new List<string>();
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                selectedItems.Add(item.ParsingName);
            }
            try
            {
                var CAI = new CreateArchive(selectedItems,
                                            false,
                                            Explorer.SelectedItems[0].ParsingName);

                CAI.Show(this.GetWin32Window());


            }
            catch (Exception exception)
            {
                var dialog = new TaskDialog();
                dialog.StandardButtons = TaskDialogStandardButtons.Ok;
                dialog.Text = exception.Message;
                dialog.Show();
            }
        }

        private void btnExtractNow_Click(object sender, RoutedEventArgs e)
        {
            if (chkUseNewFolder.IsChecked == true)
            {
        string OutputLoc = String.Format("{0}\\{1}", txtExtractLocation.Text, RemoveExtensionsFromFile(Explorer.SelectedItems[0].Name, new FileInfo(Explorer.SelectedItems[0].ParsingName).Extension));
                try
                {
                    Directory.CreateDirectory(OutputLoc);
                    ExtractToLocation(SelectedArchive, OutputLoc);
                }
                catch (Exception)
                {
          MessageBoxResult wtd = MessageBox.Show(String.Format("The directory {0} already exists. Would you like for BetterExplorer to extract there instead?", OutputLoc), "Folder Exists", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                    switch (wtd)
                    {
                        case MessageBoxResult.Cancel:
                            break;
                        case MessageBoxResult.No:
                            break;
                        case MessageBoxResult.None:
                            break;
                        case MessageBoxResult.OK:
                            break;
                        case MessageBoxResult.Yes:
                            ExtractToLocation(SelectedArchive, OutputLoc);
                            break;
                        default:
                            break;
                    }
                }

            }
            else
            {
                ExtractToLocation(SelectedArchive, txtExtractLocation.Text);
            }

        }

        private void btnChooseLocation_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtExtractLocation.Text = dlg.FileName;
            }
        }

        private void ExtractToLocation(string archive, string output)
        {
            var selectedItems = new List<string>() { archive };

            var archiveProcressScreen = new ArchiveProcressScreen(selectedItems,
                                 output,
                                 ArchiveAction.Extract,
                                 "");
            archiveProcressScreen.ExtractionCompleted += new ArchiveProcressScreen.ExtractionCompleteEventHandler(ExtractionHasCompleted);

      AddToLog(String.Format("Archive Extracted to {0} from source {1}", output, archive));

            archiveProcressScreen.Show();
        }

        private void ExtractionHasCompleted(object sender, ArchiveEventArgs e)
        {
            if (chkOpenResults.IsChecked == true)
            {
                NewTab(e.OutputLocation);
            }
        }

        //private void miCreateZIP_Click(object sender, RoutedEventArgs e)
        //{
        //    CreateArchive_Show(OutArchiveFormat.Zip);
        //}

        //private void miCreate7z_Click(object sender, RoutedEventArgs e)
        //{
        //    CreateArchive_Show(OutArchiveFormat.SevenZip);
        //}

        //private void miCreateCAB_Click(object sender, RoutedEventArgs e)
        //{
        //    CreateArchive_Show(OutArchiveFormat.Tar);
        //}

        private void miExtractToLocation_Click(object sender, RoutedEventArgs e)
        {
            ExtractFiles();
        }

        private void miExtractHere_Click(object sender, RoutedEventArgs e)
        {
            //Dispatcher.BeginInvoke(
            //					System.Windows.Threading.DispatcherPriority.Background,
            //					new ThreadStart(
            //					delegate()
            //					{

                                    string FileName = Explorer.SelectedItems[0].ParsingName;
                                    SevenZipExtractor extractor = new SevenZipExtractor(FileName);
                                    string DirectoryName = System.IO.Path.GetDirectoryName(FileName);
                                    string ArchName = System.IO.Path.GetFileNameWithoutExtension(FileName);
                                    extractor.Extracting += new EventHandler<ProgressEventArgs>(extractor_Extracting);
                                    extractor.ExtractionFinished += new EventHandler<EventArgs>(extractor_ExtractionFinished);
                                    extractor.FileExtractionStarted += new EventHandler<FileInfoEventArgs>(extractor_FileExtractionStarted);
                                    extractor.FileExtractionFinished += new EventHandler<FileInfoEventArgs>(extractor_FileExtractionFinished);
                                    extractor.PreserveDirectoryStructure = true;
                                    string Separator = "";
                                    if (DirectoryName[DirectoryName.Length - 1] != Char.Parse(@"\"))
                                    {
                                        Separator = @"\";
                                    }
                  AddToLog(String.Format("Extracted Archive to {0}{1}{2} from source {3}", DirectoryName, Separator, ArchName, FileName));
                                    extractor.BeginExtractArchive(DirectoryName + Separator + ArchName);
                            //	}));
        }

        void extractor_FileExtractionFinished(object sender, FileInfoEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void extractor_FileExtractionStarted(object sender, FileInfoEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void extractor_ExtractionFinished(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            if ((sender as SevenZipExtractor) != null)
            {
                (sender as SevenZipExtractor).Dispose();
            }

        }

        void extractor_Extracting(object sender, ProgressEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void btnExtract_Click(object sender, RoutedEventArgs e)
        {
            miExtractHere_Click(sender, e);
        }

        private void btnCheckArchive_Click(object sender, RoutedEventArgs e)
        {
            Thread trIntegrityCheck = new Thread(new ThreadStart(DoCheck));
            trIntegrityCheck.Start();
        }


        private void DoCheck()
        {

            string FileName = Explorer.SelectedItems[0].ParsingName;
            SevenZipExtractor extractor = new SevenZipExtractor(FileName);
            if (!extractor.Check())
                MessageBox.Show("Not Pass");
            else
                MessageBox.Show("Pass");

            extractor.Dispose();
        }

        private void btnViewArchive_Click(object sender, RoutedEventArgs e)
        {
            var name = Explorer.SelectedItems.First().ParsingName;
            var archiveDetailView = new ArchiveDetailView(ICON_DLLPATH, name);
            archiveDetailView.Show(this.GetWin32Window());

            //ArchiveViewWindow g = new ArchiveViewWindow(Explorer.SelectedItems[0], IsPreviewPaneEnabled, IsInfoPaneEnabled);
            //g.Show();
        }

        private void btnCreateArchive_Click(object sender, RoutedEventArgs e)
        {
            ArchiveCreateWizard acw = new ArchiveCreateWizard(Explorer.SelectedItems, Explorer.NavigationLog.CurrentLocation.ParsingName);
            acw.win = this;
            acw.LoadStrings();
            acw.Show();
            AddToLog("Archive Creation Wizard begun. Current location: " + Explorer.NavigationLog.CurrentLocation.ParsingName);
        }

        #endregion

        #region Library Commands

        private void btnOLItem_Click(object sender, RoutedEventArgs e)
        {

            ShellLibrary lib = null;
            if (Explorer.GetSelectedItemsCount() == 1)
            {
                lib = ShellLibrary.Load(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default), false);
            }
            else
            {
                lib = ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
            }
            switch ((sender as MenuItem).Tag.ToString())
            {
                case "gu":
                    lib.LibraryType = LibraryFolderType.Generic;
                    lib.Close();
                    break;
                case "doc":
                    lib.LibraryType = LibraryFolderType.Documents;
                    lib.Close();
                    break;
                case "pic":
                    lib.LibraryType = LibraryFolderType.Pictures;
                    lib.Close();
                    break;
                case "vid":
                    lib.LibraryType = LibraryFolderType.Videos;
                    lib.Close();
                    break;
                case "mu":
                    lib.LibraryType = LibraryFolderType.Music;
                    lib.Close();
                    break;
                default:
                    break;
            }
        }

        private void chkPinNav_Checked(object sender, RoutedEventArgs e)
        {
            ShellLibrary lib = null;
            if (Explorer.GetSelectedItemsCount() == 1)
            {
                lib = ShellLibrary.Load(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default), false);
            }
            else
            {
                lib = ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
            }
            if (!IsFromSelectionOrNavigation)
            {
                lib.IsPinnedToNavigationPane = true;
            }

            lib.Close();
        }

        private void chkPinNav_Unchecked(object sender, RoutedEventArgs e)
        {
            ShellLibrary lib = null;
            if (Explorer.GetSelectedItemsCount() == 1)
            {
                lib = ShellLibrary.Load(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default), false);
            }
            else
            {
                lib = ShellLibrary.Load(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default), false);
            }
            if (!IsFromSelectionOrNavigation)
            {
                lib.IsPinnedToNavigationPane = false;
            }

            lib.Close();
        }

        private void btnChangeLibIcon_Click(object sender, RoutedEventArgs e)
        {
            IconView iv = new IconView();

            iv.LoadIcons(Explorer, true);
        }

        private void btnManageLib_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShellLibrary.ShowManageLibraryUI(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default),
                    this.Handle, "Choose which folders will be in this library", "A library gathers content from all of the folders listed below and puts it all in one window for you to see.", true);
            }
            catch
            {

                ShellLibrary.ShowManageLibraryUI(Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default),
                    this.Handle, "Choose which folders will be in this library", "A library gathers content from all of the folders listed below and puts it all in one window for you to see.", true);
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            TaskDialog td = new TaskDialog();
            td.Caption = "Reset Library";
            td.Icon = TaskDialogStandardIcon.Warning;
            td.Text = "Click \"OK\" to reset currently selected library properties";
            td.InstructionText = "Reset Library Properties?";
            td.FooterIcon = TaskDialogStandardIcon.Information;
            //td.FooterText = "This will reset all the properties to default properties for library type";
            td.DetailsCollapsedLabel = "More info";
            td.DetailsExpandedLabel = "More Info";
            td.DetailsExpandedText = "This will reset all the properties to default properties for library type";
            td.DetailsExpanded = false;
            td.ExpansionMode = TaskDialogExpandedDetailsLocation.ExpandFooter;
            td.StandardButtons = TaskDialogStandardButtons.Ok | TaskDialogStandardButtons.Cancel;
            td.OwnerWindowHandle = this.Handle;
            if (td.Show() == TaskDialogResult.Ok)
            {
                if (Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default).ToLowerInvariant() != "documents" &&
                    Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default).ToLowerInvariant() != "music" &&
                    Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default).ToLowerInvariant() != "videos" &&
                    Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default).ToLowerInvariant() != "pictures")
                {
                    ShellLibrary lib = ShellLibrary.Load(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default),
                                false);
                    lib.IsPinnedToNavigationPane = true;
                    lib.LibraryType = LibraryFolderType.Generic;
                    lib.IconResourceId = new IconReference(@"C:\Windows\System32\imageres.dll", 187);
                    lib.Close();
                }
            }
        }

        #endregion

        #region Navigation (Back/Forward Arrows) and Up Button

        private void leftNavBut_Click(object sender, RoutedEventArgs e)
        {
            isGoingBackOrForward = true;
            Explorer.Navigate((tabControl1.SelectedItem as ClosableTabItem).log.NavigateBack());
        }

        private void rightNavBut_Click(object sender, RoutedEventArgs e)
        {
            isGoingBackOrForward = true;
            Explorer.Navigate((tabControl1.SelectedItem as ClosableTabItem).log.NavigateForward());
        }

        private void downArrow_Click(object sender, RoutedEventArgs e)
        {
            cmHistory.Items.Clear();
            cmHistory.Opened += new RoutedEventHandler(cmHistory_Opened);
            cmHistory.Closed += new RoutedEventHandler(cmHistory_Closed);

            NavigationLog nl = (tabControl1.SelectedItem as ClosableTabItem).log;
            int i = 0;
            foreach (ShellObject item in nl.HistoryItemsList)
            {

                if (item != null)
                {
                    item.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                    item.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                    MenuItem mi = new MenuItem();
                    mi.Header = item.GetDisplayName(DisplayNameType.Default);
                    mi.Tag = item;
                    mi.Icon = item.Thumbnail.BitmapSource;
                    mi.IsCheckable = true;
                    mi.IsChecked = (i == nl.CurrentLocPos);
                    mi.GroupName = "G1";
                    mi.Click += new RoutedEventHandler(miItems_Click);
                    cmHistory.Items.Add(mi);
                }
                i++;
            }

            cmHistory.Placement = PlacementMode.Bottom;
            cmHistory.PlacementTarget = navBarGrid;
            cmHistory.IsOpen = true;
        }

        void miItems_Click(object sender, RoutedEventArgs e)
        {
            ShellObject item = (ShellObject)(sender as MenuItem).Tag;
            if (item != null)
            {
                isGoingBackOrForward = true;
                NavigationLog nl = (tabControl1.Items[tabControl1.SelectedIndex] as ClosableTabItem).log;
                (sender as MenuItem).IsChecked = true;
                nl.CurrentLocPos = cmHistory.Items.IndexOf((sender as MenuItem));
                Explorer.Navigate(item);
            }
        }

        private void btnUpLevel_Click(object sender, RoutedEventArgs e)
        {
      if (Explorer.NavigationLog.CurrentLocation.Parent != null)
        Explorer.Navigate(Explorer.NavigationLog.CurrentLocation.Parent);
      else
        btnUpLevel.IsEnabled = false;

            WindowsAPI.SetFocus(Explorer.SysListViewHandle);
            //ShellContextMenu cm = new ShellContextMenu();
            //FileInfo fi = new FileInfo(Explorer.SelectedItems[0].ParsingName);
            //FileInfo[] fii = new FileInfo[1];
            //fii[0] = fi;
            //cm.ShowContextMenu(Handle, fii, GetMousePosition());
            


        }


        #endregion

        #region View Tab/Status Bar

        private void btnMoreColls_Click(object sender, RoutedEventArgs e)
        {
            MoreColumns fMoreCollumns = new MoreColumns();
            fMoreCollumns.PopulateAvailableColumns(Explorer.AvailableColumns(true), Explorer,
                this.PointToScreen(Mouse.GetPosition(this)));
        }

        void mig_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (sender as MenuItem);
            Explorer.SetGroupCollumn(((Collumns)item.Tag).pkey, true);
        }

        private void btnAutosizeColls_Click(object sender, RoutedEventArgs e)
        {
            Explorer.SetAutoSizeColumns();
        }

        private void btnSbDetails_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Details)
            {
                btnSbDetails.IsChecked = true;
            }

            else
                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Details;
        }

        private void btnSbIcons_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Icon)
            {
                btnSbIcons.IsChecked = true;
            }
            else
                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Icon;
        }

        private void btnSbTiles_Click(object sender, RoutedEventArgs e)
        {

            if (Explorer.ContentOptions.ViewMode == ExplorerBrowserViewMode.Tile)
            {
                btnSbTiles.IsChecked = true;
            }
            else
                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Tile;

        }
    bool isCalledFromSlider = false;
        private void zoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
      
            if (!IsCalledFromLoading && !IsCalledFromViewEnum)
            {
                //Dispatcher.BeginInvoke(
                //				new Action(
                //				delegate()
                //				{
                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Thumbnail;
                Explorer.ContentOptions.ThumbnailSize = (int)e.NewValue;
                            //	}));

            }
            IsCalledFromLoading = false;
            IsCalledFromViewEnum = false;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Explorer.RefreshExplorer();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            //DoubleAnimation da = new DoubleAnimation(100, new Duration(new TimeSpan(0, 0, 2)));
            //da.FillBehavior = FillBehavior.Stop;
            //breadcrumbBar1.BeginAnimation(Odyssey.Controls.BreadcrumbBar.ProgressValueProperty, da);
            //Explorer.HideFocusRectangle();


            //For some reason, this was giving off an error.

            //ChangePaneVisibility(0x1, true);
            //Explorer.ShowPropPage(this.Handle, Explorer.SelectedItems[0].ParsingName, "Security");



            //Explorer.RefreshExplorer();


            //resets Refresh image back to normal
            //except I can't get it to work (it's probably just a small error I made, but I don't have time to fix right now.
            //BitmapImage go = new BitmapImage(new Uri("\\Images\\Refresh16.png", UriKind.Relative));
            //RefreshButtonImage.Source = go;
        }

        void mi_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (sender as MenuItem);
      var parentButton = item.Parent as DropDownButton;
      MenuItem ascitem = (MenuItem)parentButton.Items[parentButton.Items.IndexOf(misa)];
            if (ascitem.IsChecked)
            {
        Explorer.SetSortCollumn(((Collumns)item.Tag).pkey, WindowsAPI.SORT.ASCENDING);
            }
            else
            {
                Explorer.SetSortCollumn(((Collumns)item.Tag).pkey, WindowsAPI.SORT.DESCENDING);
            }

        }

        void misng_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (sender as MenuItem);
            item.IsChecked = true;
      WindowsAPI.PROPERTYKEY pk = new WindowsAPI.PROPERTYKEY();
            pk.fmtid = new Guid("00000000-0000-0000-0000-000000000000");
            pk.pid = 0;
            Explorer.SetGroupCollumn(pk, true);
        }


        private void inRibbonGallery1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
      if (IsViewSelection)
      {
        switch (ViewGallery.SelectedIndex)
        {
            case 0:
                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Icon;
                Explorer.ContentOptions.ThumbnailSize = 256;
                break;
            case 1:
                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Icon;
                Explorer.ContentOptions.ThumbnailSize = 96;
                break;
            case 2:
                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Icon;
                Explorer.ContentOptions.ThumbnailSize = 48;
                break;
            case 3:
                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.SmallIcon;
                Explorer.ContentOptions.ThumbnailSize = 16;
                break;
            case 4:

                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.List;
                break;
            case 5:

                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Details;
                break;
            case 6:

                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Tile;
                break;
            case 7:
                Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Content;
                break;
            default:
                break;
        }
      }

        }

        private void chkHiddenFiles_Checked(object sender, RoutedEventArgs e)
        {

            if (!IsCalledFromLoading)
            {
                Dispatcher.BeginInvoke(
                                new Action(
                                delegate()
                                {
                                    WindowsAPI.SHELLSTATE state = new WindowsAPI.SHELLSTATE();
                                    state.fShowAllObjects = 1;
                                    WindowsAPI.SHGetSetSettings(ref state, WindowsAPI.SSF.SSF_SHOWALLOBJECTS, true);
                                    Explorer.RefreshExplorer();
                                }
                ));
            };
        }

        private void chkHiddenFiles_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsCalledFromLoading)
            {
                Dispatcher.BeginInvoke(
                                new Action(
                                delegate()
                                {
                                    WindowsAPI.SHELLSTATE state = new WindowsAPI.SHELLSTATE();
                                    state.fShowAllObjects = 0;
                                    WindowsAPI.SHGetSetSettings(ref state, WindowsAPI.SSF.SSF_SHOWALLOBJECTS, true);
                                    Explorer.RefreshExplorer();
                                }
                ));
            }
        }

        private void chkExtensions_Checked(object sender, RoutedEventArgs e)
        {

            if (!IsCalledFromLoading)
            {
                Dispatcher.BeginInvoke(
                                new Action(
                                delegate()
                                {
                                    WindowsAPI.SHELLSTATE state = new WindowsAPI.SHELLSTATE();
                                    state.fShowExtensions = 1;
                                    WindowsAPI.SHGetSetSettings(ref state, WindowsAPI.SSF.SSF_SHOWEXTENSIONS, true);
                                    Explorer.RefreshExplorer();
                                }
                ));
            }
        }

        private void chkExtensions_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsCalledFromLoading)
            {
                Dispatcher.BeginInvoke(
                                new Action(
                                delegate()
                                {
                                    WindowsAPI.SHELLSTATE state = new WindowsAPI.SHELLSTATE();
                                    state.fShowExtensions = 0;
                                    WindowsAPI.SHGetSetSettings(ref state, WindowsAPI.SSF.SSF_SHOWEXTENSIONS, true);
                                    Explorer.RefreshExplorer();
                                }
                ));
            }
        }

        #endregion

        #region Hide items

        private void btnHideSelItems_Click(object sender, RoutedEventArgs e)
        {

            ShellObjectCollection SelItems = Explorer.SelectedItems;
            pd = new IProgressDialog(this.Handle);
            pd.Title = "Applying attributes";
            pd.CancelMessage = "Please wait while the operation is cancelled";
            pd.Maximum = (uint)SelItems.Count;
            pd.Value = 0;
            pd.Line1 = "Applying attributes to:";
            pd.Line3 = "Calculating Time Remaining...";
            pd.ShowDialog(IProgressDialog.PROGDLG.Normal, IProgressDialog.PROGDLG.AutoTime, IProgressDialog.PROGDLG.NoMinimize);
            Thread hthread = new Thread(new ParameterizedThreadStart(DoHideShow));
            hthread.Start(SelItems);

        }
        bool IsHidingUserCancel = false;
        public void DoHideShowWithChilds(object o)
        {
            IsHidingUserCancel = false;
            ShellObjectCollection SelItems = o as ShellObjectCollection;
            List<string> ItemsToHideShow = new List<string>();
            foreach (ShellObject obj in SelItems)
            {
                ItemsToHideShow.Add(obj.ParsingName);
                if (Directory.Exists(obj.ParsingName))
                {
                    DirectoryInfo di = new DirectoryInfo(obj.ParsingName);
                    DirectoryInfo[] dirs = di.GetDirectories("*", SearchOption.AllDirectories);
                    FileInfo[] files = di.GetFiles("*", SearchOption.AllDirectories);
                    foreach (DirectoryInfo dir in dirs)
                    {
                        ItemsToHideShow.Add(dir.FullName);
                    }
                    foreach (FileInfo file in files)
                    {
                        ItemsToHideShow.Add(file.FullName);
                    }
                }

            }

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {

                pd.Maximum = (uint)ItemsToHideShow.Count;

            }));

            foreach (string item in ItemsToHideShow)
            {
                try
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        if (pd.HasUserCancelled)
                        {
                            IsHidingUserCancel = true;

                        }
                    }));

                    if (IsHidingUserCancel)
                    {
                        break;
                    }

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {

                        pd.Line2 = item;

                    }));

                    System.IO.FileAttributes attr = File.GetAttributes(item);

                    if ((attr & System.IO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden)
                    {
                        File.SetAttributes(item, attr & ~System.IO.FileAttributes.Hidden);
                    }
                    else
                    {
                        File.SetAttributes(item, attr | System.IO.FileAttributes.Hidden);
                    }

                }
                finally
                {

                }

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {

                    pd.Value++;

                }));

            }
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {

                pd.CloseDialog();
            }));
        }
        public void DoHideShow(object o)
        {
            ShellObjectCollection SelItems = o as ShellObjectCollection;
            IsHidingUserCancel = false;
            foreach (ShellObject item in SelItems)
            {
                try
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        if (pd.HasUserCancelled)
                        {
                            IsHidingUserCancel = true;

                        }
                    }));

                    if (IsHidingUserCancel)
                    {
                        break;
                    }

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {

                        pd.Line2 = item.ParsingName;

                    }));

                    System.IO.FileAttributes attr = File.GetAttributes(item.ParsingName);

                    if ((attr & System.IO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden)
                    {
                        File.SetAttributes(item.ParsingName, attr & ~System.IO.FileAttributes.Hidden);
                    }
                    else
                    {
                        File.SetAttributes(item.ParsingName, attr | System.IO.FileAttributes.Hidden);
                    }

                }
                finally
                {

                }

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {

                    pd.Value++;

                }));

            }
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {

                pd.CloseDialog();
            }));
        }

        private void miHideItems_Click(object sender, RoutedEventArgs e)
        {
            ShellObjectCollection SelItems = Explorer.SelectedItems;
            pd = new IProgressDialog(this.Handle);
            pd.Title = "Applying attributes";
            pd.CancelMessage = "Please wait while the operation is cancelled";
            pd.Maximum = 100;
            pd.Value = 0;
            pd.Line1 = "Applying attributes to:";
            pd.Line3 = "Calculating Time Remaining...";
            pd.ShowDialog(IProgressDialog.PROGDLG.Normal, IProgressDialog.PROGDLG.AutoTime, IProgressDialog.PROGDLG.NoMinimize);
            Thread hthread = new Thread(new ParameterizedThreadStart(DoHideShowWithChilds));
            hthread.Start(SelItems);
        }

        #endregion

        #region Share Tab Commands (excluding Archive)

        private void btnMapDrive_Click(object sender, RoutedEventArgs e)
        {
      WindowsAPI.MapDrive(this.Handle, Explorer.SelectedItems.Count == 1 ? Explorer.SelectedItems[0].ParsingName : String.Empty);
        }

        private void btnDisconectDrive_Click(object sender, RoutedEventArgs e)
        {
            WindowsAPI.WNetDisconnectDialog(this.Handle, 1);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            Explorer.OpenShareUI();
        }

        private void btnAdvancedSecurity_Click(object sender, RoutedEventArgs e)
        {
            Explorer.ShowPropPage(this.Handle, Explorer.SelectedItems[0].ParsingName, "Security");
        }

        #endregion

        #region Change Language

        //private void btnchangeLocale_Click(object sender, RoutedEventArgs e)
        //{
        //    RegistryKey rk = Registry.CurrentUser;
        //    RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);

        //    rks.SetValue(@"Locale", txtLocale.Text);

        //    SelectCulture(txtLocale.Text);

        //    rks.Close();
        //    rk.Close();
        //}

        public void ChangeLocale(string locale)
        {
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);

            rks.SetValue(@"Locale", locale);

            SelectCulture(locale);

            rks.Close();
            rk.Close();
        }

        /// <summary>
        /// Sets the UI language
        /// </summary>
        /// <param name="culture">Language code (ex. "en-EN")</param>
        public static void SelectCulture(string culture)
        {
            // List all our resources      
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                dictionaryList.Add(dictionary);
            }
            // We want our specific culture      
            string requestedCulture = string.Format("Locale.{0}.xaml", culture);
            ResourceDictionary resourceDictionary =
                dictionaryList.FirstOrDefault(d => d.Source.OriginalString == "/BetterExplorer;component/Translation/" + requestedCulture);
            if (resourceDictionary == null)
            {
                // If not found, we select our default language        
                //        
                requestedCulture = "DefaultLocale.xaml";
                resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == "/BetterExplorer;component/Translation/" + requestedCulture);
            }

            // If we have the requested resource, remove it from the list and place at the end.\      
            // Then this language will be our string table to use.      
            if (resourceDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            }
            // Inform the threads of the new culture      

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
        }

        /// <summary>
        /// Sets the UI language
        /// </summary>
        /// <param name="culture">Language code (ex. "en-EN")</param>
        /// <param name="filename">The file to load the resources from</param>
        public static void SelectCulture(string culture, string filename)
        {
            // List all our resources      
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                dictionaryList.Add(dictionary);
            }

            ResourceDictionary resourceDictionary = ResDictionaryLoader.Load(filename);
            if (resourceDictionary == null)
            {
                // if not found, then try from the application's resources
                string requestedCulture = string.Format("Locale.{0}.xaml", culture);
                resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == "/BetterExplorer;component/Translation/" + requestedCulture);
                if (resourceDictionary == null)
                {
                    // If not found, we select our default language        
                    //        
                    requestedCulture = "DefaultLocale.xaml";
                    resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == "/BetterExplorer;component/Translation/" + requestedCulture);
                }
            }

            // If we have the requested resource, remove it from the list and place at the end.\      
            // Then this language will be our string table to use.      
            if (resourceDictionary != null)
            {
                try
                {
                    Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
                }
                catch
                {

                }

                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            }
            // Inform the threads of the new culture      
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
        }

        private void TranslationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReadyToChangeLanguage == true)
            {
                ChangeLocale(((TranslationComboBoxItem)e.AddedItems[0]).LocaleCode);
        TranslateUI();
            }
        }

        private void btnRemoveLangSetting_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);

            rks.DeleteValue(@"Locale");

            rks.Close();
            rk.Close();
        }

        private void TranslationHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(TranslationURL.Text);
        }

        #endregion

        #region Image Editing

        private ImageFormat GetImageFormat(string FileName)
        {
            using (Bitmap bitmap = new Bitmap(FileName))
            {
                return bitmap.RawFormat;
            }
        }

        private void RotateAndSaveImage(String input, String output, ImageFormat format, RotateFlipType type)
        {
            //create an object that we can use to examine an image file
            System.Drawing.Image img = System.Drawing.Image.FromFile(input);

            //rotate the picture by 90 degrees
            img.RotateFlip(type);

            //re-save the picture
            img.Save(output, format);

            //tidy up after we've finished
            img.Dispose();
        }

        private void ConvertImage(ImageFormat format, string name, string extension)
        {
            System.Drawing.Bitmap cvt = new Bitmap(name);
            string namen = RemoveExtensionsFromFile(name, new System.IO.FileInfo(name).Extension);
            try
            {
                AddToLog("Converted Image from " + name + " to new file " + namen + extension);
                cvt.Save(namen + extension, format);
            }
            catch (Exception)
            {
                MessageBox.Show("There appears to have been an issue with converting the file. Make sure the filename \"" + RemoveExtensionsFromFile(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default), new System.IO.FileInfo(name).Extension) + extension + "\" does already not exist.", "Conversion Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            cvt.Dispose();
        }

        private void ConvertToJPG_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                ConvertImage(ImageFormat.Jpeg, item.ParsingName, ".jpg");
            }
        }

        private void ConvertToPNG_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                ConvertImage(ImageFormat.Png, item.ParsingName, ".png");
            }
        }

        private void ConvertToGIF_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                ConvertImage(ImageFormat.Gif, item.ParsingName, ".gif");
            }
        }

        private void ConvertToBMP_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                ConvertImage(ImageFormat.Bmp, item.ParsingName, ".bmp");
            }
        }

        private void ConvertToWMF_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                ConvertImage(ImageFormat.Wmf, item.ParsingName, ".wmf");
            }
        }

        private void btnRotateLeft_Click(object sender, RoutedEventArgs e)
        {
            //Dispatcher.BeginInvoke(
            //                  System.Windows.Threading.DispatcherPriority.Background,
            //                  new Action(
            //                    delegate()
            //                    {
            //                        ImageFormat imagef = GetImageFormat(Explorer.SelectedItems[0].ParsingName);
            //                        RotateAndSaveImage(Explorer.SelectedItems[0].ParsingName,
            //                            Explorer.SelectedItems[0].ParsingName, imagef, RotateFlipType.Rotate270FlipNone);
            //                    }));
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                if (OverwriteOnRotate == true)
                {
                    System.Drawing.Bitmap cvt = new Bitmap(item.ParsingName);
                    cvt.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    cvt.Save(item.ParsingName);
                    cvt.Dispose();
                    AddToLog("Rotated image " + item.ParsingName);
                }
                else
                {
                    System.Drawing.Bitmap cvt = new Bitmap(item.ParsingName);
                    string ext = GetExtension(item.ParsingName);
                    string name = item.ParsingName;
                    string namen = RemoveExtensionsFromFile(name, new System.IO.FileInfo(name).Extension);
                    cvt.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    cvt.Save(namen + "_Rotated270" + ext);
                    cvt.Dispose();
                    AddToLog("Rotated image " + item.ParsingName);
                }
            }

        }

        private void btnRotateRight_Click(object sender, RoutedEventArgs e)
        {
            //Dispatcher.BeginInvoke(
            //                  System.Windows.Threading.DispatcherPriority.Background,
            //                  new Action(
            //                    delegate()
            //                    {
            //                        ImageFormat imagef = GetImageFormat(Explorer.SelectedItems[0].ParsingName);
            //                        RotateAndSaveImage(Explorer.SelectedItems[0].ParsingName,
            //                            Explorer.SelectedItems[0].ParsingName, imagef, RotateFlipType.Rotate90FlipNone);
            //                    }));
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                if (OverwriteOnRotate == true)
                {
                    System.Drawing.Bitmap cvt = new Bitmap(item.ParsingName);
                    cvt.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    cvt.Save(item.ParsingName);
                    cvt.Dispose();
                    AddToLog("Rotated image " + item.ParsingName);
                }
                else
                {
                    System.Drawing.Bitmap cvt = new Bitmap(item.ParsingName);
                    string ext = GetExtension(item.ParsingName);
                    string name = item.ParsingName;
                    string namen = RemoveExtensionsFromFile(name, new System.IO.FileInfo(name).Extension);
                    cvt.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    cvt.Save(namen + "_Rotated90" + ext);
                    cvt.Dispose();
                    AddToLog("Rotated image " + item.ParsingName);
                }
            }
        }

        private void btnWallpaper_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
            {
                Wallpaper TheWall = new Wallpaper();

                TheWall.Set(new Uri(Explorer.SelectedItems[0].ParsingName), Wallpaper.Style.Stretched);
            }));
        }

        private void miWallFill_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
            {
                Wallpaper TheWall = new Wallpaper();

                TheWall.Set(new Uri(Explorer.SelectedItems[0].ParsingName), Wallpaper.Style.Fill);
            }));
        }

        private void miWallFit_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
            {
                Wallpaper TheWall = new Wallpaper();

                TheWall.Set(new Uri(Explorer.SelectedItems[0].ParsingName), Wallpaper.Style.Fit);
            }));
        }

        private void miWallStretch_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
            {
                Wallpaper TheWall = new Wallpaper();

                TheWall.Set(new Uri(Explorer.SelectedItems[0].ParsingName), Wallpaper.Style.Stretched);
            }));
        }

        private void miWallTile_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
            {
                Wallpaper TheWall = new Wallpaper();

                TheWall.Set(new Uri(Explorer.SelectedItems[0].ParsingName), Wallpaper.Style.Tiled);
            }));
        }

        private void miWallCenter_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
            {
                Wallpaper TheWall = new Wallpaper();

                TheWall.Set(new Uri(Explorer.SelectedItems[0].ParsingName), Wallpaper.Style.Centered);
            }));
        }

        private void btnFlipX_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                if (OverwriteOnRotate == true)
                {
                    System.Drawing.Bitmap cvt = new Bitmap(item.ParsingName);
                    cvt.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    cvt.Save(item.ParsingName);
                    cvt.Dispose();
                    AddToLog("Flipped image " + item.ParsingName);
                }
                else
                {
                    System.Drawing.Bitmap cvt = new Bitmap(item.ParsingName);
                    string ext = GetExtension(item.ParsingName);
                    string name = item.ParsingName;
                    string namen = RemoveExtensionsFromFile(name, new System.IO.FileInfo(name).Extension);
                    cvt.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    cvt.Save(namen + "_FlippedX" + ext);
                    cvt.Dispose();
                    AddToLog("Flipped image " + item.ParsingName);
                }
            }
        }

        private void btnFlipY_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShellObject item in Explorer.SelectedItems)
            {
                if (OverwriteOnRotate == true)
                {
                    System.Drawing.Bitmap cvt = new Bitmap(item.ParsingName);
                    cvt.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    cvt.Save(item.ParsingName);
                    cvt.Dispose();
                    AddToLog("Flipped image " + item.ParsingName);
                }
                else
                {
                    System.Drawing.Bitmap cvt = new Bitmap(item.ParsingName);
                    string ext = GetExtension(item.ParsingName);
                    string name = item.ParsingName;
                    string namen = RemoveExtensionsFromFile(name, new System.IO.FileInfo(name).Extension);
                    cvt.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    cvt.Save(namen + "_FlippedY" + ext);
                    cvt.Dispose();
                    AddToLog("Flipped image " + item.ParsingName);
                }
            }
        }

        private void btnResize_Click(object sender, RoutedEventArgs e)
        {
            ResizeImage ri = new ResizeImage(Explorer.SelectedItems[0]);
            ri.ShowDialog();
            if (ri.Confirm == true)
            {
                System.Drawing.Bitmap cvt = new Bitmap(Explorer.SelectedItems[0].ParsingName);
                System.Drawing.Bitmap cst = ChangeImageSize(cvt, ri.newwidth, ri.newheight);

                string ext = GetExtension(Explorer.SelectedItems[0].ParsingName);

                cst.Save(Explorer.SelectedItems[0].ParsingName + " (" + ri.newwidth + " X " + ri.newheight + ")" + ext);
                cvt.Dispose();
                cst.Dispose();
            }
        }

        private Bitmap ChangeImageSize(Bitmap img, int width, int height)
        {
            Bitmap bm_dest = new Bitmap(width, height);
            Graphics gr_dest = Graphics.FromImage(bm_dest);
            gr_dest.DrawImage(img, 0, 0, bm_dest.Width + 1, bm_dest.Height + 1);
            return bm_dest;
        }

        #endregion

        #region Folder Tools Commands
        private void btnChangeFoldericon_Click(object sender, RoutedEventArgs e)
        {
            IconView iv = new IconView();

            iv.LoadIcons(Explorer, false);
        }

        private void btnClearFoldericon_Click(object sender, RoutedEventArgs e)
        {
            Explorer.ClearFolderIcon(Explorer.SelectedItems[0].ParsingName);
        }
        #endregion

        #region Registry Setting Changes / BetterExplorerOperations Calls / Action Log

        private void btnSetCurrentasStartup_Click(object sender, RoutedEventArgs e)
        {
      backstage.IsOpen = true;
            string CurrentLocString = Explorer.NavigationLog.CurrentLocation.ParsingName;
            StartUpLocation = CurrentLocString;
            btnSetCurrentasStartup.Header = Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default);
            btnSetCurrentasStartup.Icon = Explorer.NavigationLog.CurrentLocation.Thumbnail.BitmapSource;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"StartUpLoc", CurrentLocString);
            rks.Close();
            rk.Close();

        }

        private void chkIsFlyout_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"HFlyoutEnabled", 1);
                IsHFlyoutEnabled = true;
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsFlyout_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"HFlyoutEnabled", 0);
                IsHFlyoutEnabled = false;
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsDefault_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                String CurrentexePath = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
                string dir = Path.GetDirectoryName(CurrentexePath);
                string ExePath = Path.Combine(dir, @"BetterExplorerOperations.exe");
                Process proc = new Process();
                var psi = new ProcessStartInfo
                {
                    FileName = ExePath,
                    Verb = "runas",
                    UseShellExecute = true,
          Arguments = String.Format("/env /user:Administrator \"{0}\"", ExePath),
                };
                proc.StartInfo = psi;
                proc.Start();
                Thread.Sleep(1000);
                int h = (int)WindowsAPI.getWindowId(null, "BetterExplorerOperations");
                int jj = WindowsAPI.sendWindowsStringMessage((int)WindowsAPI.getWindowId(null, "BetterExplorerOperations"),
                    0, "0x77654");
                proc.WaitForExit();
                if (proc.ExitCode == -1) {
                    isOnLoad = true;
                    (sender as Fluent.CheckBox).IsChecked = false;
                    isOnLoad = false;
                    MessageBox.Show("Can't set Better Explorer as default!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }
        public const int WM_COPYDATA = 0x4A;
        private void chkIsDefault_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                String CurrentexePath = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
                string dir = Path.GetDirectoryName(CurrentexePath);
                string ExePath = Path.Combine(dir, @"BetterExplorerOperations.exe");
                Process proc = new Process();
                var psi = new ProcessStartInfo
                {
                    FileName = ExePath,
                    Verb = "runas",
                    UseShellExecute = true,
          Arguments = String.Format("/env /user:Administrator \"{0}\"", ExePath),
                };
                proc.StartInfo = psi;
                proc.Start();
                Thread.Sleep(1000);
                WindowsAPI.sendWindowsStringMessage((int)WindowsAPI.getWindowId(null, "BetterExplorerOperations"),
                     0, "0x77655");
                proc.WaitForExit();
                if (proc.ExitCode == -1) {
                    isOnLoad = true;
                    (sender as Fluent.CheckBox).IsChecked = true;
                    isOnLoad = false;
                    MessageBox.Show("Can't restore default filemanager!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void chkIsTerraCopyEnabled_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"FileOpExEnabled", 1);
                IsExtendedFileOpEnabled = true;
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsTerraCopyEnabled_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"FileOpExEnabled", 0);
                IsExtendedFileOpEnabled = false;
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsCompartibleRename_Checked(object sender, RoutedEventArgs e)
        {
            IsCompartibleRename = true;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CompartibleRename", 1);
            rks.Close();
            rk.Close();
        }

        private void chkIsCompartibleRename_Unchecked(object sender, RoutedEventArgs e)
        {
            IsCompartibleRename = false;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CompartibleRename", 0);
            rks.Close();
            rk.Close();
        }

        private void chkIsRestoreTabs_Checked(object sender, RoutedEventArgs e)
        {
            IsrestoreTabs = true;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"IsRestoreTabs", 1);
            rks.Close();
            rk.Close();
        }

        private void chkIsRestoreTabs_Unchecked(object sender, RoutedEventArgs e)
        {
            IsrestoreTabs = false;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"IsRestoreTabs", 0);
            rks.Close();
            rk.Close();
        }

        private void chkIsVistaStyleListView_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"IsVistaStyleListView", 1);
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsVistaStyleListView_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"IsVistaStyleListView", 0);
                rks.Close();
                rk.Close();
            }
        }

        private void gridSplitter1_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"SearchBarWidth", SearchBarColumn.Width.Value);
            rks.Close();
            rk.Close();
        }

        private void SearchBarReset_Click(object sender, RoutedEventArgs e)
        {
            SearchBarColumn.Width = new GridLength(220);
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"SearchBarWidth", SearchBarColumn.Width.Value);
            rks.Close();
            rk.Close();
        }

        private void chkShowCheckBoxes_Checked(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CheckModeEnabled", 1);
            isCheckModeEnabled = true;
            Explorer.ContentOptions.CheckSelect = true;
            rk.Close();
            rks.Close();
        }

        private void chkShowCheckBoxes_Unchecked(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"CheckModeEnabled", 0);
            isCheckModeEnabled = false;
            Explorer.ContentOptions.CheckSelect = false;
            rk.Close();
            rks.Close();
        }

        private void chkTreeExpand_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                String CurrentexePath = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
                string dir = Path.GetDirectoryName(CurrentexePath);
                string ExePath = Path.Combine(dir, @"BetterExplorerOperations.exe");
                Process proc = new Process();
                var psi = new ProcessStartInfo
                {
                    FileName = ExePath,
                    Verb = "runas",
                    UseShellExecute = true,
                    Arguments = "/env /user:" + "Administrator " + "\"" + ExePath + "\"",
                };
                proc.StartInfo = psi;
                proc.Start();
                Thread.Sleep(1000);
                int h = (int)WindowsAPI.getWindowId(null, "BetterExplorerOperations");
                int jj = WindowsAPI.sendWindowsStringMessage((int)WindowsAPI.getWindowId(null, "BetterExplorerOperations"),
                    0, "0x88775");
                proc.WaitForExit();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SymLinkInfo
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string lpDestination;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string lpTarget;
            public int SymType;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string lpMsg;

        };

        private void chkTreeExpand_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                String CurrentexePath = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
                string dir = Path.GetDirectoryName(CurrentexePath);
                string ExePath = Path.Combine(dir, @"BetterExplorerOperations.exe");
                Process proc = new Process();
                var psi = new ProcessStartInfo
                {
                    FileName = ExePath,
                    Verb = "runas",
                    UseShellExecute = true,
                    Arguments = "/env /user:" + "Administrator " + "\"" + ExePath + "\"",
                };
                proc.StartInfo = psi;
                proc.Start();
                Thread.Sleep(1000);
                int h = (int)WindowsAPI.getWindowId(null, "BetterExplorerOperations");
                int jj = WindowsAPI.sendWindowsStringMessage((int)WindowsAPI.getWindowId(null, "BetterExplorerOperations"),
                    0, "0x88776");
                proc.WaitForExit();
            }
        }

        private void chkOverwriteImages_Checked(object sender, RoutedEventArgs e)
        {
            OverwriteOnRotate = true;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"OverwriteImageWhileEditing", 1);
            rks.Close();
            rk.Close();
        }

        private void chkOverwriteImages_Unchecked(object sender, RoutedEventArgs e)
        {
            OverwriteOnRotate = false;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"OverwriteImageWhileEditing", 0);
            rks.Close();
            rk.Close();
        }

        private void chkIsCFO_Click(object sender, RoutedEventArgs e)
        {
            if (chkIsCFO.IsChecked.Value)
            {
                ExplorerBrowser.SetCustomDialogs(true);
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"IsCustomFO", 1, RegistryValueKind.DWord);
                rks.Close();
                rk.Close();
            }
            else
            {
                ExplorerBrowser.SetCustomDialogs(false);
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"IsCustomFO", 0, RegistryValueKind.DWord);
                rks.Close();
                rk.Close();
            }
        }

        private void chkRibbonMinimizedGlass_Click(object sender, RoutedEventArgs e)
        {
            if (chkRibbonMinimizedGlass.IsChecked.Value)
            {
                this.IsGlassOnRibonMinimized = true;
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"RibbonMinimizedGlass", 1, RegistryValueKind.DWord);
                rks.Close();
                rk.Close();
                if (TheRibbon.IsMinimized)
                {
                    System.Windows.Point p =
                        ShellVView.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
                    this.GlassBorderThickness = new Thickness(8, p.Y, 8, 8);
                }
            }
            else
            {
                this.IsGlassOnRibonMinimized = false;
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"RibbonMinimizedGlass", 0, RegistryValueKind.DWord);
                rks.Close();
                rk.Close();
                if (TheRibbon.IsMinimized)
                {
                    System.Windows.Point p = backstage.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
                    this.GlassBorderThickness = new Thickness(8, p.Y + backstage.ActualHeight, 8, 8);
                }
            }
        }

        private void chkLogHistory_Checked(object sender, RoutedEventArgs e)
        {
            canlogactions = true;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"EnableActionLog", 1);
            rks.Close();
            rk.Close();
        }

        private void chkLogHistory_Unchecked(object sender, RoutedEventArgs e)
        {
            canlogactions = false;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
            rks.SetValue(@"EnableActionLog", 0);
            rks.Close();
            rk.Close();
        }

        private void AddToLog(string value)
        {
            try
            {
                if (canlogactions == true)
                {
                    if (Directory.Exists(logdir) == false)
                    {
                        Directory.CreateDirectory(logdir);
                    }

          using (StreamWriter sw = new StreamWriter(String.Format("{0}{1}.txt", logdir, sessionid), true))
                    {
                        sw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + " : " + value);
                    }
                }
            }
            catch (Exception exe)
            {
                MessageBox.Show("An error occurred while writing to the log file. This error can be avoided if you disable the action logging feature. Please report this issue at http://bugtracker.better-explorer.com/. \r\n\r\n Here is some information about the error: \r\n\r\n" + exe.Message + "\r\n\r\n" + exe.ToString(), "Error While Writing to Log", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnShowLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(logdir) == false)
                {
                    Directory.CreateDirectory(logdir);
                }

                NewTab(logdir);

                this.backstage.IsOpen = false;
            }
            catch (Exception exe)
            {
                MessageBox.Show("An error occurred while trying to open the logs folder. Please report this issue at http://bugtracker.better-explorer.com/. \r\n\r\n Here is some information about the error: \r\n\r\n" + exe.Message + "\r\n\r\n" + exe.ToString(), "Error While Opening Log Folder", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Change Pane Visibility

        /// <summary>
        /// Changes the PaneVisibility at Runtime
        /// </summary>
        /// <param name="PaneForChange">The pane for change (0x1 - Details, 0x2 - Preview, 0x3 - Navigation)</param>
        /// <param name="IsShow"></param>
        private async void ChangePaneVisibility(uint PaneForChange, bool IsShow)
        {

            ShellObject lastLoc = Explorer.NavigationLog.CurrentLocation;
      await Task.Run(() =>
          {
            Dispatcher.Invoke(DispatcherPriority.Render, (ThreadStart)(() =>
            {
              Explorer.DestroyBrowser();



              switch (PaneForChange)
              {
                case 0x1:
                  {
                    Explorer.NavigationOptions.PaneVisibility.Details = IsShow ? PaneVisibilityState.Show : PaneVisibilityState.Hide;
                    RegistryKey rk = Registry.CurrentUser;
                    RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                    rks.SetValue(@"InfoPaneEnabled", IsShow ? 1 : 0);
                    rks.Close();
                    rk.Close();
                    IsInfoPaneEnabled = IsShow;
                    if (!IsNavigationPaneEnabled)
                    {
                      Explorer.NavigationOptions.PaneVisibility.Navigation = PaneVisibilityState.Hide;
                    }
                    if (!IsPreviewPaneEnabled)
                    {
                      Explorer.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Hide;
                    }
                    else
                    {
                      Explorer.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Show;
                    }
                    break;
                  }
                case 0x2:
                  {
                    Explorer.NavigationOptions.PaneVisibility.Preview = IsShow ? PaneVisibilityState.Show : PaneVisibilityState.Hide;

                    RegistryKey rk = Registry.CurrentUser;
                    RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                    rks.SetValue(@"PreviewPaneEnabled", IsShow ? 1 : 0);
                    rks.Close();
                    rk.Close();
                    IsPreviewPaneEnabled = IsShow;
                    if (!IsNavigationPaneEnabled)
                    {
                      Explorer.NavigationOptions.PaneVisibility.Navigation = PaneVisibilityState.Hide;
                    }
                    if (!IsInfoPaneEnabled)
                    {
                      Explorer.NavigationOptions.PaneVisibility.Details = PaneVisibilityState.Hide;
                    }
                    break;
                  }
                case 0x3:
                  {
                    Explorer.NavigationOptions.PaneVisibility.Navigation = IsShow ? PaneVisibilityState.Show : PaneVisibilityState.Hide;
                    IsNavigationPaneEnabled = true;
                    RegistryKey rk = Registry.CurrentUser;
                    RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                    rks.SetValue(@"NavigationPaneEnabled", IsShow ? 1 : 0);
                    rks.Close();
                    rk.Close();
                    IsNavigationPaneEnabled = IsShow;
                    if (!IsPreviewPaneEnabled)
                    {
                      Explorer.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Hide;
                    }
                    else
                    {
                      Explorer.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Show;
                    }
                    if (!IsInfoPaneEnabled)
                    {
                      Explorer.NavigationOptions.PaneVisibility.Details = PaneVisibilityState.Hide;
                    }
                    break;
                  }
                default:
                  break;
              }

              Explorer.InitBrowser();
            }));
          });

      //InitializeExplorerControl();


            isGoingBackOrForward = true;

      //Explorer.Width = (int)ShellVView.Width;
      //Explorer.Height = (int)ShellVView.Height;

      Explorer.Navigate(lastLoc);
      //ShellVView.Child = Explorer;
        }

    void Explorer_ItemMouseMiddleClick(object sender, ExplorerMouseEventArgs e) {
      NewTab(e.Item);
    }

        private void btnNavigationPane_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
                ChangePaneVisibility(0x3, true);
        }

        private void btnNavigationPane_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
                ChangePaneVisibility(0x3, false);
        }

        private void btnPreviewPane_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
                ChangePaneVisibility(0x2, true);
        }

        private void btnPreviewPane_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
                ChangePaneVisibility(0x2, false);
        }

        private void btnInfoPane_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
                ChangePaneVisibility(0x1, false);
        }

        private void btnInfoPane_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
                ChangePaneVisibility(0x1, true);
        }

        // Legacy Pane Code
        private void chkIsInfoPane_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"InfoPaneEnabled", 1);
                IsInfoPaneEnabled = true;
                ChangePaneVisibility(0x1, true);
                //Explorer.Navigate(Explorer.NavigationLog.CurrentLocation);
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsInfoPane_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"InfoPaneEnabled", 0);
                IsInfoPaneEnabled = false;
                Explorer.NavigationOptions.PaneVisibility.Details = PaneVisibilityState.Hide;
                //Explorer.Navigate(Explorer.NavigationLog.CurrentLocation);
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsPreviewPane_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"PreviewPaneEnabled", 0);
                IsPreviewPaneEnabled = false;
                Explorer.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Hide;
                //Explorer.Navigate(Explorer.NavigationLog.CurrentLocation);
                rks.Close();
                rk.Close();
            }
        }

        private void chkIsPreviewPane_Checked(object sender, RoutedEventArgs e)
        {
            if (!isOnLoad)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);
                rks.SetValue(@"PreviewPaneEnabled", 1);
                IsPreviewPaneEnabled = true;
                Explorer.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Show;
                //Explorer.SetState();
                //Explorer.Navigate(Explorer.NavigationLog.CurrentLocation);
                rks.Close();
                rk.Close();
            }
        }


        #endregion

        #region IsBool Code

        private void TheStatusBar_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {

        }

        private void RibbonWindow_Activated(object sender, EventArgs e)
        {
      if (!backstage.IsOpen)
        Explorer.SetExplorerFocus();
      
        }

        void fAbout_Closed(object sender, EventArgs e)
        {

        }


        private void btnCopyto_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void btnCopyto_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void btnMoreColls_DropDownOpened(object sender, EventArgs e)
        {

        }

        private void btnMoreColls_DropDownClosed(object sender, EventArgs e)
        {

        }

        void cmHistory_Closed(object sender, RoutedEventArgs e)
        {

        }

        void cmHistory_Opened(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region Breadcrumb Bar

        private void breadcrumbBarControl1_NavigateRequested(object sender, PathEventArgs e)
        {
     
            Explorer.Navigate(e.ShellObject != null ? e.ShellObject : ShellObject.FromParsingName(e.Path));
        }

        private void RibbonWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                breadcrumbBarControl1.ExitEditMode();
        Explorer.IsCancelNavigation = true;
            }
        }

        private void TheStatusBar_GotFocus(object sender, RoutedEventArgs e)
        {
            //if (IsAfterRename)
            //{
            //    breadcrumbBarControl1.ExitEditMode();
            //}
            if (!backstage.IsOpen)
                Explorer.SetExplorerFocus();
        }

        private void RibbonWindow_GotFocus(object sender, RoutedEventArgs e)
        {

            if (breadcrumbBarControl1.IsInEditMode)
            {
                breadcrumbBarControl1.ExitEditMode();
            }

      if (!backstage.IsOpen)
          Explorer.SetExplorerFocus();
        }

        private void SaveHistoryToFile(string relativepath, List<String> history)
        {
            //string path = null;
            //foreach (ShellObject item in Explorer.SelectedItems)
            //{
            //    //This way i think is better of making multiple line in .Net ;)
            //    if (string.IsNullOrEmpty(path))
            //    {
            //        path = item.ParsingName;
            //    }
            //    else
            //    {
            //        path = path + "\r\n" + item.ParsingName;
            //    }

            //}

            // Write each entry to a file. (the "false" parameter makes sure the file is overwritten.)
            using (StreamWriter sw = new StreamWriter(relativepath, false, Encoding.UTF8))
            {
                foreach (string item in history)
                {
          if (!String.IsNullOrEmpty(item.Replace("\0",String.Empty)))
                      sw.WriteLine(item);
                }
            }

        }

        private ObservableCollection<BreadcrumbBarFSItem> ReadHistoryFromFile(string relativepath)
        {
            string line = "";
      ObservableCollection<BreadcrumbBarFSItem> hl = new ObservableCollection<BreadcrumbBarFSItem>();
            using (StreamReader sr = new StreamReader(relativepath,Encoding.UTF8))
            {
                while ((line = sr.ReadLine()) != null)
                {
          if (!String.IsNullOrEmpty(line.Replace("\0", String.Empty)))
          {
            BreadcrumbBarFSItem item = null;
            if (line.Trim().StartsWith("%"))
            {
              var path = Environment.ExpandEnvironmentVariables(line);
              item = new BreadcrumbBarFSItem(line, path);
            }
            else
            {
              item = new BreadcrumbBarFSItem(ShellObject.FromParsingName(line.StartsWith(":") ? "shell:" + line : line));
            }
            hl.Add(item);
          }
                }
            }

            return hl;
        }

        void bbi_Drop(object sender, DragEventArgs e)
        {


            System.Windows.Point pt = e.GetPosition(sender as IInputElement);


            if ((sender as BreadcrumbBarItem).ShellObject.IsFileSystemObject)
            {
                if ((e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey)
                {
                    e.Effects = DragDropEffects.Copy;
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        FileOperationsData PasteData = new FileOperationsData();
                        String[] collection = (String[])e.Data.GetData(DataFormats.FileDrop);
                        StringCollection scol = new StringCollection();
                        scol.AddRange(collection);
                        PasteData.DropList = scol;
                        PasteData.PathForDrop = (sender as BreadcrumbBarItem).ShellObject.ParsingName;
                        AddToLog("Copied Files to " + PasteData.PathForDrop + " Files copied: " + scol.ToString());
                        Thread t = null;
                        t = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start(PasteData);
                    }
                }
                else
                {
                    //if (Path.GetPathRoot((sender as ClosableTabItem).Path.ParsingName) ==
                    //    Path.GetPathRoot(Explorer.NavigationLog.CurrentLocation.ParsingName))
                    //{
                    e.Effects = DragDropEffects.Move;
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        FileOperationsData PasteData = new FileOperationsData();
                        String[] collection = (String[])e.Data.GetData(DataFormats.FileDrop);
                        StringCollection scol = new StringCollection();
                        scol.AddRange(collection);
                        PasteData.DropList = scol;
                        PasteData.PathForDrop = (sender as BreadcrumbBarItem).ShellObject.ParsingName;
                        AddToLog("Moved Files to " + PasteData.PathForDrop + " Files moved: " + scol.ToString());
                        Thread t = null;
                        t = new Thread(new ParameterizedThreadStart(Explorer.DoMove));
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start(PasteData);
                    }

                    //}
                    //else
                    //{
                    //    e.Effects = DragDropEffects.Copy;
                    //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    //    {
                    //        FileOperationsData PasteData = new FileOperationsData();
                    //        String[] collection = (String[])e.Data.GetData(DataFormats.FileDrop);
                    //        StringCollection scol = new StringCollection();
                    //        scol.AddRange(collection);
                    //        PasteData.DropList = scol;
                    //        PasteData.PathForDrop = (sender as ClosableTabItem).Path.ParsingName;
                    //        Thread t = null;
                    //        t = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
                    //        t.SetApartmentState(ApartmentState.STA);
                    //        t.Start(PasteData);
                    //    }

                    //}
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }


            DropTargetHelper.Drop(e.Data, pt, e.Effects);
        }

        void bbi_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if ((sender as BreadcrumbBarItem).ShellObject.IsFileSystemObject)
            {
                if ((e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey)
                {
                    e.Effects = DragDropEffects.Copy;

                }
                else
                {
                    //if (Path.GetPathRoot((sender as ClosableTabItem).Path.ParsingName) ==
                    //    Path.GetPathRoot(Explorer.NavigationLog.CurrentLocation.ParsingName))
                    //{
                    //    e.Effects = DragDropEffects.Move;


                    //}
                    //else
                    //{
                    //    e.Effects = DragDropEffects.Copy;

                    //}

                    // I decided just to have it do a move because it will avoid errors with special shell folders.
                    // Besides, if a person wants to copy something, they should know how to press Ctrl to copy.
                    e.Effects = DragDropEffects.Move;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            Win32Point ptw = new Win32Point();
            GetCursorPos(ref ptw);
            //e.Handled = true;
            DropTargetHelper.DragOver(new System.Windows.Point(ptw.X, ptw.Y), e.Effects);
        }

        void bbi_DragLeave(object sender, DragEventArgs e)
        {
            DropTargetHelper.DragLeave();
        }

        void bbi_DragEnter(object sender, DragEventArgs e)
        {


            if ((sender as BreadcrumbBarItem).ShellObject.IsFileSystemObject)
            {
                if ((e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey)
                {
                    e.Effects = DragDropEffects.Copy;

                }
                else
                {
                    //if (Path.GetPathRoot((sender as ClosableTabItem).Path.ParsingName) ==
                    //    Path.GetPathRoot(Explorer.NavigationLog.CurrentLocation.ParsingName))
                    //{
                    //    e.Effects = DragDropEffects.Move;


                    //}
                    //else
                    //{
                    //    e.Effects = DragDropEffects.Copy;

                    //}

                    // I decided just to have it do a move because it will avoid errors with special shell folders.
                    // Besides, if a person wants to copy something, they should know how to press Ctrl to copy.
                    e.Effects = DragDropEffects.Move;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }


            Win32Point ptw = new Win32Point();
            GetCursorPos(ref ptw);
            e.Effects = DragDropEffects.None;
            DropTargetHelper.DragEnter(this, e.Data, new System.Windows.Point(ptw.X, ptw.Y), e.Effects);
        }

        #endregion

        #region Search

        private void searchTextBox1_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            //e.Handled = true;
            //searchTextBox1.Focus();
        }

        private void searchTextBox1_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void searchTextBox1_GotFocus(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

        }

        private void searchTextBox1_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {


        }

        private void searchTextBox1_LostFocus(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

        }

        private void searchTextBox1_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            e.Handled = true;

        }

        private void searchTextBox1_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ctgSearch.Visibility = System.Windows.Visibility.Visible;
            if (!TheRibbon.IsMinimized)
            { 
                TheRibbon.SelectedTabItem = tbSearch;
            }
            
        }

        private void MenuItem_Checked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            bool isThereChecked = false;
            foreach (object item in ((sender as MenuItem).Parent as SplitButton).Items)
            {
                if (item is MenuItem)
                {
                    if ((item as MenuItem).IsChecked)
                    {
                        isThereChecked = true;
                        break;
                    }
                }

            }

            ((sender as MenuItem).Parent as SplitButton).IsChecked = isThereChecked;
        }

        private void MenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            bool isThereChecked = false;
            foreach (object item in ((sender as MenuItem).Parent as SplitButton).Items)
            {
                if (item is MenuItem)
                {
                    if ((item as MenuItem).IsChecked)
                    {
                        isThereChecked = true;
                        break;
                    }
                }

            }
            ((sender as MenuItem).Parent as SplitButton).IsChecked = isThereChecked;
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            //(sender as MenuItem).IsChecked = !(sender as MenuItem).IsChecked;
        }

        private void ctgSearch_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //if (((System.Windows.Visibility)e.NewValue) == System.Windows.Visibility.Collapsed)
            //    TheRibbon.SelectedTabItem = HomeTab;
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            //string SearchStr = "";
            //string CondStr = "";
            //string KindStr = "";
            //string OtherStr = "";
            //SearchStr = edtSearchBox.SearchCriteriatext.Text;
            ////foreach (Fluent.ToggleButton item in tsgModifiers.Items)
            ////{
            ////    if (item.IsChecked == true)
            ////    {
            ////        CondStr = item.Tag.ToString();
            ////    }
            ////}
            //CondStr.Replace("AND", "");
            //SearchStr += " " + CondStr + " ";
            //foreach (Fluent.ToggleButton item in tsgKind.Items)
            //{
            //    if (item.IsChecked == true)
            //    {
            //        if (item.Tag.ToString() == "kind:everything")
            //        {
            //            KindStr = item.Tag.ToString();
            //            break;
            //        }
            //        else
            //        {
            //            KindStr += item.Tag.ToString() + " " + CondStr + " ";
            //        }

            //    }
            //}

            //foreach (object item in tsgOther.Items)
            //{
            //    if (item is SplitButton)
            //    {
            //        if ((item as SplitButton).IsChecked)
            //        {
            //            foreach (object obj in (item as SplitButton).Items)
            //            {
            //                if (obj is MenuItem)
            //                {
            //                    if ((obj as MenuItem).IsChecked)
            //                    {
            //                        OtherStr += (obj as MenuItem).Tag.ToString() + " " + CondStr + " ";
            //                    }
            //                }

            //            }
            //        }
            //    }
            //}
            //SearchStr += KindStr;
            //SearchStr += OtherStr;

            //DoSearch(SearchStr);

            DoSearch(edtSearchBox.FullSearchTerms);
        }

        private void searchTextBox1_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
        }

        private void searchTextBox1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void edtSearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (e.Key == Key.Enter)
            {
                DoSearch(edtSearchBox.FullSearchTerms);
            }
        }

        private void edtSearchBox_BeginSearch_1(object sender, SearchRoutedEventArgs e)
        {
            DoSearch(edtSearchBox.FullSearchTerms);
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            edtSearchBox.ModifiedCondition = (string)((FrameworkElement)sender).Tag;
            dmsplit.IsChecked = true;
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            edtSearchBox.DateCondition = (string)((FrameworkElement)sender).Tag;
            dcsplit.IsChecked = true;
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ((Fluent.ToggleButton)sender).IsChecked = true;
            edtSearchBox.KindCondition = (string)((FrameworkElement)sender).Tag;
            edtSearchBox.Focus();
        }

        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            edtSearchBox.SizeCondition = (string)((FrameworkElement)sender).Tag;
            scSize.IsChecked = true;
        }

        private string GetValueOnly(string property, string value)
        {
            return value.Substring(property.Length + 1);
        }

        private void ToggleButton_Click_1(object sender, RoutedEventArgs e)
        {
            StringSearchCriteriaDialog dat = new StringSearchCriteriaDialog("ext", edtSearchBox.ExtensionCondition, FindResource("btnExtCP") as string);
            dat.ShowDialog();
            if (dat.Confirm == true)
            {
                edtSearchBox.ExtensionCondition = "ext:" + dat.textBox1.Text;
                if (dat.textBox1.Text.Length > 0)
                {
                    ExtToggle.IsChecked = true;
                }
                else
                {
                    ExtToggle.IsChecked = false;
                }
            }
            else
            {
                if (GetValueOnly("ext", edtSearchBox.ExtensionCondition).Length > 0)
                {
                    ExtToggle.IsChecked = true;
                }
                else
                {
                    ExtToggle.IsChecked = false;
                }
            }
        }

        private void edtSearchBox_FiltersCleared(object sender, EventArgs e)
        {
            scSize.IsChecked = false;
            foreach (object item in scSize.Items)
            {
                try
                {
                    (item as MenuItem).IsChecked = false;
                }
                catch
                {

                }
            }
            ExtToggle.IsChecked = false;
            AuthorToggle.IsChecked = false;
            SubjectToggle.IsChecked = false;
            dcsplit.IsChecked = false;
            foreach (object item in dcsplit.Items)
            {
                try
                {
                    (item as MenuItem).IsChecked = false;
                }
                catch
                {

                }
            }
            dmsplit.IsChecked = false;
            foreach (object item in dmsplit.Items)
            {
                try
                {
                    (item as MenuItem).IsChecked = false;
                }
                catch
                {

                }
            }
        }

        private void AuthorToggle_Click(object sender, RoutedEventArgs e)
        {
            StringSearchCriteriaDialog dat = new StringSearchCriteriaDialog("author", edtSearchBox.AuthorCondition, FindResource("btnAuthorCP") as string);
            dat.ShowDialog();
            if (dat.Confirm == true)
            {
                edtSearchBox.AuthorCondition = "author:" + dat.textBox1.Text;
                if (dat.textBox1.Text.Length > 0)
                {
                    AuthorToggle.IsChecked = true;
                }
                else
                {
                    AuthorToggle.IsChecked = false;
                }
            }
            else
            {
                if (GetValueOnly("author", edtSearchBox.AuthorCondition).Length > 0)
                {
                    AuthorToggle.IsChecked = true;
                }
                else
                {
                    AuthorToggle.IsChecked = false;
                }
            }
        }

        private void SubjectToggle_Click(object sender, RoutedEventArgs e)
        {
            StringSearchCriteriaDialog dat = new StringSearchCriteriaDialog("subject", edtSearchBox.SubjectCondition, FindResource("btnSubjectCP") as string);
            dat.ShowDialog();
            if (dat.Confirm == true)
            {
                edtSearchBox.SubjectCondition = "subject:" + dat.textBox1.Text;
                if (dat.textBox1.Text.Length > 0)
                {
                    SubjectToggle.IsChecked = true;
                }
                else
                {
                    SubjectToggle.IsChecked = false;
                }
            }
            else
            {
                if (GetValueOnly("subject", edtSearchBox.SubjectCondition).Length > 0)
                {
                    SubjectToggle.IsChecked = true;
                }
                else
                {
                    SubjectToggle.IsChecked = false;
                }
            }
        }

        private void miCustomSize_Click(object sender, RoutedEventArgs e)
        {
            SizeSearchCriteriaDialog dat = new SizeSearchCriteriaDialog();
            string sd = GetValueOnly("size", edtSearchBox.SizeCondition);
            dat.curval.Text = sd;

            dat.ShowDialog();

            if (dat.Confirm == true)
            {
                edtSearchBox.SizeCondition = "size:" + dat.GetSizeQuery();

                if (dat.GetSizeQuery().Length > 0)
                {
                    scSize.IsChecked = true;
                }
                else
                {
                    scSize.IsChecked = false;
                }
            }
            else
            {
                if (edtSearchBox.SizeCondition.Length > 5)
                {
                    scSize.IsChecked = true;
                }
                else
                {
                    scSize.IsChecked = false;
                }
            }
        }

        private void edtSearchBox_RequestCriteriaChange(object sender, SearchRoutedEventArgs e)
        {
            if (e.SearchTerms.StartsWith("author:"))
            {
                AuthorToggle_Click(sender, new RoutedEventArgs(e.RoutedEvent));
            }
            else
            {
                if (e.SearchTerms.StartsWith("ext:"))
                {
                    ToggleButton_Click_1(sender, new RoutedEventArgs(e.RoutedEvent));
                }
                else
                {
                    if (e.SearchTerms.StartsWith("subject:"))
                    {
                        SubjectToggle_Click(sender, new RoutedEventArgs(e.RoutedEvent));
                    }
                    else
                    {
                        if (e.SearchTerms.StartsWith("size:"))
                        {
                            miCustomSize_Click(sender, new RoutedEventArgs(e.RoutedEvent));
                        }
                        else
                        {
                            if (e.SearchTerms.StartsWith("date:"))
                            {
                                dcCustomTime_Click(sender, new RoutedEventArgs(e.RoutedEvent));
                            }
                            else
                            {
                                if (e.SearchTerms.StartsWith("modified:"))
                                {
                                    dmCustomTime_Click(sender, new RoutedEventArgs(e.RoutedEvent));
                                }
                                else
                                {
                                    MessageBox.Show("You have discovered an error in this program. Please tell us which filter you were trying to edit and any other details we should know. \r\n\r\nYour filter: " + e.SearchTerms, "Oops! Found a Bug!", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            //MessageBox.Show("Editor not available or invalid parameter \n\r \r\n" + e.SearchTerms, "Search Criteria Editor", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void dcCustomTime_Click(object sender, RoutedEventArgs e)
        {
      SDateSearchCriteriaDialog star = new SDateSearchCriteriaDialog(FindResource("btnODateCCP") as string);

            star.DateCriteria = GetValueOnly("date", edtSearchBox.DateCondition);
            //star.textBlock1.Text = "Set Date Created Filter";
            star.ShowDialog();

            if (star.Confirm == true)
            {
                edtSearchBox.DateCondition = "date:" + star.DateCriteria;
            }

            if (edtSearchBox.UseDateCondition == true)
            {
                dcsplit.IsChecked = true;
            }
            else
            {
                dcsplit.IsChecked = false;
            }
        }

        private void dmCustomTime_Click(object sender, RoutedEventArgs e)
        {
            SDateSearchCriteriaDialog star = new SDateSearchCriteriaDialog(FindResource("btnODateModCP") as string);

            star.DateCriteria = GetValueOnly("modified", edtSearchBox.ModifiedCondition);
            //star.textBlock1.Text = "Set Date Modified Filter";
            star.ShowDialog();

            if (star.Confirm == true)
            {
                edtSearchBox.ModifiedCondition = "modified:" + star.DateCriteria;
            }

            if (edtSearchBox.UseModifiedCondition == true)
            {
                dmsplit.IsChecked = true;
            }
            else
            {
                dmsplit.IsChecked = false;
            }
        }

        #endregion

        #region AutoSwitch

        private void chkFolder_Checked(object sender, RoutedEventArgs e)
        {
            asFolder = true;
        }

        private void chkFolder_Unchecked(object sender, RoutedEventArgs e)
        {
            asFolder = false;
        }

        private void chkDrive_Checked(object sender, RoutedEventArgs e)
        {
            asDrive = true;
        }

        private void chkDrive_Unchecked(object sender, RoutedEventArgs e)
        {
            asDrive = false;
        }

        private void chkArchive_Checked(object sender, RoutedEventArgs e)
        {
            asArchive = true;
        }

        private void chkArchive_Unchecked(object sender, RoutedEventArgs e)
        {
            asArchive = false;
        }

        private void chkApp_Checked(object sender, RoutedEventArgs e)
        {
            asApplication = true;
        }

        private void chkApp_Unchecked(object sender, RoutedEventArgs e)
        {
            asApplication = false;
        }

        private void chkImage_Checked(object sender, RoutedEventArgs e)
        {
            asImage = true;
        }

        private void chkImage_Unchecked(object sender, RoutedEventArgs e)
        {
            asImage = false;
        }

        private void chkLibrary_Checked(object sender, RoutedEventArgs e)
        {
            asLibrary = true;
        }

        private void chkLibrary_Unchecked(object sender, RoutedEventArgs e)
        {
            asLibrary = false;
        }


        private void chkVirtualTools_Checked(object sender, RoutedEventArgs e)
        {
            asVirtualDrive = true;
        }

        private void chkVirtualTools_Unchecked(object sender, RoutedEventArgs e)
        {
            asVirtualDrive = false;
        }

        #endregion

        #region Tabs

        private void ConstructMoveToCopyToMenu()
        {
            btnMoveto.Items.Clear();
            btnCopyto.Items.Clear();
            MenuItem OtherLocationMove = new MenuItem();
            OtherLocationMove.Focusable = false;
            OtherLocationMove.Header = FindResource("miOtherDestCP");
            OtherLocationMove.Click += new RoutedEventHandler(btnmtOther_Click);
            MenuItem OtherLocationCopy = new MenuItem();
            OtherLocationCopy.Focusable = false;
            OtherLocationCopy.Header = FindResource("miOtherDestCP");
            OtherLocationCopy.Click += new RoutedEventHandler(btnctOther_Click);

            MenuItem mimDesktop = new MenuItem();
            ShellObject sod = (ShellObject)KnownFolders.Desktop;
            sod.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
            sod.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
            mimDesktop.Focusable = false;
            mimDesktop.Icon = sod.Thumbnail.BitmapSource;
            mimDesktop.Header = FindResource("btnctDesktopCP");
            mimDesktop.Click += new RoutedEventHandler(btnmtDesktop_Click);
            MenuItem micDesktop = new MenuItem();
            micDesktop.Focusable = false;
            micDesktop.Icon = sod.Thumbnail.BitmapSource;
            micDesktop.Header = FindResource("btnctDesktopCP");
            micDesktop.Click += new RoutedEventHandler(btnctDesktop_Click);
            sod.Dispose();

      MenuItem mimDocuments = new MenuItem();
      MenuItem micDocuments = new MenuItem();
      try
      {
        ShellObject sodc = (ShellObject)KnownFolders.Documents;
        sodc.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
        sodc.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
        mimDocuments.Focusable = false;
        mimDocuments.Icon = sodc.Thumbnail.BitmapSource;
        mimDocuments.Header = FindResource("btnctDocumentsCP");
        mimDocuments.Click += new RoutedEventHandler(btnmtDocuments_Click);
        
        micDocuments.Focusable = false;
        micDocuments.Icon = sodc.Thumbnail.BitmapSource;
        micDocuments.Header = FindResource("btnctDocumentsCP");
        micDocuments.Click += new RoutedEventHandler(btnctDocuments_Click);
        sodc.Dispose();
      }
      catch (Exception)
      {
        mimDocuments = null;
        micDocuments = null;
        //ctach the exception in case the user deleted that basic folder somehow
      }

      MenuItem mimDownloads = new MenuItem();
      MenuItem micDownloads = new MenuItem();
      try
      {
        
        ShellObject sodd = (ShellObject)KnownFolders.Downloads;
        sodd.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
        sodd.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
        mimDownloads.Focusable = false;
        mimDownloads.Icon = sodd.Thumbnail.BitmapSource;
        mimDownloads.Header = FindResource("btnctDownloadsCP");
        mimDownloads.Click += new RoutedEventHandler(btnmtDounloads_Click);
        
        micDownloads.Focusable = false;
        micDownloads.Icon = sodd.Thumbnail.BitmapSource;
        micDownloads.Header = FindResource("btnctDownloadsCP");
        micDownloads.Click += new RoutedEventHandler(btnctDounloads_Click);
        sodd.Dispose();
      }
      catch (Exception)
      {
        micDownloads = null;
        mimDownloads = null;
        //ctach the exception in case the user deleted that basic folder somehow
      }

      if (mimDocuments != null)
              btnMoveto.Items.Add(mimDocuments);

      if (mimDownloads != null)
              btnMoveto.Items.Add(mimDownloads);

            btnMoveto.Items.Add(mimDesktop);
            btnMoveto.Items.Add(new Separator());

      if (micDocuments != null)
              btnCopyto.Items.Add(micDocuments);

      if (micDownloads != null)
              btnCopyto.Items.Add(micDownloads);

            btnCopyto.Items.Add(micDesktop);
            btnCopyto.Items.Add(new Separator());

            foreach (var item in tabControl1.Items.OfType<ClosableTabItem>().ToList())
            {
                bool IsAdditem = true;
                foreach (object mii in btnCopyto.Items)
                {
                    if (mii is MenuItem)
                    {
                        if ((mii as MenuItem).Tag != null)
                        {
                            if (((mii as MenuItem).Tag as ShellObject) == item.Path)
                            {
                                IsAdditem = false;
                                break;
                            }
                        }
                    }
                }
                if (IsAdditem && item.Path.IsFileSystemObject)
                {
                    try
                    {
                        MenuItem mim = new MenuItem();
                        mim.Header = item.Path.GetDisplayName(DisplayNameType.Default);
                        mim.Focusable = false;
                        mim.Tag = item.Path;
                        ShellObject so = ShellObject.FromParsingName(item.Path.ParsingName);
                        so.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                        so.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                        mim.Icon = so.Thumbnail.BitmapSource;
                        mim.Click += new RoutedEventHandler(mim_Click);
                        btnMoveto.Items.Add(mim);
                        MenuItem mic = new MenuItem();
                        mic.Focusable = false;
                        mic.Header = item.Path.GetDisplayName(DisplayNameType.Default);
                        mic.Tag = item.Path;
                        mic.Icon = so.Thumbnail.BitmapSource;
                        mic.Click += new RoutedEventHandler(mimc_Click);
                        btnCopyto.Items.Add(mic);
                        so.Dispose();
                    }
                    catch
                    {
                        //Do nothing if ShellObject is not available anymore and close the problematic item
                        CloseTab(item);
                    }
                }
            }
            btnMoveto.Items.Add(new Separator());
            btnMoveto.Items.Add(OtherLocationMove);
            btnCopyto.Items.Add(new Separator());
            btnCopyto.Items.Add(OtherLocationCopy);

        }

        void mimc_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            SetFOperation((mi.Tag as ShellObject), OperationType.Copy);
        }

        void mim_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            SetFOperation((mi.Tag as ShellObject), OperationType.Move);
        }

        public void NavigateAfterTabChange()
        {
            ClosableTabItem itb = tabControl1.SelectedItem as ClosableTabItem;

            isGoingBackOrForward = itb.log.HistoryItemsList.Count != 0;
            if (itb != null)
            {
                try
                {
                    BeforeLastTabIndex = LastTabIndex;

                    //tabControl1.SelectedIndex = itb.Index;
                    //LastTabIndex = itb.Index;
                    //CurrentTabIndex = LastTabIndex;
                    if (Explorer.NavigationLog.CurrentLocation == null || itb.Path.ParsingName != Explorer.NavigationLog.CurrentLocation.ParsingName)
                    {


                        if (!Keyboard.IsKeyDown(Key.Tab))
                        {
                            Explorer.Navigate(itb.Path);
                        }
                        else
                        {
                            t.Interval = 500;
                            t.Tag = itb.Path;
                            t.Tick += new EventHandler(t_Tick);
                            t.Start();
                        }

                    }

          itb.BringIntoView();
                    
                }
                catch (StackOverflowException)
                {

                }
                //'btnTabCloseC.IsEnabled = tabControl1.Items.Count > 1;
                //'there's a bug that has this enabled when there's only one tab open, but why disable it
                //'if it never crashes the program? Closing the last tab simply closes the program, so I
                //'thought, what the heck... let's just keep it enabled. :) -JaykeBird
            }
        }
        private void btnNewTab_Click(object sender, RoutedEventArgs e)
        {
            NewTab();
        }

        private void btnTabClone_Click(object sender, RoutedEventArgs e)
        {
      CloneTab(tabControl1.Items[tabControl1.SelectedIndex] as ClosableTabItem);
            
        }

        private void btnTabCloseC_Click(object sender, RoutedEventArgs e)
        {
      CloseTab(tabControl1.SelectedItem as ClosableTabItem);
        }

        //'close tab
        void cti_CloseTab(object sender, RoutedEventArgs e)
        {
      ClosableTabItem curItem = e.Source as ClosableTabItem;

            CloseTab(curItem);
        }

    public void CloneTab(ClosableTabItem CurTab)
        {
      ClosableTabItem newt = new ClosableTabItem();
            CreateTabbarRKMenu(newt);
            newt.Header = CurTab.Header;
            newt.TabIcon = CurTab.TabIcon;
            newt.Path = CurTab.Path;
      newt.ToolTip = CurTab.Path.ParsingName;
            newt.Index = tabControl1.Items.Count;
            newt.CloseTab += new RoutedEventHandler(newt_CloseTab);
            newt.DragEnter += new DragEventHandler(newt_DragEnter);
            newt.DragLeave += new DragEventHandler(newt_DragLeave);
            newt.DragOver += new DragEventHandler(newt_DragOver);
            newt.PreviewMouseMove += new MouseEventHandler(newt_PreviewMouseMove);
            newt.Drop += new DragEventHandler(newt_Drop);
            newt.TabSelected += newt_TabSelected;
            newt.AllowDrop = true;
            newt.log.CurrentLocation = CurTab.Path;
      newt.SelectedItems = CurTab.SelectedItems;
      newt.log.ImportData(CurTab.log);
            tabControl1.Items.Add(newt);
            tabControl1.SelectedItem = newt;
            LastTabIndex = tabControl1.SelectedIndex;
            ConstructMoveToCopyToMenu();
            NavigateAfterTabChange();
        }

        private void ERNewTab(object sender, ExecutedRoutedEventArgs e)
        {
            NewTab();
        }

        void SelectTab(int Index)
        {
            int selIndex = 0;
            if (tabControl1.SelectedIndex == tabControl1.Items.Count - 1)
            {
                selIndex = 0;
            }
            else
            {
                selIndex = Index;
            }
            tabControl1.SelectedItem = tabControl1.Items[selIndex];
            NavigateAfterTabChange();
        }

    void CloseTab(ClosableTabItem thetab, bool allowreopening = true)
        {
            if (tabControl1.SelectedIndex == 0 && tabControl1.Items.Count == 1)
            {
                if (this.IsCloseLastTabCloseApp)
                  Close();
                else {
                  Explorer.Navigate(this.GetShellObjectFromLocation(this.StartUpLocation));
                }
                return;
            }


            if (thetab.Index == 0 && tabControl1.Items.Count > 1)
            {
                tabControl1.SelectedIndex = thetab.Index + 1;
                tabControl1.Items.Remove(thetab);
            }
            else if (thetab.Index == tabControl1.Items.Count - 1)
            {
                tabControl1.SelectedIndex = thetab.Index - 1;
                tabControl1.Items.Remove(thetab);
            }
            else
            {

                for (int i = thetab.Index + 1; i < tabControl1.Items.Count; i++)
                {
          ClosableTabItem tab = tabControl1.Items[i] as ClosableTabItem;
                    tab.Index = tab.Index - 1;
                }
                tabControl1.Items.Remove(thetab);
            }
            
            ConstructMoveToCopyToMenu();

            if (allowreopening == true)
            {
                reopenabletabs.Add(thetab.log);
                btnUndoClose.IsEnabled = true;
        foreach (ClosableTabItem item in this.tabControl1.Items)
        {
            foreach (FrameworkElement m in item.mnu.Items)
            {
                if (m.Tag != null)
                {
                    if (m.Tag.ToString() == "UCTI")
                    {
                        (m as MenuItem).IsEnabled = true;
                    }
                }
            }
        }
            }


      ClosableTabItem itb = tabControl1.Items[tabControl1.SelectedIndex] as ClosableTabItem;

              isGoingBackOrForward = itb.log.HistoryItemsList.Count != 0;
            if (itb != null) {
              try {
                BeforeLastTabIndex = LastTabIndex;

                //tabControl1.SelectedIndex = itb.Index;
                //LastTabIndex = itb.Index;
                //CurrentTabIndex = LastTabIndex;
                if (itb.Path != Explorer.NavigationLog.CurrentLocation) {


                  if (!Keyboard.IsKeyDown(Key.Tab)) {
                    Explorer.Navigate(itb.Path);
                  } else {
                    t.Interval = 500;
                    t.Tag = itb.Path;
                    t.Tick += new EventHandler(t_Tick);
                    t.Start();
                  }

                }
              } catch (StackOverflowException) {

              }
        
                //'btnTabCloseC.IsEnabled = tabControl1.Items.Count > 1;
                //'there's a bug that has this enabled when there's only one tab open, but why disable it
                //'if it never crashes the program? Closing the last tab simply closes the program, so I
                //'thought, what the heck... let's just keep it enabled. :) -JaykeBird
            }

            NavigateAfterTabChange();
            
        }
        void newt_CloseTab(object sender, RoutedEventArgs e)
        {
      ClosableTabItem curItem = e.Source as ClosableTabItem;
            CloseTab(curItem);
        }

        /// <summary>
        /// Re-opens a previously closed tab using that tab's navigation log data.
        /// </summary>
        /// <param name="log">The navigation log data from the previously closed tab.</param>
        public void ReOpenTab(NavigationLog log)
        {
      ClosableTabItem newt = new ClosableTabItem();
            CreateTabbarRKMenu(newt);

            ShellObject DefPath = log.CurrentLocation;
            DefPath.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
            DefPath.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
            newt.Header = DefPath.GetDisplayName(DisplayNameType.Default);
            newt.TabIcon = DefPath.Thumbnail.BitmapSource;
            newt.Path = DefPath;
      newt.ToolTip = DefPath.ParsingName;
            newt.IsNavigate = false;
            newt.Index = tabControl1.Items.Count;
            newt.AllowDrop = true;
      newt.CloseTab += newt_CloseTab;
      newt.DragEnter += newt_DragEnter;
      newt.DragOver += newt_DragOver;
      newt.PreviewMouseMove += newt_PreviewMouseMove;
      newt.Drop += newt_Drop;
            newt.TabSelected += newt_TabSelected;
            tabControl1.Items.Add(newt);
            LastTabIndex = tabControl1.SelectedIndex;
            newt.log.ImportData(log);

            ConstructMoveToCopyToMenu();
            NavigateAfterTabChange();
        }

        System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
      try
      {
        if (e.RemovedItems.Count > 0)
        {
          var item = tabControl1.Items.OfType<ClosableTabItem>().Where(c => c == e.RemovedItems[0]).SingleOrDefault();
          if (item != null)
            item.SelectedItems = Explorer.SelectedItems.Select(c => c.ParsingName).ToArray();
        }
        if (e.AddedItems.Count > 0 && (e.AddedItems[0] as ClosableTabItem).Index == tabControl1.Items.Count - 1)
        {
          tabControl1.Items.OfType<ClosableTabItem>().Last().BringIntoView();
        }
      }
      catch (Exception ex)
      {
        //Catch eventual exceptions
        Exception lastException = ex;
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        logger.Warn(lastException);
      }
        }

        void t_Tick(object sender, EventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.Tab))
            {

                Explorer.Navigate((sender as System.Windows.Forms.Timer).Tag as ShellObject);
                WindowsAPI.SetFocus(Explorer.SysListViewHandle);
                (sender as System.Windows.Forms.Timer).Stop();
            }
        }

        private void tabControl1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
        CloneTab(tabControl1.SelectedItem as ClosableTabItem);
            }
        }

        private void RCloseTab(object sender, ExecutedRoutedEventArgs e)
        {
            if (tabControl1.SelectedIndex == 0 && tabControl1.Items.Count == 1)
            {
                Close();
                return;
            }
            int CurSelIndex = tabControl1.SelectedIndex;
            if (tabControl1.SelectedIndex == 0)
            {
                tabControl1.SelectedItem = tabControl1.Items[1];

            }
            else
            {
                tabControl1.SelectedItem = tabControl1.Items[CurSelIndex - 1];
            }


            tabControl1.Items.RemoveAt(CurSelIndex);
        }

        private void ChangeTab(object sender, ExecutedRoutedEventArgs e)
        {
            t.Stop();
            SelectTab(tabControl1.SelectedIndex + 1);
        }

        private void ERGoToBCCombo(object sender, ExecutedRoutedEventArgs e)
        {
            breadcrumbBarControl1.EnterEditMode();
        }


    void CreateTabbarRKMenu(ClosableTabItem tabitem)
        {

            tabitem.mnu = new ContextMenu();
            MenuItem miclosecurrentr = new MenuItem();
            miclosecurrentr.Header = "Close current tab";
            miclosecurrentr.Tag = tabitem;
            miclosecurrentr.Click += new RoutedEventHandler(miclosecurrentr_Click);
            tabitem.mnu.Items.Add(miclosecurrentr);

            MenuItem miclosealltab = new MenuItem();
            miclosealltab.Header = "Close all tabs";
            miclosealltab.Click += new RoutedEventHandler(miclosealltab_Click);
            tabitem.mnu.Items.Add(miclosealltab);

            MenuItem miclosealltabbd = new MenuItem();
            miclosealltabbd.Header = "Close all other tabs";
            miclosealltabbd.Tag = tabitem;
            miclosealltabbd.Click += new RoutedEventHandler(miclosealltabbd_Click);
            tabitem.mnu.Items.Add(miclosealltabbd);

            tabitem.mnu.Items.Add(new Separator());

            
            MenuItem minewtabr = new MenuItem();
            minewtabr.Header = "New tab";
            minewtabr.Tag = tabitem;
            minewtabr.Click += new RoutedEventHandler(minewtabr_Click);
            tabitem.mnu.Items.Add(minewtabr);

            
            MenuItem miclonecurrentr = new MenuItem();
            miclonecurrentr.Header = "Clone tab";
            miclonecurrentr.Tag = tabitem;
            miclonecurrentr.Click += new RoutedEventHandler(miclonecurrentr_Click);
            tabitem.mnu.Items.Add(miclonecurrentr);

            tabitem.mnu.Items.Add(new Separator());

            
            MenuItem miundocloser = new MenuItem();
            miundocloser.Header = "Undo close tab";
            if (btnUndoClose.IsEnabled == true)
            {
                miundocloser.IsEnabled = true;
            }
            else
            {
                miundocloser.IsEnabled = false;
            }
            miundocloser.Tag = "UCTI";
            miundocloser.Click += new RoutedEventHandler(miundocloser_Click);
            tabitem.mnu.Items.Add(miundocloser);

            tabitem.mnu.Items.Add(new Separator());

            MenuItem miopeninnew = new MenuItem();
            miopeninnew.Header = "Open in new window";
            miopeninnew.Tag = tabitem;
            miopeninnew.Click += new RoutedEventHandler(miopeninnew_Click);
          tabitem.mnu.Items.Add(miopeninnew);

        }

        void miopeninnew_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (sender as MenuItem);
      ClosableTabItem ti = mi.Tag as ClosableTabItem;
            Process.Start(Assembly.GetExecutingAssembly().GetName().Name, ti.Path.ParsingName + " /nw");
            CloseTab(ti);
            //throw new NotImplementedException();
        }

        void miclosealltabbd_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (sender as MenuItem);
      ClosableTabItem ti = mi.Tag as ClosableTabItem;
            CloseAllTabsButThis(ti);
        }

        void miclosealltab_Click(object sender, RoutedEventArgs e)
        {
            CloseAllTabs(true);
        }

        void miclosecurrentr_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (sender as MenuItem);
      ClosableTabItem ti = mi.Tag as ClosableTabItem;
            CloseTab(ti);
        }

        void minewtabr_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (sender as MenuItem);
            ClosableTabItem ti = mi.Tag as ClosableTabItem;
            NewTab();
        }

        void miclonecurrentr_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (sender as MenuItem);
            ClosableTabItem ti = mi.Tag as ClosableTabItem;
            CloneTab(ti);
        }

        void miundocloser_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (sender as MenuItem);
            
            if (btnUndoClose.IsEnabled == true)
            {
                btnUndoClose_Click(this, e);
            }
        }

        void CloseAllTabs(bool CloseFirstTab)
        {
      List<ClosableTabItem> tabs = new List<ClosableTabItem>();
            foreach (object item in tabControl1.Items)
            {
        ClosableTabItem it = item as ClosableTabItem;
                tabs.Add(it);
            }

            if (CloseFirstTab)
            {
        foreach (ClosableTabItem item in tabs)
                {

                    CloseTab(item);
                }
            }
            else
            {
        foreach (ClosableTabItem item in tabs)
                {

                    if (item.Index != 0)
                    {
                        CloseTab(item);
                    }

                }
            }

            tabs = null;

        }


    void CloseAllTabsButThis(ClosableTabItem tabitem)
        {
      List<ClosableTabItem> tabs = new List<ClosableTabItem>();
            foreach (object item in tabControl1.Items)
            {
        ClosableTabItem it = item as ClosableTabItem;
                tabs.Add(it);
            }


      foreach (ClosableTabItem item in tabs)
            {

                if (true)
                {
                    if (item != tabitem)
                    {
                        CloseTab(item);
                    }

                }

            }

            tabs = null;
            ConstructMoveToCopyToMenu();

        }

        public void NewTab(bool IsNavigate = true)
        {
            ClosableTabItem newt = new ClosableTabItem();
            CreateTabbarRKMenu(newt);

            ShellObject DefPath;
            if (StartUpLocation.IndexOf("::") == 0 && StartUpLocation.IndexOf(@"\") == -1)
                DefPath = ShellObject.FromParsingName("shell:" + StartUpLocation);
            else
                try
                {
                    DefPath = ShellObject.FromParsingName(StartUpLocation);
                }
                catch
                {
                    DefPath = (ShellObject)KnownFolders.Libraries;
                }
            DefPath.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
            DefPath.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
            newt.PreviewMouseMove += newt_PreviewMouseMove;
            newt.Header = DefPath.GetDisplayName(DisplayNameType.Default);
            newt.TabIcon = DefPath.Thumbnail.BitmapSource;
            newt.Path = DefPath;
      newt.ToolTip = DefPath.ParsingName;
            newt.IsNavigate = IsNavigate;
            newt.Index = tabControl1.Items.Count;
            newt.AllowDrop = true;
            newt.log.CurrentLocation = DefPath;

            newt.CloseTab += new RoutedEventHandler(newt_CloseTab);
            newt.DragEnter += new DragEventHandler(newt_DragEnter);
            newt.DragOver += new DragEventHandler(newt_DragOver);
            newt.PreviewMouseMove += new MouseEventHandler(newt_PreviewMouseMove);
            newt.Drop += new DragEventHandler(newt_Drop);
            newt.TabSelected += newt_TabSelected;
            tabControl1.Items.Add(newt);
            LastTabIndex = tabControl1.SelectedIndex;
            tabControl1.SelectedIndex = tabControl1.Items.Count - 1;
            tabControl1.SelectedItem = tabControl1.Items[tabControl1.Items.Count - 1];

            ConstructMoveToCopyToMenu();
            NavigateAfterTabChange();
        }

    public void NewTab(ShellObject location, bool IsNavigate = false) {
      ClosableTabItem newt = new ClosableTabItem();
      CreateTabbarRKMenu(newt);

      ShellObject DefPath = ShellObjectFactory.Create(location.PIDL);
      DefPath.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
      DefPath.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
      newt.PreviewMouseMove += newt_PreviewMouseMove;
      newt.Header = DefPath.GetDisplayName(DisplayNameType.Default);
      newt.TabIcon = DefPath.Thumbnail.BitmapSource;
      newt.Path = DefPath;
      newt.ToolTip = DefPath.ParsingName;
      newt.IsNavigate = IsNavigate;
      newt.Index = tabControl1.Items.Count;
      newt.AllowDrop = true;
      newt.log.CurrentLocation = DefPath;

      newt.CloseTab += new RoutedEventHandler(newt_CloseTab);
      newt.DragEnter += new DragEventHandler(newt_DragEnter);
      newt.DragOver += new DragEventHandler(newt_DragOver);
      newt.PreviewMouseMove += new MouseEventHandler(newt_PreviewMouseMove);
      newt.Drop += new DragEventHandler(newt_Drop);
      newt.TabSelected += newt_TabSelected;
      tabControl1.Items.Add(newt);
      LastTabIndex = tabControl1.SelectedIndex;
      if (IsNavigate) {
        tabControl1.SelectedIndex = tabControl1.Items.Count - 1;
        tabControl1.SelectedItem = tabControl1.Items[tabControl1.Items.Count - 1];
        NavigateAfterTabChange();
      }

      ConstructMoveToCopyToMenu();
    }



    public ClosableTabItem NewTab(string Location, bool IsNavigate = false)
        {
      ClosableTabItem newt = new ClosableTabItem();
            CreateTabbarRKMenu(newt);

      ShellObject DefPath = GetShellObjectFromLocation(Location);
            DefPath.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
            DefPath.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
            newt.Header = DefPath.GetDisplayName(DisplayNameType.Default);
            newt.TabIcon = DefPath.Thumbnail.BitmapSource;
            newt.Path = DefPath;
      newt.ToolTip = DefPath.ParsingName;
            newt.IsNavigate = IsNavigate;
            newt.Index = tabControl1.Items.Count;
            newt.AllowDrop = true;
            newt.CloseTab += new RoutedEventHandler(newt_CloseTab);
            newt.DragEnter += new DragEventHandler(newt_DragEnter);
            newt.DragOver += new DragEventHandler(newt_DragOver);
            newt.PreviewMouseMove += new MouseEventHandler(newt_PreviewMouseMove);
            newt.TabSelected += newt_TabSelected;
            newt.Drop += new DragEventHandler(newt_Drop);

            tabControl1.Items.Add(newt);
            LastTabIndex = tabControl1.SelectedIndex;
            if (IsNavigate)
            {
                //IsCancel = true;

                tabControl1.SelectedIndex = tabControl1.Items.Count - 1;
                tabControl1.SelectedItem = tabControl1.Items[tabControl1.Items.Count - 1];
                //IsCancel = false;
            }
            else
            {
                newt.log.CurrentLocation = DefPath;
            }

            ConstructMoveToCopyToMenu();
            return newt;
        }

        public void newt_TabSelected(object sender, RoutedEventArgs e)
        {
      ClosableTabItem itb = sender as ClosableTabItem;

                isGoingBackOrForward = itb.log.HistoryItemsList.Count != 0;
                if (itb != null)
                {
                    try
                    {
                        BeforeLastTabIndex = LastTabIndex;

                        //tabControl1.SelectedIndex = itb.Index;
                        //LastTabIndex = itb.Index;
                        //CurrentTabIndex = LastTabIndex;
                        if (itb.Path != Explorer.NavigationLog.CurrentLocation)
                        {


                            if (!Keyboard.IsKeyDown(Key.Tab))
                            {

                                Explorer.Navigate(itb.Path);
                            }
                            else
                            {
                                t.Interval = 500;
                                t.Tag = itb.Path;
                                t.Tick += new EventHandler(t_Tick);
                                t.Start();
                            }

                        }

            

            itb.BringIntoView();
                    }
                    catch (StackOverflowException)
                    {

                    }
                    //'btnTabCloseC.IsEnabled = tabControl1.Items.Count > 1;
                    //'there's a bug that has this enabled when there's only one tab open, but why disable it
                    //'if it never crashes the program? Closing the last tab simply closes the program, so I
                    //'thought, what the heck... let's just keep it enabled. :) -JaykeBird
                }

        Explorer.SetExplorerFocus();
        }

        void newt_PreviewMouseMove(object sender, MouseEventArgs e)
        {
      var tabItem = e.Source as ClosableTabItem;

            if (tabItem == null)
                return;

            if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
            }
        }

        void newt_Drop(object sender, DragEventArgs e)
        {

            e.Handled = true;
      var tabItemTarget = e.Source as ClosableTabItem;

      var tabItemSource = e.Data.GetData(typeof(ClosableTabItem)) as ClosableTabItem;
            if (tabItemSource != null)
            {
                if (!tabItemTarget.Equals(tabItemSource))
                {
                    var tabControl = tabItemTarget.Parent as TabControl;
                    int tabState = -1;
                    int sourceIndex = tabControl.Items.IndexOf(tabItemSource);
                    int targetIndex = tabControl.Items.IndexOf(tabItemTarget);
                    if (!tabItemSource.IsSelected && tabItemTarget.IsSelected)
                        tabState = 1;
                    else if (!tabItemSource.IsSelected && !tabItemTarget.IsSelected)
                        tabState = 2;
                    else
                        tabState = 0;
                    
                    tabControl.Items.Remove(tabItemSource);
                    tabControl.Items.Insert(targetIndex, tabItemSource);

                    
                    
                    tabControl.Items.Remove(tabItemTarget);
                    tabControl.Items.Insert(sourceIndex, tabItemTarget);
                    if (tabState == 1)
                        tabControl.SelectedIndex = sourceIndex;
                    else if (tabState == 0)
                        tabControl.SelectedIndex = targetIndex;

                }
            }
            else
            {

                System.Windows.Point pt = e.GetPosition(sender as IInputElement);

                //if (e.Data.GetDataPresent(DataFormats.FileDrop))
                //{
        if ((sender as ClosableTabItem).Path.IsFileSystemObject)
                {
                    if ((e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey)
                    {
                        e.Effects = DragDropEffects.Copy;
                        if (e.Data.GetDataPresent(DataFormats.FileDrop))
                        {
                            FileOperationsData PasteData = new FileOperationsData();
                            String[] collection = (String[])e.Data.GetData(DataFormats.FileDrop);
                            StringCollection scol = new StringCollection();
                            scol.AddRange(collection);
                            PasteData.DropList = scol;
              PasteData.PathForDrop = (sender as ClosableTabItem).Path.ParsingName;
                            Thread t = null;
                            t = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
                            t.SetApartmentState(ApartmentState.STA);
                            t.Start(PasteData);
                        }
                    }
                    else
                    {
                        //if (Path.GetPathRoot((sender as ClosableTabItem).Path.ParsingName) ==
                        //    Path.GetPathRoot(Explorer.NavigationLog.CurrentLocation.ParsingName))
                        //{
                        e.Effects = DragDropEffects.Move;
                        if (e.Data.GetDataPresent(DataFormats.FileDrop))
                        {
                            FileOperationsData PasteData = new FileOperationsData();
                            String[] collection = (String[])e.Data.GetData(DataFormats.FileDrop);
                            StringCollection scol = new StringCollection();
                            scol.AddRange(collection);
                            PasteData.DropList = scol;
              PasteData.PathForDrop = (sender as ClosableTabItem).Path.ParsingName;
                            Thread t = null;
                            t = new Thread(new ParameterizedThreadStart(Explorer.DoMove));
                            t.SetApartmentState(ApartmentState.STA);
                            t.Start(PasteData);
                        }

                        //}
                        //else
                        //{
                        //    e.Effects = DragDropEffects.Copy;
                        //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                        //    {
                        //        FileOperationsData PasteData = new FileOperationsData();
                        //        String[] collection = (String[])e.Data.GetData(DataFormats.FileDrop);
                        //        StringCollection scol = new StringCollection();
                        //        scol.AddRange(collection);
                        //        PasteData.DropList = scol;
                        //        PasteData.PathForDrop = (sender as ClosableTabItem).Path.ParsingName;
                        //        Thread t = null;
                        //        t = new Thread(new ParameterizedThreadStart(Explorer.DoCopy));
                        //        t.SetApartmentState(ApartmentState.STA);
                        //        t.Start(PasteData);
                        //    }

                        //}
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
                DropTargetHelper.Drop(e.Data, pt, e.Effects);
                // attempt at making drag-and-drop tabs a possibility
                //}
                //else
                //{
                //    if (e.Data.GetDataPresent(typeof(ClosableTabItem)))
                //    {
                //        var tabItemTarget = e.Source as ClosableTabItem;

                //        var tabItemSource = e.Data.GetData(typeof(ClosableTabItem)) as ClosableTabItem;
                //        if (!tabItemTarget.Equals(tabItemSource))
                //        {
                //            var tabControl = tabItemTarget.Parent as TabControl;
                //            int sourceIndex = tabControl.Items.IndexOf(tabItemSource);
                //            int targetIndex = tabControl.Items.IndexOf(tabItemTarget);

                //            tabControl.Items.Remove(tabItemSource);
                //            tabControl.Items.Insert(targetIndex, tabItemSource);

                //            tabControl.Items.Remove(tabItemTarget);
                //            tabControl.Items.Insert(targetIndex, tabItemTarget);
                //        }
                //    }
                //    else
                //    {

                //    }
                //}
            }
                
        }

        void newt_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;

      if ((sender as ClosableTabItem).Path.IsFileSystemObject)
            {
                if ((e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey)
                {
                    e.Effects = DragDropEffects.Copy;

                }
                else
                {
                    //if (Path.GetPathRoot((sender as ClosableTabItem).Path.ParsingName) ==
                    //    Path.GetPathRoot(Explorer.NavigationLog.CurrentLocation.ParsingName))
                    //{
                    //    e.Effects = DragDropEffects.Move;


                    //}
                    //else
                    //{
                    //    e.Effects = DragDropEffects.Copy;

                    //}

                    // I decided just to have it do a move because it will avoid errors with special shell folders.
                    // Besides, if a person wants to copy something, they should know how to press Ctrl to copy.
                    e.Effects = DragDropEffects.Move;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            Win32Point ptw = new Win32Point();
            GetCursorPos(ref ptw);
            //e.Handled = true;
      if (e.Data.GetType() != typeof(ClosableTabItem))
                DropTargetHelper.DragOver(new System.Windows.Point(ptw.X, ptw.Y), e.Effects);
        }

        void newt_DragLeave(object sender, DragEventArgs e)
        {
            DropTargetHelper.DragLeave();
        }

        void newt_DragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
      var tabItem = e.Source as ClosableTabItem;

            if (tabItem == null)
                return;

            //if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
            //{
            //    DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
            //}
            //if (e.Data.GetDataPresent(DataFormats.FileDrop))
            //{
      if ((sender as ClosableTabItem).Path.IsFileSystemObject)
                {
                    if ((e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey)
                    {
                        e.Effects = DragDropEffects.Copy;

                    }
                    else
                    {
                        //if (Path.GetPathRoot((sender as ClosableTabItem).Path.ParsingName) ==
                        //    Path.GetPathRoot(Explorer.NavigationLog.CurrentLocation.ParsingName))
                        //{
                        //    e.Effects = DragDropEffects.Move;


                        //}
                        //else
                        //{
                        //    e.Effects = DragDropEffects.Copy;

                        //}

                        // I decided just to have it do a move because it will avoid errors with special shell folders.
                        // Besides, if a person wants to copy something, they should know how to press Ctrl to copy.
                        e.Effects = DragDropEffects.Move;
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }


                Win32Point ptw = new Win32Point();
                GetCursorPos(ref ptw);
                e.Effects = DragDropEffects.None;
        var tabItemSource = e.Data.GetData(typeof(ClosableTabItem)) as ClosableTabItem;
                if (tabItemSource == null)
                    DropTargetHelper.DragEnter(this, e.Data, new System.Windows.Point(ptw.X, ptw.Y), e.Effects);
            //}
            //else
            //{
            //    if (e.Data.GetDataPresent(typeof(ClosableTabItem)))
            //    {
            //        e.Effects = DragDropEffects.Move;
            //    }
            //}

        }

        private void tabControl1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            string h = sender.GetType().Name.ToString();

            hitTestList = new List<DependencyObject>();

            System.Windows.Point pt = e.GetPosition(sender as IInputElement);

            VisualTreeHelper.HitTest(
                sender as Visual, null,
                CollectAllVisuals_Callback,
                new PointHitTestParameters(pt));

            hitTestList.Reverse();

            //DependencyObject elementToFind = null;
            if (hitTestList.Count() > 0)
            {
                if (hitTestList[0].GetType().Name.Equals("ScrollViewer") && hitTestList.Count == 2)
                {
                    NewTab();
                    if (StartUpLocation.IndexOf("::") == 0)
                    {

                        Explorer.Navigate(ShellObject.FromParsingName("shell:" + StartUpLocation));
                    }
                    else
                        Explorer.Navigate(ShellObject.FromParsingName(StartUpLocation.Replace("\"", "")));
          (tabControl1.SelectedItem as ClosableTabItem).Path = Explorer.NavigationLog.CurrentLocation;
                }
                else if (hitTestList.Count == 3)
                {
                    if (hitTestList[2].GetType().Name == "Grid")
                    {
                        NewTab();
                        if (StartUpLocation.IndexOf("::") == 0)
                        {

                            Explorer.Navigate(ShellObject.FromParsingName("shell:" + StartUpLocation));
                        }
                        else
                            Explorer.Navigate(ShellObject.FromParsingName(StartUpLocation.Replace("\"", "")));
            (tabControl1.SelectedItem as ClosableTabItem).Path = Explorer.NavigationLog.CurrentLocation;
                    }
                }
            }

        }

        #endregion

        #region Tab Controls

        private void MoveTabBarToBottom()
        {
            Grid.SetRow(this.tabControl1, 7);
            divNav.Visibility = Visibility.Visible;
            this.rTabbarTop.Height = new GridLength(0);
            this.rTabbarBot.Height = new GridLength(25);
            this.tabControl1.TabStripPlacement = Dock.Bottom;
        }

        private void MoveTabBarToTop()
        {
            Grid.SetRow(this.tabControl1, 3);
            divNav.Visibility = Visibility.Hidden;
            this.rTabbarTop.Height = new GridLength(25);
            this.rTabbarBot.Height = new GridLength(0);
            this.tabControl1.TabStripPlacement = Dock.Top;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            MoveTabBarToTop();
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            MoveTabBarToBottom();
        }

        public List<string> LoadListOfTabListFiles()
        {
            List<string> o = new List<string>();

            if (Directory.Exists(sstdir)) {
                foreach (string item in Directory.GetFiles(sstdir)) {
                ShellObject obj = ShellObject.FromParsingName(item);
                o.Add(RemoveExtensionsFromFile(obj.GetDisplayName(DisplayNameType.Default), GetExtension(item)));
                } 
            }
            return o;
        }

        private void btn_ToolTipOpening(object sender, ToolTipEventArgs e) {
            if (sender is SplitButton) {
            if ((sender as SplitButton).IsDropDownOpen) {
                e.Handled = true;
            }
            }
        }

        private void miSaveCurTabs_Click(object sender, RoutedEventArgs e)
        {
            List<ShellObject> objs = new List<ShellObject>();
            foreach (ClosableTabItem item in tabControl1.Items)
            {
                objs.Add(item.log.CurrentLocation);
            }
            String str = PathStringCombiner.CombinePaths(objs, "|");
            SavedTabsList list = SavedTabsList.CreateFromString(str);

            BetterExplorer.Tabs.NameTabList ntl = new BetterExplorer.Tabs.NameTabList();
            ntl.ShowDialog();
            if (ntl.dialogresult == true)
            {
                if (System.IO.Directory.Exists(sstdir) == false)
                {
                    System.IO.Directory.CreateDirectory(sstdir);
                }
                SavedTabsList.SaveTabList(list, String.Format("{0}{1}.txt", sstdir, ntl.textBox1.Text));
                if (!miTabManager.IsEnabled)
                    miTabManager.IsEnabled = true;
            }
        }

        private void btnUndoClose_Click(object sender, RoutedEventArgs e)
        {
            ReOpenTab(reopenabletabs[reopenabletabs.Count - 1]);
            reopenabletabs.RemoveAt(reopenabletabs.Count - 1);
            if (reopenabletabs.Count == 0)
            {
                btnUndoClose.IsEnabled = false;
        foreach (ClosableTabItem item in this.tabControl1.Items)
        {
            foreach (FrameworkElement m in item.mnu.Items)
            {
                if (m.Tag != null)
                {
                    if (m.Tag.ToString() == "UCTI")
                    {
                        (m as MenuItem).IsEnabled = false;
                    }
                }
            }
        }
            }
        }

        private void miClearUndoList_Click(object sender, RoutedEventArgs e)
        {
            reopenabletabs.Clear();
            btnUndoClose.IsDropDownOpen = false;
            btnUndoClose.IsEnabled = false;
      foreach (ClosableTabItem item in this.tabControl1.Items)
      {
        foreach (FrameworkElement m in item.mnu.Items)
        {
          if (m.Tag != null)
          {
            if (m.Tag.ToString() == "UCTI")
            {
                (m as MenuItem).IsEnabled = false;
            }
          }
        }
      }
        }

        private void btnUndoClose_DropDownOpened(object sender, EventArgs e)
        {
            rotGallery.Items.Clear();
            foreach (NavigationLog item in reopenabletabs)
            {
                UndoCloseGalleryItem gli = new UndoCloseGalleryItem();
                gli.LoadData(item);
        gli.Click += gli_Click;
                rotGallery.Items.Add(gli);
            }
        }

    void gli_Click(object sender, NavigationLogEventArgs e)
    {
      this.ReOpenTab(e.NavigationLog);
      reopenabletabs.Remove((sender as UndoCloseGalleryItem).nav);
      if (reopenabletabs.Count == 0)
      {
        btnUndoClose.IsEnabled = false;
      }
    }


        private void rotGallery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                (e.AddedItems[0] as UndoCloseGalleryItem).PerformClickEvent();
            }
            catch
            {

            }
        }

        private void btnChangeTabsFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog ctf = new CommonOpenFileDialog("Change Tab Folder");
            ctf.IsFolderPicker = true;
            ctf.Multiselect = false;
            ctf.InitialDirectory = new DirectoryInfo(sstdir).Parent.FullName;
            if (ctf.ShowDialog() == CommonFileDialogResult.Ok)
            {
                RegistryKey rk = Registry.CurrentUser;
                RegistryKey rks = rk.CreateSubKey(@"Software\BExplorer");
                rks.SetValue(@"SavedTabsDirectory", ctf.FileName + "\\");
                txtDefSaveTabs.Text = ctf.FileName + "\\";
                sstdir = ctf.FileName + "\\";
            }
        }

        private void tabControl1_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //MessageBox.Show("Dropped here!");
                String[] collection = (String[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string item in collection)
                {
                    ShellObject obj = ShellObject.FromParsingName(item);
                    if ((obj.IsFolder || obj.IsLink) && obj.IsFileSystemObject)
                    {
                        bool isarchive = false;
            string itemPath = String.Empty;
            if (obj.IsLink)
            {
              using (ShellLinkApi link = new ShellLinkApi(item))
              {
                itemPath = link.Target;
                ShellObject linkobj = ShellObject.FromParsingName(link.Target);
                if (!(linkobj.IsFolder && obj.IsFileSystemObject))
                  MessageBox.Show("Hey... this isn't a folder! We can't make a new tab out of this file.", "Attempt Failed", MessageBoxButton.OK, MessageBoxImage.Information);
              }
              
            }
            else
            {
              itemPath = item;
            }
                        foreach (string item2 in Archives)
                        {
                            if (itemPath.Contains(item2))
                            {
                                isarchive = true;
                            }
                        }

                        if (isarchive == false)
                        {
                            NewTab(itemPath);
                        }
                        else
                        {
                            MessageBox.Show("We see this is an archive. However, we're not able to open archives here. Try clicking on it and extracting it.", "Attempt Failed", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Hey... this isn't a folder! We can't make a new tab out of this file.", "Attempt Failed", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            else
            {
                if (e.Data.GetDataPresent(typeof(ClosableTabItem)))
                {

                }
                else
                {
                    MessageBox.Show("It appears that you tried to drag something to the blank area of the tab bar, and that something was not a folder. Drag a folder here to open it in a new tab.", "Attempt Failed", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void stGallery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                (e.AddedItems[0] as SavedTabsListGalleryItem).PerformClickEvent();
            }
            catch
            {

            }
        }

        private void btnSavedTabs_DropDownOpened(object sender, EventArgs e)
        {
            stGallery.Items.Clear();
            foreach (string item in LoadListOfTabListFiles())
            {
                SavedTabsListGalleryItem gli = new SavedTabsListGalleryItem(item);
        gli.Directory = sstdir;
        gli.Click += gli_Click;
        gli.SetUpTooltip((FindResource("tabTabsCP") as string));
                stGallery.Items.Add(gli);
            }
        }

        void gli_Click(object sender, PathStringEventArgs e)
        {
            SavedTabsList list = SavedTabsList.LoadTabList(String.Format("{0}{1}.txt", sstdir, e.PathString));
            for (int i = 0; i < list.Count; i++) {
                var tabitem = NewTab(list[i]);
                if (i == list.Count - 1)
                    tabControl1.SelectedItem = tabitem;
            }
            NavigateAfterTabChange();
        }

        private void miTabManager_Click(object sender, RoutedEventArgs e)
        {
      string sstdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\BExplorer_SavedTabs\\";
      if (Directory.Exists(sstdir))
      {
          BetterExplorer.Tabs.TabManager man = new Tabs.TabManager();
          man.MainForm = this;
          man.Show();
      }
        }

        private void RibbonWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (tabControl1.SelectedItem != null)
            {
                (tabControl1.SelectedItem as ClosableTabItem).BringIntoView();
            }
        }
        #endregion

        #region Customize Quick Access Toolbar

        public void OpenCustomizeQAT()
        {
            CustomizeQAT qal = new CustomizeQAT();
            qal.Owner = this;

            LoadInternalList();
            RefreshQATDialog(qal);

            qal.MainForm = this;
            qal.ShowDialog();
        }

        public void RefreshQATDialog(CustomizeQAT qal)
        {
            foreach (IRibbonControl item in GetNonQATButtons())
            {
                RibbonItemListDisplay rils = new RibbonItemListDisplay();
                if (item.Icon != null)
                {
                    rils.Icon = new BitmapImage(new Uri(@"/BetterExplorer;component/" +  item.Icon.ToString(), UriKind.Relative));
                }
                rils.Header = (item.Header as string);
                rils.SourceControl = item;
                rils.ItemName = (item as FrameworkElement).Name;
                rils.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                if (item is Fluent.DropDownButton || item is Fluent.SplitButton || item is Fluent.InRibbonGallery)
                {
                    rils.ShowMenuArrow = true;
                }
                qal.AllControls.Items.Add(rils);
            }

            foreach (IRibbonControl item in GetRibbonControlsFromNames(QatItems))
            {
                RibbonItemListDisplay rils = new RibbonItemListDisplay();
                if (item.Icon != null)
                {
                  rils.Icon = new BitmapImage(new Uri(@"/BetterExplorer;component/" + item.Icon.ToString(), UriKind.Relative));
                }
                rils.Header = (item.Header as string);
                rils.SourceControl = item;
                rils.ItemName = (item as FrameworkElement).Name;
                rils.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                if (item is Fluent.DropDownButton || item is Fluent.SplitButton || item is Fluent.InRibbonGallery)
                {
                    rils.ShowMenuArrow = true;
                }
                qal.QATControls.Items.Add(rils);
            }
        }

        #region Helper Functions

        public List<IRibbonControl> SortNames(List<IRibbonControl> items, List<string> headers)
        {
            List<IRibbonControl> rb = new List<IRibbonControl>();

            foreach (string item in headers)
            {
                bool found = false;
                foreach (IRibbonControl thing in items)
                {
                    if (found == false)
                    {
                        if (thing.Header as string == item)
                        {
                            rb.Add(thing);
                            found = true;
                        }
                    }
                }
            }

            return rb;
        }

        public List<string> GetNamesFromRibbonControls(List<IRibbonControl> input)
        {
            List<string> o = new List<string>();

            foreach (IRibbonControl item in input)
            {
                o.Add((item as FrameworkElement).Name);
            }

            return o;
        }

        public void LoadInternalList()
        {
          Dispatcher.BeginInvoke(DispatcherPriority.Render, (ThreadStart)(() =>
          {
            QatItems.Clear();
            QatItems = GetNamesFromRibbonControls(GetQATButtons());
          }));
        }

        public void PutItemsOnQAT(List<string> names)
        {
            this.TheRibbon.ClearQuickAccessToolBar();
            Dictionary<string, IRibbonControl> items = GetAllButtonsAsDictionary();
            foreach (string item in names)
            {
                IRibbonControl ctrl;
                if (items.TryGetValue(item, out ctrl))
                {
                    curitem = (ctrl as UIElement);
                    this.TheRibbon.AddToQuickAccessToolBar(ctrl as UIElement);
                    curitem = null;
                }

            }
            LoadInternalList();
        }

        #endregion

        #region Get Buttons Helper Functions

        public Dictionary<string, IRibbonControl> GetAllButtonsAsDictionary()
        {
            Dictionary<string, Fluent.IRibbonControl> rb = new Dictionary<string, Fluent.IRibbonControl>();

            foreach (RibbonTabItem item in TheRibbon.Tabs)
            {
                foreach (RibbonGroupBox itg in item.Groups)
                {
                    foreach (object ic in itg.Items)
                    {
                        if (ic is IRibbonControl)
                        {
                            rb.Add((ic as FrameworkElement).Name, (ic as IRibbonControl));
                        }
                    }
                }
            }



            return rb;
        }

        public Tuple<string, IRibbonControl> AddOtherButtonForDictionary(IRibbonControl item, Dictionary<string, IRibbonControl> dict)
        {
            Tuple<string, IRibbonControl> entry = new Tuple<string, IRibbonControl>((item as FrameworkElement).Name, item);
            dict.Add(entry.Item1, entry.Item2);
            return entry;
        }

        public Tuple<string, IRibbonControl> AddOtherButtonForLists(IRibbonControl item, List<IRibbonControl> rb, List<string> rs)
        {
            Tuple<string, IRibbonControl> entry = new Tuple<string, IRibbonControl>((item as FrameworkElement).Name, item);
            rb.Add(item);
            rs.Add(item.Header as string);
            return entry;
        }

        public Tuple<string, IRibbonControl> RemoveOtherButtonForLists(IRibbonControl item, List<IRibbonControl> rb, List<string> rs)
        {
            Tuple<string, IRibbonControl> entry = new Tuple<string, IRibbonControl>((item as FrameworkElement).Name, item);
            rb.Remove(item);
            rs.Remove(item.Header as string);
            return entry;
        }

        public List<Fluent.IRibbonControl> GetAllButtons()
        {
            List<Fluent.IRibbonControl> rb = new List<Fluent.IRibbonControl>();
            List<string> rs = new List<string>();

            foreach (RibbonTabItem item in TheRibbon.Tabs)
            {
                foreach (RibbonGroupBox itg in item.Groups)
                {
                    foreach (object ic in itg.Items)
                    {
                        if (ic is IRibbonControl)
                        {
                            rb.Add(ic as IRibbonControl);
                            rs.Add((ic as IRibbonControl).Header as string);
                        }
                    }
                }
            }

            rs.Sort();

            return SortNames(rb, rs);
        }

        public List<Fluent.IRibbonControl> GetNonQATButtons()
        {
            List<Fluent.IRibbonControl> rb = new List<Fluent.IRibbonControl>();
            List<string> rs = new List<string>();

            foreach (RibbonTabItem item in TheRibbon.Tabs)
            {
                foreach (RibbonGroupBox itg in item.Groups)
                {
                    foreach (object ic in itg.Items)
                    {
                        if (ic is IRibbonControl)
                        {
                            if (!(TheRibbon.IsInQuickAccessToolBar(ic as UIElement)))
                            {
                                rb.Add(ic as IRibbonControl);
                                rs.Add((ic as IRibbonControl).Header as string);
                            }
                        }
                    }
                }
            }

            rs.Sort();

            return SortNames(rb, rs);
        }

        public void AddOtherButton(IRibbonControl item, bool test, List<IRibbonControl> rb, List<string> rs)
        {
            if (TheRibbon.IsInQuickAccessToolBar(item as UIElement) == test)
            {
                rb.Add(item);
                rs.Add(item.Header as string);
            }
        }

        public List<Fluent.IRibbonControl> GetQATButtonsSorted()
        {
            List<Fluent.IRibbonControl> rb = new List<Fluent.IRibbonControl>();
            List<string> rs = new List<string>();

            foreach (RibbonTabItem item in TheRibbon.Tabs)
            {
                foreach (RibbonGroupBox itg in item.Groups)
                {
                    foreach (object ic in itg.Items)
                    {
                        if (ic is IRibbonControl)
                        {
                            if (TheRibbon.IsInQuickAccessToolBar(ic as UIElement))
                            {
                                rb.Add(ic as IRibbonControl);
                                rs.Add((ic as IRibbonControl).Header as string);
                            }
                        }
                    }
                }
            }

            rs.Sort();
            return SortNames(rb, rs);
        }

        public List<IRibbonControl> GetQATButtons()
        {
            List<Fluent.IRibbonControl> rb = new List<Fluent.IRibbonControl>();

            foreach (UIElement item in TheRibbon.QuickAccessToolbarItems.Keys)
            {
                rb.Add(item as IRibbonControl);
            }

            return rb;
        }

        public List<IRibbonControl> GetRibbonControlsFromNames(List<string> input)
        {
            List<Fluent.IRibbonControl> rb = new List<Fluent.IRibbonControl>();
            Dictionary<string, IRibbonControl> dic = GetAllButtonsAsDictionary();

            foreach (string name in input)
            {
                IRibbonControl ri;
                if (dic.TryGetValue(name, out ri))
                {
                    rb.Add(ri);
                }
                else
                {

                }
            }

            return rb;
        }

        #endregion

        #region Add/Remove Event Handlers

        private void TheRibbon_ItemAddedToQuickAccessToolbar(object sender, Ribbon.UIElementEventArgs e)
        {
            if (curitem == null)
            {
                Debug.WriteLine("Item being added: " + (e.Item as FrameworkElement).Name);
                QatItems.Add((e.Item as FrameworkElement).Name);
            }
        }

        private void TheRibbon_ItemRemovedToQuickAccessToolbar(object sender, Ribbon.UIElementEventArgs e)
        {
            if (curitem == null)
            {
                Debug.WriteLine("Item being removed: " + (e.Item as FrameworkElement).Name);
                QatItems.Remove((e.Item as FrameworkElement).Name);
            }
        }

        #endregion

        #endregion

        private void inRibbonGallery1_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
          IsViewSelection = true;
        }

        private void beNotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Visible;
            if (this.WindowState == WindowState.Minimized)
            {
                WindowsAPI.ShowWindow(Handle,
                    (int)WindowsAPI.ShowCommands.SW_RESTORE);
            }

            this.Activate();
            this.Topmost = true;  // important
            this.Topmost = false; // important
            this.Focus();         // important
        }

        #region Recycle Bin

        public void UpdateRecycleBinInfos()
        {
          var rb = (ShellContainer)KnownFolders.RecycleBin;
          var count = rb.Count();
          
          if (rb.Count() == 0)
          {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                 (ThreadStart)(() =>
                 {
                   miEmptyRB.Visibility = System.Windows.Visibility.Collapsed;
                   miRestoreALLRB.Visibility = System.Windows.Visibility.Collapsed;
                   miRestoreRBItems.Visibility = System.Windows.Visibility.Collapsed;
                   btnRecycleBin.LargeIcon = @"..\Images\RecycleBinEmpty32.png";
                   btnRecycleBin.Icon = @"..\Images\RecycleBinEmpty16.png";
                   lblRBItems.Text = "0 Items";
                   lblRBItems.Visibility = System.Windows.Visibility.Collapsed;
                   lblRBSize.Text = "0 bytes";
                   lblRBSize.Visibility = System.Windows.Visibility.Collapsed;
                 }));
          }
          else
          {
            var size = (long)rb.Where(c => c.IsFolder == false || c.Properties.System.FileExtension.Value != null).Sum(c => c.Properties.System.Size.Value == null ? 0 : (long)c.Properties.System.Size.Value);
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                 (ThreadStart)(() =>
                 {
                   miRestoreALLRB.Visibility = System.Windows.Visibility.Visible;
                   miEmptyRB.Visibility = System.Windows.Visibility.Visible;
                   btnRecycleBin.LargeIcon = @"..\Images\RecycleBinFull32.png";
                   btnRecycleBin.Icon = @"..\Images\RecycleBinFull16.png";
                   btnRecycleBin.UpdateLayout();
                   lblRBItems.Visibility = System.Windows.Visibility.Visible;
                   lblRBItems.Text = String.Format("{0} Items", count);
                   lblRBSize.Text = WindowsAPI.StrFormatByteSize(size);
                   lblRBSize.Visibility = System.Windows.Visibility.Visible;
                 }));
          }
        }

        private void miEmptyRB_Click(object sender, RoutedEventArgs e)
        {
            WindowsAPI.SHEmptyRecycleBin(this.Handle, string.Empty, 0);
            UpdateRecycleBinInfos();
        }

        private void miOpenRB_Click(object sender, RoutedEventArgs e)
        {
          Explorer.Navigate((ShellObject)KnownFolders.RecycleBin);
        }

        private void miRestoreRBItems_Click(object sender, RoutedEventArgs e)
        {
          foreach (ShellObject item in Explorer.SelectedItems.ToArray())
          {
            RestoreFromRB(item.Name);
          }
          UpdateRecycleBinInfos();
        }

        private bool RestoreAllFromRB()
        {
          var Shl = new Shell();
          Folder Recycler = Shl.NameSpace(10);

          for (int i = 0; i < Recycler.Items().Count; i++)
          {
            FolderItem FI = Recycler.Items().Item(i);
            DoVerb(FI, "ESTORE");
            return true;
          }
          return false;
        }

        private bool RestoreFromRB(string Item) {
          var Shl = new Shell();
          Folder Recycler = Shl.NameSpace(10);
          for (int i = 0; i < Recycler.Items().Count; i++) {
            FolderItem FI = Recycler.Items().Item(i);
            string FileName = Recycler.GetDetailsOf(FI, 0);
            if (Path.GetExtension(FileName) == "") FileName += Path.GetExtension(FI.Path);
            //Necessary for systems with hidden file extensions.
            string FilePath = Recycler.GetDetailsOf(FI, 1);
            if (Item == Path.Combine(FilePath, FileName)) {
              DoVerb(FI, "ESTORE");
              return true;
            }
          }
          return false;
        }
        private bool DoVerb(FolderItem Item, string Verb) {
          foreach (FolderItemVerb FIVerb in Item.Verbs()) {
            if (FIVerb.Name.ToUpper().Contains(Verb.ToUpper())) {
              FIVerb.DoIt();
              return true;
            }
          }
          return false;
        }

        private void miRestoreALLRB_Click(object sender, RoutedEventArgs e)
        {
          RestoreAllFromRB();
          UpdateRecycleBinInfos();
        }

        #endregion

        #region Networks and Accounts ("Sharing Options")

        private void btnAddWebServer_Click(object sender, RoutedEventArgs e)
        {
            Networks.AddServer asw = new Networks.AddServer();
            asw.Owner = this;
            asw.ShowDialog();
            if (asw.yep == true)
            {
                NetworkItem ni = asw.GetNetworkItem();
                nam.Add(ni);
                ServerItem ui = new ServerItem();
                ui.RequestRemove += ui_RequestRemove;
                ui.RequestEdit += ui_RequestEdit;
                ui.LoadFromNetworkItem(ni);
                //pnlServers.Children.Add(ui);
            }

        }

        void ui_RequestEdit(object sender, NetworkItemEventArgs e)
        {
            Networks.AddServer asw = new Networks.AddServer();
            asw.Owner = this;
            asw.ImportNetworkItem(e.NetworkItem);
            asw.ShowDialog();
            if (asw.yep == true)
            {
                nam.Remove(e.NetworkItem);
                //pnlServers.Children.Remove(sender as ServerItem);
                NetworkItem ni = asw.GetNetworkItem();
                nam.Add(ni);
                ServerItem ui = new ServerItem();
                ui.RequestRemove += ui_RequestRemove;
                ui.RequestEdit += ui_RequestEdit;
                ui.LoadFromNetworkItem(ni);
                //pnlServers.Children.Add(ui);
            }
        }

        void ui_RequestRemove(object sender, NetworkItemEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to remove this account?", "Remove Account", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                nam.Remove(e.NetworkItem);
                //pnlServers.Children.Remove(sender as ServerItem);
            }
        }

        private void btnAddStorageService_Click(object sender, RoutedEventArgs e)
        {
            AccountAuthWindow aaw = new AccountAuthWindow();
            aaw.LoadStorageServices();
            aaw.LoadSocialMediaServices();
            aaw.Owner = this;
            aaw.ShowDialog();
        }

        private void btnAddSocialMedia_Click(object sender, RoutedEventArgs e)
        {
            AccountAuthWindow aaw = new AccountAuthWindow();
            aaw.LoadSocialMediaServices();
            aaw.Owner = this;
            aaw.ShowDialog();
        }

        #endregion

        private void btnNewItem_DropDownOpened(object sender, EventArgs e)
        {

          ContextShellMenu mnu = new ContextShellMenu(Explorer, false);
          var controlPos = btnNewItem.TransformToAncestor(Application.Current.MainWindow)
                          .Transform(new System.Windows.Point(0, 0));
          var tempPoint = PointToScreen(new System.Windows.Point(controlPos.X, controlPos.Y));
          mnu.ShowContextMenu(new System.Drawing.Point((int)tempPoint.X, (int)tempPoint.Y + (int)btnNewItem.ActualHeight),true);
          btnNewItem.IsDropDownOpen = false;
        }

        private void mnuPinToStart_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.GetSelectedItemsCount() == 1)
            {
                string loc = KnownFolders.StartMenu.ParsingName + @"\" +
                    Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default) + ".lnk";
                ShellLinkApi link = new ShellLinkApi();
                link.DisplayMode = ShellLinkApi.LinkDisplayMode.edmNormal;
                link.Target = Explorer.SelectedItems[0].ParsingName;
                link.Save(loc);
                link.Dispose();

                WindowsAPI.PinUnpinToStartMenu(loc);
            }

            if (Explorer.GetSelectedItemsCount() == 0)
            {
                string loc = KnownFolders.StartMenu.ParsingName + @"\" +
                    Explorer.NavigationLog.CurrentLocation.GetDisplayName(DisplayNameType.Default) + ".lnk";
                ShellLinkApi link = new ShellLinkApi();
                link.DisplayMode = ShellLinkApi.LinkDisplayMode.edmNormal;
                link.Target = Explorer.NavigationLog.CurrentLocation.ParsingName;
                link.Save(loc);
                link.Dispose();

                WindowsAPI.PinUnpinToStartMenu(loc);
            }
        }

        void mln_Click(object sender, RoutedEventArgs e)
        {
            ShellLibrary lib = Explorer.CreateNewLibrary(Explorer.SelectedItems[0].GetDisplayName(DisplayNameType.Default));
            if (Explorer.SelectedItems[0].IsFolder)
            {
                lib.Add(Explorer.SelectedItems[0].ParsingName);
            }
        }

        void mli_Click(object sender, RoutedEventArgs e)
        {
          ShellLibrary lib = ShellLibrary.Load(((ShellObject)(sender as Fluent.MenuItem).Tag).GetDisplayName(DisplayNameType.Default),false);
            if (Explorer.SelectedItems[0].IsFolder)
            {
                lib.Add(Explorer.SelectedItems[0].ParsingName);
            }
        }

        private void btnEasyAccess_DropDownOpened(object sender, EventArgs e)
        {
            if (Explorer.SelectedItems.Count == 1)
            {
                mnuIncludeInLibrary.Items.Clear();

                foreach (ShellObject lib in (ShellContainer)KnownFolders.Libraries)
                {
                    Fluent.MenuItem mli = new MenuItem();
                    mli.Header = lib.GetDisplayName(DisplayNameType.Default);
                    lib.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);
                    mli.Icon = lib.Thumbnail.BitmapSource;
                    mli.Tag = ShellLibrary.FromParsingName(lib.ParsingName);
                    mli.Click += mli_Click;
                    mnuIncludeInLibrary.Items.Add(mli);
                }

                mnuIncludeInLibrary.Items.Add(new Separator());

                Fluent.MenuItem mln = new MenuItem();
                mln.Header = "Create new library";
                mln.Click += mln_Click;
                mnuIncludeInLibrary.Items.Add(mln);
            }
        }

        private void inRibbonGallery1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
          //switch (ViewGallery.SelectedIndex)
          //{
          //  case 0:
          //    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Thumbnail;
          //    Explorer.ContentOptions.ThumbnailSize = 256;
          //    break;
          //  case 1:
          //    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Thumbnail;
          //    Explorer.ContentOptions.ThumbnailSize = 96;
          //    break;
          //  case 2:
          //    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Icon;
          //    //Explorer.ContentOptions.ThumbnailSize = 64;
          //    break;
          //  case 3:
          //    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.SmallIcon;
          //    //Explorer.ContentOptions.ThumbnailSize = 48;
          //    break;
          //  case 4:

          //    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.List;
          //    break;
          //  case 5:

          //    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Details;
          //    break;
          //  case 6:

          //    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Tile;
          //    break;
          //  case 7:
          //    Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Content;
          //    break;
          //  default:
          //    break;
          //}
        }

        private void inRibbonGallery1_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void ExtraLarge_GalleryItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
          //Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.Thumbnail;
          //Explorer.ContentOptions.ThumbnailSize = 256;
        }

        private void GalleryItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
          //Explorer.ContentOptions.ViewMode = ExplorerBrowserViewMode.SmallIcon;
          //Explorer.ContentOptions.ThumbnailSize = 16;
        }

    }
}
