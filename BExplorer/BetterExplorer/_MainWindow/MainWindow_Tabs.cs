﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BExplorer.Shell;
using BExplorer.Shell.Interop;
using MenuItem = Fluent.MenuItem;

namespace BetterExplorer {

	partial class MainWindow {

		private void InitializeInitialTabs() {
			tcMain_Setup(null, null);

			var InitialTabs = Utilities.GetRegistryValue("OpenedTabs", "").ToString().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (InitialTabs.Length == 0 || !IsrestoreTabs) {
				var sho = new ShellItem(tcMain.StartUpLocation.ToShellParsingName());

				if (tcMain.Items.OfType<Wpf.Controls.TabItem>().Any())
					NavigationController(sho);
				else
					tcMain.NewTab(sho, true);
			}
			if (IsrestoreTabs) {
				isOnLoad = true;
				int i = 0;
				foreach (string str in InitialTabs) {
					try {
						i++;
						if (str.ToLowerInvariant() == "::{22877a6d-37a1-461a-91b0-dbda5aaebc99}") {
							continue;
						}

						tcMain.NewTab(ShellItem.ToShellParsingName(str), i == InitialTabs.Length);
						if (i == InitialTabs.Count()) {
							var sho = new ShellItem(str.ToShellParsingName());
							NavigationController(sho);
							(tcMain.SelectedItem as Wpf.Controls.TabItem).ShellObject = sho;
							(tcMain.SelectedItem as Wpf.Controls.TabItem).ToolTip = sho.ParsingName;
						}
					}
					catch {
						//AddToLog(String.Format("Unable to load {0} into a tab!", str));
						MessageBox.Show("BetterExplorer is unable to load one of the tabs from your last session. Your other tabs are perfectly okay though! \r\n\r\nThis location was unable to be loaded: " + str, "Unable to Create New Tab", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}

				if (tcMain.Items.Count == 0) {
					tcMain.NewTab();

					string idk = tcMain.StartUpLocation.StartsWith("::") ? tcMain.StartUpLocation.ToShellParsingName() : tcMain.StartUpLocation.Replace("\"", "");
					NavigationController(new ShellItem(idk));
					(tcMain.SelectedItem as Wpf.Controls.TabItem).ShellObject = ShellListView.CurrentFolder;
					(tcMain.SelectedItem as Wpf.Controls.TabItem).ToolTip = ShellListView.CurrentFolder.ParsingName;
				}

				isOnLoad = false;
			}
		}

		private void SelectTab(Wpf.Controls.TabItem tab) {
			if (tab == null) return;
			//tcMain.isGoingBackOrForward = tab.log.HistoryItemsList.Any();
			//if (!Keyboard.IsKeyDown(Key.Tab)) {
			if (tab.ShellObject != this.ShellListView.CurrentFolder || tab.ShellObject.IsSearchFolder) {
				tcMain.isGoingBackOrForward = true;
				NavigationController(tab.ShellObject);
        var selectedItem = tab;
        selectedItem.Header = this.ShellListView.CurrentFolder.DisplayName;
        selectedItem.Icon = this.ShellListView.CurrentFolder.Thumbnail.SmallBitmapSource;
        selectedItem.ShellObject = this.ShellListView.CurrentFolder;
        if (selectedItem != null) {
          var selectedPaths = selectedItem.SelectedItems;
          if (selectedPaths != null && selectedPaths.Count > 0) {
            foreach (var path in selectedPaths.ToArray()) {
              var sho = this.ShellListView.Items.Where(w => w.CachedParsingName == path).SingleOrDefault();
              if (sho != null) {
                var index = this.ShellListView.ItemsHashed[sho];
                this.ShellListView.SelectItemByIndex(index, true);
                selectedPaths.Remove(path);
              }
            }
          } else {
            this.ShellListView.ScrollToTop();
          }
        }
			}
			//}
			/*
			else {
				t.Interval = 500;
				t.Tag = tab.ShellObject;
				t.Tick += new EventHandler(t_Tick);
				t.Start();
			}
			*/
			//}
			//}
		}

		private void ConstructMoveToCopyToMenu() {
			btnMoveto.Items.Clear();
			btnCopyto.Items.Clear();

			var sod = (ShellItem)KnownFolders.Desktop;
			sod.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
			sod.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);

			var OtherLocationMove = Utilities.Build_MenuItem(FindResource("miOtherDestCP"), onClick: new RoutedEventHandler(btnmtOther_Click));
			var OtherLocationCopy = Utilities.Build_MenuItem(FindResource("miOtherDestCP"), onClick: new RoutedEventHandler(btnctOther_Click));
			var mimDesktop = Utilities.Build_MenuItem(FindResource("btnctDesktopCP"), icon: sod.Thumbnail.BitmapSource, onClick: new RoutedEventHandler(btnmtDesktop_Click));
			var micDesktop = Utilities.Build_MenuItem(FindResource("btnctDesktopCP"), icon: sod.Thumbnail.BitmapSource, onClick: new RoutedEventHandler(btnctDesktop_Click));

			MenuItem mimDocuments = new MenuItem(), micDocuments = new MenuItem();
			try {
				var sodc = (ShellItem)KnownFolders.Documents;
				sodc.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
				sodc.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);

				mimDocuments = Utilities.Build_MenuItem(FindResource("btnctDocumentsCP"), icon: sodc.Thumbnail.BitmapSource, onClick: new RoutedEventHandler(btnmtDocuments_Click));
				micDocuments = Utilities.Build_MenuItem(FindResource("btnctDocumentsCP"), icon: sodc.Thumbnail.BitmapSource, onClick: new RoutedEventHandler(btnctDocuments_Click));
			}
			catch (Exception) {
				mimDocuments = null;
				micDocuments = null;
				//catch the exception in case the user deleted that basic folder somehow
			}

			MenuItem mimDownloads = new MenuItem(), micDownloads = new MenuItem();
			try {
				var sodd = (ShellItem)KnownFolders.Downloads;
				sodd.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
				sodd.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);

				mimDownloads = Utilities.Build_MenuItem(FindResource("btnctDownloadsCP"), icon: sodd.Thumbnail.BitmapSource, onClick: new RoutedEventHandler(btnmtDounloads_Click));
				micDownloads = Utilities.Build_MenuItem(FindResource("btnctDownloadsCP"), icon: sodd.Thumbnail.BitmapSource, onClick: new RoutedEventHandler(btnctDounloads_Click));
			}
			catch (Exception) {
				micDownloads = null;
				mimDownloads = null;
				//catch the exception in case the user deleted that basic folder somehow
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

			foreach (var item in tcMain.Items.OfType<Wpf.Controls.TabItem>()) {
				bool IsAdditem = true;

				foreach (var mii in btnCopyto.Items.OfType<MenuItem>().Where(x => x.Tag != null)) {
					if ((mii.Tag as ShellItem) == item.ShellObject) {
						IsAdditem = false;
					}
				}

				if (IsAdditem && item.ShellObject.IsFileSystem) {
					try {
						var so = item.ShellObject;
						so.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
						so.Thumbnail.CurrentSize = new System.Windows.Size(16, 16);

						btnMoveto.Items.Add(Utilities.Build_MenuItem(item.ShellObject.CachedDisplayName, item.ShellObject,
																	 so.Thumbnail.BitmapSource, onClick: new RoutedEventHandler(mim_Click)));

						btnCopyto.Items.Add(Utilities.Build_MenuItem(item.ShellObject.CachedDisplayName, item.ShellObject, so.Thumbnail.BitmapSource));
					}
					catch {
						//Do nothing if ShellItem is not available anymore and close the problematic item
						//tcMain.RemoveTabItem(item);
					}
				}
			}

			btnMoveto.Items.Add(new Separator());
			btnMoveto.Items.Add(OtherLocationMove);
			btnCopyto.Items.Add(new Separator());
			btnCopyto.Items.Add(OtherLocationCopy);
		}


