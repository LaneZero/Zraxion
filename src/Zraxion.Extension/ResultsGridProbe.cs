using Microsoft.SqlServer.Management.UI.Grid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;
using OperandType = System.Reflection.Emit.OperandType;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Windows.Forms;
using System.Drawing;


namespace Zraxion.Extension
{
    /// <summary>
    /// Provides read-only diagnostics for discovering the SSMS Results Grid
    /// through the Windows UI Automation tree.
    /// </summary>
    internal static class ResultsGridProbe
    {


        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(
            IntPtr hWndParent,
            EnumWindowsProc lpEnumFunc,
            IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(
            IntPtr hWnd,
            StringBuilder lpClassName,
            int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(
            IntPtr hWnd,
            StringBuilder lpString,
            int nMaxCount);


        private const int MaxCandidates = 40;

        /// <summary>
        /// Inspects the UI Automation hierarchy of the current SSMS process
        /// and reports controls that appear related to query results.
        /// </summary>
        public static string InspectAutomationTree()
        {
            var process = Process.GetCurrentProcess();

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                return "SSMS main window handle was not available.";
            }

            AutomationElement root;

            try
            {
                root = AutomationElement.FromHandle(process.MainWindowHandle);
            }
            catch (Exception ex)
            {
                return $"Unable to access the SSMS UI Automation root.\n\n{ex.Message}";
            }

            var candidates = new List<string>();

            try
            {
                var elements = root.FindAll(
                    TreeScope.Descendants,
                    Condition.TrueCondition);

                foreach (AutomationElement element in elements)
                {
                    if (candidates.Count >= MaxCandidates)
                    {
                        break;
                    }

                    InspectElement(element, candidates);
                }
            }
            catch (ElementNotAvailableException)
            {
                // The SSMS UI may change while the automation tree is being inspected.
            }

            var builder = new StringBuilder();

            builder.AppendLine($"Automation candidates: {candidates.Count}");
            builder.AppendLine();

            if (candidates.Count == 0)
            {
                builder.AppendLine(
                    "No Results Grid candidates were detected through UI Automation.");

                return builder.ToString();
            }

            for (var i = 0; i < candidates.Count; i++)
            {
                builder.AppendLine($"{i + 1}. {candidates[i]}");
                builder.AppendLine();
            }

            return builder.ToString();
        }

        /// <summary>
        /// Inspects one UI Automation element and records it when it
        /// appears relevant to the SSMS Results Grid.
        /// </summary>
        private static void InspectElement(
            AutomationElement element,
            ICollection<string> candidates)
        {
            try
            {
                var name = element.Current.Name ?? string.Empty;
                var className = element.Current.ClassName ?? string.Empty;
                var automationId = element.Current.AutomationId ?? string.Empty;
                var controlType = element.Current.ControlType;

                var controlTypeName =
                    controlType?.ProgrammaticName ?? string.Empty;

                var searchable =
                    $"{name} {className} {automationId} {controlTypeName}";

                if (!LooksRelevant(searchable, controlType))
                {
                    return;
                }

                candidates.Add(
                    $"Name: {Normalize(name)}\n" +
                    $"Class: {Normalize(className)}\n" +
                    $"AutomationId: {Normalize(automationId)}\n" +
                    $"ControlType: {Normalize(controlTypeName)}\n" +
                    $"HWND: 0x{element.Current.NativeWindowHandle:X}\n" +
                    $"Keyboard focus: {element.Current.HasKeyboardFocus}\n" +
                    $"Patterns: {GetPatternNames(element)}");
            }
            catch (ElementNotAvailableException)
            {
                // Ignore controls that disappear while SSMS updates its UI.
            }
        }

        /// <summary>
        /// Determines whether an automation element is likely to represent
        /// the SSMS Results Grid or one of its immediate containers.
        /// </summary>
        private static bool LooksRelevant(
            string value,
            ControlType? controlType)
        {
            // Strong UI Automation candidates.
            if (controlType == ControlType.DataGrid ||
                controlType == ControlType.Table)
            {
                return true;
            }

            // SSMS may expose its Results Grid as a custom/native control.
            if (value.IndexOf(
                    "Grid",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (value.IndexOf(
                    "Result",
                    StringComparison.OrdinalIgnoreCase) >= 0 &&
                (controlType == ControlType.Pane ||
                 controlType == ControlType.Custom ||
                 controlType == ControlType.Table ||
                 controlType == ControlType.DataGrid))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the active SSMS Results pane and inspects all native child windows
        /// hosted underneath it.
        /// </summary>
        public static string InspectResultsPaneChildren()
        {
            var process = Process.GetCurrentProcess();

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                return "SSMS main window handle was not available.";
            }

            AutomationElement root;

            try
            {
                root = AutomationElement.FromHandle(process.MainWindowHandle);
            }
            catch (Exception ex)
            {
                return $"Unable to access the SSMS automation root.\n\n{ex.Message}";
            }

            var resultsPanes = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        "Results"),
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ControlType.Pane)));

            AutomationElement? activeResultsPane = null;

            foreach (AutomationElement element in resultsPanes)
            {
                try
                {
                    if (element.Current.HasKeyboardFocus)
                    {
                        activeResultsPane = element;
                        break;
                    }

                    if (activeResultsPane is null &&
                        !element.Current.IsOffscreen)
                    {
                        activeResultsPane = element;
                    }
                }
                catch (ElementNotAvailableException)
                {
                    // Ignore controls that disappear while SSMS updates its UI.
                }
            }

            if (activeResultsPane is null)
            {
                return "No active Results pane was detected.";
            }

            var nativeHandle =
                new IntPtr(activeResultsPane.Current.NativeWindowHandle);

            if (nativeHandle == IntPtr.Zero)
            {
                return "The active Results pane does not expose a native HWND.";
            }

            var entries = new List<string>();

            // Inspect the Results host itself.
            AddNativeWindowEntry(nativeHandle, entries);

            // Inspect every native child hosted inside Results.
            EnumChildWindows(
                nativeHandle,
                (childHandle, _) =>
                {
                    AddNativeWindowEntry(childHandle, entries);
                    return entries.Count < 100;
                },
                IntPtr.Zero);

