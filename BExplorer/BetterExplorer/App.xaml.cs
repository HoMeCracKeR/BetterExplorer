﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using SingleInstanceApplication;
using System.Reflection;
using Microsoft.Win32;
using System.Windows.Threading;
using System.Threading;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using Fluent;
using System.Globalization;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Interop;

using BExplorer.Shell;
using BExplorer.Shell.Interop;

namespace BetterExplorer {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		public static bool isStartMinimized = false, /*isStartNewWindows = false,*/ isStartWithStartupTab = false;

		#region Culture

		/// <summary>
		/// Sets the UI language
		/// </summary>
		/// <param name="culture">Language code (ex. "en-EN")</param>
		public void SelectCulture(string culture) {
			if (culture == ":null:")
				culture = CultureInfo.InstalledUICulture.Name;
			// List all our resources      
			var dictionaryList = new List<ResourceDictionary>(Application.Current.Resources.MergedDictionaries);
			// We want our specific culture      
			string requestedCulture = string.Format("Locale.{0}.xaml", culture);
			ResourceDictionary resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == "/BetterExplorer;component/Translation/" + requestedCulture);
			if (resourceDictionary == null) {
				// If not found, we select our default language        
				//        
				requestedCulture = "DefaultLocale.xaml";
				resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == "/BetterExplorer;component/Translation/" + requestedCulture);
			}

			// If we have the requested resource, remove it from the list and place at the end.\      
			// Then this language will be our string table to use.      
			if (resourceDictionary != null) {
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
		public void SelectCulture(string culture, string filename) {
			if (culture == ":null:")
				culture = CultureInfo.InstalledUICulture.Name;
			// List all our resources      
			var dictionaryList = new List<ResourceDictionary>(Application.Current.Resources.MergedDictionaries);

			var resourceDictionary = Utilities.Load(filename);
			if (resourceDictionary == null) {
				// if not found, then try from the application's resources
				string requestedCulture = string.Format("Locale.{0}.xaml", culture);
				resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == "/BetterExplorer;component/Translation/" + requestedCulture);
				if (resourceDictionary == null) {
					// If not found, we select our default language        
					//        
					requestedCulture = "DefaultLocale.xaml";
					resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == "/BetterExplorer;component/Translation/" + requestedCulture);
				}
			}

			// If we have the requested resource, remove it from the list and place at the end.\      
			// Then this language will be our string table to use.      
			if (resourceDictionary != null) {
				try {
					Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
				}
				catch {

				}

				Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
			}
			// Inform the threads of the new culture      
			Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(culture);
			Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
		}

		#endregion

		#region UnhandledException

		void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
			var lastException = (Exception)e.ExceptionObject;
			var logger = NLog.LogManager.GetCurrentClassLogger();
			logger.Fatal(lastException);
		}

		void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
			var lastException = e.Exception;
			var logger = NLog.LogManager.GetCurrentClassLogger();
			logger.Fatal(lastException);

			e.Handled = true;
		}

		#endregion


		protected override void OnSessionEnding(SessionEndingCancelEventArgs e) {
			Application.Current.Shutdown();
			base.OnSessionEnding(e);
		}

		protected override void OnStartup(StartupEventArgs e) {
			string Locale = ""; bool dmi = true;
			Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(Current_DispatcherUnhandledException);
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
			//RegistryKey rk = Registry.CurrentUser;
			//RegistryKey rks = rk.OpenSubKey(@"Software\BExplorer", true);

			//// loads current Ribbon color theme
			//try {
			//	var Color = Convert.ToString(rks.GetValue("CurrentTheme", "Blue"));
			//	switch (Color) {
			//		case "Blue":
			//		case "Silver":
			//		case "Black":
			//		case "Green":
			//			Application.Current.Resources.MergedDictionaries.RemoveAt(0);
			//			Application.Current.Resources.MergedDictionaries.Insert(0, new ResourceDictionary() { Source = new Uri("pack://application:,,,/Fluent;component/Themes/Generic.xaml") });
			//			break;
			//		case "Metro":
			//			Application.Current.Resources.MergedDictionaries.RemoveAt(0);
			//			Application.Current.Resources.MergedDictionaries.Insert(0, new ResourceDictionary() { Source = new Uri("pack://application:,,,/Fluent;component/Themes/Office2013/Generic.xaml") });
			//			break;
			//		default:
			//			Application.Current.Resources.MergedDictionaries.RemoveAt(0);
			//			Application.Current.Resources.MergedDictionaries.Insert(0, new ResourceDictionary() { Source = new Uri("pack://application:,,,/Fluent;component/Themes/Generic.xaml") });
			//			break;
			//	}
			//} catch (Exception ex) {
			//	MessageBox.Show(String.Format("An error occurred while trying to load the theme data from the Registry. \n\r \n\r{0}\n\r \n\rPlease let us know of this issue at http://bugtracker.better-explorer.com/", ex.Message), "RibbonTheme Error - " + ex.ToString());
			//}
			//rks.Close();
			//rk.Close();
			if (e.Args != null && e.Args.Any()) {
				dmi = e.Args.Contains("/nw") || e.Args[0] == "t";
				isStartWithStartupTab = e.Args.Contains("/norestore");

				if (e.Args[0] != "-minimized")
					this.Properties["cmd"] = e.Args[0];
				else
					isStartMinimized = true;
			}

			
			if (dmi) {
				if (!ApplicationInstanceManager.CreateSingleInstance(Assembly.GetExecutingAssembly().GetName().Name, SingleInstanceCallback))
					return; // exit, if same app. is running
			}
			
			base.OnStartup(e);

			try {
				var regLocale = Utilities.GetRegistryValue("Locale", "").ToString();
				Locale = String.IsNullOrEmpty(regLocale) ? CultureInfo.CurrentUICulture.Name : regLocale;
				SelectCulture(Locale);
			}
			catch {
				MessageBox.Show(String.Format("A problem occurred while loading the locale from the Registry. This was the value in the Registry: \r\n \r\n {0}\r\n \r\nPlease report this issue at http://bugtracker.better-explorer.com/.", Locale));
				Shutdown();
			}
		}

		private void CreateInitialTab(MainWindow win, ShellItem sho) {
			sho.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
			sho.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);

			var newt = new Wpf.Controls.TabItem(sho);
			newt.Header = sho.GetDisplayName(SIGDN.NORMALDISPLAY);
			newt.Icon = sho.Thumbnail.BitmapSource;
			newt.PreviewMouseMove += newt_PreviewMouseMove;
			newt.ToolTip = sho.ParsingName;
			win.tcMain.CloneTabItem(newt);
		}

		/// <summary>
		/// Single instance callback handler.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The <see cref="SingleInstanceApplication.InstanceCallbackEventArgs"/> instance containing the event data.</param>
		/// 
		private void SingleInstanceCallback(object sender, InstanceCallbackEventArgs args) {
			if (args == null || Dispatcher == null) return;
			var StartUpLocation = Utilities.GetRegistryValue("StartUpLoc", KnownFolders.Libraries.ParsingName).ToString();

			Action<bool> d = (bool x) => {
				var win = MainWindow as MainWindow;

				if (!x) return;
				if (args == null || args.CommandLineArgs == null || !args.CommandLineArgs.Any()) return;
				if (win == null) return;
				if (args.CommandLineArgs[1] == "/nw") {
					var mainWin = new MainWindow();
					mainWin.IsMultipleWindowsOpened = true;
					mainWin.Show();
				}
				else {
					ShellItem sho = null;
					if (args.CommandLineArgs[1] == "t") {
						win.Visibility = Visibility.Visible;
						if (win.WindowState == WindowState.Minimized) {
							User32.ShowWindow((HwndSource.FromVisual(win) as HwndSource).Handle, User32.ShowWindowCommands.Restore);
						};

						sho = ShellItem.ToShellParsingName(StartUpLocation);
					}
					else {
						String cmd = args.CommandLineArgs[1];
						sho = ShellItem.ToShellParsingName(cmd);
					}

					if (!isStartMinimized || win.tcMain.Items.Count == 0) {
						CreateInitialTab(win, sho);
					}
					else if ((int)Utilities.GetRegistryValue("IsRestoreTabs", "1") == 0) {
						win.tcMain.Items.Clear();
						CreateInitialTab(win, sho);
					}
					else if (args.CommandLineArgs.Length > 1 && args.CommandLineArgs[1] != null) {
						String cmd = args.CommandLineArgs[1];
						sho = ShellItem.ToShellParsingName(cmd);
						CreateInitialTab(win, sho);
					}
				}
				win.Activate();
				win.Topmost = true;  // important
				win.Topmost = false; // important
				win.Focus();         // important				
			};
			Dispatcher.BeginInvoke(d, true);
		}

		void newt_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
			var tabItem = e.Source as Wpf.Controls.TabItem;

			if (tabItem == null)
				return;
			else if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
				DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
		}
	}
}
