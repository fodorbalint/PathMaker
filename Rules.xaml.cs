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
        string liveEnd, emptyField, takenField, takenOrBorderField, futureStartField, futureEndField, forbiddenField, notCornerField, countAreaStartField, countAreaEndField;
        string newRule;
        int draggedElement = 0;
        SKElement draggedObj;
        double startMouseX, startMouseY;
        Thickness origMargin;
        List<int[]> takenCoordinates = new();
        List<int[]> forbiddenCoordinates = new();
        int[]? noCornerCoordinates = null;
        int[]? countAreaStartCoordinates = null;
        int[]? countAreaEndCoordinates = null;
        int childIndex = 0;
        List<string> activeRules = new();


        public Rules()
        {
            InitializeComponent();
            LoadDir();
            XSize.Text = "6";
            YSize.Text = "5";
            AppliedSize.Text = "9";
        }

        private void LoadDir()
        {
            MainGrid.Children.RemoveRange(0, childIndex);
            childIndex = 0;
            string[] listOfSizes = Directory.GetDirectories("rules");            
            int yTotalPos = 0;
            string codeString = "namespace OneWayLabyrinth\n{\n\tpublic partial class Path\n\t{\n[conditionVariablesDeclare]\n\t\tpublic void RunRules()\n\t\t{\n[conditionVariablesReset]\n";
            string conditionVariablesDeclare = "";
            string conditionVariablesReset = "";
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

                codeString += "\t\t\tif (size >= " + sizeNumber + ")\n\t\t\t{\n";

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
                    }
                    codeString += "\t\t\t\t// " + ruleName + "\n";
                    activeRules.Add(ruleName);

                    pos = 0;
                    int section1Pos = content.IndexOf("<!--1-->");
                    int section2Pos = content.IndexOf("<!--2-->");
                    List<int[]> takenFields = new();
                    List<int[]> forbiddenFields = new();
                    int[]? noCornerField = null;
                    int[]? countAreaStartField = null;
                    int[]? countAreaEndField = null;

                    while (true)
                    {
                        pos = content.IndexOf("<!-- ", pos);
                        if (pos == -1) break;
                        lastPos = content.IndexOf(" -->", pos);
                        string codeLine = content.Substring(pos + 5, lastPos - pos - 5);
                        string[] arr = codeLine.Split(" ");
                        if (pos < section1Pos)
                        {
                            takenFields.Add(new int[] { int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]) });
                        }
                        else if (pos < section2Pos)
                        {
                            if (int.Parse(arr[2]) == 9)
                            {
                                countAreaStartField = new int[] { int.Parse(arr[0]), int.Parse(arr[1]) };
                            }
                            else
                            {
                                countAreaEndField = new int[] { int.Parse(arr[0]), int.Parse(arr[1]) };
                            }
                        }
                        else
                        {
                            if (int.Parse(arr[2]) == 8)
                            {
                                forbiddenFields.Add(new int[] { int.Parse(arr[0]), int.Parse(arr[1]) });
                            }
                            else
                            {
                                noCornerField = new int[] { int.Parse(arr[0]), int.Parse(arr[1]) };
                            }
                        }
                        pos = lastPos;
                    }

                    T("countAreaStartField " + countAreaStartField + " countAreaEndField " + countAreaEndField);

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
                        if (countAreaStartField != null && countAreaEndField != null)
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

                    
                    codeString += GenerateCode(variableName, metaArr, takenFields, forbiddenFields, noCornerField, countAreaStartField, countAreaEndField);
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
            codeString += "\t\t}\n\t}\n}";
            codeString = codeString.Replace("[conditionVariablesDeclare]", conditionVariablesDeclare);
            codeString = codeString.Replace("[conditionVariablesReset]", conditionVariablesReset);
            File.WriteAllText("PathRules.cs", codeString);
        }

        private string GenerateCode(string variableName, string[] meta, List<int[]> takenFields, List<int[]> forbiddenFields, int[]? noCornerField, int[]? countAreaStartField, int[]? countAreaEndField)
        {
            if (variableName == "CShape") return "\t\t\t\t// Embedded in Path.cs as the absolute checking functions need it.\n\n";

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

            foreach (int[] field in takenFields)
            {
                int relX = startX - field[0];
                int relY = startY - field[1];
                switch (field[2])
                {
                    case 2:
                        conditionStr += "!InTakenRel(" + relX + "," + relY + ") && ";
                        break;
                    case 3:
                        conditionStr += "InTakenRel(" + relX + "," + relY + ") && ";
                        break;
                    case 4:
                        conditionStr += "(InTakenRel(" + relX + "," + relY + ") || InBorderRel(" + relX + "," + relY + ")) && ";
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

                // First, we need to make sure that the wall ahead is going into the direction according the circle direction (it has to go in the same direction)
                // Find wall fields, one straight, one side, but we need to find which side is taken
                // The wall can also be the border !

                int[] middleWall;

                // suppose count area start and end is 2 distance apart

                bool diff2 = false;
                if (Math.Abs(countAreaStartField[0] - countAreaEndField[0]) == 2 || Math.Abs(countAreaStartField[1] - countAreaEndField[1]) == 2)
                {
                    diff2 = true;
                    middleWall = new int[] { countAreaEndField[0] - (countAreaStartField[0] - countAreaEndField[0]) / 2, countAreaEndField[1] - (countAreaStartField[1] - countAreaEndField[1]) / 2 };
                }
                else // fields 1 distance apart, we don't count area, we only need to check the direction of the middle field
                {
                    middleWall = new int[] { countAreaEndField[0] - (countAreaStartField[0] - countAreaEndField[0]), countAreaEndField[1] - (countAreaStartField[1] - countAreaEndField[1]) };
                }

                int relX = startX - middleWall[0];
                int relY = startY - middleWall[1];
                int startRelX = startX - countAreaStartField[0];
                int startRelY = startY - countAreaStartField[1];
                int endRelX = startX - countAreaEndField[0];
                int endRelY = startY - countAreaEndField[1];

                string countAreaRule;

                if (diff2)
                {
                    forbiddenStr = "\t\t\t" + forbiddenStr.Replace("\n", "\n\t\t\t") + "\n";

                    countAreaRule = "\t\tcircleDirectionLeft = (i == 0) ? " + circleDirectionLeft.ToString().ToLower() + " : " + (!circleDirectionLeft).ToString().ToLower() + ";\n" +
                    "\t\tif (CountAreaRel(" + startRelX + "," + startRelY + ", " + endRelX + "," + endRelY + "))\n" +
                    "\t\t{\n" +
                    "\t\t\t" + variableName + " = true;\n" +
                    forbiddenStr +
                    "\t\t}\n";
                }
                else
                {
                    forbiddenStr = "\t\t" + forbiddenStr.Replace("\n", "\n\t\t") + "\n";

                    countAreaRule = "\t\t" + variableName + " = true;\n" +
                    forbiddenStr;
                }

                ruleCore = "int middleIndex = InTakenIndexRel(" + relX + "," + relY + ");\n" +
                    "if (middleIndex != -1)\n" +
                    "{\n" +
                    "\tif (InTakenRel(" + (relX + 1) + "," + relY + "))\n" +
                    "\t{\n" +
                    "\t\tint sideIndex = InTakenIndexRel(" + (relX + 1) + "," + relY + ");\n" +
                    "\t\tif (sideIndex [1] middleIndex)\n" +
                    "\t\t{\n" +
                    countAreaRule.Replace("\t\t", "\t\t\t") +
                    "\t\t}\n" +
                    "\t}\n" +
                    "\telse\n" +
                    "\t{\n" +
                    "\t\tint sideIndex = InTakenIndexRel(" + (relX - 1) + "," + relY + ");\n" +
                    "\t\tif (sideIndex [2] middleIndex)\n" +
                    "\t\t{\n" +
                    countAreaRule.Replace("\t\t", "\t\t\t") +
                    "\t\t}\n" +
                    "\t}\n" +
                    "}\n" +
                    "else\n" +
                    "{\n" +
                    "\tmiddleIndex = InBorderIndexRel(" + relX + "," + relY + ");\n" +
                    "\tint farSideIndex = InBorderIndexRel(" + (circleDirectionLeft?(relX - 1):(relX + 1)) + "," + relY + ");\n" +
                    "\tif (farSideIndex [2] middleIndex)\n" +
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

            countAreaStartField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#008000\" fill-opacity=\"0.25\" />\r\n\t<path d=\"M 0.3 0.3 v 0.4 v -0.2 h 0.4 l -0.13 -0.13 l 0.13 0.13 l -0.13 0.13\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", countAreaStartField);
            if (!File.Exists("countAreaStartField.svg")) File.WriteAllText("countAreaStartField.svg", content);

            countAreaEndField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#008000\" fill-opacity=\"0.25\" />\r\n\t<path d=\"M 0.3 0.5 h 0.4 l -0.13 -0.13 l 0.13 0.13 l -0.13 0.13 l 0.13 -0.13 v 0.2 v -0.4\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", countAreaEndField);
            if (!File.Exists("countAreaEndField.svg")) File.WriteAllText("countAreaEndField.svg", content);

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
        }

        private void SaveRule_Click(object sender, RoutedEventArgs e)
        {
            size = int.Parse(AppliedSize.Text);

            if (countAreaStartCoordinates != null && countAreaEndCoordinates == null)
            {
                M("Count area end field has to be added.");
                return;
            }
            else if (countAreaEndCoordinates != null && countAreaStartCoordinates == null)
            {
                M("Count area start field has to be added.");
                return;
            }
            else if (countAreaStartCoordinates != null && countAreaEndCoordinates != null)
            {
                int takenOrBorderX, takenOrBorderY;
                if (Math.Abs(countAreaStartCoordinates[0] - countAreaEndCoordinates[0]) == 2)
                {
                    takenOrBorderX = countAreaEndCoordinates[0] - (countAreaStartCoordinates[0] - countAreaEndCoordinates[0]) / 2;
                }
                else
                {
                    takenOrBorderX = countAreaEndCoordinates[0] - (countAreaStartCoordinates[0] - countAreaEndCoordinates[0]);
                }

                if (Math.Abs(countAreaStartCoordinates[1] - countAreaEndCoordinates[1]) == 2)
                {
                    takenOrBorderY = countAreaEndCoordinates[1] - (countAreaStartCoordinates[1] - countAreaEndCoordinates[1]) / 2;
                }
                else
                {
                    takenOrBorderY = countAreaEndCoordinates[1] - (countAreaStartCoordinates[1] - countAreaEndCoordinates[1]);
                }

                if (newRule.IndexOf("<!-- " + takenOrBorderX + " " + takenOrBorderY + " 3 -->") == -1 && newRule.IndexOf("<!-- " + takenOrBorderX + " " + takenOrBorderY + " 4 -->") == -1)
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
            if (size%2 == 0 || size < 5 || size > 21)
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
            countAreaStartCoordinates = null;
            countAreaEndCoordinates = null;

            NewRuleGrid.Height = 148 + ySize * 40; // the dragged element will otherwise stretch it on the bottom, no other solution have been found.
            RotateClockwise.IsChecked = false;
            RotateCounterClockwise.IsChecked = false;
            CircleDirectionLeft.IsChecked = false;
            Canvas.InvalidateVisual();
        }

        private void ResizeGrid()
        {
            DrawGrid();

            int pos = newRule.IndexOf("viewBox=\"0 0 ");
            int lastPos = newRule.IndexOf("\"", pos + 13);
            string sizeStr = newRule.Substring(pos + 13, lastPos - pos - 13);
            newRule = newRule.Replace("viewBox=\"0 0 " + sizeStr, "viewBox=\"0 0 " + xSize + " " + ySize);

            pos = newRule.IndexOf("<!--3-->");
            newRule = newRule.Substring(0, pos + 8) + "\r\n" + grid + "\r\n</svg>";

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

            NewRuleGrid.Height = 148 + ySize * 40;
            Canvas.InvalidateVisual();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NewRuleGrid.Visibility = Visibility.Collapsed;
            SaveRule.Visibility = Visibility.Collapsed;
            ResetRule.Visibility = Visibility.Collapsed;
            Cancel.Visibility = Visibility.Collapsed;
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

        private void SaveMeta()
        {
            if (newRule == null) return;
            int pos = newRule.IndexOf("<svg");
            newRule = "<!--|" + RotateClockwise.IsChecked + "|" + RotateCounterClockwise.IsChecked + "|" + CircleDirectionLeft.IsChecked + "|-->\r\n" + newRule.Substring(pos);
            File.WriteAllText(svgName, newRule);
        }

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
            if (CircleDirectionLeft.IsChecked == null || (bool)CircleDirectionLeft.IsChecked)
            {
                CircleDirectionLeft.IsChecked = false;
            }
            SaveMeta();
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

        private void RuleGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            T("Start mouse coordinates: " + e.GetPosition(RuleGrid).X + " " + e.GetPosition(RuleGrid).Y);
            T("NewRuleGrid height " + NewRuleGrid.ActualHeight);
            double x = e.GetPosition(RuleGrid).X;
            double y = e.GetPosition(RuleGrid).Y;
            startMouseX = x;
            startMouseY = y;
            if (y <= 40)
            {
                if (x >= 0 && x <= 40) draggedElement = 1;
                if (x >= 50 && x <= 90) draggedElement = 2;
                if (x >= 100 && x <= 140) draggedElement = 3;
                if (x >= 150 && x <= 190) draggedElement = 4;
                if (x >= 200 && x <= 240) draggedElement = 5;
                if (x >= 250 && x <= 290) draggedElement = 6;
                if (x >= 300 && x <= 340) draggedElement = 7;
                if (x >= 350 && x <= 390) draggedElement = 8;
                if (x >= 400 && x <= 440) draggedElement = 9;
                if (x >= 450 && x <= 490) draggedElement = 10;
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
            draggedObj.Margin = new Thickness(x - startMouseX + 50 * (draggedElement - 1), y - startMouseY, 0, 0);
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
            double centerX = 0;
            double centerY = 0;

            draggedObj.Margin = origMargin;
            Panel.SetZIndex(draggedObj, draggedElement);
            centerX = x - startMouseX + 20 + 50 * (draggedElement - 1);
            centerY = y - startMouseY + 20;

            if (centerX < 0 || centerY < 0)
            {
                draggedElement = 0;
                return;
            }

            int coordX = (int)(centerX - centerX % 40) / 40 + 1;
            int coordY = (int)(centerY - centerY % 40) / 40 + 1;

            if (coordX <= xSize && coordY <= ySize)
            {
                T("End in table: " + coordX + " " + coordY);

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
                else if (draggedElement == 9) // count area start
                {
                    if (!(countAreaStartCoordinates is null))
                    {
                        T("Count area start already exists.");
                        draggedElement = 0;
                        return;
                    }

                    if (!(countAreaEndCoordinates is null) && !(Math.Abs(coordX - countAreaEndCoordinates[0]) == 2 && coordY == countAreaEndCoordinates[1] || Math.Abs(coordY - countAreaEndCoordinates[1]) == 2 && coordX == countAreaEndCoordinates[0] || Math.Abs(coordX - countAreaEndCoordinates[0]) == 1 && coordY == countAreaEndCoordinates[1] || Math.Abs(coordY - countAreaEndCoordinates[1]) == 1 && coordX == countAreaEndCoordinates[0]))
                    {
                        T("Count area start and end field has to be 2 distance apart.");
                        draggedElement = 0;
                        return;
                    }

                    foreach (int[] coord in takenCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY && coord[2] != 2)
                        {
                            T("Count area start can only be placed on an empty field");
                            draggedElement = 0;
                            return;
                        }
                    }

                    countAreaStartCoordinates = new int[] { coordX, coordY };
                    SaveMeta();
                }
                else if (draggedElement == 10) // count area end
                {
                    if (!(countAreaEndCoordinates is null))
                    {
                        T("Count area end already exists.");
                        draggedElement = 0;
                        return;
                    }

                    if (!(countAreaStartCoordinates is null) && !(Math.Abs(coordX - countAreaStartCoordinates[0]) == 2 && coordY == countAreaStartCoordinates[1] || Math.Abs(coordY - countAreaStartCoordinates[1]) == 2 && coordX == countAreaStartCoordinates[0] || Math.Abs(coordX - countAreaStartCoordinates[0]) == 1 && coordY == countAreaStartCoordinates[1] || Math.Abs(coordY - countAreaStartCoordinates[1]) == 1 && coordX == countAreaStartCoordinates[0]))
                    {
                        T("Count area start and end field has to be 2 distance apart.");
                        draggedElement = 0;
                        return;
                    }

                    foreach (int[] coord in takenCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY && coord[2] != 2)
                        {
                            T("Count area end can only be placed on an empty field");
                            draggedElement = 0;
                            return;
                        }
                    }

                    countAreaEndCoordinates = new int[] { coordX, coordY };
                    SaveMeta();
                }
                else
                {
                    if (draggedElement == 1)
                    {
                        if (forbiddenCoordinates.Count == 1)
                        {
                            if (!(Math.Abs(coordX - forbiddenCoordinates[0][0]) == 1 && coordY == forbiddenCoordinates[0][1] || coordY - forbiddenCoordinates[0][1] == 1 && coordX == forbiddenCoordinates[0][0])) {
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

                    if (draggedElement != 2 && (countAreaStartCoordinates != null && coordX == countAreaStartCoordinates[0] && coordY == countAreaStartCoordinates[1] || countAreaEndCoordinates != null && coordX == countAreaEndCoordinates[0] && coordY == countAreaEndCoordinates[1]))
                    {
                        T("Only empty field can be count area start or end.");
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
                        addField = liveEnd.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.5 1", "M " + (coordX - 0.5f) + " " + coordY);
                        if (coordY != ySize)
                        {
                            addField += liveEnd.Replace("M 0 0", "M " + (coordX - 1) + " " + coordY).Replace("M 0.5 1 v -0.5", "M " + (coordX - 0.5f) + " " + coordY + " v 0.5");
                            takenCoordinates.Add(new int[] { coordX, coordY + 1, 0 });
                        }
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
                        addField = countAreaStartField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.3", "M " + (coordX - 1 + 0.3f) + " " + (coordY - 1 + 0.3f));
                        break;
                    case 10:
                        addField = countAreaEndField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.5", "M " + (coordX - 1 + 0.3f) + " " + (coordY - 1 + 0.5f));
                        break;
                }

                if (draggedElement == 7 || draggedElement == 8)
                {
                    newRule = newRule.Replace("<!--3-->", "<!-- " + coordX + " " + coordY + " " + draggedElement + " -->\r\n\t" + addField + "\r\n\t<!--3-->");
                }
                else if (draggedElement == 9 || draggedElement == 10)
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

        private void T(object o)
        {
            Trace.WriteLine(o.ToString());
        }

        private void M(object o)
        {
            MessageBox.Show(o.ToString());
        }
    }
}
