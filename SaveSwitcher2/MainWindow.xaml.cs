﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;
using SaveSwitcher2.Annotations;
using Path = System.IO.Path;

namespace SaveSwitcher2
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string GamePath { get; set; }
        public string SavePath { get; set; }

        public string InfoLabelText { get; set; }

        public Visibility ProgressBarVisibility { get; set; }

        public List<StoredSave> StoredSaves { get; set; }

        public StoredSave SelectedItem { get; set; }

        public bool ItemSelected
        {
            get
            {
                return SelectedItem != null;
            }
        }

        public bool InactiveItemSelected
        {
            get { return ItemSelected && !SelectedItem.Equals(ActiveSave); }
        }

        private bool _launchEnabled;

        public bool LaunchEnabled
        {
            get { return _launchEnabled && ActiveSave != null; }
            set { _launchEnabled = value; }
        }

        private StoredSave _activeSave;

        public StoredSave ActiveSave
        {
            get { return _activeSave; }
            set
            {
                _activeSave = value;
                //ActiveLabelText =  ((value != null) ? value.Name : "");
            }
        }

        public string ActiveLabelText
        {
            get { return ActiveSave != null ? ActiveSave.Name : ""; }
        }

        public string DialogLabelText { get; set; }

        public bool AutoSyncChecked { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LaunchGameEvent += OnLaunchGameEvent;
            ToggleProcess("Initializing", true);

            StoredSaves = FileService.LoadStoredSaves();

            string[] paths = FileService.readPath();

            GamePath = paths[0]; 
            SavePath = paths[1];
            

            LaunchEnabled = true;
            AutoSyncChecked = true;

            StoredSave storedActive = FileService.readActive();
            if (storedActive != null)
            {
                ActiveSave = StoredSaves.FirstOrDefault(x => x.Name.Equals(storedActive.Name));
                if (new DirectoryInfo(SavePath).LastWriteTime > ActiveSave.LastChangedDate)
                {
                    //MessageBox.Show(new DirectoryInfo(SavePath).LastWriteTime +" " + ActiveSave.LastChangedDate);
                    string tmpName = FileService.FindNewProfileName("Online_Save");
                    FileService.StoreSaveFile(SavePath, tmpName);
                    RefreshDataSet();
                    ActiveSave = StoredSaves.FirstOrDefault(x => x.Name.Equals(tmpName));
                    FileService.SaveActive(ActiveSave);

                    DialogName = tmpName.ToString();
                    _dialogBackupName = tmpName.ToString();
                    DialogSaveEnabled = false;
                    DialogLabelText =
                        "Active save seems to be newer than stored backup. \nProbably due to online synchronyzation. \nSelect a profile name for the found data \nor click away for automatic naming.";
                    IsDialogOpen = true;
                }
            }

            ToggleProcess();
        }

        #region launchgame

        

       
        private void Launch_OnClick(object sender, RoutedEventArgs e)
        {
            LaunchEnabled = false;
            ToggleProcess("Game running", true);
            InvokeLaunchGameEvent(EventArgs.Empty);
        }

        private async void OnLaunchGameEvent(object sender, EventArgs e)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GamePath,
                    WorkingDirectory = Path.GetDirectoryName(GamePath)
                }
            };
            try
            {
                process.Start();
                process.WaitForExit();
                if (AutoSyncChecked)
                {
                    ToggleProcess("Game closed. Synchronizing Backup.", true);
                    FileService.StoreSaveFile(SavePath, ActiveSave.Name);
                    RefreshDataSet();
                }else if(Boolean.Parse((string) await MaterialDesignThemes.Wpf.DialogHost.Show(new MessageContainer("Do you want to refresh the backup for profile '" + ActiveLabelText + "'? (overwrite)"), "YesNoDialog")))
                {

                }
                ToggleProcess();
            }
            catch (Exception ex)
            {
                ToggleProcess("ERROR, game .exe could not be found");
            }

            LaunchEnabled = true;
        }

        public event EventHandler<EventArgs> LaunchGameEvent;
        protected virtual void InvokeLaunchGameEvent(EventArgs e)
        {
            // Event will be null if there are no subscribers
            if (LaunchGameEvent != null)
            {
                LaunchGameEvent.Invoke(this,e);
            }
        }
        #endregion

        #region Fody
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region gamepaths
        private bool _pathChanged = false;
        private void GamePathTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _pathChanged = true;
        }

        private void SavePathTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _pathChanged = true;
        }

        private void PathTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (_pathChanged)
            {
                FileService.SavePath(GamePathTextBox.Text,SavePathTextBox.Text);
            }
            _pathChanged = false;
        }
