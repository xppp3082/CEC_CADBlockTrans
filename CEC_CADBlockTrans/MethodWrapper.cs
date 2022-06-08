﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CEC_CADBlockTrans
{
    public class EventHandlerWithStringArg : RevitEventWrapper<string>
    {
        /// <summary>
        /// The Execute override void must be present in all methods wrapped by the RevitEventWrapper.
        /// This defines what the method will do when raised externally.
        /// </summary>
        public override void Execute(UIApplication uiApp, string args)
        {
            // Do your processing here with "args"
            TaskDialog.Show("External Event", args);
        }
    }

    /// <summary>
    /// This is an example of of wrapping a method with an ExternalEventHandler using an instance of WPF
    /// as an argument. Any type of argument can be passed to the RevitEventWrapper, and therefore be used in
    /// the execution of a method which has to take place within a "Valid Revit API Context". This specific
    /// pattern can be useful for smaller applications, where it is convenient to access the WPF properties
    /// directly, but can become cumbersome in larger application architectures. At that point, it is suggested
    /// to use more "low-level" wrapping, as with the string-argument-wrapped method above.
    /// </summary>
    public class EventHandlerWithWpfArg : RevitEventWrapper<UI>
    {
        /// <summary>
        /// The Execute override void must be present in all methods wrapped by the RevitEventWrapper.
        /// This defines what the method will do when raised externally.
        /// </summary>
        public override void Execute(UIApplication uiApp, UI ui)
        {
            // SETUP
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;
            #region 舊作法，東西在execute中蒐集，微怪
            //蒐集CAD Block
            //Autodesk.Revit.DB.View activeView = doc.ActiveView;
            //Element viewLevel = activeView.GenLevel;
            //string output = "";
            //ElementLevelFilter levelFilter = new ElementLevelFilter(activeView.LevelId);
            //FilteredElementCollector CADcollector = new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).WhereElementIsNotElementType();
            ////MessageBox.Show(CADcollector.Count().ToString());
            //List<string> blockNameList = new List<string>();
            //List<Element> blockElement = new List<Element>();
            //foreach (ImportInstance inst in CADcollector)
            //{
            //    ElementId typeId = inst.GetTypeId();
            //    string tempName = doc.GetElement(typeId).Name;
            //    if (!blockNameList.Contains(tempName))
            //    {
            //        blockNameList.Add(tempName);
            //        blockElement.Add(doc.GetElement(typeId));
            //    }
            //}
            //ui.Dispatcher.Invoke(() =>ui.BlockListBox.ItemsSource = blockElement);
            #endregion
            List<CAD> cadList = new List<CAD>();
            int count = 0;
            foreach (CAD cad in ui.BlockListBox.Items)
            {
                if (cad.Selected == true)
                {
                    count++;
                    cadList.Add(cad);
                }
            }
            //ui.Dispatcher.Invoke(() => count = ui.BlockListBox.SelectedItems.Count);
            //MessageBox.Show($"BlockListBox 中共有 {count} 個物件被選取");
            ui.Dispatcher.Invoke(() => ui.pbar.Value = 0);
            ui.Dispatcher.Invoke(() => ui.pbar.Maximum = count);
            int number = 1;
            List<ImportInstance> targetList = new List<ImportInstance>();

            #region 原來作法
            ////原來作法
            //using (Transaction trans = new Transaction(doc))
            //{
            //    trans.Start("圖塊批次放置");
            //    foreach (CAD cad in cadList)
            //    {
            //        ElementType elemType = doc.GetElement(cad.Id) as ElementType;
            //        ui.Dispatcher.Invoke(() => Method.cadBlockCount(ui, doc, elemType)); //-->targetBlocks仍然為0，UI.dispatche.invoke無法賦值運算?
            //        ui.Dispatcher.Invoke(() => Method.createFamilyInstance(ui, doc, elemType));
            //        ui.Dispatcher.Invoke(() => ui.pbar.Value += number);
            //    }
            //    trans.Commit();
            //    trans.Dispose();
            //}
            //ui.Activate();
            #endregion

            #region 新作法
            //新作法嘗試，先蒐集所有的ImportInst
            int completeNum = 0;
            foreach (CAD cad in cadList)
            {
                ElementType elemType = doc.GetElement(cad.Id) as ElementType;
                List<ImportInstance> tempList = new importedCAD().instanceOfType(doc, elemType);
                ui.Dispatcher.Invoke(() => Method.cadBlockCount(ui, doc, elemType));
                if (tempList.Count() > 0)
                {
                    foreach (ImportInstance inst in tempList)
                    {
                        targetList.Add(inst);
                    }
                }
            }
            ui.Dispatcher.Invoke(() => ui.pbar.Value = 0);
            ui.Dispatcher.Invoke(() => ui.pbar.Maximum = targetList.Count);
            using (TransactionGroup transGroup = new TransactionGroup(doc))
            {
                transGroup.Start("圖塊批次放置");
                foreach (ImportInstance inst in targetList)
                {
                    //如果有成功創造
                    if (Method.createInstanceByCAD(ui, doc, inst))
                    {
                        completeNum += 1;
                    }
                    #region 關於progrssbar的更新-->注意要設定DispatcherPriority為Background
                    ui.Dispatcher.Invoke(() => ui.pbar.Value += 1, System.Windows.Threading.DispatcherPriority.Background);
                    //ui.pbar.Dispatcher.Invoke(() => ui.pbar.Value += 1, System.Windows.Threading.DispatcherPriority.Background);
                    #endregion
                }
                transGroup.Assimilate();
            }
            FamilySymbol selectedSymbol = ui.symbolComboBox.SelectedItem as FamilySymbol;
            Task.Run(() =>
            {
                string completeMessage = $"【轉換完成】共成功將 {completeNum} 個圖塊轉換為 {selectedSymbol.FamilyName} - {selectedSymbol.Name} 元件";
                ui.Dispatcher.Invoke(() =>
                    ui.outputBox.Text += "\n" + completeMessage);
            });
            ui.Activate();
            #endregion
        }
    }
}