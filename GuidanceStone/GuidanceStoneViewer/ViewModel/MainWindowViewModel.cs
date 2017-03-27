using GuidanceStone;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WArchiveTools;

namespace GuidanceStoneViewer.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private const string kWindowTitle = "Guidance Stone: Static Mesh Instance Editor";

        public string WindowTitle
        {
            get
            {
                if (FileIsLoaded)
                    return $"{CurrentFile.FileName} - {kWindowTitle}";
                else
                    return kWindowTitle;
            }
        }

        public bool FileIsLoaded { get { return CurrentFile != null; } }
        public bool InstanceIsValid { get { return CurrentInstance != null; } }

        public BLWP CurrentFile
        {
            get { return m_currentFile; }
            private set
            {
                m_currentFile = value;

                if (CurrentFile != null && CurrentFile.ObjectInstances.Count > 0)
                    CurrentInstanceHeader = CurrentFile.ObjectInstances[0];
                else
                    CurrentInstance = null;

                OnPropertyChanged();
                OnPropertyChanged("FileIsLoaded");
                OnPropertyChanged("WindowTitle");
            }
        }

        public InstanceHeader CurrentInstanceHeader
        {
            get { return m_currentInstanceHeader; }
            set
            {
                m_currentInstanceHeader = value;
                if (m_currentInstanceHeader != null && CurrentInstanceHeader.Instances.Count > 0)
                    CurrentInstance = CurrentInstanceHeader.Instances[0];
                else
                    CurrentInstance = null;

                OnPropertyChanged();
            }
        }
        public Instance CurrentInstance
        {
            get { return m_currentInstance; }
            set
            {
                m_currentInstance = value;
                OnPropertyChanged();
                OnPropertyChanged("InstanceIsValid");
            }
        }

        private BLWP m_currentFile;
        private InstanceHeader m_currentInstanceHeader;
        private Instance m_currentInstance;

        private string m_fileSavePath;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel()
        {
            // Check to see if there's any file on the command line argument now that we've initialized, incase they opened via double clicking on a file.
            string[] cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length > 1)
            {
                OpenNewFile(cmdArgs[1]);
            }
        }

        private void OpenNewFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new InvalidOperationException("Attempted to open file from non-existant path!");

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            var newFile = new BLWP(fileName);
            using (var reader = FileUtilities.LoadFile(filePath))
            {
                newFile.LoadFromStream(reader);
            }

            // Wait until the BLWP is loaded before triggering the INotifyPropertyChanged.
            CurrentFile = newFile;
        }

        private bool UserSaveChangesPrompt()
        {
            // If there's no current file, there's no changes to save and prompt them about.
            if (CurrentFile == null)
                return true;

            var results = MessageBox.Show("Save changes to the current file?", "Close File Confirmation", MessageBoxButton.YesNoCancel);

            switch (results)
            {
                // Save their changes and then tell our caller they wish to continue the action.
                case MessageBoxResult.Yes:
                    SaveCurrentFile(m_fileSavePath);
                    return true;
                // Skip saving changes and then tell our caller they wish to continue the action.
                case MessageBoxResult.No:
                    return true;
                //  They don't want to perform our caller's action, oops!
                case MessageBoxResult.Cancel:
                    return false;
            }

            return true;
        }

        private void SaveCurrentFile(string filePath)
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("Attempted to save current file with no file loaded.");

            // Update our cached FilePath where they're saving the file.
            m_fileSavePath = filePath;
            CurrentFile.FileName = Path.GetFileName(m_fileSavePath);

            OnPropertyChanged("WindowTitle");

            byte[] uncompressedFile = CurrentFile.SaveToMemory();
            FileUtilities.SaveFile(filePath, new MemoryStream(uncompressedFile), ArchiveCompression.Yaz0);
        }

        private bool CloseCurrentFileWithConfirm()
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("Attempted to close current file with no file loaded.");

            bool userWantsAction = UserSaveChangesPrompt();

            // They either saved the file or don't care, go ahead and close the file.
            if (userWantsAction)
            {
                CurrentFile = null;
            }

            return userWantsAction;
        }

        private void OnUserRequestDeleteCurrentInstanceHeader()
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("Tried to delete current instance header but no file loaded!");
            if (CurrentInstanceHeader == null)
                throw new InvalidOperationException("Tried to delete current instance header, but there is none!");

            int oldIndex = CurrentFile.ObjectInstances.IndexOf(CurrentInstanceHeader);
            CurrentFile.ObjectInstances.Remove(CurrentInstanceHeader);

            // Set the previous one as selected (if any are remaining)
            oldIndex--;
            if (oldIndex < 0)
                oldIndex = 0;

            if (oldIndex >= 0 && CurrentFile.ObjectInstances.Count > 0)
                CurrentInstanceHeader = CurrentFile.ObjectInstances[oldIndex];
        }

        private void OnUserRequestAddNewInstanceHeaderCommand()
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("Tried to create new instance but no file loaded!");

            // Initialize a new InstanceHeader
            CurrentFile.ObjectInstances.Add(new InstanceHeader());

            // And then assign it as our selected one.
            CurrentInstanceHeader = CurrentFile.ObjectInstances[CurrentFile.ObjectInstances.Count - 1];
        }

        private void OnUserRequestDeleteCurrentInstance()
        {
            if (CurrentInstanceHeader == null)
                throw new InvalidOperationException("Tried to delete current instance header, but there is none!");

            if (CurrentInstance == null)
                throw new InvalidOperationException("Tried to delete current instance, but there is none!");

            int oldIndex = CurrentInstanceHeader.Instances.IndexOf(CurrentInstance);
            CurrentInstanceHeader.Instances.Remove(CurrentInstance);

            // Combobox bug workaround. 
            // ItemsSource doesn't update the labels on existing entries when ItemSource is modified.
            // Meaning it shows the old index of the instance which is now wrong! Doing this fixes that.
            var oldCurInstanceHeader = CurrentInstanceHeader;
            CurrentInstanceHeader = null;
            CurrentInstanceHeader = oldCurInstanceHeader;

            // Set the previous one as selected (if any are remaining)
            oldIndex--;
            if (oldIndex < 0)
                oldIndex = 0;

            if (oldIndex >= 0 && CurrentInstanceHeader.Instances.Count > 0)
                CurrentInstance = CurrentInstanceHeader.Instances[oldIndex];
        }

        private void OnUserRequestAddNewInstance()
        {
            if (CurrentInstanceHeader == null)
                throw new InvalidOperationException("Tried to create new instance but no current Instance Header!");

            // Initialize a new Instance
            CurrentInstanceHeader.Instances.Add(new Instance());

            // And then assign it as our selected one.
            CurrentInstance = CurrentInstanceHeader.Instances[CurrentInstanceHeader.Instances.Count - 1];
        }
        #region Commands
        // File Menu
        public ICommand NewFileCommand { get { return new RelayCommand(x => OnUserRequestNewFile()); } }
        public ICommand OpenFileCommand { get { return new RelayCommand(x => OnUserRequestOpenFile()); } }
        public ICommand SaveFileCommand { get { return new RelayCommand(x => OnUserRequestSaveFile(), x => CurrentFile != null); } }
        public ICommand SaveFileAsCommand { get { return new RelayCommand(x => OnUserRequestSaveFileAs(), x => CurrentFile != null); } }
        public ICommand CloseFileCommand { get { return new RelayCommand(x => OnUserRequestCloseFile(), x => CurrentFile != null); } }
        public ICommand ExitApplicationCommand { get { return new RelayCommand(x => OnUserRequestExitApplication()); } }

        // Help Menu
        public ICommand OpenReportABugCommand { get { return new RelayCommand(x => OnUserRequestReportBug()); } }
        public ICommand OpenWikiCommand { get { return new RelayCommand(x => OnUserRequestOpenWiki()); } }
        public ICommand OpenAboutDialogCommand { get { return new RelayCommand(x => OnUserRequestOpenAboutDialog()); } }

        // Buttons
        public ICommand DeleteCurrentInstanceHeaderCommand { get { return new RelayCommand(x => OnUserRequestDeleteCurrentInstanceHeader(), x => CurrentInstanceHeader != null); } }
        public ICommand AddNewInstanceHeaderCommand { get { return new RelayCommand(x => OnUserRequestAddNewInstanceHeaderCommand(), x=> CurrentFile != null); } }

        public ICommand DeleteCurrentInstanceCommand { get { return new RelayCommand(x => OnUserRequestDeleteCurrentInstance(), x => CurrentInstance != null); } }
        public ICommand AddNewInstanceCommand { get { return new RelayCommand(x => OnUserRequestAddNewInstance(), x => CurrentInstanceHeader != null); } }

        private void OnUserRequestCloseFile()
        {
            CloseCurrentFileWithConfirm();
        }

        private void OnUserRequestSaveFileAs()
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("Attempted to save current file as with no file loaded.");

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = CurrentFile.FileName;
            sfd.DefaultExt = ".sblwp";
            sfd.Title = "Save BLWP File As...";
            sfd.Filter = "Compressed BLWP File|*.sblwp|All Files|*.*";

            var result = sfd.ShowDialog();
            if (result == true)
            {
                string fileName = sfd.FileName;
                SaveCurrentFile(fileName);
            }
        }

        private void OnUserRequestSaveFile()
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("Attempted to save current file as with no file loaded.");

            // If this is a new file, use the Save As dialog flow
            if (string.IsNullOrEmpty(m_fileSavePath))
                OnUserRequestSaveFileAs();
            else
                SaveCurrentFile(m_fileSavePath);
        }

        private void OnUserRequestOpenFile()
        {
            if (CurrentFile != null)
            {
                bool wantsAction = CloseCurrentFileWithConfirm();
                if (!wantsAction)
                    return;
            }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = ".sblwp";
            ofd.Title = "Open BLWP File...";
            ofd.Filter = "Compressed BLWP File|*.sblwp|All Files|*.*";

            var result = ofd.ShowDialog();
            if (result == true)
            {
                OpenNewFile(ofd.FileName);
            }
        }

        private void OnUserRequestNewFile()
        {
            if (CurrentFile != null)
            {
                bool wantsAction = CloseCurrentFileWithConfirm();
                if (!wantsAction)
                    return;
            }

            CurrentFile = new BLWP(string.Empty);
        }

        private void OnUserRequestExitApplication()
        {
            if (CurrentFile != null)
            {
                bool wantsAction = CloseCurrentFileWithConfirm();
                if (!wantsAction)
                    return;
            }

            App.Current.Shutdown();
        }


        private void OnUserRequestReportBug()
        {
            System.Diagnostics.Process.Start("https://github.com/LordNed/Guidance-Stone/issues");
        }

        private void OnUserRequestOpenWiki()
        {
            System.Diagnostics.Process.Start("https://github.com/LordNed/Guidance-Stone/wiki");
        }

        private void OnUserRequestOpenAboutDialog()
        {
            throw new NotImplementedException();
        }
        #endregion

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
