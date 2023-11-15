using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.DirectoryServices.ActiveDirectory;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Diagnostics;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using OpenTK.Graphics.ES11;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

/*

----- OTHER -----

In 1-thin future line extension rule, it is not necessary that the far end is at the corner. We are in a closed loop where the far end cannot have effect on the near end, unless the field 2 to left is part of the same future line which took a U-turn.
Write about counting area start end field rules
Can it happen that two different checknearfield rules are true on opposite sides, and one of them is cancelled due to preconditions?
1021_9: Causes error of field exists in arealine, but we shouldn't come here to start with
1021_10: Causes error of single field in arealine, but we shouldn't come here to start with
Show arealine upon loading from file
Create inferface for disabling/enabling rules. Find out why it is not equal:
- Amount of walkthroughs before getting stuck at the second rule
- Amount of walkthroughs before getting stuck at the first rule, plus further walkthroughs until the second rule.
Becaouse of 1027, we cannot rotate Future 2 x 2 Start End 9 and Future 2 x 2 Start End. Future 3 x 3 Start End cannot be applied on 9 x 9 (not even necessary) 

Needed printed fields: 2 arrows, 1 count area pair/impair start/end with gray background. 2 forbidden fields with gray background
6 border or taken fields, 1 no-corner field with gray background
Draw arrow in rule editor when adding taken field after the area start and end are laid
Reset farStraight = false; and farMidAcross = false; within 2-cycle?


----- 11 x 11 -----

3x3 countarea rule can be rotated counter-clockwise too, but it may only be actual on 11 x 11 (1010_2)

----- 21 x 21 -----

0829: it is right that we cannot step straight or right, but the C-shape condition is not correct, because it only takes into consideration to the field 2 left. The previous step is already impossible. The near end should be extended, and then the main line has no choice.
0829_1: Stepping left will make a loop in the future line to the left. The situation is still impossible, but it is not clear when it became impossible.
0327_2: Draw future line when entering a circle with 1 line to come out from and 3 spaces at the exit (impossible)
0413: Path.CheckFutureSide: check case when both sides are true simultaneously, draw future line on left side to start with. See 0430_2
0415_1: Future line start can be extended, but there is mistakenly no possibilities because of right across check.
	Right now, forbidden fields only apply on the main line when the across field goes up. Are all across checks invalid for future line?
0521: Future line start can be extended now. Taken is connecting to future, but since the left and right fields are not simultaneously empty, future line cannot be extended. In this case, the end of the selected future line cannot fill the empty space next to the main line, unlike it would be the case when connecting to the future section on the top.
0430: previously used CheckLeftRightFuture to determine that we must step towards the future line.
Implement right side of connecting to a loop
Implement CheckNearFutureEnd on 21x21
Test C-shape on 0620_2, one step forward, the near end extends. The future line does not create a C shape if the end of the main line is the empty field to the left.
CountArea needs to be implemented upon closed loop with future ?
0430: When we step on the right field, future line cannot be completed. It is right, but not because of C shape left and straight. The straight C shape is not right, because the field 2 ahead is a section start. We should also consider that the actual end to the right cannot go anywhere else.
0425: Challenge to complete

----- UNKNOWN SIZE -----

1x2, 1x3 future line: check section merge after finding an example
CheckFutureL: find a case when both sides are true
Find out the minimum size for Check1x3 when far end of a future line extends
Countarea is not needed when a future line has completely filled the area.

*/

namespace OneWayLabyrinth
{
	/// <summaly>
	/// Interaction logic for MainWindow.xaml
	/// </summaly>
	public partial class MainWindow : Window
	{
		string grid = "";
		Random rand = new Random();
		public Path taken;
		public Path future;

		List<int> futureIndex = new List<int>();
		public static List<bool> futureActive = new List<bool>();
		List<int> futureLoop = new List<int>();
		List<int> futureLoopSelectedSection = new List<int>();
		public static List<int[]> futureSections = new List<int[]>();
		public static List<int[]> futureSectionMerges = new List<int[]>();
        public static List<List<object>> futureSectionMergesHistory = new List<List<object>>();
        int selectedSection = -1;
        int foundSection = -1;
        int foundIndex = -1;
		int foundEndIndex = -1;
        int foundSectionStart, foundSectionEnd;

        List<int[]> possibleDirections = new List<int[]>(); //directions
		public List<int[]> exits = new List<int[]>();
		public List<int> exitIndex = new List<int>();
		public bool inFuture = false;
		int inFutureIndex = -1;
		int insertIndex = -1;