            var builder = new StringBuilder();

            builder.AppendLine(
                $"Results HWND: 0x{nativeHandle.ToInt64():X}");

            builder.AppendLine(
                $"Native controls found: {entries.Count}");

            builder.AppendLine();

            for (var i = 0; i < entries.Count; i++)
            {
                builder.AppendLine($"{i + 1}. {entries[i]}");
                builder.AppendLine();
            }

            return builder.ToString();
        }



        /// <summary>
        /// Captures diagnostic information about one native child window.
        /// </summary>
        private static void AddNativeWindowEntry(
            IntPtr handle,
            ICollection<string> entries)
        {
            var classBuilder = new StringBuilder(512);
            GetClassName(
                handle,
                classBuilder,
                classBuilder.Capacity);

            var textBuilder = new StringBuilder(1024);
            GetWindowText(
                handle,
                textBuilder,
                textBuilder.Capacity);

            Control? control = null;

            try
            {
                control = Control.FromHandle(handle);
            }
            catch
            {
                // A native child does not necessarily have a managed WinForms wrapper.
            }

            var managedType =
                control?.GetType().AssemblyQualifiedName
                ?? "<none>";

            var controlName =
                string.IsNullOrWhiteSpace(control?.Name)
                    ? "<none>"
                    : control!.Name;

            entries.Add(
                $"HWND: 0x{handle.ToInt64():X}\n" +
                $"Class: {Normalize(classBuilder.ToString())}\n" +
                $"Text: {Normalize(textBuilder.ToString())}\n" +
                $"Managed type: {managedType}\n" +
                $"Control name: {controlName}");
        }
        /// <summary>
        /// Reports UI Automation patterns supported by an element.
        /// </summary>
        private static string GetPatternNames(AutomationElement element)
        {
            var patterns = new List<string>();

            AddPattern<GridPattern>(
                element,
                GridPattern.Pattern,
                "Grid",
                patterns);

            AddPattern<TablePattern>(
                element,
                TablePattern.Pattern,
                "Table",
                patterns);

            AddPattern<SelectionPattern>(
                element,
                SelectionPattern.Pattern,
                "Selection",
                patterns);

            AddPattern<SelectionItemPattern>(
                element,
                SelectionItemPattern.Pattern,
                "SelectionItem",
                patterns);

            AddPattern<ValuePattern>(
                element,
                ValuePattern.Pattern,
                "Value",
                patterns);

            return patterns.Count == 0
                ? "<none>"
                : string.Join(", ", patterns);
        }

