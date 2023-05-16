using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Automation;
using System.Windows.Forms;
using Microsoft.Test.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;
using log4net;
using System.Text;
using UiClickTestDSL.DslObjects;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net.Config;
using UiClickTestDSL;


namespace UiClickTestDSL.DslObjects {
    public class GuiDataGrid {
        public static IEnumerable<AutomationElement> GetAll(AutomationElement window) {
            var res = window.FindAllChildrenByClassName("DataGrid");
            return res;
        }

        public AutomationElement InternalElement { get; private set; }

        public AutomationElement Window;
        public virtual GuiCheckBox CheckBox(string caption) { return GuiCheckBox.Find(Window, caption); }
        private static GuiDataGrid _cachedDatagrid = null;
        public static GuiDataGrid GetDataGridC(AutomationElement window, string automationId) {
            if (_cachedDatagrid == null || _cachedDatagrid.AutomationId != automationId) {
                var dataGrid = window.FindChildByClassAndAutomationId("DataGrid", automationId);
                _cachedDatagrid = new GuiDataGrid(dataGrid, automationId);
            }
            return _cachedDatagrid;
        }

        public static void InvalidateCache() {
            _cachedDatagrid = null;
        }

        private readonly AutomationElement dgAutoEl;
        private readonly TablePattern tablePatt;
        public string AutomationId { get; private set; }
        public Dictionary<string, int> HeaderNamesToIndex { get; private set; }
        public Dictionary<int, string> HeaderIndexToNames { get; private set; }

        public GuiDataGrid(AutomationElement datagrid, string automationId) {
            dgAutoEl = datagrid;
            tablePatt = datagrid.GetPattern<TablePattern>(TablePattern.Pattern);

            AutomationId = automationId;

            HeaderNamesToIndex = new Dictionary<string, int>();
            HeaderIndexToNames = new Dictionary<int, string>();
            BuildHeaderDictCache();
        }

        private void BuildHeaderDictCache() {
            int i = 0;
            for (int retries = 3; retries > 0; retries--) {
                var headers = dgAutoEl.FindAllChildrenByClassName("DataGridColumnHeader");
                foreach (var header in headers) {
                    if (header.Current.IsEnabled) {
                        HeaderNamesToIndex[header.Current.Name] = i;
                        HeaderIndexToNames[i] = header.Current.Name;
                        i++;
                    }
                }
                if (HeaderNamesToIndex.Count > 0)
                    return;
                UiTestDslCoreCommon.SleepMilliseconds(100);
            }
        }

        public GuiCell Cell(int row, string columnName) {
            if (HeaderNamesToIndex.Count == 0)
                BuildHeaderDictCache();
            int colIndex = HeaderNamesToIndex[columnName];
            var temp = tablePatt.GetItem(row, colIndex);
            return new GuiCell(temp, columnName);
        }

        public GuiCell Cell(int row, int columnIndex) {
            if (HeaderNamesToIndex.Count == 0)
                BuildHeaderDictCache();
            string colName = HeaderIndexToNames[columnIndex];
            var temp = tablePatt.GetItem(row, columnIndex);
            Assert.IsNotNull(temp, "Could not find cell in row: " + row + ", column: " + colName);
            return new GuiCell(temp, colName);
        }

        public void SetFocus() {
            dgAutoEl.SetFocus();
        }

        public int CountRows() {
            return tablePatt.Current.RowCount;
        }

        public int RowCount { get { return tablePatt.Current.RowCount; } }
        public int ColumnCount { get { return tablePatt.Current.ColumnCount; } }

        public bool IsRowSelected(int rowIndex) {
            SelectionItemPattern selPatt = GetRowSelectionPattern(rowIndex);
            return selPatt.Current.IsSelected;
        }

        public void SelectRow(int rowIndex) {
            SelectRowNoWait(rowIndex);
            UiTestDslCoreCommon.SleepMilliseconds(300);
        }

        public void SelectRowNoWait(int rowIndex) {
            SelectionItemPattern selPatt = GetRowSelectionPattern(rowIndex);
            selPatt.Select();
        }

