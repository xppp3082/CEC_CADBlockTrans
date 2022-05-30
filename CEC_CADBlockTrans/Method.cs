using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace CEC_CADBlockTrans
{
    internal class Method
    {

        private static async Task<List<ViewSheet>> GetSheets(Document doc)
        {
            return await Task.Run(() =>
            {
                return new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSheet))
                    .Select(p => (ViewSheet)p).ToList();
            });
        }

        //public static void SheetRename(UI ui, Document doc)
        //{
        //    // get sheets - note that this may be replaced with the Async Task method above,
        //    // however that will only work if we want to only PULL data from the sheets,
        //    // and executing a transaction like below from an async collection, will crash the app-->因為await 和 async的任務可能還未完成
        //    List<ViewSheet> sheets = new FilteredElementCollector(doc)
        //        .OfClass(typeof(ViewSheet))
        //        .Select(p => (ViewSheet)p).ToList();

        //    // report results - push the task off to another thread
        //    Task.Run(() =>
        //    {
        //        // report the count
        //        string message = $"There are {sheets.Count} Sheets in the project";
        //        ui.Dispatcher.Invoke(() =>
        //            ui.TbDebug.Text += "\n" + (DateTime.Now).ToLongTimeString() + "\t" + message);
        //    });

        //    // rename all the sheets, but first open a transaction
        //    using (Transaction t = new Transaction(doc, "Rename Sheets"))
        //    {
        //        // start a transaction within the valid Revit API context
        //        t.Start("Rename Sheets");

        //        // loop over the collection of sheets using LINQ syntax
        //        foreach (string renameMessage in from sheet in sheets
        //                                         let renamed = sheet.LookupParameter("Sheet Name")?.Set("TEST")
        //                                         select $"Renamed: {sheet.Title}, Status: {renamed}")
        //        {
        //            ui.Dispatcher.Invoke(() =>
        //                ui.TbDebug.Text += "\n" + (DateTime.Now).ToLongTimeString() + "\t" + renameMessage);
        //        }

        //        t.Commit();
        //        t.Dispose();
        //    }

        //    // invoke the UI dispatcher to print the results to report completion
        //    ui.Dispatcher.Invoke(() =>
        //        ui.TbDebug.Text += "\n" + (DateTime.Now).ToLongTimeString() + "\t" + "SHEETS HAVE BEEN RENAMED");
        //}

        ///// <summary>
        ///// Print the Title of the Revit Document on the main text box of the WPF window of this application.
        ///// </summary>
        ///// <param name="ui">An instance of our UI class, which in this template is the main WPF
        ///// window of the application.</param>
        ///// <param name="doc">The Revit Document to print the Title of.</param>
        //public static void DocumentInfo(UI ui, Document doc)
        //{
        //    ui.Dispatcher.Invoke(() => ui.TbDebug.Text += "\n" + (DateTime.Now).ToLongTimeString() + "\t" + doc.Title);
        //}

        ///// <summary>
        ///// Count the walls in the Revit Document, and print the count
        ///// on the main text box of the WPF window of this application.
        ///// </summary>
        ///// <param name="ui">An instance of our UI class, which in this template is the main WPF
        ///// window of the application.</param>
        ///// <param name="doc">The Revit Document to count the walls of.</param>
        //public static void WallInfo(UI ui, Document doc)
        //{
        //    Task.Run(() =>
        //    {
        //        // get all walls in the document
        //        ICollection<Wall> walls = new FilteredElementCollector(doc)
        //            .OfCategory(BuiltInCategory.OST_Walls).WhereElementIsNotElementType()
        //            .Select(p => (Wall)p).ToList();

        //        // format the message to show the number of walls in the project
        //        string message = $"There are {walls.Count} Walls in the project";

        //        // invoke the UI dispatcher to print the results to the UI
        //        ui.Dispatcher.Invoke(() =>
        //            ui.TbDebug.Text += "\n" + (DateTime.Now).ToLongTimeString() + "\t" + message);
        //    });
        //}
        //public static void cadBlockCount(UI ui, Document doc, ElementType CADType)
        public static List<Element> cadBlockCount(UI ui, Document doc, ElementType CADType)
        {
            List<Element> selectedBlocks = new List<Element>();
            View activeView = doc.ActiveView;
            Level activeLevel = activeView.GenLevel;
            //為何此處需要taskRun -->且activeLevel還必須放在Task.Run()之前

            int count = 0;
            string instName = CADType.Name;
            ElementLevelFilter levelFilter = new ElementLevelFilter(activeLevel.Id);
            FilteredElementCollector cadImportInst = new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).WherePasses(levelFilter).WhereElementIsNotElementType();
            foreach (Element temp in cadImportInst)
            {
                ElementType tempType = doc.GetElement(temp.GetTypeId()) as ElementType;
                if (tempType.Name == instName)
                {
                    count += 1;
                    selectedBlocks.Add(temp);
                }
            }
            Task.Run(() =>
            {
                string message = $"{DateTime.Now} \n「{instName}」 圖塊在 {activeLevel.Name} 層中共有「{count}」 個";
                ui.Dispatcher.Invoke(() =>
                    ui.outputBox.Text += "\n" + message);
            });
            return selectedBlocks;
        }
        public static void DocumentInfo(UI ui, Document doc)
        {
            ui.Dispatcher.Invoke(() => ui.outputBox.Text += "\n" + (DateTime.Now).ToLongTimeString() + "\t" + doc.Title);
        }

        public static void getCADBlockList(UI ui, Document doc)
        {
            //蒐集CAD Block
            Autodesk.Revit.DB.View activeView = doc.ActiveView;
            Element viewLevel = activeView.GenLevel;
            string output = "";
            ElementLevelFilter levelFilter = new ElementLevelFilter(viewLevel.Id);
            FilteredElementCollector CADcollector = new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).WherePasses(levelFilter).WhereElementIsNotElementType();
            //MessageBox.Show(CADcollector.Count().ToString());
            List<string> blockNameList = new List<string>();
            List<Element> blockElement = new List<Element>();
            List<CAD> cadElement = new List<CAD>();
            foreach (ImportInstance inst in CADcollector)
            {
                ElementId typeId = inst.GetTypeId();
                string tempName = doc.GetElement(typeId).Name;
                Element tempElement = doc.GetElement(typeId);
                if (!blockNameList.Contains(tempName))
                {
                    cadElement.Add(new CAD { Name = tempElement.Name, Id = tempElement.Id, Selected = false });
                    blockNameList.Add(tempName);
                    blockElement.Add(tempElement);
                }
            }
            ui.BlockListBox.ItemsSource = cadElement;
            //ui.Dispatcher.Invoke(() => ui.BlockListBox.ItemsSource = cadElement);
            ui.Dispatcher.Invoke(() => ui.activLevelBox.Items.Add(viewLevel));
            ui.Dispatcher.Invoke(() => ui.activLevelBox.SelectedItem = viewLevel);
        }

        public static void getSymbolCategory(UI ui, Document doc)
        {
            //蒐集Document 中有Symbol的Category
            Categories categories = doc.Settings.Categories;
            List<Category> usefulCategories = new List<Category>();
            foreach (Category c in categories)
            {
                if (!c.IsTagCategory) usefulCategories.Add(c);
            }
            //品類篩選器測試 --> 2022.05.09測試後發現要以FamilySymbol反查才行，而且可以試著用dictionary的型別去儲存資料
            Dictionary<Category, List<Element>> symbolDictByCate = new Dictionary<Category, List<Element>>();
            foreach (Category c in usefulCategories)
            {
                List<Element> symbolCollectorByCate = new FilteredElementCollector(doc).OfCategoryId(c.Id).OfClass(typeof(FamilySymbol)).ToList();
                if (symbolCollectorByCate.Count() != 0 && !symbolDictByCate.Keys.Contains(c))
                {
                    symbolDictByCate.Add(c, symbolCollectorByCate);
                }
                else if (symbolCollectorByCate.Count() != 0 && symbolDictByCate.Keys.Contains(c))
                {
                    List<Element> result = symbolDictByCate[c].Union(symbolCollectorByCate).ToList();
                    symbolDictByCate[c] = result;
                }
            }
            ui.categoryComboBox.ItemsSource = symbolDictByCate.Keys;
        }

        public static void getFamilyfromCategory(UI ui, Document doc)
        {
            Category cate = ui.categoryComboBox.SelectedItem as Category;
            ui.Dispatcher.Invoke(() => ui.familyComboBox.ItemsSource = new familyListbyCategory().getFamilybyCategory(doc, cate));
            ui.Dispatcher.Invoke(() => ui.symbolComboBox.ItemsSource = null);
            ui.Dispatcher.Invoke(() => ui.SymbolPreviewImage.Source = null);
        }

        public static void getSymbolsfromFamily(UI ui, Document doc)
        {
            if (ui.familyComboBox.SelectedItem != null)
            {
                Family family = ui.familyComboBox.SelectedItem as Family;
                List<FamilySymbol> symbolList = new familyListbyCategory().getSymbolbyFamily(family);
                BitmapImage catchImage = new familyListbyCategory().getPreviewImage(family);
                ui.Dispatcher.Invoke(() => ui.symbolComboBox.ItemsSource = symbolList);
                ui.Dispatcher.Invoke(() => ui.SymbolPreviewImage.Source = catchImage);
            }
        }

        public static void createFamilyInstance(UI ui, Document doc, ElementType CADType)
        {
            //先根據UI的內容蒐集要轉換的block
            List<Element> selectedBlocks = new List<Element>();
            View activeView = doc.ActiveView;
            Level activeLevel = activeView.GenLevel;
            string instName = CADType.Name;
            //task開始

            ElementLevelFilter levelFilter = new ElementLevelFilter(activeLevel.Id);
            FilteredElementCollector cadImportInst = new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).WherePasses(levelFilter).WhereElementIsNotElementType();
            foreach (Element temp in cadImportInst)
            {
                ElementType tempType = doc.GetElement(temp.GetTypeId()) as ElementType;
                if (tempType.Name == instName)
                {
                    selectedBlocks.Add(temp);
                }
            }
            //蒐集UI上的資訊
            DisplayUnitType unitType = DisplayUnitType.DUT_MILLIMETERS;
            FamilySymbol selectedSymbol = ui.symbolComboBox.SelectedItem as FamilySymbol;
            string offsetText = ui.offsetBox.Text;
            double offsetValue = Convert.ToDouble(offsetText);
            double valueToSet = UnitUtils.ConvertToInternalUnits(offsetValue, unitType);
            //MessageBox.Show($"欲創造的模型名稱為 {selectedSymbol.Name} ，輸入的位移值為 {offsetValue} mm");
            int createNum = 0;
            FamilyInstance inst = null;
            if (selectedSymbol != null && offsetValue != null)
            {
                using (Transaction trans = new Transaction(doc))
                {
                    trans.Start("圖塊批次放置");
                    foreach (Element e in selectedBlocks)
                    {
                        ImportInstance cadInst = e as ImportInstance;
                        Transform t = cadInst.GetTotalTransform();
                        XYZ cadOrigin = t.Origin;
                        inst = doc.Create.NewFamilyInstance(cadOrigin, selectedSymbol, activeLevel, StructuralType.NonStructural);
                        createNum += 1;
                        Parameter instOffset = inst.LookupParameter("偏移");
                        instOffset.Set(valueToSet);
                    }
                    trans.Commit();
                    trans.Dispose();
                }
            }
            Task.Run(() =>
            {
                string message = $"{DateTime.Now} \n「共創造了「{createNum} 」個「{selectedSymbol.Name}」元件";
                ui.Dispatcher.Invoke(() =>
                    ui.outputBox.Text += "\n" + message);
            });
        }

    }
    //自定義CAD物件，提供給ListBox使用
    public class CAD
    {
        public string Name { get; set; }
        public ElementId Id { get; set; }
        public bool Selected { get; set; }
    }

    public class familyListbyCategory
    {
        public List<FamilySymbol> getSymbolbyCategory(Document doc, Category category)
        {
            List<FamilySymbol> targetList = new List<FamilySymbol>();
            //利用categoryID與Enum相互對照
            //MessageBox.Show(category.Name);
            //BuiltInCategory myCatEnum = (BuiltInCategory)Enum.Parse(typeof(BuiltInCategory), category.Id.ToString());
            //FilteredElementCollector familyCollector = new FilteredElementCollector(doc).OfCategory(myCatEnum).WhereElementIsNotElementType();
            FilteredElementCollector familyCollector = new FilteredElementCollector(doc).OfCategoryId(category.Id).OfClass(typeof(FamilySymbol));
            //FilteredElementCollector familyCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls)/*OfClass(typeof(Family)).*//*WhereElementIsElementType()*/;
            foreach (FamilySymbol symbol in familyCollector)
            {
                if (symbol != null)
                {
                    targetList.Add(symbol);
                }
            }
            return targetList;
        }

        public List<Family> getFamilybyCategory(Document doc, Category category)
        {
            List<Family> targetList = new List<Family>();
            FilteredElementCollector familyCollector = new FilteredElementCollector(doc).OfClass(typeof(Family));
            foreach (Family fam in familyCollector)
            {
                Category famCate = fam.FamilyCategory;
                if (famCate.Id == category.Id)
                {
                    targetList.Add(fam);
                }
            }
            return targetList;
        }

        public List<FamilySymbol> getSymbolbyFamily(Family fam)
        {
            List<FamilySymbol> targetList = new List<FamilySymbol>();
            Document doc = fam.Document;
            ICollection<ElementId> symbolList = fam.GetFamilySymbolIds();
            foreach (ElementId id in symbolList)
            {
                FamilySymbol tempSymbol = doc.GetElement(id) as FamilySymbol;
                targetList.Add(tempSymbol);
            }
            return targetList;
        }

        public BitmapImage getPreviewImage(Family target)
        {
            Family targetFamily = null;
            Document doc = target.Document;
            FilteredElementCollector familyCollector = new FilteredElementCollector(doc);
            ElementFilter FamilyFilter = new ElementClassFilter(typeof(Family));
            familyCollector.WherePasses(FamilyFilter).ToElements();
            foreach (Family e in familyCollector)
            {
                if (e.Name == target.Name)
                {
                    targetFamily = e;
                }
            }
            ICollection<ElementId> symbol_IDs = targetFamily.GetFamilySymbolIds();
            FamilySymbol tempSymbol = doc.GetElement(symbol_IDs.First()) as FamilySymbol; //取的第一個ID所代表的FamilySymbol
            ElementType type = tempSymbol as ElementType;
            System.Drawing.Size imgSize = new System.Drawing.Size(500, 500);
            Bitmap image = type.GetPreviewImage(imgSize);
            BitmapImage bitmapimage = new BitmapImage();

            using (MemoryStream memory = new MemoryStream())
            {
                image.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

            }
            return bitmapimage;
        }
        public BitmapImage getSymbolImage(FamilySymbol symbol)
        {
            Document doc = symbol.Document;
            ElementType type = symbol as ElementType;
            System.Drawing.Size imgSize = new System.Drawing.Size(500, 500);
            Bitmap image = type.GetPreviewImage(imgSize);
            BitmapImage bitmapimage = new BitmapImage();

            using (MemoryStream memory = new MemoryStream())
            {
                image.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

            }
            return bitmapimage;
        }
    }
}

