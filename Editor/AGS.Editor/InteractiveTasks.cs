using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace AGS.Editor
{
    public class InteractiveTasks
    {
        private const int ENGINE_EXIT_CODE_NORMAL = 91;
        private const int ENGINE_EXIT_CODE_CRASH = 92;

        private delegate void TestGameFinishedDelegate(int exitCode);
        public delegate void TestGameFinishedHandler();
        public event TestGameFinishedHandler TestGameFinished;
        public delegate void TestGameStartingHandler();
        public event TestGameStartingHandler TestGameStarting;

        private Control _mainGUIThread;
        private bool _currentlyTesting = false;
        private Tasks _tasks;

        public InteractiveTasks(Tasks tasks)
        {
            _tasks = tasks;
            _tasks.TestGameFinished += new Tasks.TestGameFinishedHandler(Tasks_TestGameFinished);
        }

        public bool BrowseForAndLoadGame()
        {
            bool loadedSuccessfully = false;
            string gameToLoad = Factory.GUIController.ShowOpenFileDialog("Sélectionnez le jeu à ouvrir", "Fichiers jeu AGS (*.agf, ac2game.dta)|*.agf;ac2game.dta|Jeux AGS 3.x (*.agf)|*.agf|Jeux AGS 2.72 (*.dta)|ac2game.dta", false);
            if (gameToLoad != null)
            {
                try
                {
                    loadedSuccessfully = LoadGameFromDisk(gameToLoad);
                }
                catch (AGS.Types.InvalidDataException e)
                {
                    Factory.GUIController.ShowMessage("Impossible d'importer le jeu.\n\n" + e.Message, MessageBoxIcon.Warning);
                }
                catch (AGS.Types.AGSEditorException e)
                {
                    Factory.GUIController.ShowMessage("Impossible d'importer le jeu.\n\n" + e.Message, MessageBoxIcon.Warning);
                }
            }
            return loadedSuccessfully;
        }

        public bool LoadGameFromDisk(string gameToLoad)
        {
            try
            {
                bool success = _tasks.LoadGameFromDisk(gameToLoad, true);

                AGS.Types.Game game = Factory.AGSEditor.CurrentGame;
				if (((game.SavedXmlVersion != null) &&
					 (game.SavedXmlVersion != AGSEditor.LATEST_XML_VERSION))
                       ||
                    ((game.SavedXmlVersionIndex != null) &&
                     (game.SavedXmlVersionIndex != AGSEditor.LATEST_XML_VERSION_INDEX)))
				{
					Factory.GUIController.ShowMessage("Ce jeu a été dernièrement sauvé avec " +
                        ((game.SavedXmlEditorVersion == null) ? "une version plus ancienne" : ("la version " + game.SavedXmlEditorVersion))
                        + " d'AGS. Si vous le sauvez maintenant, le jeu sera mis à niveau et les version précédentes d'AGS ne pourront pas l'ouvrir.", MessageBoxIcon.Information);
				}

				return success;
            }
            catch (Exception ex)
            {
                string messageDetails = string.Empty;
                if ((!(ex is AGS.Types.InvalidDataException)) &&
                    (!(ex is AGS.Types.AGSEditorException)))
                {
                    messageDetails = "\r\n\r\nDétails de l'erreur : " + ex.ToString();
                }
                Factory.GUIController.ShowMessage("Une erreur est survenue au chargement de votre jeu. L'erreur est la suivante : " + Environment.NewLine + Environment.NewLine + ex.Message + "\r\n\r\nSi vous ne pouvez pas résourdre le problème, veuillez demander de l'aide sur le Forum Technique d'AGS." + messageDetails, MessageBoxIcon.Warning);
                return false;
            }
        }

        public void CreateTemplateFromCurrentGame(string templateFileName)
        {
            BusyDialog.Show("Patientez durant la création du modèle...", new BusyDialog.ProcessingHandler(CreateTemplateFromCurrentGameProcess), templateFileName);
        }

        private object CreateTemplateFromCurrentGameProcess(object templateFileName)
        {
            _tasks.CreateTemplateFromCurrentGame((string)templateFileName);
            return null;
        }

        public void TestGame(bool withDebugger)
        {
            _mainGUIThread = new Control();
            IntPtr forceWindowHandleCreation = _mainGUIThread.Handle;
            _currentlyTesting = true;
            try
            {
                _tasks.TestGame(withDebugger);
            }
            catch (Exception ex)
            {
                Tasks_TestGameFinished(-1);
                throw new Exception(ex.Message, ex);
            }

            if (TestGameStarting != null)
            {
                TestGameStarting();
            }
        }

        private void Tasks_TestGameFinished(int exitCode)
        {
            if (_currentlyTesting)
            {
                _currentlyTesting = false;
                _mainGUIThread.Invoke(new TestGameFinishedDelegate(TestGameExitedOnGUIThread), exitCode);
            }
        }

        private void TestGameExitedOnGUIThread(int exitCode)
        {
            if (exitCode == ENGINE_EXIT_CODE_NORMAL)
            {
                // TODO: Check warnings.log and display
            }
            else if (exitCode == ENGINE_EXIT_CODE_CRASH)
            {
            }
            else
            {
                Factory.GUIController.ShowMessage("Le moteur du jeu ne semble pas s'être fermé correctement. Si le problème persiste, reportez-le sur le Forum Technique.", MessageBoxIcon.Warning);
            }

            if (TestGameFinished != null)
            {
                TestGameFinished();
            }

        }

    }
}
