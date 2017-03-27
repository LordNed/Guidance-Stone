using GuidanceStone;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WArchiveTools;

namespace GuidanceStoneViewer.ViewModel
{
    public class MainWindowViewModel
    {
        public BLWP CurrentFile { get { return m_currentFile; } private set { m_currentFile = value; } }

        private BLWP m_currentFile;
        private string m_fileSavePath;

        private void OpenNewFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new InvalidOperationException("Attempted to open file from non-existant path!");

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            CurrentFile = new BLWP(fileName);
            using (var reader = FileUtilities.LoadFile(filePath))
            {
                CurrentFile.LoadFromStream(reader);
            }
        }

        private bool UserSaveChangesPrompt()
        {
            // If there's no current file, there's no changes to save and prompt them about.
            if (CurrentFile == null)
                return true;

            var results = MessageBox.Show("Save changes to the current file?", "Close File Confirmation", MessageBoxButton.YesNoCancel);

            switch(results)
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

            byte[] uncompressedFile = CurrentFile.SaveToMemory();
            GameFormatReader.Common.EndianBinaryWriter compressedFile;

            using (MemoryStream file = new MemoryStream(uncompressedFile))
            {
                compressedFile = WArchiveTools.Compression.Yaz0.Encode(file);
            }

            compressedFile.BaseStream.Position = 0;
            using (FileStream output = new FileStream(m_fileSavePath, FileMode.Create, FileAccess.Write))
            {
                compressedFile.BaseStream.CopyTo(output);
            }
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
            sfd.Filter = "Compressed BLWP File (.sblwp)|*.sblwp";

            var result = sfd.ShowDialog();
            if(result == true)
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
            if(CurrentFile != null)
            {
                bool wantsAction = CloseCurrentFileWithConfirm();
                if (!wantsAction)
                    return;
            }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = ".sblwp";
            ofd.Title = "Open BLWP File...";
            ofd.Filter = "Compressed BLWP File (.sblwp)|*.sblwp";

            var result = ofd.ShowDialog();
            if(result == true)
            {
                OpenNewFile(ofd.FileName);
            }
        }

        private void OnUserRequestNewFile()
        {
            if(CurrentFile != null)
            {
                bool wantsAction = CloseCurrentFileWithConfirm();
                if (!wantsAction)
                    return;
            }

            CurrentFile = new BLWP(string.Empty);
        }

        private void OnUserRequestExitApplication()
        {
            if(CurrentFile != null)
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
    }
}
