using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;
using Microsoft.Test.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiDataGrid {
        public static IEnumerable<AutomationElement> GetAll(AutomationElement window) {
            var res = window.FindAllChildrenByClassName("DataGrid");
            return res;
        }

        private static GuiDataGrid _cachedDatagrid = null;
        public static GuiDataGrid GetDataGrid(AutomationElement window, string automationId) {
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
                Thread.Sleep(100);
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
            return new GuiCell(temp, colName);
        }

        public void SetFocus() {
            dgAutoEl.SetFocus();
        }

        public int RowCount { get { return tablePatt.Current.RowCount; } }
        public int ColumnCount { get { return tablePatt.Current.ColumnCount; } }

        public void SelectRow(int rowIndex) {
            SelectRowNoWait(rowIndex);
            Thread.Sleep(300);
        }

        public void SelectRowNoWait(int rowIndex) {
            SelectionItemPattern selPatt = GetRowSelectionPattern(rowIndex);
            selPatt.Select();
        }

        public void MultiSelectFirstRow(int rowIndex) {
            SelectionItemPattern selPatt = GetRowSelectionPattern(rowIndex);
            ClearSelection();
            selPatt.AddToSelection();
            Thread.Sleep(300);
        }

        private SelectionItemPattern GetRowSelectionPattern(int rowIndex) {
            var rows = dgAutoEl.FindAllChildrenByClassName("DataGridRow");
            Console.WriteLine("found rows: " + rows.Count() + ";  index to select: " + rowIndex);
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
                scroll.SetScrollPercent(-1, 100);
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
            SendKeys.SendWait("%^N");
            UiTestDslCoreCommon.WaitWhileBusy();
            SelectRow(RowCount-2);
            return RowCount - 2;
        }

        private AutomationElement GetNewRowPlaceholder() {
            return dgAutoEl.FindChildByClassAndName("DataGridRow", "{NewItemPlaceholder}");
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

        private AutomationElement GetFirstColumnHeader() {
            return dgAutoEl.FindAllChildrenByClassName("DecoratedHeader").First();
        }

        public void RightClickColumnHeader() {
            AutomationElement header = GetFirstColumnHeader();
            var clickablePoint = header.GetClickablePoint();
            Mouse.MoveTo(new Point((int)clickablePoint.X, (int)clickablePoint.Y));
            Mouse.Click(MouseButton.Right);
        }

        public void RowCountShouldBe(int count) {
            Assert.AreEqual(count, RowCount);
        }

        public void ClearSelection() {
            for (int i = 0; i < RowCount; i++) {
                GetRowSelectionPattern(i).RemoveFromSelection();
            }
        }

        public void PrintColumns() {
            foreach (var name in HeaderNamesToIndex.Keys) {
                Console.WriteLine(name);
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
    }
}