        public void MultiSelectFirstRow(int rowIndex) {
            SelectionItemPattern selPatt = GetRowSelectionPattern(rowIndex);
            ClearSelection();
            selPatt.AddToSelection();
            UiTestDslCoreCommon.SleepMilliseconds(300);
        }

        public void AddToSelectionByName(string columnName, string content) {
            int foundRow = -1;
            int colIndex = HeaderNamesToIndex[columnName];

            for (int i = 0; i < RowCount; i++) {
                var cell = Cell(i, colIndex);
                if (cell.Text == content) {
                    foundRow = i;
                    SelectionItemPattern selPatt = GetRowSelectionPattern(foundRow);
                    //ClearSelection();
                    selPatt.AddToSelection();
                    UiTestDslCoreCommon.SleepMilliseconds(300);
                    break;
                }
            }
            if (foundRow == -1)
                throw new Exception(string.Format("Error: No row with {0} in column {1} found!", content, columnName));
        }

        public void VerifyCountOfColumnsByName(string name, int nocolumns) {
            var res = dgAutoEl.FindAll(TreeScope.Descendants, Name(name)).Count;
            res = res - 1;
            Assert.AreEqual(nocolumns, res); 
            return ;
        }

        public bool VerifyNoOfColumnsByName(string name, int nocolumns) {
            var res = dgAutoEl.FindAll(TreeScope.Descendants, Name(name)).Count;
            if (Equals(nocolumns, res)) { 
            return true;
            }
            return false;
        }

        private static PropertyCondition Name(string name) {
            return new PropertyCondition(AutomationElement.NameProperty, name, PropertyConditionFlags.IgnoreCase);
        }

        public int FindRowByCellContent(string columnName, string content) {
            int colIndex = HeaderNamesToIndex[columnName];
            return FindRowByCellContent(colIndex, content, columnName);
        }

        public int FindRowByCellContent(int colIndex, string content, string columnName = null) {
            if (columnName == null) columnName = HeaderIndexToNames[colIndex];
            for (int i = 0; i < RowCount; i++) {
                var cell = Cell(i, colIndex);
                if (cell.Text == content)
                    return i;
            }
            throw new Exception(string.Format("Error: No row with {0} in column {1} found!", content, columnName));
        }

        public int FindRowByCellContainingContent(string columnName, string content) {
            int colIndex = HeaderNamesToIndex[columnName];
            return FindRowByCellContainingContent(colIndex, content, columnName);
        }

        public int FindRowByCellContainingContent(int colIndex, string content, string columnName = null) {
            if (columnName == null) columnName = HeaderIndexToNames[colIndex];
            for (int i = 0; i < RowCount; i++) {
                var cell = Cell(i, colIndex);
                if (cell.ShouldContainValue(content))
                    return i;
            }
            throw new Exception(string.Format("Error: No row with {0} in column {1} found!", content, columnName));
        }


        /*
        public int VerifyCellContentByRow(string columnName, string row, string content) {
            int colIndex = HeaderNamesToIndex[columnName];
            return VerifyCellContentByRow(colIndex, row, content, columnName);

        }

        public int VerifyCellContentByRow(int colIndex, string row, string content, string columnName = null) {
            if (columnName == null) columnName = HeaderIndexToNames[colIndex];
            for (int i = 0; i < RowCount; i++) {
                var cell = Cell(i, colIndex);
                if (cell.Text == content)
                    return i;
            }
            throw new Exception(string.Format("Error: No row with {0} in column {1} found!", content, columnName));
        }
        */

        /*
        public int SetTextOfFirstCellByCellContent(string columnName, string content, string txvalue) {
            int colIndex = HeaderNamesToIndex[columnName];
            return SetTextOfFirstCellByCellContentWithColIndex(colIndex, content, txvalue, columnName);
        }
        */
        /*
        public int SetTextOfFirstCellByCellContentWithColIndex(int colIndex, string content, string txvalue , string columnName = null) {
            if (columnName == null) columnName = HeaderIndexToNames[colIndex];
            for (int i = 0; i < RowCount; i++) {
                var cell = Cell(i, colIndex);
                if (cell.Text == content)
                    cell.SetText(txvalue)
                    content = txvalue;
            }
            throw new Exception(string.Format("Error: No row with {0} in column {1} found!", content, columnName));
        }
        */

