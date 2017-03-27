using GuidanceStone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GuidanceStoneViewer.ViewModel
{
    public class MainWindowViewModel
    {
        public BLWP CurrentFile { get { return m_currentFile; } }

        private BLWP m_currentFile;

       

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
            throw new NotImplementedException();
        }

        private void OnUserRequestSaveFileAs()
        {
            throw new NotImplementedException();
        }

        private void OnUserRequestSaveFile()
        {
            throw new NotImplementedException();
        }

        private void OnUserRequestOpenFile()
        {
            throw new NotImplementedException();
        }

        private void OnUserRequestNewFile()
        {
            throw new NotImplementedException();
        }

        private void OnUserRequestExitApplication()
        {
            throw new NotImplementedException();
        }


        private void OnUserRequestReportBug()
        {
            throw new NotImplementedException();
        }

        private void OnUserRequestOpenWiki()
        {
            throw new NotImplementedException();
        }

        private void OnUserRequestOpenAboutDialog()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
