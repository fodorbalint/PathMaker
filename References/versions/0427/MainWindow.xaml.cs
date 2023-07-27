using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
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

// Ability to reload old standard files (without possibilities) and convert them to current standard
// CountArea needs to be implemented upon closed loop with future

// 1x3 C shape future valid: can it be, it is not the latest future line? Then use taken.foundFutureStartIndex instead taken.Count - 1.	
// 1x3 C shape future: can this be not the last future section? Then indexing should be corrected.

// 0227: Unreproducible error previously
// 0324: Due to C shape, we can only step on the right field, but there are two possibilities displayed.
// 0327_2: Draw future line when entering a circle with 1 line to come out from and 3 spaces at the exit (impossible)
// 0413: Path.CheckFutureSide: check case when both sides are true simultaneously, draw future line on left side to start with
// 0415_1 / 0427_!: Future line start can be extended now 

// 0425: Challenge to complete

namespace SvgApp
{
	/// <summaly>
	/// Interaction logic for MainWindow.xaml
	/// </summaly>
	public partial class MainWindow : Window
	{
		private string grid = "";
		Random rand = new Random();
		Path taken;
		Path future;

		List<int> futureIndex = new List<int>();
		List<bool> futureActive = new List<bool>();
		List<int> futureLoop = new List<int>();
		List<int> futureLoopSelectedSection = new List<int>();
		public static List<int[]> futureSections = new List<int[]>();
		int selectedSection = -1;
		int foundEndIndex = -1;

		List<int[]> possibleDirections = new List<int[]>(); //directions
		public List<int[]> exits = new List<int[]>();
		public List<int> exitIndex = new List<int>();
		bool inFuture = false;
		int inFutureIndex = -1;
		int insertIndex = -1;