        /*
        public int CountRowByCellContent(int shouldcount, string columnName, string content) {
            int colIndex = HeaderNamesToIndex[columnName];
            return CountRowByCellContent(shouldcount , colIndex, content, columnName);
        }

        public int CountRowByCellContent(string datagrid, int count, int colIndex, string content, string columnName = null) { //"DeliveryLinesGrid"

            //DataGridC(datgrid). //GetChildListItems().Count;
            datagrid("")

            var isrowcount = 0;
            
            if (columnName == null) columnName = HeaderIndexToNames[colIndex];
            int i = RowCount;  
            foreach i in  { //(int i = 0; i < RowCount; i++)
                var cell = Cell(i, colIndex);
                if (cell.Text == content)
                isrowcount = i + 1;
            }
            Assert.AreEqual(isrowcount, count);

            throw new Exception(string.Format("Error: No row with {0} in column {1} found!", content, columnName));
        }
        */

        public int SelectRowByCellContent(string columnName, string content) {
            int foundRow = -1;
            int colIndex = HeaderNamesToIndex[columnName];
            for (int i = 0; i < RowCount; i++) {
                var cell = Cell(i, colIndex);
                if (cell.Text == content) {
                    foundRow = i;
                    break;
                }
            }
            if (foundRow == -1)
                throw new Exception(string.Format("Error: No row with {0} in column {1} found!", content, columnName));
            SelectRow(foundRow);
            return foundRow;
        }

        public bool VerifyRowSelectedByCellContent(string columnName, string content) {
            int foundRow = -1;
            int colIndex = HeaderNamesToIndex[columnName];
            for (int i = 0; i < RowCount; i++) {
                var cell = Cell(i, colIndex);
                if (cell.Text == content) {
                    foundRow = i;
                    break;
                }
            }
            if (foundRow == -1)
                throw new Exception(string.Format("Error: No row with {0} in column {1} found!", content, columnName));
            return IsRowSelected(foundRow);
        }

        public void SetTextInCellByContent(string columnName, string content, string txvalue) {
            int foundRow = -1;
            int colIndex = HeaderNamesToIndex[columnName];
            for (int i = 0; i < RowCount; i++) {
                var cell = Cell(i, colIndex);
                if (cell.Text == content) {
                    foundRow = i;
                    Cell(i, colIndex).SetText(txvalue);
                    break;
                }
            }
            if (foundRow == -1)
                throw new Exception(string.Format("Error: No row with {0} in column {1} found!", content, columnName));
            SelectRow(foundRow);
        }

        public void SetTextInFollowingCellByCollumAndRowContent(string columnName, string rowcontent, int addedcolno , string txvalue) {  //TODO
            int foundRow = -1;
            int colIndex = HeaderNamesToIndex[columnName];
            for (int i = 0; i < RowCount; i++) {
                var cell = Cell(i, colIndex);
                if (cell.ShouldContainText(rowcontent)) {
                    foundRow = i;
                    var ci2 = colIndex + addedcolno;
                    Cell(i, ci2).SetText(txvalue);
                    break;
                }
            }
            if (foundRow == -1)
                throw new Exception(string.Format("Error: No row with {0} in column {1} found!", rowcontent, columnName));
            SelectRow(foundRow);
        }


        public virtual void DoubleClick(MouseButton button = MouseButton.Left) {
            MoveMouseHere();
            Mouse.DoubleClick(button);
        }

        public virtual void MoveMouseHere() {
            Mouse.MoveTo(ClickablePoint);
        }

        public virtual Point ClickablePoint {
            get { return Window.GetClickablePoint().Convert(); }
        }

