#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

using System.Linq;
using System.Threading;
using System.Windows.Threading;



#endregion

namespace CEC_CADBlockTrans
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        private Thread _uiThread;
        // ModelessForm instance
        private UI _mMyForm;
        public virtual Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                EventHandlerWithStringArg evStr = new EventHandlerWithStringArg();
                EventHandlerWithWpfArg evWpf = new EventHandlerWithWpfArg();
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;
                #region
                this.ShowForm(commandData.Application);
                return Result.Succeeded;
                #endregion
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
        public void ShowForm(UIApplication uiapp)
        {
            // If we do not have a dialog yet, create and show it
            Document doc = uiapp.ActiveUIDocument.Document;
            if (_mMyForm != null && _mMyForm == null) return;
            //EXTERNAL EVENTS WITH ARGUMENTS
            EventHandlerWithStringArg evStr = new EventHandlerWithStringArg();
            EventHandlerWithWpfArg evWpf = new EventHandlerWithWpfArg();

            #region

            // The dialog becomes the owner responsible for disposing the objects given to it.
            #endregion
            _mMyForm = new UI(uiapp, evStr, evWpf);
            _mMyForm.Show();
        }

        public void ShowFormSeparateThread(UIApplication uiapp)
        {
            // If we do not have a thread started or has been terminated start a new one
            Document doc = uiapp.ActiveUIDocument.Document;
            if (!(_uiThread is null) && _uiThread.IsAlive) return;
            //EXTERNAL EVENTS WITH ARGUMENTS
            EventHandlerWithStringArg evStr = new EventHandlerWithStringArg();
            EventHandlerWithWpfArg evWpf = new EventHandlerWithWpfArg();

            #region
            //»`¶°CAD Block
            Autodesk.Revit.DB.View activeView = doc.ActiveView;
            Element viewLevel = activeView.GenLevel;
            string output = "";
            ElementLevelFilter levelFilter = new ElementLevelFilter(activeView.LevelId);
            FilteredElementCollector CADcollector = new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).WhereElementIsNotElementType();
            //MessageBox.Show(CADcollector.Count().ToString());
            List<string> blockNameList = new List<string>();
            List<Element> blockElement = new List<Element>();
            foreach (ImportInstance inst in CADcollector)
            {
                ElementId typeId = inst.GetTypeId();
                string tempName = doc.GetElement(typeId).Name;
                if (!blockNameList.Contains(tempName))
                {
                    blockNameList.Add(tempName);
                    blockElement.Add(doc.GetElement(typeId));
                }
            }
            // The dialog becomes the owner responsible for disposing the objects given to it.
            #endregion

            _uiThread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(
                        Dispatcher.CurrentDispatcher));
                // The dialog becomes the owner responsible for disposing the objects given to it.
                _mMyForm = new UI(uiapp, evStr, evWpf);
                _mMyForm.Closed += (s, e) => Dispatcher.CurrentDispatcher.InvokeShutdown();
                _mMyForm.Show();
                _mMyForm.BlockListBox.ItemsSource = blockElement;
                Dispatcher.Run();
            });

            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.IsBackground = true;
            _uiThread.Start();
        }
    }
}