﻿using System;
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

----- COMMENTS -----

CheckNearBorder is already actual on 5x5 (for example, at the first path of a complete walkthrough), write in documentation
Make separate button for fastRun
Write about 1-thin future line extension rule and that when the far end is at the corner, the near end cannot connect to the main line if it has other options. Write about merging procedure.

----- 7 x 7 -----

0909_1: 1x2 future line cannot be completed
CheckMiddleCorner2x2 is not enough, countarea needs to be introduced in light of 0913_2

----- 9 x 9 -----

----- 21 x 21 -----

0829: it is right that we cannot step straight or right, but the C-shape condition is not correct, because it only takes into consideration to the field 2 left. The previous step is already impossible. The near end should be extended, and then the main line has no choice.
0829_1: Stepping left will make a loop in the future line to the left. The situation is still impossible, but it is not clear when it became impossible.

----- FIX SCENARIOS, OTHER -----

0327_2: Draw future line when entering a circle with 1 line to come out from and 3 spaces at the exit (impossible)
0413: Path.CheckFutureSide: check case when both sides are true simultaneously, draw future line on left side to start with. See 0430_2
0415_1: Future line start can be extended, but there is mistakenly no possibilities because of right across check.
	Right now, forbidden fields only apply on the main line when the across field goes up. Are all across checks invalid for future line?
0521: Future line start can be extended now. Taken is connecting to future, but since the left and right fields are not simultaneously empty, future line cannot be extended. In this case, the end of the selected future line cannot fill the empty space next to the main line, unlike it would be the case when connecting to the future section on the top.
Review commented section of CheckNearFutureSide, it caused trouble on 5x5
Does CheckLeftRightFuture() make sense?
Implement right side of connecting to a loop
Implement CheckNearFutureEnd on 21x21

----- SITUATIONS TO CONSIDER -----

CheckFutureL: find a case when both sides are true
Find out the minimum size for Check1x3 when far end of a future line extends
CountArea needs to be implemented upon closed loop with future ?
0811_1 could have been solved with the condition "When the far end of a future line has reached the corner, the near end, when presented with at least 2 choices, cannot connect to the main line", but instead I used "When the near end of a future line can only connect to the main line, the far end, when presented with the possibility of entering the corner and another field, it has to choose the other field". Is the first condition needed in another example?
In CheckFutureLine we adjust inFutureIndex when we connect to a new section of a merged line. SelectedSection will not change. Therefore, 3 merged lines will not be walkthroughable. Find an example, correct and test
Review CheckNearFutureEnd
Countarea not needed in 0729_1
0729_2: Future end will not be able to go further
Examine commented sections in CheckNearFieldCommon, are they needed?
When we step on a future line which is merged, this has to be taken into account, because the far end will be extended if it i close to the near end. Is such situation possible? See 0727
Are there any cases where the returnCode in Move() is 1 an DrawPath calling is needed?
0714_!: When we step up, count area will be impair, so 19,11 is removed. But if we stepped up from 19,11 instead of right, count area impairity would be avoided. Shouldn't we remove the corner (20,11) instead of the side upon count area impairity? 
1x3 reverse future: Examine case where the main line does not go straight between the two future sections
Path.CheckLeftRightFuture: is it possible that this field is the end of a future section?
0430: When we step on the right field, future line cannot be completed. It is right, but not because of C shape left and straight. The straight C shape is not right, because the field 2 ahead is a section start. We should also consider that the actual end to the right cannot go anywhere else.
Simplify ExtendFutureLine: If selectedSection is known, there is no need for start and end parameter.

----- INCREASE EFFECTIVENESS -----

When connecting to another future line in ExtendFutureLine, we cycle through all future fields. It may not be necessary. Just taking the starts or ends.

----- UI IMPROVEMENTS -----nextst

User interface: Add selector: "keep left" or "random steps"
Ability to reload old standard files (without possibilities) and convert them to current standard

----- CHALLENGES TO COMPLETE -----