        public void VerifyRowByCellContent(string columnName1, string content1, string columnName2, string content2) {
            var foundRow = -1;
            for (var i = 0; i < RowCount; i++) {
                var cell1 = Cell(i, columnName1);
                var cell2 = Cell(i, columnName2);
                if (cell1.Text == content1 && cell2.Text == content2) {
                    foundRow = i;
                    break;
                }
            }
            if (foundRow == -1)
                throw new Exception($"Error: No row with \"{content1}\" in column \"{columnName1}\" and \"{content2}\" in column \"{columnName2}\" found!");
        }

        private SelectionItemPattern GetRowSelectionPattern(int rowIndex) {
            var rows = dgAutoEl.FindAllChildrenByClassName("DataGridRow");
            UiTestDslCoreCommon.PrintLine("found rows: " + rows.Count() + ";  index to select: " + rowIndex);
            var row = rows.ToList()[rowIndex];
            return row.GetPattern<SelectionItemPattern>(SelectionItemPattern.Pattern);
        }

        public void AddToSelection(int rowIndex) {
            SelectionItemPattern selPatt = GetRowSelectionPattern(rowIndex);
            selPatt.AddToSelection();
        }

        public void SelectLastRow() {
            try {
                var scroll = dgAutoEl.GetPattern<ScrollPattern>(ScrollPattern.Pattern);
                scroll.SetScrollPercent(horizontalPercent: ScrollPattern.NoScroll, verticalPercent: 100);
            } catch (InvalidOperationException) {
                //This means there was no scrollbar because the list in the grid is to short to be scrollable   
            }
            IEnumerable<AutomationElement> rows = dgAutoEl.FindAllChildrenByClassName("DataGridRow");
            AutomationElement row = rows.Last();
            var selectionPattern = row.GetPattern<SelectionItemPattern>(SelectionItemPattern.Pattern);
            selectionPattern.Select();
        }

        public int LastRowNo { get { return RowCount - 1; } }

        /// <summary>
        /// Pushes the secret hotkey: ctrl+alt+shift+n
        /// </summary>
        public int NewDeliveryLineRow() {
            SetFocus();
            SendKeys.SendWait("%^N");
            UiTestDslCoreCommon.WaitWhileBusy();
            UiTestDslCoreCommon.Sleep(1);
            SelectRow(RowCount - 2);
            return RowCount - 2;
        }
        
        private AutomationElement GetNewRowPlaceholder() {
            try {
                var scroll = dgAutoEl.GetPattern<ScrollPattern>(ScrollPattern.Pattern);
                scroll.SetScrollPercent(horizontalPercent: ScrollPattern.NoScroll, verticalPercent: 100);
            } catch (InvalidOperationException) {
                //This means there was no scrollbar because the DataGrid is to short to be scrollable   
            }
            return dgAutoEl.FindChildByClassAndName("DataGridRow", "{NewItemPlaceholder}");
        }

        private AutomationElement GetRowPlaceholder() {
            try {
                var scroll = dgAutoEl.GetPattern<ScrollPattern>(ScrollPattern.Pattern);
                scroll.SetScrollPercent(horizontalPercent: ScrollPattern.NoScroll, verticalPercent: 100);
            } catch (InvalidOperationException) {
                //This means there was no scrollbar because the DataGrid is to short to be scrollable   
            }
            return dgAutoEl.FindChildByControlTypeAndClass(ControlType.DataItem,"DataGridRow"); 
        }

        public void AddNewRowMarkerToSelection() {
            AutomationElement newRowPlaceholder = GetNewRowPlaceholder();
            var selPattern = newRowPlaceholder.GetPattern<SelectionItemPattern>(SelectionItemPattern.Pattern);
            selPattern.AddToSelection();
        }

        public void RightClickNewRowMarker() {
            AutomationElement newRowPlaceholder = GetNewRowPlaceholder();
            newRowPlaceholder.MoveMouseToCenter();
            Mouse.Click(MouseButton.Right);
        }

        
        public void RightClickSelectedRow() {
            AutomationElement RowPlaceholder = GetRowPlaceholder();
            RowPlaceholder.MoveMouseToCenter();
            Mouse.Click(MouseButton.Right);
        }

