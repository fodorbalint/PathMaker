using SkiaSharp.Views.Desktop;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp.Views.WPF;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Windows.Automation;
using System.Diagnostics.Eventing.Reader;

namespace OneWayLabyrinth
{
    /// <summary>
    /// Interaction logic for Rules.xaml
    /// </summary>
    public partial class Rules : Window
    {
        string svgName;
        int size;
        int xSize;
        int ySize;
        string grid;
        string liveEnd, emptyField, takenField, takenOrBorderField, futureStartField, futureEndField, forbiddenField, notCornerField, countAreaPairStartField, countAreaPairEndField, countAreaPairBorderField, countAreaImpairStartField, countAreaImpairEndField, countAreaImpairBorderField;
        string newRule;
        int draggedElement = 0;
        SKElement draggedObj;
        double startMouseX, startMouseY;
        Thickness origMargin;
        List<int[]> takenCoordinates = new();
        List<int[]> forbiddenCoordinates = new();
        int[]? noCornerCoordinates = null;
        int[]? countAreaPairStartCoordinates = null;
        int[]? countAreaPairEndCoordinates = null;
        List<int[]> countAreaPairBorderCoordinates = new();
        int[]? countAreaImpairStartCoordinates = null;
        int[]? countAreaImpairEndCoordinates = null;
        List<int[]> countAreaImpairBorderCoordinates = new();
        int childIndex = 0;
        List<string> activeRules = new();
        List<int[]> directions = new() { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } };


        public Rules()
        {
            InitializeComponent();
            LoadDir();
            XSize.Text = "6";
            YSize.Text = "5";
            AppliedSize.Text = "9";
        }

        // ----- Generate grid and field types -----


        private void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            xSize = 1;
            ySize = 1;
            DrawGrid();

            string singleGrid = "<svg xmlns =\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\">\r\n\t<!---->\r\n" + grid + "</svg>";
            if (!File.Exists("singleGrid.svg")) File.WriteAllText("singleGrid.svg", singleGrid);

            string color = "#ff0000";
            string opacity = "0.15";
            liveEnd = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\r\n" +
                "\t<path d=\"M 0.5 1 v -0.5\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" />";
            string content = singleGrid.Replace("<!---->", liveEnd);
            if (!File.Exists("liveEnd.svg")) File.WriteAllText("liveEnd.svg", content);

            emptyField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"#dddddd\" fill-opacity=\"1\" />";
            content = singleGrid.Replace("<!---->", emptyField);
            if (!File.Exists("emptyField.svg")) File.WriteAllText("emptyField.svg", content);

            color = "#ff0000";
            opacity = "0.15";
            takenField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\r\n" + "\t<path d=\"M 0.3 0.5 h 0.4 M 0.5 0.3 v 0.4\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" />";
            content = singleGrid.Replace("<!---->", takenField);
            if (!File.Exists("takenField.svg")) File.WriteAllText("takenField.svg", content);

            color = "#000000";
            opacity = "0.5";
            takenOrBorderField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />";
            content = singleGrid.Replace("<!---->", takenOrBorderField);
            if (!File.Exists("takenOrBorderField.svg")) File.WriteAllText("takenOrBorderField.svg", content);