		string savePath = "";
		int size = 0;		
		string areaBackground = "";
		//int x, y, dx, dy, lx, ly, rx, ry; //based on an up direction, these indicate left and right coordinates.
		int[] straightField = new int[] { };
		int[] leftField = new int[] { };
		int[] rightField = new int[] { };
		List<int[]> directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } }; //down, right, up, left
		int[] selectedDirection = new int[] { };
		private DispatcherTimer timer;

		public MainWindow()
		{
			InitializeComponent();

			timer = new DispatcherTimer();
			timer.Tick += Timer_Tick;

			if (File.Exists("size.txt"))
			{
				size = int.Parse(File.ReadAllText("size.txt"));
				Size.Text = size.ToString();
			}
			else
			{
				size = int.Parse(Size.Text);
			}			

			DrawGrid();
			
			exits = new List<int[]>();
			exitIndex = new List<int>();
			taken = new Path(this, size, new List<int[]>(), null, true);
			future = new Path(this, size, new List<int[]>(), null, false);

			if (File.Exists("savePath.txt"))
			{
				LoadFromFile();
			}
			else
			{
				InitializeList();
			}

			if (taken != null && possibleDirections.Count == taken.path.Count) //null checking is only needed for removing warning
			{
				NextStepPossibilities();
			}
			else if (taken != null && possibleDirections.Count != taken.path.Count + 1)
			{
				M("Error in file");
				return;
			}

			DrawPath();
		}

		private void StartTimer()
		{
			StartStopButton.Content = "Stop";
			StartStopButton.Background = Brushes.LightPink;
			timer.Interval = TimeSpan.FromMilliseconds(100);
			timer.Start();
		}

		private void StopTimer()
		{
			StartStopButton.Content = "Start";
			StartStopButton.Background = Brushes.LightGreen;
			timer.Stop();
		}

		private void Timer_Tick(object? sender, EventArgs e)
		{
			if (!(taken.x == size && taken.y == size))
			//for (int i = 0; i < steps; i++)
			{
				if (NextStep())
				{
					if (taken.x == size && taken.y == size)
					{
						possibleDirections.Add(new int[] { });
						DrawPath();
						StopTimer();						
						return;
					}
					NextStepPossibilities();
				}
				DrawPath();
			}
			else
			{
				StopTimer();
			}
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

		private void LoadFromFile()
		{
			string content = File.ReadAllText("savePath.txt");
			string[] loadPath;
			bool circleDirectionLeft = true;
			inFuture = false;
			selectedSection = -1;

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

					CheckFutureLine(x, y);					
				}
			}

			T("LoadFromFile " + taken.path.Count + " " + possibleDirections.Count);

			taken.x = taken.path[taken.path.Count - 1][0];
			taken.y = taken.path[taken.path.Count - 1][1];

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
		}

		private void InitializeFuture()
		{
			future = new Path(this, size, new List<int[]>(), null, false);
			futureIndex = new List<int>();
			futureActive = new List<bool>();
			futureLoop = new List<int>();
			futureLoopSelectedSection = new List<int>();
			futureSections = new List<int[]>();
		}

		private void StartStop_Click(object sender, RoutedEventArgs e)
		{
			if (timer.IsEnabled)
			{
				StopTimer();
				return;
			}
			
			if (taken.path.Count >= 1)
			{
				StartTimer();				
			}			
		}

		private void Reload_Click(object sender, RoutedEventArgs e)
		{
			if (File.Exists("size.txt") && Size.Text != File.ReadAllText("size.txt") || !File.Exists("size.txt"))
			{
				File.WriteAllText("size.txt", Size.Text);
			}
			size = int.Parse(Size.Text);

			DrawGrid();

			exits = new List<int[]>();
			exitIndex = new List<int>();
			areaBackground = "";

			if (FromFileCheck.IsChecked == true && File.Exists("savePath.txt"))
			{
				LoadFromFile();
			}
			else
			{
				InitializeList();
			}

			if (possibleDirections.Count == taken.path.Count)
			{
				NextStepPossibilities();
			}
			else if (possibleDirections.Count != taken.path.Count + 1)
			{
				M("Error in file");
				return;
			}

			DrawPath();
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			File.WriteAllText("savePath.txt", savePath);
		}

		private void Previous_Click(object sender, RoutedEventArgs e)
		{
			//T("Previous_Click1 exits.Count x " + taken.x + " exitsCount " + exits.Count);
			PreviousStep(false);
			//T("Previous_Click x " + taken.x + " nearField " + taken.nearField + " possible.Count " + taken.possible.Count);
			DrawPath();
		}

		private void Next_Click(object sender, RoutedEventArgs e)
		{
			if (taken.x == size && taken.y == size)
			{
				return;
			}

			if (NextStep())
			{
				if (taken.x == size && taken.y == size)
				{
					possibleDirections.Add(new int[] { });
					DrawPath();
					return;
				}

				NextStepPossibilities();
			}
			DrawPath();
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
					RemoveFutureAt(count - 1);
					ActivateFutureAt(removeX, removeY);
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
				PossibleCoords.Text = "";
				List<int> dirs = possibleDirections[possibleDirections.Count - 1].ToList<int>();

				foreach(int dir in dirs)
				{
					int newX = taken.x + directions[dir][0];
					int newY = taken.y + directions[dir][1];

					taken.possible.Add(new int[] { newX, newY });
					PossibleCoords.Text += newX + " " + newY + "\n";
				}

				CurrentCoords.Content = taken.x + " " + taken.y;				
			}
			else //The step that would cause the error is not added yet, but x and y is set. We remove that possibility. Next step possibilities will not run.				
			{
				int lastX = taken.path[taken.path.Count - 1][0];
				int lastY = taken.path[taken.path.Count - 1][1];

				PossibleCoords.Text = "";
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
							PossibleCoords.Text += newX + " " + newY + "\n";
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
						RemoveFutureAt(count - 1);
						ActivateFutureAt(removeX, removeY);
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
			RemoveFutureAt(count - 1);
			ActivateFutureAt(removeX, removeY);
			
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
			//T("NextStepPossibilities main, inFuture: " + inFuture + " inFutureIndex: " + inFutureIndex);
			if (inFuture)
			{
				int[] futureField = future.path[inFutureIndex];
				//T("Possible: " + futureField[0] + " " + futureField[1]);
				taken.possible = new List<int[]> { new int[] { futureField[0], futureField[1] } };
				taken.nearField = false;
			}
			else
			{
				taken.path2 = future.path;
				taken.NextStepPossibilities();
			}

			PossibleCoords.Text = "";
			List<int> possibleFields = new List<int>();
			List<int[]> newPossible = new List<int[]>();

			foreach (int[] field in taken.possible)
			{
				int fx = field[0];
				int fy = field[1];

				if (inFuture)
				{
					//T("NextStepPossibilities InFuture fx " + fx + " fy " + fy + " count: " + future.path.Count);
					PossibleCoords.Text += fx + " " + fy + "\n";
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
						PossibleCoords.Text += fx + " " + fy + "\n";
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

			/*foreach (int[] field in taken.possible)
			{
			{
				T("NextStep, possible: " + field[0] + " " + field[1]);
			}*/

			int[] newField = taken.possible[rand.Next(0, taken.possible.Count)];

			return AddNextStep(newField[0], newField[1]);
		}

		private int Move(int directionIndex)
		{
			T("Move x " + taken.x + " y " + taken.y + " directionIndex " + directionIndex);

			List<int> possibleFields = possibleDirections[possibleDirections.Count - 1].ToList<int>();
			if (possibleFields.IndexOf(directionIndex) != -1)
			{
				if (AddNextStep(taken.x + directions[directionIndex][0], taken.y + directions[directionIndex][1]))
				{
					return 2;
				}
				else
				{
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
					return 1; //back step
				}
			}
			return 0; //the chosen direction was no option to move
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

			CurrentCoords.Content = x + " " + y;

			if (exits.Count > 0)
			{
				int lastExit = exits.Count - 1;
				if (x == exits[lastExit][0] && y == exits[lastExit][1])
				{
					exits.RemoveAt(lastExit);
					exitIndex.RemoveAt(lastExit);
				}
			}

			return CheckFutureLine(x, y);
		}		

		public void RemoveFutureAt(int index)
		{
			T("RemoveFutureAt " + index);
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
		}

		public void ActivateFutureAt (int removeX, int removeY)
		{
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
			areaBackground = "\t<rect width=\"1\" height=\"1\" x=\"" + (nextX - 1) + "\" y=\"" + (nextY - 1) + "\" fill=\"#0000ff\" fill-opacity=\"0.15\" />\r\n";

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

				T("Adding to arealine: " + nextX + " " + nextY);
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

				areaBackground += "\t<rect width=\"1\" height=\"1\" x=\"" + (nextX - 1) + "\" y=\"" + (nextY - 1) + "\" fill=\"#0000ff\" fill-opacity=\"0.15\" />\r\n";
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

			T("CountArea first x " + areaLine[startIndex][0] + " y " + currentY + " limitX " + limitX);

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

				T("CountArea " + fieldX + " " + fieldY);

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

			count = endSquares.Count;			
			int area = 0;

			T("CountArea start: " + startSquares.Count + " end: " + count);
			if (startSquares.Count != count)
			{
				File.WriteAllText("savePath_error.txt", savePath);
				StopTimer();				
				M("Count of start and end squares are inequal: " + startSquares.Count + " " + count); 
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
						RemoveFutureAt(i);
						ActivateFutureAt(field[0], field[1]);
					}
				}
				else
				{
					break;
				}
			}

			taken.path.RemoveAt(i);
			possibleDirections.RemoveAt(i + 1);
			if (futureIndex.Count > 0)
			{
				RemoveFutureAt(i);
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
			
			CurrentCoords.Content = taken.x + " " + taken.y;
			PossibleCoords.Text = "";			

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
					PossibleCoords.Text += taken.x + directions[dir][0] + " " + (taken.y + directions[dir][1]) + "\n";
					taken.possible.Add(new int[] { taken.x + directions[dir][0], taken.y + directions[dir][1] });
				}
			}
		}

		private bool CheckFutureLine(int x, int y)
		{
			//T("CheckFutureLine inFuture: " + inFuture + " x " + x + " y " + y + " future.path.Count " + future.path.Count);
			if (future.path.Count > 0)
			{
				//we don't necessarily step on the last future section (for example when there are future lines on both sides)				

				//check if we are at the end of a future section

				if (inFuture)
				{
					//T("inFuture selectedSection " + selectedSection);
					int endIndex = futureSections[selectedSection][1];
					int[] field = future.path[endIndex];
					if (field[0] == x && field[1] == y)
					{
						//T("--- Exiting future " + x + " " + y + " at " + endIndex);
						futureActive[endIndex] = false;
						inFuture = false;
						selectedSection = -1;
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
						//T("Stepped on future " + x + " " + y + " at " + foundIndex);

						inFuture = true;
						inFutureIndex = foundIndex - 1;
						futureActive[foundIndex] = false;
						//we do not a	dd future lines while completing one

						//when extending the other end, the starting end is now not live anymore
						future.liveEndX = -1;
						future.liveEndY = -1;

						taken.path2 = future.path;

						int endIndex = futureSections[selectedSection][1];

						int endX = future.path[endIndex][0];
						int endY = future.path[endIndex][1];

						if (x == endX && Math.Abs(y - endY) == 2 || y == endY && Math.Abs(x - endX) == 2)
						{
							future.path2 = taken.path;

							if (!ExtendFutureLine(false, foundIndex, endIndex, true))
							{
								//is it possible to form another future line in this step?
								possibleDirections.Add(new int[] { });
								DrawPath();
								possibleDirections.RemoveAt(possibleDirections.Count - 1);
								T("Stepped on future, other end cannot be completed");
								M("Stepped on future, other end cannot be completed");
								//the new possible directions is not set yet.

								PreviousStepWithFuture();

								inFuture = false;
								selectedSection = -1;
								return false; //to prevent NextStepPossibilities from running
							}
							else
							{
							}
						}

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
			//T("AddFutureLines");
			int count = taken.path.Count;
			if (count < 8) return true;

			taken.path2 = null;
			future.path2 = null;

			int x = taken.path[count - 2][0];
			int y = taken.path[count - 2][1];
			int dx = x - taken.path[count - 3][0];
			int dy = y - taken.path[count - 3][1];

			int lx = 0;
			int ly = 0;

			for (int i = 0; i < 4; i++)
			{
				if (directions[i][0] == dx && directions[i][1] == dy)
				{
					int newIndex = (i == 3) ? 0 : i + 1;
					lx = directions[newIndex][0];
					ly = directions[newIndex][1];
				}
			}

			int thisDx = dx;
			int thisDy = dy;
			int thisLx = lx;
			int thisLy = ly;

			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					if (taken.InTaken(x - dx + lx, y - dy + ly) && taken.InTaken(x - dx + 2 * lx, y - dy + 2 * ly) && taken.InTaken(x - dx + 3 * lx, y - dy + 3 * ly)
					&& (taken.InTaken(x + 4 * lx, y + 4 * ly) || taken.InBorder(x + 4 * lx, y + 4 * ly))
					&& !taken.InTaken(x + lx, y + ly) && !taken.InTaken(x + 2 * lx, y + 2 * ly) && !taken.InTaken(x + 3 * lx, y + 3 * ly)
					&& !(taken.x == x + lx && taken.y == y + ly))
					{
						//if the x + 4 * lx field was in border, we can extend the future lines after this.
						T("1x3 future valid at x " + x + " y " + y + " i " + i + " j " + j + " dx " + dx + " dy " + dy + " lx " + lx + " ly " + ly);
						future.path.Add(new int[] { x + 3 * lx + dx, y + 3 * ly + dy });
						future.path.Add(new int[] { x + 3 * lx, y + 3 * ly });
						future.path.Add(new int[] { x + 2 * lx, y + 2 * ly });
						future.path.Add(new int[] { x + lx, y + ly });
						future.path.Add(new int[] { x + lx + dx, y + ly + dy });
						for (int k = 0; k < 5; k++)
						{
							futureIndex.Add(count - 1);
							futureActive.Add(true);
						}
						futureSections.Add(new int[] { future.path.Count - 1, future.path.Count - 5});
					}

					//turn right, pattern goes upwards
					int tempdx = dx;
					int tempdy = dy;
					dx = -lx;
					dy = -ly;
					lx = tempdx;
					ly = tempdy;
				}

				//mirror directions
				dx = thisDx;
				dy = thisDy;
				lx = -thisLx; //equal to rx and ry
				ly = -thisLy;
			}

			if (future.path.Count == 0) return true;

			//check C shape with the nearest end of the future. If the live end went beyond, extending the future line is obvuous.
			//the path may return to a previous future line to complete the pattern, example 0427

			future.searchReverse = true;
			future.searchStartIndex = future.path.Count + 1; // 2 will be subtracted

			x = taken.path[count - 2][0];
			y = taken.path[count - 2][1];
			dx = x - taken.path[count - 3][0];
			dy = y - taken.path[count - 3][1];

			for (int i = 0; i < 4; i++)
			{
				if (directions[i][0] == dx && directions[i][1] == dy)
				{
					int newIndex = (i == 3) ? 0 : i + 1;
					lx = directions[newIndex][0];
					ly = directions[newIndex][1];
				}
			}

			thisDx = dx;
			thisDy = dy;
			thisLx = lx;
			thisLy = ly;

			taken.path2 = future.path;

			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					if (InFutureStart(x + 2 * lx, y + 2 * ly) && InFutureEnd(x + 4 * lx, y + 4 * ly) && taken.InTaken(x - dx + lx, y - dy + ly)	&& !taken.InTaken(x + lx, y + ly))
					{
						T("1x3 C shape future valid at x " + x + " y " + y);
						if (!File.Exists("log.txt"))
						{
							File.WriteAllText("log.txt", "1x3 C shape future valid at x " + x + " y " + y);
						}

						int addIndex = futureSections[selectedSection][0] + 1;
						insertIndex = futureSections[selectedSection][1];
						future.path.Insert(addIndex, new int[] { x + lx, y + ly }); 
						future.path.Insert(insertIndex, new int[] { x + 3 * lx, y + 3 * ly });
						futureIndex.Insert(addIndex, count - 1);
						futureActive.Insert(addIndex, true);
						futureIndex.Insert(insertIndex, count - 1);
						futureActive.Insert(insertIndex, true);	
						futureSections[selectedSection][0] += 2;
						IncreaseFurtherSections();
						IncreaseFurtherSections();

						future.path2 = taken.path;

						future.liveEndX = x + lx;
						future.liveEndY = y + ly;
						T("Extending future end");
						if (!ExtendFutureLine(false, addIndex + 1, insertIndex, false))
						{
							possibleDirections.Add(new int[] { });
							DrawPath();
							possibleDirections.RemoveAt(possibleDirections.Count - 1);

							M("C shape future line cannot be completed at " + taken.path.Count + " x " + taken.path[taken.path.Count - 1][0] + " y " + taken.path[taken.path.Count - 1][1]);

							PreviousStepWithFuture();

							//find an example
							return false;
						}						
					}

					//turn right, pattern goes upwards
					int tempdx = dx;
					int tempdy = dy;
					dx = -lx;
					dy = -ly;
					lx = tempdx;
					ly = tempdy;
				}

				//mirror directions
				dx = thisDx;
				dy = thisDy;
				lx = -thisLx; //equal to rx and ry
				ly = -thisLy;
			}

			//check 3x1 for the previous step. If the live end went beyond, a field needs to be added to future

			future.searchReverse = true;
			future.searchStartIndex = future.path.Count + 1; // 2 will be subtracted

			x = taken.path[count - 1][0];
			y = taken.path[count - 1][1];
			dx = x - taken.path[count - 2][0];
			dy = y - taken.path[count - 2][1];

			for (int i = 0; i < 4; i++)
			{
				if (directions[i][0] == dx && directions[i][1] == dy)
				{
					int newIndex = (i == 3) ? 0 : i + 1;
					lx = directions[newIndex][0];
					ly = directions[newIndex][1];
				}
			}

			thisDx = dx;
			thisDy = dy;
			thisLx = lx;
			thisLy = ly;

			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{					
					if (taken.InTaken(x - 2 * dx + lx, y - 2 * dy + ly) && taken.InTaken(x - 3 * dx + 2 * lx, y - 3 * dy + 2 * ly) && taken.InTaken(x - 3 * dx + 3 * lx, y - 3 * dy + 3 * ly) && taken.InTaken(x - 3 * dx + 4 * lx, y - 3 * dy + 4 * ly) && InFutureStart(x - 2 * dx + 5 * lx, y - 2 * dy + 5 * ly)
						&& !taken.InTaken(x - dx + lx, y - dy + ly) && !taken.InTaken(x - 2 * dx + 2 * lx, y - 2 * dy + 2 * ly) && !taken.InTaken(x - 2 * dx + 3 * lx, y - 2 * dy + 3 * ly) && !taken.InTaken(x - 2 * dx + 4 * lx, y - 2 * dy + 4 * ly))
					{
						T("1x3 reverse future valid at x " + x + " y " + y + " selectedSection " + selectedSection);

						int addIndex = futureSections[selectedSection][0] + 1;
						insertIndex = futureSections[selectedSection][1];						
						future.path.Insert(addIndex, new int[] { x - 2 * dx + 4 * lx, y - 2 * dy + 4 * ly });
						future.path.Insert(insertIndex, new int[] { x - 2 * dx + 6 * lx, y - 2 * dy + 6 * ly });
						futureIndex.Insert(addIndex, count - 1);
						futureActive.Insert(addIndex, true);
						futureIndex.Insert(insertIndex, count - 1);
						futureActive.Insert(insertIndex, true);
						futureSections[selectedSection][0] += 2;
						IncreaseFurtherSections();
						IncreaseFurtherSections();

						future.path2 = taken.path;

						future.liveEndX = x - 2 * dx + 4 * lx;
						future.liveEndY = y - 2 * dy + 4 * ly;

						if (!ExtendFutureLine(false, addIndex + 1, insertIndex, false))
						{
							possibleDirections.Add(new int[] { });
							DrawPath();
							possibleDirections.RemoveAt(possibleDirections.Count - 1);

							M("1x3 reverse future line cannot be completed");

							PreviousStepWithFuture();

							//find an example
							return false;
						}
					}

					//turn right, pattern goes upwards
					int tempdx = dx;
					int tempdy = dy;
					dx = -lx;
					dy = -ly;
					lx = tempdx;
					ly = tempdy;
				}

				//mirror directions
				dx = thisDx;
				dy = thisDy;
				lx = -thisLx; //equal to rx and ry
				ly = -thisLy;
			}

			taken.path2 = future.path;

			dx = thisDx;
			dy = thisDy;
			lx = thisLx;
			ly = thisLy;
			int rx = -lx;
			int ry = -ly;

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
					future.liveEndX = x;
					future.liveEndY = y;
					selectedSection = loopSelectedSection;
					ExtendFutureLine(true, startIndex, -1, true);					
				}
				else
				{
					T("Turning off closed loop");
				}	
			}
			else if (InFutureStart(x + dx, y + dy)) //selectedSection is set here
			{
				//how to decide when we are entering a loop?
				//implement right side

				future.liveEndX = -1; //live end can be now disregarded when checking C shape, it wlll go the other way
				future.liveEndY = -1;
				int startIndex = futureSections[selectedSection][0];
				int endIndex = futureSections[selectedSection][1];

				T("");
				T("Taken connecting to future, selectedSection " + selectedSection);

				//in order to have a loop, left and right fields should be empty, also at 1 step forward. Suppose the latter is true, since we are connecting to the start of a future line. Counter-example?
				if (!taken.InTaken(x + lx, y + ly) && !future.InTakenAll(x + lx, y + ly) && !taken.InTaken(x + rx, y + ry) && !future.InTakenAll(x + rx, y + ry))
				{
					startIndex++;

					//Suppose the end of the future line is 2 to the left or right of the start end, as in 0415
					if (future.path[endIndex][0] == x + 2 * rx + dx && future.path[endIndex][1] == y + 2 * ry + dy)
					{
						future.path.Insert(startIndex, new int[] { x + dx + lx, y + dy + ly });
					} 
					else
					{
						future.path.Insert(startIndex, new int[] { x + lx + dx, y + ly + dy});						
					}
					futureIndex.Insert(startIndex, count - 1);
					futureActive.Insert(startIndex, true);
					futureSections[selectedSection][0] += 1;
					IncreaseFurtherSections();

					future.path2 = taken.path;

					//only extend the far end. The near end has to be extended simultaneously with the live end.
					if (!ExtendFutureLine(false, -1, endIndex, true))
					{
						possibleDirections.Add(new int[] { });
						DrawPath();
						possibleDirections.RemoveAt(possibleDirections.Count - 1);
							
						M("Closing loop, future line cannot be completed");

						PreviousStepWithFuture();

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
			return true;
		}

		private bool ExtendFutureLine(bool directionReverse, int nearEndIndex, int farEndIndex, bool once)
		{
			int stepCount = 0;
			
			/*if (selectedSection == -1)
			{
				int i = 0;
				foreach (int[] section in futureSections)
				{
					if (section[0] == nearEndIndex && section[1] == farEndIndex)
					{
						selectedSection = i;
					}
					i++;
				}
			}	*/		

			do
			{
				int index = (directionReverse)?nearEndIndex:farEndIndex;
				T("ExtendFuture directionReverse " + directionReverse + " index " + index + " nearEndIndex " + nearEndIndex + " farEndIndex " + farEndIndex);
				future.NextStepPossibilities(directionReverse, index);

				//T("ExtendFutureLine, future.possible.Count " + future.possible.Count);
				
				if (future.possible.Count == 1)
				{
					int[] newField = future.possible[0];

					future.x = newField[0];
					future.y = newField[1];

					//is counting area needed?					

					//we might be connecting to an older or newer section now
					bool found = false;
					foreach (int[] field in future.path)
					{
						int fx = field[0];
						int fy = field[1];
						if (future.x == fx && future.y == fy) { found = true; break; }
					}

					if (found) return true;									

					nearEndIndex++;
					futureSections[selectedSection][0]++;
					//increase further future line sections (we have stepped on them already or, in case of connecting to a loop, they are still active)
					IncreaseFurtherSections();

					T("Adding future: " + future.x + " " + future.y + " start of section " + futureSections[selectedSection][0]);
					foreach (int[] section in futureSections)
					{
						T("--- Section: " + section[0] + " " + section[1]);
					}

					if (directionReverse)
					{
						T("Inserting nearEndIndex: " + nearEndIndex);
						future.path.Insert(nearEndIndex, new int[] { future.x, future.y });
						futureIndex.Insert(nearEndIndex, taken.path.Count - 1);
						futureActive.Insert(nearEndIndex, true);
					}
					else
					{
						T("Inserting farEndIndex: " + farEndIndex);
						future.path.Insert(farEndIndex, new int[] { future.x, future.y });
						futureIndex.Insert(farEndIndex, taken.path.Count - 1);
						futureActive.Insert(farEndIndex,true);	
						// for use when stepping on a future line and extending the other end.
						inFutureIndex++;
					}
					T("nearEndIndex: " + nearEndIndex);
					stepCount++;
				}
				else if (future.possible.Count > 1)
				{
					foreach (int[] field in future.possible)
					{
						T(field[0] + " " + field[1]);
					}
				}
			} while (future.possible.Count == 1);

			if (future.possible.Count == 0) T("Possible count: 0");
				if (future.possible.Count == 0)	return false;

			T("Future path has multiple choice");

			if (stepCount == 0) return true;

			/*foreach (int[] field in future.possible)
			{
				T(field[0] + " " + field[1]);
			}*/

			if (!once)
			{
				future.liveEndX = future.x;
				future.liveEndY = future.y;
				if (directionReverse)
				{
					T("Start from far end");					
					return ExtendFutureLine(false, nearEndIndex, farEndIndex, false);
				}
				else
				{
					T("Start from near end");
					return ExtendFutureLine(true, nearEndIndex, farEndIndex, false);
				}
			}
			return true;
			
		}

		private void IncreaseFurtherSections()
		{
			for (int i = selectedSection + 1; i < futureSections.Count; i++)
			{
				futureSections[i][0]++;
				futureSections[i][1]++;
			}
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

				if (field[0] == x && field[1] == y)
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

			for (int i = c - 1; i >= 0; i--)
			{
				int[] field = future.path[i];
				if (field[0] == x && field[1] == y)
				{
					foundIndex = i;
				}
			}

			if (foundIndex == -1) return false;

			foreach (int[] section in futureSections)
			{
				if (section[1] == foundIndex)
				{
					foundEndIndex = foundIndex;
					return true;
				}				
			}

			return false;
		}

		private void DrawPath()
		{
			string startPos = 0 + " " + 0.5;
			string startPosFuture = "";
			string path = "";
			string pathFuture = "";

			float posX = 0;
			float posY = 0.5f;
			int startX = 1;
			int startY = 1;
			string backgrounds = "\t<rect width=\"1\" height=\"1\" x=\"" + (startX - 1) + "\" y=\"" + (startY - 1) + "\" fill=\"#ff0000\" fill-opacity=\"0.15\" />\r\n";
			savePath = "1-" + startX + "," + startY + ";";

			for (int i = 1; i < taken.path.Count; i++)
			{
				int[] field = taken.path[i];
				int newX = field[0];
				int newY = field[1];

				string color = "#ff0000";
				string opacity = "0.15";
				if (exitIndex.Count > 0 && i > exitIndex[exitIndex.Count - 1])
				{
					color = "#000000";
					opacity = "0.25";
				}
				backgrounds += "\t<rect width=\"1\" height=\"1\" x=\"" + (newX - 1) + "\" y=\"" + (newY - 1) + "\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\r\n";

				foreach (int direction in possibleDirections[i])
				{
					savePath += direction + ",";
				}
				savePath = savePath.Substring(0, savePath.Length - 1);
				savePath += "-" + newX + "," + newY + ";";

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

			string futureBackgrounds = "";
			string futurePath = "";
			startX = -1;
			startY = -1;

			foreach (int[] section in futureSections)
			{
				T("--- Section: " + section[0] + " " + section[1]);
			}

			bool prevStartItem = false;

			if (future.path.Count != 0)
			{
				for (int i = future.path.Count - 1; i >= 0; i--)
				{
					bool startItem = false;
					bool endItem = false;

					foreach (int[] section in futureSections)
					{
						if (i == section[0])
						{
							startItem = true;
						}
						if (i == section[1])
						{
							endItem = true;
						}
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
					int newX = field[0];
					int newY = field[1];

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
						futurePath += "\t<path d=\"M " + startPosFuture + "\r\n" + pathFuture + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"blue\" stroke-width=\"0.05\" />\r\n";
					}
				}
			}

			string possibles = "";
			if (possibleDirections.Count > 1)
			{
				foreach (int direction in possibleDirections[possibleDirections.Count - 1])
				{
					savePath += direction + ",";

					possibles += "\t<rect width=\"1\" height=\"1\" x=\"" + (taken.path[taken.path.Count - 1][0] - 1 + directions[direction][0]) + "\" y=\"" + (taken.path[taken.path.Count - 1][1] - 1 + directions[direction][1]) + "\" fill=\"#000000\" fill-opacity=\"0.1\" />\r\n";
				}
				savePath = savePath.Substring(0, savePath.Length - 1);
			}
			else
			{
				savePath = savePath.Substring(0, savePath.Length - 1);
			}

			ExitCoords.Text = "";
			if (exits.Count > 0)
			{
				savePath += ":";

				for (int i = 0; i < exits.Count; i++)
				{
					int[] field = exits[i];
					int x = field[0];
					int y = field[1];
					backgrounds += "\t<rect width=\"1\" height=\"1\" x=\"" + (x - 1) + "\" y=\"" + (y - 1) + "\" fill=\"#00ff00\" fill-opacity=\"0.4\" />\r\n";
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
			File.WriteAllText("test.svg", content);
			File.WriteAllText("savePath_temp.txt", savePath);
			Canvas.InvalidateVisual();
		}

		private void SKElement_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			var canvas = e.Surface.Canvas;
			canvas.Clear(SKColors.White);

			var svg = new SkiaSharp.Extended.Svg.SKSvg();
			var picture = svg.Load("test.svg");

			var fit = e.Info.Rect.AspectFit(svg.CanvasSize.ToSizeI());
			e.Surface.Canvas.Scale(fit.Width / svg.CanvasSize.Width);
			e.Surface.Canvas.DrawPicture(picture);
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
		public static extern short GetKeyState(int keyCode);

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			bool CapsLock = (((ushort)GetKeyState(0x14)) & 0xffff) != 0;

			if (e.Key == Key.Enter)
			{
				Reload_Click(new object(), new RoutedEventArgs());
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

		private void M(object o)
		{
			MessageBox.Show(o.ToString());
		}

		private void T(object o)
		{
			Trace.WriteLine(o.ToString());
		}
	}
}