        private static void AddPattern<T>(
            AutomationElement element,
            AutomationPattern pattern,
            string name,
            ICollection<string> patterns)
        {
            if (element.TryGetCurrentPattern(pattern, out _))
            {
                patterns.Add(name);
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<none>"
                : value;
        }

        /// <summary>
        /// Inspects the runtime type information exposed by the active SSMS
        /// Results Grid without modifying the query results.
        /// </summary>
        public static string InspectGridRuntimeType()
        {
            var process = Process.GetCurrentProcess();

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                return "SSMS main window handle was not available.";
            }

            AutomationElement root;

            try
            {
                root = AutomationElement.FromHandle(process.MainWindowHandle);
            }
            catch (Exception ex)
            {
                return $"Unable to access the SSMS automation root.\n\n{ex.Message}";
            }

            var resultsPanes = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        "Results"),
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ControlType.Pane)));

            foreach (AutomationElement element in resultsPanes)
            {
                try
                {
                    if (!element.Current.HasKeyboardFocus)
                    {
                        continue;
                    }

                    var handle =
                        new IntPtr(element.Current.NativeWindowHandle);

                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    var control = Control.FromHandle(handle);

                    if (control is null)
                    {
                        return "The Results HWND does not map to a managed control.";
                    }

                    return DescribeRuntimeType(control);
                }
                catch (ElementNotAvailableException)
                {
                    // SSMS may rebuild the Results UI while probing it.
                }
            }

            return "No focused Results Grid control was detected.";
        }

        /// <summary>
        /// Returns useful runtime metadata for the discovered SSMS Grid control.
        /// </summary>
        private static string DescribeRuntimeType(Control control)
        {
            var type = control.GetType();
            var builder = new StringBuilder();

            builder.AppendLine($"Type: {type.FullName}");
            builder.AppendLine($"Assembly: {type.Assembly.FullName}");
            builder.AppendLine($"Assembly location: {type.Assembly.Location}");
            builder.AppendLine($"Base type: {type.BaseType?.FullName ?? "<none>"}");
            builder.AppendLine();

            builder.AppendLine("Interfaces:");

            foreach (var interfaceType in type.GetInterfaces()
                         .OrderBy(item => item.FullName))
            {
                builder.AppendLine($"- {interfaceType.FullName}");
            }

            builder.AppendLine();
            builder.AppendLine("Interesting properties:");

            var properties = type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (var property in properties
                         .Where(property =>
                             IsInterestingMember(property.Name))
                         .OrderBy(property => property.Name))
            {
                builder.AppendLine(
                    $"- {property.PropertyType.FullName} {property.Name}");
            }

            builder.AppendLine();
            builder.AppendLine("Interesting fields:");

            var fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (var field in fields
                         .Where(field =>
                             IsInterestingMember(field.Name))
                         .OrderBy(field => field.Name))
            {
                builder.AppendLine(
                    $"- {field.FieldType.FullName} {field.Name}");
            }

            builder.AppendLine();
            builder.AppendLine("Interesting methods:");

            var methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (var method in methods
                         .Where(method =>
                             IsInterestingMember(method.Name))
                         .Select(method => method.Name)
                         .Distinct()
                         .OrderBy(name => name))
            {
                builder.AppendLine($"- {method}");
            }

            return builder.ToString();
        }

        private static bool IsInterestingMember(string name)
        {
            return name.IndexOf("Grid", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Select", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Storage", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Cell", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Row", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Column", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Data", StringComparison.OrdinalIgnoreCase) >= 0;
        }


        public static string InspectSelectedCells()
        {
            var process = Process.GetCurrentProcess();

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                return "SSMS main window handle was not available.";
            }

            AutomationElement root;

            try
            {
                root = AutomationElement.FromHandle(process.MainWindowHandle);
            }
            catch (Exception ex)
            {
                return $"Unable to access the SSMS automation root.\n\n{ex.Message}";
            }

            var resultsPanes = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        "Results"),
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ControlType.Pane)));

            foreach (AutomationElement element in resultsPanes)
            {
                try
                {
                    if (!element.Current.HasKeyboardFocus)
                    {
                        continue;
                    }

                    var handle = new IntPtr(
                        element.Current.NativeWindowHandle);

                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    var control = Control.FromHandle(handle);

                    if (control is not IGridControl grid)
                    {
                        continue;
                    }

                    return DescribeSelection(grid);
                }
                catch (ElementNotAvailableException)
                {
                    // Ignore transient UI changes.
                }
            }

            return "No focused SSMS Results Grid was detected.";
        }



        private static string DescribeSelection(IGridControl grid)
        {
            var builder = new StringBuilder();

            var selectedBlocks = grid.SelectedCells;
            var storage = grid.GridStorage;

            builder.AppendLine(
                $"Rows in storage: {storage?.NumRows() ?? 0}");

            builder.AppendLine(
                $"Columns: {grid.ColumnsNumber}");

            builder.AppendLine(
                $"Selected blocks: {selectedBlocks?.Count ?? 0}");

            builder.AppendLine();

            if (selectedBlocks is null || selectedBlocks.Count == 0)
            {
                builder.AppendLine("No cells are currently selected.");
                return builder.ToString();
            }

            var blockIndex = 0;

            foreach (BlockOfCells block in selectedBlocks)
            {
                blockIndex++;

                builder.AppendLine($"Block {blockIndex}:");
                builder.AppendLine(
                    $"Rows: {block.Y} -> {block.Bottom}");

                builder.AppendLine(
                    $"Columns: {block.X} -> {block.Right}");

                builder.AppendLine(
                    $"Width: {block.Width}");

                builder.AppendLine(
                    $"Height: {block.Height}");

                builder.AppendLine();
            }

            return builder.ToString();
        }


        /// <summary>
        /// Reads values from the cells currently selected in the active SSMS Results Grid.
        /// This operation is read-only and does not re-run the SQL query.
        /// </summary>
        public static string InspectSelectedCellValues()
        {
            var process = Process.GetCurrentProcess();

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                return "SSMS main window handle was not available.";
            }

            AutomationElement root;

            try
            {
                root = AutomationElement.FromHandle(process.MainWindowHandle);
            }
            catch (Exception ex)
            {
                return $"Unable to access the SSMS automation root.\n\n{ex.Message}";
            }

            var resultsPanes = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        "Results"),
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ControlType.Pane)));

            foreach (AutomationElement element in resultsPanes)
            {
                try
                {
                    if (!element.Current.HasKeyboardFocus)
                    {
                        continue;
                    }

                    var handle =
                        new IntPtr(element.Current.NativeWindowHandle);

                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    var control = Control.FromHandle(handle);

                    if (control is not GridControl grid)
                    {
                        continue;
                    }

                    return DescribeSelectedCellValues(grid);
                }
                catch (ElementNotAvailableException)
                {
                    // SSMS may rebuild the Results UI while the probe is running.
                }
            }

            return "No focused SSMS Results Grid was detected.";
        }


        /// <summary>
        /// Returns diagnostic information and values for selected grid cells.
        /// </summary>
        private static string DescribeSelectedCellValues(GridControl grid)
        {
            const int maxCellsToDisplay = 100;

            var selectedBlocks = grid.SelectedCells;
            var storage = grid.GridStorage;

            if (storage is null)
            {
                return "The active Results Grid does not expose grid storage.";
            }

            if (selectedBlocks is null || selectedBlocks.Count == 0)
            {
                return "No cells are currently selected.";
            }

            var builder = new StringBuilder();

            var displayedCells = 0;
            long totalSelectedCells = 0;

            foreach (BlockOfCells block in selectedBlocks)
            {
                totalSelectedCells +=
                    block.Height * block.Width;
            }

            builder.AppendLine(
                $"Selected cells: {totalSelectedCells}");

            builder.AppendLine(
                $"Rows in storage: {storage.NumRows()}");

            builder.AppendLine();

            foreach (BlockOfCells block in selectedBlocks)
            {
                for (long row = block.Y; row <= block.Bottom; row++)
                {
                    for (var uiColumn = block.X;
                         uiColumn <= block.Right;
                         uiColumn++)
                    {
                        if (displayedCells >= maxCellsToDisplay)
                        {
                            builder.AppendLine();
                            builder.AppendLine(
                                $"Output limited to {maxCellsToDisplay} cells.");

                            return builder.ToString();
                        }

                        var storageColumn =
                            grid.GetStorageColumnIndexByUIIndex(uiColumn);

                        if (storageColumn < 0)
                        {
                            builder.AppendLine(
                                $"Row {row} | UI Col {uiColumn} | " +
                                "Storage Col <invalid>");

                            continue;
                        }

                        string value;

                        try
                        {
                            value = storage.GetCellDataAsString(
                                        row,
                                        storageColumn)
                                    ?? "<null>";
                        }
                        catch (Exception ex)
                        {
                            value =
                                $"<read error: {ex.GetType().Name}>";
                        }

                        builder.AppendLine(
                            $"Row {row} | " +
                            $"UI Col {uiColumn} | " +
                            $"Storage Col {storageColumn} | " +
                            $"Value: {value}");

                        displayedCells++;
                    }
                }
            }

            return builder.ToString();
        }

        public static string SumSelectedCells()
        {
            var process = Process.GetCurrentProcess();

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                return "SSMS main window handle was not available.";
            }

            AutomationElement root;

            try
            {
                root = AutomationElement.FromHandle(process.MainWindowHandle);
            }
            catch (Exception ex)
            {
                return $"Unable to access the SSMS automation root.\n\n{ex.Message}";
            }

            var resultsPanes = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        "Results"),
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ControlType.Pane)));

            foreach (AutomationElement element in resultsPanes)
            {
                try
                {
                    if (!element.Current.HasKeyboardFocus)
                    {
                        continue;
                    }

                    var handle = new IntPtr(
                        element.Current.NativeWindowHandle);

                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    var control = Control.FromHandle(handle);

                    if (control is not GridControl grid)
                    {
                        continue;
                    }

                    return CalculateSelectionSum(grid);
                }
                catch (ElementNotAvailableException)
                {
                    // SSMS may rebuild the Results pane while it is being inspected.
                }
            }

            return "No focused SSMS Results Grid was detected.";
        }

        private static string CalculateSelectionSum(GridControl grid)
        {
            GridSelectionAnalytics.NumericSelectionSummary summary;

            try
            {
                summary = GridSelectionAnalytics.Analyze(grid);
            }
            catch (OverflowException)
            {
                return
                    "SUM could not be calculated because the selected " +
                    "numeric values exceed the supported decimal range.";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }

            if (summary.SelectedCount == 0)
            {
                return "No cells are currently selected.";
            }

            if (summary.NumericCount == 0)
            {
                return
                    $"No numeric values were found.\n\n" +
                    $"Selected: {summary.SelectedCount}\n" +
                    $"NULL / Empty: {summary.NullOrEmptyCount}\n" +
                    $"Non-numeric: {summary.NonNumericCount}";
            }

            return
                $"SUM = {summary.Sum.ToString("G29", CultureInfo.CurrentCulture)}\n\n" +
                $"Numeric cells: {summary.NumericCount}\n" +
                $"Selected cells: {summary.SelectedCount}\n" +
                $"NULL / Empty: {summary.NullOrEmptyCount}\n" +
                $"Non-numeric: {summary.NonNumericCount}";
        }




        public static string AverageSelectedCells()
        {
            return FormatNumericStatistic(
                "AVERAGE",
                summary => summary.Average);
        }

        public static string MinSelectedCells()
        {
            return FormatNumericStatistic(
                "MIN",
                summary => summary.Min);
        }

        public static string MaxSelectedCells()
        {
            return FormatNumericStatistic(
                "MAX",
                summary => summary.Max);
        }

        public static string CountSelectedCells()
        {
            return FormatNumericCount();
        }

        private static string FormatNumericStatistic(
            string label,
            Func<GridSelectionAnalytics.NumericSelectionSummary, decimal?> selector)
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            GridSelectionAnalytics.NumericSelectionSummary summary;

            try
            {
                summary = GridSelectionAnalytics.Analyze(grid);
            }
            catch (OverflowException)
            {
                return
                    $"{label} could not be calculated because the selected " +
                    "numeric values exceed the supported decimal range.";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }

            if (summary.SelectedCount == 0)
            {
                return "No cells are currently selected.";
            }

            if (summary.NumericCount == 0)
            {
                return
                    $"No numeric values were found.\n\n" +
                    $"Selected: {summary.SelectedCount}\n" +
                    $"NULL / Empty: {summary.NullOrEmptyCount}\n" +
                    $"Non-numeric: {summary.NonNumericCount}";
            }

            var value = selector(summary);

            return
                $"{label} = {value?.ToString("G29", CultureInfo.CurrentCulture)}\n\n" +
                $"Numeric cells: {summary.NumericCount}\n" +
                $"Selected cells: {summary.SelectedCount}\n" +
                $"NULL / Empty: {summary.NullOrEmptyCount}\n" +
                $"Non-numeric: {summary.NonNumericCount}";
        }

        private static string FormatNumericCount()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            GridSelectionAnalytics.NumericSelectionSummary summary;

            try
            {
                summary = GridSelectionAnalytics.Analyze(grid);
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }

            if (summary.SelectedCount == 0)
            {
                return "No cells are currently selected.";
            }

            return
                $"COUNT = {summary.NumericCount}\n\n" +
                $"Selected cells: {summary.SelectedCount}\n" +
                $"NULL / Empty: {summary.NullOrEmptyCount}\n" +
                $"Non-numeric: {summary.NonNumericCount}";
        }


        private static GridControl? GetFocusedResultsGrid()
        {
            var process = Process.GetCurrentProcess();

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                return null;
            }

            AutomationElement root;

            try
            {
                root = AutomationElement.FromHandle(process.MainWindowHandle);
            }
            catch
            {
                return null;
            }

            var resultsPanes = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        "Results"),
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ControlType.Pane)));

            foreach (AutomationElement element in resultsPanes)
            {
                try
                {
                    if (!element.Current.HasKeyboardFocus)
                    {
                        continue;
                    }

                    var handle = new IntPtr(
                        element.Current.NativeWindowHandle);

                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    var control = Control.FromHandle(handle);

                    if (control is GridControl grid)
                    {
                        return grid;
                    }
                }
                catch (ElementNotAvailableException)
                {
                    // SSMS may rebuild the Results pane while it is inspected.
                }
            }

            return null;
        }

        internal static string InspectGridContextMenuRuntime()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            var builder = new StringBuilder();

            builder.AppendLine("Zraxion Results Grid Context Menu Diagnostic");
            builder.AppendLine(new string('=', 44));
            builder.AppendLine();

            builder.AppendLine($"Runtime type: {grid.GetType().FullName}");
            builder.AppendLine($"Assembly: {grid.GetType().Assembly.FullName}");
            builder.AppendLine($"Base type: {grid.GetType().BaseType?.FullName ?? "<none>"}");
            builder.AppendLine();

            builder.AppendLine("WinForms context menu properties:");
            builder.AppendLine(
                $"- ContextMenuStrip: {DescribeObject(grid.ContextMenuStrip)}");
            builder.AppendLine(
                $"- ContextMenu: {DescribeObject(grid.ContextMenu)}");
            builder.AppendLine();

            builder.AppendLine("Type hierarchy:");
            for (Type? type = grid.GetType();
                 type is not null;
                 type = type.BaseType)
            {
                builder.AppendLine($"- {type.FullName}");
            }

            builder.AppendLine();
            builder.AppendLine("Interesting properties:");

            foreach (var property in GetContextMenuRelatedProperties(grid.GetType()))
            {
                builder.AppendLine(
                    $"- {property.DeclaringType?.FullName}.{property.Name} " +
                    $"[{property.PropertyType.FullName}] = " +
                    TryDescribePropertyValue(grid, property));
            }

            builder.AppendLine();
            builder.AppendLine("Interesting fields:");

            foreach (var field in GetContextMenuRelatedFields(grid.GetType()))
            {
                builder.AppendLine(
                    $"- {field.DeclaringType?.FullName}.{field.Name} " +
                    $"[{field.FieldType.FullName}] = " +
                    TryDescribeFieldValue(grid, field));
            }

            builder.AppendLine();
            builder.AppendLine("Interesting events:");

            foreach (var eventInfo in GetContextMenuRelatedEvents(grid.GetType()))
            {
                builder.AppendLine(
                    $"- {eventInfo.DeclaringType?.FullName}.{eventInfo.Name} " +
                    $"[{eventInfo.EventHandlerType?.FullName ?? "<unknown>"}]");
            }

            builder.AppendLine();
            builder.AppendLine("Interesting methods:");

            foreach (var method in GetContextMenuRelatedMethods(grid.GetType()))
            {
                var parameters =
                    string.Join(
                        ", ",
                        method
                            .GetParameters()
                            .Select(
                                parameter =>
                                    $"{parameter.ParameterType.Name} {parameter.Name}"));

                builder.AppendLine(
                    $"- {method.DeclaringType?.FullName}.{method.Name}({parameters})");
            }

            builder.AppendLine();
            builder.AppendLine("GridResultsGrid.OnMouseDown IL:");
            builder.AppendLine(
                DumpMethodIl(
                    grid.GetType(),
                    "OnMouseDown"));

            var gridControlType = grid.GetType().BaseType;

            if (gridControlType is not null)
            {
                builder.AppendLine();
                builder.AppendLine("GridControl.OnMouseDown IL:");
                builder.AppendLine(
                    DumpMethodIl(
                        gridControlType,
                        "OnMouseDown"));

                builder.AppendLine();
                builder.AppendLine("GridControl.HandleCustomCellMouseBtnDown IL:");
                builder.AppendLine(
                    DumpMethodIl(
                        gridControlType,
                        "HandleCustomCellMouseBtnDown"));

                builder.AppendLine();
                builder.AppendLine("GridControl.OnMouseButtonClicking IL:");
                builder.AppendLine(
                    DumpMethodIl(
                        gridControlType,
                        "OnMouseButtonClicking"));

                builder.AppendLine();
                builder.AppendLine("GridControl.OnMouseButtonClicked IL:");
                builder.AppendLine(
                    DumpMethodIl(
                        gridControlType,
                        "OnMouseButtonClicked"));

                builder.AppendLine();
                builder.AppendLine("GridControl.OnGridSpecialEvent IL:");
                builder.AppendLine(
                    DumpMethodIl(
                        gridControlType,
                        "OnGridSpecialEvent"));
            }

            return builder.ToString();
        }

        private static string DumpMethodIl(
            Type runtimeType,
            string methodName)
        {
            var method =
                runtimeType
                    .GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly)
                    .FirstOrDefault(
                        candidate =>
                            string.Equals(
                                candidate.Name,
                                methodName,
                                StringComparison.Ordinal));

            if (method is null)
            {
                return "<method not found on runtime type>";
            }

            var body = method.GetMethodBody();

            if (body is null)
            {
                return "<method has no IL body>";
            }

            var bytes = body.GetILAsByteArray();

            if (bytes is null || bytes.Length == 0)
            {
                return "<empty IL body>";
            }

            var oneByte = new OpCode[0x100];
            var twoByte = new OpCode[0x100];

            foreach (var field in typeof(OpCodes).GetFields(
                         BindingFlags.Public |
                         BindingFlags.Static))
            {
                if (field.GetValue(null) is not OpCode opCode)
                {
                    continue;
                }

                var value = unchecked((ushort)opCode.Value);

                if (value < 0x100)
                {
                    oneByte[value] = opCode;
                }
                else if ((value & 0xFF00) == 0xFE00)
                {
                    twoByte[value & 0xFF] = opCode;
                }
            }

            var builder = new StringBuilder();
            var module = method.Module;
            var position = 0;

            while (position < bytes.Length)
            {
                var offset = position;
                OpCode opCode;

                var first = bytes[position++];

                if (first == 0xFE)
                {
                    if (position >= bytes.Length)
                    {
                        builder.AppendLine(
                            $"IL_{offset:X4}: <truncated opcode>");
                        break;
                    }

                    opCode = twoByte[bytes[position++]];
                }
                else
                {
                    opCode = oneByte[first];
                }

                if (opCode.Size == 0)
                {
                    builder.AppendLine(
                        $"IL_{offset:X4}: <unknown opcode 0x{first:X2}>");
                    break;
                }

                string operand;

                try
                {
                    operand =
                        ReadIlOperand(
                            bytes,
                            ref position,
                            opCode,
                            module,
                            method);
                }
                catch (Exception ex)
                {
                    operand =
                        $"<operand decode failed: {ex.GetType().Name}>";

                    builder.AppendLine(
                        $"IL_{offset:X4}: {opCode.Name} {operand}");

                    break;
                }

                builder.AppendLine(
                    string.IsNullOrEmpty(operand)
                        ? $"IL_{offset:X4}: {opCode.Name}"
                        : $"IL_{offset:X4}: {opCode.Name} {operand}");
            }

            return builder.ToString();
        }

        private static string ReadIlOperand(
            byte[] il,
            ref int position,
            OpCode opCode,
            Module module,
            MethodBase contextMethod)
        {
            switch (opCode.OperandType)
            {
                case OperandType.InlineNone:
                    return string.Empty;

                case OperandType.ShortInlineI:
                    return il[position++].ToString(
                        CultureInfo.InvariantCulture);

                case OperandType.InlineI:
                    {
                        var value =
                            BitConverter.ToInt32(il, position);
                        position += 4;
                        return value.ToString(
                            CultureInfo.InvariantCulture);
                    }

                case OperandType.InlineI8:
                    {
                        var value =
                            BitConverter.ToInt64(il, position);
                        position += 8;
                        return value.ToString(
                            CultureInfo.InvariantCulture);
                    }

                case OperandType.ShortInlineR:
                    {
                        var value =
                            BitConverter.ToSingle(il, position);
                        position += 4;
                        return value.ToString(
                            CultureInfo.InvariantCulture);
                    }

                case OperandType.InlineR:
                    {
                        var value =
                            BitConverter.ToDouble(il, position);
                        position += 8;
                        return value.ToString(
                            CultureInfo.InvariantCulture);
                    }

                case OperandType.ShortInlineBrTarget:
                    {
                        var delta =
                            unchecked((sbyte)il[position++]);
                        return
                            $"IL_{position + delta:X4}";
                    }

                case OperandType.InlineBrTarget:
                    {
                        var delta =
                            BitConverter.ToInt32(il, position);
                        position += 4;
                        return
                            $"IL_{position + delta:X4}";
                    }

                case OperandType.InlineSwitch:
                    {
                        var count =
                            BitConverter.ToInt32(il, position);
                        position += 4;

                        var basePosition =
                            position + (count * 4);

                        var targets =
                            new string[count];

                        for (var i = 0; i < count; i++)
                        {
                            var delta =
                                BitConverter.ToInt32(
                                    il,
                                    position);

                            position += 4;

                            targets[i] =
                                $"IL_{basePosition + delta:X4}";
                        }

                        return
                            string.Join(", ", targets);
                    }

                case OperandType.InlineString:
                    {
                        var token =
                            BitConverter.ToInt32(il, position);
                        position += 4;

                        try
                        {
                            return
                                $"\"{module.ResolveString(token)}\"";
                        }
                        catch
                        {
                            return
                                $"string-token 0x{token:X8}";
                        }
                    }

                case OperandType.InlineMethod:
                    {
                        var token =
                            BitConverter.ToInt32(il, position);
                        position += 4;

                        return
                            ResolveMemberToken(
                                module,
                                token,
                                contextMethod,
                                MemberTypes.Method |
                                MemberTypes.Constructor);
                    }

                case OperandType.InlineField:
                    {
                        var token =
                            BitConverter.ToInt32(il, position);
                        position += 4;

                        return
                            ResolveMemberToken(
                                module,
                                token,
                                contextMethod,
                                MemberTypes.Field);
                    }

                case OperandType.InlineType:
                    {
                        var token =
                            BitConverter.ToInt32(il, position);
                        position += 4;

                        return
                            ResolveTypeToken(
                                module,
                                token,
                                contextMethod);
                    }

                case OperandType.InlineTok:
                    {
                        var token =
                            BitConverter.ToInt32(il, position);
                        position += 4;

                        return
                            ResolveGenericMemberToken(
                                module,
                                token,
                                contextMethod);
                    }

                case OperandType.InlineSig:
                    {
                        var token =
                            BitConverter.ToInt32(il, position);
                        position += 4;

                        return
                            $"signature-token 0x{token:X8}";
                    }

                case OperandType.ShortInlineVar:
                    return
                        $"var {il[position++]}";

                case OperandType.InlineVar:
                    {
                        var index =
                            BitConverter.ToUInt16(il, position);
                        position += 2;
                        return
                            $"var {index}";
                    }

                default:
                    return
                        $"<unsupported operand type: {opCode.OperandType}>";
            }
        }

        private static string ResolveMemberToken(
            Module module,
            int token,
            MethodBase contextMethod,
            MemberTypes allowedTypes)
        {
            try
            {
                var member =
                    module.ResolveMember(
                        token,
                        GetGenericTypeArguments(contextMethod),
                        GetGenericMethodArguments(contextMethod));

                if (member is null)
                {
                    return $"token 0x{token:X8}";
                }

                if ((member.MemberType & allowedTypes) == 0)
                {
                    return
                        $"{member.DeclaringType?.FullName}.{member.Name}";
                }

                return
                    $"{member.DeclaringType?.FullName}.{member.Name}";
            }
            catch
            {
                return
                    $"token 0x{token:X8}";
            }
        }

        private static string ResolveTypeToken(
            Module module,
            int token,
            MethodBase contextMethod)
        {
            try
            {
                var type =
                    module.ResolveType(
                        token,
                        GetGenericTypeArguments(contextMethod),
                        GetGenericMethodArguments(contextMethod));

                return type.FullName ?? type.Name;
            }
            catch
            {
                return $"type-token 0x{token:X8}";
            }
        }

        private static string ResolveGenericMemberToken(
            Module module,
            int token,
            MethodBase contextMethod)
        {
            try
            {
                var member =
                    module.ResolveMember(
                        token,
                        GetGenericTypeArguments(contextMethod),
                        GetGenericMethodArguments(contextMethod));

                return
                    member is null
                        ? $"token 0x{token:X8}"
                        : $"{member.DeclaringType?.FullName}.{member.Name}";
            }
            catch
            {
                return $"token 0x{token:X8}";
            }
        }

        private static Type[]? GetGenericTypeArguments(
            MethodBase method)
        {
            return
                method.DeclaringType?.IsGenericType == true
                    ? method.DeclaringType.GetGenericArguments()
                    : null;
        }

        private static Type[]? GetGenericMethodArguments(
            MethodBase method)
        {
            return
                method.IsGenericMethod
                    ? method.GetGenericArguments()
                    : null;
        }

        private static IEnumerable<PropertyInfo>
            GetContextMenuRelatedProperties(Type type)
        {
            return GetTypeHierarchy(type)
                .SelectMany(
                    current =>
                        current.GetProperties(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly))
                .Where(
                    property =>
                        IsContextMenuRelatedName(property.Name) ||
                        IsContextMenuRelatedType(property.PropertyType))
                .OrderBy(property => property.Name);
        }

        private static IEnumerable<FieldInfo>
            GetContextMenuRelatedFields(Type type)
        {
            return GetTypeHierarchy(type)
                .SelectMany(
                    current =>
                        current.GetFields(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly))
                .Where(
                    field =>
                        IsContextMenuRelatedName(field.Name) ||
                        IsContextMenuRelatedType(field.FieldType))
                .OrderBy(field => field.Name);
        }

        private static IEnumerable<EventInfo>
            GetContextMenuRelatedEvents(Type type)
        {
            return GetTypeHierarchy(type)
                .SelectMany(
                    current =>
                        current.GetEvents(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly))
                .Where(
                    eventInfo =>
                        IsContextMenuRelatedName(eventInfo.Name) ||
                        (eventInfo.EventHandlerType is not null &&
                         IsContextMenuRelatedType(
                             eventInfo.EventHandlerType)))
                .OrderBy(eventInfo => eventInfo.Name);
        }

        private static IEnumerable<MethodInfo>
            GetContextMenuRelatedMethods(Type type)
        {
            return GetTypeHierarchy(type)
                .SelectMany(
                    current =>
                        current.GetMethods(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly))
                .Where(
                    method =>
                        IsContextMenuRelatedName(method.Name) ||
                        IsContextMenuRelatedType(method.ReturnType) ||
                        method
                            .GetParameters()
                            .Any(
                                parameter =>
                                    IsContextMenuRelatedType(
                                        parameter.ParameterType)))
                .Where(method => !method.IsSpecialName)
                .OrderBy(method => method.Name);
        }

        private static IEnumerable<Type> GetTypeHierarchy(Type type)
        {
            for (Type? current = type;
                 current is not null;
                 current = current.BaseType)
            {
                yield return current;
            }
        }

        private static bool IsContextMenuRelatedName(string name)
        {
            return
                name.IndexOf(
                    "Context",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf(
                    "Menu",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf(
                    "Popup",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf(
                    "Command",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf(
                    "Mouse",
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsContextMenuRelatedType(Type type)
        {
            var name = type.FullName ?? type.Name;

            return IsContextMenuRelatedName(name);
        }

        private static string TryDescribePropertyValue(
            object instance,
            PropertyInfo property)
        {
            if (property.GetIndexParameters().Length != 0)
            {
                return "<indexed>";
            }

            var getter = property.GetGetMethod(nonPublic: true);

            if (getter is null)
            {
                return "<no getter>";
            }

            try
            {
                return DescribeObject(
                    getter.Invoke(instance, null));
            }
            catch (Exception ex)
            {
                return $"<read failed: {ex.GetType().Name}>";
            }
        }

        private static string TryDescribeFieldValue(
            object instance,
            FieldInfo field)
        {
            try
            {
                return DescribeObject(
                    field.GetValue(instance));
            }
            catch (Exception ex)
            {
                return $"<read failed: {ex.GetType().Name}>";
            }
        }

        private static string DescribeObject(object? value)
        {
            if (value is null)
            {
                return "<null>";
            }

            return
                $"{value.GetType().FullName} :: {value}";
        }

        internal static GridControl? GetFocusedResultsGridForUi()
        {
            return GetFocusedResultsGrid();
        }

        internal static int GetResultGridIndexForUi(
            GridControl grid)
        {
            return GetResultGridIndex(grid);
        }


        public static string SetCurrentSelectionAsComparisonBaseline()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            GridAnalyticsSnapshot snapshot;

            try
            {
                snapshot = GridSelectionAnalytics.CreateSnapshot(
                    grid,
                    "A");
            }
            catch (OverflowException)
            {
                return
                    "The comparison baseline could not be created because " +
                    "the selected numeric values exceed the supported decimal range.";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }

            if (snapshot.SelectedCount == 0)
            {
                return "No cells are currently selected.";
            }

            if (snapshot.NumericCount == 0)
            {
                return
                    "The selected cells do not contain numeric values.";
            }

            GridComparisonSession.SetLeft(snapshot);

            return
                $"Comparison baseline A saved.\n\n" +
                $"SUM = {snapshot.Sum.ToString("G29", CultureInfo.CurrentCulture)}\n" +
                $"Numeric cells: {snapshot.NumericCount}\n" +
                $"Selected cells: {snapshot.SelectedCount}";
        }

        public static string CompareCurrentSelectionWithBaseline()
        {
            var baseline = GridComparisonSession.LeftSnapshot;

            if (baseline is null)
            {
                return
                    "No comparison baseline has been saved.\n\n" +
                    "Select numeric cells and use 'Set Selection as A' first.";
            }

            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            GridAnalyticsSnapshot current;

            try
            {
                current = GridSelectionAnalytics.CreateSnapshot(
                    grid,
                    "B");
            }
            catch (OverflowException)
            {
                return
                    "The comparison could not be completed because " +
                    "the selected numeric values exceed the supported decimal range.";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }

            if (current.SelectedCount == 0)
            {
                return "No cells are currently selected.";
            }

            if (current.NumericCount == 0)
            {
                return
                    "The current selection does not contain numeric values.";
            }

            var comparison =
                new GridAnalyticsComparison(
                    baseline,
                    current);
            GridAnalyticsBarManager.ShowComparison(
             comparison);



            return string.Empty;
        }
        public static string InspectResultsGridParentChain()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            var builder = new StringBuilder();

            Control? current = grid;
            var level = 0;

            while (current is not null && level < 12)
            {
                builder.AppendLine($"Level {level}");
                builder.AppendLine($"Type: {current.GetType().FullName}");
                builder.AppendLine($"Name: {current.Name}");
                builder.AppendLine($"Dock: {current.Dock}");
                builder.AppendLine($"Bounds: {current.Bounds}");
                builder.AppendLine($"ClientSize: {current.ClientSize}");
                builder.AppendLine($"Children: {current.Controls.Count}");
                builder.AppendLine();

                current = current.Parent;
                level++;
            }

            return builder.ToString();
        }
        public static string InspectResultsContainerChildren()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            var smartPanel = grid.Parent;
            var multiControlPanel = smartPanel?.Parent;

            if (smartPanel is null || multiControlPanel is null)
            {
                return "Expected Results Grid container hierarchy was not found.";
            }

            var builder = new StringBuilder();

            builder.AppendLine("=== SmartPanel ===");
            builder.AppendLine($"Type: {smartPanel.GetType().FullName}");
            builder.AppendLine($"Children: {smartPanel.Controls.Count}");
            builder.AppendLine();

            for (var i = 0; i < smartPanel.Controls.Count; i++)
            {
                var child = smartPanel.Controls[i];

                builder.AppendLine($"Child {i}");
                builder.AppendLine($"Type: {child.GetType().FullName}");
                builder.AppendLine($"Dock: {child.Dock}");
                builder.AppendLine($"Bounds: {child.Bounds}");
                builder.AppendLine();
            }

            builder.AppendLine("=== MultiControlPanel ===");
            builder.AppendLine($"Type: {multiControlPanel.GetType().FullName}");
            builder.AppendLine($"Children: {multiControlPanel.Controls.Count}");
            builder.AppendLine();

            for (var i = 0; i < multiControlPanel.Controls.Count; i++)
            {
                var child = multiControlPanel.Controls[i];

                builder.AppendLine($"Child {i}");
                builder.AppendLine($"Type: {child.GetType().FullName}");
                builder.AppendLine($"Dock: {child.Dock}");
                builder.AppendLine($"Bounds: {child.Bounds}");
                builder.AppendLine($"Children: {child.Controls.Count}");
                builder.AppendLine();
            }

            return builder.ToString();
        }

        public static string InspectSmartPanelRuntimeStructure()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            var smartPanel = grid.Parent;

            if (smartPanel is null)
            {
                return "SmartPanel parent was not found.";
            }

            var type = smartPanel.GetType();
            var builder = new StringBuilder();

            builder.AppendLine($"Type: {type.FullName}");
            builder.AppendLine();

            builder.AppendLine("=== Properties ===");

            foreach (var property in type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic))
            {
                if (property.Name.IndexOf("panel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    property.Name.IndexOf("control", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    property.Name.IndexOf("layout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    property.Name.IndexOf("split", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    property.Name.IndexOf("size", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    builder.AppendLine(
                        $"{property.PropertyType.FullName} {property.Name}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("=== Fields ===");

            foreach (var field in type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic))
            {
                if (field.Name.IndexOf("panel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    field.Name.IndexOf("control", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    field.Name.IndexOf("layout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    field.Name.IndexOf("split", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    field.Name.IndexOf("size", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    builder.AppendLine(
                        $"{field.FieldType.FullName} {field.Name}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("=== Methods ===");

            foreach (var method in type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic))
            {
                if (method.Name.IndexOf("layout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.Name.IndexOf("control", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.Name.IndexOf("split", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.Name.IndexOf("panel", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    builder.AppendLine(method.ToString());
                }
            }

            return builder.ToString();
        }


        public static string AttachTestAnalyticsBar()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            Control? current = grid;

            while (current is not null &&
                   current.GetType().FullName !=
                   "Microsoft.SqlServer.Management.UI.VSIntegration.Editors.GridResultsTabPage")
            {
                current = current.Parent;
            }

            if (current is null)
            {
                return "GridResultsTabPage was not found.";
            }

            const string barName = "ZraxionAnalyticsBar";
            const string labelName = "ZraxionAnalyticsLabel";

            Panel? bar = current.Controls[barName] as Panel;
            Label? label = null;

            if (bar is null)
            {
                bar = new Panel
                {
                    Name = barName,
                    Dock = DockStyle.Bottom,
                    Height = 36,
                    Padding = new Padding(10, 0, 10, 0)
                };

                label = new Label
                {
                    Name = labelName,
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                bar.Controls.Add(label);

                current.SuspendLayout();

                try
                {
                    current.Controls.Add(bar);
                    bar.BringToFront();
                }
                finally
                {
                    current.ResumeLayout(true);
                }
            }
            else
            {
                label = bar.Controls[labelName] as Label;
            }

            if (label is null)
            {
                return "Zraxion Analytics Bar exists, but its label could not be found.";
            }

            GridSelectionAnalytics.NumericSelectionSummary summary;

            try
            {
                summary = GridSelectionAnalytics.Analyze(grid);
            }
            catch (OverflowException)
            {
                label.Text =
                    "Zraxion  |  Selected values exceed the supported decimal range.";

                return "Analytics bar updated with an overflow warning.";
            }
            catch (InvalidOperationException ex)
            {
                label.Text = $"Zraxion  |  {ex.Message}";
                return ex.Message;
            }

            var resultIndex = GetResultGridIndex(grid);

            if (summary.SelectedCount == 0)
            {
                label.Text =
                    $"Zraxion  |  Result {resultIndex}  |  No cells selected";

                return "Analytics bar updated.";
            }

            if (summary.NumericCount == 0)
            {
                label.Text =
                    $"Zraxion  |  Result {resultIndex}  |  " +
                    $"No numeric values  |  Selected {summary.SelectedCount}";

                return "Analytics bar updated.";
            }

            var averageText =
                summary.Average.HasValue
                    ? summary.Average.Value.ToString(
                        "N2",
                        CultureInfo.CurrentCulture)
                    : "-";

            label.Text =
                $"Zraxion  |  Result {resultIndex}  |  " +
                $"SUM {summary.Sum.ToString("N2", CultureInfo.CurrentCulture)}  |  " +
                $"COUNT {summary.NumericCount}  |  " +
                $"AVG {averageText}";

            return
                $"Analytics bar updated for Result {resultIndex}.";
        }

        private static int GetResultGridIndex(GridControl grid)
        {
            var parent = grid.Parent;

            if (parent is null)
            {
                return 1;
            }

            var resultIndex = 0;

            foreach (Control control in parent.Controls)
            {
                if (control is GridControl resultGrid)
                {
                    resultIndex++;

                    if (ReferenceEquals(resultGrid, grid))
                    {
                        return resultIndex;
                    }
                }
            }

            return 1;
        }


    }
}