            color = "#0000ff";
            opacity = "0.15";
            futureStartField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\r\n" +
                "\t<path d=\"M 0.2 0.2 v 0.6 v -0.3 h 0.6 l -0.2 -0.2 l 0.2 0.2 l -0.2 0.2\" fill=\"white\" fill-opacity=\"0\" stroke=\"blue\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", futureStartField);
            if (!File.Exists("futureStartField.svg")) File.WriteAllText("futureStartField.svg", content);

            color = "#0000ff";
            opacity = "0.15";

            futureEndField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\r\n" +
                "\t<path d=\"M 0.2 0.5 h 0.6 l -0.2 -0.2 l 0.2 0.2 l -0.2 0.2 l 0.2 -0.2 v 0.3 v -0.6\" fill=\"white\" fill-opacity=\"0\" stroke=\"blue\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", futureEndField);
            if (!File.Exists("futureEndField.svg")) File.WriteAllText("futureEndField.svg", content);

            notCornerField = "<path d=\"M 0.3 0.2 v 0.5 h 0.5 h -0.2 a 0.3 0.3 0 0 0 -0.3 -0.3\" fill=\"white\" fill-opacity=\"0\" stroke=\"red\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" /><circle cx=\"0.42\" cy=\"0.58\" r=\"0.05\" fill=\"red\" fill-opacity=\"1\" />";
            content = singleGrid.Replace("<!---->", notCornerField);
            if (!File.Exists("notCornerField.svg")) File.WriteAllText("notCornerField.svg", content);

            forbiddenField = "<path d=\"M 0.2 0.2 l 0.6 0.6 M 0.2 0.8 l 0.6 -0.6\" fill=\"white\" fill-opacity=\"0\" stroke=\"red\" stroke-width=\"0.05\" stroke-linecap=\"round\" />";
            content = singleGrid.Replace("<!---->", forbiddenField);
            if (!File.Exists("forbiddenField.svg")) File.WriteAllText("forbiddenField.svg", content);

            countAreaPairStartField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#008000\" fill-opacity=\"0.25\" />\r\n\t<path d=\"M 0.3 0.3 v 0.4 v -0.2 h 0.4 l -0.13 -0.13 l 0.13 0.13 l -0.13 0.13\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", countAreaPairStartField);
            if (!File.Exists("countAreaPairStartField.svg")) File.WriteAllText("countAreaPairStartField.svg", content);

            countAreaPairEndField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#008000\" fill-opacity=\"0.25\" />\r\n\t<path d=\"M 0.3 0.5 h 0.4 l -0.13 -0.13 l 0.13 0.13 l -0.13 0.13 l 0.13 -0.13 v 0.2 v -0.4\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", countAreaPairEndField);
            if (!File.Exists("countAreaPairEndField.svg")) File.WriteAllText("countAreaPairEndField.svg", content);

            countAreaPairBorderField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#008000\" fill-opacity=\"0.25\" />";
            content = singleGrid.Replace("<!---->", countAreaPairBorderField);
            if (!File.Exists("countAreaPairBorderField.svg")) File.WriteAllText("countAreaPairBorderField.svg", content);

            countAreaImpairStartField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#808000\" fill-opacity=\"0.25\" />\r\n\t<path d=\"M 0.3 0.3 v 0.4 v -0.2 h 0.4 l -0.13 -0.13 l 0.13 0.13 l -0.13 0.13\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", countAreaImpairStartField);
            if (!File.Exists("countAreaImpairStartField.svg")) File.WriteAllText("countAreaImpairStartField.svg", content);

            countAreaImpairEndField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#808000\" fill-opacity=\"0.25\" />\r\n\t<path d=\"M 0.3 0.5 h 0.4 l -0.13 -0.13 l 0.13 0.13 l -0.13 0.13 l 0.13 -0.13 v 0.2 v -0.4\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", countAreaImpairEndField);
            if (!File.Exists("countAreaImpairEndField.svg")) File.WriteAllText("countAreaImpairEndField.svg", content);

            countAreaImpairBorderField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#808000\" fill-opacity=\"0.25\" />";
            content = singleGrid.Replace("<!---->", countAreaImpairBorderField);
            if (!File.Exists("countAreaImpairBorderField.svg")) File.WriteAllText("countAreaImpairBorderField.svg", content);

            svgName = "newRule.svg";

            xSize = int.Parse(XSize.Text);
            ySize = int.Parse(YSize.Text);

            Canvas.Width = xSize * 40;
            Canvas.Height = ySize * 40;

            ResetRule_Click(null, null);

            NewRuleGrid.Visibility = Visibility.Visible;
            SaveRule.Visibility = Visibility.Visible;
            ResetRule.Visibility = Visibility.Visible;
            Cancel.Visibility = Visibility.Visible;

            Canvas1.InvalidateVisual();
            Canvas2.InvalidateVisual();
            Canvas3.InvalidateVisual();
            Canvas4.InvalidateVisual();
            Canvas5.InvalidateVisual();
            Canvas6.InvalidateVisual();
            Canvas7.InvalidateVisual();
            Canvas8.InvalidateVisual();
            Canvas9.InvalidateVisual();
            Canvas10.InvalidateVisual();
            Canvas11.InvalidateVisual();
            Canvas12.InvalidateVisual();
            Canvas13.InvalidateVisual();
            Canvas14.InvalidateVisual();
        }

        private void ResetRule_Click(object sender, RoutedEventArgs e)
        {
            xSize = int.Parse(XSize.Text);
            ySize = int.Parse(YSize.Text);

            Canvas.Width = xSize * 40;
            Canvas.Height = ySize * 40;

            DrawGrid();

            newRule = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 " + xSize + " " + ySize + "\">\r\n\t<!--1-->\r\n\t<!--2-->\r\n\t<!--3-->\r\n" + grid + "</svg>";
            File.WriteAllText(svgName, newRule);
            takenCoordinates = new();
            forbiddenCoordinates = new();
            noCornerCoordinates = null;
            countAreaPairStartCoordinates = null;
            countAreaPairEndCoordinates = null;
            countAreaPairBorderCoordinates = new();
            countAreaImpairStartCoordinates = null;
            countAreaImpairEndCoordinates = null;
            countAreaImpairBorderCoordinates = new();

            NewRuleGrid.Height = 198 + ySize * 40; // the dragged element will otherwise stretch it on the bottom, no other solution have been found.
            RotateClockwise.IsChecked = false;
            RotateCounterClockwise.IsChecked = false;
            CircleDirectionLeft.IsChecked = false;
            Canvas.InvalidateVisual();
        }

        private void DrawGrid()
        {
            grid = "";

            for (int i = 0; i <= xSize; i++)
            {
                float i2 = i;
                if (i == 0)
                {
                    i2 = i + 0.01f;
                }
                else if (i == xSize)
                {
                    i2 = i - 0.01f;
                }

                grid += "\t<path fill=\"transparent\" stroke=\"gray\" stroke-width=\"0.02\" d=\"M " + i2 + " 0 v " + ySize + "\" />\r\n";
            }
            for (int i = 0; i <= ySize; i++)
            {
                float i2 = i;
                if (i == 0)
                {
                    i2 = i + 0.01f;
                }
                else if (i == ySize)
                {
                    i2 = i - 0.01f;
                }

                grid += "\t<path fill=\"transparent\" stroke=\"gray\" stroke-width=\"0.02\" d=\"M 0 " + i2 + " h " + xSize + "\" />\r\n";
            }
        }

        private void XYSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                xSize = int.Parse(XSize.Text);
                ySize = int.Parse(YSize.Text);

                Canvas.Width = xSize * 40;
                Canvas.Height = ySize * 40;

                ResizeGrid();
            }
        }

        private void ResizeGrid()
        {
            DrawGrid();

            int pos = newRule.IndexOf("viewBox=\"0 0 ");
            int lastPos = newRule.IndexOf("\"", pos + 13);
            string sizeStr = newRule.Substring(pos + 13, lastPos - pos - 13);
            newRule = newRule.Replace("viewBox=\"0 0 " + sizeStr, "viewBox=\"0 0 " + xSize + " " + ySize);

            pos = newRule.IndexOf("<!--3-->");
            newRule = newRule.Substring(0, pos + 8) + "\r\n" + grid + "</svg>";

            // remove fields that are now outside the area

            pos = 0;
            do
            {
                pos = newRule.IndexOf("<!-- ", pos);
                if (pos == -1) break;
                lastPos = newRule.IndexOf(" -->", pos);
                string areaCode = newRule.Substring(pos + 5, lastPos - pos - 5);

                string[] codes = areaCode.Split(" ");
                bool deleted = false;
                if (int.Parse(codes[0]) > xSize || int.Parse(codes[1]) > ySize)
                {
                    int nextPos = newRule.IndexOf("<!--", lastPos);
                    newRule = newRule.Substring(0, pos) + newRule.Substring(nextPos);
                    deleted = true;
                }
                if (!deleted) pos++;

            } while (true);

            File.WriteAllText(svgName, newRule);

            NewRuleGrid.Height = 198 + ySize * 40;
            Canvas.InvalidateVisual();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NewRuleGrid.Visibility = Visibility.Collapsed;
            SaveRule.Visibility = Visibility.Collapsed;
            ResetRule.Visibility = Visibility.Collapsed;
            Cancel.Visibility = Visibility.Collapsed;
        }


        // ----- Rule creation process -----


        private void RuleGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            T("Start mouse coordinates: " + e.GetPosition(RuleGrid).X + " " + e.GetPosition(RuleGrid).Y);
            T("NewRuleGrid height " + NewRuleGrid.ActualHeight);
            double x = e.GetPosition(RuleGrid).X;
            double y = e.GetPosition(RuleGrid).Y;
            startMouseX = x;
            startMouseY = y;
            if (y <= 90)
            {
                if (x >= 0 && x <= 40 && y <= 40) draggedElement = 1;
                if (x >= 50 && x <= 90 && y <= 40) draggedElement = 2;
                if (x >= 100 && x <= 140 && y <= 40) draggedElement = 3;
                if (x >= 150 && x <= 190 && y <= 40) draggedElement = 4;
                if (x >= 200 && x <= 240 && y <= 40) draggedElement = 5;
                if (x >= 250 && x <= 290 && y <= 40) draggedElement = 6;
                if (x >= 300 && x <= 340 && y <= 40) draggedElement = 7;
                if (x >= 350 && x <= 390 && y <= 40) draggedElement = 8;
                if (x >= 0 && x <= 40 && y >= 50 && y <= 90) draggedElement = 9;
                if (x >= 50 && x <= 90 && y >= 50 && y <= 90) draggedElement = 10;
                if (x >= 100 && x <= 140 && y >= 50 && y <= 90) draggedElement = 11;
                if (x >= 150 && x <= 190 && y >= 50 && y <= 90) draggedElement = 12;
                if (x >= 200 && x <= 240 && y >= 50 && y <= 90) draggedElement = 13;
                if (x >= 250 && x <= 290 && y >= 50 && y <= 90) draggedElement = 14;
            }

            draggedObj = (SKElement)RuleGrid.Children[draggedElement];
            T("draggedObj " + draggedObj);
            origMargin = draggedObj.Margin;
            Panel.SetZIndex(draggedObj, RuleGrid.Children.Count);
        }

        private void RuleGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (draggedElement == 0) return;
            double x = e.GetPosition(RuleGrid).X;
            double y = e.GetPosition(RuleGrid).Y;
            draggedObj.Margin = new Thickness(x - startMouseX + 50 * ((draggedElement - 1) % 8), y - startMouseY + 50 * ((draggedElement - 1 - (draggedElement - 1) % 8) / 8), 0, 0);
        }

        private void RuleGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (draggedElement == 0) return;
            draggedObj.Margin = origMargin;
            Panel.SetZIndex(draggedObj, draggedElement);
            draggedElement = 0;

        }

        private void RuleGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (draggedElement == 0) return;

            double x = e.GetPosition(Canvas).X;
            double y = e.GetPosition(Canvas).Y;

            draggedObj.Margin = origMargin;
            Panel.SetZIndex(draggedObj, draggedElement);
            double centerX = x - startMouseX + 20 + 50 * ((draggedElement - 1) % 8);
            double centerY = y - startMouseY + 20 + 50 * (draggedElement - 1 - (draggedElement - 1) % 8) / 8;

            if (centerX < 0 || centerY < 0)
            {
                draggedElement = 0;
                return;
            }

            T("centerX: " + centerX + " centerY: " + centerY);

            int coordX = (int)(centerX - centerX % 40) / 40 + 1;
            int coordY = (int)(centerY - centerY % 40) / 40 + 1;

            T("coordX: " + coordX + " coordY: " + coordY);

            if (coordX <= xSize && coordY <= ySize)
            {
                T("End in table: " + coordX + " " + coordY);

                // only pair or impair count area fields are allowed
                if (draggedElement >= 9 && draggedElement <= 11)
                {
                    if (!(countAreaImpairStartCoordinates is null) || !(countAreaImpairEndCoordinates is null) || countAreaImpairBorderCoordinates.Count > 0)
                    {
                        T("Count area impair fields already exist.");
                        draggedElement = 0;
                        return;
                    }
                }
                else if (draggedElement >= 12 && draggedElement <= 14)
                {
                    if (!(countAreaPairStartCoordinates is null) || !(countAreaPairEndCoordinates is null) || countAreaPairBorderCoordinates.Count > 0)
                    {
                        T("Count area pair fields already exist.");
                        draggedElement = 0;
                        return;
                    }
                }

                if (draggedElement == 7) // no corner
                {
                    if (!(noCornerCoordinates is null))
                    {
                        T("Corner already exists.");
                        draggedElement = 0;
                        return;
                    }

                    if (coordX > 2 && coordX < xSize - 1 && coordY > 2)
                    {
                        T("Corner misplaced.");
                        draggedElement = 0;
                        return;
                    }

                    foreach (int[] coord in forbiddenCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY)
                        {
                            T("Corner cannot be forbidden.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    noCornerCoordinates = new int[] { coordX, coordY };
                }
                else if (draggedElement == 8) // forbidden
                {
                    if (!(noCornerCoordinates is null) && coordX == noCornerCoordinates[0] && coordY == noCornerCoordinates[1])
                    {
                        T("Corner cannot be forbidden.");
                        draggedElement = 0;
                        return;
                    }

                    if (forbiddenCoordinates.Count == 2)
                    {
                        T("Too many forbidden.");
                        draggedElement = 0;
                        return;
                    }

                    foreach (int[] coord in forbiddenCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY)
                        {
                            T("Found forbidden.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    if (forbiddenCoordinates.Count == 1 && !(Math.Abs(coordX - forbiddenCoordinates[0][0]) == 2 && coordY == forbiddenCoordinates[0][1] || Math.Abs(coordX - forbiddenCoordinates[0][0]) == 1 && Math.Abs(coordY - forbiddenCoordinates[0][1]) == 1))
                    {
                        T("Existing forbidden dislocated.");
                        draggedElement = 0;
                        return;
                    }

                    foreach (int[] coord in takenCoordinates)
                    {
                        if (coord[2] == 1 && !(Math.Abs(coordX - coord[0]) == 1 && coordY == coord[1] || coordY - coord[1] == -1 && coordX == coord[0]))
                        {
                            T("Live end dislocated.");
                            draggedElement = 0;
                            return;
                        }
                        else if (coordX == coord[0] && coordY == coord[1] && coord[2] != 2 && coord[2] != 5)
                        {
                            T("Only empty, undetermined or future start field should be marked forbidden.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    forbiddenCoordinates.Add(new int[] { coordX, coordY });
                }
                else if (draggedElement == 9) // count area pair start
                {
                    if (!(countAreaPairStartCoordinates is null))
                    {
                        T("Count area pair start already exists.");
                        draggedElement = 0;
                        return;
                    }

                    foreach (int[] coord in takenCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY && coord[2] != 2)
                        {
                            T("Count area pair start can only be placed on an empty field");
                            draggedElement = 0;
                            return;
                        }
                    }

                    foreach (int[] coord in countAreaPairBorderCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY && coord[2] != 2)
                        {
                            T("Count area pair start cannot be place on a count area border field.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    countAreaPairStartCoordinates = new int[] { coordX, coordY };
                    SaveMeta();
                }
                else if (draggedElement == 10) // count area pair end
                {
                    if (!(countAreaPairEndCoordinates is null))
                    {
                        T("Count area pair end already exists.");
                        draggedElement = 0;
                        return;
                    }

                    foreach (int[] coord in takenCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY && coord[2] != 2)
                        {
                            T("Count area pair end can only be placed on an empty field.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    foreach (int[] coord in countAreaPairBorderCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY && coord[2] != 2)
                        {
                            T("Count area pair end cannot be place on a count area border field.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    countAreaPairEndCoordinates = new int[] { coordX, coordY };
                    SaveMeta();
                }
                else if (draggedElement == 11) // count area pair border
                {
                    if (!(countAreaPairStartCoordinates is null))
                    {
                        if (countAreaPairStartCoordinates[0] == coordX && countAreaPairStartCoordinates[1] == coordY)
                        {
                            T("Count area pair border cannot be placed on count area start field.");
                            draggedElement = 0;
                            return;
                        }
                    }
                    if (!(countAreaPairEndCoordinates is null))
                    {
                        if (countAreaPairEndCoordinates[0] == coordX && countAreaPairEndCoordinates[1] == coordY)
                        {
                            T("Count area pair border cannot be placed on count area end field.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    foreach (int[] coord in takenCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY && coord[2] != 2)
                        {
                            T("Count area pair border can only be placed on an empty field");
                            draggedElement = 0;
                            return;
                        }
                    }

                    countAreaPairBorderCoordinates.Add(new int[] { coordX, coordY });
                }
                else if (draggedElement == 12) // count area impair start
                {
                    if (!(countAreaImpairStartCoordinates is null))
                    {
                        T("Count area impair start already exists.");
                        draggedElement = 0;
                        return;
                    }

                    foreach (int[] coord in takenCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY && coord[2] != 2)
                        {
                            T("Count area impair start can only be placed on an empty field");
                            draggedElement = 0;
                            return;
                        }
                    }

                    foreach (int[] coord in countAreaImpairBorderCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY && coord[2] != 2)
                        {
                            T("Count area impair start cannot be place on a count area border field.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    countAreaImpairStartCoordinates = new int[] { coordX, coordY };
                    SaveMeta();
                }
                else if (draggedElement == 13) // count area impair end
                {
                    if (!(countAreaImpairEndCoordinates is null))
                    {
                        T("Count area impair end already exists.");
                        draggedElement = 0;
                        return;
                    }

                    foreach (int[] coord in takenCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY && coord[2] != 2)
                        {
                            T("Count area impair end can only be placed on an empty field.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    foreach (int[] coord in countAreaImpairBorderCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY && coord[2] != 2)
                        {
                            T("Count area impair end cannot be place on a count area border field.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    countAreaImpairEndCoordinates = new int[] { coordX, coordY };
                    SaveMeta();
                }
                else if (draggedElement == 14) // count area impair border
                {
                    if (!(countAreaImpairStartCoordinates is null))
                    {
                        if (countAreaImpairStartCoordinates[0] == coordX && countAreaImpairStartCoordinates[1] == coordY)
                        {
                            T("Count area impair border cannot be placed on count area start field.");
                            draggedElement = 0;
                            return;
                        }
                    }
                    if (!(countAreaImpairEndCoordinates is null))
                    {
                        if (countAreaImpairEndCoordinates[0] == coordX && countAreaImpairEndCoordinates[1] == coordY)
                        {
                            T("Count area impair border cannot be placed on count area end field.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    foreach (int[] coord in takenCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY && coord[2] != 2)
                        {
                            T("Count area impair border can only be placed on an empty field");
                            draggedElement = 0;
                            return;
                        }
                    }

                    countAreaImpairBorderCoordinates.Add(new int[] { coordX, coordY });
                }
                else
                {
                    if (draggedElement == 1)
                    {
                        if (forbiddenCoordinates.Count == 1)
                        {
                            if (!(Math.Abs(coordX - forbiddenCoordinates[0][0]) == 1 && coordY == forbiddenCoordinates[0][1] || coordY - forbiddenCoordinates[0][1] == 1 && coordX == forbiddenCoordinates[0][0]))
                            {
                                T("Forbidden field dislocated.");
                                draggedElement = 0;
                                return;
                            }
                        }
                        else if (forbiddenCoordinates.Count == 2)
                        {
                            if (forbiddenCoordinates[0][1] == forbiddenCoordinates[1][1] && !(coordX == (forbiddenCoordinates[0][0] + forbiddenCoordinates[1][0]) / 2 && coordY == forbiddenCoordinates[0][1]))
                            {
                                T("Two forbidden fields in a row dislocated.");
                                draggedElement = 0;
                                return;
                            }
                            else if (forbiddenCoordinates[0][1] > forbiddenCoordinates[1][1] && !(coordX == forbiddenCoordinates[1][0] && coordY == forbiddenCoordinates[0][1]) || forbiddenCoordinates[1][1] > forbiddenCoordinates[0][1] && !(coordX == forbiddenCoordinates[0][0] && coordY == forbiddenCoordinates[1][1]))
                            {
                                T("Two forbidden fields across dislocated.");
                                draggedElement = 0;
                                return;
                            }
                        }
                    }
                    else if (draggedElement != 2)
                    {
                        foreach (int[] coord in forbiddenCoordinates)
                        {
                            if (coordX == coord[0] && coordY == coord[1])
                            {
                                T("Only empty or undetermined should be marked forbidden.");
                                draggedElement = 0;
                                return;
                            }
                        }
                    }

                    if (draggedElement != 2 && (countAreaPairStartCoordinates != null && coordX == countAreaPairStartCoordinates[0] && coordY == countAreaPairStartCoordinates[1] || countAreaPairEndCoordinates != null && coordX == countAreaPairEndCoordinates[0] && coordY == countAreaPairEndCoordinates[1]))
                    {
                        T("Only empty field can be count area pair start or end.");
                        draggedElement = 0;
                        return;
                    }

                    if (draggedElement != 2 && (countAreaImpairStartCoordinates != null && coordX == countAreaImpairStartCoordinates[0] && coordY == countAreaImpairStartCoordinates[1] || countAreaImpairEndCoordinates != null && coordX == countAreaImpairEndCoordinates[0] && coordY == countAreaImpairEndCoordinates[1]))
                    {
                        T("Only empty field can be count area impair start or end.");
                        draggedElement = 0;
                        return;
                    }

                    foreach (int[] coord in takenCoordinates)
                    {
                        if (draggedElement == 1 && coord[2] == 1)
                        {
                            T("Existing live end");
                            draggedElement = 0;
                            return;
                        }
                        else if (draggedElement == 5 && coord[2] == 5)
                        {
                            T("Existing future start");
                            M("Only one future start field can be added.");
                            draggedElement = 0;
                            return;
                        }
                        else if (draggedElement == 6 && coord[2] == 6)
                        {
                            T("Existing future end");
                            M("Only one future end field can be added.");
                            draggedElement = 0;
                            return;
                        }

                        if (draggedElement == 1 && coord[0] == coordX && coord[1] == coordY + 1)
                        {
                            T("Live end bottom taken.");
                            draggedElement = 0;
                            return;
                        }
                        else if (coord[0] == coordX && coord[1] == coordY)
                        {
                            T("Field taken.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    takenCoordinates.Add(new int[] { coordX, coordY, draggedElement });
                }

                string addField = "";

                switch (draggedElement)
                {
                    case 1:
                        addField = liveEnd.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.5 1", "M " + (coordX - 0.5f) + " " + coordY) + 
                            liveEnd.Replace("M 0 0", "M " + (coordX - 1) + " " + coordY).Replace("M 0.5 1 v -0.5", "M " + (coordX - 0.5f) + " " + coordY + " v 0.5");
                        takenCoordinates.Add(new int[] { coordX, coordY + 1, 0 });
                        break;
                    case 2:
                        addField = emptyField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1));
                        break;
                    case 3:
                        addField = takenField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.5", "M " + (coordX - 0.7f) + " " + (coordY - 0.5f)).Replace("M 0.5 0.3", "M " + (coordX - 0.5f) + " " + (coordY - 0.7f));
                        break;
                    case 4:
                        addField = takenOrBorderField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1));
                        break;
                    case 5:
                        addField = futureStartField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.2 0.2", "M " + (coordX - 1 + 0.2f) + " " + (coordY - 1 + 0.2f));
                        break;
                    case 6:
                        addField = futureEndField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.2 0.5", "M " + (coordX - 1 + 0.2f) + " " + (coordY - 1 + 0.5f));
                        break;
                    case 7:
                        addField = notCornerField.Replace("M 0.3 0.2", "M " + (coordX - 0.7f) + " " + (coordY - 0.8f)).Replace("0.42", (coordX - 0.58f).ToString()).Replace("0.58", (coordY - 0.42f).ToString());
                        break;
                    case 8:
                        addField = forbiddenField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.2 0.2", "M " + (coordX - 1 + 0.2f) + " " + (coordY - 1 + 0.2f)).Replace("M 0.2 0.8", "M " + (coordX - 1 + 0.2f) + " " + (coordY - 1 + 0.8f));
                        break;
                    case 9:
                        addField = countAreaPairStartField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.3", "M " + (coordX - 1 + 0.3f) + " " + (coordY - 1 + 0.3f));
                        break;
                    case 10:
                        addField = countAreaPairEndField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.5", "M " + (coordX - 1 + 0.3f) + " " + (coordY - 1 + 0.5f));
                        break;
                    case 11:
                        addField = countAreaPairBorderField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1));
                        break;
                    case 12:
                        addField = countAreaImpairStartField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.3", "M " + (coordX - 1 + 0.3f) + " " + (coordY - 1 + 0.3f));
                        break;
                    case 13:
                        addField = countAreaImpairEndField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.5", "M " + (coordX - 1 + 0.3f) + " " + (coordY - 1 + 0.5f));
                        break;
                    case 14:
                        addField = countAreaImpairBorderField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1));
                        break;
                }

                if (draggedElement == 7 || draggedElement == 8)
                {
                    newRule = newRule.Replace("<!--3-->", "<!-- " + coordX + " " + coordY + " " + draggedElement + " -->\r\n\t" + addField + "\r\n\t<!--3-->");
                }
                else if (draggedElement >= 9)
                {
                    newRule = newRule.Replace("<!--2-->", "<!-- " + coordX + " " + coordY + " " + draggedElement + " -->\r\n\t" + addField + "\r\n\t<!--2-->");
                }
                else
                {
                    newRule = newRule.Replace("<!--1-->", "<!-- " + coordX + " " + coordY + " " + draggedElement + " -->\r\n\t" + addField + "\r\n\t<!--1-->");
                }

                File.WriteAllText(svgName, newRule);

                Canvas.InvalidateVisual();
            }

            draggedElement = 0;
        }

        private void SaveRule_Click(object sender, RoutedEventArgs e)
        {
            size = int.Parse(AppliedSize.Text);

            if (countAreaPairStartCoordinates != null && countAreaPairEndCoordinates == null)
            {
                M("Count area pair end field has to be added.");
                return;
            }
            else if (countAreaPairEndCoordinates != null && countAreaPairStartCoordinates == null)
            {
                M("Count area pair start field has to be added.");
                return;
            }
            if (countAreaImpairStartCoordinates != null && countAreaImpairEndCoordinates == null)
            {
                M("Count area impair end field has to be added.");
                return;
            }
            else if (countAreaImpairEndCoordinates != null && countAreaImpairStartCoordinates == null)
            {
                M("Count area impair start field has to be added.");
                return;
            }

            // there has to be a taken or border field next to or across the count area end field 
            if (countAreaPairStartCoordinates != null && countAreaPairEndCoordinates != null || countAreaImpairStartCoordinates != null && countAreaImpairEndCoordinates != null)
            {
                int distX, distY;

                int[] countAreaStartCoordinates = (countAreaImpairStartCoordinates == null) ? countAreaPairStartCoordinates : countAreaImpairStartCoordinates;
                int[] countAreaEndCoordinates = (countAreaImpairStartCoordinates == null) ? countAreaPairEndCoordinates : countAreaImpairEndCoordinates;
                List<int[]> countAreaBorderCoordinates = (countAreaImpairStartCoordinates == null) ? countAreaPairBorderCoordinates : countAreaImpairBorderCoordinates;

                bool found = false;
                if (countAreaBorderCoordinates.Count > 0)
                {
                    foreach (int[] field in countAreaBorderCoordinates)
                    {
                        distX = field[0] - countAreaEndCoordinates[0];
                        distY = field[1] - countAreaEndCoordinates[1];

                        if (Math.Abs(distX) + Math.Abs(distY) == 1)
                        {
                            if (CheckTakenOrBorderNearEnd(countAreaEndCoordinates[0], countAreaEndCoordinates[1], distX, distY))
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                }
                else if (Math.Abs(countAreaStartCoordinates[0] - countAreaEndCoordinates[0]) == 2 && countAreaStartCoordinates[1]== countAreaEndCoordinates[1] || Math.Abs(countAreaStartCoordinates[1] - countAreaEndCoordinates[1]) == 2 && countAreaStartCoordinates[0] == countAreaEndCoordinates[0])
                {
                    distX = (countAreaStartCoordinates[0] - countAreaEndCoordinates[0]) / 2;
                    distY = (countAreaStartCoordinates[1] - countAreaEndCoordinates[1]) / 2;

                    if (CheckTakenOrBorderNearEnd(countAreaEndCoordinates[0], countAreaEndCoordinates[1], distX, distY))
                    {
                        found = true;
                    }
                }
                else if (Math.Abs(countAreaStartCoordinates[0] - countAreaEndCoordinates[0]) == 1 && countAreaStartCoordinates[1] == countAreaEndCoordinates[1] || Math.Abs(countAreaStartCoordinates[1] - countAreaEndCoordinates[1]) == 1 && countAreaStartCoordinates[0] == countAreaEndCoordinates[0])
                {
                    distX = countAreaStartCoordinates[0] - countAreaEndCoordinates[0];
                    distY = countAreaStartCoordinates[1] - countAreaEndCoordinates[1];

                    if (CheckTakenOrBorderNearEnd(countAreaEndCoordinates[0], countAreaEndCoordinates[1], distX, distY))
                    {
                        found = true;
                    }
                }
                else
                {
                    M("There has to be count area border fields between the start end end fields.");
                    return;
                }

                if (!found)
                {
                    M("There has to be a taken or border field next to the count area end.");
                    return;
                }
            }

            if (xSize > size || ySize > size)
            {
                M("The rule is larger than the applied size.");
                return;
            }
            if (takenCoordinates.Count == 0)
            {
                M("Empty rule not saved.");
                return;
            }
            if (RuleName.Text.Trim() == "")
            {
                M("Enter filename.");
                return;
            }
            if (size % 2 == 0 || size < 5 || size > 21)
            {
                M("Applied size must be impair and between 5 and 21.");
                return;
            }
            if (newRule.IndexOf("1 -->") == -1)
            {
                M("No live end added.");
                return;
            }
            if (newRule.IndexOf("8 -->") == -1)
            {
                M("No forbidden fields added.");
                return;
            }
            if (newRule.IndexOf("2 -->") == -1 && newRule.IndexOf("3 -->") == -1 && newRule.IndexOf("4 -->") == -1 && newRule.IndexOf("5 -->") == -1 && newRule.IndexOf("6 -->") == -1)
            {
                M("No conditions are added.");
                return;
            }
            if (!Directory.Exists("rules/" + size))
            {
                Directory.CreateDirectory("rules/" + size);
            }

            string fileName = RuleName.Text + ".svg";
            if (RuleName.Text.Length > 4)
            {
                fileName = RuleName.Text.Substring(RuleName.Text.Length - 4) == ".svg" ? RuleName.Text : RuleName.Text + ".svg";
            }

            string fullName = "rules/" + size + "/" + fileName;
            if (File.Exists(fullName))
            {
                int i = 1;
                while (File.Exists(fullName.Replace(".svg", "_" + i + ".svg")))
                {
                    i++;
                }
                fullName = fullName.Replace(".svg", "_" + i + ".svg");
            }

            T("Save: " + fullName);
            File.Copy("newRule.svg", fullName);
            ResetRule_Click(null, null);
            LoadDir();
        }

        private bool CheckTakenOrBorderNearEnd(int countAreaEndX, int countAreaEndY, int distX, int distY)
        {
            int middleWallX, middleWallY, leftAcrossWallX, leftAcrossWallY, rightAcrossWallX, rightAcrossWallY;

            middleWallX = countAreaEndX - distX;
            middleWallY = countAreaEndY - distY;

            int dir = FindDirection(distX, distY);
            int leftDir = (dir == 0) ? 3 : dir - 1;
            int rightDir = (dir == 3) ? 0 : dir + 1;

            leftAcrossWallX = middleWallX + directions[leftDir][0];
            leftAcrossWallY = middleWallY + directions[leftDir][1];
            rightAcrossWallX = middleWallX + directions[rightDir][0];
            rightAcrossWallY = middleWallY + directions[rightDir][1];

            foreach (int[] takenField in takenCoordinates)
            {
                if (takenField[2] == 3 || takenField[2] == 4 &&
                (takenField[0] == middleWallX && takenField[1] == middleWallY || takenField[0] == leftAcrossWallX && takenField[1] == leftAcrossWallY || takenField[0] == rightAcrossWallX && takenField[1] == rightAcrossWallY))
                {
                    return true;
                }
            }

            return false;
        }


        // ----- Generate code -----


        private void LoadDir()
        {
            MainGrid.Children.RemoveRange(0, childIndex);
            childIndex = 0;
            string[] listOfSizes = Directory.GetDirectories("rules");            
            int yTotalPos = 0;
            string codeString = "namespace OneWayLabyrinth\n{\n\tusing System.Collections.Generic;\n\n\tpublic partial class Path\n\t{\n[conditionVariablesDeclare]\n\t\tpublic void RunRules()\n\t\t{\n[conditionVariablesReset]\n";
            string conditionVariablesDeclare = "";
            string conditionVariablesReset = "";
            string conditionVariablesSet = "T(";
            activeRules = new() { "(no rule)" };            

            foreach (string size in listOfSizes)
            {
                string sizeNumber = size.Replace("rules\\", "");
                TextBlock t = new();
                t.Text = sizeNumber + " x " + sizeNumber;
                t.FontSize = 14;
                t.FontWeight = FontWeights.Bold;
                t.Margin = new Thickness(10, 10 + childIndex / 2 * 35 + yTotalPos, 10, 10);
                t.HorizontalAlignment = HorizontalAlignment.Left;
                t.VerticalAlignment = VerticalAlignment.Top;
                MainGrid.Children.Insert(childIndex, t);
                Grid.SetRow(t, 1);

                Grid g = new();
                g.Margin = new Thickness(10, 35 + childIndex / 2 * 35 + yTotalPos, 10, 10);
                g.HorizontalAlignment = HorizontalAlignment.Left;
                g.VerticalAlignment = VerticalAlignment.Top;
                MainGrid.Children.Insert(++childIndex, g);
                Grid.SetRow(g, 1);

                if (int.Parse(sizeNumber) < 9)
                {
                    codeString += "\t\t\tif (size == " + sizeNumber + ")\n\t\t\t{\n";
                }
                else
                {
                    codeString += "\t\t\tif (size >= " + sizeNumber + ")\n\t\t\t{\n";
                }

                string[] listOfRules = Directory.GetFiles(size);

                int yPos = 0;
                foreach (string rule in listOfRules)
                {
                    if (rule.Substring(rule.Length - 3) == ".cs") continue;
                    string content = File.ReadAllText(rule);
                    int pos = content.IndexOf("viewBox=\"0 0 ");
                    int lastPos = content.IndexOf("\"", pos + 13);
                    string sizeStr = content.Substring(pos + 13, lastPos - pos - 13);
                    T("sizeStr " + sizeStr);
                    string[] sizes = sizeStr.Split(" ");

                    string ruleName = rule.Replace(size + "\\", "").Replace(".svg","");
                    string variableName = ruleName.Replace(" ", "").Replace("-", "");
                    if (variableName != "CShape")
                    {
                        conditionVariablesDeclare += "\t\tpublic bool " + variableName + " = false;\n";
                        conditionVariablesReset += "\t\t\t" + variableName + " = false;\n";
                        conditionVariablesSet += "\"" + variableName + ": \" + " + variableName + " + \"\\n\" + ";
                    }
                    codeString += "\t\t\t\t// " + ruleName + "\n";
                    activeRules.Add(ruleName);

                    pos = 0;
                    int section1Pos = content.IndexOf("<!--1-->");
                    int section2Pos = content.IndexOf("<!--2-->");
                    List<int[]> takenFields = new();
                    List<int[]> forbiddenFields = new();
                    int[]? noCornerField = null;
                    int[]? countAreaPairStartField = null;
                    int[]? countAreaPairEndField = null;
                    List<int[]> countAreaPairBorderFields = new();
                    int[]? countAreaImpairStartField = null;
                    int[]? countAreaImpairEndField = null;
                    List<int[]> countAreaImpairBorderFields = new();

                    while (true)
                    {
                        pos = content.IndexOf("<!-- ", pos);
                        if (pos == -1) break;
                        lastPos = content.IndexOf(" -->", pos);
                        string codeLine = content.Substring(pos + 5, lastPos - pos - 5);
                        string[] arr = codeLine.Split(" ");
                        int fieldX = int.Parse(arr[0]);
                        int fieldY = int.Parse(arr[1]);
                        int fieldCode = int.Parse(arr[2]);
                        if (pos < section1Pos)
                        {
                            takenFields.Add(new int[] { fieldX, fieldY, fieldCode });
                        }
                        else if (pos < section2Pos)
                        {
                            if (fieldCode == 9)
                            {
                                countAreaPairStartField = new int[] { fieldX, fieldY };
                            }
                            else if (fieldCode == 10)
                            {
                                countAreaPairEndField = new int[] { fieldX, fieldY };
                            }
                            else if (fieldCode == 11)
                            {
                                countAreaPairBorderFields.Add(new int[] { fieldX, fieldY });
                            }
                            if (fieldCode == 12)
                            {
                                countAreaImpairStartField = new int[] { fieldX, fieldY };
                            }
                            else if (fieldCode == 13)
                            {
                                countAreaImpairEndField = new int[] { fieldX, fieldY };
                            }
                            else if (fieldCode == 14)
                            {
                                countAreaImpairBorderFields.Add(new int[] { fieldX, fieldY });
                            }
                        }
                        else
                        {
                            if (fieldCode == 8)
                            {
                                forbiddenFields.Add(new int[] { fieldX, fieldY });
                            }
                            else
                            {
                                noCornerField = new int[] { fieldX, fieldY };
                            }
                        }
                        pos = lastPos;
                    }

                    t = new();
                    t.Text = ruleName;
                    t.Margin = new Thickness(0, yPos, 0, 0);
                    t.HorizontalAlignment = HorizontalAlignment.Left;
                    t.VerticalAlignment = VerticalAlignment.Top;

                    pos = content.IndexOf("<svg");
                    string[] metaArr = new string[] { "", "", "" };
                    if (pos != 0)
                    {
                        string meta = content.Substring(0, pos).Trim();
                        meta = meta.Replace("<!--|", "").Replace("|-->", "");
                        metaArr = meta.Split("|");

                        if (metaArr[0] == "True")
                        {
                            t.Text += " | Rotate clockwise";
                        }
                        if (metaArr[1] == "True")
                        {
                            t.Text += " | Rotate counter-clockwise";
                        }
                        if (countAreaPairStartField != null && countAreaPairEndField != null || countAreaImpairStartField != null && countAreaImpairEndField != null)
                        {
                            if (metaArr[2] == "True")
                            {
                                t.Text += " | Circle direction left";
                            }
                            else
                            {
                                t.Text += " | Circle direction right";
                            }
                        }
                    }

                    g.Children.Add(t);

                    yPos += 21;

                    SKElement c = new();
                    c.IgnorePixelScaling = true;
                    c.Tag = rule.Replace(".svg", "");
                    c.PaintSurface += SKElement_PaintSurface;
                    c.Width = int.Parse(sizes[0]) * 40;
                    c.Height = int.Parse(sizes[1]) * 40;
                    c.Margin = new Thickness(0, yPos, 0, 0);
                    c.HorizontalAlignment = HorizontalAlignment.Left;
                    c.VerticalAlignment = VerticalAlignment.Top;
                    g.Children.Add(c);

                    yPos += int.Parse(sizes[1]) * 40 + 10;

                    
                    codeString += GenerateCode(variableName, metaArr, takenFields, forbiddenFields, noCornerField, countAreaPairStartField, countAreaPairEndField, countAreaPairBorderFields, countAreaImpairStartField, countAreaImpairEndField, countAreaImpairBorderFields);
                }
                codeString = codeString.Substring(0, codeString.Length - 1);
                codeString += "\t\t\t}\n\n";
                yTotalPos += yPos - 10;
                childIndex++;
            }
            RotateClockwise.IsChecked = false;
            RotateCounterClockwise.IsChecked = false;
            CircleDirectionLeft.IsChecked = false;
            codeString = codeString.Substring(0, codeString.Length - 1);
            codeString += "\t\t\t" + conditionVariablesSet.Substring(0, conditionVariablesSet.Length - 10) + ");\n\t\t}\n\t}\n}";
            codeString = codeString.Replace("[conditionVariablesDeclare]", conditionVariablesDeclare);
            codeString = codeString.Replace("[conditionVariablesReset]", conditionVariablesReset);
            File.WriteAllText("PathRules.cs", codeString);
        }

        private string GenerateCode(string variableName, string[] meta, List<int[]> takenFields, List<int[]> forbiddenFields, int[]? noCornerField, int[]? countAreaPairStartField, int[]? countAreaPairEndField, List<int[]> countAreaPairBorderFields, int[]? countAreaImpairStartField, int[]? countAreaImpairEndField, List<int[]> countAreaImpairBorderFields)
        {
            if (variableName == "CShape") return "\t\t\t\t// Embedded in Path.cs as the absolute checking functions need it.\n\n";

            //currently, one count area field is allowed
            int[]? countAreaStartField = (countAreaImpairStartField == null) ? countAreaPairStartField : countAreaImpairStartField;
            int[]? countAreaEndField = (countAreaImpairEndField == null) ? countAreaPairEndField : countAreaImpairEndField;
            List<int[]> countAreaBorderFields = (countAreaImpairBorderFields.Count == 0) ? countAreaPairBorderFields : countAreaImpairBorderFields;
            bool areaPair = (countAreaImpairStartField == null) ? true : false;

            int startX = 0;
            int startY = 0;

            foreach (int[] field in takenFields)
            {
                if (field[2] == 1)
                {
                    startX = field[0];
                    startY = field[1];
                }
            }

            string conditionStr = "";

            List<int[]> takenOrBorderFields = new();

            foreach (int[] field in takenFields)
            {
                int relX = startX - field[0];
                int relY = startY - field[1];
                switch (field[2])
                {
                    case 2:
                        conditionStr += "!InTakenRel(" + relX + "," + relY + ") && !InBorderRel(" + relX + "," + relY + ") && ";
                        break;
                    case 3:
                        conditionStr += "InTakenRel(" + relX + "," + relY + ") && ";
                        takenOrBorderFields.Add(new int[] { field[0], field[1] });
                        break;
                    case 4:
                        conditionStr += "(InTakenRel(" + relX + "," + relY + ") || InBorderRel(" + relX + "," + relY + ")) && ";
                        takenOrBorderFields.Add(new int[] { field[0], field[1] });
                        break;
                    case 5:
                        conditionStr += "InFutureStartRel(" + relX + "," + relY + ") && ";
                        break;
                    case 6:
                        conditionStr += "InFutureEndRel(" + relX + "," + relY + ") && ";
                        break;
                }
            }

            if (!(noCornerField is null))
            {
                int relX = startX - noCornerField[0];
                int relY = startY - noCornerField[1];
                conditionStr += "!InCornerRel(" + relX + "," + relY + ") && ";
            }

            conditionStr = conditionStr.Substring(0, conditionStr.Length - 4);

            // only works if only one future start and end fields are added
            if (conditionStr.IndexOf("InFutureStart") != -1 && conditionStr.IndexOf("InFutureEnd") != -1)
            {
                conditionStr += " && foundSectionStart == foundSectionEnd";
            }

            string forbiddenStr = "";

            foreach (int[] field in forbiddenFields)
            {
                int relX = startX - field[0];
                int relY = startY - field[1];
                if (relX == 0)
                {
                    forbiddenStr += "forbidden.Add(new int[] { x + sx, y + sy });\n";
                }
                else if (relX == 1)
                {
                    forbiddenStr += "forbidden.Add(new int[] { x + lx, y + ly });\n";
                }
                else
                {
                    forbiddenStr += "forbidden.Add(new int[] { x - lx, y - ly });\n";
                }
            }
            forbiddenStr = forbiddenStr.Substring(0, forbiddenStr.Length - 1);

            string codeStr, ruleCore;

            if (countAreaStartField != null && countAreaEndField != null)
            {
                bool circleDirectionLeft = bool.Parse(meta[2]);

                // We need to make sure that the wall ahead is going into the direction according the circle direction (it has to go in the same direction). To do this, we find the taken or border field next to the count area end field.

                int[] middleWall = new int[] { 0, 0 };
                bool straightFound = false;
                bool acrossFound = false;
                foreach (int[] field in takenOrBorderFields)
                {
                    // field next to the count area end field (prioritized)
                    if (field[0] == countAreaEndField[0] && Math.Abs(field[1] - countAreaEndField[1]) == 1 || field[1] == countAreaEndField[1] && Math.Abs(field[0] - countAreaEndField[0]) == 1)
                    {
                        straightFound = true;
                        middleWall = field;
                        break;
                    }
                    // field across the count area end field
                    else if (Math.Abs(field[0] - countAreaEndField[0]) == 1 && Math.Abs(field[1] - countAreaEndField[1]) == 1)
                    {
                        acrossFound = true;
                        middleWall = field;
                    }
                }

                // determine distance between count area start end end fields; if it is more than 2, draw border line. Find direction between them or between the end field and the nearest border line field.

                bool diff1 = false;
                bool diff2 = false;
                int diffX = 0, diffY = 0;
                string countAreaBorderFieldsStr = "List<int[]> countAreaBorderFields = new List<int[]> { ";

                if (Math.Abs(countAreaStartField[0] - countAreaEndField[0]) == 1 && countAreaStartField[1] == countAreaEndField[1] || Math.Abs(countAreaStartField[1] - countAreaEndField[1]) == 1 && countAreaStartField[0] == countAreaEndField[0]) // fields 1 distance apart, we don't count area, we only need to check the direction of the middle field
                {
                    diff1 = true;
                    diffX = countAreaStartField[0] - countAreaEndField[0];
                    diffY = countAreaStartField[1] - countAreaEndField[1];
                }
                else if (Math.Abs(countAreaStartField[0] - countAreaEndField[0]) == 2 && countAreaStartField[1] == countAreaEndField[1] || Math.Abs(countAreaStartField[1] - countAreaEndField[1]) == 2 && countAreaStartField[0] == countAreaEndField[0])
                {
                    diff2 = true;
                    diffX = (countAreaStartField[0] - countAreaEndField[0]) / 2;
                    diffY = (countAreaStartField[1] - countAreaEndField[1]) / 2;
                }
                else // count area border present
                {
                    //sort fields from next to the end to next to the start
                    int[][] newCountAreaBorderFields = new int[countAreaBorderFields.Count][];
                    foreach (int[] field in countAreaBorderFields)
                    {
                        int index = Math.Abs(field[0] - countAreaEndField[0]) + Math.Abs(field[1] - countAreaEndField[1]) - 1;
                        newCountAreaBorderFields[index] = new int[] { field[0], field[1] };

                        if (index == 0)
                        {
                            diffX = field[0] - countAreaEndField[0];
                            diffY = field[1] - countAreaEndField[1];
                        }
                    }

                    foreach (int[] field in newCountAreaBorderFields)
                    {
                        int relX = startX - field[0];
                        int relY = startY - field[1];
                        countAreaBorderFieldsStr += "new int[] {" + relX + "," + relY + "}, ";
                    }
                    countAreaBorderFieldsStr = countAreaBorderFieldsStr.Substring(0, countAreaBorderFieldsStr.Length - 2) + "};\n";
                }

                // find perpendicular left/right directions

                int directionIndex = FindDirection(diffX, diffY);
                int leftDirection = (directionIndex == 0) ? 3 : directionIndex - 1;
                int rightDirection = (directionIndex == 3) ? 0 : directionIndex + 1;

                int[] leftSideWall = new int[] { middleWall[0] + directions[leftDirection][0], middleWall[1] + directions[leftDirection][1] };
                int[] rightSideWall = new int[] { middleWall[0] + directions[rightDirection][0], middleWall[1] + directions[rightDirection][1] };

                int middleWallRelX = startX - middleWall[0];
                int middleWallRelY = startY - middleWall[1];
                int leftSideWallRelX = startX - leftSideWall[0];
                int leftSideWallRelY = startY - leftSideWall[1];
                int rightSideWallRelX = startX - rightSideWall[0];
                int rightSideWallRelY = startY - rightSideWall[1];
                int startRelX = startX - countAreaStartField[0];
                int startRelY = startY - countAreaStartField[1];
                int endRelX = startX - countAreaEndField[0];
                int endRelY = startY - countAreaEndField[1];

                string countAreaRule;

                if (diff1)
                {
                    forbiddenStr = "\t\t" + forbiddenStr.Replace("\n", "\n\t\t") + "\n";

                    countAreaRule = "\t\t" + variableName + " = true;\n" +
                    forbiddenStr;
                }
                else if (diff2)
                {
                    forbiddenStr = "\t\t\t" + forbiddenStr.Replace("\n", "\n\t\t\t") + "\n";

                    countAreaRule = "\t\tcircleDirectionLeft = (i == 0) ? " + circleDirectionLeft.ToString().ToLower() + " : " + (!circleDirectionLeft).ToString().ToLower() + ";\n" +
                    "\t\tif (" + (areaPair ? "" : "!") + "CountAreaRel(" + startRelX + ", " + startRelY + ", " + endRelX + ", " + endRelY + "))\n" +
                    "\t\t{\n" +
                    "\t\t\t" + variableName + " = true;\n" +
                    forbiddenStr +
                    "\t\t}\n";
                }
                else
                {
                    forbiddenStr = "\t\t\t" + forbiddenStr.Replace("\n", "\n\t\t\t") + "\n";

                    countAreaRule = "\t\tcircleDirectionLeft = (i == 0) ? " + circleDirectionLeft.ToString().ToLower() + " : " + (!circleDirectionLeft).ToString().ToLower() + ";\n" +
                    "\t\t" + countAreaBorderFieldsStr +
                    "\t\tif (" + (areaPair ? "" : "!") + "CountAreaRel(" + startRelX + ", " + startRelY + ", " + endRelX + ", " + endRelY + ", 0, 0, countAreaBorderFields))\n" +
                    "\t\t{\n" +
                    "\t\t\t" + variableName + " = true;\n" +
                    forbiddenStr +
                    "\t\t}\n";
                }

                ruleCore = "int middleIndex = InTakenIndexRel(" + middleWallRelX + "," + middleWallRelY + ");\n" +
                    "if (middleIndex != -1)\n" +
                    "{\n" +
                    "\tif (InTakenRel(" + leftSideWallRelX + "," + leftSideWallRelY + "))\n" +
                    "\t{\n" +
                    "\t\tint sideIndex = InTakenIndexRel(" + leftSideWallRelX + "," + leftSideWallRelY + ");\n" +
                    "\t\tif (sideIndex [1] middleIndex)\n" +
                    "\t\t{\n" +
                    countAreaRule.Replace("\t\t", "\t\t\t") +
                    "\t\t}\n" +
                    "\t}\n" +
                    "\telse\n" +
                    "\t{\n" +
                    "\t\tint sideIndex = InTakenIndexRel(" + rightSideWallRelX + "," + rightSideWallRelY + ");\n" +
                    "\t\tif (sideIndex [2] middleIndex)\n" +
                    "\t\t{\n" +
                    countAreaRule.Replace("\t\t", "\t\t\t") +
                    "\t\t}\n" +
                    "\t}\n" +
                    "}\n" +
                    "else\n" +
                    "{\n" +
                    "\tmiddleIndex = InBorderIndexRel(" + middleWallRelX + "," + middleWallRelY + ");\n" +
                    "\tint farSideIndex = InBorderIndexRel(" + (circleDirectionLeft ? rightSideWallRelX : leftSideWallRelX) + "," + (circleDirectionLeft ? rightSideWallRelY : leftSideWallRelY) + ");\n" +
                    "\tif (farSideIndex > middleIndex)\n" +
                    "\t{\n" +
                    countAreaRule +
                    "\t}\n" +
                    "}";

                if (circleDirectionLeft)
                {
                    ruleCore = ruleCore.Replace("[1]", ">");
                    ruleCore = ruleCore.Replace("[2]", "<");
                }
                else
                {
                    ruleCore = ruleCore.Replace("[1]", "<");
                    ruleCore = ruleCore.Replace("[2]", ">");
                }
            }
            else
            {
                ruleCore = variableName + " = true;\n" + forbiddenStr;
                    
            }

            if (meta[0] == "True")
            {
                ruleCore = "\t\t\t" + ruleCore.Replace("\n", "\n\t\t\t") + "\n";
                codeStr = "for (int i = 0; i < 2; i++)\n" +
                    "{\n" +
                    "\tfor (int j = 0; j < 2; j++)\n" +
                    "\t{\n" +
                    "\t\tif (" + conditionStr + ")\n" +
                    "\t\t{\n" +
                    ruleCore +
                    "\t\t}\n" +
                    "\t\tint s0 = sx;\n" +
                    "\t\tint s1 = sy;\n" +
                    "\t\tsx = -lx;\n" +
                    "\t\tsy = -ly;\n" +
                    "\t\tlx = s0;\n" +
                    "\t\tly = s1;\n" +
                    "\t}\n" +
                    "\tsx = thisSx;\n" +
                    "\tsy = thisSy;\n" +
                    "\tlx = -thisLx;\n" +
                    "\tly = -thisLy;\n" +
                    "}\n" +
                    "sx = thisSx;\n" +
                    "sy = thisSy;\n" +
                    "lx = thisLx;\n" +
                    "ly = thisLy;";

            }
            else if (meta[1] == "True")
            {
                ruleCore = "\t\t\t" + ruleCore.Replace("\n", "\n\t\t\t") + "\n";
                codeStr = "for (int i = 0; i < 2; i++)\n" +
                    "{\n" +
                    "\tfor (int j = 0; j < 2; j++)\n" +
                    "\t{\n" +
                    "\t\tif (" + conditionStr + ")\n" +
                    "\t\t{\n" +
                    ruleCore +
                    "\t\t}\n" +
                    "\t\tint l0 = lx;\n" +
                    "\t\tint l1 = ly;\n" +
                    "\t\tlx = -sx;\n" +
                    "\t\tly = -sy;\n" +
                    "\t\tsx = l0;\n" +
                    "\t\tsy = l1;\n" +
                    "\t}\n" +
                    "\tsx = thisSx;\n" +
                    "\tsy = thisSy;\n" +
                    "\tlx = -thisLx;\n" +
                    "\tly = -thisLy;\n" +
                    "}\n" +
                    "sx = thisSx;\n" +
                    "sy = thisSy;\n" +
                    "lx = thisLx;\n" +
                    "ly = thisLy;";
            }
            else
            {
                ruleCore = "\t\t" + ruleCore.Replace("\n", "\n\t\t") + "\n";
                codeStr = "for (int i = 0; i < 2; i++)\n" +
                    "{\n" +
                    "\tif (" + conditionStr + ")\n" +
                    "\t{\n" +
                    ruleCore +
                    "\t}\n" +
                    "\tlx = -lx;\n" +
                    "\tly = -ly;\n" +
                    "}\n" +
                    "lx = thisLx;\n" +
                    "ly = thisLy;";
            }

            codeStr = "\t\t\t\t" + codeStr.Replace("\n", "\n\t\t\t\t") + "\n\n";

            return codeStr;
        }


        // ----- Metadata settings -----

        
        private void RotateClockwise_Click(object sender, RoutedEventArgs e)
        {
            if (RotateClockwise.IsChecked == null || (bool)RotateClockwise.IsChecked)
            {
                RotateCounterClockwise.IsChecked = false;
            }
            SaveMeta();
        }

        private void RotateCounterClockwise_Click(object sender, RoutedEventArgs e)
        {
            if (RotateCounterClockwise.IsChecked == null || (bool)RotateCounterClockwise.IsChecked)
            {
                RotateClockwise.IsChecked = false;
            }
            SaveMeta();
        }

        private void CircleDirectionLeft_Click(object sender, RoutedEventArgs e)
        {
            SaveMeta();
        }

        private void SaveMeta()
        {
            if (newRule == null) return;
            int pos = newRule.IndexOf("<svg");
            newRule = "<!--|" + RotateClockwise.IsChecked + "|" + RotateCounterClockwise.IsChecked + "|" + CircleDirectionLeft.IsChecked + "|-->\r\n" + newRule.Substring(pos);
            File.WriteAllText(svgName, newRule);
        }


        // ----- Log and helper functions -----


        private void T(object o)
        {
            Trace.WriteLine(o.ToString());
        }

        private void M(object o)
        {
            MessageBox.Show(o.ToString());
        }

        private int FindDirection(int xDiff, int yDiff)
        {
            for (int i = 0; i < 4; i++)
            {
                if (directions[i][0] == xDiff && directions[i][1] == yDiff)
                {
                    return i;
                }
            }
            return 0;
        }

        private void SKElement_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            string fileName = (string)((SKElement)sender).Tag + ".svg";
            if (!File.Exists(fileName)) return;
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            var svg = new SkiaSharp.Extended.Svg.SKSvg();
            T("SKElement_PaintSurface Filename: " + fileName);
            var picture = svg.Load(fileName);

            var fit = e.Info.Rect.AspectFit(svg.CanvasSize.ToSizeI());
            e.Surface.Canvas.Scale(fit.Width / svg.CanvasSize.Width);
            e.Surface.Canvas.DrawPicture(picture);
        }
    }
}