0425

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
		Path taken;
		Path future;

		List<int> futureIndex = new List<int>();
		public static List<bool> futureActive = new List<bool>();
		List<int> futureLoop = new List<int>();
		List<int> futureLoopSelectedSection = new List<int>();
		public static List<int[]> futureSections = new List<int[]>();
		public static List<int[]> futureSectionMerges = new List<int[]>();
        public static List<List<object>> futureSectionMergesHistory = new List<List<object>>();
        int selectedSection = -1;
		int foundIndex = -1;
		int foundEndIndex = -1;

		List<int[]> possibleDirections = new List<int[]>(); //directions
		public List<int[]> exits = new List<int[]>();
		public List<int> exitIndex = new List<int>();
		bool inFuture = false;
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
		bool nearExtDone, farExtDone, nearEndDone;
		bool displayArea = false; //used for CountArea
		bool displayExits = true;
		bool lineFinished = false;
		int nextDirection = -2;
		int lastDirection = -1;
		bool fastRun = false;
		//bool saveOnlyDirecions = true;
		bool keepLeftCheck, continueCheck;
        int completedCount;
		bool completedWalkthrough = false;
        bool errorInWalkthrough = false;
        CancellationTokenSource source;
        bool isTaskRunning = false;


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
				arr = lines[2].Split(": ");
				SaveCheck.IsChecked = bool.Parse(arr[1]);
                arr = lines[3].Split(": ");
                ContinueCheck.IsChecked = bool.Parse(arr[1]);
                continueCheck = (bool)ContinueCheck.IsChecked;                
                arr = lines[4].Split(": ");
                KeepLeftCheck.IsChecked = bool.Parse(arr[1]);
                keepLeftCheck = (bool)KeepLeftCheck.IsChecked;

                CheckSize();
				Size.Text = size.ToString();
			}
			else
			{
				size = 7;
				Size.Text = size.ToString();
				LoadCheck.IsChecked = false;
				SaveCheck.IsChecked = false;
                ContinueCheck.IsChecked = false;
				continueCheck = false;
                KeepLeftCheck.IsChecked = false;
				keepLeftCheck = false;
            }
			
			exits = new List<int[]>();
			exitIndex = new List<int>();
			taken = new Path(this, size, new List<int[]>(), null, true);
			future = new Path(this, size, new List<int[]>(), null, false);

			ReadDir();

			if ((bool)LoadCheck.IsChecked && loadFile != "")
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
                NextStepPossibilities();
            }
			else if (taken != null && possibleDirections.Count != taken.path.Count + 1)
			{
                M("Error in file", 0);
                T("init 8");
                return;
			}
            DrawPath();
        }

        private void StartStop_Click(object sender, RoutedEventArgs e)
        {
			T("StartStop_Click isTaskRunning: " + isTaskRunning);
            if (fastRun && isTaskRunning)
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
                StartTimer();
            }
        }

        private void StartTimer()
		{
			StartStopButton.Content = "Stop";
            StartStopButton.Style = Resources["RedButton"] as Style;
            if (!fastRun)
			{
                timer.Interval = TimeSpan.FromMilliseconds(100);
                timer.Start();
            }
			else
			{
				completedWalkthrough = false;
				errorInWalkthrough = false;
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
            completedCount = 0;

            do
            {				
                Timer_Tick(null, null);                
				if (source.IsCancellationRequested) break;
            }
            while (!completedWalkthrough && !errorInWalkthrough && completedCount < 1000);

            this.Dispatcher.Invoke(() =>
            {
                StopTimer(); //in case of errors, it is already called.
                DrawPath();
				if (completedWalkthrough) M("The number of walkthroughs are " + completedCount + ".", 0);
				else if (!errorInWalkthrough) M(completedCount + " walkthroughs are completed.", 0);
				else MessageLine.Content = MessageLine.Content.ToString().Replace("Error", "Error at " + completedCount);
            });
			isTaskRunning = false;
        }

		private void StopTimer()
		{
			StartStopButton.Content = "Start";
            StartStopButton.Style = Resources["GreenButton"] as Style;
            if (!fastRun) timer.Stop();
		}

		private void DrawGrid()
		{
			grid = "";

			for (int i = 1; i < size; i++)
			{
				grid += "\t<path fill=\"transparent\" stroke=\"gray\" stroke-width=\"0.02\" d=\"M " + i + " 0 v " + size + "\" />\r\n";
				grid += "\t<path fill=\"transparent\" stroke=\"gray\" stroke-width=\"0.02\" d=\"M 0 " + i + " h " + size + "\" />\r\n";
			}
		}

		private void ReadDir()
		{
			loadFile = "";
			string[] files = Directory.GetFiles("./", "*.txt");
			foreach (string file in files)
			{
				string fileName = file.Substring(2);
				if (fileName != "settings.txt" && fileName.IndexOf("_temp") == -1 && fileName.IndexOf("_error") == -1)
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

			CurrentCoords.Content = taken.x + " " + taken.y;
			PossibleCoords.Text = "";
			ExitCoords.Text = "";

			taken.possible = new List<int[]>();
			foreach (int direction in possibleDirections[possibleDirections.Count - 1])
			{
				PossibleCoords.Text += taken.x + directions[direction][0] + " " + (taken.y + directions[direction][1]) + "\n";
				taken.possible.Add(new int[] { taken.x + directions[direction][0], taken.y + directions[direction][1] });
			}
			foreach (int[] exit in exits)
			{
				ExitCoords.Text += exit[0] + " " + exit[1] + "\n";
			}
			if (ExitCoords.Text.Length > 0)
			{
				ExitCoords.Text = ExitCoords.Text.Substring(0, ExitCoords.Text.Length - 1);
			}

			if (exitIndex.Count > 0)
			{
				int lastExit = exitIndex[exitIndex.Count - 1];
				if (lastExit == taken.path.Count - 1)
				{
					taken.nearField = true;
				}
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

			CurrentCoords.Content = taken.x + " " + taken.y;
			PossibleCoords.Text = "";
			ExitCoords.Text = "";
			foreach (int direction in possibleDirections[possibleDirections.Count - 1])
			{
				PossibleCoords.Text += taken.x + directions[direction][0] + " " + (taken.y + directions[direction][1]) + "\n";
				taken.possible.Add(new int[] { taken.x + directions[direction][0], taken.y + directions[direction][1] });
			}
			taken.nearField = false;
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

		private void Reload_Click(object sender, RoutedEventArgs e)
		{
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

			if (LoadCheck.IsChecked == true && loadFile != "")
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
				NextStepPossibilities();
			}
			else if (possibleDirections.Count != taken.path.Count + 1)
			{
				M("Error in file", 0);
				return;
			}

			DrawPath();
		}

		private void SaveSettings(object sender, RoutedEventArgs e)
        {
			continueCheck = (bool)ContinueCheck.IsChecked;
			keepLeftCheck = (bool)KeepLeftCheck.IsChecked;

            string[] lines = new string[] { "size: " + size, "loadFromFile: " + LoadCheck.IsChecked, "saveOnCompletion: " + SaveCheck.IsChecked, "continueOnCompletion: " + ContinueCheck.IsChecked, "keepLeft: " + KeepLeftCheck.IsChecked };
			
			File.WriteAllLines("settings.txt", lines);
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			ReadDir();

			string saveName = loadFile;
			if (saveName == "")
			{
				saveName = DateTime.Today.Month.ToString("00") + DateTime.Today.Day.ToString("00") + ".txt";
			}
			File.WriteAllText(saveName, savePath);
		}

		private void Previous_Click(object sender, RoutedEventArgs e)
		{
			if (messageCode == 2)
			{
				possibleDirections.RemoveAt(possibleDirections.Count - 1);
				PreviousStepWithFuture();
				messageCode = -1;
				DrawPath();
				return;
			}
			else if (messageCode != -1) return;

			PreviousStep(false);
			DrawPath();
		}

		private void Next_Click(object sender, RoutedEventArgs e)
		{
			NextClick(false);
		}

        private void Timer_Tick(object? sender, EventArgs e)
		{
			NextClick(true);           
		}

		private void NextClick(bool isTimer)
		{
			if (isTimer && fastRun && messageCode != -1)
			{
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
                if ((continueCheck == true || isTimer && fastRun) && keepLeftCheck == true)
                {
					bool rightFound = false;
					nextDirection = -1;
					int c;

					do
					{
						PreviousStep(false);
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
									PreviousStep(false);
								}
							}
						}

					} while (!rightFound && c > 2);

					if (!rightFound)
					{
						PreviousStep(false); // c = 2. We reached the end, step back to the start position
											 // Reset nextDirection, so that we can start again
						completedWalkthrough = true;
						nextDirection = -2;

                        if (isTimer)
                        {
                            StopTimer();
                        }
                    }

                    if (!(isTimer && fastRun)) DrawPath();
                }
                else if (continueCheck == true || isTimer && fastRun)
                {
                    exits = new List<int[]>();
                    exitIndex = new List<int>();
                    areaBackground = "";

                    InitializeList();
                    if (!(isTimer && fastRun)) DrawPath();
                }
				else if (isTimer)
				{
					StopTimer();
				} 
                return;
			}

			if (NextStep())
			{
				if (taken.x == size && taken.y == size)
				{
					possibleDirections.Add(new int[] { });
					lineFinished = true;
                    if (isTimer && fastRun)
                    {
                        completedCount++;
                        //SavePath();
                        //this.Dispatcher.Invoke(() => { DrawPath(); }); //frequent error in SKCanvas: Attempt to read or write protected memory
                    }
                    else DrawPath();
					return;
				}

				NextStepPossibilities();
			}
            if (!(isTimer && fastRun)) DrawPath();
		}

		private void PreviousStep(bool isForbidden)
		{
			
			int count = taken.path.Count;
			if (count < 2) return;

			areaBackground = "";

			int removeX = taken.path[count - 1][0];
			int removeY = taken.path[count - 1][1];

			if (!isForbidden) //actual step back
			{
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

				lastExit = exitIndex.Count - 1;
				if (lastExit >= 0 && exitIndex[lastExit] == taken.path.Count - 1)
				{
					taken.nearField = true;
				}
				else
				{
					taken.nearField = false;
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
			else //The step that would cause the error is not added yet, but x and y is set. We remove that possibility. Next step possibilities will not run.				
			{
				int lastX = taken.path[taken.path.Count - 1][0];
				int lastY = taken.path[taken.path.Count - 1][1];

                if (!isTaskRunning) PossibleCoords.Text = "";
				List<int> dirs = possibleDirections[possibleDirections.Count - 1].ToList<int>();

				if (dirs.Count > 0) //count area is impair
				{
					T("PreviousStep, directions count: " + dirs.Count);
					List<int[]> newPossible = new List<int[]>();
					int foundIndex = -1;
					for (int i = 0; i < dirs.Count; i++)
					{
						int newX = lastX + directions[dirs[i]][0];
						int newY = lastY + directions[dirs[i]][1];

						T(taken.x + " " + taken.y + " " + lastX + " " + lastY + " " + newX + " " + newY);

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
					T("Removing last element again, taken.path.Count " + taken.path.Count);

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

					lastExit = exitIndex.Count - 1;
					if (lastExit >= 0 && exitIndex[lastExit] == taken.path.Count - 1)
					{
						taken.nearField = true;
					}
					else
					{
						taken.nearField = false;
					}

					PreviousStep(true);
				}
				else
				{
					possibleDirections[possibleDirections.Count - 1] = dirs.ToArray<int>();
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
			PreviousStep(true);
		}

		private void TracePossible()
		{
			foreach (int[] field in taken.possible)
			{
				T(field[0] + " - " + field[1]);
			}
		}

		private void NextStepPossibilities()
		{
			T("NextStepPossibilities main, inFuture: " + inFuture + " inFutureIndex: " + inFutureIndex);
			if (inFuture)
			{
				int[] futureField = future.path[inFutureIndex];
				T("Possible: " + futureField[0] + " " + futureField[1]);
				taken.possible = new List<int[]> { new int[] { futureField[0], futureField[1] } };
				taken.nearField = false;
			}
			else
			{
				taken.path2 = future.path;
				taken.NextStepPossibilities(true, -1, -1, -1);
			}

			if (!isTaskRunning) PossibleCoords.Text = "";

            List<int> possibleFields = new List<int>();
			List<int[]> newPossible = new List<int[]>();

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
		}

		private bool NextStep()
		{
			T("NextStep taken.possible.Count: " + taken.possible.Count);
			if (taken.possible.Count == 0)
			{
				T("Removing last element due to no options");
				PreviousStep(true);
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
				int newX = taken.x + directions[directionIndex][0];
				int newY = taken.y + directions[directionIndex][1];
				int[] prevField = taken.path[taken.path.Count - 2];
				if (prevField[0] == newX && prevField[1] == newY)
				{
					PreviousStep(false);
					return 1; //back step, draw only, future possibilities already calculated
				}
			}
			return 0; //the chosen direction was no option to move, no draw
		}

		private bool AddNextStep(int x, int y)
		{
			taken.x = x;
			taken.y = y;

			T("AddNextStep newX " + x + " newY " + y + " nearField " + taken.nearField);

			areaBackground = "";
			if (taken.nearField && !CountArea(x, y))
			{
				return false; //count area impair
			}

			taken.path.Add(new int[] { x, y });

			if (!isTaskRunning) CurrentCoords.Content = x + " " + y;

			return CheckFutureLine(x, y);
		}		

		public void RemoveAndActivateFutureAt(int index, int removeX, int removeY)
		{
			if (futureSectionMergesHistory.Count > 0)
			{
				// There can be more than one future line merges in one step, like in 0913_1. Removing only the last one is not correct.
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
					T("Activate at " + i + " " + removeX + " " + removeY + " inend " + InFutureEnd(removeX, removeY));
					futureActive[i] = true;
					
					if (InFutureStart(removeX, removeY))
					{
						inFuture = false;
						selectedSection = -1;
					}
					else
					{
						if (InFutureEnd(removeX, removeY))
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
						}						
					}
					return;
				}
			}	
		}

		private void Canvas_MouseMove(object sender, MouseEventArgs e)
		{
			double posX = e.GetPosition(Canvas).X;
			double posY = e.GetPosition(Canvas).Y;
			double gridX = e.GetPosition(MainGrid).X;
			double gridY = e.GetPosition(MainGrid).Y;
			CoordinateLabel.Content = Math.Ceiling(posX/Canvas.ActualWidth * size) + " " + Math.Ceiling(posY / Canvas.ActualHeight * size);
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

		private bool CountArea(int startX, int startY)
		{
			int nextX = startX;
			int nextY = startY;
			int exitX = exits[exits.Count - 1][0];
			int exitY = exits[exits.Count - 1][1];
			int count = taken.path.Count;
			int xDiffStart = nextX - taken.path[count - 1][0];
			int yDiffStart = nextY - taken.path[count - 1][1];
			int xDiff, yDiff;
			int minY = nextY;
			int limitX = nextX;
			int startIndex = 0;

			T("CountArea startX " + startX + " startY " + startY + " circleDirectionLeft " + taken.circleDirectionLeft);

			if (Math.Abs(exitY - nextY) == 2) //start and exit are in an L-shaped distance
			{
				nextX = exitX;
				nextY = (exitY + nextY) / 2;
				xDiff = 0;
				yDiff = nextY - exitY;
			}
			else if (Math.Abs(exitX - nextX) == 2)
			{
				nextX = (exitX + nextX) / 2; 
				nextY = exitY;
				xDiff = nextX - exitX;
				yDiff = 0;
			}
			else
			{
				xDiff = nextX - taken.path[count - 1][0];
				yDiff = nextY - taken.path[count - 1][1];
			}

			T("CountArea nextX " + nextX + " nextY " + nextY);

			List<int[]> areaLine = new List<int[]> { new int[] { nextX, nextY } };

			if (displayArea)
			{
				areaBackground = "\t<rect width=\"1\" height=\"1\" x=\"" + (nextX - 1) + "\" y=\"" + (nextY - 1) + "\" fill=\"#0000ff\" fill-opacity=\"0.15\" />\r\n";
			}
			
			T("Count area: " + taken.circleDirectionLeft + " eX " + exitX + " eY " + exitY + " nX " + nextX + " nY " + nextY + " limitX " + limitX + " minY " + minY);

			//return true;

			List<int[]> directions;
			int possibleNextX, possibleNextY;

			if (taken.circleDirectionLeft)
			{
				directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } };
			}
			else
			{
				directions = new List<int[]> { new int[] { 0, 1 }, new int[] { -1, 0 }, new int[] { 0, -1 }, new int[] { 1, 0 } };
			}

			//In case of an impair area, the newest bordering element needs to be removed and blocked from the path. The path here takes a new route so that the area later can be filled.

			int[]? firstInTaken = null;

			int i;
			for (i = 0; i < 4; i++)
			{
				if (xDiffStart == directions[i][0] && yDiffStart == directions[i][1])
				{
					break;
				}
			}

			i = (i == 3) ? 0 : i + 1;
			int xDiffTemp = directions[i][0];
			int yDiffTemp = directions[i][1];
			possibleNextX = startX + xDiffTemp;
			possibleNextY = startY + yDiffTemp;

			while (taken.InBorder(possibleNextX, possibleNextY) || taken.InTaken(possibleNextX, possibleNextY))
			{
				if (firstInTaken == null)
				{
					T("firstInTaken set " + possibleNextX + " " + possibleNextY);
					firstInTaken = new int[] { possibleNextX, possibleNextY };
				}
				i = (i == 0) ? 3 : i - 1;
				xDiffTemp = directions[i][0];
				yDiffTemp = directions[i][1];
				possibleNextX = nextX + xDiffTemp;
				possibleNextY = nextY + yDiffTemp;
			}

			//walkaround

			while (!(nextX == exitX && nextY == exitY))
			{
				for (i = 0; i < 4; i++)
				{
					if (xDiff == directions[i][0] && yDiff == directions[i][1])
					{
						break;
					}
				}
				i = (i == 3) ? 0 : i + 1;
				xDiff = directions[i][0];
				yDiff = directions[i][1];
				possibleNextX = nextX + xDiff;
				possibleNextY = nextY + yDiff;

				while (taken.InBorder(possibleNextX, possibleNextY) || taken.InTaken(possibleNextX, possibleNextY))
				{
					if (firstInTaken == null)
					{
						T("firstInTaken set " + possibleNextX + " " + possibleNextY);
						firstInTaken = new int[] { possibleNextX, possibleNextY };
					}
					i = (i == 0) ? 3 : i - 1;
					xDiff = directions[i][0];
					yDiff = directions[i][1];
					possibleNextX = nextX + xDiff;
					possibleNextY = nextY + yDiff;
				}

				nextX = possibleNextX;
				nextY = possibleNextY;

				//T("Adding to arealine: " + nextX + " " + nextY);
				areaLine.Add(new int[] { nextX, nextY });

				if (nextY < minY)
				{
					minY = nextY;
					limitX = nextX;
					startIndex = areaLine.Count - 1;
				}
				else if (nextY == minY)
				{
					if (taken.circleDirectionLeft) //top right corner
					{
						if (nextX > limitX)
						{
							limitX = nextX;
							startIndex = areaLine.Count - 1;
						}
					}
					else //top left corner
					{
						if (nextX < limitX)
						{
							limitX = nextX;
							startIndex = areaLine.Count - 1;
						}
					}
					
				}

				if (displayArea)
				{
					areaBackground += "\t<rect width=\"1\" height=\"1\" x=\"" + (nextX - 1) + "\" y=\"" + (nextY - 1) + "\" fill=\"#0000ff\" fill-opacity=\"0.15\" />\r\n";
				}
				
			}

			T("Arealine drawn " + areaLine.Count);

			if (areaLine.Count == 2) return true;

			bool lastFieldStartEnd = false;
			if (exitY == startY - 2 && limitX == exitX) //start and exit are in an L-shaped distance, exit is on top. The last field during walkaround when counting starts and ends have to have either added, depending on circle direction. The circle may go higher than the enter and exit.
			{
				lastFieldStartEnd = true;
			}

			List<int[]> startSquares = new List<int[]>();
			List<int[]> endSquares = new List<int[]>();
			int[] startCandidate = new int[] { limitX, minY }; 
			int[] endCandidate = new int[] { limitX, minY };

			int currentY = minY;

			//T("CountArea first x " + areaLine[startIndex][0] + " y " + currentY + " limitX " + limitX);

			for (i = 1; i < areaLine.Count; i++)
			{
				int index = startIndex + i;
				if (index >= areaLine.Count)
				{
					index -= areaLine.Count;
				}
				int[] field = areaLine[index];
				int fieldX = field[0];
				int fieldY = field[1];

				//T("CountArea " + fieldX + " " + fieldY);

				if (fieldY > currentY)
				{
					if (taken.circleDirectionLeft)
					{
						//in the case where where the previous row was a closed peak, but an open dip was preceding it: the previous end field should have the same y and lower x
						if (endSquares.Count > 0)
						{
							int[] square = endSquares[endSquares.Count - 1];
							int x = square[0];
							int y = square[1];

							if (y == fieldY - 1 && x < fieldX)
							{
								startSquares.Add(startCandidate);
								endSquares.Add(endCandidate);
								startCandidate = endCandidate = field;
								currentY = fieldY;
								continue;
							}
						}

						if (startSquares.Count > 0)
						{
							int[] square = startSquares[startSquares.Count - 1];
							int x = square[0];
							int y = square[1];

							if (y == fieldY)
							{
								//the previous row was a closed peak
								if (x <= fieldX)
								{
									endSquares.Add(endCandidate);
									startSquares.Add(startCandidate);
								}
								// else: open peak, no start and end should be marked
							}
							else
							{
								endSquares.Add(endCandidate);
							}
						}
						else
						{
							endSquares.Add(endCandidate);
						}

						if (startSquares.Count == 0 && endSquares.Count == 1) //the area starts with a single field on the top
						{
							int endIndex = startIndex + areaLine.Count - 1;
							if (endIndex >= areaLine.Count)
							{
								endIndex -= areaLine.Count;
							}
							if (areaLine[endIndex][1] != startCandidate[1])
							{
								startSquares.Add(startCandidate);
							}							
						}
					}
					else
					{				
						if (startSquares.Count > 0)
						{
							int[] square = startSquares[startSquares.Count - 1];
							int x = square[0];
							int y = square[1];

							if (y == fieldY - 1 && x > fieldX)
							{
								startSquares.Add(startCandidate);
								endSquares.Add(endCandidate);
								startCandidate = endCandidate = field;
								currentY = fieldY;
								continue;
							}							
						}

						if (endSquares.Count > 0)
						{ 
							int[] square = endSquares[endSquares.Count - 1];
							int x = square[0];
							int y = square[1];

							if (y == fieldY)
							{
								//the previous row was a closed peak
								if (x >= fieldX)
								{
									startSquares.Add(startCandidate);
									endSquares.Add(endCandidate);
								}
								// else: open peak, no start and end should be marked
							}
							else
							{
								startSquares.Add(startCandidate);
							}
						}
						else
						{
							startSquares.Add(startCandidate);							
						}

						if (endSquares.Count == 0 && startSquares.Count == 1)
						{
							int endIndex = startIndex + areaLine.Count - 1;
							if (endIndex >= areaLine.Count)
							{
								endIndex -= areaLine.Count;
							}
							if (areaLine[endIndex][1] != endCandidate[1])
							{
								endSquares.Add(endCandidate);
							}
							
						}
					}
					startCandidate = endCandidate = field;
				}
				else if (fieldY == currentY)
				{
					if (fieldX < startCandidate[0])
					{
						startCandidate = field;
					}
					else if (fieldX > endCandidate[0])
					{
						endCandidate = field;
					}
				}
				else
				{
					if (taken.circleDirectionLeft)
					{
						if (startSquares.Count > 0)
						{
							int[] square = startSquares[startSquares.Count - 1];
							int x = square[0];
							int y = square[1];

							if (y == fieldY + 1 && x > fieldX)
							{
								startSquares.Add(startCandidate);
								endSquares.Add(endCandidate);
								startCandidate = endCandidate = field;
								currentY = fieldY;
								continue;
							}
						}

						if (endSquares.Count > 0)
						{
							int[] square = endSquares[endSquares.Count - 1];
							int x = square[0];
							int y = square[1];

							if (y == fieldY)
							{
								//the previous row was a closed peak
								if (x >= fieldX)
								{
									startSquares.Add(startCandidate);
									endSquares.Add(endCandidate);
								}
								// else: open peak, no start and end should be marked
							}
							else
							{
								startSquares.Add(startCandidate);
							}
						}
						else
						{
							startSquares.Add(startCandidate);
						}
					}
					else
					{
						if (endSquares.Count > 0)
						{
							int[] square = endSquares[endSquares.Count - 1];
							int x = square[0];
							int y = square[1];

							if (y == fieldY + 1 && x < fieldX)
							{
								startSquares.Add(startCandidate);
								endSquares.Add(endCandidate);
								startCandidate = endCandidate = field;
								currentY = fieldY;
								continue;
							}
						}

						if (startSquares.Count > 0)
						{
							int[] square = startSquares[startSquares.Count - 1];
							int x = square[0];
							int y = square[1];

							if (y == fieldY)
							{
								//the previous row was a closed peak
								if (x <= fieldX)
								{
									endSquares.Add(endCandidate);
									startSquares.Add(startCandidate);
								}
								// else: open peak, no start and end should be marked
							}
							else
							{
								endSquares.Add(endCandidate);
							}
						}
						else
						{
							T("direciton right, adding end square");
							endSquares.Add(endCandidate);
						}
					}
					startCandidate = endCandidate = field;
				}
				currentY = fieldY;
			}

			T("lastFieldStartEnd: " + lastFieldStartEnd + " startCandidateX " + startCandidate[0] + " " + currentY);

			//We need to also check that it is a single field in the row
			if (lastFieldStartEnd && startCandidate[0] == endCandidate[0] && startCandidate[0] == exitX)
			{
				if (taken.circleDirectionLeft)
				{
					startSquares.Add(startCandidate);
				}
				else
				{
					endSquares.Add(endCandidate);
				}
			}
			else
			{
				if (taken.circleDirectionLeft)
				{
					//check if last (top) row was an open peak
					int[] square = endSquares[endSquares.Count - 1];
					int x = square[0];
					int y = square[1];
					if (!(startCandidate[0] == x && startCandidate[1] == y + 1))
					{
						startSquares.Add(startCandidate);
					}

					//finish circle at bottom row (the circle consists of 2 rows, with one field in the top
					if (endCandidate[1] == y + 1)
					{
						endSquares.Add(endCandidate);
					}
				}
				else
				{
					int[] square = startSquares[startSquares.Count - 1];
					int x = square[0];
					int y = square[1];
					if (!(endCandidate[0] == x && endCandidate[1] == y + 1))
					{
						endSquares.Add(endCandidate);
					}

					//finish circle at bottom row (the circle consists of 2 rows, with one field in the top
					if (startCandidate[1] == y + 1)
					{
						startSquares.Add(startCandidate);
					}
				}
			}			

			if (displayArea)
			{
				foreach (int[] f in startSquares)
				{
					//T("startSquares " + f[0] + " " + f[1]);
					areaBackground += "\t<path d=\"M " + (f[0] - 1) + " " + (f[1] - 1) + " h 1 l -1 1 v -1\" fill=\"#ff0000\" fill-opacity=\"0.3\" />\r\n";
					//areaBackground += "\t<rect width=\"1\" height=\"1\" x=\"" + (f[0] - 1) + "\" y=\"" + (f[1] - 1) + "\" fill=\"#ff0000\" fill-opacity=\"0.25\" />\r\n";
				}
				foreach (int[] f in endSquares)
				{
					//T("endSquares " + f[0] + " " + f[1]);
					areaBackground += "\t<path d=\"M " + f[0] + " " + f[1] + " h -1 l 1 -1 v 1\" fill=\"#0000ff\" fill-opacity=\"0.3\" />\r\n";
					//areaBackground += "\t<rect width=\"1\" height=\"1\" x=\"" + (f[0] - 1) + "\" y=\"" + (f[1] - 1) + "\" fill=\"#00ff00\" fill-opacity=\"0.25\" />\r\n";
				}
			}			

			count = endSquares.Count;			
			int area = 0;

			T("CountArea start: " + startSquares.Count + " end: " + count);
			if (startSquares.Count != count)
			{
				File.WriteAllText("p_error.txt", savePath);
				StopTimer();				
				M("Count of start and end squares are inequal: " + startSquares.Count + " " + count, 0); 
				return true;
			}

			for (i = 0; i < count; i++)
			{
				area += endSquares[i][0] - startSquares[i][0] + 1;
			}

			T("Count area: " + area);
			if (area % 2 == 1)
			{
				T("Count area is impair " + firstInTaken[0] + " " + firstInTaken[1] + " " + taken.InTakenIndex(firstInTaken[0], firstInTaken[1]));
				TruncatePath(firstInTaken[0], firstInTaken[1]);
				return false;
			}

			//examine circle, draw future fields

			return true;
		}

		private void TruncatePath(int endX, int endY)
		{
			T("TruncatePath " + endX + " " + endY);
			int i;
			for(i = taken.path.Count - 1; i >= 0; i--)
			{
				int[] field = taken.path[i];
				if (!(field[0] == endX && field[1] == endY))
				{
					taken.path.RemoveAt(i);
					possibleDirections.RemoveAt(i + 1);

					if (futureIndex.Count > 0)
					{
						RemoveAndActivateFutureAt(i, field[0], field[1]);
					}
				}
				else
				{
					break;
				}
			}

			taken.path.RemoveAt(i);
			possibleDirections.RemoveAt(i + 1);
			if (futureIndex.Count > 0) //the last element of futureIndex is not the highest if not the most recent future path was extended last time.
			{
                RemoveAndActivateFutureAt(i, endX, endY);
			}

			int[] prevField = taken.path[i - 1];
			int diffX = endX - prevField[0];
			int diffY = endY - prevField[1];

			int j;
			for (j = 0; j < 4; j++)
			{
				int[] dir = directions[j];
				if (diffX == dir[0] && diffY == dir[1])
				{
					break;
				}
			}
			
			int exitCount = exits.Count;
			for(int k = exitCount - 1; k >= 0; k--)
			{
				if (exitIndex[k] >= i)
				{
					exits.RemoveAt(k);
					exitIndex.RemoveAt(k);
				}
			}
			if (exitIndex.Count == 0 || exitIndex.Count > 0 && exitIndex[exitIndex.Count - 1] != i - 1)
			{
				taken.nearField = false;
			} 

			int[] currentField = taken.path[i - 1];
			taken.x = currentField[0];
			taken.y = currentField[1];

			if (!isTaskRunning)
			{
				CurrentCoords.Content = taken.x + " " + taken.y;
				PossibleCoords.Text = "";
			}

			List<int> possibleFields = possibleDirections[i].ToList();
			possibleFields.Remove(j);
			possibleDirections[i] = possibleFields.ToArray();

			if (possibleFields.Count == 0)
			{
				T("Truncated path, no way to go");
				PreviousStep(true);
			}
			else
			{
				taken.possible = new List<int[]>();
				foreach (int dir in possibleFields)
				{
                    if (!isTaskRunning) PossibleCoords.Text += taken.x + directions[dir][0] + " " + (taken.y + directions[dir][1]) + "\n";
					taken.possible.Add(new int[] { taken.x + directions[dir][0], taken.y + directions[dir][1] });
				}
			}
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
							T("--- Exiting future " + x + " " + y + " at " + endIndex);
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

						// if the near and far end are 2 apart, the field between will be now taken by the far end and can be extended further.
						if (x == endX && Math.Abs(y - endY) == 2 || y == endY && Math.Abs(x - endX) == 2)
						{
							future.path2 = taken.path;

                            nearExtDone = true;
                            farExtDone = false;
                            nearEndDone = true;

                            if (!ExtendFutureLine(false, foundIndex, farEndIndex, selectedSection, lastMerged, true))
							{
								//is it possible to form another future line in this step?
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

                            ExtendFutureLine(false, foundIndex, farEndIndex, selectedSection, lastMerged, true);
						}

						int tempSelectedSection = selectedSection;

						// adding other future lines can be timely, as in 0811
						if (!AddFutureLines()) return false;

						selectedSection = tempSelectedSection;
						// selectedSection will be used in CheckFutureLine
						
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

			taken.path2 = null;
			future.path2 = null;

            int thisS0;
            int thisS1;
            int thisL0;
            int thisL1;

            // 0911: Future line on the right can be extended
            if (future.path.Count != 0)
			{
                taken.s = new int[] { taken.x - taken.path[count - 2][0], taken.y - taken.path[count - 2][1] };

                for (int i = 0; i < 4; i++)
                {
                    if (directions[i][0] == taken.s[0] && directions[i][1] == taken.s[1])
                    {
                        int newIndex = (i == 3) ? 0 : i + 1;
                        taken.l = new int[] { directions[newIndex][0], directions[newIndex][1] };
                    }
                }

                thisS0 = taken.s[0];
                thisS1 = taken.s[1];
                thisL0 = taken.l[0];
                thisL1 = taken.l[1];

                


                for (int i = 0; i < 2; i++)
				{
                    if (InFuture(1, 0) && (taken.InTakenRel(2, 0) || taken.InBorderRel(2, 0) || InFuture(2,0)) && !InFuture(1, 1))
					{
						T("1 thin future line valid at " + taken.x + " " + taken.y);

                        nearExtDone = false;
                        farExtDone = false;
                        nearEndDone = false;

						InFuture(1, 0);
						int[] sections = FindFutureSections(foundIndex);

						int nearSection = sections[0];
                        int farSection = sections[1];
                        int nearEndIndex = futureSections[nearSection][0];
                        int farEndIndex = futureSections[farSection][1];

						if (future.path[farEndIndex][0] == size && future.path[farEndIndex][1] == size)
						{
                            if (!ExtendFutureLine(true, nearEndIndex, farEndIndex, nearSection, farSection, false))
                            {
                                possibleDirections.Add(new int[] { });
                                M("Error: 1 thin future line cannot be completed.", 2);
                                return false;
                            }
                        }
                    }
                    //mirror directions
                    taken.s[0] = thisS0;
                    taken.s[1] = thisS1;
                    taken.l[0] = -thisL0;
                    taken.l[1] = -thisL1;
                }
			}

			// First future line, C shape

			bool x2found = false;

			int x = taken.path[count - 2][0];
            int y = taken.path[count - 2][1];
			taken.x2 = x;
			taken.y2 = y;
			taken.s = new int[] { taken.x2 - taken.path[count - 3][0], taken.y2 - taken.path[count - 3][1] };

			for (int i = 0; i < 4; i++)
			{
				if (directions[i][0] == taken.s[0] && directions[i][1] == taken.s[1])
				{
					int newIndex = (i == 3) ? 0 : i + 1;
                    taken.l = new int[] { directions[newIndex][0], directions[newIndex][1] };
				}
			}

			thisS0 = taken.s[0];
            thisS1 = taken.s[1];
            thisL0 = taken.l[0];
            thisL1 = taken.l[1];

            for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
                    int sx = taken.s[0];
                    int sy = taken.s[1];
                    int lx = taken.l[0];
					int ly = taken.l[1];

					// at the lower right corner future line shouldn't be drawn

                    if (!(taken.x == x + lx && taken.y == y + ly) && (taken.InTakenRel2(1, -1) || taken.InBorderRel2(1, -1)) && (taken.InTakenRel2(2, -1) || taken.InBorderRel2(2, -1)) && (taken.InTakenRel2(3, 0) || taken.InBorderRel2(3, 0))
					&& !taken.InTakenRel2(1, 0) && !taken.InTakenRel2(2, 0) && !InFuture2(1, 0) && !(x + 2 * lx == size && y + 2 * ly == size))
					{
						x2found = true;
						T("1x2 future valid at x " + x + " y " + y);

						int startCount = 3;
						
						//This is not added in 0803:						
						if (!InFuture2(2, 1))
						{
							future.path.Add(new int[] { x + 2 * lx + sx, y + 2 * ly + sy });
							startCount = 4;
						}						
						future.path.Add(new int[] { x + 2 * lx, y + 2 * ly });
						future.path.Add(new int[] { x + lx, y + ly });
						future.path.Add(new int[] { x + lx + sx, y + ly + sy });
						for (int k = 0; k < startCount; k++)
						{
							futureIndex.Add(count - 1);
							futureActive.Add(true);
						}
						futureSections.Add(new int[] { future.path.Count - 1, future.path.Count - startCount});
						selectedSection = futureSections.Count - 1;

						future.path2 = taken.path;

						nearExtDone = false;
						farExtDone = false;
						nearEndDone = false;

						if (!ExtendFutureLine(false, future.path.Count - 1, future.path.Count - startCount, selectedSection, selectedSection, false))
						{
							//when making a c shape 3 distance across from the border in the corner, it cannot be completed
							possibleDirections.Add(new int[] { });
							M("Error: 1x2 future line cannot be completed.", 2);							
							return false;														
						}
					}

					//turn right, pattern goes upwards
					int temps0 = taken.s[0];
                    int temps1 = taken.s[1];
                    taken.s[0] = -lx;
                    taken.s[1] = -ly;
					taken.l[0] = temps0;
                    taken.l[1] = temps1;
                }

				//mirror directions
				taken.s[0] = thisS0;
				taken.s[1] = thisS1;
                taken.l[0] = -thisL0;
                taken.l[1] = -thisL1;
            }

			taken.s[0] = thisS0;
            taken.s[1] = thisS1;
            taken.l[0] = thisL0;
            taken.l[1] = thisL1;

			if (!x2found)
			{
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
                        int sx = taken.s[0];
                        int sy = taken.s[1];
                        int lx = taken.l[0];
                        int ly = taken.l[1];

                        //first clause is for quick exclusion. Last clause is to prevent a duplicate line as in 0711
                        if (!(taken.x == x + lx && taken.y == y + ly) && (taken.InTakenRel2(1, -1) || taken.InBorderRel2(1, -1)) && (taken.InTakenRel2(2, -1) || taken.InBorderRel2(2, -1)) && (taken.InTakenRel2(3, -1) || taken.InBorderRel2(3, -1))
						&& (taken.InTakenRel2(4, 0) || taken.InBorderRel2(4, 0))
						&& !taken.InTakenRel2(1, 0) && !taken.InTakenRel2(2, 0) && !taken.InTakenRel2(3, 0) && !InFuture2(2, 0) && !(x + 3 * lx == size && y + 3 * ly == size))
						{
							//if the x + 4 * lx field was in border, we can extend the future lines after this.
							T("1x3 future valid at x " + x + " y " + y);
							future.path.Add(new int[] { x + 3 * lx + sx, y + 3 * ly + sy });
							future.path.Add(new int[] { x + 3 * lx, y + 3 * ly });
							future.path.Add(new int[] { x + 2 * lx, y + 2 * ly });
							future.path.Add(new int[] { x + lx, y + ly });
							future.path.Add(new int[] { x + lx + sx, y + ly + sy });
							for (int k = 0; k < 5; k++)
							{
								futureIndex.Add(count - 1);
								futureActive.Add(true);
							}
							futureSections.Add(new int[] { future.path.Count - 1, future.path.Count - 5 });
							selectedSection = futureSections.Count - 1;

							future.path2 = taken.path;

							nearExtDone = false;
							farExtDone = false;
							nearEndDone = false;

							if (!ExtendFutureLine(false, future.path.Count - 1, future.path.Count - 5, selectedSection, selectedSection, false))
							{
								//when making a c shape 3 distance across from the border in the corner, it cannot be completed
								possibleDirections.Add(new int[] { });
								M("Error: 1x3 future line cannot be completed.", 2);
								return false;

							}
						}

                        //turn right, pattern goes upwards
                        int temps0 = taken.s[0];
                        int temps1 = taken.s[1];
                        taken.s[0] = -lx;
                        taken.s[1] = -ly;
                        taken.l[0] = temps0;
                        taken.l[1] = temps1;
                    }

                    //mirror directions
                    taken.s[0] = thisS0;
                    taken.s[1] = thisS1;
                    taken.l[0] = -thisL0;
                    taken.l[1] = -thisL1;
                }
            }

			if (future.path.Count == 0) return true;

            // If there was a future start left or right to the head of the line in the previous step, that future line may be extended now if it has no other options to move.
            // Example: 0430_2. All 4 directions of needs to be examined, so that O618 works too.
            // minimum size: 5

            taken.s[0] = thisS0;
            taken.s[1] = thisS1;
            taken.l[0] = thisL0;
            taken.l[1] = thisL1;

            taken.path2 = future.path;

			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
                    int sx = taken.s[0];
                    int sy = taken.s[1];
                    int lx = taken.l[0];
                    int ly = taken.l[1];

					// Future lines does not always extend from a 2x2 shape. 0901_2 has a one-wide line.
                    if (!(taken.x == x + lx && taken.y == y + ly) && InFutureStartRel(1, 0) && (InFuture2(2, 0) || taken.InTakenRel2(2, 0) || taken.InBorderRel2(2, 0)) && (InFuture2(1, -1) || taken.InTakenRel2(1, -1)))
					{
						T("Left/right future start valid at x " + x + " y " + y + ", start x " + (x + lx) + " y " + (y + ly));

						if (!InFuture2(1, 1)) // Example: 0902_2
                        {
                            int addIndex = futureSections[selectedSection][0] + 1;
                            future.path.Insert(addIndex, new int[] { x + lx + sx, y + ly + sy });
                            futureIndex.Insert(addIndex, count - 1);
                            futureActive.Insert(addIndex, true);
                            futureSections[selectedSection][0] += 1;
                            IncreaseFurtherSections(selectedSection);
                        }

                        future.path2 = taken.path;
                        T("Extend near end");

						int farEndIndex = futureSections[selectedSection][1];
						int lastMerged = selectedSection;

						//check if this section is merged with another one, to prevent duplicate merging.
						//this section is the first of a possible merge.
						for (int k = 0; k < futureSectionMerges.Count; k++)
						{
							int[] merge = futureSectionMerges[k];
							if (merge[0] == selectedSection)
							{
								lastMerged = merge[merge.Length - 1];
								farEndIndex = futureSections[lastMerged][1];
							}
						}

						nearExtDone = false;
						farExtDone = false;
						nearEndDone = false;

						//start extension from near end, since the far end already has multiple choice
						if (!ExtendFutureLine(true, futureSections[selectedSection][0], farEndIndex, selectedSection, lastMerged, false))
						{
							possibleDirections.Add(new int[] { });
							messageCode = 2;
							T("Left/right future start line cannot be completed.");
							//M("Left/right future start line cannot be completed.", 2);
							return false;
						}
					}

                    //turn right, pattern goes upwards
                    int temps0 = taken.s[0];
                    int temps1 = taken.s[1];
                    taken.s[0] = -lx;
                    taken.s[1] = -ly;
                    taken.l[0] = temps0;
                    taken.l[1] = temps1;
                }

                //mirror directions
                taken.s[0] = thisS0;
                taken.s[1] = thisS1;
                taken.l[0] = -thisL0;
                taken.l[1] = -thisL1;
            }

            // When there is a future start 2 to the left or right (due to the extension of the originally created C shape), and the live end goes straight or the other way (example: 0430_1), the start end can be extended. It will in some cases act as 1x3 C shape checking 
            // The future line is not necessarily the newest. Example: 0427, 0427_1

            if (size >= 7)
			{
                taken.s[0] = thisS0;
                taken.s[1] = thisS1;
                taken.l[0] = thisL0;
                taken.l[1] = thisL1;

                taken.path2 = future.path;

				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
                        int sx = taken.s[0];
						int sy = taken.s[1];
						int lx = taken.l[0];
						int ly = taken.l[1];

						if (!(taken.x == x + lx && taken.y == y + ly) && InFutureStartRel(2, 0) && !taken.InTakenRel2(1, 0) && (taken.InTakenRel2(1, -1) || InFuture2(1, -1)))
						{
                            // Similar to the Left/right future start, here is an example where the two connect: 0903_3
                            // But the previous function extends and connect to the section on the right side befure this function is called. Condition of !InFuture2(1, 1) is not needed in this example.
                            T("Left/right to 2 future start valid at x " + x + " y " + y);

							int addIndex = futureSections[selectedSection][0] + 1;
							future.path.Insert(addIndex, new int[] { x + lx, y + ly });
							future.path.Insert(addIndex + 1, new int[] { x + lx + sx, y + ly + sy });
							futureIndex.Insert(addIndex, count - 1);
							futureIndex.Insert(addIndex + 1, count - 1);
							futureActive.Insert(addIndex, true);
							futureActive.Insert(addIndex + 1, true);
							futureSections[selectedSection][0] += 2;
							IncreaseFurtherSections(selectedSection);
							IncreaseFurtherSections(selectedSection);

							future.path2 = taken.path;

							nearExtDone = false;
							farExtDone = false;
							nearEndDone = false;

							if (!ExtendFutureLine(false, futureSections[selectedSection][0], futureSections[selectedSection][1], selectedSection, selectedSection, false))
							{
								possibleDirections.Add(new int[] { });
								messageCode = 2;
								T("Left/right to 2 future start line cannot be completed.");
								//M("Left/right to 2 future start line cannot be completed.", 2);
								return false;
							}
                        }

                        //turn right, pattern goes upwards
                        int temps0 = taken.s[0];
                        int temps1 = taken.s[1];
                        taken.s[0] = -lx;
                        taken.s[1] = -ly;
                        taken.l[0] = temps0;
                        taken.l[1] = temps1;
                    }

                    //mirror directions
                    taken.s[0] = thisS0;
                    taken.s[1] = thisS1;
                    taken.l[0] = -thisL0;
                    taken.l[1] = -thisL1;
                }
            }			

			//check 3x1 for the original C shape created. If the live end went beyond, a field needs to be added to future
			if (size >= 11)
			{
				int sx, sy, lx, ly, rx, ry;
				
				future.searchReverse = true;
				future.searchStartIndex = future.path.Count + 1; // 2 will be subtracted

                taken.s[0] = thisS0;
                taken.s[1] = thisS1;
                taken.l[0] = thisL0;
                taken.l[1] = thisL1;

                taken.path2 = future.path;

                for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
                        sx = taken.s[0];
                        sy = taken.s[1];
                        lx = taken.l[0];
                        ly = taken.l[1];

                        if (!(taken.x == x + lx && taken.y == y + ly) && taken.InTakenRel2(1, -1) && taken.InTakenRel2(2, -2) && taken.InTakenRel2(3, -2) && taken.InTakenRel2(4, -2) && InFutureStartRel(5, -1)
							&& !taken.InTakenRel2(2, -1) && !taken.InTakenRel2(3, -1) && !taken.InTakenRel2(4, -1))
						{
							T("1x3 reverse future valid at x " + x + " y " + y + " selectedSection " + selectedSection);

							int addIndex = futureSections[selectedSection][0] + 1;
							insertIndex = futureSections[selectedSection][1];
							future.path.Insert(addIndex, new int[] { x - sx + 4 * lx, y - sy + 4 * ly });
							future.path.Insert(insertIndex, new int[] { x - sx + 6 * lx, y - sy + 6 * ly });
							futureIndex.Insert(addIndex, count - 1);
							futureActive.Insert(addIndex, true);
							futureIndex.Insert(insertIndex, count - 1);
							futureActive.Insert(insertIndex, true);
							futureSections[selectedSection][0] += 2;
							IncreaseFurtherSections(selectedSection);
							IncreaseFurtherSections(selectedSection);

							future.path2 = taken.path;

							nearExtDone = false;
							farExtDone = false;
							nearEndDone = false;

							if (!ExtendFutureLine(false, addIndex + 1, insertIndex, selectedSection, selectedSection, false))
							{
								possibleDirections.Add(new int[] { });
								M("1x3 reverse future line cannot be completed.", 2);
								return false;
							}

							/* See 0701_1:
	This situation is not possible. When the start of the left future section and the end of the right future section gets connected, it will go through the field in the middle, 11x11, because it is next to the main line. From there we can extend this mid section to either both sides or one side and up.
	In the first case, the near end will be 12x11, 12x10 and 13x10, the far end the same mirrored. There will be a C shape, 11x10 cannot be filled.
	In the second case, the near end being the same as above, the far end can be 11x10. The start of the left future line will extend to 10x11 and 10x10. 9x10 cannot be filled. The second case can be mirrored, so that 13x10 is the field that cannot be filled.*/

							if (selectedSection > 0)
							{
								int[] section1Start = future.path[futureSections[selectedSection][0]];
								int[] section1End = future.path[futureSections[selectedSection][1]];
								int[] section0Start = future.path[futureSections[selectedSection - 1][0]];
								int[] section0End = future.path[futureSections[selectedSection - 1][1]];

								T(section1Start[0] + " " + section1Start[1]);

								// When the ends of the future lines make up a 4x2 rectangle
								// Examine case where the main line does not go straight between the two future sections!
								if ((section1Start[0] == section1End[0] && section0Start[0] == section0End[0] && section1Start[1] == section0End[1] && section1End[1] == section0Start[1] &&
									Math.Abs(section1Start[1] - section1End[1]) == 2 && Math.Abs(section1End[0] - section0Start[0]) == 4) ||
									(section1Start[1] == section1End[1] && section0Start[1] == section0End[1] && section1Start[0] == section0End[0] && section1End[0] == section0Start[0] &&
									Math.Abs(section1Start[0] - section1End[0]) == 2 && Math.Abs(section1End[1] - section0Start[1]) == 4))
								{
									possibleDirections.Add(new int[] { });
									M("1x3 reverse future line makes a 4x2 rectangle", 2);
									return false;
								}
							}
						}

                        //turn right, pattern goes upwards
                        int temps0 = taken.s[0];
                        int temps1 = taken.s[1];
                        taken.s[0] = -lx;
                        taken.s[1] = -ly;
                        taken.l[0] = temps0;
                        taken.l[1] = temps1;
                    }

                    //mirror directions
                    taken.s[0] = thisS0;
                    taken.s[1] = thisS1;
                    taken.l[0] = -thisL0;
                    taken.l[1] = -thisL1;
                }

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
						future.path2 = taken.path;
						selectedSection = loopSelectedSection;

                        nearExtDone = false;
                        farExtDone = false;
                        nearEndDone = false;

                        ExtendFutureLine(true, startIndex, -1, selectedSection, selectedSection, true);
					}
					else
					{
						T("Turning off closed loop");
					}
				}
				else if (InFutureStart(x + sx, y + sy)) //selectedSection is set here
				{
					//how to decide when we are entering a loop?
					//implement right side

					int startIndex = futureSections[selectedSection][0];
					int endIndex = futureSections[selectedSection][1];

					T("");
					T("Taken connecting to future, selectedSection " + selectedSection);

					//in order to have a loop, left and right fields should be empty, also at 1 step forward. Suppose the latter is true, since we are connecting to the start of a future line. Counter-example?
					if (!taken.InTaken(x + lx, y + ly) && !future.InTakenAll(x + lx, y + ly) && !taken.InTaken(x + rx, y + ry) && !future.InTakenAll(x + rx, y + ry))
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
						futureSections[selectedSection][0] += 1;
						IncreaseFurtherSections(selectedSection);

						future.path2 = taken.path;

                        nearExtDone = false;
                        farExtDone = false;
                        nearEndDone = false;

                        //only extend the far end. The near end has to be extended simultaneously with the live end.
                        if (!ExtendFutureLine(false, -1, endIndex, selectedSection, selectedSection, true))
						{
							possibleDirections.Add(new int[] { });
							M("Closing loop, future line cannot be completed.", 2);
							return false;
						}
						else
						{
							T("--- Creating loop");
							futureLoop.Add(count - 1);
							futureLoopSelectedSection.Add(selectedSection);
						}
					}
                }
			}
			return true;
		}
		private List<int[]> Copy(List<int[]> obj)
		{
			List<int[]> newObj = new();
			foreach (int[] element in obj)
			{
				newObj.Add(element);
			}
			return newObj;
		}

		private bool ExtendFutureLine(bool directionReverse, int nearEndIndex, int farEndIndex, int nearSection, int farSection, bool once)
		{
			int stepCount = 0;
			
			do
			{
				int index = (directionReverse) ? nearEndIndex : farEndIndex;
				T("ExtendFuture directionReverse " + directionReverse + " index " + index + " nearEndIndex " + nearEndIndex + " farEndIndex " + farEndIndex + " nearSection " + nearSection + " farSection " + farSection);

				// In 0814, future line is already extended to the end
				if (!directionReverse && future.path[index][0] == size && future.path[index][1] == size)
				{
					farExtDone = true;
					if (nearExtDone) return true;
					break;
				}

				future.NextStepPossibilities(directionReverse, index, nearSection, farSection);

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
				if (future.path[farEndIndex][0] == size && future.path[farEndIndex][1] == size && future.possible.Count == 2)
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

					// if the only possibility is to connect to the main line, we continue the extension at the far end. example: 0618_2
					if (newField[0] == taken.x && newField[1] == taken.y)
					{
						nearExtDone = true;
						nearEndDone = true;
						break;
					}

					future.x = newField[0];
					future.y = newField[1];

                    //is counting area needed?					

                    // The far end might be connecting to an older section now, but we cannot merge the sections, because there might be a future line in between, like in 0714_2. Instead, we mark the connection.                      
                    // Not only far end can connect to the near end of an older section, but also near end to the far end of the older section, as i n 0730_1 
                    
					// If, after a merge, the line connected to extends, but it ends already in the corner, is it okay to just return, or should we try to extend the near end? Find an example.
                    for (int i = 0; i < future.path.Count; i++)
					{
						int[] field = future.path[i];
						int fx = field[0];
						int fy = field[1];
						if (future.x == fx && future.y == fy)
						{
                            T("Connecting to other section at " + i);

							if (directionReverse) // extend near end. Example: 0911_2. A 2x2 line is created on the right side, the far end extends to the corner, and then the near end extends and want to connect.
							{
                                if (nearSection == farSection) // single line connects to a merged or another single line
                                {
                                    bool foundInMerge = false;
                                    for (int j = 0; j < futureSectionMerges.Count; j++)
                                    {
                                        int[] merge = futureSectionMerges[j];
                                        int[] farthestSection = futureSections[merge[merge.Length - 1]];
                                        if (i == farthestSection[1])
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
                                                return ExtendFutureLine(directionReverse, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                            }
                                            return true;
                                        }
                                    }

                                    if (!foundInMerge)
                                    {
                                        for (int j = 0; j < futureSections.Count; j++)
                                        {
                                            int[] section = futureSections[j];
                                            if (i == section[1])
                                            {
                                                T("Near end: single line connects to single");
                                                futureSectionMerges.Add(new int[] { j, nearSection });
                                                futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });

                                                nearSection = j;
                                                nearEndIndex = futureSections[j][0];
                                                if (!(future.path[nearEndIndex][0] == taken.x && future.path[farEndIndex][1] == taken.y))
                                                {
                                                    return ExtendFutureLine(directionReverse, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                }
                                                return true;
                                            }
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
                                                if (i == farthestSection[1])
                                                {
                                                    T("Near end: merged line connects to merged");
                                                    List<int> listMerge = merge.ToList();
                                                    origListMerge.RemoveAt(0); //removes the path index
                                                    listMerge.AddRange(origListMerge);
                                                    futureSectionMerges[k] = listMerge.ToArray();
                                                    futureSectionMerges.RemoveAt(j);
                                                    futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });
                                                    foundInMerge = true;

                                                    nearSection = listMerge[0];
                                                    nearEndIndex = futureSections[nearSection][0];
                                                    if (!(future.path[nearEndIndex][0] == taken.x && future.path[nearEndIndex][1] == taken.y))
                                                    {
                                                        return ExtendFutureLine(directionReverse, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                    }
                                                    return true;
                                                }
                                            }

                                            if (!foundInMerge)
                                            {
                                                for (int k = 0; k < futureSections.Count; k++)
                                                {
                                                    int[] section = futureSections[k];
                                                    if (i == section[1])
                                                    {
                                                        T("Near end: merged line connects to single");
                                                        origListMerge.Insert(0, k);
                                                        futureSectionMerges[j] = origListMerge.ToArray();
                                                        futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });

                                                        nearSection = k;
                                                        nearEndIndex = futureSections[k][0];
                                                        if (!(future.path[nearEndIndex][0] == taken.x && future.path[nearEndIndex][1] == taken.y))
                                                        {
                                                            return ExtendFutureLine(directionReverse, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                        }
                                                        return true;
                                                    }
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
                                        if (i == nearestSection[0])
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
                                                return ExtendFutureLine(directionReverse, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                            }
                                            return true;
                                        }
                                    }

									if (!foundInMerge)
									{
                                        for (int j = 0; j < futureSections.Count; j++)
                                        {
                                            int[] section = futureSections[j];
                                            if (i == section[0])
                                            {
                                                T("Far end: single line connects to single");
                                                futureSectionMerges.Add(new int[] { farSection, j });
                                                futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });

                                                farSection = j;
                                                farEndIndex = futureSections[j][1];
                                                if (!(future.path[farEndIndex][0] == size && future.path[farEndIndex][1] == size))
                                                {
                                                    return ExtendFutureLine(directionReverse, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                }
                                                return true;
                                            }
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
                                                if (i == nearestSection[0])
                                                {
                                                    T("Far end: merged line connects to merged");
                                                    List<int> listMerge = merge.ToList();
                                                    listMerge.RemoveAt(0); //removes the path index
													origListMerge.AddRange(listMerge);
													futureSectionMerges[j] = origListMerge.ToArray();
													futureSectionMerges.RemoveAt(k);
                                                    futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });
                                                    foundInMerge = true;

                                                    farSection = origListMerge[origListMerge.Count - 1];
                                                    farEndIndex = futureSections[farSection][1];
                                                    if (!(future.path[farEndIndex][0] == size && future.path[farEndIndex][1] == size))
                                                    {
                                                        return ExtendFutureLine(directionReverse, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                    }
                                                    return true;
                                                }
                                            }

                                            if (!foundInMerge)
                                            {
                                                for (int k = 0; k < futureSections.Count; k++)
                                                {
                                                    int[] section = futureSections[k];
                                                    if (i == section[0])
                                                    {
                                                        T("Far end: merged line connects to single");
                                                        origListMerge.Add(k);
                                                        futureSectionMerges[j] = origListMerge.ToArray();
                                                        futureSectionMergesHistory.Add(new List<object>() { taken.path.Count - 1, Copy(futureSectionMerges) });

                                                        farSection = k;
                                                        farEndIndex = futureSections[k][1];
                                                        if (!(future.path[farEndIndex][0] == size && future.path[farEndIndex][1] == size))
                                                        {
                                                            return ExtendFutureLine(directionReverse, nearEndIndex, farEndIndex, nearSection, farSection, once);
                                                        }
														return true;
                                                    }
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

					if (directionReverse)
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
						futureActive.Insert(farEndIndex,true);
					}
					T("nearEndIndex: " + nearEndIndex);
					stepCount++;

					if (future.x == size && future.y == size)
					{
						T("Far ext done");
						farExtDone = true;
						if (nearExtDone) return true;
						break;
					}

				}
			} while (future.possible.Count == 1);
			
			if (future.possible.Count == 0)
			{
				T("Possible count: 0");
				// in 0811_4 it can happen that after stepping on the future line, the other line gets extended and merges into the line being stepped on.
				if (directionReverse && future.path[nearEndIndex][0] == taken.x && future.path[nearEndIndex][1] == taken.y)
				{
					nearExtDone = true;
				}
				else return false;
			}

			T("Future path has multiple choice or reached the end or can only connect to the main line, stepCount " + stepCount);
			foreach (int[] field in future.possible)
			{
				T(field[0] + " " + field[1]);
			}
			foreach (int[] section in futureSections)
			{
				T("- Section: " + section[0] + " " + section[1]);
			}

			// return only when both ends have multiple choice or the near end can only connect to the main line
			// if steps were taken, we cannot return, because the other end could now advance
			if (stepCount == 0)
			{
				if (directionReverse)
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

			if (!once)
			{
				if (directionReverse)
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

		private void IncreaseFurtherSections(int section)
		{
			for (int i = section + 1; i < futureSections.Count; i++)
			{
				futureSections[i][0]++;
				futureSections[i][1]++;
			}
			// In 0811_4, other future line is extended after we stepped on one
			// inFutureIndex can also be within the current section when we step on one, so it just needs to be larger than its far end.
			if (inFutureIndex > futureSections[section][1]) inFutureIndex++;
		}

        public bool InFuture(int left, int straight)
        {
            int x = taken.x + left * taken.l[0] + straight * taken.s[0];
            int y = taken.y + left * taken.l[1] + straight * taken.s[1];

			//In 0913_3 it csan happen that after stepping on the future line, the 1-thin rule is true if we don't check that the coordinates are within size.
			if (x > size || y > size) return true;

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

        public bool InFuture2(int left, int straight)
		{
            int x = taken.x2 + left * taken.l[0] + straight * taken.s[0];
            int y = taken.y2 + left * taken.l[1] + straight * taken.s[1];

            int c = future.path.Count;
            if (c == 0) return false;

            for (int i = c - 1; i >= 0; i--)
            {
                int[] field = future.path[i];
                if (x == field[0] && y == field[1] && futureActive[i])
                {
                    return true;
                }
            }
            return false;
        }

		public bool InFutureStartRel(int left, int straight)
		{
			int x = taken.x2 + left * taken.l[0] + straight * taken.s[0];
			int y = taken.y2 + left * taken.l[1] + straight * taken.s[1];

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

					selectedSection = i;
					return true;
				}
			}

			return false;
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
					return true;
				}				
			}

			return false;
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

            if (exits.Count > 0)
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
            }

            if (loadFile != "" && File.ReadAllText(loadFile) != savePath || loadFile == "") File.WriteAllText("completed/" + completedPathCode + ".txt", savePath);
        }

		private void DrawPath()
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

				if (!taken.InTaken(x, y))
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

				if ((SaveCheck.IsChecked == true || fastRun) && lineFinished)
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
			t += "\n";
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
            t += "\n";
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

			if (future.path.Count != 0)
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
						futurePath += "\t<path d=\"M " + startPosFuture + "\r\n" + pathFuture + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"blue\" stroke-width=\"0.05\" />\r\n";
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
									futurePath += "\t<path d=\"M " + startPosFuture + "\r\n" + pathFuture + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"blue\" stroke-width=\"0.05\" />\r\n";
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

			ExitCoords.Text = "";
			if (exits.Count > 0)
			{
				savePath += ":";

				for (int i = 0; i < exits.Count; i++)
				{
					int[] field = exits[i];
					int x = field[0];
					int y = field[1];
					
					if (displayExits && !taken.InTaken(x, y))
					{
						backgrounds += "\t<rect width=\"1\" height=\"1\" x=\"" + (x - 1) + "\" y=\"" + (y - 1) + "\" fill=\"#00ff00\" fill-opacity=\"0.4\" />\r\n";
					}
					
					savePath += x + "," + y + "," + exitIndex[i] + ";";
					ExitCoords.Text += field[0] + " " + field[1] + "\n";
				}
				ExitCoords.Text = ExitCoords.Text.Substring(0, ExitCoords.Text.Length - 1);

				savePath = savePath.Substring(0, savePath.Length - 1) + "," + taken.circleDirectionLeft;
			}

			if (path != "")
			{
				path = path.Substring(0, path.Length - 2);
			}

			string content = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 " + +size + " " + size + "\">\r\n\t<path d=\"M0,0 h" + size + " v" + size + " h-" + size + " z\" fill=\"#dddddd\" />\r\n" + backgrounds + futureBackgrounds + possibles + areaBackground + grid +
				"\t<path d=\"M " + startPos + "\r\n[path]\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" />\r\n" +
				futurePath + "</svg>";
			content = content.Replace("[path]", path);

			svgName = loadFile.Replace("txt", "svg");
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
				FocusButton.Focus();
				//Keyboard.ClearFocus(); removes the focus from the textbox, but the window becomes unresponsive. Calling MWindow.Focus(); will put the focus on the textbox again.
			}

			else if (e.Key == Key.Space)
			{
				StartStop_Click(new object(), new RoutedEventArgs());
			}
			else if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.S)
			{
				Save_Click(new object(), new RoutedEventArgs());
				return;
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

		private void M(object o, int code)
		{
			this.Dispatcher.Invoke(() =>
			{
                MessageLine.Content = o.ToString();
                MessageLine.Visibility = Visibility.Visible;
                OKButton.Visibility = Visibility.Visible;
                messageCode = code;
                StopTimer();
            });
        }

		private void T(object o)
		{
			Trace.WriteLine(o.ToString());
		}

		private void OK_Click(object sender, RoutedEventArgs e)
		{
			HideMessage();
			switch (messageCode)
			{
				case 0:
					break;
				case 1: // when stepping on a future line and it cannot extend
					possibleDirections.RemoveAt(possibleDirections.Count - 1);
					PreviousStepWithFuture();
					inFuture = false;
					selectedSection = -1;
					DrawPath();
					break;
				case 2: // when added future lines cannot extend
					T("PreviousStepWithFuture");
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
	}
}