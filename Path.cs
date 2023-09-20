using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace OneWayLabyrinth
{
	public class Path
	{
		MainWindow window;
		int size;
		public List<int[]> path;
		public List<int[]> path2 = new List<int[]>(); //future line uses the main line to check forbidden fields
		bool isMain = true;
		public bool isNearEnd = false;
		public int count;
		public List<int[]> possible = new List<int[]>(); //field coordinates
		List<int[]> forbidden = new List<int[]>();
		public bool nearField;
		public bool circleDirectionLeft = true;
		public int x, y, x2, y2;
		public int[] s = new int[] { 0, 0 }; //straight, left and right coordinates
        public int[] l = new int[] { 0, 0 };
        public int[] r = new int[] { 0, 0 };
        int[] straightField = new int[] { };
		int[] leftField = new int[] { };
		int[] rightField = new int[] { };
		List<int[]> directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } }; //down, right, up, left
		int[] selectedDirection = new int[] { };
		bool CShape;
		int nearSection, farSection;
		int foundSectionStart, foundSectionEnd;

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

		public void NextStepPossibilities(bool isNearEnd, int index, int nearSection, int farSection)
		{
			this.nearSection = nearSection;
            this.farSection = farSection;

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
				this.isNearEnd = isNearEnd;
				if (index != -1) //for checking future lines
				{
					if (!isNearEnd)
					{
						//extend far end
						x = path[index][0];
						y = path[index][1];
						x0 = path[index + 1][0];
						y0 = path[index + 1][1];
					}
					else
					{
						//extend near end
						x = path[index][0];
						y = path[index][1];
						x0 = path[index - 1][0];
						y0 = path[index - 1][1];
					}
                    T("NextSteppossibilities future, x " + x + " y " + y + " isNearEnd " + isNearEnd + " index " + index + " length " + path.Count);
                }
				else
				{
					T("NextSteppossibilities main, x " + x + " y " + y + " length " + path.Count + " future length " + path2.Count);
					x0 = path[count - 2][0];
					y0 = path[count - 2][1];
				}

				int i;
				for (i = 0; i < 4; i++)
				{
					//last movement: down, right, up, left
					s[0] = directions[i][0];
					s[1] = directions[i][1];

					//this shouldn't be needed. But there is a bug in the framework that changes s when l changes below
					int s0 = s[0];
                    int s1 = s[1];

                    if (x - x0 == s[0] && y - y0 == s[1])
					{
                        selectedDirection = directions[i];

                        if (i == 0)
						{
							l[0] = directions[1][0];
							l[1] = directions[1][1];
							r[0] = directions[3][0];
							r[1] = directions[3][1];
						}
						else if (i == 3)
						{
							l[0] = directions[0][0];
							l[1] = directions[0][1];
							r[0] = directions[2][0];
							r[1] = directions[2][1];
						}
						else
						{
                            l[0] = directions[i + 1][0]; // bug: this line changes s[0] too
                            l[1] = directions[i + 1][1]; // bug: this line changes s[1] too
                            r[0] = directions[i - 1][0];
                            r[1] = directions[i - 1][1];
                        }
						s = new int[] { s0, s1 };

                        straightField = new int[] { x + s[0], y + s[1] };
						leftField = new int[] { x + l[0], y + l[1] };
						rightField = new int[] { x + r[0], y + r[1] };

                        if (!InTakenAbs(straightField) && !InBorderAbs(straightField) && !InFutureAbs(straightField))
						{
							T("possible straight");
							possible.Add(straightField);
						}
						if (!InTakenAbs(rightField) && !InBorderAbs(rightField) && !InFutureAbs(rightField))
						{
							T("possible right");
							possible.Add(rightField);
						}
						if (!InTakenAbs(leftField) && !InBorderAbs(leftField) && !InFutureAbs(leftField))
						{
							T("possible left");
							possible.Add(leftField);
						}

                        // A future line may connect to another section as in 0714_2 when we step up, and a 2x2 line is created on the left
                        // For connecting to an older line, see 0730
                        // It cannot connect to the end of the same section
						if (!isMain) {
							if (!isNearEnd && InFutureStartAbs(straightField, nearSection) || isNearEnd && InFutureEndAbs(straightField, farSection))
							{
								T("possible future connection straight");
								possible.Add(straightField);
							}
							// See 0803
							if (!isNearEnd && InFutureStartAbs(rightField, nearSection) || isNearEnd && InFutureEndAbs(rightField, farSection))
							{
								T("possible future connection right");
								possible.Add(rightField);
							}
							if (!isNearEnd && InFutureStartAbs(leftField, nearSection) || isNearEnd && InFutureEndAbs(leftField, farSection))
							{
								T("possible future connection left");
								possible.Add(leftField);
							}
						}
                        else
                        {
                            if (InFutureStartAbs(straightField))
                            {
                                T("possible future start straight");
                                possible.Add(straightField);
                            }
                            if (InFutureStartAbs(rightField))
                            {
                                T("possible future start right");
                                possible.Add(rightField);
                            }
                            if (InFutureStartAbs(leftField))
                            {
                                T("possible future start left");
                                possible.Add(leftField);
                            }
                        }

						//if the only possible field is a future field, we don't need to check more. This will prevent unnecessary exits, as in 0804.

						if (isMain && possible.Count == 1 && InFutureStartAbs(possible[0])) break;

						//check for an empty cell next to the position, and a taken one further. In case of a C shape, we need to fill the middle. The shape was formed by the previous 5 steps.
						T("possible.Count " + possible.Count);
											
						CShape = false;
						CheckNearFieldCommon();
						if (!isMain && !isNearEnd) // when the far end of the future line extends, it should be checked for border just like the main line. Near end shouldn't be checked, see 0714
						{
							CheckNearBorder();
							/*T("forbidden.Count after CheckNearBorder " + forbidden.Count);
							foreach (int[] f in forbidden)
							{
								T(f[0] + " " + f[1]);
							}*/

							if (size >= 21) // 0430_2. Find out the minimum size
							{
                                Check1x3();
                            }
                        }
						else if (isMain)
						{							
                            CheckNearBorder();                            
                            CheckNearCorner(); // 0811_2
                            if (size >= 7)
							{
                                CheckNearField(); // 0901, 0917_1, 0917_4
                                CheckCOnFarBorder();
                                CheckFutureL(); // Live end, future line near end and far end make an L, with one space between.
								CheckArea(); // 0909. A 2x2 area would created with one way to go in and out
								Check2x2FutureStartEnd(); // 0909_1, 0909_1_2
								Check2x3FutureStartEnd(); // 0915
                                Check3x3FutureStartEnd(); // 0916
                            }
                            if (size >= 9)
                            {
                                CheckCOnNearBorder();
                            }
                            if (size >= 11)
                            {
                                Check1x3();
                                Check3x3();
                            }
                            if (size >= 21)
                            {
                                // found on 21x21, may recreate the situation on smaller boards.
                                CheckNearFutureStartEnd();
                                CheckNearFutureSide();
                                // (Will be actual only on 21x21) 0630, but needs to work with 0804_1 too
                                // CheckNearFutureEnd();
                            }    
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


		public void CheckNearFieldCommon()
		{
            // Even future line can make a straight C shape, see 0727_1

            int thisS0 = s[0];
            int thisS1 = s[1];
            int thisL0 = l[0];
            int thisL1 = l[1];

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    //C shape that needs to be filled, unless there is a live end nearby that can fill it
                    if ((InTakenRel(2, 0) || InBorderRel(2, 0)) && InTakenRel(1, -1) && !InTakenRel(1, 0, 0) &&
                        (isMain || !isMain && !InFutureStartRel(1, -1) && !InFutureEndRel(1, -1) && !InFutureStartRel(2, 0) && !InFutureEndRel(2, 0))) //2 to left
                    {
                        T("C shape at " + i + " " + j);
                        forbidden.Add(new int[]{ x + s[0], y + s[1]}); //straight
                        forbidden.Add(new int[]{ x - l[0], y - l[1]}); //right
                        CShape = true; // left/right across checking will be disabled, no exits needed					
                    }

                    //turn right, pattern goes upwards
                    int temps0 = s[0];
                    int temps1 = s[1];
                    s[0] = -l[0];
                    s[1] = -l[1];
                    l[0] = temps0;
                    l[1] = temps1;
                }

                //mirror directions
                s[0] = thisS0;
                s[1] = thisS1;
                l[0] = -thisL0;
                l[1] = -thisL1;
            }

            s[0] = thisS0;
            s[1] = thisS1;
            l[0] = thisL0;
            l[1] = thisL1;
		}

        public void CheckNearBorder()
        {
            int directionIndex = Math.Abs(selectedDirection[0]); //0 for vertical, 1 for horizontal

            // going up on left side
            if (x == 2 && x + l[0] == 1 && !InTakenAbs(leftField) && !InTaken(x - 1, y - 1))
            {
                forbidden.Add(leftField);
            }
            // going left on up side
            else if (y == 2 && y + r[1] == 1 && !InTakenAbs(rightField) && !InTaken(x - 1, y - 1))
            {
                forbidden.Add(rightField);
            }

            if (leftField[directionIndex] == size && !InTakenAbs(leftField))
            {
                if (directionIndex == 0) //going down on right side
                {
                    forbidden.Add(straightField);
                    forbidden.Add(rightField);
                }
                else //going left on down side
                {
                    if ((isMain && !InTaken(x - 1, y + 1) || !isMain && (!InTaken(x - 1, y + 1) || InFutureEnd(x - 1, y + 1))) && !InBorder(x - 1, y + 1)) //InBorder checking is necessar[1] when x=1
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
            else if (rightField[directionIndex] == size && !InTakenAbs(rightField))
            {
                if (directionIndex == 0) //going up on right side
                {
                    //the field to the right and up might be a succeeding future line end when we extend a far end. (0910) In this case, InTaken(x + 1, y - 1) will be true.
                    if ((isMain && !InTaken(x + 1, y - 1) || !isMain && (!InTaken(x + 1, y - 1) || InFutureEnd(x + 1, y - 1))) && !InBorder(x + 1, y - 1))
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

            if (isMain) // The field x - 1, y - 1 can be filled earlier than the future line far end, so extending the far end on the border is not correct. 
            {
                //going towards an edge
                if (x + 2 * s[0] == 0)
                {
                    forbidden.Add(leftField);
                    if (!InTaken(x - 1, y - 1))
                    {
                        forbidden.Add(straightField);

                        if (size >= 9) // count area is always pair on 7 x 7. Situations where iy would get impair are avoided with CheckFutureL, see 0902
                        {
                            AddExit(x - 1, y - 1);
                            circleDirectionLeft = false;
                        }
                    }
                }
                else if (x + 2 * s[0] == size + 1)
                {
                    forbidden.Add(rightField);
                    if (!InTaken(x + 1, y - 1) && y != 1)
                    {
                        forbidden.Add(straightField);

                        if (isMain && size >= 9)
                        {
                            AddExit(x + 1, y - 1);
                            circleDirectionLeft = true;
                        }
                    }
                }
                else if (y + 2 * s[1] == 0)
                {
                    forbidden.Add(rightField);
                    if (!InTaken(x - 1, y - 1))
                    {
                        forbidden.Add(straightField);

                        if (isMain && size >= 9)
                        {
                            AddExit(x - 1, y - 1);
                            circleDirectionLeft = true;
                        }
                    }
                }
                else if (y + 2 * s[1] == size + 1)
                {
                    forbidden.Add(leftField);
                    if (!InTaken(x - 1, y + 1) && x != 1)
                    {
                        forbidden.Add(straightField);

                        if (isMain && size >= 9)
                        {
                            AddExit(x - 1, y + 1);
                            circleDirectionLeft = false;
                        }
                    }
                }
            }            
        }

        private void CheckNearCorner()
        {
            //First condition is needed for when we are at 4,5 and the left field is 4,4 on a 5x5 field
            if (y != size && leftField[0] == size - 1 && leftField[1] == size - 1)
            {
                T("Left field near corner");
                forbidden.Add(leftField);
            }
            else if (x != size && rightField[0] == size - 1 && rightField[1] == size - 1)
            {
                T("Right field near corner");
                forbidden.Add(rightField);
            }
        }

        // 7 x 7

        public void CheckNearField()
		{
			if (!CShape)
			{
                int thisL0 = l[0];
                int thisL1 = l[1];

                for (int i = 0; i < 2; i++)
                {
                    // 
                    // oX
                    //xox

                    //xo
                    // oX
                    //  x
                    if (InTakenRel(2, -1) && !InTakenRel(1, -1) && !InTakenRel(1, 0)  || InTakenRel(2, 1) && !InTakenRel(1, 1) && !InTakenRel(1, 0))
                    {
                        T("Side front/back at " + i);
                        forbidden.Add(i == 0 ? leftField : rightField);
                    }

                    //mirror directions
                    l[0] = -thisL0;
                    l[1] = -thisL1;
                }

                l[0] = thisL0;
                l[1] = thisL1;

                if (size == 7)
                {
                    bool closeMidAcrossFound = false;
                    bool closeAcrossFound = false;

                    // 0917_1, 0917_4, 0901 (straight line ahead, count area is true at this size, we just need to disable the directions)
                    if (InTakenRel(0, 2) && !InTakenRel(0, 1)) // the field 2 straight is taken 
                    {
                        T("CheckNearField close straight");
                        int index = InTakenIndex(x + 2 * s[0], y + 2 * s[1]);
                        int[] nextField = path[index + 1];

                        forbidden.Add(straightField);
                        if (nextField[0] == x + 3 * s[0] && nextField[1] == y + 3 * s[1]) //next step is straight, examine previous step
                        {
                            int[] prevField = path[index - 1];
                            if (prevField[0] == x + l[0] + 2 * s[0] && prevField[1] == y + l[1] + 2 * s[1]) // previous step left, area is on the right
                            {
                                forbidden.Add(leftField);
                            }
                            else
                            {
                                forbidden.Add(rightField);
                            }
                        }
                        else if (nextField[0] == x + l[0] + 2 * s[0] && nextField[1] == y + l[1] + 2 * s[1])
                        { // area is on the left
                            forbidden.Add(rightField);
                        }
                        else
                        {
                            forbidden.Add(leftField);
                        }
                    }
                    else // the field 2 straight and 1 left/right is taken
                    {
                        thisL0 = l[0];
                        thisL1 = l[1];

                        for (int i = 0; i < 2; i++)
                        {
                            if (InTakenRel(1, 2) && !InTakenRel(0, 1) && !InTakenRel(1, 1))
                            {
                                T("CheckNearField close mid across at " + i);
                                closeMidAcrossFound = true;
                                int index = InTakenIndex(x + l[0] + 2 * s[0], y + l[1] + 2 * s[1]);
                                int[] nextField = path[index + 1];

                                forbidden.Add(straightField);
                                if (nextField[0] == x + l[0] + 3 * s[0] && nextField[1] == y + l[1] + 3 * s[1]) //next step is straight, area on right
                                {
                                    forbidden.Add(i == 0 ? leftField : rightField);
                                }
                                else
                                {
                                    forbidden.Add(i == 0 ? rightField : leftField);
                                }
                            }

                            //mirror directions
                            l[0] = -thisL0;
                            l[1] = -thisL1;
                        }

                        l[0] = thisL0;
                        l[1] = thisL1;

                        if (!closeMidAcrossFound)
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                if (InTakenRel(2, 2) && !InTakenRel(0, 1) && !InTakenRel(1, 1) && !InTakenRel(2, 1))
                                {
                                    T("CheckNearField close across at " + i);
                                    closeAcrossFound = true;
                                    int index = InTakenIndex(x + 2 * l[0] + 2 * s[0], y + 2 * l[1] + 2 * s[1]);
                                    int[] nextField = path[index + 1];

                                    if (nextField[0] == x + 2 * l[0] + 3 * s[0] && nextField[1] == y + 2 * l[1] + 3 * s[1]) //next step is straight, area on right
                                    {
                                        forbidden.Add(i == 0 ? leftField : rightField);
                                    }
                                    else
                                    {
                                        forbidden.Add(straightField);
                                        forbidden.Add(i == 0 ? rightField : leftField);
                                    }
                                }

                                //mirror directions
                                l[0] = -thisL0;
                                l[1] = -thisL1;
                            }

                            l[0] = thisL0;
                            l[1] = thisL1;

                            if (!closeAcrossFound)
                            {
                                int thisS0 = s[0];
                                int thisS1 = s[1];

                                for (int i = 0; i < 2; i++)
                                {
                                    for (int j = 0; j < 2; j++)
                                    {
                                        if (InTakenRel(1, 3) && !InTakenRel(1, 2) && !InTakenRel(0, 3)) //start with area on the right, mirrored to the example
                                        {
                                            int index = InTakenIndex(x + l[0] + 3 * s[0], y + l[1] + 3 * s[1]);
                                            int[] nextField = path[index + 1];

                                            if (nextField[0] == x + l[0] + 4 * s[0] && nextField[1] == y + l[1] + 4 * s[1])
                                            {
                                                T("CheckNearField across, next field up at " + i + " " + j);
                                                circleDirectionLeft = i == 0 ? false : true;
                                                if (!CountArea(x + s[0], y + s[1], x + 2 * s[0], y + 2 * s[1]))
                                                {
                                                    if (j == 0)
                                                    {
                                                        forbidden.Add(straightField);
                                                    }
                                                    else
                                                    {
                                                        forbidden.Add(i == 0 ? leftField : rightField);
                                                    }                                                    
                                                }
                                            }
                                            else
                                            {
                                                T("CheckNearField across, next field side at " + i + " " + j);
                                                circleDirectionLeft = i == 0 ? true : false;
                                                if (!CountArea(x + s[0], y + s[1], x + 2 * s[0], y + 2 * s[1]))
                                                {
                                                    if (j == 0)
                                                    {
                                                        forbidden.Add(straightField);
                                                    }
                                                    else
                                                    {
                                                        forbidden.Add(i == 0 ? leftField : rightField);
                                                    }
                                                }
                                            }
                                        }

                                        //turn left, pattern goes downwards
                                        int temps0 = s[0];
                                        int temps1 = s[1];
                                        s[0] = l[0];
                                        s[1] = l[1];
                                        l[0] = -temps0;
                                        l[1] = -temps1;
                                    }

                                    //mirror directions
                                    s[0] = thisS0;
                                    s[1] = thisS1;
                                    l[0] = -thisL0;
                                    l[1] = -thisL1;
                                }

                                s[0] = thisS0;
                                s[1] = thisS1;
                                l[0] = thisL0;
                                l[1] = thisL1;
                            }
                        }
                    }   
                }
                else if (size > 7)
				{
                    int directionIndex = Math.Abs(selectedDirection[0]); //0 for vertical, 1 for horizontal

                    if (InTakenRel(1, 2) &&
                        !InTakenAbs(straightField) &&
                        !InTakenRel(1, 1) && !(x + l[0] + 2 * s[0] == 1 && y + l[1] + 2 * s[1] == 1))
                    { //inTakenIndex would be negative when getting to the upper left corner, rigtt mid across check will be true instead.
                        int index = InTakenIndex(x + l[0] + 2 * s[0], y + l[1] + 2 * s[1]);
                        T("Left mid across field: x " + (x + l[0] + 2 * s[0]) + " y " + (y + l[1] + 2 * s[1]) + " x " + x + " y " + y);

                        int[] nextField;
                        int[] prevField;

                        nextField = path[index + 1];
                        prevField = path[index - 1];

                        forbidden.Add(straightField);

                        //In the 7x7 example, the area is pair, and there does not seem to be another scenario
                        AddExit(x + l[0] + s[0], y + l[1] + s[1]);

                        int firstConditionValue, secondConditionValue;
                        if (directionIndex == 0)
                        {
                            firstConditionValue = x + 2 * l[0];
                            secondConditionValue = x;
                        }
                        else
                        {
                            firstConditionValue = y + 2 * l[1];
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
                    else if (InTakenRel(-1, 2) &&
                        !InTakenAbs(straightField) &&
                        !InTakenRel(-1, 1) && !(x + r[0] + 2 * s[0] == 1 && y + r[1] + 2 * s[1] == 1))
                    {
                        int index = InTakenIndex(x + r[0] + 2 * s[0], y + r[1] + 2 * s[1]);
                        T("Right mid across field: x " + (x + r[0] + 2 * s[0]) + " y " + (y + r[1] + 2 * s[1]) + " x " + x + " y " + y);

                        int[] nextField;
                        int[] prevField;

                        nextField = path[index + 1];
                        prevField = path[index - 1];

                        forbidden.Add(straightField);

                        AddExit(x + r[0] + s[0], y + r[1] + s[1]);

                        int firstConditionValue, secondConditionValue;
                        if (directionIndex == 0)
                        {
                            firstConditionValue = x + 2 * r[0];
                            secondConditionValue = x;
                        }
                        else
                        {
                            firstConditionValue = y + 2 * r[1];
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
                            firstConditionValue = y + 3 * s[1];
                            secondConditionValue = y + s[1];
                        }
                        else
                        {
                            firstConditionValue = x + 3 * s[0];
                            secondConditionValue = x + s[0];
                        }

                        if (InTakenRel(2, 2) &&
                            !InTakenAbs(straightField) &&
                            !InTakenRel(1, 1) &&
                            !InTakenAbs(leftField))
                        {
                            int index = InTakenIndex(x + 2 * l[0] + 2 * s[0], y + 2 * l[1] + 2 * s[1]);

                            T("Left across field: x " + (x + 2 * l[0] + 2 * s[0]) + " y " + (y + 2 * l[1] + 2 * s[1]) + " x " + x + " y " + y);
                            // Example on 7 x 7: 0917 

                            int[]? nextField = null;
                            int[] prevField;

                            nextField = path[index + 1];
                            prevField = path[index - 1];

                            AddExit(x + l[0] + s[0], y + l[1] + s[1]);

                            if (nextField != null && nextField[1 - directionIndex] == firstConditionValue) //up
                            {
                                if (isMain)
                                {
                                    forbidden.Add(leftField);
                                    circleDirectionLeft = false;
                                }
                            }
                            else //right
                            {
                                if (index != 0) //not the start field
                                {
                                    if (prevField[1 - directionIndex] == secondConditionValue) // from down
                                    {
                                        forbidden.Add(leftField);
                                        circleDirectionLeft = false;
                                    }
                                    else
                                    {
                                        forbidden.Add(straightField);
                                        forbidden.Add(rightField);
                                        circleDirectionLeft = true;
                                    }
                                }
                                else
                                {
                                    forbidden.Add(straightField);
                                    forbidden.Add(rightField);
                                    circleDirectionLeft = true;
                                }
                            }
                        }

                        if (InTakenRel(-2, 2) &&
                        !InTakenAbs(straightField) &&
                        !InTakenRel(-1, 1) &&
                        !InTakenAbs(rightField))
                        {
                            int index = InTakenIndex(x + 2 * r[0] + 2 * s[0], y + 2 * r[1] + 2 * s[1]);

                            T("Right across field: x " + (x + 2 * r[0] + 2 * s[0]) + " y " + (y + 2 * r[1] + 2 * s[1]) + " x " + x + " y " + y);

                            int[]? nextField = null;
                            int[] prevField;

                            nextField = path[index + 1];
                            prevField = path[index - 1];

                            AddExit(x + r[0] + s[0], y + r[1] + s[1]);

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
		}

        private void CheckCOnFarBorder()
        {
            // Applies from 7x7, see 0831_1. Similar to CheckNearCorner, it is just not at the corner.
            if (x == size - 2 && rightField[0] == size - 1 && !InTakenRel(-1, -1) && InTakenRel(-1, -2))
            {
                T("Right field C on far border");
                forbidden.Add(rightField);
            }
            else if (y == size - 2 && leftField[1] == size - 1 && !InTakenRel(1, -1) && InTakenRel(1, -2))
            {
                T("Left field C on far border");
                forbidden.Add(leftField);
            }
        }

        public void CheckFutureL()
        {
            // 0821, 0827
            // the start and end fields have to be in the same section, otherwise they can connect, like in 0913
            // conditions are true already on 5x5 at 0831_2, but it is handled in CheckNearCorner

            if (InFutureStart(x + 2 * l[0], y + 2 * l[1]) && InFutureEnd(x + 2 * l[0] + 2 * s[0], y + 2 * l[1] + 2 * s[1]) && foundSectionStart == foundSectionEnd)
            {
                T("CheckFutureL left");
                forbidden.Add(rightField);
                forbidden.Add(straightField);
            }
            if (InFutureStart(x + 2 * r[0], y + 2 * r[1]) && InFutureEnd(x + 2 * r[0] + 2 * s[0], y + 2 * r[1] + 2 * s[1]) && foundSectionStart == foundSectionEnd)
            {
                T("CheckFutureL right");
                forbidden.Add(leftField);
                forbidden.Add(straightField);
            }
            if (InFutureStart(x + 2 * s[0], y + 2 * s[1]) && InFutureEnd(x + 2 * l[0] + 2 * s[0], y + 2 * l[1] + 2 * s[1]) && foundSectionStart == foundSectionEnd)
            {
                T("CheckFutureL straight left");
                forbidden.Add(leftField);
            }
            if (InFutureStart(x + 2 * s[0], y + 2 * s[1]) && InFutureEnd(x + 2 * r[0] + 2 * s[0], y + 2 * r[1] + 2 * s[1]) && foundSectionStart == foundSectionEnd)
            {
                T("CheckFutureL straight right");
                forbidden.Add(rightField);
            }
        }

        public void CheckArea() // 0909. Check both straight approach and side.
        {
			if (CShape) return;

            if (x == 3 && straightField[0] == 2 && !InTakenAbs(straightField) && !InFutureAbs(straightField) && !InTakenAbs(rightField) && !InTaken(1, y))
            {
                T("CheckArea left");
				circleDirectionLeft = false;
				if (!CountArea(2, y - 1, 1, y - 1))
				{
					forbidden.Add(straightField);
                    forbidden.Add(leftField);
                }
            }
			else if (y == 3 && straightField[1] == 2 && !InTakenAbs(straightField) && !InFutureAbs(straightField) && !InTakenAbs(leftField) &&!InTaken(x, 1))
			{
                T("CheckArea up");
                circleDirectionLeft = true;
                if (!CountArea(x - 1, 2, x - 1, 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(rightField);
                }
            }
            else if (x == size - 2 && y >= 2 && straightField[0] == size - 1 && !InTakenAbs(straightField) && !InFutureAbs(straightField) && !InTakenAbs(leftField) && !InTaken(size, y))
            {
                T("CheckArea right");
                circleDirectionLeft = true;
                if (!CountArea(size - 1, y - 1, size, y - 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(rightField);
                }
            }
            else if (y == size - 2 && x >= 2 && straightField[1] == size - 1 && !InTakenAbs(straightField) && !InFutureAbs(straightField) && !InTakenAbs(rightField) && !InTaken(x, size))
            {
                T("CheckArea down");
                circleDirectionLeft = false;
                if (!CountArea(x - 1, size - 1, x - 1, size))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(leftField);
                }
            }
            else if (x == 3 && y >= 4 && leftField[0] == 2 && !InTaken(3, y - 1) &&!InTaken(1, y)) //straight and left field cannot be taken, but it is enough we check the most left field on border.
            {
                T("CheckArea left side");
                circleDirectionLeft = false;
                if (!CountArea(2, y - 1, 1, y - 1))
                {
                    forbidden.Add(leftField);
                }
            }
            else if (y == 3 && x >= 4 && rightField[1] == 2 && !InTaken(x - 1, 3) && !InTaken(x, 1))
            {
                T("CheckArea up side");
                circleDirectionLeft = true;
                if (!CountArea(x - 1, 2, x - 1, 1))
                {
                    forbidden.Add(rightField);
                }
            }
            else if (x == size - 2 && y >= 3 && rightField[0] == size - 1 && !InTaken(size - 2, y - 1) && !InTaken(size, y))
            {
                T("CheckArea right side");
                circleDirectionLeft = true;
                if (!CountArea(size - 1, y - 1, size, y - 1))
                {
                    forbidden.Add(rightField);
                }
            }
            else if (y == size - 2 && x >= 3 && leftField[1] == size - 1 && !InTaken(x - 1, size - 2) && !InTaken(x, size))
            {
                T("CheckArea down side");
                circleDirectionLeft = false;
                if (!CountArea(x - 1, size - 1, x - 1, size))
                {
                    forbidden.Add(leftField);
                }
            }
        }

        public void Check2x2FutureStartEnd() // 0901_1 and 0901_1_2
												// On boards larger than 7 x 7, is it possible to apply the rule in straight left/right directions? It means that in the original example, the line is coming downwards instead of heading right.
        {
			if (InTakenRel(1, 1) && InTakenRel(1, 2) && InTakenRel(0, 3) && InFutureStartRel(-1, 0) && InFutureEndRel(-3, 0) && (InBorderRel(-4, 1) || InTakenRel(-4, 1)))
			{
                T("Check2x2AndFutureStartEnd to right");
				forbidden.Add(rightField);
            }
			else if (InTakenRel(-1, 1) && InTakenRel(-1, 2) && InTakenRel(0, 3) && InFutureStartRel(1, 0) && InFutureEndRel(3, 0) && (InBorderRel(4, 1) || InTakenRel(4, 1)))
			{
                T("Check2x2AndFutureStartEnd to left");
                forbidden.Add(leftField);
            }

        }

        public void Check2x3FutureStartEnd() // 0915
											 // Is there a situation where the start and end fields are not part of one future line?
                                             // On boards larger than 7 x 7, is it possible to apply the rule in straight left/right directions? It means that in the original example, the line is coming downwards instead of heading right.
        {
            if (InFutureStartRel(0, 1) && InFutureEndRel(-2, 1) && (InBorderRel(-1, -2) || InTakenRel(-1, -2)))
            {
                T("Check2x3FutureStartEnd to right");
                forbidden.Add(rightField);
            }
            else if (InFutureStartRel(0, 1) && InFutureEndRel(2, 1) && (InBorderRel(1, -2) || InTakenRel(1, -2)))
            {
                T("Check2x3FutureStartEnd to left");
                forbidden.Add(leftField);
            }

        }

		public void Check3x3FutureStartEnd() // 0916, mirrored
		{
            int thisS0 = s[0];
            int thisS1 = s[1];
            int thisL0 = l[0];
            int thisL1 = l[1];

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (InFutureStartRel(1, 0) && InFutureEndRel(3, 0) && (InBorderRel(1, 4) || InTakenRel(1, 4)) && (InBorderRel(2, 4) || InTakenRel(2, 4)) && (InBorderRel(3, 4) || InTakenRel(3, 4)) && (InBorderRel(4, 3) || InTakenRel(4, 3)) && (InBorderRel(4, 2) || InTakenRel(4, 2)) && (InBorderRel(4, 1) || InTakenRel(4, 1)) && !InTakenRel(3, 1) && !InTakenRel(3, 2) && !InTakenRel(3, 3) && !InTakenRel(2, 3) && !InTakenRel(1, 3))
                    {
                        T("Check3x3FutureStartEnd");
                        forbidden.Add(new int[] { x + l[0], y + l[1] }); //left field in the example
                    }

                    //turn right, pattern goes upwards
                    int temps0 = s[0];
                    int temps1 = s[1];
                    s[0] = -l[0];
                    s[1] = -l[1];
                    l[0] = temps0;
                    l[1] = temps1;
                }

                //mirror directions
                s[0] = thisS0;
                s[1] = thisS1;
                l[0] = -thisL0;
                l[1] = -thisL1;
            }

            s[0] = thisS0;
            s[1] = thisS1;
            l[0] = thisL0;
            l[1] = thisL1;
        }

        // 9 x 9

        private void CheckCOnNearBorder()
        {
            // Applies from 9x9, see 0901_1.
            if (x == 3 && leftField[0] == 2 && !InTakenRel(1, -1) && InTakenRel(1, -2))
            {
                T("Left field C on near border");
                forbidden.Add(leftField);
            }
            else if (y == 3 && rightField[1] == 2 && !InTakenRel(-1, -1) && InTakenRel(-1, -2))
            {
                T("Right field C on near border");
                forbidden.Add(rightField);
            }
        }

		// 11 x 11

        private void Check1x3()
		{
            //Certain edges are impossible to fill.

            //xo
            // xoo?x
            //  xxxx

            //next step has to be left

			//if I give value to int[] thisS = s, it will change when s changes.
            int thisS0 = s[0];
            int thisS1 = s[1];
            int thisL0 = l[0];
            int thisL1 = l[1];

            for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
                    if (InTakenRel(1, -1) && InTakenRel(2, -1) && InTakenRel(3, -1)
					&& InTakenRel(4, 0) && InTakenRel(5, 1)
					&& !InTakenRel(1, 0) && !InTakenRel(2, 0) && !InTakenRel(3, 0)
					&& !InTakenRel(4, 1))
					{
						T("1x3 valid at x " + x + " y " + y);
						forbidden.Add(new int[] { x + s[0], y + s[1] }); //straight field
						forbidden.Add(new int[] { x - l[0], y - l[1] }); //right field (per the start direction)
					}

                    //turn right, pattern goes upwards
                    int temps0 = s[0];
					int temps1 = s[1];
					s[0] = -l[0];
					s[1] = -l[1];                    
                    l[0] = temps0;
					l[1] = temps1;
                }

				//mirror directions
				s[0] = thisS0;
                s[1] = thisS1;
                l[0] = -thisL0;
				l[1] = -thisL1;
            }

            s[0] = thisS0;
            s[1] = thisS1;
            l[0] = thisL0;
            l[1] = thisL1;
        }

		private void Check3x3()
		{
            // x
            //xo
            //xo
            //xoo?x
            // xxxx

            //next step has to be left

            int thisS0 = s[0];
            int thisS1 = s[1];
            int thisL0 = l[0];
            int thisL1 = l[1];

            for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					if (InTakenRel(1, -1) && InTakenRel(2, -1) && InTakenRel(3, -1)
					 && InTakenRel(4, 0) && InTakenRel(4, 1) && InTakenRel(4, 2)
					 && InTakenRel(3,3)
					 && !InTakenRel(1, 0) && !InTakenRel(2, 0) && !InTakenRel(3, 0)
					 && !InTakenRel(3, 1) && !InTakenRel(3, 2))
					{
						T("3x3 valid at x " + x + " y " + y);
						forbidden.Add(new int[] { x + s[0], y + s[1] }); //straight field
						forbidden.Add(new int[] { x - l[0], y - l[1] }); //right field (per the start direction)
					}

                    //turn right, pattern goes upwards
                    int temps0 = s[0];
                    int temps1 = s[1];
                    s[0] = -l[0];
                    s[1] = -l[1];
                    l[0] = temps0;
                    l[1] = temps1;
                }

                //mirror directions
                s[0] = thisS0;
                s[1] = thisS1;
                l[0] = -thisL0;
                l[1] = -thisL1;
            }

            s[0] = thisS0;
            s[1] = thisS1;
            l[0] = thisL0;
            l[1] = thisL1;
        }

		// 21 x 21

		public void CheckNearFutureStartEnd()
		{
			T(" x " + x + " " + y + " " + l[0] + " " + l[1] + " " + s[0] + " " + s[1]);
			T("CheckNearFutureStartEnd x + l[0] + 2 * s[0] " + (x + l[0] + 2 * s[0]) + " y + l[1] + 2 * s[1] " + (y + l[1] + 2 * s[1]));
			//meeting start and end ahead. Ends are 2 apart from each other at same forward distance.
			if (InFutureStart(x + 2 * s[0], y + 2 * s[1]) && InFutureEnd(x + 2 * r[0] + 2 * s[0], y + 2 * r[1] + 2 * s[1]) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField))
			{
				T("CheckNearFutureStartEnd to right");
				forbidden.Add(rightField);
			}
			else if (InFutureStart(x + 2 * s[0], y + 2 * s[1]) && InFutureEnd(x + 2 * l[0] + 2 * s[0], y + 2 * l[1] + 2 * s[1]) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField))
			{
				T("CheckNearFutureStartEnd to left");
				forbidden.Add(leftField);
			}
			else if (InFutureStart(x + l[0] + 2 * s[0], y + l[1] + 2 * s[1]) && InFutureEnd(x + r[0] + 2 * s[0], y + r[1] + 2 * s[1]) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField))
			{
				T("CheckNearFutureStartEnd to middle, start on left");
				forbidden.Add(rightField);
				forbidden.Add(straightField);
			}
			else if (InFutureStart(x + r[0] + 2 * s[0], y + r[1] + 2 * s[1]) && InFutureEnd(x + l[0] + 2 * s[0], y + l[1] + 2 * s[1]) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField))
			{
				T("CheckNearFutureStartEnd to middle, start on right");
				forbidden.Add(leftField);
				forbidden.Add(straightField);
			}
		}

        //Example: 0305
        public void CheckNearFutureSide() //suppose the side is of stair form
        {
            //mid cases are used when approaching an end, example: 0415_2
            //but in case of 0620, this is an error
            if (InFuture(x + 2 * s[0], y + 2 * s[1]) && InFuture(x + r[0] + 2 * s[0], y + r[1] + 2 * s[1]) && InFutureStart(x + r[0] + s[0], y + r[1] + s[1]) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InFutureAbs(leftField) && !InFutureAbs(straightField) && !InFutureAbs(rightField))
            {
                //touching the end at the straight right corner. Can the future line go in other direction?
                T("CheckNearFutureSide mid right");
                forbidden.Add(rightField);
                forbidden.Add(straightField);
            }
            else if (InFuture(x + 2 * s[0], y + 2 * s[1]) && InFuture(x + l[0] + 2 * s[0], y + l[1] + 2 * s[1]) && InFutureStart(x + l[0] + s[0], y + l[1] + s[1]) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InFutureAbs(leftField) && !InFutureAbs(straightField) && !InFutureAbs(rightField))
            {
                T("CheckNearFutureSide mid left");
                forbidden.Add(leftField);
                forbidden.Add(straightField);
            }
            else //check possible across fields. 2 across is in future, 1 across is free, plug 1 forward (0316 can otherwise go wrong). 1 to the side is also free, but we don't need to check it. Both sides can be true simultaneousl[1], example: 0413
                 //Condition has to hold right even in situations like 0425_1
            {
                /*if (InFutureStart(x + 2 * r[0] + 2 * s[0], y + 2 * r[1] + 2 * s[1]) && !InFuture(x + r[0] + s[0], y + r[1] + s[1]) && !InFuture(x + s[0], y + s[1]) && !InFutureF(rightField)
					&& !InTaken(x + r[0] + s[0], y + r[1] + s[1]) && !InTaken(x + s[0], y + s[1]) )
				{
					//2 to right and 1 forward, plus 1 to right and 2 forward is also free
					if (!InFuture(x + 2 * r[0] + s[0], y + 2 * r[1] + s[1]) && !InFuture(x + r[0] + 2 * s[0], y + r[1] + 2 * s[1]))
					{
						T("CheckNearFutureSide across right");

						if (!InFutureStartIndex(i) && !InFutureEndIndex(i))
						{
							T("Not start of section");
							int[] nextField = path2[i - 1];
							//goes to right further
							if (nextField[0] == x + 3 * r[0] + 2 * s[0] &&
								nextField[1] == y + 3 * r[1] + 2 * s[1])
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
					else if (!(InTakenAbs(leftField) || InFutureF(leftField) || InBorderAbs(leftField)))
					{
						T("CheckNearFutureSide across right, entered at start");
						forbidden.Add(rightField);
						forbidden.Add(straightField);
					}
				}

				//left across
				if (InFutureStart(x + 2 * l[0] + 2 * s[0], y + 2 * l[1] + 2 * s[1]) && !InFuture(x + l[0] + s[0], y + l[1] + s[1]) && !InFuture(x + s[0], y + s[1]) && !InTaken(x + l[0] + s[0], y + l[1] + s[1]) && !InTaken(x + s[0], y + s[1]) && !InFutureF(leftField))
				{
					if (!InFuture(x + 2 * l[0] + s[0], y + 2 * l[1] + s[1]) && !InFuture(x + l[0] + 2 * s[0], y + l[1] + 2 * s[1]))
					{
						T("CheckNearFutureSide across left");

						if (!InFutureStartIndex(i) && !InFutureEndIndex(i))
						{
							T("Not start of section");
							int[] nextField = path2[i - 1];
							//goes to right further
							if (nextField[0] == x + 3 * l[0] + 2 * s[0] &&
								nextField[1] == y + 3 * l[1] + 2 * s[1])
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
					else if (!(InTakenAbs(rightField) || InFutureF(rightField) || InBorderAbs(rightField)))
					{
						T("CheckNearFutureSide across left, entered at start");
						forbidden.Add(leftField);
						forbidden.Add(straightField);
					}
				}*/
            }
        }

        private void CheckNearFutureEnd()
		{
			if (InFutureEnd(x + l[0] + s[0], y + l[1] + s[1]))
			{
				T("CheckNearFutureEnd to left");
				forbidden.Add(rightField);
				//0804_1 makes this untrue: forbidden.Add(straightField);
			}
			else if (InFutureEnd(x + r[0] + s[0], y + r[1] + s[1]))
			{
				T("CheckNearFutureEnd to right");
				forbidden.Add(leftField);
			}
		}

		// Check functions end here

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

		private bool CountArea(int startX, int startY, int endX, int endY)
        {            
            int xDiff = startX - endX;
            int yDiff = startY - endY;
            
            List<int[]> areaLine = new List<int[]> { new int[] { startX, startY } };

            List<int[]> directions;

            if (circleDirectionLeft)
            {
                directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } };
            }
            else
            {
                directions = new List<int[]> { new int[] { 0, 1 }, new int[] { -1, 0 }, new int[] { 0, -1 }, new int[] { 1, 0 } };
            }

			int currentDirection = -1;
			int i = -1;
			foreach (int[] direction in directions)
			{
				i++;
				if (direction[0] == xDiff && direction[1] == yDiff)
				{
					currentDirection = i;
					break;
				}
			}

			// find coorinates of the top left (circleDirection = right) or top right corner (circleDirection = left)
            int minY = startY;
            int limitX = startX;
            int startIndex = 0;

            int nextX = startX;
            int nextY = startY;

            if (InTaken(nextX + xDiff, nextY + yDiff)) // in cases of nearfield across situation. When circling to left, now we have to turn right.
            {
                currentDirection = currentDirection == 0 ? 3 : currentDirection - 1;
            }
            nextX += directions[currentDirection][0];
            nextY += directions[currentDirection][1];
            areaLine.Add(new int[] { nextX, nextY });

            while (!(nextX == endX && nextY == endY))
			{
				currentDirection = currentDirection == 3 ? 0 : currentDirection + 1;
				i = currentDirection;
                int possibleNextX = nextX + directions[currentDirection][0];
                int possibleNextY = nextY + directions[currentDirection][1];
                
                while (InBorder(possibleNextX, possibleNextY) || InTaken(possibleNextX, possibleNextY))
                {
                    i = (i == 0) ? 3 : i - 1;
                    possibleNextX = nextX + directions[i][0];
                    possibleNextY = nextY + directions[i][1];
                }
				currentDirection = i;

				nextX = possibleNextX;
				nextY = possibleNextY;
                areaLine.Add(new int[] { nextX, nextY });

                if (nextY < minY)
                {
                    minY = nextY;
                    limitX = nextX;
                    startIndex = areaLine.Count - 1;
                }
                else if (nextY == minY)
                {
                    if (circleDirectionLeft) //top right corner
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
            }

            /*foreach (int[] area in areaLine)
			{
				T(area[0] + " " + area[1]);
			}*/

			//Special cases are not yet programmed in here as in MainWindow.xaml.cs. We take a gradual approach, starting from the cases that can happen on 7 x 7.

            List<int[]> startSquares = new List<int[]>();
            List<int[]> endSquares = new List<int[]>();
            int[] startCandidate = new int[] { limitX, minY };
            int[] endCandidate = new int[] { limitX, minY };
            int currentY = minY;

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

                if (fieldY > currentY)
                {
                    if (circleDirectionLeft)
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
                    if (circleDirectionLeft)
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

			//add last field
            if (circleDirectionLeft)
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

            count = endSquares.Count;
            int area = 0;

            if (startSquares.Count != count)
            {
                T("Count of start and end squares are inequal: " + startSquares.Count + " " + count);
                foreach (int[] f in startSquares)
                {
                    T("startSquares " + f[0] + " " + f[1]);
                }
                foreach (int[] f in endSquares)
                {
                    T("endSquares " + f[0] + " " + f[1]);
                }
				window.errorInWalkthrough = true;
                window.StopAll("Count of start and end squares are inequal: " + startSquares.Count + " " + count);
                return false;
            }

            for (i = 0; i < count; i++)
            {
                area += endSquares[i][0] - startSquares[i][0] + 1;
            }

            T("Count area: " + area);
            if (area % 2 == 1)
            {
				T("Count area is impair.");
                return false;
            }

            return true;
        }

        // ----- Check functions start -----

        public bool InBorderAbs(int[] field)
        {
            int x = field[0];
            int y = field[1];
            return InBorder(x, y);
        }

        public bool InBorderRel(int left, int straight)
        {
            int x = this.x + left * l[0] + straight * s[0];
            int y = this.y + left * l[1] + straight * s[1];
            return InBorder(x, y);
        }

        public bool InBorder(int x, int y) // allowing negative values could cause an error in AddFutureLines 2x2 checking
		{
			if (x == 0 || x == size + 1 || y == 0 || y == size + 1) return true;
			return false;
		}

        public bool InTakenAbs(int[] field0)
        {
            int x = field0[0];
            int y = field0[1];

            return InTaken(x, y);
        }

        public bool InTakenRel(int left, int straight, int searchStart = -1)
        {
            int x = this.x + left * l[0] + straight * s[0];
            int y = this.y + left * l[1] + straight * s[1];
            return InTaken(x, y, searchStart);
        }

        public bool InTaken(int x, int y, int searchStart = -1) //more recent fields are more probable to encounter, so this way processing time is optimized
		{
			if (!isMain)
			{
				//In AddFutureLines, path2 of future gets null
				if (path2 != null)
				{
					int c2 = path2.Count;

                    if (searchStart != -1) // for checking C shape middle field
                    {
                        searchStart = c2 - 1;
                    }
                    else // for checking possibilities, the last field of the main line is considered empty when the near end is extended
                    {
                        searchStart = isNearEnd? c2 -2: c2 - 1;
                    }
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
                for (int i = c1 - 1; i >= 0; i--)
                {
                    int[] field = path[i];
                    //T(x + " " + y + " " + field[0] + " " + field[1]);
                    if (x == field[0] && y == field[1]) // In 0919_1 (step right, the near end of the bottom line extends), even if the near end being stepped on is now inactive, it is not an option (Near end cannot connect to near end.) Active checking is unnecessary.
                    {
                        return true;
                    }
                }
/* does it have any use?
                // only check the current section or section merge. Other future lines may preceed or come after the current one, irrespective of the creation order. Example: 0910
                if (nearSection == farSection)
				{
					int[] section = MainWindow.futureSections[nearSection];

                    for (int i = section[1]; i <= section[0]; i++)
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
                    foreach (int[] merge in MainWindow.futureSectionMerges)
                    {
                        if (merge[1] == nearSection)
						{
                            for (int i = 0; i < merge.Length; i++)
                            {
                                int[] section = MainWindow.futureSections[merge[i]];
                                for (int j = section[1]; j <= section[0]; j++)
                                {
                                    int[] field = path[j];
                                    if (x == field[0] && y == field[1])
                                    {
                                        return true;
                                    }
                                }
                            }
                        }                            
                    }
                }	*/		
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

        public bool InFutureAbs(int[] f)
        {
            return InFuture(f[0], f[1]);
        }

        public bool InFutureRel(int left, int straight)
        {
            int x = this.x + left * l[0] + straight * s[0];
            int y = this.y + left * l[1] + straight * s[1];
            return InFuture(x, y);
        }

        public bool InFuture(int x, int y)
        {
            List<int[]> searchPath = isMain ? window.future.path : path;
            int c = searchPath.Count;
            if (c == 0) return false;

            for (int i = c - 1; i >= 0; i--)
            {
                int[] field = searchPath[i];
                if (x == field[0] && y == field[1])
                {
                    return true;
                }
            }
            return false;
        }

        public bool InFutureStartAbs(int[] f, int nearSection = -1) // absulute position
		{
			return InFutureStart(f[0], f[1], nearSection);
		}

        public bool InFutureStartRel(int left, int straight) // relative position
        {
                int x = this.x + left * l[0] + straight * s[0];
                int y = this.y + left * l[1] + straight * s[1];
                return InFutureStart(x, y);
        }

		public bool InFutureStart(int x, int y, int nearSection = -1)
		{
            List<int[]> searchPath = isMain ? window.future.path : path;
            int c = searchPath.Count;
			if (c == 0) return false;

			int foundIndex = -1;

			int i;
			for (i = c - 1; i >= 0; i--)
			{
				int[] field = searchPath[i];

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
                    // for checking possible steps, all near ends from the current section/merge cannot be stepped on. For checking C-shape, the section can be whatever
                    if (nearSection == -1 || nearSection != -1 && i != nearSection)
                    {
                        // We examine all mearges. The start of them can be stepped on, but all the others cannot.
                        foreach (int[] merge in MainWindow.futureSectionMerges)
                        {
                            for (int j = 1; j < merge.Length; j++)
                            {
                                if (merge[j] == i) return false;
                            }
                        }

                        return true;
                    }
                    // else i == nearSection: Found field is not a start that can be stepped on. 
                }
            }

            return false;
		}

        public bool InFutureEndAbs(int[] f, int farSection = -1) // absulute position
        {
            return InFutureEnd(f[0], f[1], farSection);
        }

        public bool InFutureEndRel(int left, int straight) // relative position
        {
            int x = this.x + left * l[0] + straight * s[0];
            int y = this.y + left * l[1] + straight * s[1];
            return InFutureEnd(x, y);
        }

        public bool InFutureEnd(int x, int y, int farSection = -1)
		{
            List<int[]> searchPath = isMain ? window.future.path : path;
            int c = searchPath.Count;
            if (c == 0) return false;

			if (x == size && y == size) return false; // a far end that reached the corner is not considered live.

			int foundIndex = -1;

			int i;
			for (i = c - 1; i >= 0; i--)
			{
				int[] field = searchPath[i];
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
                if (section[1] == foundIndex)
                {
                    // for checking possible steps, all far ends from the current section/merge cannot be stepped on. For checking C-shape, the section can be whatever
                    if (farSection == -1 || farSection != -1 && i != farSection)
                    {
                        // We examine all mearges. The end of them can be stepped on, but all the others cannot.
                        foreach (int[] merge in MainWindow.futureSectionMerges)
                        {
                            for (int j = 0; j < merge.Length - 1; j++)
                            {
                                if (merge[j] == i) return false;
                            }
                        }

                        return true;
                    }
                }
            }

            return false;
		}

        public int InTakenIndex(int x, int y)
        {
            int c = path.Count;
            for (int i = 0; i < c; i++)
            {
                int[] field = path[i];
                if (x == field[0] && y == field[1])
                {
                    return i;
                }
            }
            return -1;
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

        // ----- Check functions end -----

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
