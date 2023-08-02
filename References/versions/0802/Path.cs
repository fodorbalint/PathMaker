using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace SvgApp
{
	internal class Path
	{
		MainWindow window;
		int size;
		public List<int[]> path;
		public List<int[]>? path2; //additional forbidden fields
		bool isMain = true;
		public int searchStartIndex = -1;
		public bool searchReverse = false;
		public int liveEndX = -1;
		public int liveEndY = -1;
		List<int[]> foundPath = new List<int[]>();
		public int count;
		public List<int[]> possible = new List<int[]>(); //field coordinates
		List<int[]> forbidden = new List<int[]>();
		public bool nearField;
		public bool circleDirectionLeft = true;
		public int x, y;
		public int dx, dy, lx, ly, rx, ry; //based on an up direction, these indicate left and right coordinates.		
		int[] straightField = new int[] { };
		int[] leftField = new int[] { };
		int[] rightField = new int[] { };
		List<int[]> directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } }; //down, right, up, left
		int[] selectedDirection = new int[] { };
		bool CShape;

		public Path(MainWindow window, int size, List<int[]> path, List<int[]>? path2, bool isMain)
		{
			this.window = window;
			this.size = size;
			count = path.Count;
			if (count > 0)
			{
				x = path[count - 1][0];
				y = path[count - 1][1];
			}
			else
			{
				x = 1;
				y = 1;
			}
			this.path = path;
			this.path2 = path2;
			this.isMain = isMain;
		}

		public void NextStepPossibilities(bool searchReverse = true, int startIndex = -1)
		{
			nearField = false;

			possible = new List<int[]>();
			forbidden = new List<int[]>();

			count = path.Count;
			if (count < 2)
			{
				possible.Add(new int[] { 2, 1 });
				possible.Add(new int[] { 1, 2 });
			}
			else
			{
				int x0, y0;
				this.searchReverse = searchReverse;
				if (startIndex != -1) //for checking future lines
				{
					T("NextSteppossibilities startIndex " + startIndex + " path.Count " + path.Count + " sR " + searchReverse);
					if (!searchReverse)
					{
						//extend far end
						x = path[startIndex][0];
						y = path[startIndex][1];
						x0 = path[startIndex + 1][0];
						y0 = path[startIndex + 1][1];
					}
					else
					{
						//extend near end
						x = path[startIndex][0];
						y = path[startIndex][1];
						x0 = path[startIndex - 1][0];
						y0 = path[startIndex - 1][1];
					}
					
					searchStartIndex = startIndex;
				}
				else
				{
					T("NextSteppossibilities normal x " + x + " y " + y + " path2.Count " + path2.Count);
					searchStartIndex = count - 1;
					x0 = path[count - 2][0];
					y0 = path[count - 2][1];
				}				

				int i;
				for (i = 0; i < 4; i++)
				{
					//last movement: down, right, up, left
					dx = directions[i][0];
					dy = directions[i][1];

					if (x - x0 == dx && y - y0 == dy)
					{
						selectedDirection = directions[i];

						if (i == 0)
						{
							lx = directions[1][0];
							ly = directions[1][1];
							rx = directions[3][0];
							ry = directions[3][1];
						}
						else if (i == 3)
						{
							lx = directions[0][0];
							ly = directions[0][1];
							rx = directions[2][0];
							ry = directions[2][1];
						}
						else
						{
							lx = directions[i + 1][0];
							ly = directions[i + 1][1];
							rx = directions[i - 1][0];
							ry = directions[i - 1][1];
						}

						straightField = new int[] { x + dx, y + dy };
						leftField = new int[] { x + lx, y + ly };
						rightField = new int[] { x + rx, y + ry };

						T("x " + x + " y " + y + " InTakenF(straightField) " + InTakenF(straightField) + " InTakenF(rightField) " + InTakenF(rightField) + " InTakenF(leftField) " + InTakenF(leftField));
						
						if (!InTakenAllF(straightField) && !InBorderF(straightField))
						{
							T("possible straight");
							possible.Add(straightField);
						}
						if (!InTakenAllF(rightField) && !InBorderF(rightField))
						{
							T("possible right");
							possible.Add(rightField);
						}
						if (!InTakenAllF(leftField) && !InBorderF(leftField))
						{
							T("possible left");
							possible.Add(leftField);
						}

						// A future line may connect to another section as in 0714_2 when we step up, and a 2x2 line is created on the left
						// For connecting to an older line, see 0730
						if (!isMain)
						{
							if (!searchReverse && InFutureOwnStartF(straightField) || searchReverse && InFutureOwnEndF(straightField))
							{
								T("possible straight 2");
								possible.Add(straightField);
							}
							//Are there cases where there is a future line start on the left or the right? Unless I find one, adding these may connect the current future line back to its own start.
							/*if (InFutureOwnStartF(leftField))
							{
								possible.Add(leftField);
							}
							if (InFutureOwnStartF(rightField))
							{
								possible.Add(rightField);
							}*/
						} 

						//check for an empty cell next to the position, and a taken one further. In case of a C shape, we need to fill the middle. The shape was formed by the previous 5 steps.
						T("possible.Count " + possible.Count);							
						Check1x3();
						Check3x3();
						CShape = false;
						CheckNearFieldCommon();
						T("forbidden.Count after CheckNearFieldCommon " + forbidden.Count);
						if (isMain)
						{
							CheckNearBorder(); // Future line shouldn't be checked for border. See 0714
							//T("forbidden.Count before CheckNearField " + forbidden.Count);
							CheckNearField();
							//T("forbidden.Count after CheckNearField " + forbidden.Count);
							CheckNearFutureStartEnd();
							//T("forbidden.Count after CheckNearFutureStartEnd " + forbidden.Count);
							CheckNearFutureSide();
							//T("forbidden.Count after CheckNearFutureSide " + forbidden.Count);
							CheckLeftRightFuture();
							//0630
							CheckNearFutureEnd();
						}
						break;
					}
				}
			}

			List<int[]> newPossible = new List<int[]>();

			foreach (int[] field in possible)
			{
				if (!InForbidden(field))
				{
					newPossible.Add(field);
				}
			}

			possible = newPossible;			
		}

		public void CheckNearBorder()
		{
			int directionIndex = Math.Abs(selectedDirection[0]); //0 for vertical, 1 for horizontal

			/* duplicate code with CheckNearFieldCommon
			if (leftField[directionIndex] == 1 && !InTakenF(leftField))
			{
				if (!InTaken(x - 1, y - 1))
				{
					forbidden.Add(leftField);
				}
				else
				{
					forbidden.Add(straightField);
					forbidden.Add(rightField);
				}
			}
			else if (rightField[directionIndex] == 1 && !InTakenF(rightField))
			{
				if (!InTaken(x - 1, y - 1))
				{
					forbidden.Add(rightField);
				}
				else
				{
					forbidden.Add(straightField);
					forbidden.Add(leftField);
				}
			}*/
			if (leftField[directionIndex] == size && !InTakenF(leftField))
			{
				if (directionIndex == 0) //going down on right side
				{
					forbidden.Add(straightField);
					forbidden.Add(rightField);
				}
				else //going left on down side
				{
					if (!InTaken(x - 1, y + 1) && !InBorder(x - 1, y + 1)) //InBorder checking is necessary when x=1
					{
						forbidden.Add(leftField);
					}
					else if (!InTaken(x, y + 1)) //bottom may be already filled, and we had an u-turn longer right
					{
						forbidden.Add(straightField);
						forbidden.Add(rightField);
					}
				}
			}
			else if (rightField[directionIndex] == size && !InTakenF(rightField))
			{
				if (directionIndex == 0) //going up on right side
				{
					if (!InTaken(x + 1, y - 1) && !InBorder(x + 1, y - 1))
					{
						forbidden.Add(rightField);
					}
					else if (!InTaken(x + 1, y)) //side may be already filled, and we had an u-turn longer down
					{
						forbidden.Add(straightField);
						forbidden.Add(leftField);
					}
				}
				else //going right on down side
				{
					forbidden.Add(straightField);
					forbidden.Add(leftField);
				}
			}

			if (x + 2 * dx == 0)
			{
				forbidden.Add(leftField);
				if (!InTaken(x - 1, y - 1))
				{
					forbidden.Add(straightField);

					AddExit(x - 1, y - 1);
					circleDirectionLeft = false;

				}
			}
			else if (x + 2 * dx == size + 1)
			{
				forbidden.Add(rightField);
				if (!InTaken(x + 1, y - 1) && y != 1)
				{
					forbidden.Add(straightField);

					AddExit(x + 1, y - 1);
					circleDirectionLeft = true;
				}
			}
			else if (y + 2 * dy == 0)
			{
				forbidden.Add(rightField);
				if (!InTaken(x - 1, y - 1))
				{
					forbidden.Add(straightField);

					AddExit(x - 1, y - 1);
					circleDirectionLeft = true;
				}
			}
			else if (y + 2 * dy == size + 1)
			{
				forbidden.Add(leftField);
				if (!InTaken(x - 1, y + 1) && x != 1)
				{
					forbidden.Add(straightField);

					AddExit(x - 1, y + 1);
					circleDirectionLeft = false;
				}
			}
		}

		public void CheckNearFieldCommon()
		{
			if ((InTaken(x + 2 * lx, y + 2 * ly) || InBorder(x + 2 * lx, y + 2 * ly)) && !InTakenF(leftField)) //2 to left
			{
				T("2 to left");
				if (InTaken(x + lx - dx, y + ly - dy)) //C shape that needs to be filled, unless there is a live end nearby that can fill it
				{
					T("C shape liveEndX " + liveEndX + " " + liveEndY);
					if (!(liveEndX == leftField[0] && Math.Abs(liveEndY - leftField[1]) == 1 || liveEndY == leftField[1] && Math.Abs(liveEndX - leftField[0]) == 1))
					{
						// The future line does not create a C shape if the end of the main line is the empty field to the left. 
						// 0620_2, one step forward, the near end extends. 
						if (!(path2.Count != 0 && leftField[0] == path2[path2.Count - 1][0] && leftField[1] == path2[path2.Count - 1][1]))
						{
							T("C shape left");
							forbidden.Add(straightField);
							forbidden.Add(rightField);
							CShape = true; // left/right across checking will be disabled, no exits needed
						}
					}
				}
				/*else if (!InTaken(x + lx + dx, y + ly + dy)) //if not upside down C
				{
					//lower left corner
					if (!(x == 1 && (y + ly) == size))
					{
						forbidden.Add(leftField);
					}
				}*/
			}

			if ((InTaken(x + 2 * rx, y + 2 * ry) || InBorder(x + 2 * rx, y + 2 * ry)) && !InTakenF(rightField)) //2 to right
			{
				T("2 to right");
				if (InTaken(x + rx - dx, y + ry - dy))
				{
					//if the right field is not next to the other live end of the future path
					if (!(liveEndX == rightField[0] && Math.Abs(liveEndY - rightField[1]) == 1 || liveEndY == rightField[1] && Math.Abs(liveEndX - rightField[0]) == 1))
					{
						if (!(path2.Count != 0 && rightField[0] == path2[path2.Count - 1][0] && rightField[1] == path2[path2.Count - 1][1]))
						{
							T("C shape right");
							forbidden.Add(straightField);
							forbidden.Add(leftField);
							CShape = true;
						}
					}
				}
				/*else if (!InTaken(x + rx + dx, y + ry + dy))
				{
					//upper right corner
					if (!(y == 1 && (x + rx) == size))
					{
						T("forbidden.Count " + y);
						forbidden.Add(rightField);
					}
				}*/
			}

			//Even future line can make this C shape, see 0727_1
			if (InTaken(x + 2 * dx, y + 2 * dy) && !InTakenF(straightField) && (InTaken(x + dx + lx, y + dy + ly) || InTaken(x + dx + rx, y + dy + ry))) //C shape straight
			{
				if (!(liveEndX == straightField[0] && Math.Abs(liveEndY - straightField[1]) == 1 || liveEndY == straightField[1] && Math.Abs(liveEndX - straightField[0]) == 1))
				{
					T("C shape straight");
					forbidden.Add(leftField);
					forbidden.Add(rightField);
					CShape = true;
				}
			}
		}

		public void CheckNearField()
		{ 
			// o
			//xo
			//xox

			if (!CShape)
			{
				if (InTaken(x + 2 * lx - dx, y + 2 * ly - dy) && !InTakenF(leftField) && !InTaken(x + lx - dx, y + ly - dy) && !InTaken(x + lx + dx, y + ly + dy))
				{
					forbidden.Add(leftField);
				}
				if (InTaken(x + 2 * rx - dx, y + 2 * ry - dy) && !InTakenF(rightField) && !InTaken(x + rx - dx, y + ry - dy)! && !InTaken(x + rx + dx, y + ry + dy))
				{
					forbidden.Add(rightField);
				}

				if (InTaken(x + 2 * lx + dx, y + 2 * ly + dy) && !InTakenF(leftField) && !InTaken(x + lx + dx, y + ly + dy) && !(path2 != null && leftField[0] == path2[path2.Count - 1][0] && leftField[1] == path2[path2.Count - 1][1]))
				{
					if (InTaken(x + lx - dx, y + ly - dy) && InTaken(x + 2 * lx, y + 2 * ly))
					{
						forbidden.Add(straightField);
						forbidden.Add(rightField);
					}
					else
					{
						forbidden.Add(leftField);
						T("Adding left to forbidden");
					}
				}
				if (InTaken(x + 2 * rx + dx, y + 2 * ry + dy) && !InTakenF(rightField) && !InTaken(x + rx + dx, y + ry + dy) && !(path2 != null && rightField[0] == path2[path2.Count - 1][0] && rightField[1] == path2[path2.Count - 1][1]))
				{
					if (InTaken(x + rx - dx, y + ry - dy) && InTaken(x + 2 * rx, y + 2 * ry))
					{
						forbidden.Add(straightField);
						forbidden.Add(leftField);
					}
					else
					{
						forbidden.Add(rightField);
					}
				}
			}			

			foreach (int[] field in forbidden)
			{
				T("CheckNearField forbidden " + field[0] + " " + field[1]);
			}

			if (!CShape)
			{
				int directionIndex = Math.Abs(selectedDirection[0]); //0 for vertical, 1 for horizontal

				if (InTaken(x + lx + 2 * dx, y + ly + 2 * dy) &&
					!InTakenF(straightField) &&
					!InTaken(x + lx + dx, y + ly + dy))
				{
					int index = InTakenIndex(x + lx + 2 * dx, y + ly + 2 * dy);
					T("Left mid across field: x " + (x + lx + 2 * dx) + " y " + (y + ly + 2 * dy) + " x " + x + " y " + y);

					int[] nextField;
					int[] prevField;
					if (isMain)
					{
						if (foundPath == path2)
						{
							nextField = foundPath[index - 1];
							prevField = foundPath[index + 1];
						}
						else
						{
							nextField = foundPath[index + 1];
							prevField = foundPath[index - 1];
						}
					}
					else
					{
						nextField = foundPath[index + 1];
						prevField = foundPath[index - 1];
					}

					forbidden.Add(straightField);

					AddExit(x + lx + dx, y + ly + dy);

					int firstConditionValue, secondConditionValue;
					if (directionIndex == 0)
					{
						firstConditionValue = x + 2 * lx;
						secondConditionValue = x;
					}
					else
					{
						firstConditionValue = y + 2 * ly;
						secondConditionValue = y;
					}

					if (nextField[directionIndex] == firstConditionValue) //left
					{
						forbidden.Add(rightField);
						circleDirectionLeft = true;
					}
					else if (nextField[directionIndex] == secondConditionValue) //right
					{
						forbidden.Add(leftField);
						circleDirectionLeft = false;
					}
					else //up
					{
						T("Left mid across, next field up");

						if (prevField[directionIndex] == firstConditionValue) //from left
						{
							T("Left mid across, next field up from left");
							forbidden.Add(leftField);
							circleDirectionLeft = false;
						}
						else //from right
						{
							T("Left mid across, next field up from right");
							forbidden.Add(rightField);
							circleDirectionLeft = true;
						}
					}
				}
				else if (InTaken(x + rx + 2 * dx, y + ry + 2 * dy) &&
					!InTakenF(straightField) &&
					!InTaken(x + rx + dx, y + ry + dy))
				{
					int index = InTakenIndex(x + rx + 2 * dx, y + ry + 2 * dy);
					T("Right mid across field: x " + (x + rx + 2 * dx) + " y " + (y + ry + 2 * dy) + " x " + x + " y " + y);

					int[] nextField;
					int[] prevField;
					if (isMain)
					{
						if (foundPath == path2)
						{
							nextField = foundPath[index - 1];
							prevField = foundPath[index + 1];
						}
						else
						{
							nextField = foundPath[index + 1];
							prevField = foundPath[index - 1];
						}
					}
					else
					{
						nextField = foundPath[index + 1];
						prevField = foundPath[index - 1];
					}

					forbidden.Add(straightField);

					AddExit(x + rx + dx, y + ry + dy);
					int firstConditionValue, secondConditionValue;
					if (directionIndex == 0)
					{
						firstConditionValue = x + 2 * rx;
						secondConditionValue = x;
					}
					else
					{
						firstConditionValue = y + 2 * ry;
						secondConditionValue = y;
					}

					if (nextField[directionIndex] == firstConditionValue) //right
					{
						forbidden.Add(leftField);
						circleDirectionLeft = false;
					}
					else if (nextField[directionIndex] == secondConditionValue) //left
					{
						forbidden.Add(rightField);
						circleDirectionLeft = true;
					}
					else //up
					{
						if (prevField[directionIndex] == firstConditionValue) //from right
						{
							forbidden.Add(rightField);
							circleDirectionLeft = true;
						}
						else //from left
						{
							//check C shape
							forbidden.Add(leftField);
							circleDirectionLeft = false;
						}
					}
				}
				else
				{
					int firstConditionValue, secondConditionValue;
					if (directionIndex == 0)
					{
						firstConditionValue = y + 3 * dy;
						secondConditionValue = y + dy;
					}
					else
					{
						firstConditionValue = x + 3 * dx;
						secondConditionValue = x + dx;
					}

					if (InTaken(x + 2 * lx + 2 * dx, y + 2 * ly + 2 * dy) &&
						!InTakenF(straightField) &&
						!InTaken(x + lx + dx, y + ly + dy) &&
						!InTakenF(leftField))
					{
						int index = InTakenIndex(x + 2 * lx + 2 * dx, y + 2 * ly + 2 * dy);

						T("Left across field: x " + (x + 2 * lx + 2 * dx) + " y " + (y + 2 * ly + 2 * dy) + " x " + x + " y " + y + " " + InTaken(x + 2 * lx + 2 * dx, y + 2 * ly + 2 * dy) + " index " + index);

						int[]? nextField = null;
						int[] prevField;
						if (isMain)
						{
							if (foundPath == path2)
							{
								if (index != 0) //not the far end of the future line
								{
									nextField = foundPath[index - 1];
								}								
								prevField = foundPath[index + 1];
							}
							else
							{
								nextField = foundPath[index + 1];
								prevField = foundPath[index - 1];
							}
						}
						else
						{
							nextField = foundPath[index + 1];
							prevField = foundPath[index - 1];
						}

						T("Adding exit at " + (x + lx + dx) + " " + (y + ly + dy));
						T("nextfield " + nextField[0] + " " + nextField[1]);
						AddExit(x + lx + dx, y + ly + dy);

						if (nextField != null && nextField[1 - directionIndex] == firstConditionValue) //up
						{
							T("up");
							if (isMain)
							{
								forbidden.Add(leftField);
								circleDirectionLeft = false;
							}
						}
						else //right
						{
							T("right");
							if (index != 0) //not the start field
							{
								if (prevField[1 - directionIndex] == secondConditionValue) // from down
								{
									T("forbid left");
									forbidden.Add(leftField);
									circleDirectionLeft = false;
								}
								else
								{
									T("forbid straight and right");
									forbidden.Add(straightField);
									forbidden.Add(rightField);
									circleDirectionLeft = true;
								}
							}
							else
							{
								T("forbid straight and right2");
								forbidden.Add(straightField);
								forbidden.Add(rightField);
								circleDirectionLeft = true;
							}
						}
					}

					if (InTaken(x + 2 * rx + 2 * dx, y + 2 * ry + 2 * dy) &&
					!InTakenF(straightField) &&
					!InTaken(x + rx + dx, y + ry + dy) &&
					!InTakenF(rightField))
					{
						int index = InTakenIndex(x + 2 * rx + 2 * dx, y + 2 * ry + 2 * dy);

						T("Right across field: x " + (x + 2 * rx + 2 * dx) + " y " + (y + 2 * ry + 2 * dy) + " x " + x + " y " + y + " isMain " + isMain + " index " + index);

						int[]? nextField = null;
						int[] prevField;
						if (isMain)
						{
							if (foundPath == path2)
							{
								if (index != 0) //not the far end of the future line
								{
									nextField = foundPath[index - 1];
								}
								prevField = foundPath[index + 1];
							}
							else
							{
								nextField = foundPath[index + 1];
								prevField = foundPath[index - 1];
							}
						}
						else
						{
							nextField = foundPath[index + 1];
							prevField = foundPath[index - 1];
						}

						AddExit(x + rx + dx, y + ry + dy);

						if (nextField != null && nextField[1 - directionIndex] == firstConditionValue) //up
						{
							if (isMain) //Restriction only valid for real line, because it has to come out of the enclosed area. Future line is the line coming out. See 0415_1.
							{
								forbidden.Add(rightField);
								circleDirectionLeft = true;
							}
						}
						else //left
						{
							if (index != 0) //not the start field
							{
								if (prevField[1 - directionIndex] == secondConditionValue) // from down
								{
									forbidden.Add(rightField);
									circleDirectionLeft = true;
								}
								else
								{
									forbidden.Add(straightField); //cannot go up or left
									forbidden.Add(leftField);
									circleDirectionLeft = false;
								}
							}
							else
							{
								forbidden.Add(straightField); //cannot go up or left
								forbidden.Add(leftField);
								circleDirectionLeft = false;
							}
						}
					}
				}
			}
		}

		private void Check1x3()
		{
			//Certain edges are impossible to fill.

			//xo
			// xoo?x
			//  xxxx

			//next step has to be left

			int dx = this.dx;
			int dy = this.dy;
			int lx = this.lx;
			int ly = this.ly;

			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					if (InTaken(x - dx + lx, y - dy + ly) && InTaken(x - dx + 2 * lx, y - dy + 2 * ly) && InTaken(x - dx + 3 * lx, y - dy + 3 * ly)
					&& InTaken(x + 4 * lx, y + 4 * ly) && InTaken(x + dx + 5 * lx, y + dy + 5 * ly)
					&& !InTaken(x + lx, y + ly) && !InTaken(x + 2 * lx, y + 2 * ly) && !InTaken(x + 3 * lx, y + 3 * ly)
					&& !InTaken(x + dx + 4 * lx, y + dy + 4 * ly))
					{
						T("1x3 valid at x " + x + " y " + y + " j " + i + " j " + j + " dx " + dx + " dy " + dy + " lx " + lx + " ly " + ly);
						forbidden.Add(new int[] { x + dx, y + dy }); //straight field
						forbidden.Add(new int[] { x - lx, y - ly }); //right field (per the start direction)
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
				dx = this.dx;
				dy = this.dy;
				lx = -this.lx; //equal to rx and ry
				ly = -this.ly;
			}
		}

		private void Check3x3()
		{
			// x
			//xo
			//xo
			//xoo?x
			// xxxx

			//next step has to be left

			int dx = this.dx;
			int dy = this.dy;
			int lx = this.lx;
			int ly = this.ly;

			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					if (InTaken(x - dx + lx, y - dy + ly) && InTaken(x - dx + 2 * lx, y - dy + 2 * ly) && InTaken(x - dx + 3 * lx, y - dy + 3 * ly)
					 && InTaken(x + 4 * lx, y + 4 * ly) && InTaken(x + dx + 4 * lx, y + dy + 4 * ly) && InTaken(x + 2 * dx + 4 * lx, y + 2 * dy + 4 * ly)
					 && InTaken(x + 3 * dx + 3 * lx, y + 3 * dy + 3 * ly)
					 && !InTaken(x + lx, y + ly) && !InTaken(x + 2 * lx, y + 2 * ly) && !InTaken(x + 3 * lx, y + 3 * ly)
					 && !InTaken(x + dx + 3 * lx, y + dy + 3 * ly) && !InTaken(x + 2 * dx + 3 * lx, y + 2 * dy + 3 * ly))
					{
						T("3x3 valid at x " + x + " y " + y);
						forbidden.Add(new int[] { x + dx, y + dy }); //straight field
						forbidden.Add(new int[] { x - lx, y - ly }); //right field (per the start direction)
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
				dx = this.dx;
				dy = this.dy;
				lx = -this.lx; //equal to rx and ry
				ly = -this.ly;
			}
		}

		//Example: savePath0305.txt
		public void CheckNearFutureSide() //suppose the side is of stair form
		{
			//mid cases are used when approaching an end, example: 0415_2
			//but in case of 0620, this is an error
			if (InFuture(x + 2 * dx, y + 2 * dy) && InFuture(x + rx + 2 * dx, y + ry + 2 * dy) && InFutureStart(x + rx + dx, y + ry + dy) && !InTakenF(leftField) && !InTakenF(straightField) && !InTakenF(rightField) && !InFutureF(leftField) && !InFutureF(straightField) && !InFutureF(rightField))
			{
				//touching the end at the straight right corner. Can the future line go in other direction?
				T("CheckNearFutureSide mid right");
				forbidden.Add(rightField);
				forbidden.Add(straightField);
			}
			else if (InFuture(x + 2 * dx, y + 2 * dy) && InFuture(x + lx + 2 * dx, y + ly + 2 * dy) && InFutureStart(x + lx + dx, y + ly + dy) && !InTakenF(leftField) && !InTakenF(straightField) && !InTakenF(rightField) && !InFutureF(leftField) && !InFutureF(straightField) && !InFutureF(rightField))
			{
				T("CheckNearFutureSide mid left");
				forbidden.Add(leftField);
				forbidden.Add(straightField);
			}
			else //check possible across fields. 2 across is in future, 1 across is free, plug 1 forward (0316 can otherwise go wrong). 1 to the side is also free, but we don't need to check it. Both sides can be true simultaneously, example: 0413
			//Condition has to hold right even in situations like 0425_1
			{
				//right across
				int i = InFutureIndex(x + 2 * rx + 2 * dx, y + 2 * ry + 2 * dy);

				if (i != -1 && !InFuture(x + rx + dx, y + ry + dy) && !InFuture(x + dx, y + dy) && !InTaken(x + rx + dx, y + ry + dy) && !InTaken(x + dx, y + dy) && !InFutureF(rightField))
				{
					//2 to right and 1 forward, plus 1 to right and 2 forward is also free
					if (!InFuture(x + 2 * rx + dx, y + 2 * ry + dy) && !InFuture(x + rx + 2 * dx, y + ry + 2 * dy))
					{
						T("CheckNearFutureSide across right");

						if (!InFutureStartIndex(i) && !InFutureEndIndex(i))
						{
							T("Not start of section");
							int[] nextField = path2[i - 1];
							//goes to right further
							if (nextField[0] == x + 3 * rx + 2 * dx &&
								nextField[1] == y + 3 * ry + 2 * dy)
							{
								T("Goes right");
								forbidden.Add(leftField);
								forbidden.Add(straightField);
							}
							//goes forward further
							else
							{
								T("Goes straight");
								forbidden.Add(rightField);
							}
						}
						// In case the across field is the start of a section, we can still go in all directions, it is possible to connect to it.
					}
					// we entered the stair from the start, as in 0305. The across field is going forward.
					// But in 0729, we cannot disable the straight field, because the left field is not free.
					else if (!CShape)
					{
						T("CheckNearFutureSide across right, entered at start");
						forbidden.Add(rightField);
						forbidden.Add(straightField);
					}
				}

				//left across
				i = InFutureIndex(x + 2 * lx + 2 * dx, y + 2 * ly + 2 * dy);

				if (i != -1 && !InFuture(x + lx + dx, y + ly + dy) && !InFuture(x + dx, y + dy) && !InTaken(x + lx + dx, y + ly + dy) && !InTaken(x + dx, y + dy) && !InFutureF(leftField))
				{
					if (!InFuture(x + 2 * lx + dx, y + 2 * ly + dy) && !InFuture(x + lx + 2 * dx, y + ly + 2 * dy))
					{
						T("CheckNearFutureSide across left");

						if (!InFutureStartIndex(i) && !InFutureEndIndex(i))
						{
							T("Not start of section");
							int[] nextField = path2[i - 1];
							//goes to right further
							if (nextField[0] == x + 3 * lx + 2 * dx &&
								nextField[1] == y + 3 * ly + 2 * dy)
							{
								T("Goes left");
								forbidden.Add(rightField);
								forbidden.Add(straightField);
							}
							//goes forward further
							else
							{
								T("Goes straight");
								forbidden.Add(leftField);
							}
						}
					}
					else if (!CShape)
					{
						T("CheckNearFutureSide across left, entered at start");
						forbidden.Add(leftField);
						forbidden.Add(straightField);
					}
				}
			}			
		}

		public void CheckNearFutureStartEnd()
		{			
			//meeting start and end ahead. Ends are 2 apart from each other at same forward distance.
			if (InFutureStart(x + 2 * dx, y + 2 * dy) && InFutureEnd(x + 2 * rx + 2 * dx, y + 2 * ry + 2 * dy) && !InTakenF(leftField) && !InTakenF(straightField) && !InTakenF(rightField))
			{
				T("CheckNearFutureStartEnd to right");
				forbidden.Add(rightField);
			}
			else if (InFutureStart(x + 2 * dx, y + 2 * dy) && InFutureEnd(x + 2 * lx + 2 * dx, y + 2 * ly + 2 * dy) && !InTakenF(leftField) && !InTakenF(straightField) && !InTakenF(rightField))
			{
				T("CheckNearFutureStartEnd to left");
				forbidden.Add(leftField);
			}
			else if (InFutureStart(x + lx + 2 * dx, y + ly + 2 * dy) && InFutureEnd(x + rx + 2 * dx, y + ry + 2 * dy) && !InTakenF(leftField) && !InTakenF(straightField) && !InTakenF(rightField))
			{
				T("CheckNearFutureStartEnd to middle, start on left");
				forbidden.Add(rightField);
				forbidden.Add(straightField);
			}
			else if (InFutureStart(x + rx + 2 * dx, y + ry + 2 * dy) && InFutureEnd(x + lx + 2 * dx, y + ly + 2 * dy) && !InTakenF(leftField) && !InTakenF(straightField) && !InTakenF(rightField))
			{
				T("CheckNearFutureStartEnd to middle, start on right");
				forbidden.Add(leftField);
				forbidden.Add(straightField);
			}
		}

		public void CheckLeftRightFuture()
		{
			//Due to previous extension, a future line may run at 2 distance to the left or right. If it is not the section start, and neither is the field one step behind and to the left/right, we must step there. Example: 0430_2, step one forward.
			if (InFuture(x + 2 * lx, y + 2 * ly) && !InFutureStart(x + 2 * lx, y + 2 * ly) && !InFutureEnd(x + 2 * lx, y + 2 * ly) && !InFutureStart(x + lx - dx, y + ly - dy) && !InTakenF(leftField) && !InFutureF(leftField))
			{
				T("CheckLeftRightFuture to left");
				forbidden.Add(rightField);
				forbidden.Add(straightField);
			}
			else if (InFuture(x + 2 * rx, y + 2 * ry) && !InFutureStart(x + 2 * rx, y + 2 * ry) && !InFutureEnd(x + 2 * rx, y + 2 * ry) && !InFutureStart(x + rx - dx, y + ry - dy) && !InTakenF(rightField) && !InFutureF(rightField))
			{
				T("CheckLeftRightFuture to right");
				forbidden.Add(leftField);
				forbidden.Add(straightField);
			}
		}

		private void CheckNearFutureEnd()
		{
			if (InFutureEnd(x + lx + dx, y + ly + dy))
			{
				T("CheckNearFutureEnd to left");
				forbidden.Add(rightField);
				forbidden.Add(straightField);
			}
			else if (InFutureEnd(x + rx + dx, y + ry + dy))
			{
				T("CheckNearFutureEnd to right");
				forbidden.Add(leftField);
				forbidden.Add(straightField);
			}
		}

		private void AddExit(int x, int y)
		{
			if (!isMain) return; //future path does not need exit

			T("AddExit x " + x + " y " + y);
			if (window.exitIndex.Count > 0)
			{
				int lastIndex = window.exits.Count - 1;
				int[] lastExitField = window.exits[lastIndex]; //the new exit may have the same coordinates as the last one if the path came back from a U-turn. In this case the new exit index is at least 2 steps after the old one.
				if (x == lastExitField[0] && y == lastExitField[1])
				{
					window.exits.RemoveAt(lastIndex);
					window.exitIndex.RemoveAt(lastIndex);
				}

				int lastExitIndex = -1;
				if (window.exits.Count > 0)
				{
					lastIndex = window.exits.Count - 1;
					lastExitIndex = window.exitIndex[lastIndex];
				}

				if (count - 1 >= lastExitIndex + 2) //exits right after each other are unnecessary, there should be at least 2 steps between them 
				{
					window.exits.Add(new int[] { x, y });
					window.exitIndex.Add(count - 1);
					circleDirectionLeft = false;
					nearField = true;
				}
			}
			else
			{
				window.exits.Add(new int[] { x, y });
				window.exitIndex.Add(count - 1);
				circleDirectionLeft = false;
				nearField = true;
			}
		}

		public bool InBorder(int x, int y) // allowing negative values could cause an error in AddFutureLines 2x2 checking
		{
			if (x == 0 || x == size + 1 || y == 0 || y == size + 1) return true;
			return false;
		}

		public bool InBorderF(int[] field)
		{
			int x = field[0];
			int y = field[1];
			if (x < 1 || x > size || y < 1 || y > size) return true;
			return false;
		}

		public bool InTaken(int x, int y) //more recent fields are more probable to encounter, so this way processing time is optimized
		{
			if (!isMain)
			{
				//In AddFutureLines, path2 of future gets null
				if (path2 != null)
				{
					int c2 = path2.Count;

					//when we extend the near end, it can connect to the main line. The far end cannot.
					int searchStart = searchReverse ? c2 - 2: c2 - 1;
					for (int i = searchStart; i >= 0; i--)
					{
						int[] field = path2[i];
						if (x == field[0] && y == field[1])
						{
							return true;
						}
					}
				}				

				int c1 = path.Count;

				if (!searchReverse)
				{
					for (int i = searchStartIndex + 2; i < c1; i++) // In 0618_2 when the far end of the upper line extends, it encounters the side of the previous future section, which is not checked in this funtion.					
					{
						int[] field = path[i];
						if (x == field[0] && y == field[1])
						{
							return true;
						}
					}
				}
				else
				{
					for (int i = searchStartIndex - 2; i >= 0; i--) //the first area that is checked (in checknearfield) is one back and one to left/right side, 2 steps from the current
					{
						int[] field = path[i];
						if (x == field[0] && y == field[1])
						{
							return true;
						}
					}					
				}				
			}
			else
			{				
				int c1 = path.Count;
				for (int i = c1 - 1; i >= 0; i--)
				{
					int[] field = path[i];
					if (x == field[0] && y == field[1])
					{
						return true;
					}
				}				
			}			

			return false;			
		}

		public bool InTakenAll(int x, int y)
		{
			if (!isMain)
			{
				//In AddFutureLines, path2 of future gets null
				if (path2 != null)
				{
					int c2 = path2.Count;

					//when we extend the near end, it can connect to the main line. The far end cannot.
					int searchStart = searchReverse ? c2 - 2 : c2 - 1;
					for (int i = searchStart; i >= 0; i--)
					{
						int[] field = path2[i];
						if (x == field[0] && y == field[1])
						{
							return true;
						}
					}
				}

				int c1 = path.Count;

				for (int i = 0; i < c1; i++)				
				{
					int[] field = path[i];
					if (x == field[0] && y == field[1])
					{
						return true;
					}
				}				
			}
			else
			{
				int c1 = path.Count;
				for (int i = c1 - 1; i >= 0; i--)
				{
					int[] field = path[i];
					if (x == field[0] && y == field[1])
					{
						return true;
					}
				}
			}

			return false;
		}

		public bool InFutureOwnStartF(int[] f)
		{
			int c = path.Count;
			if (c == 0) return false;

			int x = f[0];
			int y = f[1];

			int foundIndex = -1;

			int i;
			for (i = c - 1; i >= 0; i--)
			{
				int[] field = path[i];

				if (field[0] == x && field[1] == y && MainWindow.futureActive[i])
				{
					foundIndex = i;
				}
			}

			if (foundIndex == -1 || foundIndex >= searchStartIndex) return false;

			i = -1;
			foreach (int[] section in MainWindow.futureSections)
			{
				i++;
				if (section[0] == foundIndex)
				{
					return true;
				}
			}

			return false;
		}

		public bool InFutureOwnEndF(int[] f)
		{
			int c = path.Count;
			if (c == 0) return false;

			int x = f[0];
			int y = f[1];

			int foundIndex = -1;

			int i;
			for (i = c - 1; i >= 0; i--)
			{
				int[] field = path[i];

				if (field[0] == x && field[1] == y && MainWindow.futureActive[i])
				{
					foundIndex = i;
				}
			}

			if (foundIndex == -1 || foundIndex >= searchStartIndex) return false;

			i = -1;
			foreach (int[] section in MainWindow.futureSections)
			{
				i++;
				if (section[1] == foundIndex)
				{
					return true;
				}
			}

			return false;
		}

		public bool InFutureStart(int x, int y)
		{
			int c = path2.Count;
			if (c == 0) return false;

			int foundIndex = -1;

			int i;
			for (i = c - 1; i >= 0; i--)
			{
				int[] field = path2[i];

				if (field[0] == x && field[1] == y && MainWindow.futureActive[i])
				{
					foundIndex = i;
				}
			}

			if (foundIndex == -1) return false;

			i = -1;
			foreach (int[] section in MainWindow.futureSections)
			{
				i++;
				if (section[0] == foundIndex)
				{
					return true;
				}
			}

			return false;
		}

		public bool InFutureEnd(int x, int y)
		{
			int c = path2.Count;
			if (c == 0) return false;

			int foundIndex = -1;

			for (int i = c - 1; i >= 0; i--)
			{
				int[] field = path2[i];
				if (field[0] == x && field[1] == y && MainWindow.futureActive[i])
				{
					foundIndex = i;
				}
			}

			if (foundIndex == -1) return false;

			foreach (int[] section in MainWindow.futureSections)
			{
				if (section[1] == foundIndex)
				{
					return true;
				}
			}

			return false;
		}

		private bool InFutureStartIndex(int i)
		{
			foreach (int[] section in MainWindow.futureSections)
			{
				if (section[0] == i)
				{
					return true;
				}
			}

			return false;
		}

		private bool InFutureEndIndex(int i)
		{
			foreach (int[] section in MainWindow.futureSections)
			{
				if (section[1] == i)
				{
					return true;
				}
			}

			return false;
		}

		public bool InFuture(int x, int y)
		{
			int c2 = path2.Count;
			if (c2 == 0) return false;

			for (int i = c2 - 1; i >= 0; i--)
			{
				int[] field = path2[i];
				if (x == field[0] && y == field[1])
				{
					return true;
				}
			}
			return false;
		}

		public int InFutureIndex(int x, int y)
		{
			int c2 = path2.Count;

			for (int i = c2 - 1; i >= 0; i--)
			{
				int[] field = path2[i];
				//without checking active state, CheckNearFutureSide can come true
				if (x == field[0] && y == field[1] && MainWindow.futureActive[i])
				{
					return i;
				}
			}
			return -1;
		}

		public bool InTakenF(int[] field0)
		{
			int x = field0[0];
			int y = field0[1];

			return InTaken(x, y);
		}

		public bool InTakenAllF(int[] field0)
		{
			int x = field0[0];
			int y = field0[1];

			return InTakenAll(x, y);
		}

		public bool InFutureF(int[] field0)
		{
			int x = field0[0];
			int y = field0[1];

			return InFuture(x, y);
		}

		public int InTakenIndex(int x, int y)
		{
			int foundIndex = -1;
			
			if (path2 != null && path2.Count > 0)
			{
				int c2 = path2.Count;
				//first field (near end) doesn't count as taken, it can be connected to. Also every open ends.foundpath
				int prevX = path2[c2 - 1][0];
				int prevY = path2[c2 - 1][1];
				for (int i = c2 - 2; i >= 0; i--)
				{
					int[] field = path2[i];
					int searchX = field[0];
					int searchY = field[1];
					if (x == searchX && y == searchY && (searchX == prevX && Math.Abs(searchY - prevY) == 1 || searchY == prevY && Math.Abs(searchX - prevX) == 1))
					{
						foundIndex = i;
						foundPath = path2;
					}
					prevX = searchX;
					prevY = searchY;
				}
			}

			int c1 = path.Count;
			for (int i = 0; i < c1; i++)
			{
				int[] field = path[i];
				if (x == field[0] && y == field[1])
				{
					foundIndex = i;
					foundPath = path;
				}
			}
			return foundIndex;
		}

		public bool InForbidden(int[] value)
		{
			bool found = false;
			foreach (int[] field in forbidden)
			{
				if (value[0] == field[0] && value[1] == field[1])
				{
					found = true;
				}
			}
			return found;
		}

		private void T(object o)
		{
			Trace.WriteLine(o.ToString());
		}
	}
}