        public void DoubleClickSelectedRow() {
            AutomationElement RowPlaceholder = GetRowPlaceholder();
            RowPlaceholder.MoveMouseToCenter();
            Mouse.DoubleClick(MouseButton.Left);
        }


        public void LeftClickSelectedRow() {
            AutomationElement RowPlaceholder = GetRowPlaceholder();
            RowPlaceholder.MoveMouseToCenter();
            Mouse.Click(MouseButton.Left);
        }


        public void DoubleClickFirstCellInNewRowMarker() {
            AutomationElement newRowPlaceholder = GetNewRowPlaceholder();
            var newRowCell = newRowPlaceholder.FindAllChildrenByClassName("DataGridCell").First();
            newRowCell.MoveMouseToCenter();
            Mouse.DoubleClick(MouseButton.Left);
        }

        public void InvokeFirstCellInNewRowMarker() {
            //This method has basically the same functionality as the DoubleClickFirstCellInNewRowMarker, except it uses the InvokePattern instead of the mouse to activate the first cell.
            //Activating the placeholder-cell activates the ValuePattern for the Cell.
            AutomationElement newRowPlaceholder = GetNewRowPlaceholder();
            var newRowCell = newRowPlaceholder.FindAllChildrenByClassName("DataGridCell").First();
            newRowCell.GetInvokePattern().Invoke();
        }

        private AutomationElement GetFirstColumnHeader() {
            return dgAutoEl.FindAllChildrenByClassName("DecoratedHeader").First();
        }

        public void RightClickColumnHeader() {
            AutomationElement header = GetFirstColumnHeader();
            var clickablePoint = header.GetClickablePoint();
            Mouse.MoveTo(new Point((int)clickablePoint.X, (int)clickablePoint.Y));
            Mouse.Click(MouseButton.Right);
        }

        public void LeftClickColumnHeader() {
            AutomationElement header = GetFirstColumnHeader();
            var clickablePoint = header.GetClickablePoint();
            Mouse.MoveTo(new Point((int)clickablePoint.X, (int)clickablePoint.Y));
            Mouse.Click(MouseButton.Left);
        }


        public void RowCountShouldBe(int count) {
            Assert.AreEqual(count, RowCount);
        }

        public void ColumnCountShouldBe(int count) {
            Assert.AreEqual(count, ColumnCount);
        }

        public void ClearSelection() {
            for (int i = 0; i < RowCount; i++) {
                GetRowSelectionPattern(i).RemoveFromSelection();
                UiTestDslCoreCommon.WaitWhileBusy();
            }
        }

        public void PrintColumns() {
            foreach (var name in HeaderNamesToIndex.Keys) {
                UiTestDslCoreCommon.PrintLine(name);
            }
        }

        public void FirstColumnShouldNotBe(string header) {
            Assert.AreNotEqual(header, HeaderIndexToNames[0]);
        }

        public void FirstNamedColumnShouldBe(string header) {
            int i = 0;
            while (string.IsNullOrWhiteSpace(HeaderIndexToNames[i++])) { }
            Assert.AreEqual(header, HeaderIndexToNames[i]);
        }

        public void RowCountShouldBeLargerThan(int count) {
            Assert.IsTrue(count < RowCount, "Row count should be larger than: " + count + " was: " + RowCount);
        }

        public void ShouldBeDisabled() {
            Assert.IsTrue(dgAutoEl.Current.IsEnabled, AutomationId + " was disabled.");
        }

        public void ShouldBeEnabled() {
            Assert.IsTrue(dgAutoEl.Current.IsEnabled, AutomationId + " was disabled.");
        }

        public void ScrollToBottom() {
            try {
                var scroll = InternalElement.GetPattern<ScrollPattern>(ScrollPattern.Pattern);
                scroll.SetScrollPercent(horizontalPercent: ScrollPattern.NoScroll, verticalPercent: 100);
            } catch (InvalidOperationException) {
                //This means there was no scrollbar because the list in the TreeView is to short to be scrollable   
            }
        }
    }
}