		private void tcMain_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e.RemovedItems.Count > 0) {

				var tab = e.RemovedItems[0] as Wpf.Controls.TabItem;

				if (tab != null && this.ShellListView.GetSelectedCount() > 0) {
					tab.SelectedItems = this.ShellListView.SelectedItems.Select(s => s.ParsingName).ToList();
				}
			}

			if (e.AddedItems.Count == 0) return;
      var newTab = e.AddedItems[0] as Wpf.Controls.TabItem;
      if (this.ShellListView.CurrentFolder != newTab.ShellObject && tcMain.CurrentTabItem == null) {
        SelectTab(newTab);
      } else {
        if (!tcMain.IsSelectionHandled) {
          SelectTab(newTab);

          //btnUndoClose
          btnUndoClose.Items.Clear();
          foreach (var item in tcMain.ReopenableTabs) {
            btnUndoClose.Items.Add(item.CurrentLocation);
          }
        } else {
          if (e.RemovedItems.Count == 0) {
            e.Handled = true;
            SelectTab(newTab);
            tcMain.SelectedItem = e.AddedItems[0];
          } else {
            if (e.RemovedItems[0] == tcMain.CurrentTabItem) {
              e.Handled = true;
              tcMain.IsSelectionHandled = false;
              tcMain.SelectedItem = e.RemovedItems[0];
              tcMain.CurrentTabItem = null;
            }
          }
        }
      }
      tcMain.IsSelectionHandled = false;
			this.ShellListView.Focus();
		}

		

	}
}