		string loadFile = "";
		string svgName = "";
		string savePath = "";
		int size = 0;		
		string areaBackground = "";
		List<int[]> directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } }; //down, right, up, left
		DispatcherTimer timer;
		int messageCode = -1;
		bool nearExtDone, farExtDone, nearEndDone, farEndDone; // extension at near / far end, near end connected to live end, far end reached the corner.		
		bool lineFinished = false;
		int nextDirection = -2;
		int lastDirection = -1;
		bool saveCheck, loadCheck, continueCheck;
		public bool keepLeftCheck;
        public int completedCount, fileCompletedCount;
		bool completedWalkthrough = false;
        public bool errorInWalkthrough = false;
		bool toReload = false;
		bool toSave = false;
        CancellationTokenSource source;
        public bool isTaskRunning = false;

        bool displayFuture = true;
        bool displayArea = true;
		public bool makeStats;
        bool displayExits = true;
		bool settingsOpen = false;
        int numberOfRuns = 0;
        int numberOfCompleted = 0;
        int saveFrequency = 10000;
        bool saveConcise = false; // save only routes without their possibilities in one file


        // ----- Initialize -----


        public MainWindow()
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            
            InitializeComponent();

			timer = new DispatcherTimer();
			timer.Tick += Timer_Tick;

			if (File.Exists("settings.txt"))
			{
				string[] lines = File.ReadAllLines("settings.txt");
				string[] arr = lines[0].Split(": ");
				size = int.Parse(arr[1]);				
				arr = lines[1].Split(": ");
				LoadCheck.IsChecked = bool.Parse(arr[1]);
				loadCheck = (bool)LoadCheck.IsChecked;
                arr = lines[2].Split(": ");
				SaveCheck.IsChecked = bool.Parse(arr[1]);
				saveCheck = (bool)SaveCheck.IsChecked;
                arr = lines[3].Split(": ");
                ContinueCheck.IsChecked = bool.Parse(arr[1]);
                continueCheck = (bool)ContinueCheck.IsChecked;                
                arr = lines[4].Split(": ");
                KeepLeftCheck.IsChecked = bool.Parse(arr[1]);
                keepLeftCheck = (bool)KeepLeftCheck.IsChecked;
                arr = lines[5].Split(": ");
                DisplayFutureCheck.IsChecked = bool.Parse(arr[1]);
                displayFuture = (bool)DisplayFutureCheck.IsChecked;
                arr = lines[6].Split(": ");
                DisplayAreaCheck.IsChecked = bool.Parse(arr[1]);
                displayArea = (bool)DisplayAreaCheck.IsChecked;
                arr = lines[7].Split(": ");
                MakeStatsCheck.IsChecked = bool.Parse(arr[1]);
                makeStats = (bool)MakeStatsCheck.IsChecked;

                CheckSize();
				Size.Text = size.ToString();
			}
			else
			{
				size = 9;
				Size.Text = size.ToString();
				LoadCheck.IsChecked = false;
				loadCheck = false;
				SaveCheck.IsChecked = false;
				saveCheck = false;;
                ContinueCheck.IsChecked = false;
				continueCheck = false;
                KeepLeftCheck.IsChecked = false;
				keepLeftCheck = false;
                DisplayFutureCheck.IsChecked = true;
                displayFuture = true;
                DisplayAreaCheck.IsChecked = true;
                displayArea = true;
                MakeStatsCheck.IsChecked = false;
                makeStats = false;
            }
			
			exits = new List<int[]>();
			exitIndex = new List<int>();
			taken = new Path(this, size, new List<int[]>(), null, true);
			future = new Path(this, size, new List<int[]>(), null, false);

			ReadDir();

			if ((bool)loadCheck && loadFile != "")
			{
				LoadFromFile();
			}
			else
			{				
				InitializeList();
			}

			if (File.Exists("settings.txt") && size.ToString() != Size.Text || !File.Exists("settings.txt"))
			{
                Size.Text = size.ToString();
                SaveSettings(null, null);
			}
            DrawGrid();

            if (taken != null && possibleDirections.Count == taken.path.Count) //null checking is only needed for removing warning
			{
                if (!lineFinished)
                {
                    NextStepPossibilities();
                }
				else
				{
                    possibleDirections.Add(new int[] { });
                }
            }
			else if (taken != null && possibleDirections.Count != taken.path.Count + 1)
			{
                M("Error in file", 0);
                return;
			}
            DrawPath();
			CL();
        }

        private void ReadDir()
        {
            loadFile = "";
            string[] files = Directory.GetFiles("./", "*.txt");
            foreach (string file in files)
            {
                string fileName = file.Substring(2);
                if (fileName != "settings.txt" && fileName != "log.txt" && fileName != "completedPaths.txt" && fileName.IndexOf("_temp") == -1 && fileName.IndexOf("_error") == -1)
                {
                    T(fileName);
                    loadFile = fileName;
                    return;
                }
            }
        }

        private void LoadFromFile()
        {
            string content = File.ReadAllText(loadFile);
            string[] loadPath;
            bool circleDirectionLeft = true;
            inFuture = false;
            selectedSection = -1;

            if (content.IndexOf("|") != -1)
            {
                string[] arr = content.Split("|");
                size = int.Parse(arr[0]);
                CheckSize();
                content = arr[1];
            }

            if (content.IndexOf(":") != -1)
            {
                string[] sections = content.Split(":");
                loadPath = sections[0].Split(";");
                string[] exitStrings = sections[1].Split(";");

                int pos = sections[1].LastIndexOf(",");
                circleDirectionLeft = bool.Parse(sections[1].Substring(pos + 1));

                foreach (string exit in exitStrings)
                {
                    string[] arr = exit.Split(",");
                    exits.Add(new int[] { int.Parse(arr[0]), int.Parse(arr[1]) });
                    exitIndex.Add(int.Parse(arr[2]));
                    //if the last field is an exit field, it will be overwritten in NextStepPossibilities
                }
            }
            else
            {
                loadPath = content.Split(";");
            }

            taken = new Path(this, size, new List<int[]>(), null, true);
            possibleDirections = new List<int[]>();
            InitializeFuture();

            taken.circleDirectionLeft = circleDirectionLeft;

            if (content.IndexOf("-") != -1) // normal mode, with possibilities
            {
                foreach (string coords in loadPath)
                {
                    string[] sections = coords.Split("-");
                    int[] possibles = Array.ConvertAll(sections[0].Split(","), s => int.Parse(s));
                    possibleDirections.Add(possibles);
                    if (sections.Length == 2)
                    {
                        int[] field = Array.ConvertAll(sections[1].Split(","), s => int.Parse(s));
                        taken.path.Add(field);
                        int x = field[0];
                        int y = field[1];
                        taken.x = x;
                        taken.y = y;

                        CheckFutureLine(x, y);
                    }
                }
            }
            else // only coordinates
            {
                int startX = 0;
                int startY = 1;

                foreach (string coords in loadPath)
                {
                    int[] field = Array.ConvertAll(coords.Split(","), s => int.Parse(s));
                    taken.path.Add(field);
                    int x = field[0];
                    int y = field[1];
                    taken.x = x;
                    taken.y = y;

                    possibleDirections.Add(new int[] { FindDirection(x - startX, y - startY) });
                    startX = x;
                    startY = y;

                    CheckFutureLine(x, y);
                }
                possibleDirections.Add(new int[] { });
            }

            T("LoadFromFile " + taken.path.Count + " " + possibleDirections.Count);

            nextDirection = -2;

            if (taken.path.Count > 1)
            {
                int[] prevField = taken.path[taken.path.Count - 2];
                int prevX = prevField[0];
                int prevY = prevField[1];
                for (int i = 0; i < 4; i++)
                {
                    //last movement: down, right, up, left
                    int dx = directions[i][0];
                    int dy = directions[i][1];

                    if (taken.x - prevX == dx && taken.y - prevY == dy)
                    {
                        lastDirection = i;
                    }
                }
            }
            else lastDirection = 0;

            if (taken.x == size && taken.y == size)
            {
                lineFinished = true;
            }
            else
            {
                lineFinished = false;
            }

            if (loadFile.IndexOf("_") != -1)
            {
                string[] arr = loadFile.Split("_");
                fileCompletedCount = int.Parse(arr[0]);
            }

            CurrentCoords.Content = taken.x + " " + taken.y;
            PossibleCoords.Text = "";

            taken.possible = new List<int[]>();
            foreach (int direction in possibleDirections[possibleDirections.Count - 1])
            {
                PossibleCoords.Text += taken.x + directions[direction][0] + " " + (taken.y + directions[direction][1]) + "\n";
                taken.possible.Add(new int[] { taken.x + directions[direction][0], taken.y + directions[direction][1] });
            }
        }

        private void InitializeList()
        {
            taken = new Path(this, size, new List<int[]>
                {
                    new int[] {1, 1}
                }, null, true);
            possibleDirections = new List<int[]> { new int[] { 1 }, new int[] { 0, 1 } };
            nextDirection = -2;
            lastDirection = 0;
            InitializeFuture();
            fileCompletedCount = 0;

            if (!isTaskRunning)
            {
                CurrentCoords.Content = taken.x + " " + taken.y;
                PossibleCoords.Text = "";
            }

            foreach (int direction in possibleDirections[possibleDirections.Count - 1])
            {
                if (!isTaskRunning)
                {
                    PossibleCoords.Text += taken.x + directions[direction][0] + " " + (taken.y + directions[direction][1]) + "\n";
                }

                taken.possible.Add(new int[] { taken.x + directions[direction][0], taken.y + directions[direction][1] });
            }

            lineFinished = false;
        }

        private void InitializeFuture()
        {
            future = new Path(this, size, new List<int[]>(), null, false);
            futureIndex = new List<int>();
            futureActive = new List<bool>();
            futureLoop = new List<int>();
            futureLoopSelectedSection = new List<int>();
            futureSections = new List<int[]>();
            futureSectionMerges = new List<int[]>();
            futureSectionMergesHistory = new List<List<object>>();
        }

        private void CheckSize()
        {
            if (size > 99)
            {
                M("Size should be between 3 and 99.", 0);
                size = 99;
            }
            else if (size < 3)
            {
                M("Size should be between 3 and 99.", 0);
                size = 3;
            }
            else if (size % 2 == 0)
            {
                M("Size cannot be pair.", 0);
                size = size - 1;
            }
        }


        // ----- Button click -----


        private void FastRun_Click(object sender, RoutedEventArgs e)
        {
            FocusButton.Focus();
            if (isTaskRunning)
            {
                T("Stopping timer");
                source.Cancel();
                return;
            }
            else if (timer.IsEnabled)
            {
                StopTimer();
                return;
            }

            if (taken.path.Count >= 1)
            {
                StartTimer(true);
            }
        }

        private void StartStop_Click(object sender, RoutedEventArgs e)
        {
            FocusButton.Focus();
            if (isTaskRunning)
            {
                source.Cancel();
                return;
            }
            else if (timer.IsEnabled)
            {
                StopTimer();
                return;
            }

            if (taken.path.Count >= 1)
            {
                StartTimer(false);
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            FocusButton.Focus();

            if (sender != null)
            {
                if (isTaskRunning)
                {
                    source.Cancel();
                    toReload = true;
                    return;
                }
                else if (timer.IsEnabled)
                {
                    StopTimer();
                }
            }

            HideMessage();
            messageCode = -1;

            exits = new List<int[]>();
            exitIndex = new List<int>();
            areaBackground = "";

            int oldSize = size;
            size = int.Parse(Size.Text);
            CheckSize();
            if (oldSize != size)
            {
                SaveSettings(null, null);
            }

            ReadDir();

            if (loadCheck == true && loadFile != "")
            {
                LoadFromFile();
            }
            else
            {
                InitializeList();
            }

            if (size.ToString() != Size.Text)
            {
                Size.Text = size.ToString();
                SaveSettings(null, null);
            }

            DrawGrid();

            if (possibleDirections.Count == taken.path.Count)
            {
                if (!lineFinished)
                {
                    NextStepPossibilities();
                }
                else
                {
                    possibleDirections.Add(new int[] { });
                }
            }
            else if (possibleDirections.Count != taken.path.Count + 1)
            {
                M("Error in file", 0);
                return;
            }

            DrawPath();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
		{
            FocusButton.Focus();
			if (messageCode == -1)
			{
                if (isTaskRunning)
                {
                    source.Cancel();
                    toSave = true;
                    return;
                }
                else if (timer.IsEnabled)
                {
                    StopTimer();
                }
            }

            ReadDir();

            string saveName = (errorInWalkthrough) ? completedCount + ".txt" : loadFile;
			if (saveName == "")
			{
				saveName = DateTime.Today.Month.ToString("00") + DateTime.Today.Day.ToString("00") + ".txt";
			}
			File.WriteAllText(saveName, savePath);
		}

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            FocusButton.Focus();
            if (isTaskRunning)
            {
                source.Cancel();
                return;
            }
            else if (timer.IsEnabled)
            {
                StopTimer();
                return;
            }

            NextClick();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
		{
            FocusButton.Focus();
            if (isTaskRunning)
            {
                source.Cancel();
                return;
            }
            else if (timer.IsEnabled)
            {
                StopTimer();
                return;
            }

            if (messageCode == 2)
			{
				possibleDirections.RemoveAt(possibleDirections.Count - 1);
				PreviousStepWithFuture();
				messageCode = -1;
				DrawPath();
				return;
			}
			else if (messageCode != -1) return;

			PreviousStep(true);
			DrawPath();
		}


        // ----- Button functions -----


        private void StartTimer(bool fastRun)
        {
            T("Starting timer");
            if (messageCode != -1) return;

            MessageLine.Content = "";
            FastRunButton.Content = "Stop run";
            FastRunButton.Style = Resources["RedButton"] as Style;
            StartStopButton.Content = "Stop";
            StartStopButton.Style = Resources["RedButton"] as Style;
            if (!fastRun)
            {
                timer.Interval = TimeSpan.FromMilliseconds(100);
                timer.Start();
            }
            else
            {
                if (makeStats && !keepLeftCheck)
                {
                    CL();
                    numberOfRuns = 0;
                    numberOfCompleted = 0;
                }
                completedWalkthrough = false;
                errorInWalkthrough = false;
                MessageLine.Visibility = Visibility.Visible;
                if (saveConcise) File.WriteAllText("completedPaths.txt", "");
                source = new CancellationTokenSource();
                CancellationToken token = source.Token;
                Task task = new Task(() => DoThread(), token);
                task.Start();
                isTaskRunning = true;
                T("StartTimer task started");
            }
        }

        private void DoThread()
        {
            completedCount = fileCompletedCount;
            do
            {
                Timer_Tick(null, null);
                if (source.IsCancellationRequested) break;
            }
            while (!completedWalkthrough && !errorInWalkthrough);

            if (!source.IsCancellationRequested && makeStats && !keepLeftCheck)
            {
                if (numberOfRuns < 999)
                {
                    numberOfRuns++;
                    numberOfCompleted += completedCount;
                    errorInWalkthrough = false;
                    messageCode = -1;
                    Dispatcher.Invoke(() =>
                    {
                        InitializeList();

                        T("New list initialized " + taken.path.Count);
                        MessageLine.Visibility = Visibility.Visible;
                        MessageLine.Content = "In " + numberOfRuns + " runs, average " + Math.Round((float)numberOfCompleted / numberOfRuns, 1) + " per run.";
                        source = new CancellationTokenSource();
                        CancellationToken token = source.Token;
                        Task task = new Task(() => DoThread(), token);
                        task.Start();
                    });
                }
                else
                {
                    numberOfRuns++;
                    numberOfCompleted += completedCount;
                    errorInWalkthrough = false;
                    messageCode = -1;
                    isTaskRunning = false;

                    Dispatcher.Invoke(() =>
                    {
                        StopTimer();
                        MessageLine.Content = "In " + numberOfRuns + " runs, average " + Math.Round((float)numberOfCompleted / numberOfRuns, 1) + " per run.";
                        DrawPath();
                    });
                }
            }
            else if (makeStats && !keepLeftCheck) // stopped by button / critical error: start and end fields in countarea unequal
            {
                isTaskRunning = false;

                Dispatcher.Invoke(() =>
                {
                    StopTimer();
                    if (MessageLine.Content.ToString().IndexOf("inequal") == -1 && MessageLine.Content.ToString().IndexOf("arealine") == -1)
                    {
                        MessageLine.Content = "In " + numberOfRuns + " runs, average " + Math.Round((float)numberOfCompleted / numberOfRuns, 1) + " per run.";
                    }
                    DrawPath();
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    isTaskRunning = false;
                    StopTimer(); //in case of countarea inequal error, it is already called.

                    if (toReload)
                    {
                        toReload = false;
                        Reload_Click(null, null);
                    }
                    else if (toSave)
                    {
                        toSave = false;
                        Save_Click(null, null);
                        DrawPath();
                    }
                    else
                    {
                        if (completedWalkthrough) M("The number of walkthroughs are " + completedCount + ".", 0);
                        else if (!errorInWalkthrough) M(completedCount + " walkthroughs are completed.", 0);
                        else MessageLine.Content = "Error at " + completedCount + ": " + MessageLine.Content;
                        DrawPath();
                    }
                });
            }
        }        

        private void Timer_Tick(object? sender, EventArgs e)
		{
			NextClick();           
		}

        private void StopTimer()
        {
            FastRunButton.Content = "Fast run";
            FastRunButton.Style = Resources["GreenButton"] as Style;
            StartStopButton.Content = "Start";
            StartStopButton.Style = Resources["GreenButton"] as Style;
            if (!isTaskRunning) timer.Stop();
        }

        public void StopAll(string error) // used when count area is inequal in Path.cs or there is no possibility to move
        {
            if (isTaskRunning)
            {
                source.Cancel();
            }
            else if (timer.IsEnabled)
            {
                StopTimer();
            }
            M(error, 0);
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            loadCheck = (bool)LoadCheck.IsChecked;
            saveCheck = (bool)SaveCheck.IsChecked;
            continueCheck = (bool)ContinueCheck.IsChecked;
            keepLeftCheck = (bool)KeepLeftCheck.IsChecked;
            displayFuture = (bool)DisplayFutureCheck.IsChecked;
            displayArea = (bool)DisplayAreaCheck.IsChecked;
            makeStats = (bool)MakeStatsCheck.IsChecked;

            string[] lines = new string[] { "size: " + size, "loadFromFile: " + loadCheck, "saveOnCompletion: " + saveCheck, "continueOnCompletion: " + continueCheck, "keepLeft: " + keepLeftCheck, "displayFutureLines: " + displayFuture, "displayArea: " + displayArea, "makeStats: " + makeStats  };

            string linesStr = string.Join("\n", lines);

            string fileContent = File.ReadAllText("settings.txt");
            int pos = fileContent.IndexOf("appliedSize");

            if (pos!= -1)
            {
                File.WriteAllText("settings.txt", linesStr + "\n" + fileContent.Substring(pos));
            }
            else
            {
                File.WriteAllLines("settings.txt", lines);
            }
        }

        private int Move(int directionIndex)
        {
            T("Move x " + taken.x + " y " + taken.y + " directionIndex " + directionIndex);

            List<int> possibleFields = possibleDirections[possibleDirections.Count - 1].ToList<int>();
            if (possibleFields.IndexOf(directionIndex) != -1)
            {
                if (AddNextStep(taken.x + directions[directionIndex][0], taken.y + directions[directionIndex][1]))
                {
                    // draw and next step possibilities
                    return 2;
                }
                else
                {
                    // draw only
                    return 1;
                }
            }
            else
            {
                if (taken.path.Count == 1) return 0;

                int newX = taken.x + directions[directionIndex][0];
                int newY = taken.y + directions[directionIndex][1];
                int[] prevField = taken.path[taken.path.Count - 2];
                if (prevField[0] == newX && prevField[1] == newY)
                {
                    PreviousStep(true);
                    return 1; //back step, draw only, future possibilities already calculated
                }
            }
            return 0; //the chosen direction was no option to move, no draw
        }


        // ----- Line progress -----


        private void NextClick()
		{
			if (isTaskRunning && messageCode != -1 && messageCode != 3) // 3 indicats halfway walkthrough
            {
                T("NextClick error in walkthrough");
				errorInWalkthrough = true;
				return;
            }

			if (messageCode == 2)
			{
				possibleDirections.RemoveAt(possibleDirections.Count - 1);
				PreviousStepWithFuture();
				messageCode = -1;
				DrawPath();
				return;
			}
			else if (messageCode != -1) return;

			if (taken.x == size && taken.y == size)
			{
                // step back until there is an option to move right of the step that had been taken.
                if ((continueCheck == true || isTaskRunning) && keepLeftCheck == true)
                {
					bool rightFound = false;
					nextDirection = -1;
					int c;

					do
					{
						PreviousStep(true);
						c = taken.path.Count;

						int[] startField = taken.path[c - 2];
						int[] newField = taken.path[c - 1];
						int startX = startField[0];
						int startY = startField[1];
						int newX = newField[0];
						int newY = newField[1];

						for (int i = 0; i < 4; i++)
						{
							if (directions[i][0] == newX - startX && directions[i][1] == newY - startY)
							{
								foreach (int direction in possibleDirections[c - 1])
								{
									if (direction == i - 1 || i == 0 && direction == 3)
									{
										rightFound = true;
										break;
									} 
								}
								if (rightFound)
								{
									nextDirection = i == 0 ? 3 : i - 1;
									PreviousStep(true);
								}
							}
						}

					} while (!rightFound && c > 2);

					if (!rightFound)
					{
						PreviousStep(true); // c = 2. We reached the end, step back to the start position
											 // Reset nextDirection, so that we can start again
						completedWalkthrough = true;
						nextDirection = -2;

                        if (timer.IsEnabled)
                        {
                            StopTimer();
                        }
                    }
					else if (c == 2)
					{
                        M("Halfway: " + completedCount + " walkthroughs are completed.", 3);
                        Dispatcher.Invoke(() =>
                        {
							DrawPath();
                        });
                    }

                    if (!isTaskRunning) DrawPath();
                }
                else if (continueCheck == true || isTaskRunning)
                {
                    exits = new List<int[]>();
                    exitIndex = new List<int>();
                    areaBackground = "";

                    InitializeList();
                    if (!(isTaskRunning)) DrawPath();
                }
				else if (timer.IsEnabled)
				{
					StopTimer();
				} 
                return;
			}

			if (NextStep())
			{
				if (taken.x == size && taken.y == size)
				{
                    if (taken.path.Count != size * size)
                    {
                        possibleDirections.Add(new int[] { });
                        errorInWalkthrough = true;

                        if (!(isTaskRunning && makeStats && !keepLeftCheck))
                        {
                            StopAll("The number of steps were only " + taken.path.Count + ".");
                        }
						else
						{
							L("The number of steps were only " + taken.path.Count + ".");
						}

						if (!isTaskRunning) DrawPath();
                    }
					else
					{
                        possibleDirections.Add(new int[] { });
                        lineFinished = true;
                        if (isTaskRunning)
                        {
                            completedCount++;
                            string pathStr = "";
                            foreach (int[] field in taken.path)
                            {
                                pathStr += field[0] + ","+ field[1] + ";";
                            }
                            pathStr = pathStr.Substring(0, pathStr.Length - 1);
                            if (saveConcise) File.AppendAllText("completedPaths.txt", pathStr + "\n");
                            Dispatcher.Invoke(() =>
                            {
                                if (!(makeStats && !keepLeftCheck))
                                {
                                    MessageLine.Content = completedCount + " walkthroughs are completed.";
                                }
                            });

                            if (saveCheck && completedCount % saveFrequency == 0)
							{
                                SavePath();
                            }
							
                            //Dispatcher.Invoke(() => { DrawPath(); }); //frequent error in SKCanvas: Attempt to read or write protected memory
                        }
                        else DrawPath();
                    }
                    
                    return;
				}

				NextStepPossibilities();
			}
            if (!(isTaskRunning)) DrawPath();
		}        

        private bool NextStep()
        {
            T("NextStep taken.possible.Count: " + taken.possible.Count + " errorInWalkthrough " + errorInWalkthrough);
            if (!isTaskRunning && taken.possible.Count == 0)
            {
                T("Removing last element due to no options");
                PreviousStep(false);
                return false;
            }
            else if (taken.possible.Count == 0)
            {
                errorInWalkthrough = true;
                return false;
            }

            if (nextDirection != -1)
            {
                bool determined = false;
                int lastDirectionTemp = -1;

                int[] newField = new int[] { };
                if (nextDirection == -2)
                {
                    if (keepLeftCheck != true)
                    {
                        newField = taken.possible[rand.Next(0, taken.possible.Count)];
                    }
                    else
                    {
                        // Find the most left field. It is possible to have the left and right field but not straight when the program steps back from an impair count area situation.
                        int[] newDirections = possibleDirections[possibleDirections.Count - 1];

                        T("Keeping left, lastDirection " + lastDirection);
                        foreach (int dir in newDirections)
                        {
                            T("Direction in last possibles: " + dir);
                        }

                        bool foundLeft = false;
                        bool foundStraight = false;
                        int i = 0;
                        for (i = 0; i < newDirections.Length; i++)
                        {
                            T("NextStep, lastDirection: " + lastDirection);
                            int leftDirection = lastDirection == 3 ? 0 : lastDirection + 1;
                            if (newDirections[i] == leftDirection)
                            {
                                foundLeft = true;
                                break;
                            }
                            else if (newDirections[i] == lastDirection)
                            {
                                foundStraight = true;
                                //no break, left may be found later
                            }
                        }
                        if (foundLeft)
                        {
                            lastDirectionTemp = newDirections[i];
                            newField = new int[] { taken.x + directions[lastDirectionTemp][0], taken.y + directions[lastDirectionTemp][1] };
                        }
                        else if (foundStraight)
                        {
                            lastDirectionTemp = lastDirection;
                            newField = new int[] { taken.x + directions[lastDirectionTemp][0], taken.y + directions[lastDirectionTemp][1] };
                        }
                        else //only right is possible
                        {
                            lastDirectionTemp = newDirections[0];
                            newField = new int[] { taken.x + directions[lastDirectionTemp][0], taken.y + directions[lastDirectionTemp][1] };
                        }
                    }
                }
                else
                {
                    T("Determined next direction " + nextDirection);
                    determined = true;
                    newField = new int[] { taken.x + directions[nextDirection][0], taken.y + directions[nextDirection][1] };
                    lastDirection = nextDirection;
                    nextDirection = -2;
                }

                if (!AddNextStep(newField[0], newField[1]))
                {
                    return false;
                }
                else
                {
                    if (!determined)
                    {
                        lastDirection = lastDirectionTemp;
                    }

                    return true;
                }
            }
            else return true;
        }

        private bool AddNextStep(int x, int y)
        {
            taken.x = x;
            taken.y = y;
            taken.minAreaLine = new();

            T("AddNextStep newX " + x + " newY " + y);

            areaBackground = "";

            taken.path.Add(new int[] { x, y });

            if (!isTaskRunning) CurrentCoords.Content = x + " " + y;

            return CheckFutureLine(x, y);
        }

        private bool CheckFutureLine(int x, int y)
        {
            //if (inFutureIndex == -1) DrawPath();
            T("CheckFutureLine inFuture: " + inFuture + " x " + x + " y " + y + " selectedSection " + selectedSection + " inFutureIndex: " + inFutureIndex);
            if (future.path.Count > 0)
            {
                //we don't necessarily step on the last future section (for example when there are future lines on both sides)
                //check if we are at the end of a future section
                if (inFuture)
                {
                    int endIndex = futureSections[selectedSection][1];
                    int[] field = future.path[endIndex];

                    T(endIndex + " " + field[0] + " " + field[1]);
                    if (field[0] == x && field[1] == y)
                    {
                        // check connected sections. If selectedSection is among the merged paths and not the last of a merge, we do not exit inFuture.
                        bool isConnected = false;
                        for (int i = 0; i < futureSectionMerges.Count; i++)
                        {
                            int[] merge = futureSectionMerges[i];
                            for (int j = 0; j < merge.Length - 1; j++)
                            {
                                if (merge[j] == selectedSection)
                                {
                                    isConnected = true;
                                    futureActive[inFutureIndex] = false;
                                    inFutureIndex = futureSections[merge[j + 1]][0];
                                    selectedSection = merge[j + 1];
                                    T("Connected section, new selectedSection " + selectedSection + " new inFutureIndex: " + inFutureIndex);
                                    break;
                                }
                            }
                            if (isConnected) break;
                        }

                        if (!isConnected)
                        {
                            T("Exiting future " + x + " " + y + " at " + endIndex);
                            futureActive[endIndex] = false;
                            inFuture = false;
                            selectedSection = -1;
                        }
                    }
                    else
                    {
                        futureActive[inFutureIndex] = false;
                        inFutureIndex--;
                    }

                    return AddFutureLines();
                }
                else
                {
                    //check if we stepped on the start of a future field
                    selectedSection = -1;
                    int foundIndex = -1;

                    //T("Not infuture");

                    for (int i = futureSections.Count - 1; i >= 0; i--)
                    {
                        int startIndex = futureSections[i][0];
                        //T("startIndex: " + startIndex + " count " + future.path.Count);
                        int[] field = future.path[startIndex];
                        if (field[0] == x && field[1] == y)
                        {
                            selectedSection = i;
                            foundIndex = startIndex;
                            break;
                        }
                    }

                    //T("Not infuture foundIndex: " + foundIndex);

                    if (foundIndex != -1)
                    {
                        T("Stepped on future " + x + " " + y + " at " + foundIndex + " selectedSection " + selectedSection);

                        inFuture = true;
                        inFutureIndex = foundIndex - 1;
                        futureActive[foundIndex] = false;

                        T("inFutureIndex " + inFutureIndex);

                        taken.path2 = future.path;

                        int farEndIndex = futureSections[selectedSection][1];
                        int lastMerged = selectedSection;

                        for (int i = 0; i < futureSectionMerges.Count; i++)
                        {
                            int[] merge = futureSectionMerges[i];
                            if (merge[0] == selectedSection)
                            {
                                lastMerged = merge[merge.Length - 1];
                                farEndIndex = futureSections[lastMerged][1];
                            }
                        }

                        int endX = future.path[farEndIndex][0];
                        int endY = future.path[farEndIndex][1];

                        int prevX = taken.path[taken.path.Count - 2][0];
                        int prevY = taken.path[taken.path.Count - 2][1];

                        // if the near and far end are 2 apart, the field between will be now taken by the far end and can be extended further.
                        if (x == endX && Math.Abs(y - endY) == 2 || y == endY && Math.Abs(x - endX) == 2)
                        {
                            future.path2 = taken.path;

                            nearExtDone = true;
                            farExtDone = false;
                            nearEndDone = true;
                            farEndDone = false;

                            if (!ExtendFutureLine(false, foundIndex, farEndIndex, selectedSection, lastMerged, true))
                            {
                                possibleDirections.Add(new int[] { });
                                M("Stepped on future, other end cannot be completed.", 1);
                                //the new possible directions is not set yet.

                                return false; //to prevent NextStepPossibilities from running
                            }
                        }
                        // 1021_3. Only if C-shape is created.
                        if (Math.Abs(x - endX) == 1 && Math.Abs(y - endY) == 1)
                        {
                            future.path2 = taken.path;

                            nearExtDone = true;
                            farExtDone = false;
                            nearEndDone = true;
                            farEndDone = false;

                            if (!ExtendFutureLine(false, foundIndex, farEndIndex, selectedSection, lastMerged, true))
                            {
                                possibleDirections.Add(new int[] { });
                                M("Stepped on future, other end cannot be completed.", 1);
                                //the new possible directions is not set yet.

                                return false; //to prevent NextStepPossibilities from running
                            }
                        }
                        // if the end is next to the lower right corner, and it has 2 possibilities, it has to choose the other field
                        else if (endX == size && endY == size - 1 || endY == size && endX == size - 1)
                        {
                            future.path2 = taken.path;

                            nearExtDone = true;
                            farExtDone = false;
                            nearEndDone = true;
                            farEndDone = false;

                            ExtendFutureLine(false, foundIndex, farEndIndex, selectedSection, lastMerged, true);
                        }

                        // adding other future lines can be timely, as in 0811
                        if (!AddFutureLines()) return false;
                    }
                    //not stepped on a future field
                    else
                    {
                        inFuture = false;
                        return AddFutureLines();
                    }
                }
            }
            //no future field yet
            else
            {
                inFuture = false;
                selectedSection = -1;
                return AddFutureLines();
            }
            return true;
        }

        private bool AddFutureLines()
        {
            int count = taken.path.Count;
            if (count < 3) return true;

            int x = taken.path[count - 2][0];
            int y = taken.path[count - 2][1];

            if (future.path.Count != 0)
            {
                future.path2 = taken.path;

                // 0911: Future line on the right can be extended

                taken.sx = taken.x - taken.path[count - 2][0];
                taken.sy = taken.y - taken.path[count - 2][1];

                for (int i = 0; i < 4; i++)
                {
                    if (directions[i][0] == taken.sx && directions[i][1] == taken.sy)
                    {
                        int newIndex = (i == 3) ? 0 : i + 1;
                        taken.lx = directions[newIndex][0];
                        taken.ly = directions[newIndex][1];
                    }
                }

                taken.thisSx = taken.sx;
                taken.thisSy = taken.sy;
                taken.thisLx = taken.lx;
                taken.thisLy = taken.ly;

                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int sx = taken.sx;
                        int sy = taken.sy;
                        int lx = taken.lx;
                        int ly = taken.ly;

                        if (InFutureStartRel(1, 0) && (InTakenRel(2, 0) || InBorderRel(2, 0) || InFutureRel(2, 0)) && !InTakenRel(1, 1) && !InBorderRel(1, 1) && !InFutureRel(1, 1))
                        {
                            T("1-thin future line valid at " + taken.x + " " + taken.y);

                            InFutureRel(1, 0);
                            int[] sections = FindFutureSections(foundIndex);

                            int nearSection = sections[0];
                            int farSection = sections[1];
                            int nearEndIndex = futureSections[nearSection][0];
                            int farEndIndex = futureSections[farSection][1];

                            if (future.path[farEndIndex][0] == size && future.path[farEndIndex][1] == size)
                            {
                                int addIndex = nearEndIndex + 1;
                                future.path.Insert(addIndex, new int[] { taken.x + lx + sx, taken.y + ly + sy });
                                futureIndex.Insert(addIndex, count - 1);
                                futureActive.Insert(addIndex, true);
                                futureSections[nearSection][0] += 1;
                                IncreaseFurtherSections(foundSection);

                                nearEndDone = false;
                                nearExtDone = false;
                                farExtDone = true;
                                farEndDone = true;

                                if (!ExtendFutureLine(true, addIndex, farEndIndex, nearSection, farSection, false))
                                {
                                    possibleDirections.Add(new int[] { });
                                    T("1-thin future line cannot be completed.");
                                    M("1-thin future line cannot be completed.", 2);
                                    return false;
                                }
                            }


                        }
                        //turn right, pattern goes upwards
                        int s0 = taken.sx;
                        int s1 = taken.sy;
                        taken.sx = -lx;
                        taken.sy = -ly;
                        taken.lx = s0;
                        taken.ly = s1;
                    }
                    //mirror directions
                    taken.sx = taken.thisSx;
                    taken.sy = taken.thisSy;
                    taken.lx = -taken.thisLx;
                    taken.ly = -taken.thisLy;
                }

                // If there was a future start left or right to the head of the line in the previous step, that future line may be extended now if it has no other options to move.
                // Example: 0430_2. All 4 directions of needs to be examined, so that O618 works too.
                // minimum size: 5

                x = taken.path[count - 2][0];
                y = taken.path[count - 2][1];
                taken.x2 = x;
                taken.y2 = y;
                taken.sx = taken.x2 - taken.path[count - 3][0];
                taken.sy = taken.y2 - taken.path[count - 3][1];

                for (int i = 0; i < 4; i++)
                {
                    if (directions[i][0] == taken.sx && directions[i][1] == taken.sy)
                    {
                        int newIndex = (i == 3) ? 0 : i + 1;
                        taken.lx = directions[newIndex][0];
                        taken.ly = directions[newIndex][1];
                    }
                }

                taken.thisSx = taken.sx;
                taken.thisSy = taken.sy;
                taken.thisLx = taken.lx;
                taken.thisLy = taken.ly;

                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int sx = taken.sx;
                        int sy = taken.sy;
                        int lx = taken.lx;
                        int ly = taken.ly;

                        // Future lines does not always extend from a 2x2 shape. 0901_2 has a one-wide line.
                        // Checking InFutureEndRel2(2, 0) gets important in 0928
                        if (InFutureStartRel2(1, 0) && (InTakenRel2(2, 0) || InBorderRel2(2, 0) || (InFutureRel2(2, 0) && (!InFutureEndRel2(2, 0) || InFutureEndRel2(2, 0) && foundSectionStart == foundSectionEnd))) && (InFutureRel2(1, -1) || InBorderRel2(1, -1) || InTakenRel2(1, -1)))
                        {
                            T("Left/right future start valid at x " + x + " y " + y + ", start x " + (x + lx) + " y " + (y + ly));

                            if (!InFutureRel2(1, 1)) // Example: 0902_2
                            {
                                int addIndex = futureSections[foundSection][0] + 1;
                                future.path.Insert(addIndex, new int[] { x + lx + sx, y + ly + sy });
                                futureIndex.Insert(addIndex, count - 1);
                                futureActive.Insert(addIndex, true);
                                futureSections[foundSection][0] += 1;
                                IncreaseFurtherSections(foundSection);
                            }

                            int farEndIndex = futureSections[foundSection][1];
                            int lastMerged = foundSection;

                            //check if this section is merged with another one, to prevent duplicate merging.
                            //this section is the first of a possible merge.
                            for (int k = 0; k < futureSectionMerges.Count; k++)
                            {
                                int[] merge = futureSectionMerges[k];
                                if (merge[0] == foundSection)
                                {
                                    lastMerged = merge[merge.Length - 1];
                                    farEndIndex = futureSections[lastMerged][1];
                                }
                            }

                            nearEndDone = false;
                            nearExtDone = false;

                            if (future.path[farEndIndex][0] == size && future.path[farEndIndex][1] == size)
                            {
                                farExtDone = true;
                                farEndDone = true;
                            }
                            else
                            {
                                farExtDone = false;
                                farEndDone = false;
                            }

                            //start extension from near end, since the far end already has multiple choice
                            if (!ExtendFutureLine(true, futureSections[foundSection][0], farEndIndex, foundSection, lastMerged, false))
                            {
                                possibleDirections.Add(new int[] { });
                                T("Left/right future start line cannot be completed.");
                                M("Left/right future start line cannot be completed.", 2);
                                return false;
                            }
                        }
                        //turn right, pattern goes upwards
                        int s0 = taken.sx;
                        int s1 = taken.sy;
                        taken.sx = -lx;
                        taken.sy = -ly;
                        taken.lx = s0;
                        taken.ly = s1;
                    }
                    //mirror directions
                    taken.sx = taken.thisSx;
                    taken.sy = taken.thisSy;
                    taken.lx = -taken.thisLx;
                    taken.ly = -taken.thisLy;
                }
                taken.sx = taken.thisSx;
                taken.sy = taken.thisSy;
                taken.lx = taken.thisLx;
                taken.ly = taken.thisLy;

                // When there is a future start 2 to the left or right (due to the extension of the originally created C shape), and the live end goes straight or the other way (example: 0430_1), the start end can be extended. It will in some cases act as 1x3 C shape checking 
                // The future line is not necessarily the newest. Example: 0427, 0427_1

                if (size >= 7)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            int lx = taken.lx;
                            int ly = taken.ly;
                            int sx = taken.sx;
                            int sy = taken.sy;

                            //!InFutureRel2(1, 0) is needed for 0927_2
                            //last clause is needed for 0927_3
                            if (!InTakenRel2(1, 0) && !InFutureRel2(1, 0) && InFutureStartRel2(2, 0) && (InTakenRel2(1, -1) || InBorderRel2(1, -1) || (InFutureRel2(1, -1) && (!InFutureEndRel2(1, -1) || InFutureEndRel2(1, -1) && foundSectionStart == foundSectionEnd))))
                            {
                                // Similar to the Left/right future start, here is an example where the two connect: 0903_3
                                // But the previous function extends and connect to the section on the right side befure this function is called. Condition of !InFutureRel2(1, 1) is not needed in this example.
                                // If !InFutureRel2(1, 0) is omitted, this will go wrong: 0916_2, upper line will extend even though field below is filled with a future line.

                                T("Left/right to 2 future start valid at x " + x + " y " + y);

                                int addIndex = futureSections[foundSection][0] + 1;
                                future.path.Insert(addIndex, new int[] { x + lx, y + ly });
                                future.path.Insert(addIndex + 1, new int[] { x + lx + sx, y + ly + sy });
                                futureIndex.Insert(addIndex, count - 1);
                                futureIndex.Insert(addIndex + 1, count - 1);
                                futureActive.Insert(addIndex, true);
                                futureActive.Insert(addIndex + 1, true);
                                futureSections[foundSection][0] += 2;
                                IncreaseFurtherSections(foundSection);
                                IncreaseFurtherSections(foundSection);

                                int farEndIndex = futureSections[foundSection][1];
                                int lastMerged = foundSection;

                                //check if this section is merged with another one, to prevent duplicate merging.
                                //this section is the first of a possible merge.
                                for (int k = 0; k < futureSectionMerges.Count; k++) // 0916_1 happens if it is not checked
                                {
                                    int[] merge = futureSectionMerges[k];
                                    if (merge[0] == foundSection)
                                    {
                                        lastMerged = merge[merge.Length - 1];
                                        farEndIndex = futureSections[lastMerged][1];
                                    }
                                }

                                nearEndDone = false;
                                nearExtDone = false;

                                if (future.path[farEndIndex][0] == size && future.path[farEndIndex][1] == size)
                                {
                                    farExtDone = true;
                                    farEndDone = true;
                                }
                                else
                                {
                                    farExtDone = false;
                                    farEndDone = false;
                                }

                                if (!ExtendFutureLine(true, futureSections[foundSection][0], farEndIndex, foundSection, lastMerged, false))
                                {
                                    possibleDirections.Add(new int[] { });
                                    T("Left/right to 2 future start line cannot be completed.");
                                    M("Left/right to 2 future start line cannot be completed.", 2);
                                    return false;
                                }
                            }
                            //turn right, pattern goes upwards
                            int s0 = taken.sx;
                            int s1 = taken.sy;
                            taken.sx = -lx;
                            taken.sy = -ly;
                            taken.lx = s0;
                            taken.ly = s1;
                        }
                        //mirror directions
                        taken.sx = taken.thisSx;
                        taken.sy = taken.thisSy;
                        taken.lx = -taken.thisLx;
                        taken.ly = -taken.thisLy;
                    }
                    taken.sx = taken.thisSx;
                    taken.sy = taken.thisSy;
                    taken.lx = taken.thisLx;
                    taken.ly = taken.thisLy;
                }
            }

            taken.x2 = x;
            taken.y2 = y;
            taken.sx = taken.x2 - taken.path[count - 3][0];
            taken.sy = taken.y2 - taken.path[count - 3][1];

            for (int i = 0; i < 4; i++)
            {
                if (directions[i][0] == taken.sx && directions[i][1] == taken.sy)
                {
                    int newIndex = (i == 3) ? 0 : i + 1;
                    taken.lx = directions[newIndex][0];
                    taken.ly = directions[newIndex][1];
                }
            }

            taken.thisSx = taken.sx;
            taken.thisSy = taken.sy;
            taken.thisLx = taken.lx;
            taken.thisLy = taken.ly;

            bool x2found = false;

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int lx = taken.lx;
                    int ly = taken.ly;
                    int sx = taken.sx;
                    int sy = taken.sy;

                    // at the lower right corner future line shouldn't be drawn
                    // !InFutureRel2(1, 0) is to exclude a previously drawn 1x3 future line if we afterwards turned towards the middle

                    if (!InTakenRel2(1, 0) && !InTakenRel2(2, 0) &&
                        (InTakenRel2(1, -1) || InBorderRel2(1, -1)) && (InTakenRel2(2, -1) || InBorderRel2(2, -1)) && (InTakenRel2(3, 0) || InBorderRel2(3, 0))
                    && !InFutureRel2(1, 0) && !InCornerRel2(2, 0) && !InCornerRel2(2, 1))
                    {
                        x2found = true;
                        T("1x2 future valid at x " + x + " y " + y);

                        int addCount = 2;

                        if (!InFutureRel2(2, 1)) //This is not added in 0803:
                        {
                            future.path.Add(new int[] { x + 2 * lx + sx, y + 2 * ly + sy });
                            addCount++;
                        }
                        future.path.Add(new int[] { x + 2 * lx, y + 2 * ly });
                        future.path.Add(new int[] { x + lx, y + ly });
                        if (!InFutureRel2(1, 1)) //0919_4
                        {
                            future.path.Add(new int[] { x + lx + sx, y + ly + sy });
                            addCount++;
                        }

                        for (int k = 0; k < addCount; k++)
                        {
                            futureIndex.Add(count - 1);
                            futureActive.Add(true);
                        }
                        futureSections.Add(new int[] { future.path.Count - 1, future.path.Count - addCount });
                        int selectedExtSection = futureSections.Count - 1;

                        if (!(x + 2 * lx + sx == size && y + 2 * ly + sy == size)) //far end
                        {
                            future.path2 = taken.path;

                            // check section merge after finding an example

                            nearExtDone = false;
                            farExtDone = false;
                            nearEndDone = false;
                            farEndDone = false;

                            if (!ExtendFutureLine(false, future.path.Count - 1, future.path.Count - addCount, selectedExtSection, selectedExtSection, false))
                            {
                                //when making a c shape 3 distance across from the border in the corner, it cannot be completed
                                possibleDirections.Add(new int[] { });
                                T("1x2 future line cannot be completed.");
                                M("1x2 future line cannot be completed.", 2);
                                return false;
                            }
                        }
                    }
                    //turn right, pattern goes upwards
                    int s0 = taken.sx;
                    int s1 = taken.sy;
                    taken.sx = -lx;
                    taken.sy = -ly;
                    taken.lx = s0;
                    taken.ly = s1;
                }
                //mirror directions
                taken.sx = taken.thisSx;
                taken.sy = taken.thisSy;
                taken.lx = -taken.thisLx;
                taken.ly = -taken.thisLy;
            }
            taken.sx = taken.thisSx;
            taken.sy = taken.thisSy;
            taken.lx = taken.thisLx;
            taken.ly = taken.thisLy;

            if (!x2found)
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int sx = taken.sx;
                        int sy = taken.sy;
                        int lx = taken.lx;
                        int ly = taken.ly;

                        //InFutureRel2(2, 0) is to prevent a duplicate line as in 0927_1
                        if (!InTakenRel2(1, 0) && !InTakenRel2(2, 0) && !InTakenRel2(3, 0) &&
                            (InTakenRel2(1, -1) || InBorderRel2(1, -1)) && (InTakenRel2(2, -1) || InBorderRel2(2, -1)) && (InTakenRel2(3, -1) || InBorderRel2(3, -1)) && (InTakenRel2(4, 0) || InBorderRel2(4, 0)) && !InFutureRel2(2, 0) && !InCornerRel2(3, 0) && !InCornerRel2(3, 1))
                        {
                            //if the x + 4 * lx field was in border, we can extend the future lines after this.
                            T("1x3 future valid at x " + x + " y " + y);

                            int addCount = 3;

                            if (!InFutureRel2(3, 1)) // 1006_2
                            {
                                future.path.Add(new int[] { x + 3 * lx + sx, y + 3 * ly + sy });
                                addCount++;
                            }
                            future.path.Add(new int[] { x + 3 * lx, y + 3 * ly });
                            future.path.Add(new int[] { x + 2 * lx, y + 2 * ly });
                            future.path.Add(new int[] { x + lx, y + ly });
                            if (!InFutureRel2(1, 1)) // 1006_1
                            {
                                future.path.Add(new int[] { x + lx + sx, y + ly + sy });
                                addCount++;
                            }
                            for (int k = 0; k < addCount; k++)
                            {
                                futureIndex.Add(count - 1);
                                futureActive.Add(true);
                            }
                            futureSections.Add(new int[] { future.path.Count - 1, future.path.Count - addCount });
                            int selectedExtSection = futureSections.Count - 1;

                            future.path2 = taken.path;

                            // check section merge after finding an example

                            nearExtDone = false;
                            farExtDone = false;
                            nearEndDone = false;
                            farEndDone = false;

                            if (!ExtendFutureLine(false, future.path.Count - 1, future.path.Count - addCount, selectedExtSection, selectedExtSection, false))
                            {
                                //when making a c shape 3 distance across from the border in the corner, it cannot be completed
                                possibleDirections.Add(new int[] { });
                                T("1x3 future line cannot be completed.");
                                M("1x3 future line cannot be completed.", 2);
                                return false;

                            }
                        }
                        //turn right, pattern goes upwards
                        int s0 = taken.sx;
                        int s1 = taken.sy;
                        taken.sx = -lx;
                        taken.sy = -ly;
                        taken.lx = s0;
                        taken.ly = s1;
                    }
                    //mirror directions
                    taken.sx = taken.thisSx;
                    taken.sy = taken.thisSy;
                    taken.lx = -taken.thisLx;
                    taken.ly = -taken.thisLy;
                }
                taken.sx = taken.thisSx;
                taken.sy = taken.thisSy;
                taken.lx = taken.thisLx;
                taken.ly = taken.thisLy;
            }


            if (size >= 23) // 11
            {
                //check 3x1 for the original C shape created. If the live end went beyond, a field needs to be added to future
                //if 6, 2 is not taken, the near end can step 2 straight, 2 right, 1 right, 1 right to begin the other C-Shape.
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int sx = taken.sx;
                        int sy = taken.sy;
                        int lx = taken.lx;
                        int ly = taken.ly;

                        if (!(taken.x == x + lx && taken.y == y + ly) && InTakenRel2(1, -1) && InTakenRel2(2, -2) && InTakenRel2(3, -2) && InTakenRel2(4, -2) && InFutureStartRel2(5, -1)
                            && !InTakenRel2(2, -1) && !InTakenRel2(3, -1) && !InTakenRel2(4, -1) && InTakenRel2(6, 2))
                        {
                            T("1x3 reverse future valid at x " + x + " y " + y + " foundSection " + foundSection);

                            int addIndex = futureSections[foundSection][0] + 1;
                            insertIndex = futureSections[foundSection][1];
                            future.path.Insert(addIndex, new int[] { x - sx + 4 * lx, y - sy + 4 * ly });
                            future.path.Insert(insertIndex, new int[] { x - sx + 6 * lx, y - sy + 6 * ly });
                            futureIndex.Insert(addIndex, count - 1);
                            futureActive.Insert(addIndex, true);
                            futureIndex.Insert(insertIndex, count - 1);
                            futureActive.Insert(insertIndex, true);
                            futureSections[foundSection][0] += 2;
                            IncreaseFurtherSections(foundSection);
                            IncreaseFurtherSections(foundSection);

                            nearExtDone = false;
                            farExtDone = false;
                            nearEndDone = false;
                            farEndDone = false;

                            if (!ExtendFutureLine(false, addIndex + 1, insertIndex, foundSection, foundSection, false))
                            {
                                possibleDirections.Add(new int[] { });
                                M("1x3 reverse future line cannot be completed.", 2);
                                return false;
                            }

                            /*See 0701_1:
	This situation is not possible. When the start of the left future section and the end of the right future section gets connected, it will go through the field in the middle, 11x11, because it is next to the main line. From there we can extend this mid section to either both sides or one side and up.
	In the first case, the near end will be 12x11, 12x10 and 13x10, the far end the same mirrored. There will be a C shape, 11x10 cannot be filled.
	In the second case, the near end being the same as above, the far end can be 11x10. The start of the left future line will extend to 10x11 and 10x10. 9x10 cannot be filled. The second case can be mirrored, so that 13x10 is the field that cannot be filled.*/

                            if (foundSection > 0)
                            {
                                int[] section1Start = future.path[futureSections[foundSection][0]];
                                int[] section1End = future.path[futureSections[foundSection][1]];
                                int[] section0Start = future.path[futureSections[foundSection - 1][0]];
                                int[] section0End = future.path[futureSections[foundSection - 1][1]];

                                T(section1Start[0] + " " + section1Start[1]);

                                // When the ends of the future lines make up a 4x2 rectangle
                                // Examine case where the main line does not go straight between the two future sections!
                                if ((section1Start[0] == section1End[0] && section0Start[0] == section0End[0] && section1Start[1] == section0End[1] && section1End[1] == section0Start[1] &&
                                    Math.Abs(section1Start[1] - section1End[1]) == 2 && Math.Abs(section1End[0] - section0Start[0]) == 4) ||
                                    (section1Start[1] == section1End[1] && section0Start[1] == section0End[1] && section1Start[0] == section0End[0] && section1End[0] == section0Start[0] &&
                                    Math.Abs(section1Start[0] - section1End[0]) == 2 && Math.Abs(section1End[1] - section0Start[1]) == 4))
                                {
                                    possibleDirections.Add(new int[] { });
                                    T("1x3 reverse future line makes a 4x2 rectangle");
                                    M("1x3 reverse future line makes a 4x2 rectangle", 2);
                                    return false;
                                }
                            }
                        }
                        //turn right, pattern goes upwards
                        int s0 = taken.sx;
                        int s1 = taken.sy;
                        taken.sx = -lx;
                        taken.sy = -ly;
                        taken.lx = s0;
                        taken.ly = s1;
                    }
                    //mirror directions
                    taken.sx = taken.thisSx;
                    taken.sy = taken.thisSy;
                    taken.lx = -taken.thisLx;
                    taken.ly = -taken.thisLy;
                }
                taken.sx = taken.thisSx;
                taken.sy = taken.thisSy;
                taken.lx = taken.thisLx;
                taken.ly = taken.thisLy;

                /*
                // what is the minimum size for this?

                taken.path2 = future.path;

				sx = thisS0;
				sy = thisS1;
				lx = thisL0;
				ly = thisL1;
				rx = -lx;
				ry = -ly;

				int index = futureLoop.IndexOf(count - 2);
				if (index != -1)
				{
					int loopSelectedSection = futureLoopSelectedSection[index];
					int startIndex = futureSections[loopSelectedSection][0];
					int futureStartX = future.path[startIndex][0];
					int futureStartY = future.path[startIndex][1];

					T("Closed loop, loopSelectedSection: " + loopSelectedSection + " " + x + " " + y + " " + futureStartX + " " + futureStartY);

					if (x == futureStartX && Math.Abs(y - futureStartY) == 1 || y == futureStartY && Math.Abs(x - futureStartX) == 1)
					{
						futureLoop.Add(count - 1);
						futureLoopSelectedSection.Add(loopSelectedSection);
						// extend near end once if near end is next to the live end
						
						int selectedExtSection = loopSelectedSection;

                        nearExtDone = false;
                        farExtDone = false;
                        nearEndDone = false;
						farEndDone = false;

                        ExtendFutureLine(true, startIndex, -1, selectedExtSection, selectedExtSection, true);
					}
					else
					{
						T("Turning off closed loop");
					}
				}
				else if (InFutureStart(x + sx, y + sy)) //foundSection is set here
				{
					//how to decide when we are entering a loop?
					//implement right side

					int startIndex = futureSections[foundSection][0];
					int endIndex = futureSections[foundSection][1];

					T("");
					T("Taken connecting to future, foundSection " + foundSection);

					//in order to have a loop, left and right fields should be empty, also at 1 step forward. Suppose the latter is true, since we are connecting to the start of a future line. Counter-example?
					if (!InTaken(x + lx, y + ly) && !InFutureRel(x + lx, y + ly) && !InTaken(x + rx, y + ry) && !InFutureRel(x + rx, y + ry))
					{
						startIndex++;

						//Suppose the end of the future line is 2 to the left or right of the start end, as in 0415
						if (future.path[endIndex][0] == x + 2 * rx + sx && future.path[endIndex][1] == y + 2 * ry + sy)
						{
							future.path.Insert(startIndex, new int[] { x + sx + lx, y + sy + ly });
						}
						else
						{
							future.path.Insert(startIndex, new int[] { x + lx + sx, y + ly + sy });
						}
						futureIndex.Insert(startIndex, count - 1);
						futureActive.Insert(startIndex, true);
						futureSections[foundSection][0] += 1;
						IncreaseFurtherSections(foundSection);

                        nearExtDone = false;
                        farExtDone = false;
                        nearEndDone = false;
						farEndDone = false;

                        //only extend the far end. The near end has to be extended simultaneously with the live end.
                        if (!ExtendFutureLine(false, -1, endIndex, foundSection, foundSection, true))
						{
							possibleDirections.Add(new int[] { });
                            T("Closing loop, future line cannot be completed.");
                            M("Closing loop, future line cannot be completed.", 2);
							return false;
						}
						else
						{
							T("Creating loop");
							futureLoop.Add(count - 1);
							futureLoopSelectedSection.Add(foundSection);
						}
					}
                }*/
            }
            return true;
        }

        private bool ExtendFutureLine(bool isNearEnd, int nearEndIndex, int farEndIndex, int nearSection, int farSection, bool once)
        {
            int stepCount = 0;

            do
            {
                int index = (isNearEnd) ? nearEndIndex : farEndIndex;
                T("ExtendFuture isNearEnd " + isNearEnd + " index " + index + " nearEndIndex " + nearEndIndex + " farEndIndex " + farEndIndex + " nearSection " + nearSection + " farSection " + farSection);

                // In 0814, future line is already extended to the end
                if (!isNearEnd && future.path[index][0] == size && future.path[index][1] == size)
                {
                    farExtDone = true;
                    farEndDone = true;
                    if (nearExtDone) return true;
                    break;
                }
                // In 0919_4, the newly created 1x2 line extends to the corner and then connects to the line on the left that is already stepped on.
                if (isNearEnd && InTaken(future.path[index][0], future.path[index][1]))
                {
                    nearExtDone = true;
                    nearEndDone = true;
                    if (farExtDone) return true;
                    break;
                }

                future.NextStepPossibilities(isNearEnd, index, nearSection, farSection);

                T("ExtendFutureLine, future.possible.Count " + future.possible.Count);

                // 0811_1: When the near end of a future line can only connect to the main line, the far end, when presented with the possibility of entering the corner and another field, it has to choose the other field.
                // A future line on the edge cannot have 3 possibilities
                if (nearEndDone && future.possible.Count == 2)
                {
                    for (int i = 0; i < future.possible.Count; i++)
                    {
                        int[] field = future.possible[i];
                        if (field[0] == size && field[1] == size)
                        {
                            T("Near end connected to main line, removing corner from possibilities");
                            future.possible.RemoveAt(i);
                            break;
                        }
                    }
                }

                // 0911: If the far end has reached the corner, the near end has to choose the other option besides connecting to the live end.
                if (farEndDone && future.possible.Count == 2)
                {
                    for (int i = 0; i < future.possible.Count; i++)
                    {
                        int[] field = future.possible[i];
                        if (field[0] == taken.x && field[1] == taken.y)
                        {
                            T("Far end reached the corner, removing live end from possibilities");
                            future.possible.RemoveAt(i);
                            break;
                        }
                    }
                }

                if (future.possible.Count == 1)
                {
                    int[] newField = future.possible[0];

                    // if the only possibility is to connect to the main line, we continue the extension at the far end.
                    if (newField[0] == taken.x && newField[1] == taken.y)
                    {
                        T("Near end done");
                        if (farEndDone) return true;
                        nearExtDone = true;
                        nearEndDone = true;
                        farExtDone = false;
                        break;
                    }

                    future.x = newField[0];
                    future.y = newField[1];

                    //is counting area needed?					

                    // The far end might be connecting to an older section now, but we cannot merge the sections, because there might be a future line in between, like in 0714_2. Instead, we mark the connection.                      
                    // Not only far end can connect to the near end of an older section, but also near end to the far end of the older section, as i n 0730_1 

                    // If, after a merge, the line connected to extends, but it ends already in the corner, is it okay to just return, or should we try to extend the near end? Find an example.
                    for (int i = 0; i < futureSections.Count; i++)
                    {
                        int[] field = isNearEnd ? future.path[futureSections[i][1]] : future.path[futureSections[i][0]];
                        int fx = field[0];
                        int fy = field[1];
                        if (future.x == fx && future.y == fy)
                        {
                            T("Connecting to section " + i);

                            if (isNearEnd) // extend near end. Example: 0911_2. A 2x2 line is created on the right side, the far end extends to the corner, and then the near end extends and want to connect.
                            {
                                if (nearSection == farSection) // single line connects to a merged or another single line
                                {
                                    bool foundInMerge = false;
                                    for (int j = 0; j < futureSectionMerges.Count; j++)
                                    {
                                        int[] merge = futureSectionMerges[j];
                                        int[] farthestSection = futureSections[merge[merge.Length - 1]];
                                        if (futureSections[i] == farthestSection)
                                        {
                                            T("Near end: single line connects to merged");
                                            List<int> listMerge = merge.ToList();
                                            listMerge.Add(nearSection);
                                            futureSectionMerges[j] = listMerge.ToArray();
                                            futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });
                                            foundInMerge = true;

                                            nearSection = listMerge[0];
                                            nearEndIndex = futureSections[nearSection][0];
                                            if (!(future.path[nearEndIndex][0] == taken.x && future.path[nearEndIndex][1] == taken.y))
                                            {
                                                return ExtendFutureLine(isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                            }
                                            else
                                            {
                                                return ExtendFutureLine(!isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                            }
                                        }
                                    }

                                    if (!foundInMerge)
                                    {
                                        T("Near end: single line connects to single");
                                        futureSectionMerges.Add(new int[] { i, nearSection });
                                        futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });

                                        nearSection = i;
                                        nearEndIndex = futureSections[i][0];
                                        if (!(future.path[nearEndIndex][0] == taken.x && future.path[farEndIndex][1] == taken.y))
                                        {
                                            return ExtendFutureLine(isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                        }
                                        else // extend far end after the connection, the farExtDone may have been canceled, as in 0919_3
                                        {
                                            return ExtendFutureLine(!isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                        }
                                    }
                                }
                                else // merged line connects to a merged or a single line
                                {
                                    for (int j = 0; j < futureSectionMerges.Count; j++)
                                    {
                                        int[] origMerge = futureSectionMerges[j];
                                        List<int> origListMerge = origMerge.ToList();

                                        if (origMerge[0] == nearSection)
                                        {
                                            bool foundInMerge = false;
                                            for (int k = 0; k < futureSectionMerges.Count; k++)
                                            {
                                                int[] merge = futureSectionMerges[k];
                                                int[] farthestSection = futureSections[merge[merge.Length - 1]];
                                                if (futureSections[i] == farthestSection)
                                                {
                                                    T("Near end: merged line connects to merged");
                                                    List<int> listMerge = merge.ToList();
                                                    listMerge.AddRange(origListMerge);
                                                    futureSectionMerges[k] = listMerge.ToArray();
                                                    futureSectionMerges.RemoveAt(j);
                                                    futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });
                                                    foundInMerge = true;

                                                    nearSection = listMerge[0];
                                                    nearEndIndex = futureSections[nearSection][0];
                                                    if (!(future.path[nearEndIndex][0] == taken.x && future.path[nearEndIndex][1] == taken.y))
                                                    {
                                                        return ExtendFutureLine(isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                    }
                                                    else
                                                    {
                                                        return ExtendFutureLine(!isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                    }
                                                }
                                            }

                                            if (!foundInMerge)
                                            {
                                                T("Near end: merged line connects to single");
                                                origListMerge.Insert(0, i);
                                                futureSectionMerges[j] = origListMerge.ToArray();
                                                futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });

                                                nearSection = i;
                                                nearEndIndex = futureSections[i][0];
                                                if (!(future.path[nearEndIndex][0] == taken.x && future.path[nearEndIndex][1] == taken.y))
                                                {
                                                    return ExtendFutureLine(isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                }
                                                else
                                                {
                                                    return ExtendFutureLine(!isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else // extend far end
                            {
                                if (nearSection == farSection) // single line connects to a merged or another single line
                                {
                                    bool foundInMerge = false;
                                    for (int j = 0; j < futureSectionMerges.Count; j++)
                                    {
                                        int[] merge = futureSectionMerges[j];
                                        int[] nearestSection = futureSections[merge[0]];
                                        if (futureSections[i] == nearestSection)
                                        {
                                            T("Far end: single line connects to merged");
                                            List<int> listMerge = merge.ToList();
                                            listMerge.Insert(0, farSection);
                                            futureSectionMerges[j] = listMerge.ToArray();
                                            futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });
                                            foundInMerge = true;

                                            farSection = listMerge[listMerge.Count - 1];
                                            farEndIndex = futureSections[farSection][1];
                                            if (!(future.path[farEndIndex][0] == size && future.path[farEndIndex][1] == size))
                                            {
                                                return ExtendFutureLine(isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                            }
                                            else
                                            {
                                                return ExtendFutureLine(!isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                            }
                                        }
                                    }

                                    if (!foundInMerge)
                                    {
                                        T("Far end: single line connects to single");
                                        futureSectionMerges.Add(new int[] { farSection, i });
                                        futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });

                                        farSection = i;
                                        farEndIndex = futureSections[i][1];
                                        if (!(future.path[farEndIndex][0] == size && future.path[farEndIndex][1] == size))
                                        {
                                            return ExtendFutureLine(isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                        }
                                        else
                                        {
                                            return ExtendFutureLine(!isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                        }
                                    }
                                }
                                else // merged line connects to a merged or a single line
                                {
                                    for (int j = 0; j < futureSectionMerges.Count; j++)
                                    {
                                        int[] origMerge = futureSectionMerges[j];
                                        List<int> origListMerge = origMerge.ToList();

                                        if (origMerge[origMerge.Length - 1] == farSection)
                                        {
                                            bool foundInMerge = false;
                                            for (int k = 0; k < futureSectionMerges.Count; k++)
                                            {
                                                int[] merge = futureSectionMerges[k];
                                                int[] nearestSection = futureSections[merge[0]];
                                                if (futureSections[i] == nearestSection)
                                                {
                                                    T("Far end: merged line connects to merged");
                                                    List<int> listMerge = merge.ToList();
                                                    origListMerge.AddRange(listMerge);
                                                    futureSectionMerges[j] = origListMerge.ToArray();
                                                    futureSectionMerges.RemoveAt(k);
                                                    futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });
                                                    foundInMerge = true;

                                                    farSection = origListMerge[origListMerge.Count - 1];
                                                    farEndIndex = futureSections[farSection][1];
                                                    if (!(future.path[farEndIndex][0] == size && future.path[farEndIndex][1] == size))
                                                    {
                                                        return ExtendFutureLine(isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                    }
                                                    else
                                                    {
                                                        return ExtendFutureLine(!isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                    }
                                                }
                                            }

                                            if (!foundInMerge)
                                            {
                                                T("Far end: merged line connects to single");
                                                origListMerge.Add(i);
                                                futureSectionMerges[j] = origListMerge.ToArray();
                                                futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });

                                                farSection = i;
                                                farEndIndex = futureSections[i][1];
                                                if (!(future.path[farEndIndex][0] == size && future.path[farEndIndex][1] == size))
                                                {
                                                    return ExtendFutureLine(isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                }
                                                else
                                                {
                                                    return ExtendFutureLine(!isNearEnd, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    T("Adding future: " + future.x + " " + future.y);
                    /*foreach (int[] section in futureSections)
					{
						T("--- Section: " + section[0] + " " + section[1]);
					}*/

                    // increase further future line sections (we have stepped on them already or, in case of connecting to a loop, they are still active)
                    // if the far end of merged sections is extended, nearEndIndex will only increase if the starting section is newer.

                    if (isNearEnd)
                    {
                        nearEndIndex++;
                        if (farSection > nearSection) farEndIndex++;
                        futureSections[nearSection][0]++;
                        IncreaseFurtherSections(nearSection);

                        //T("Inserting nearEndIndex: " + nearEndIndex);
                        future.path.Insert(nearEndIndex, new int[] { future.x, future.y });
                        futureIndex.Insert(nearEndIndex, taken.path.Count - 1);
                        futureActive.Insert(nearEndIndex, true);
                    }
                    else
                    {
                        if (nearSection >= farSection) nearEndIndex++;
                        futureSections[farSection][0]++;
                        IncreaseFurtherSections(farSection);

                        //T("Inserting farEndIndex: " + farEndIndex);
                        future.path.Insert(farEndIndex, new int[] { future.x, future.y });
                        futureIndex.Insert(farEndIndex, taken.path.Count - 1);
                        futureActive.Insert(farEndIndex, true);
                    }
                    T("nearEndIndex: " + nearEndIndex);
                    stepCount++;

                    if (future.x == size && future.y == size)
                    {
                        T("Far end done");
                        if (nearEndDone) return true; //only return if the near end can only connect to the main line. Otherwise extend the near end again, it might connect to another section now as in 0919_2
                        farExtDone = true;
                        farEndDone = true;
                        nearExtDone = false;
                        break;
                    }

                }
            } while (future.possible.Count == 1);

            if (future.possible.Count == 0)
            {
                T("Possible count: 0");
                // in 0811_4 it can happen that after stepping on the future line, the other line gets extended and merges into the line being stepped on.
                if (!(isNearEnd && future.path[nearEndIndex][0] == taken.x && future.path[nearEndIndex][1] == taken.y))
                {
                    return false;
                }
            }

            if (future.possible.Count > 1)
            {
                T("Future path has multiple choice, stepCount " + stepCount);
                foreach (int[] field in future.possible)
                {
                    T(field[0] + " " + field[1]);
                }
            }
            if (nearEndDone)
            {
                T("Future path can only connect to the main line, stepCount " + stepCount);
            }
            if (farEndDone)
            {
                T("Future path reached the end, stepCount " + stepCount);
            }

            // only return if no steps were taken on either end. It is not enough to check if the possibilities of an end were taken by the other end, a C-shape can be created as in 1002 where the near and the far end are next to each other at one point.
            if (stepCount == 0)
            {
                if (isNearEnd)
                {
                    nearExtDone = true;
                    if (farExtDone) return true;
                }
                else
                {
                    farExtDone = true;
                    if (nearExtDone) return true;
                }
            }
            else if (isNearEnd)
            {
                if (farEndDone) return true;
                if (!nearEndDone) nearExtDone = false;
                farExtDone = false;
            }
            else
            {
                if (nearEndDone) return true;
                if (!farEndDone) farExtDone = false;
                nearExtDone = false;
            }

            if (!once)
            {
                if (isNearEnd)
                {
                    T("Start from far end");
                    return ExtendFutureLine(false, nearEndIndex, farEndIndex, nearSection, farSection, false);
                }
                else
                {
                    T("Start from near end");
                    return ExtendFutureLine(true, nearEndIndex, farEndIndex, nearSection, farSection, false);
                }
            }
            return true;

        }

        private void NextStepPossibilities()
        {
            T("NextStepPossibilities main, inFuture: " + inFuture + " inFutureIndex: " + inFutureIndex + " errorInWalkthrough " + errorInWalkthrough);
            if (inFuture)
            {
                int[] futureField = future.path[inFutureIndex];
                T("Possible: " + futureField[0] + " " + futureField[1]);
                taken.possible = new List<int[]> { new int[] { futureField[0], futureField[1] } };
            }
            else
            {
                taken.path2 = future.path;
                taken.NextStepPossibilities(true, -1, -1, -1);
            }

            if (!isTaskRunning) PossibleCoords.Text = "";

            List<int> possibleFields = new List<int>();
            List<int[]> newPossible = new List<int[]>();

            if (errorInWalkthrough) // countarea errors
            {
                T("isTaskRunning " + isTaskRunning + " makeStats " + makeStats + " keepLeftCheck " + keepLeftCheck);
                if (!(isTaskRunning && makeStats && !keepLeftCheck))
                {
                    possibleDirections.Add(possibleFields.ToArray());
                    Dispatcher.Invoke(() =>
                    {
                        T("Nextstepposs stopping timer");
                        StopAll((string)MessageLine.Content);
                    });
                }
                return;
            }

            foreach (int[] field in taken.possible)
            {
                int fx = field[0];
                int fy = field[1];

                if (inFuture)
                {
                    T("NextStepPossibilities InFuture fx " + fx + " fy " + fy + " count: " + future.path.Count);
                    if (!isTaskRunning) PossibleCoords.Text += fx + " " + fy + "\n";
                    newPossible.Add(field);

                    for (int i = 0; i < 4; i++)
                    {
                        //last movement: down, right, up, left
                        int dx = directions[i][0];
                        int dy = directions[i][1];

                        if (fx - taken.x == dx && fy - taken.y == dy)
                        {
                            possibleFields.Add(i);
                        }
                    }
                }
                else
                {
                    int futureFieldIndex = future.InTakenIndex(fx, fy);

                    //T("NextStepPossibilities not inFuture, futureFieldIndex: " + futureFieldIndex + " fx " + fx + " fy " + fy);
                    //can only step on an empty field or the near end of a visible future line (newer lines might have already been stepped on)
                    //connecting to a future line that is not the last should be prevented in AddFutureLines. It makes a loop. There might be a newer future line on the other side, in which case the one we are connecting to is not the last.
                    bool found = false;
                    foreach (int[] section in futureSections)
                    {
                        if (futureFieldIndex == section[0])
                        {
                            found = true;
                        }
                    }

                    if (futureFieldIndex == -1 || found == true)
                    {
                        if (!isTaskRunning) PossibleCoords.Text += fx + " " + fy + "\n";
                        newPossible.Add(field);

                        for (int i = 0; i < 4; i++)
                        {
                            //last movement: down, right, up, left
                            int dx = directions[i][0];
                            int dy = directions[i][1];

                            if (fx - taken.x == dx && fy - taken.y == dy)
                            {
                                possibleFields.Add(i);
                            }
                        }
                    }
                }
            }

            taken.possible = newPossible;
            possibleDirections.Add(possibleFields.ToArray()); //array containing possible fields for all steps

            if (taken.possible.Count == 0)
            {
                errorInWalkthrough = true;

                if (!(isTaskRunning && makeStats && !keepLeftCheck))
                {
                    StopAll("No option to move.");
                }
                else
                {
                    L("No option to move.");
                }
            }

            //Stop at pattern here

            /*if (!taken.countAreaImpair && taken.FutureL)
            {
                M("Future L: " + completedCount + " walkthroughs are completed.", 3);
                if (isTaskRunning)
                {
                    Dispatcher.Invoke(() =>
                    {
                        DrawPath();
                    });
                }
            }*/
        }

        private void PreviousStep(bool stepBack)
		{
			
			int count = taken.path.Count;
			if (count < 2) return;

			areaBackground = "";
			taken.minAreaLine = new();


            int removeX = taken.path[count - 1][0];
			int removeY = taken.path[count - 1][1];

			if (stepBack) //actual step back
			{
				errorInWalkthrough = false;
				lineFinished = false;
				taken.x = taken.path[count - 2][0];
				taken.y = taken.path[count - 2][1];
				int lastExit = exitIndex.Count - 1;

				if (lastExit >= 0)
				{
					T("Previous Step " + exitIndex[lastExit] + " " + count);
					if (exitIndex[lastExit] == count - 1)
					{
						T("Removing exit");
						exits.RemoveAt(lastExit);
						exitIndex.RemoveAt(lastExit);
					}
				}

				taken.path.RemoveAt(count - 1);
				//T("Actual step back, possibleDirections.Count " + possibleDirections.Count + " " + taken.path.Count + " " + possibleDirections[possibleDirections.Count - 1].Length);
				possibleDirections.RemoveAt(count);
				int index = futureLoop.IndexOf(count - 1);
				if (index != -1)
				{
					futureLoop.RemoveAt(index);
					futureLoopSelectedSection.RemoveAt(index);
				}

				T("previousStep not forbidden removed taken and possibleDirs inFuture: " + inFuture);

				if (futureIndex.Count > 0) //the last element of futureIndex is not the highest if not the most recent future path was extended last time.
				{
                    RemoveAndActivateFutureAt(count - 1, removeX, removeY);
				}

				taken.possible = new List<int[]>();
                if (!isTaskRunning) PossibleCoords.Text = "";
				List<int> dirs = possibleDirections[possibleDirections.Count - 1].ToList<int>();

				foreach(int dir in dirs)
				{
					int newX = taken.x + directions[dir][0];
					int newY = taken.y + directions[dir][1];

					taken.possible.Add(new int[] { newX, newY });
                    if (!isTaskRunning) PossibleCoords.Text += newX + " " + newY + "\n";
				}

                if (!isTaskRunning) CurrentCoords.Content = taken.x + " " + taken.y;				
			}
            else // Next click when there is no possibilities or Next/Previous/OK click after stepping on future line, and it cannot extend				
            {
				int lastX = taken.path[taken.path.Count - 1][0];
				int lastY = taken.path[taken.path.Count - 1][1];

                PossibleCoords.Text = "";
				List<int> dirs = possibleDirections[possibleDirections.Count - 1].ToList<int>();

				if (dirs.Count > 0) // when stepping on future line, and it cannot extend
				{
					T("PreviousStep, directions count: " + dirs.Count);
					List<int[]> newPossible = new List<int[]>();
					int foundIndex = -1;
					for (int i = 0; i < dirs.Count; i++)
					{
						int newX = lastX + directions[dirs[i]][0];
						int newY = lastY + directions[dirs[i]][1];

						if (taken.x == newX && taken.y == newY)
						{
							foundIndex = i;
						}
						else
						{
							newPossible.Add(new int[] { newX, newY });
                            if (!isTaskRunning) PossibleCoords.Text += newX + " " + newY + "\n";
						}
					}

					T("FoundIndex: " + foundIndex);

					dirs.RemoveAt(foundIndex);
					taken.possible = newPossible;
					taken.x = lastX;
					taken.y = lastY;
				}

				T("dirs.Count: " + dirs.Count + " " + possibleDirections.Count);
				TracePossible();

				if (dirs.Count == 0)
				{
					T("Directions count 0");

					int lastExit = exitIndex.Count - 1;
					if (lastExit >= 0 && exitIndex[lastExit] == taken.path.Count - 1)
					{
						exits.RemoveAt(lastExit);
						exitIndex.RemoveAt(lastExit);
					}

					taken.path.RemoveAt(taken.path.Count - 1);
					possibleDirections.RemoveAt(possibleDirections.Count - 1);
					int index = futureLoop.IndexOf(count - 1);
					if (index != -1)
					{
						futureLoop.RemoveAt(index);
						futureLoopSelectedSection.RemoveAt(index);
					}

					if (futureIndex.Count > 0)
					{
                        RemoveAndActivateFutureAt(count - 1, removeX, removeY);
					}

					PreviousStep(false);
				}
				else
				{
					possibleDirections[possibleDirections.Count - 1] = dirs.ToArray<int>();
                    errorInWalkthrough = false;
				}
			}
		}

		private void PreviousStepWithFuture()
		{
			int count = taken.path.Count;

			int removeX = taken.path[count - 1][0];
			int removeY = taken.path[count - 1][1];

			taken.path.RemoveAt(count - 1);
            RemoveAndActivateFutureAt(count - 1, removeX, removeY);
			
			//necessary when loading from file
			taken.x = removeX;
			taken.y = removeY;
			PreviousStep(false);
		}

        private void IncreaseFurtherSections(int section)
        {
            for (int i = section + 1; i < futureSections.Count; i++)
            {
                futureSections[i][0]++;
                futureSections[i][1]++;
            }
            // In 0811_4, other future line is extended after we stepped on one
            // inFutureIndex can also be within the current section when we step on one, so it just needs to be larger than its far end.
            if (inFutureIndex > futureSections[section][1])
            {
                inFutureIndex++;
            }
        }

        public void RemoveAndActivateFutureAt(int index, int removeX, int removeY)
		{
			if (futureSectionMergesHistory.Count > 0)
			{
				// There can be more than one future line merges in one step, like in 0913. Removing only the last one is not correct.
				for(int i = futureSectionMergesHistory.Count -1; i >= 0; i--)
				{
					if ((int)futureSectionMergesHistory[i][0] == index)
					{
                        futureSectionMergesHistory.RemoveAt(i);
                        if (i > 0)
                        {
                            futureSectionMerges = Copy((List<int[]>)futureSectionMergesHistory[i - 1][1]);
                        }
                        else
                        {
                            futureSectionMerges = new List<int[]>();
                        }
                    }
				}
            }                    

            for (int i = future.path.Count - 1; i >= 0 ; i--)
			{
				if (futureIndex[i] == index)
				{
					//T("Remove future with index " + i + " " + future.path[i][0] + " " + future.path[i][1]);
					future.path.RemoveAt(i);
					futureIndex.RemoveAt(i);
					futureActive.RemoveAt(i);

					for (int j = futureSections.Count - 1; j >= 0; j--)
					{
						int[] section = futureSections[j];
						//reduce all newer sections (the ones stepped on or being on the other side)
						if (i < section[1])
						{
							futureSections[j][0]--;
							futureSections[j][1]--;
						}
						//section consists of only one element, we delete the section 
						else if (i == section[0] && i == section[1])
						{
							futureSections.RemoveAt(j); //was last in section
						}
						//element is within the section
						else if (i <= section[0] && i >= section[1])
						{
							futureSections[j][0]--;
						}
					}
				}
			}

			for (int i = futureIndex.Count - 1; i >= 0; i--)
			{
				int[] field = future.path[i];
				int fx = field[0];
				int fy = field[1];

				if (fx == removeX && fy == removeY)
				{
					T("Activate at " + i + " " + removeX + " " + removeY + " inend " + InFutureSectionEnd(removeX, removeY));
					futureActive[i] = true;
					
					if (InFutureSectionStart(removeX, removeY))
					{
						inFuture = false;
						selectedSection = -1;
					}
					else
					{
						if (InFutureSectionEnd(removeX, removeY))
						{
							inFuture = true;
							inFutureIndex = foundEndIndex;

							for (int j = futureSections.Count - 1; j >= 0; j--)
							{
								int[] section = futureSections[j];
								if (inFutureIndex == section[1])
								{
									selectedSection = j;
									break;
								}
							}							
						}
						else
						{
							inFutureIndex++;
							T("----- Remove and activate inFutureIndex " + inFutureIndex);
						}						
					}
					return;
				}
			}	
		}


        // ----- Check functions -----


        public bool InBorderAbs(int[] field)
        {
            int x = field[0];
            int y = field[1];
            return InBorder(x, y);
        }

        public bool InBorderRel(int left, int straight)
        {
            int x = taken.x + left * taken.lx + straight * taken.sx;
            int y = taken.y + left * taken.ly + straight * taken.sy;
            return InBorder(x, y);
        }

        public bool InBorderRel2(int left, int straight)
        {
            int x = taken.x2 + left * taken.lx + straight * taken.sx;
            int y = taken.y2 + left * taken.ly + straight * taken.sy;
            return InBorder(x, y);
        }

        public bool InBorder(int x, int y) // allowing negative values could cause an error in AddFutureLines 2x2 checking
        {
            if (x == 0 || x == size + 1 || y == 0 || y == size + 1) return true;
            return false;
        }

        private bool InTakenRel(int left, int straight)
        {
			int x = taken.x + left * taken.lx + straight * taken.sx;
			int y = taken.y + left * taken.ly + straight * taken.sy;
			return InTaken(x, y);
		}

        private bool InTakenRel2(int left, int straight)
        {
            int x = taken.x2 + left * taken.lx + straight * taken.sx;
            int y = taken.y2 + left * taken.ly + straight * taken.sy;
            return InTaken(x, y);
        }

        private bool InTaken(int x, int y)
		{
            int c1 = taken.path.Count;
            for (int i = c1 - 1; i >= 0; i--)
            {
                int[] field = taken.path[i];
                if (x == field[0] && y == field[1])
                {
                    return true;
                }
            }

			return false;
        }

        public bool InCornerRel2(int left, int straight)
        {
            int x = taken.x2 + left * taken.lx + straight * taken.sx;
            int y = taken.y2 + left * taken.ly + straight * taken.sy;
            if (x == size && y == size) return true;
            return false;
        }

        public bool InFutureRel(int left, int straight)
        {
            int x = taken.x + left * taken.lx + straight * taken.sx;
            int y = taken.y + left * taken.ly + straight * taken.sy;

			//In 0913_2 it can happen that after stepping on the future line, the 1-thin rule is true if we don't check that the coordinates are within size.
			if (x > size || y > size) return true;

            return InFuture(x, y);
        }
		
		public bool InFutureRel2(int left, int straight)
        {
            int x = taken.x2 + left * taken.lx + straight * taken.sx;
            int y = taken.y2 + left * taken.ly + straight * taken.sy;

            return InFuture(x, y);
        }

        public bool InFuture(int x, int y)
		{
            int c = future.path.Count;
            if (c == 0) return false;

            for (int i = c - 1; i >= 0; i--)
            {
                int[] field = future.path[i];
                if (x == field[0] && y == field[1] && futureActive[i])
                {
                    foundIndex = i;
                    return true;
                }
            }
            return false;
        }

        public bool InFutureStartRel(int left, int straight)
        {
            int x = taken.x + left * taken.lx + straight * taken.sx;
            int y = taken.y + left * taken.ly + straight * taken.sy;

            return InFutureStart(x, y);
        }

        public bool InFutureStartRel2(int left, int straight)
		{
			int x = taken.x2 + left * taken.lx + straight * taken.sx;
			int y = taken.y2 + left * taken.ly + straight * taken.sy;

			return InFutureStart(x, y);
		}

        public bool InFutureStart(int x, int y)
        {
            int c = future.path.Count;
			if (c == 0) return false;

			int foundIndex = -1;

			int i;
			for (i = c - 1; i >= 0; i--)
			{
				int[] field = future.path[i];

				if (field[0] == x && field[1] == y && futureActive[i])
				{
					foundIndex = i;
				}
			}

			if (foundIndex == -1) return false;

			i = -1;
			foreach (int[] section in futureSections)
			{
				i++;
				if (section[0] == foundIndex)
				{
					foreach (int[] merge in futureSectionMerges)
					{
						// examine all but the first merged section
						for (int j = 1; j < merge.Length; j++)
						{
							if (merge[j] == i) return false;
						}
					}

					foundSection = i;
                    foundSectionStart = i;
                    return true;
				}
			}

			return false;
		}

        public bool InFutureSectionStart(int x, int y) // used by previousstep / activate future line. To get the correct infutureIndex, only the sections have to be examined, not the merged lines.
        {
            int c = future.path.Count;
            if (c == 0) return false;

            int foundIndex = -1;

            int i;
            for (i = c - 1; i >= 0; i--)
            {
                int[] field = future.path[i];

                if (field[0] == x && field[1] == y && futureActive[i])
                {
                    foundIndex = i;
                }
            }

            if (foundIndex == -1) return false;

            i = -1;
            foreach (int[] section in futureSections)
            {
                i++;
                if (section[0] == foundIndex)
                {
                    return true;
                }
            }

            return false;
        }

        public bool InFutureEndRel2(int left, int straight)
        {
            int x = taken.x2 + left * taken.lx + straight * taken.sx;
            int y = taken.y2 + left * taken.ly + straight * taken.sy;

            return InFutureEnd(x, y);
        }

        public bool InFutureEnd(int x, int y)
        {
			int c = future.path.Count;
			if (c == 0) return false;

			int foundIndex = -1;

			int i;
			for (i = c - 1; i >= 0; i--)
			{
				int[] field = future.path[i];
				if (field[0] == x && field[1] == y && futureActive[i])
				{
					foundIndex = i;
				}
			}

			if (foundIndex == -1) return false;

			i = -1;
			foreach (int[] section in futureSections)
			{
				i++;
				if (section[1] == foundIndex)
				{
					foreach (int[] merge in futureSectionMerges)
					{
						// examine all but the last merged section
						for (int j = 0; j < merge.Length - 1; j++)
						{
							if (merge[j] == i) return false;
						}
					}

					foundEndIndex = foundIndex;
                    foundSectionEnd = i;
                    return true;
				}				
			}

			return false;
		}

        public bool InFutureSectionEnd(int x, int y)
        {
            int c = future.path.Count;
            if (c == 0) return false;

            int foundIndex = -1;

            int i;
            for (i = c - 1; i >= 0; i--)
            {
                int[] field = future.path[i];
                if (field[0] == x && field[1] == y && futureActive[i])
                {
                    foundIndex = i;
                }
            }

            if (foundIndex == -1) return false;

            i = -1;
            foreach (int[] section in futureSections)
            {
                i++;
                if (section[1] == foundIndex)
                {
                    foundEndIndex = foundIndex;
                    return true;
                }
            }

            return false;
        }

        public int[] FindFutureSections(int index)
        {
            int i = -1;
            foreach (int[] section in futureSections)
            {
                i++;
                if (index <= section[0] && index >= section[1])
                {
                    foreach (int[] merge in futureSectionMerges)
                    {
                        for (int j = 0; j < merge.Length; j++)
                        {
                            if (merge[j] == i)
                            {
                                return new int[] { merge[0], merge[merge.Length - 1] };
                            }
                        }
                    }

                    return new int[] { i, i };
                }
            }
            return new int[] { -1, -1 };
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


        // ----- Save / draw path -----

        private void DrawGrid()
        {
            grid = "";

            for (int i = 0; i <= size; i++)
            {
                float i2 = i;
                if (i == 0)
                {
                    i2 = i + 0.01f;
                }
                else if (i == size)
                {
                    i2 = i - 0.01f;
                }

                grid += "\t<path fill=\"transparent\" stroke=\"gray\" stroke-width=\"0.02\" d=\"M " + i2 + " 0 v " + size + "\" />\r\n";
                grid += "\t<path fill=\"transparent\" stroke=\"gray\" stroke-width=\"0.02\" d=\"M 0 " + i2 + " h " + size + "\" />\r\n";
            }
        }

        public void SavePathUncompleted() // used in Path.cs when count area start and end fields are inequal
		{
			int startX = 1;
			int startY = 1;

			savePath = size + "|1-" + startX + "," + startY + ";";

			for (int i = 1; i < taken.path.Count; i++)
			{
				int[] field = taken.path[i];
				int newX = field[0];
				int newY = field[1];

				foreach (int direction in possibleDirections[i])
				{
					savePath += direction + ",";
				}
				savePath = savePath.Substring(0, savePath.Length - 1);
				savePath += "-" + newX + "," + newY + ";";

                startX = newX;
                startY = newY;
            }

            if (possibleDirections.Count > 1)
            {
                foreach (int direction in possibleDirections[possibleDirections.Count - 1])
                {
                    savePath += direction + ",";
                }
            }
            savePath = savePath.Substring(0, savePath.Length - 1);

			File.WriteAllText("uncompleted.txt", savePath);
        }

        private void SavePath() // used in fast run mode
		{
            int startX = 1;
            int startY = 1;
            string completedPathCode = "";
            int lastDrawnDirection = 0;
            savePath = size + "|1-" + startX + "," + startY + ";";


            for (int i = 1; i < taken.path.Count; i++)
			{
                int[] field = taken.path[i];
                int newX = field[0];
                int newY = field[1];

                foreach (int direction in possibleDirections[i])
                {
                    savePath += direction + ",";
                }
                savePath = savePath.Substring(0, savePath.Length - 1);
                savePath += "-" + newX + "," + newY + ";";

                for (int j = 0; j < 4; j++)
                {
                    if (directions[j][0] == newX - startX && directions[j][1] == newY - startY)
                    {
                        if (possibleDirections[i].Length > 1)
                        {
                            if (j == lastDrawnDirection) //stepped straight. We need to check if there is a left or right field in the possibilities
                            {
                                bool leftFound = false;
                                bool rightFound = false;
                                foreach (int direction in possibleDirections[i])
                                {
                                    if (direction == j + 1 || j == 3 && direction == 0) leftFound = true;
                                    if (direction == j - 1 || j == 0 && direction == 3) rightFound = true;
                                }

                                if (leftFound && rightFound) // straight
                                {
                                    completedPathCode += "b";
                                }
                                else if (leftFound) // right
                                {
                                    completedPathCode += "c";
                                }
                                else // left
                                {
                                    completedPathCode += "a";
                                }
                            }
                            else if (j == lastDrawnDirection + 1 || lastDrawnDirection == 3 && j == 0) completedPathCode += "a"; // left
                            else completedPathCode += "c"; // right
                        }

                        lastDrawnDirection = j;
                    }
                }

                startX = newX;
                startY = newY;
            }

            if (possibleDirections.Count > 1)
            {
                foreach (int direction in possibleDirections[possibleDirections.Count - 1])
                {
                    savePath += direction + ",";
                }
            }
            savePath = savePath.Substring(0, savePath.Length - 1);

            /*if (exits.Count > 0)
            {
                savePath += ":";

                for (int i = 0; i < exits.Count; i++)
                {
                    int[] field = exits[i];
                    int x = field[0];
                    int y = field[1];

                    savePath += x + "," + y + "," + exitIndex[i] + ";";
                }
                savePath = savePath.Substring(0, savePath.Length - 1) + "," + taken.circleDirectionLeft;
            }*/

            if (loadFile != "" && File.ReadAllText(loadFile) != savePath || loadFile == "") File.WriteAllText("completed/" + completedCount + "_" + completedPathCode + ".txt", savePath);
        }

		public void DrawPath()
		{
			string startPos = 0.5 + " " + 0.5;
			string startPosFuture = "";
			string path = "";
			string pathFuture = "";

			float posX = 0.5f;
			float posY = 0.5f;
			int startX = 1;
			int startY = 1;
			string backgrounds = "\t<rect width=\"1\" height=\"1\" x=\"" + (startX - 1) + "\" y=\"" + (startY - 1) + "\" fill=\"#ff0000\" fill-opacity=\"0.15\" />\r\n";
			savePath = size + "|1-" + startX + "," + startY + ";";
			string completedPathCode = "";

			int newX = 0;
			int newY = 0;

			bool inLoop = false;
			if (exits.Count > 0)
			{
				int[] lastExit = exits[exits.Count - 1];
				int x = lastExit[0];
				int y = lastExit[1];

				if (!InTaken(x, y))
				{
					inLoop = true;
				}
			}

			int lastDrawnDirection = 0;

			for (int i = 1; i < taken.path.Count; i++)
			{
				int[] field = taken.path[i];
				newX = field[0];
				newY = field[1];

				string color = "#ff0000";
				string opacity = "0.15";

				if (displayExits && inLoop && i > exitIndex[exitIndex.Count - 1])
				{
					color = "#996600";
					opacity = "0.25";
				}

				backgrounds += "\t<rect width=\"1\" height=\"1\" x=\"" + (newX - 1) + "\" y=\"" + (newY - 1) + "\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\r\n";

				foreach (int direction in possibleDirections[i])
				{
					savePath += direction + ",";
				}
				savePath = savePath.Substring(0, savePath.Length - 1);
				savePath += "-" + newX + "," + newY + ";";

				if (saveCheck == true && lineFinished)
				{
					for (int j = 0; j < 4; j++)
					{
						if (directions[j][0] == newX - startX && directions[j][1] == newY - startY)
						{
							if (possibleDirections[i].Length > 1)
							{
								if (j == lastDrawnDirection) //stepped straight. We need to check if there is a left or right field in the possibilities
								{
									bool leftFound = false;
									bool rightFound = false;
									foreach (int direction in possibleDirections[i])
									{
										if (direction == j + 1 || j == 3 && direction == 0) leftFound = true;
										if (direction == j - 1 || j == 0 && direction == 3) rightFound = true;
									}

									if (leftFound && rightFound) // straight
									{
										completedPathCode += "b";
									}
									else if (leftFound) // right
									{
										completedPathCode += "c";
									}
									else // left
									{
										completedPathCode += "a";
									}
								}
								else if (j == lastDrawnDirection + 1 || lastDrawnDirection == 3 && j == 0) completedPathCode += "a"; // left
								else completedPathCode += "c"; // right
							}
							
							lastDrawnDirection = j;
						}
					}
				}

				float prevX = posX;
				float prevY = posY;				

				if (startY == newY)
				{
					posX = (float)(startX + newX) / 2 - 0.5f;
					posY = newY - 0.5f;
				}
				else
				{
					posY = (float)(startY + newY) / 2 - 0.5f;
					posX = newX - 0.5f;
				}			

				if (prevX == posX || prevY == posY)
				{
					path += "L " + posX + " " + posY + "\r\n";
				}
				else
				{
					int dir = 0; //counter-clockwise

					if (posX > prevX && posY > prevY)
					{
						if (startX - 0.5f == posX)
						{
							dir = 1;
						}
					}
					if (posX < prevX && posY < prevY)
					{
						if (startX - 0.5f == posX)
						{
							dir = 1;
						}
					}
					if (posX > prevX && posY < prevY)
					{
						if (startY - 0.5f == posY)
						{
							dir = 1;
						}
					}
					if (posX < prevX && posY > prevY)
					{
						if (startY - 0.5f == posY)
						{
							dir = 1;
						}
					}
					path += "A 0.5 0.5 0 0 " + dir + " " + posX + " " + posY + "\r\n";
				}

				startX = newX;
				startY = newY;
			}

			if (taken.path.Count > 1) path += "L " + (newX - 0.5f) + " " + (newY - 0.5f) + "\r\n";

			string futureBackgrounds = "";
			string futurePath = "";
			startX = -1;
			startY = -1;

			string t = "";

            foreach (int[] section in futureSections)
			{
				t += "--- Section: " + section[0] + " " + section[1] + "\n";
			}
			foreach (int[] section in futureSectionMerges)
			{
				t += "--- Section merge: ";
                foreach (int s in section)
				{
					t += s + ", ";
				}
				t = t.Substring(0, t.Length - 2) + "\n";
				T(t);
			}
            foreach (List<object> obj in futureSectionMergesHistory)
            {
                t += "--- Section merge history: " + obj[0] + "\n";

                foreach (int[] section in (List<int[]>)obj[1])
                {
                    t += "--- Section merge: ";
                    foreach (int s in section)
                    {
                        t += s + ", ";
                    }
                    t = t.Substring(0, t.Length - 2) + "\n";
                }                
            }
			T(t);

            bool prevStartItem = false;

			if (future.path.Count != 0 && displayFuture)
			{
				for (int i = future.path.Count - 1; i >= 0; i--)
				{
					int currentSection = -1;

					bool startItem = false;
					bool endItem = false;

					//T("Future block: " + i + " " + future.path[i][0] + " " + future.path[i][1]);

					//merged sections will be drawn later
					bool continueSign = false;

					for (int j = 0; j < futureSections.Count; j++)
					{
						int[] section = futureSections[j];

						//T(" section[0] " + section[0] + " " + section[1]);
						if (i <= section[0] && i >= section[1])
						{
							currentSection = j;
							//T("currentSection " + currentSection + " i " + i + " " + section[0] + " " + section[1]);
						}

						// examine if the current section is one of the merged sections
						for (int k = 0; k < futureSectionMerges.Count; k++)
						{
							int[] merge = futureSectionMerges[k];
							for (int h = 0; h < merge.Length; h++)
							{
								//T(merge[h] + " " + currentSection);
								if (merge[h] == currentSection)
								{
									//T("Continue at " + i);
									continueSign = true;
								}
							}
						}

						if (i == section[0])
						{
							startItem = true;
						}
						if (i == section[1])
						{
							endItem = true;
						}
					}

					if (continueSign)
					{
						continue;
					}

					//if we stepped on the start item, it is inactive now. The next active field needs to be the start item.
					if (!futureActive[i] && startItem)
					{
						prevStartItem = true;
					}
					
					if (!futureActive[i]) continue;

					if (prevStartItem)
					{
						startItem = true;
						prevStartItem = false;
					}					

					int[] field = future.path[i];
					newX = field[0];
					newY = field[1];

					string color = "#0000ff";
					string opacity = "0.15";
					futureBackgrounds += "\t<rect width=\"1\" height=\"1\" x=\"" + (newX - 1) + "\" y=\"" + (newY - 1) + "\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\r\n";

					if (startItem)
					{
						posX = newX - 0.5f;
						posY = newY - 0.5f;
						startPosFuture = newX - 0.5 + " " + (newY - 0.5);
						pathFuture = "";

						startX = newX;
						startY = newY;

						continue;
					}

					float prevX = posX;
					float prevY = posY;

					if (startY == newY)
					{
						posX = (float)(startX + newX) / 2 - 0.5f;
						posY = newY - 0.5f;
					}
					else
					{
						posY = (float)(startY + newY) / 2 - 0.5f;
						posX = newX - 0.5f;
					}			

					if (prevX == posX || prevY == posY)
					{
						pathFuture += "L " + posX + " " + posY + "\r\n";
					}
					else
					{
						int dir = 0; //counter-clockwise

						if (posX > prevX && posY > prevY)
						{
							if (startX - 0.5f == posX)
							{
								dir = 1;
							}
						}
						if (posX < prevX && posY < prevY)
						{
							if (startX - 0.5f == posX)
							{
								dir = 1;
							}
						}
						if (posX > prevX && posY < prevY)
						{
							if (startY - 0.5f == posY)
							{
								dir = 1;
							}
						}
						if (posX < prevX && posY > prevY)
						{
							if (startY - 0.5f == posY)
							{
								dir = 1;
							}
						}
						pathFuture += "A 0.5 0.5 0 0 " + dir + " " + posX + " " + posY + "\r\n";
					}

					startX = newX;
					startY = newY;

					if (endItem)
					{
						pathFuture += "L " + (newX - 0.5f) + " " + (newY - 0.5f) + "\r\n";
						futurePath += "\t<path d=\"M " + startPosFuture + "\r\n" + pathFuture + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"blue\" stroke-width=\"0.05\" stroke-linecap=\"round\" />\r\n";
					}
				}

				if (futureSectionMerges.Count != 0)
				{
					prevStartItem = false;

					for (int i = futureSectionMerges.Count - 1; i >= 0; i--)
					{
						int[] merge = futureSectionMerges[i];
						for (int j = 0; j < merge.Length; j++)
						{
							int startIndex = futureSections[merge[j]][0];
							int endIndex = futureSections[merge[j]][1];

							T("startIndex " + startIndex + " endIndex " + endIndex);

							for (int k = startIndex; k >= endIndex; k--)
							{
								bool startItem = false;
								bool endItem = false;

								if (j == 0 && k == startIndex)
								{
									startItem = true;
								}
								if (j == merge.Length - 1 && k == endIndex)
								{
									endItem = true;
								}

								//repeat the code above

								if (!futureActive[k] && startItem)
								{
									prevStartItem = true;
								}

								if (!futureActive[k]) continue;

								if (prevStartItem)
								{
									startItem = true;
									prevStartItem = false;
								}

								int[] field = future.path[k];
								newX = field[0];
								newY = field[1];

								string color = "#0000ff";
								string opacity = "0.15";
								futureBackgrounds += "\t<rect width=\"1\" height=\"1\" x=\"" + (newX - 1) + "\" y=\"" + (newY - 1) + "\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\r\n";

								if (startItem)
								{
									posX = newX - 0.5f;
									posY = newY - 0.5f;
									startPosFuture = newX - 0.5 + " " + (newY - 0.5);
									pathFuture = "";

									startX = newX;
									startY = newY;

									continue;
								}

								float prevX = posX;
								float prevY = posY;

								if (startY == newY)
								{
									posX = (float)(startX + newX) / 2 - 0.5f;
									posY = newY - 0.5f;
								}
								else
								{
									posY = (float)(startY + newY) / 2 - 0.5f;
									posX = newX - 0.5f;
								}

								if (prevX == posX || prevY == posY)
								{
									pathFuture += "L " + posX + " " + posY + "\r\n";
								}
								else
								{
									int dir = 0; //counter-clockwise

									if (posX > prevX && posY > prevY)
									{
										if (startX - 0.5f == posX)
										{
											dir = 1;
										}
									}
									if (posX < prevX && posY < prevY)
									{
										if (startX - 0.5f == posX)
										{
											dir = 1;
										}
									}
									if (posX > prevX && posY < prevY)
									{
										if (startY - 0.5f == posY)
										{
											dir = 1;
										}
									}
									if (posX < prevX && posY > prevY)
									{
										if (startY - 0.5f == posY)
										{
											dir = 1;
										}
									}
									pathFuture += "A 0.5 0.5 0 0 " + dir + " " + posX + " " + posY + "\r\n";
								}

								startX = newX;
								startY = newY;

								if (endItem)
								{
									pathFuture += "L " + (newX - 0.5f) + " " + (newY - 0.5f) + "\r\n";
									futurePath += "\t<path d=\"M " + startPosFuture + "\r\n" + pathFuture + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"blue\" stroke-width=\"0.05\" stroke-linecap=\"round\" />\r\n";
								}
							}
						}
					}
				}
			}

			string possibles = "";
			if (possibleDirections.Count > 1)
			{
				foreach (int direction in possibleDirections[possibleDirections.Count - 1])
				{
					savePath += direction + ",";

					possibles += "\t<rect width=\"1\" height=\"1\" x=\"" + (taken.x - 1 + directions[direction][0]) + "\" y=\"" + (taken.y - 1 + directions[direction][1]) + "\" fill=\"#000000\" fill-opacity=\"0.1\" />\r\n";
				}
			}
			savePath = savePath.Substring(0, savePath.Length - 1);

			if (exits.Count > 0)
			{
				savePath += ":";

				for (int i = 0; i < exits.Count; i++)
				{
					int[] field = exits[i];
					int x = field[0];
					int y = field[1];
					
					if (displayExits && !InTaken(x, y))
					{
						backgrounds += "\t<rect width=\"1\" height=\"1\" x=\"" + (x - 1) + "\" y=\"" + (y - 1) + "\" fill=\"#00ff00\" fill-opacity=\"0.4\" />\r\n";
					}
					
					savePath += x + "," + y + "," + exitIndex[i] + ";";
				}

				savePath = savePath.Substring(0, savePath.Length - 1) + "," + taken.circleDirectionLeft;
			}

			if (path != "")
			{
				path = path.Substring(0, path.Length - 2);
			}

            if (taken.minAreaLine.Count != 0 && displayArea)
			{
				string color = taken.countAreaImpair ? "#808000" : "#008000";
				taken.countAreaImpair = false;

                int i = 0;
				foreach (int[] field in taken.minAreaLine)
				{
					int[] prevField = taken.minAreaLine[i == 0 ? taken.minAreaLine.Count - 1 : i - 1];
                    int[] nextField = taken.minAreaLine[i == taken.minAreaLine.Count - 1 ? 0 : i + 1];

					if (taken.circleDirectionLeft)
					{
                        if (field[0] == prevField[0] && field[0] == nextField[0]) // straight vertical
                        {
                            if (field[1] < nextField[1]) // down
                            {
                                areaBackground += "\t<rect width=\"0.25\" height=\"1\" x=\"" + (field[0] - 0.25) + "\" y=\"" + (field[1] - 1) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
                            else // up
                            {
                                areaBackground += "\t<rect width=\"0.25\" height=\"1\" x=\"" + (field[0] - 1) + "\" y=\"" + (field[1] - 1) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
                        }
                        else if (field[1] == prevField[1] && field[1] == nextField[1]) // straight horizontal
                        {
                            if (field[0] < nextField[0]) // right
                            {
                                areaBackground += "\t<rect width=\"1\" height=\"0.25\" x=\"" + (field[0] - 1) + "\" y=\"" + (field[1] - 1) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
                            else // left
                            {
                                areaBackground += "\t<rect width=\"1\" height=\"0.25\" x=\"" + (field[0] - 1) + "\" y=\"" + (field[1] - 0.25) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
                        }
                        else if (field[1] < prevField[1]) // up
						{
							if (field[0] > nextField[0]) // turn left
							{
								areaBackground += "\t<rect width=\"0.25\" height=\"0.25\" x=\"" + (field[0] - 1) + "\" y=\"" + (field[1] - 0.25) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
							}
							else // turn right
							{
								areaBackground += "\t<path d=\"M " + (field[0] - 1) + " " + (field[1] - 1) + " h 1 v 0.25 h -0.75 v 0.75 h -0.25 z\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
							}
						}
						else if (field[1] > prevField[1]) // down
                        {
							if (field[0] < nextField[0]) // turn left
							{
								areaBackground += "\t<rect width=\"0.25\" height=\"0.25\" x=\"" + (field[0] - 0.25) + "\" y=\"" + (field[1] - 1) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
							}
							else // turn right
							{
								areaBackground += "\t<path d=\"M " + field[0] + " " + field[1] + " h -1 v -0.25 h 0.75 v -0.75 h 0.25 z\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
							}
						}
                        else if (field[0] < prevField[0]) // left
                        {
                            if (field[1] < nextField[1]) // turn left
                            {
                                areaBackground += "\t<rect width=\"0.25\" height=\"0.25\" x=\"" + (field[0] - 0.25) + "\" y=\"" + (field[1] - 0.25) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
                            else // turn right
                            {
                                areaBackground += "\t<path d=\"M " + (field[0] - 1) + " " + field[1] + " v -1 h 0.25 v 0.75 h 0.75 v 0.25 z\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
                        }
                        else if (field[0] > prevField[0]) // right
                        {
                            if (field[1] > nextField[1]) // turn left
                            {
                                areaBackground += "\t<rect width=\"0.25\" height=\"0.25\" x=\"" + (field[0] - 1) + "\" y=\"" + (field[1] - 1) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
                            else // turn right
                            {
                                areaBackground += "\t<path d=\"M " + field[0] + " " + (field[1] - 1) + " v 1 h -0.25 v -0.75 h -0.75 v -0.25 z\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
                        }
                    }
                    else // circle direction right
                    {
                        if (field[0] == prevField[0] && field[0] == nextField[0]) // straight vertical
                        {
							if (field[1] < nextField[1]) // down
							{
                                areaBackground += "\t<rect width=\"0.25\" height=\"1\" x=\"" + (field[0] - 1) + "\" y=\"" + (field[1] - 1) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
							else // up
							{
                                areaBackground += "\t<rect width=\"0.25\" height=\"1\" x=\"" + (field[0] - 0.25) + "\" y=\"" + (field[1] - 1) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }                            
                        }
                        else if (field[1] == prevField[1] && field[1] == nextField[1]) // straight horizontal
                        {
                            if (field[0] < nextField[0]) // right
							{
                                areaBackground += "\t<rect width=\"1\" height=\"0.25\" x=\"" + (field[0] - 1) + "\" y=\"" + (field[1] - 0.25) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
							else // left
							{
                                areaBackground += "\t<rect width=\"1\" height=\"0.25\" x=\"" + (field[0] - 1) + "\" y=\"" + (field[1] - 1) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
                        }
                        else if (field[1] < prevField[1]) // up
                        {
                            if (field[0] > nextField[0]) // turn left
                            {
                                areaBackground += "\t<path d=\"M " + (field[0]) + " " + (field[1] - 1) + " v 1 h -0.25 v -0.75 h -0.75 v -0.25 z\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                                
                            }
                            else // turn right
                            {
                                areaBackground += "\t<rect width=\"0.25\" height=\"0.25\" x=\"" + (field[0] - 0.25) + "\" y=\"" + (field[1] - 0.25) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
                        }
                        else if (field[1] > prevField[1]) // down
                        {
							if (field[0] < nextField[0]) // turn left
							{
								areaBackground += "\t<path d=\"M " + (field[0] - 1) + " " + field[1] + " v -1 h 0.25 v 0.75 h 0.75 v 0.25 z\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
							}
							else // turn right
							{
								areaBackground += "\t<rect width=\"0.25\" height=\"0.25\" x=\"" + (field[0] - 1) + "\" y=\"" + (field[1] - 1) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
                        }
                        else if (field[0] < prevField[0]) // left
                        {
                            if (field[1] < nextField[1]) // turn left
                            {
                                areaBackground += "\t<path d=\"M " + (field[0] - 1) + " " + (field[1] - 1) + " h 1 v 0.25 h -0.75 v 0.75 h -0.25 z\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";                      
                            }
                            else // turn right
                            {
                                areaBackground += "\t<rect width=\"0.25\" height=\"0.25\" x=\"" + (field[0] - 0.25) + "\" y=\"" + (field[1] - 1) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
                        }
                        else if (field[0] > prevField[0]) // right
                        {
                            if (field[1] > nextField[1]) // turn left
                            {
                                areaBackground += "\t<path d=\"M " + field[0] + " " + field[1] + " h -1 v -0.25 h 0.75 v -0.75 h 0.25 z\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";                     
                            }
                            else // turn right
                            {
                                areaBackground += "\t<rect width=\"0.25\" height=\"0.25\" x=\"" + (field[0] - 1) + "\" y=\"" + (field[1] - 0.25) + "\" fill=\"" + color + "\" fill-opacity=\"0.25\" />\r\n";
                            }
                        }
                    }

					i++;
                }
            }
			string content = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 " + size + " " + size + "\">\r\n\t<path d=\"M0,0 h" + size + " v" + size + " h-" + size + " z\" fill=\"#dddddd\" />\r\n" + backgrounds + futureBackgrounds + possibles + areaBackground + grid +
				"\t<path d=\"M " + startPos + "\r\n[path]\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" />\r\n" +
				futurePath + "</svg>";
			content = content.Replace("[path]", path);

            svgName = (errorInWalkthrough) ? completedCount + ".svg" : loadFile.Replace("txt", "svg");
			if (svgName == "")
			{
				svgName = DateTime.Today.Month.ToString("00") + DateTime.Today.Day.ToString("00") + ".svg";
			}
			File.WriteAllText(svgName, content);
			if (completedPathCode != "")
			{
				if (loadFile != "" && File.ReadAllText(loadFile) != savePath || loadFile == "") File.WriteAllText("completed/" + completedPathCode + ".txt", savePath);
			}
			Canvas.InvalidateVisual();
		}

		private void SKElement_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			var canvas = e.Surface.Canvas;
			canvas.Clear(SKColors.White);

			var svg = new SkiaSharp.Extended.Svg.SKSvg();
			var picture = svg.Load(svgName);

			var fit = e.Info.Rect.AspectFit(svg.CanvasSize.ToSizeI());
			e.Surface.Canvas.Scale(fit.Width / svg.CanvasSize.Width);
			e.Surface.Canvas.DrawPicture(picture);
		}


        // ----- UI interaction -----


        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
		public static extern short GetKeyState(int keyCode);

		private void MWindow_PreviewKeyDown(object sender, KeyEventArgs e) // with normal KeyDown, event does not fire after calling FocusButton.Focus();
		{			
			if (Size.IsFocused && e.Key != Key.Enter) return;

			if (messageCode != -1 && OKButton.Visibility == Visibility.Visible)
			{
				if (e.Key == Key.Enter)
				{
					OK_Click(new object(), new RoutedEventArgs());
				}
				return;
			}

			bool CapsLock = (((ushort)GetKeyState(0x14)) & 0xffff) != 0;

			if (e.Key == Key.Enter)
			{
				Reload_Click(new object(), new RoutedEventArgs());
				//Keyboard.ClearFocus(); removes the focus from the textbox, but the window becomes unresponsive. Calling MWindow.Focus(); will put the focus on the textbox again.
			}

			else if (e.Key == Key.Space)
			{
				StartStop_Click(new object(), new RoutedEventArgs());
			}
			else if (e.Key == Key.S && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
			{
				Save_Click(new object(), new RoutedEventArgs());
				return;
			}
            else if (e.Key == Key.D && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                ExtractDifference();
            }
            else if (CapsLock || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {

				int direction = -1;
				switch(e.Key)
				{
					case Key.Down:
						direction = 0;
						break;
					case Key.Right:
						direction = 1;
						break;
					case Key.Up:
						direction = 2;
						break;
					case Key.Left:
						direction = 3;
						break;
				}

				if (direction != -1)
				{
					int returnCode = Move(direction);

					if (taken.x == size && taken.y == size && returnCode != 0)
					{
						possibleDirections.Add(new int[] { });
						lineFinished = true;
						DrawPath();
						return;
					}

					if (returnCode == 2)
					{
						NextStepPossibilities();
					}
					if (returnCode != 0)
					{
						DrawPath();
					}
				}				
			}
			else
			{
				if (e.Key == Key.Left)
				{
					Previous_Click(new object(), new RoutedEventArgs());
				}
				else if (e.Key == Key.Right)
				{
					Next_Click(new object(), new RoutedEventArgs());
				}
			}
		}

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (settingsOpen)
            {
                Point pt = e.GetPosition((UIElement)sender);
                HitTestResult result = VisualTreeHelper.HitTest((UIElement)sender, pt);

                if (result.VisualHit == SettingsGrid)
                {
                    CloseSettings_Click(null, null);
                    e.Handled = true;
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            double posX = e.GetPosition(Canvas).X;
            double posY = e.GetPosition(Canvas).Y;
            double gridX = e.GetPosition(MainGrid).X;
            double gridY = e.GetPosition(MainGrid).Y;
            CoordinateLabel.Content = Math.Ceiling(posX / Canvas.ActualWidth * size) + " " + Math.Ceiling(posY / Canvas.ActualHeight * size);
            double right = MainGrid.ActualWidth - gridX;
            double bottom = MainGrid.ActualHeight - gridY;
            if (posX < CoordinateLabel.ActualWidth)
            {
                right -= CoordinateLabel.ActualWidth + 10;
            }
            if (posY < CoordinateLabel.ActualHeight)
            {
                bottom -= CoordinateLabel.ActualHeight + 10;
            }
            CoordinateLabel.Margin = new Thickness(0, 0, right, bottom);
        }

        private void Canvas_MouseEnter(object sender, MouseEventArgs e)
        {
            CoordinateLabel.Visibility = Visibility.Visible;
        }

        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            CoordinateLabel.Visibility = Visibility.Hidden;
        }

        private void Rules_Click(object sender, RoutedEventArgs e)
        {
            Rules rules = new Rules();
            rules.Show();
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
			SettingsGrid.Visibility = Visibility.Visible;
			settingsOpen = true;
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsGrid.Visibility = Visibility.Hidden;
            settingsOpen = false;
        }


        // ----- Logging -----


        public void CL()
        {
            File.Delete("log.txt");
        }

        public void L(string s)
		{
			File.AppendAllText("log.txt", s + "\n");
		}

        public void M(object o, int code)
		{
			Dispatcher.Invoke(() =>
			{
                messageCode = code;
                if (!(isTaskRunning && makeStats && !keepLeftCheck && !source.IsCancellationRequested))
				{
                    MessageLine.Content = o.ToString();
                    MessageLine.Visibility = Visibility.Visible;
                    OKButton.Visibility = Visibility.Visible;
                    if (code != 3) StopTimer();
                }    
				else
				{
                    L(o.ToString());
                }
            });
        }

		private void T(object o)
		{
			Trace.WriteLine(o.ToString());
		}

        private void TracePossible()
        {
            foreach (int[] field in taken.possible)
            {
                T(field[0] + " - " + field[1]);
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
		{
			HideMessage();
			switch (messageCode)
			{
				case 0:
					break;
                case 3:
                    MessageLine.Visibility = Visibility.Visible;
                    break;
				case 1: // when stepping on a future line and it cannot extend
					possibleDirections.RemoveAt(possibleDirections.Count - 1);
					PreviousStepWithFuture();
					inFuture = false;
					selectedSection = -1;
					DrawPath();
					break;
				case 2: // when added future lines cannot extend
					possibleDirections.RemoveAt(possibleDirections.Count - 1);
					PreviousStepWithFuture();
					DrawPath();
					break;
			}
			messageCode = -1;
		}

		private void HideMessage()
		{
			MessageLine.Visibility = Visibility.Hidden;
			OKButton.Visibility = Visibility.Hidden;			
		}


        // ----- Other -----

        private List<int[]> Copy(List<int[]> obj)
        {
            List<int[]> newObj = new();
            foreach (int[] element in obj)
            {
                newObj.Add(element);
            }
            return newObj;
        }

        private void ExtractDifference()
		{
			string[] complete = File.ReadAllLines("completedPaths.txt");
            string[] incomplete = File.ReadAllLines("completedPaths_1.txt");
			if (!Directory.Exists("diff"))
			{
				Directory.CreateDirectory("diff");
			}

			int count = 0;
			int i = 0;
			for (int c = 0; c < complete.Length; c++)
			{
				string lineC = complete[c];
				string lineiC = incomplete[i];

				if (lineC != lineiC)
				{
					File.WriteAllText("diff/" + (c + 1) + ".txt", lineC);
					count++;
				}
				else
				{
                    i++;
                }
            }
			M(count + " differences extracted.", 0);
        }
    }
}