#endregion


private void EditButton_OnClick(object sender, RoutedEventArgs e)
{
    DialogName = SelectedItem.Name.ToString();
    _dialogBackupName = SelectedItem.Name.ToString();
    DialogSaveEnabled = false;
    DialogLabelText = "Edit Profile: " + _dialogBackupName;
    IsDialogOpen = true;
}

private async void DeleteButton_OnClick(object sender, RoutedEventArgs e)
{
    string yesNoString = (string)await MaterialDesignThemes.Wpf.DialogHost.Show(new MessageContainer("Do you really want to delete profile '"+SelectedItem.Name+"' permanently?"), "YesNoDialog");
    if (Boolean.Parse(yesNoString))
    {
        ToggleProcess("Deleting profile " + SelectedItem.Name, true);
        FileService.DeleteSaveFile(SelectedItem.Name);
        RefreshDataSet();
            
        ToggleProcess();
    }
}

private void AddButton_OnClick(object sender, RoutedEventArgs e)
{
    DialogName = "";
    _dialogBackupName = null;
    DialogSaveEnabled = false;
    DialogLabelText = "New Profile";
            IsDialogOpen = true;
        }

private void StoredSavesDataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
{
    
}

private void LoadButton_OnClick(object sender, RoutedEventArgs e)
{
    ToggleProcess("Loading Profile "+SelectedItem.Name, true);
    FileService.LoadSaveFile(SavePath,SelectedItem.Name);
    FileService.SaveActive(SelectedItem);
    RefreshDataSet();
    ToggleProcess();
}
public bool IsDialogOpen { get; set; }

public string DialogName { get; set; }

private string _dialogBackupName;

public bool DialogContentChanged
{
    get
    {
        return _dialogBackupName != null ? !_dialogBackupName.Equals(DialogName) : true;
    }
}

public bool DialogSaveEnabled { get; set; }

private void DialogSaveButton_OnClick(object sender, RoutedEventArgs e)
        { 
    IsDialogOpen = false;
    FinishDialog(true);
}

private void DialogHost_OnDialogClosing(object sender, DialogClosingEventArgs eventargs)
{
FinishDialog();
}

private void FinishDialog(bool saving = false)
{
    if (saving)
    {
        ToggleProcess("Saving Profile " + DialogName,true);
        //store new data
        FileService.StoreSaveFile(SavePath,DialogName, _dialogBackupName);
        if (ActiveSave != null && ActiveSave.Name.Equals(_dialogBackupName))
        {
            ActiveSave.Name = DialogName;
            FileService.SaveActive(ActiveSave);
        }

        RefreshDataSet();
        ToggleProcess();
    }
    
    DialogName = null;
    _dialogBackupName = null;
}

private void RefreshDataSet()
{
    //refresh dataset
    StoredSaves = FileService.LoadStoredSaves();
    //refresh activesave variable 
    ActiveSave = null;
    StoredSave storedActive = FileService.readActive();
    if (storedActive != null)ActiveSave = StoredSaves.FirstOrDefault(x => x.Name.Equals(storedActive.Name));
        }

private void DialogNameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
{
    DialogSaveEnabled = !DialogNameTextBox.Text.Equals("") && !DialogNameTextBox.Text.Equals(_dialogBackupName) && (StoredSaves.FirstOrDefault(x => x.Name.Equals(DialogNameTextBox.Text)) == null);
    //For some reason property is correct in messagebox but not when being assigned.
    //DialogSaveEnabled = !DialogNameTextBox.Text.Equals("") && DialogContentChanged;
    //MessageBox.Show("" + DialogContentChanged);
}

private void ToggleProcess(string name = null, bool on = false)
{
    string finishedHeader = "Finished: ";
    string newName = name != null ? name : InfoLabelText;
    ProgressBarVisibility = on ? Visibility.Visible : Visibility.Collapsed;
    InfoLabelText = (on ? "" : finishedHeader) + newName.Replace(finishedHeader,"");
    ExtensionMethods.Refresh(ProgressBar);
    ExtensionMethods.Refresh(InfoLabel);
    ExtensionMethods.Refresh(ProcessPanel);
}
    }
}
