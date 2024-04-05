using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OneWayLabyrinth
{
	public partial class Path
	{
		MainWindow window;
		int size;
		public List<int[]> path;
		public List<int[]> path2 = new List<int[]>(); //future line uses the main line to check forbidden fields
        public List<int[]> nextStepPath = new List<int[]>();
        bool isMain = true;
		public bool isNearEnd = false;
		public int count;
		public List<int[]> possible = new List<int[]>(); //field coordinates
        public List<int[]> nextStepPossible = new List<int[]>();
        List<int[]> forbidden = new List<int[]>();
		public int x, y, x2, y2;
        public int sx = 0; //straight, left and right coordinates
        public int sy = 0;
        public int lx = 0;
        public int ly = 0;
        public int rx = 0;
        public int ry = 0;
        public int thisSx = 0; // remain constant in one step, while the above variables change for the InTakenRel calls.
        public int thisSy = 0;
        public int thisLx = 0;
        public int thisLy = 0;
        int[] straightField = new int[] { };
		int[] leftField = new int[] { };
		int[] rightField = new int[] { };
		List<int[]> directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } }; //down, right, up, left
        int foundSectionStart, foundSectionEnd;
        //bool CShape = false;
        bool CShapeLeft = false;
        bool CShapeRight = false;

        bool closeStraight = false;
        bool closeMidAcross = false;
        bool closeAcross = false;

        bool isNextStep = false;

        bool closeStraightSmall = false;
        bool closeMidAcrossSmall = false;
        bool closeAcrossSmall = false;
        bool closeStraightLarge = false;
        bool closeMidAcrossLarge = false;
        bool closeAcrossLarge = false;

        public List<List<int[]>> examAreaLines = new();
        public List<int> examAreaLineTypes = new();
        public List<bool> examAreaLineDirections = new();
        //used only for displaying area
        public List<List<int[]>> areaLines = new();
        public List<int> areaLineTypes = new();
        public List<bool> areaLineDirections = new();
        public List<List<int[]>> areaPairFields = new();
        public List<List<int[]>> areaImpairFields = new();

        List<object> info;

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

		public void NextStepPossibilities(bool isNearEnd, int index, int nearSection, int farSection, bool isNextStep = false)
		{
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
					thisSx = sx = directions[i][0];
					thisSy = sy = directions[i][1];

                    if (x - x0 == sx && y - y0 == sy)
					{
                        T("--- direction: " + i);
                        if (i == 0)
						{
							thisLx = lx = directions[1][0];
                            thisLy = ly = directions[1][1];
							rx = directions[3][0];
							ry = directions[3][1];
						}
						else if (i == 3)
						{
                            thisLx = lx = directions[0][0];
                            thisLy = ly = directions[0][1];
							rx = directions[2][0];
							ry = directions[2][1];
						}
						else
						{
                            thisLx = lx = directions[i + 1][0];
                            thisLy = ly = directions[i + 1][1];
                            rx = directions[i - 1][0];
                            ry = directions[i - 1][1];
                        }

                        straightField = new int[] { x + sx, y + sy };
						leftField = new int[] { x + lx, y + ly };
						rightField = new int[] { x + rx, y + ry };

                        if (!window.calculateFuture)
                        {
                            if (!InTakenAbs(straightField) && !InBorderAbs(straightField))
                            {
                                T("possible straight");
                                possible.Add(straightField);
                            }
                            if (!InTakenAbs(rightField) && !InBorderAbs(rightField))
                            {
                                T("possible right");
                                possible.Add(rightField);
                            }
                            if (!InTakenAbs(leftField) && !InBorderAbs(leftField))
                            {
                                T("possible left");
                                possible.Add(leftField);
                            }
                        }
                        else
                        {
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
                        }
                        

                        // A future line may connect to another section as in 0714_2 when we step up, and a 2x2 line is created on the left
                        // For connecting to an older line, see 0730
                        // It cannot connect to the end of the same section
						if (!isMain) {

                            if (isNearEnd && !window.inFuture) // main line can be connected to if it is not already connected to another future line
                            {
                                int c2 = path2.Count;
                                if (path2[c2 - 1][0] == straightField[0] && path2[c2 - 1][1] == straightField[1]) possible.Add(straightField);
                                if (path2[c2 - 1][0] == rightField[0] && path2[c2 - 1][1] == rightField[1]) possible.Add(rightField);
                                if (path2[c2 - 1][0] == leftField[0] && path2[c2 - 1][1] == leftField[1]) possible.Add(leftField);
                            }

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
                        else if (window.calculateFuture)
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

						//CShape = false;

                        if (!isMain)
                        {
                            CheckFutureCShape();

                            if (!isNearEnd) // when the far end of the future line extends, it should be checked for border as in 0714. Find out the minimum size for when it is needed.
                            {
                                if (size >= 21) // 0430_2. Find out the minimum size
                                {
                                    Check1x3();
                                }
                            }
                        }
                        else
                        {
                            T("--- rules checking ");

                            this.isNextStep = isNextStep;

                            closeStraight = false;
                            closeMidAcross = false;
                            closeAcross = false;
                            //CShapeLeft = false;
                            //CShapeRight = false;

                            CheckCShape();


                            CheckStraight();
                            T("CheckLeftRightAreaUp");
                            CheckLeftRightAreaUp();
                            T("CheckLeftRightCorner");
                            CheckLeftRightCorner();
                            T("CheckLeftRightArea");
                            CheckLeftRightArea();

                            if (!closeStraight && !closeMidAcross && !closeAcross)
                            {
                                T("CheckLeftRightAreaUpBig");
                                CheckLeftRightAreaUpBig();
                                T("CheckLeftRightCornerBig");
                                CheckLeftRightCornerBig();
                            }                            

                            //CheckNearBorder();
                            //CheckNearField();
                            //CheckAreaNearBorder();
                            
                            
                            //RunRules();

                            /*if (size >= 7)
                            {
                                // when a straight C-shape is true, CheckNearField close straight would be as well, disabling the straight opportunity
                                // CheckNearField(); // 0901, 0917_1, 0917_4

                                // --- Relative rules, created by the editor:

                                // Side back

                                // Side front

                                // Side front L

                                // Future L
                                // 0821, 0827
                                // the start and end fields have to be in the same section, otherwise they can connect, like in 0913
                                // conditions are true already on 5x5 at 0831_2, but it is handled in CheckNearCorner

                                // Future 2 x 2 Start End
                                // 0909_1, 0909_1_2
                                // On boards larger than 7 x 7, is it possible to apply the rule in straight left/right directions? It means that in the original example, the line is coming downwards instead of heading right.

                                // Future 2 x 3 Start End
                                // 0915
                                // Is there a situation where the start and end fields are not part of one future line?
                                // On boards larger than 7 x 7, is it possible to apply the rule in straight left/right directions? It means that in the original example, the line is coming downwards instead of heading right.

                                // Future 3 x 3 Start End
                                //0916

                            }
                            if (size >= 9)
                            {
                                // Count Area 3 x 3
                                // 1008

                                // Future 2 x 2 Start End 9
                                // 1010_4
                            }*/

                            /*if (!closeStraightSmall && !closeMidAcrossSmall && !closeAcrossSmall && !closeStraightLarge && !closeMidAcrossLarge) // if there is a close across large obstacle leading to a large area, there can be valid rules on the other side, see 2707632
                            {
                                CheckAreaNearBorder(); // Uses countarea, see 0909. A 2x2 area would be created with one way to go in and out
                                                       // With the exception of closeAcross large area, all near field rules disable two fields, leaving only one option. Running further rules are not necessary. 
                                                       // Example of interference: 1031_1

                                CheckLeftRightArea();
                                CheckLeftRightAreaUp();
                                CheckLeftRightAreaUpBig();
                                CheckLeftRightCorner();
                                CheckLeftRightCornerBig();
                                RunRules();
                            }*/



                            // CountArea3x3 2,2: 1021_1

                            /* (not actual yet)
                            if (size >= 21)
                            {
                                // found on 21x21, may recreate the situation on smaller boards.
                                CheckNearFutureStartEnd();
                                CheckNearFutureSide();
                                // 0630, but needs to work with 0804_1 too
                                // CheckNearFutureEnd();
                            } */
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

            if (isNextStep) return;

            // check each possible field if they would result in an impossible situation

            T("--- check future poss");

            List<int[]> savedPath = Copy(path);
            List<int[]> savedPossible = Copy(possible);

            forbidden = new List<int[]>();

            foreach (int[] field in savedPossible)
            {
                T("--- check future poss " + field[0] + " " + field[1]);
                path = Copy(savedPath);
                path.Add(field);
                x = field[0];
                y = field[1];
                NextStepPossibilities(true, -1, -1, -1, true);

                if (possible.Count == 0)
                {
                    forbidden.Add(new int[] { field[0], field[1] });
                }
            }

            newPossible = new List<int[]>();

            foreach (int[] field in savedPossible)
            {
                if (!InForbidden(field))
                {
                    newPossible.Add(field);
                }
            }

            possible = newPossible;
            path = Copy(savedPath);
            x = path[path.Count - 1][0];
            y = path[path.Count - 1][1];
        }

        public void ResetExamAreas()
        {
            examAreaLines = new();
            examAreaLineTypes = new();
            examAreaLineDirections = new();
        }

        public void AddExamAreas() // if a rule is true, we display all examined circles.
        {
            if (!isNextStep)
            {
                for (int i = 0; i < examAreaLines.Count; i++)
                {
                    areaLines.Add(examAreaLines[i]);
                    areaLineTypes.Add(examAreaLineTypes[i]);
                    areaLineDirections.Add(examAreaLineDirections[i]);
                }
            }                       
        }

        public void CheckCShape()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if ((InTakenRel(2, 0) || InBorderRel(2, 0)) && (InTakenRel(1, -1) || InBorderRel(1, -1)) && !InTakenRel(1, 0) && !InCornerRel(1, 0))
                    {
                        //CShape = true;
                        /*if (j == 0)
                        {
                            if (i == 0)
                            {
                                CShapeLeft = true;
                            }
                            else
                            {
                                CShapeRight = true;
                            }
                        }*/
                        forbidden.Add(new int[] { x + sx, y + sy });
                        forbidden.Add(new int[] { x - lx, y - ly });
                    }
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

		public void CheckFutureCShape() // Even future line can make a straight C-shape, see 0727_1
        {
            //T("CheckFutureCShape " + sx + " " + sy + " " + lx + " " + ly + " " + path.Count + " " + path[path.Count - 1][0] + " " + path[path.Count - 1][1]);
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int[] liveEnd = path2[path2.Count - 1];
                    if (!(x + lx == size && y + ly == size) && (InTakenRel(1, -1) || InBorderRel(1, -1)) && (InTakenRel(2, 0) || InBorderRel(2, 0)) && !InTakenRel(1, 0)
                        && !InFutureStartRel(1, -1) && !InFutureEndRel(1, -1)
                        && !InFutureStartRel(2, 0) && !InFutureEndRel(2, 0)
                        && !(isNearEnd && !window.inFuture && liveEnd[0] == x + lx - sx && liveEnd[1] == y + ly - sy)
                         && !(isNearEnd && !window.inFuture && liveEnd[0] == x + 2 * lx && liveEnd[1] == y + 2 * ly))
                    {
                        T("Future C Shape");
                        //CShape = true;
                        forbidden.Add(new int[] { x - lx, y - ly }); //right
                        forbidden.Add(new int[]{ x + sx, y + sy}); //straight				
                    }
                    //turn right, pattern goes upwards
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                //mirror directions
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
		}

        public void CheckNearBorder()
        {
            // going right on down side and going down on right side cases are checked by the CShape rule.

            // going up on left side
            if (x == 2 && leftField[0] == 1 && !InTakenAbs(leftField) && !InTaken(x - 1, y - 1))
            {
                forbidden.Add(leftField);
            }
            // going left on up side
            else if (y == 2 && rightField[1] == 1 && !InTakenAbs(rightField) && !InTaken(x - 1, y - 1))
            {
                forbidden.Add(rightField);
            }

            if (y == size - 1 && leftField[1] == size && !InTakenAbs(leftField) && !InTaken(x - 1, y + 1) && !InBorder(x - 1, y + 1)) // going left on down side
            {
                forbidden.Add(leftField);
            }
            else if (x == size - 1 && rightField[0] == size && !InTakenAbs(rightField) && !InTaken(x + 1, y - 1) && !InBorder(x + 1, y - 1)) //going up on right side
            {
                forbidden.Add(rightField);
            }

            //going towards an edge
            if (x + 2 * sx == 0)
            {
                forbidden.Add(leftField);
                if (!InTaken(x - 1, y - 1))
                {
                    forbidden.Add(straightField);
                }
            }
            else if (x + 2 * sx == size + 1)
            {
                forbidden.Add(rightField);
                if (!InTaken(x + 1, y - 1) && y != 1)
                {
                    forbidden.Add(straightField);
                }
            }
            else if (y + 2 * sy == 0)
            {
                forbidden.Add(rightField);
                if (!InTaken(x - 1, y - 1))
                {
                    forbidden.Add(straightField);
                }
            }
            else if (y + 2 * sy == size + 1)
            {
                forbidden.Add(leftField);
                if (!InTaken(x - 1, y + 1) && x != 1)
                {
                    forbidden.Add(straightField);
                }
            }    
        }

        /* May not be needed, countarea on the border takes care of it
        private void CheckNearCorner() // 0811_2, which also satisfies CheckFutureL rule
        {
            //First condition is needed for when we are at 4,5 and the left field is 4,4 on a 5x5 field
            if (y != size && leftField[0] == size - 1 && leftField[1] == size - 1)
            {
                forbidden.Add(leftField);
            }
            else if (x != size && rightField[0] == size - 1 && rightField[1] == size - 1)
            {
                forbidden.Add(rightField);
            }
        }*/


        // 7 x 7

        /* May not be needed, countarea on the border takes care of it
         * private void CheckCOnFarBorder() // 0831_1_2
        {
            // Applies from 7x7, see 0831_1_2. Similar to CheckNearCorner, it is just not at the corner.
            if (x == size - 2 && rightField[0] == size - 1 && !InTakenRel(-1, -1) && InTakenRel(-1, -2))
            {
                T("COnFarBorder horizontal");
                forbidden.Add(rightField);
            }
            else if (y == size - 2 && leftField[1] == size - 1 && !InTakenRel(1, -1) && InTakenRel(1, -2))
            {
                T("COnFarBorder vertical");
                forbidden.Add(leftField);
            }
        }*/

        public void CheckAreaNearBorder() // 0909. Check both straight approach and side.
        {
            if (x == 3 && straightField[0] == 2 && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InTaken(1, y))
            {
                T("CheckArea left");
                if (CountArea(2, y - 1, 1, y - 1, null, false, 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(leftField);
                }
            }
            else if (y == 3 && straightField[1] == 2 && !InTakenAbs(straightField) && !InTakenAbs(leftField) && !InTaken(x, 1))
            {
                T("CheckArea up");
                if (CountArea(x - 1, 2, x - 1, 1, null, true, 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(rightField);
                }
            }
            else if (x == size - 2 && y >= 2 && straightField[0] == size - 1 && !InTakenAbs(straightField) && !InTakenAbs(leftField) && !InTaken(size, y))
            {
                T("CheckArea right");
                if (CountArea(size - 1, y - 1, size, y - 1, null, true, 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(rightField);
                }
            }
            else if (y == size - 2 && x >= 2 && straightField[1] == size - 1 && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InTaken(x, size))
            {
                T("CheckArea down");
                if (CountArea(x - 1, size - 1, x - 1, size, null, false, 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(leftField);
                }
            }
            else if (x == 3 && y >= 4 && leftField[0] == 2 && !InTaken(3, y - 1) && !InTaken(1, y) && !InTaken(2, y - 2)) //straight and left field cannot be taken, but it is enough we check the most left field on border. Also, 1 left and 2 up, 2 left and 2 up cannot be taken in order to draw an arealine. Checking 1 left and 2 up is enough.
            {
                T("CheckArea left side");
                if (CountArea(2, y - 1, 1, y - 1, null, false, 1))
                {
                    forbidden.Add(leftField);
                }
            }
            else if (y == 3 && x >= 4 && rightField[1] == 2 && !InTaken(x - 1, 3) && !InTaken(x, 1) && !InTaken(x - 2, 2))
            {
                T("CheckArea up side");
                if (CountArea(x - 1, 2, x - 1, 1, null, true, 1))
                {
                    forbidden.Add(rightField);
                }
            }
            else if (x == size - 2 && y >= 3 && rightField[0] == size - 1 && !InTaken(size - 2, y - 1) && !InTaken(size, y) && !InTaken(size - 1, y - 2))
            {
                T("CheckArea right side");
                if (CountArea(size - 1, y - 1, size, y - 1, null, true, 1))
                {
                    forbidden.Add(rightField);
                }
            }
            else if (y == size - 2 && x >= 3 && leftField[1] == size - 1 && !InTaken(x - 1, size - 2) && !InTaken(x, size) && !InTaken(x - 2, size - 1))
            {
                T("CheckArea down side");
                if (CountArea(x - 1, size - 1, x - 1, size, null, false, 1))
                {
                    forbidden.Add(leftField);
                }
            }
        }

        public void CheckStraight()
        {
            for (int i = 0; i < 2; i++)
            {
                //if (i == 0 && CShapeLeft) return;
                //if (i == 1 && CShapeRight) return;

                bool circleDirectionLeft = (i == 0) ? true : false;
                int dist = 1;
                List<int[]> borderFields = new();

                while (!InTakenRel(0, dist) && !InBorderRel(0, dist))
                {
                    dist++;
                }

                if (!InTakenRel(1, 1))
                {
                    int ex = dist - 1;

                    if (ex == 1)
                    {
                        int i1 = InTakenIndexRel(0, 2);
                        int i2 = InTakenIndexRel(1, 2);

                        closeStraight = true;

                        if (i2 > i1) // area on left
                        {
                            forbidden.Add(new int[] { x + sx, y + sy });
                            forbidden.Add(new int[] { x - lx, y - ly });
                        }
                    }

                    if (ex > 2)
                    {
                        for (int j = ex - 1; j >= 2; j--)
                        {
                            borderFields.Add(new int[] { 0, j });
                        }
                    }

                    T("CheckStraight dist " + ex);
                    if (ex > 1 && !InTakenRel(1, ex))
                    {
                        ResetExamAreas();

                        // circle type 3 means the start field will be drawn white
                        // same as UpBig case
                        if (CountAreaRel(0, 1, 0, ex, borderFields, circleDirectionLeft, 3, true))
                        {
                            int black = (int)info[1];
                            int white = (int)info[2];

                            T("CheckStraight black " + black + " white " + white + " mod " + ex % 4);

                            int whiteDiff = white - black;
                            int nowWCount = 0;
                            int nowWCountRight = 0;
                            int nowBCount = 0;
                            int laterWCount = 0;
                            int laterBCount = 0;

                            switch (ex % 4)
                            {
                                case 0:
                                    nowWCountRight = nowWCount = ex / 4;
                                    nowBCount = ex / 4 - 1;
                                    laterWCount = ex / 4;
                                    laterBCount = ex / 4;
                                    break;
                                case 1:
                                    nowWCountRight = nowWCount = (ex + 3) / 4;
                                    nowBCount = (ex - 5) / 4;
                                    laterWCount = (ex - 1) / 4;
                                    laterBCount = (ex - 1) / 4;
                                    break;
                                case 2:
                                    if (ex == 2)
                                    {
                                        nowWCountRight = 1;
                                        nowWCount = 0;
                                    }
                                    else
                                    {
                                        nowWCountRight = nowWCount = (ex + 2) / 4;
                                        nowBCount = (ex - 2) / 4;
                                        laterWCount = (ex - 2) / 4;
                                        laterBCount = (ex - 2) / 4;
                                    }                                    
                                    break;
                                case 3:
                                    nowWCountRight = nowWCount = (ex + 1) / 4;
                                    nowBCount = (ex - 3) / 4;
                                    laterWCount = (ex + 1) / 4;
                                    laterBCount = (ex - 3) / 4;
                                    break;
                            }

                            if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount)) // not in range
                            {
                                T("Cannot enter now up");
                                forbidden.Add(new int[] { x + sx, y + sy });
                                AddExamAreas();
                                areaPairFields.Add((List<int[]>)info[3]);
                            }
                            if (!(whiteDiff <= nowWCountRight && whiteDiff >= -nowBCount)) // not in range
                            {
                                T("Cannot enter now");
                                forbidden.Add(new int[] { x + lx, y + ly });
                                AddExamAreas();
                                areaPairFields.Add((List<int[]>)info[3]);
                            }
                            if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                            {
                                T("Cannot enter later");
                                forbidden.Add(new int[] { x - lx, y - ly });
                                AddExamAreas();
                                areaPairFields.Add((List<int[]>)info[3]);
                            }
                        }
                    }
                }

                lx = -lx;
                ly = -ly;
            }
            lx = thisLx;
            ly = thisLy;
        }

        public void CheckLeftRightArea()
        // check area that goes from 1,1 to the side. If we step left, we enter the area. Otherwise, we will enter it later.
        {
            for (int i = 0; i < 2; i++)
            {
                //if (i == 0 && CShapeLeft) return;
                //if (i == 1 && CShapeRight) return;

                bool circleDirectionLeft = (i == 0) ? true : false;             
                int dist = 1;
                List<int[]> borderFields = new();

                while (!InTakenRel(dist, 1) && !InBorderRel(dist, 1))
                {
                    dist++; 
                }

                int ex = dist - 1;

                if (ex > 2)
                {
                    for (int j = ex - 1; j >= 2; j--)
                    {
                        borderFields.Add(new int[] { j, 1 });
                    }
                }
                if (ex > 1 && !InTakenRel(1, 0) && !InTakenRel(ex + 1, 0)) //area cannot be drawn if one of these fields is taken
                {
                    int i1 = InTakenIndexRel(ex + 1, 1);
                    int i2 = InTakenIndexRel(ex + 1, 2);

                    if (i1 > i2) // area downwards
                    {
                        ResetExamAreas();
                        if (CountAreaRel(1, 1, ex, 1, borderFields, circleDirectionLeft, 2, true))
                        {
                            int black = (int)info[1];
                            int white = (int)info[2];

                            T("CheckLeftRightArea black " + black + " white " + white + " mod " + ex % 4);

                            switch (ex % 4)
                            {
                                case 0:
                                    if (black >= white + ex / 4)
                                    {
                                        T("Cannot enter now");
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                        AddExamAreas();
                                        areaPairFields.Add((List<int[]>)info[3]); //list of black fields, will be drawn black
                                    }
                                    break;
                                case 1:
                                    if (black >= white + (ex + 3) / 4)
                                    {
                                        T("Cannot enter now");
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                        AddExamAreas();
                                        areaPairFields.Add((List<int[]>)info[3]);
                                    }
                                    break;
                                case 2:
                                    if (black >= white + (ex + 2) / 4)
                                    {
                                        T("Cannot enter now");
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                        AddExamAreas();
                                        areaPairFields.Add((List<int[]>)info[3]);
                                    }
                                    if (white >= black + (ex + 2) / 4)
                                    {
                                        T("Cannot enter later");
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        forbidden.Add(new int[] { x - lx, y - ly });
                                        AddExamAreas();
                                        areaPairFields.Add((List<int[]>)info[3]);
                                    }
                                    break;
                                case 3:
                                    break;

                            }
                        }
                    }
                }
                lx = -lx;
                ly = -ly;
            }
            lx = thisLx;
            ly = thisLy;
        }

        public void CheckLeftRightAreaUp()
        {
            for (int i = 0; i < 2; i++)
            {
                //if (i == 0 && CShapeLeft) return;
                //if (i == 1 && CShapeRight) return;

                bool circleDirectionLeft = (i == 0) ? true : false;
                int dist = 1;

                List<int[]> borderFields = new();
                while (!InTakenRel(1, dist) && !InBorderRel(1, dist))
                {
                    dist++;
                }

                if (!InTakenRel(0, dist))
                {
                    int ex = dist - 1;

                    if (ex == 1)
                    {
                        int i1 = InTakenIndexRel(1, 2);
                        int i2 = InTakenIndexRel(2, 2);

                        if (i2 > i1) // area on left
                        {
                            closeMidAcross = true;

                            forbidden.Add(new int[] { x + sx, y + sy });
                            forbidden.Add(new int[] { x - lx, y - ly });
                        }
                    }

                    if (ex > 2)
                    {
                        for (int j = ex - 1; j >= 2; j--)
                        {
                            borderFields.Add(new int[] { 1, j });
                        }
                    }

                    T("CheckLeftRightAreaUp dist " + ex);
                    if (ex > 1 && !InTakenRel(2, 1) && !InTakenRel(2, ex)) //area cannot be drawn if one of these fields is taken
                    {
                        ResetExamAreas();

                        if (CountAreaRel(1, 1, 1, ex, borderFields, circleDirectionLeft, 2, true))
                        {
                            int black = (int)info[1];
                            int white = (int)info[2];

                            T("CheckLeftRightAreaUp black " + black + " white " + white + " mod " + ex % 4);

                            int whiteDiff = white - black;
                            int nowWCount = 0;
                            int nowBCount = 0;
                            int laterWCount = 0;
                            int laterBCount = 0;

                            switch (ex % 4)
                            {
                                /* // range approach
                                case 0:
                                    nowWCount = ex / 4;
                                    nowBCount = ex / 4 - 1;
                                    laterWCount = ex / 4;
                                    laterBCount = ex / 4;
                                    break;
                                case 1:
                                    nowWCount = (ex - 1) / 4;
                                    nowBCount = (ex - 1) / 4;
                                    laterWCount = (ex - 1) / 4;
                                    laterBCount = (ex - 1) / 4;
                                    break;
                                case 2:
                                    nowWCount = (ex + 2) / 4;
                                    nowBCount = (ex - 2) / 4; ;
                                    laterWCount = (ex - 2) / 4;
                                    laterBCount = (ex - 2) / 4;
                                    break;
                                case 3:
                                    nowWCount = (ex + 1) / 4;
                                    nowBCount = (ex - 3) / 4;
                                    laterWCount = (ex - 3) / 4;
                                    laterBCount = (ex + 1) / 4;
                                    break;*/

                                // exclusive approach
                                case 0:
                                    if (black >= white + ex / 4)
                                    {
                                        T("Cannot enter now");
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                        AddExamAreas();
                                        areaPairFields.Add((List<int[]>)info[3]);
                                    }
                                    break;
                                case 1:
                                    break;
                                case 2:
                                    if (white >= black + (ex + 2) / 4)
                                    {
                                        T("Cannot enter later");
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        forbidden.Add(new int[] { x - lx, y - ly });
                                        AddExamAreas();
                                        areaPairFields.Add((List<int[]>)info[3]);
                                    }
                                    break;
                                case 3:
                                    if (white >= black + (ex + 1) / 4)
                                    {
                                        T("Cannot enter later ");
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        forbidden.Add(new int[] { x - lx, y - ly });
                                        AddExamAreas();
                                        areaPairFields.Add((List<int[]>)info[3]);
                                    }
                                    if (black >= white + (ex + 1) / 4)
                                    {
                                        T("Cannot enter now");
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                        AddExamAreas();
                                        areaPairFields.Add((List<int[]>)info[3]);
                                    }
                                    break;
                            }

                            /*if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount)) // not in range
                            {
                                T("CheckLeftRightCorner cannot enter now");
                                forbidden.Add(new int[] { this.x + lx, y + ly });
                                AddExamAreas();
                                areaPairFields.Add((List<int[]>)info[3]);
                            }
                            if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                            {
                                T("CheckLeftRightCorner cannot enter later");
                                forbidden.Add(new int[] { this.x + sx, y + sy });
                                forbidden.Add(new int[] { this.x - lx, y - ly });
                                AddExamAreas();
                                areaPairFields.Add((List<int[]>)info[3]);
                            }*/
                        }
                    }
                }
                lx = -lx;
                ly = -ly;
            }
            lx = thisLx;
            ly = thisLy;
        }

        public void CheckLeftRightAreaUpBig()
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;
                int dist = 1;
                List<int[]> borderFields = new();

                while (!InTakenRel(1, dist) && !InBorderRel(1, dist))
                {
                    dist++;
                }

                if (!InTakenRel(0, dist))
                {
                    int ex = dist - 1;

                    if (ex == 1)
                    {
                        int i1 = InTakenIndexRel(1, 2);
                        int i2 = InTakenIndexRel(2, 2);

                        if (i1 > i2) // area on right
                        {
                            forbidden.Add(new int[] { x + sx, y + sy });
                            forbidden.Add(new int[] { x + lx, y + ly });                          
                        }
                    }

                    if (ex > 2)
                    {
                        for (int j = ex - 1; j >= 2; j--)
                        {
                            borderFields.Add(new int[] { 0, j });
                        }
                    }

                    T("CheckLeftRightAreaUpBig dist " + ex);
                    if (ex > 1 && !InTakenRel(-1, 1) && !InTakenRel(-1, ex) && !InBorderRel(-1, 1)  && !InBorderRel(-1, ex)) //area cannot be drawn if one of these fields is taken
                    {
                        ResetExamAreas();

                        // circle type 3 means the start field will be drawn white
                        if (CountAreaRel(0, 1, 0, ex, borderFields, circleDirectionLeft, 3, true))
                        {
                            int black = (int)info[1];
                            int white = (int)info[2];

                            T("CheckLeftRightAreaUpBig black " + black + " white " + white + " mod " + ex % 4);

                            int whiteDiff = white - black;
                            int nowWCount = 0;
                            int nowWCountRight = 0;
                            int nowBCount = 0;
                            int laterWCount = 0;
                            int laterBCount = 0;

                            switch (ex % 4)
                            {
                                case 0:
                                    nowWCountRight = nowWCount = ex / 4;
                                    nowBCount = ex / 4 - 1;
                                    laterWCount = ex / 4;
                                    laterBCount = ex / 4;
                                    break;
                                case 1:
                                    nowWCountRight = nowWCount = (ex + 3) / 4;
                                    nowBCount = (ex - 5) / 4;
                                    laterWCount = (ex - 1) / 4;
                                    laterBCount = (ex - 1) / 4;
                                    break;
                                case 2:
                                    if (ex == 2)
                                    {
                                        nowWCountRight = 1;
                                        nowWCount = 0;
                                    }
                                    else
                                    {
                                        nowWCountRight = nowWCount = (ex + 2) / 4;
                                        nowBCount = (ex - 2) / 4;
                                        laterWCount = (ex - 2) / 4;
                                        laterBCount = (ex - 2) / 4;
                                    }
                                    break;
                                case 3:
                                    nowWCountRight = nowWCount = (ex + 1) / 4;
                                    nowBCount = (ex - 3) / 4;
                                    laterWCount = (ex + 1) / 4;
                                    laterBCount = (ex - 3) / 4;
                                    break;
                            }

                            if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount)) // not in range
                            {
                                T("Cannot enter now up");
                                forbidden.Add(new int[] { x + sx, y + sy });
                                AddExamAreas();
                                areaPairFields.Add((List<int[]>)info[3]);
                            }
                            if (!(whiteDiff <= nowWCountRight && whiteDiff >= -nowBCount)) // not in range
                            {
                                T("Cannot enter now");
                                forbidden.Add(new int[] { x - lx, y - ly });
                                AddExamAreas();
                                areaPairFields.Add((List<int[]>)info[3]);
                            }
                            if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                            {
                                T("Cannot enter later");
                                forbidden.Add(new int[] { x + lx, y + ly });
                                AddExamAreas();
                                areaPairFields.Add((List<int[]>)info[3]);
                            }
                        }
                    }
                }
                lx = -lx;
                ly = -ly;
            }
            lx = thisLx;
            ly = thisLy;
        }

        public void CheckLeftRightCorner()
        {
            for (int j = 0; j < 2; j++)
            {
                // enter now and forward area has to be empty
                if (!(InTakenRel(1, 0) || InBorderRel(1, 0) || InTakenRel(0, 1) || InBorderRel(0, 1)))
                {
                    int hori = 1;
                    int vert = 2;

                    while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                    {
                        hori++;

                        while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                        {
                            hori++;
                        }

                        if (InBorderRel(hori, vert))
                        {
                            vert++;
                            hori = 1;
                            continue;
                        }

                        if (hori == 2 && vert == 2) // close across
                        {
                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori, vert + 1);

                            if (i2 < i1)
                            {
                                closeAcross = true;

                                forbidden.Add(new int[] { x + sx, y + sy });
                                forbidden.Add(new int[] { x - lx, y - ly });
                                continue;
                            }
                        }

                        if (!InTakenRel(hori, vert - 1))
                        {
                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori, vert + 1);

                            if (i2 < i1) // area is downwards
                            {
                                T("CheckLeftRightCorner Corner found at side " + j + " horizontal " + hori + " vertical " + vert + " i1 " + i1 + " i2 " + i2);

                                bool takenFound = false;
                                int left1 = 1;
                                int straight1 = 1;
                                int left2 = hori - 1;
                                int straight2 = vert - 1;
                                List<int[]> borderFields = new();

                                int nowWCount, nowBCount, laterWCount, laterBCount;
                                int x, n;

                                //check if all fields on the border line is free
                                if (vert == hori)
                                {
                                    x = hori - 1;
                                    nowWCount = 0;
                                    nowBCount = x - 1;
                                    laterWCount = -1;// means B = 1
                                    laterBCount = x - 1;

                                    for (int i = 1; i < hori; i++)
                                    {
                                        if (i < hori - 1)
                                        {
                                            if (InTakenRel(i, i) || InTakenRel(i + 1, i))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (InTakenRel(i, i))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }

                                        if (i == 1)
                                        {
                                            borderFields.Add(new int[] { 2, 1 });
                                        }
                                        else if (i < hori - 1)
                                        {
                                            borderFields.Add(new int[] { i, i });
                                            borderFields.Add(new int[] { i + 1, i });
                                        }
                                    }
                                }
                                else if (hori > vert)
                                {
                                    x = vert - 1;
                                    n = (hori - vert - (hori - vert) % 2) / 2;

                                    if ((hori - vert) % 2 == 0)
                                    {
                                        if (n > 1)
                                        {
                                            nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                        }
                                        else
                                        {
                                            nowWCount = 0;
                                        }
                                        nowBCount = x + (n - 1 - (n - 1) % 2) / 2;
                                        laterWCount = (n - n % 2) / 2;
                                        laterBCount = x + (n - n % 2) / 2;
                                    }
                                    else
                                    {
                                        if (n > 0)
                                        {
                                            nowWCount = x + (n - n % 2) / 2;
                                            laterBCount = (n + 2 - (n + 2) % 2) / 2;
                                        }
                                        else
                                        {
                                            nowWCount = x - 1;
                                            laterBCount = 0;
                                        }
                                        nowBCount = (n + 1 - (n + 1) % 2) / 2;
                                        laterWCount = x - 1 + (n + 1 - (n + 1) % 2) / 2;

                                    }

                                    for (int i = 1; i <= hori - vert; i++)
                                    {
                                        if (InTakenRel(i, 1))
                                        {
                                            takenFound = true;
                                            break;
                                        }

                                        if (i > 1)
                                        {
                                            borderFields.Add(new int[] { i, 1 });
                                        }
                                    }

                                    for (int i = 1; i < vert; i++)
                                    {
                                        if (i < vert - 1)
                                        {
                                            if (InTakenRel(hori - vert + i, i) || InTakenRel(hori - vert + i + 1, i))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (InTakenRel(hori - vert + i, i))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }

                                        if (i < vert - 1)
                                        {
                                            borderFields.Add(new int[] { hori - vert + i, i });
                                            borderFields.Add(new int[] { hori - vert + i + 1, i });
                                        }
                                    }
                                }
                                else // vert > hori
                                {
                                    x = hori - 1;
                                    n = (vert - hori - (vert - hori) % 2) / 2;

                                    if ((vert - hori) % 2 == 0)
                                    {
                                        nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                        nowBCount = x + (n - 1 - (n - 1) % 2) / 2;
                                        laterWCount = (n - n % 2) / 2;
                                        laterBCount = x + (n - n % 2) / 2;
                                    }
                                    else
                                    {
                                        nowWCount = 1 + (n + 1 - (n + 1) % 2) / 2;
                                        nowBCount = x - 1 + (n - n % 2) / 2;
                                        if (n > 0)
                                        {
                                            laterWCount = 1 + (n - n % 2) / 2;
                                        }
                                        else
                                        {
                                            laterWCount = 0;
                                        }
                                        laterBCount = x - 1 + (n + 1 - (n + 1) % 2) / 2;
                                    }

                                    for (int i = 1; i < hori; i++)
                                    {
                                        if (i < hori - 1 && hori > 2)
                                        {
                                            if (InTakenRel(i, i) || InTakenRel(i + 1, i))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (InTakenRel(i, i))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }

                                        if (hori > 2) // there is no stair if corner is at 1 distance, only one field which is the start field.
                                        {
                                            if (i == 1)
                                            {
                                                borderFields.Add(new int[] { 2, 1 });
                                            }
                                            else if (i < hori - 1)
                                            {
                                                borderFields.Add(new int[] { i, i });
                                                borderFields.Add(new int[] { i + 1, i });
                                            }
                                            else
                                            {
                                                borderFields.Add(new int[] { i, i });
                                            }
                                        }
                                    }

                                    for (int i = 1; i <= vert - hori; i++)
                                    {
                                        if (InTakenRel(hori - 1, hori - 1 + i))
                                        {
                                            takenFound = true;
                                            break;
                                        }

                                        if (i < vert - hori)
                                        {
                                            borderFields.Add(new int[] { hori - 1, hori - 1 + i });
                                        }
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int i = borderFields.Count - 1; i >= 0; i--)
                                    {
                                        newBorderFields.Add(borderFields[i]);
                                    }

                                    ResetExamAreas();

                                    // here, true means that count area succeeds, does not run into an error
                                    if (CountAreaRel(left1, straight1, left2, straight2, newBorderFields, j == 0 ? true : false, 2, true))
                                    {
                                        int impair = (int)info[0];
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        T("nowWCount " + nowWCount + " nowBCount " + nowBCount + " laterWCount " + laterWCount + " laterBCount " + laterBCount);
                                        T("impair " + impair + " black " + black + " white " + white);

                                        int whiteDiff = white - black;

                                        if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount)) // not in range
                                        {
                                            T("CheckLeftRightCorner cannot enter now");
                                            forbidden.Add(new int[] { this.x + lx, y + ly });
                                            AddExamAreas();
                                            areaPairFields.Add((List<int[]>)info[3]);
                                        }
                                        if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                        {
                                            T("CheckLeftRightCorner cannot enter later");
                                            forbidden.Add(new int[] { this.x + sx, y + sy });
                                            forbidden.Add(new int[] { this.x - lx, y - ly });
                                            AddExamAreas();
                                            areaPairFields.Add((List<int[]>)info[3]);
                                        }


                                    }
                                }
                                else
                                {
                                    T("arealine taken");
                                }
                            }
                        }

                        vert++;
                        hori = 1;
                    }
                }     
                
                lx = -lx;
                ly = -ly;
            }
            lx = thisLx;
            ly = thisLy;
        }

        public void CheckLeftRightCornerBig()
        {
            for (int j = 0; j < 2; j++)
            {
                // enter now and forward area has to be empty
                if (!(InTakenRel(1, 0) || InBorderRel(1, 0) || InTakenRel(0, 1) || InBorderRel(0, 1)))
                {
                    int hori = 1;
                    int vert = 2;

                    while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                    {
                        hori++;

                        while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                        {
                            hori++;
                        }

                        if (InBorderRel(hori, vert))
                        {
                            vert++;
                            hori = 1;
                            continue;
                        }

                        if (hori == 2 && vert == 2) // close across
                        {
                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori, vert + 1);

                            if (i2 > i1)
                            {
                                closeAcrossLarge = true;

                                if (j == 0)
                                {
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }
                                else
                                {
                                    forbidden.Add(new int[] { x - lx, y - ly });
                                }
                                continue;
                            }                            
                        }

                        if (!InTakenRel(hori, vert - 1))
                        {
                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori, vert + 1);

                            if (i2 > i1) // area is upwards
                            {
                                T("CheckLeftRightCornerBig Corner found at side " + j + " horizontal " + hori + " vertical " + vert + " i1 " + i1 + " i2 " + i2);

                                bool takenFound = false;
                                int left1 = 1;
                                int straight1 = 1;
                                int left2 = hori - 1;
                                int straight2 = vert - 1;
                                List<int[]> borderFields = new();

                                int nowWCount, nowWCountRight, nowBCount, laterWCount, laterBCount;
                                int x, n;

                                //check if all fields on the border line is free
                                if (vert == hori)
                                {
                                    x = hori - 1;
                                    nowWCountRight = nowWCount = 0;
                                    nowBCount = x - 1;
                                    laterWCount = -1;// means B = 1
                                    laterBCount = x - 1;

                                    for (int i = 1; i < hori; i++)
                                    {
                                        if (i < hori - 1)
                                        {
                                            if (InTakenRel(i, i) || InTakenRel(i, i + 1))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (InTakenRel(i, i))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }

                                        if (i == 1)
                                        {
                                            borderFields.Add(new int[] { 1, 2 });
                                        }
                                        else if (i < hori - 1)
                                        {
                                            borderFields.Add(new int[] { i, i });
                                            borderFields.Add(new int[] { i, i + 1 });
                                        }
                                    }
                                }
                                else if (hori > vert)
                                {
                                    x = vert - 1;
                                    n = (hori - vert - (hori - vert) % 2) / 2;

                                    if ((hori - vert) % 2 == 0)
                                    {
                                        nowWCountRight = nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                        nowBCount = x + (n - 1 - (n - 1) % 2) / 2;
                                        laterWCount = (n - n % 2) / 2;
                                        laterBCount = x + (n - n % 2) / 2;
                                    }
                                    else
                                    {
                                        nowWCountRight = nowWCount = 1 + (n + 1 - (n + 1) % 2) / 2;
                                        nowBCount = x - 1 + (n - n % 2) / 2;
                                        if (n > 0)
                                        {
                                            laterWCount = 1 + (n - n % 2) / 2;
                                        }
                                        else
                                        {
                                            laterWCount = 0;
                                        }
                                        laterBCount = x - 1 + (n + 1 - (n + 1) % 2) / 2;
                                    }

                                    for (int i = 1; i < vert; i++)
                                    {
                                        if (i < vert - 1 && vert > 2)
                                        {
                                            if (InTakenRel(i, i) || InTakenRel(i, i + 1))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (InTakenRel(i, i))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }

                                        if (vert > 2) // there is no stair if corner is at 1 distance, only one field which is the start field.
                                        {
                                            if (i == 1)
                                            {
                                                borderFields.Add(new int[] { 1, 2 });
                                            }
                                            else if (i < vert - 1)
                                            {
                                                borderFields.Add(new int[] { i, i });
                                                borderFields.Add(new int[] { i, i + 1 });
                                            }
                                            else
                                            {
                                                borderFields.Add(new int[] { i, i });
                                            }
                                        }
                                    }

                                    for (int i = 1; i <= hori - vert; i++)
                                    {
                                        if (InTakenRel(vert - 1 + i, vert - 1))
                                        {
                                            takenFound = true;
                                            break;
                                        }

                                        if (i < hori - vert)
                                        {
                                            borderFields.Add(new int[] { vert - 1 + i, vert - 1 });
                                        }
                                    }
                                }
                                else // vert > hori
                                {
                                    x = hori - 1;
                                    n = (vert - hori - (vert - hori) % 2) / 2;

                                    if ((vert - hori) % 2 == 0)
                                    {
                                        if (n > 1)
                                        {
                                            nowWCountRight = nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                        }
                                        else
                                        {
                                            nowWCount = 0;
                                            nowWCountRight = 1;
                                        }
                                        nowBCount = x + (n - 1 - (n - 1) % 2) / 2;
                                        laterWCount = (n - n % 2) / 2;
                                        laterBCount = x + (n - n % 2) / 2;
                                    }
                                    else
                                    {
                                        if (n > 0)
                                        {
                                            nowWCountRight = nowWCount = x + (n - n % 2) / 2;
                                            laterBCount = (n + 2 - (n + 2) % 2) / 2;
                                        }
                                        else
                                        {
                                            nowWCount = x - 1;
                                            nowWCountRight = x;
                                            laterBCount = 0;
                                        }
                                        nowBCount = (n + 1 - (n + 1) % 2) / 2;
                                        laterWCount = x - 1 + (n + 1 - (n + 1) % 2) / 2;

                                    }

                                    for (int i = 1; i <= vert - hori; i++)
                                    {
                                        if (InTakenRel(1, i))
                                        {
                                            takenFound = true;
                                            break;
                                        }

                                        if (i > 1)
                                        {
                                            borderFields.Add(new int[] { 1, i });
                                        }
                                    }

                                    for (int i = 1; i < hori; i++)
                                    {
                                        if (i < hori - 1)
                                        {
                                            if (InTakenRel(i, vert - hori + i) || InTakenRel(i, vert - hori + i + 1))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (InTakenRel(i, vert - hori + i))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }

                                        if (i < hori - 1)
                                        {
                                            borderFields.Add(new int[] { i, vert - hori + i });
                                            borderFields.Add(new int[] { i, vert - hori + i + 1 });
                                        }
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int i = borderFields.Count - 1; i >= 0; i--)
                                    {
                                        newBorderFields.Add(borderFields[i]);
                                    }

                                    ResetExamAreas();

                                    // here, true means that count area succeeds, does not run into an error
                                    if (CountAreaRel(left1, straight1, left2, straight2, newBorderFields, j == 0 ? false : true, 2, true))
                                    {
                                        int impair = (int)info[0];
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        T("nowWCount " + nowWCount + " nowBCount " + nowBCount + " laterWCount " + laterWCount + " laterBCount " + laterBCount);
                                        T("impair " + impair + " black " + black + " white " + white);

                                        int whiteDiff = white - black;

                                        if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount)) // not in range
                                        {
                                            T("CheckLeftRightCornerBig cannot enter now up");
                                            forbidden.Add(new int[] { this.x + sx, y + sy });
                                            AddExamAreas();
                                            areaPairFields.Add((List<int[]>)info[3]);
                                        }
                                        if (!(whiteDiff <= nowWCountRight && whiteDiff >= -nowBCount)) // not in range
                                        {
                                            T("CheckLeftRightCornerBig cannot enter now right");
                                            forbidden.Add(new int[] { this.x - lx, y - ly });
                                            AddExamAreas();
                                            areaPairFields.Add((List<int[]>)info[3]);
                                        }
                                        if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                        {
                                            T("CheckLeftRightCornerBig cannot enter later");
                                            forbidden.Add(new int[] { this.x + lx, y + ly });
                                            AddExamAreas();
                                            areaPairFields.Add((List<int[]>)info[3]);
                                        }
                                    }
                                }
                                else
                                {
                                    T("arealine taken");
                                }
                            }
                        }

                        vert++;
                        hori = 1;
                    }
                }

                lx = -lx;
                ly = -ly;
            }
            lx = thisLx;
            ly = thisLy;
        }

        public void CheckNearField()
        {
            bool farSideStraightUp = false;
            bool farSideStraightDown = false;
            bool farSideMidAcrossUp = false;
            bool farSideMidAcrossDown = false;

            // for 2-distance simultaneous rules
            bool farStraightLeft = false;
            bool farStraightRight = false;
            bool farSideUp;
            bool farSideDown;

            // used also for determining if custom rules have to run
            closeStraightSmall = false;
            closeMidAcrossSmall = false;
            closeAcrossSmall = false;
            closeStraightLarge = false;
            closeMidAcrossLarge = false;
            closeAcrossLarge = false;

            for (int i = 0; i < 2; i++)
            {
                bool closeStraight = false;
                bool closeMidAcross = false;

                if (InTakenRel(0, 2) && InTakenRel(1, 2) && !InTakenRel(0, 1))
                {
                    closeStraight = true;

                    forbidden.Add(new int[] { x + sx, y + sy });

                    int middleIndex = InTakenIndexRel(0, 2);
                    int sideIndex = InTakenIndexRel(1, 2);
                    if (sideIndex > middleIndex) // area on left
                    {
                        closeStraightSmall = true;
                        forbidden.Add(new int[] { x - lx, y - ly });
                    }
                    else
                    {
                        closeStraightLarge = true;
                        forbidden.Add(new int[] { x + lx, y + ly });
                    }
                }

                if (!closeStraight)
                {
                    if (InTakenRel(1, 2) && !InTakenRel(0, 1) && !InTakenRel(1, 1))
                    {
                        closeMidAcross = true;

                        forbidden.Add(new int[] { x + sx, y + sy });

                        int middleIndex = InTakenIndexRel(1, 2);
                        int sideIndex = InTakenIndexRel(2, 2);
                        if (sideIndex > middleIndex)
                        {
                            closeMidAcrossSmall = true;
                            forbidden.Add(new int[] { x - lx, y - ly });
                        }
                        else
                        {
                            closeMidAcrossLarge = true;
                            forbidden.Add(new int[] { x + lx, y + ly });
                        }
                    }
                }

                if (!closeStraight && !closeMidAcross)
                {
                    if (InTakenRel(2, 2) && !InTakenRel(0, 1) && !InTakenRel(1, 1) && !InTakenRel(2, 1))
                    {
                        int middleIndex = InTakenIndexRel(2, 2);
                        int sideIndex = InTakenIndexRel(3, 2);
                        if (sideIndex > middleIndex)
                        {
                            closeAcrossSmall = true;
                            forbidden.Add(new int[] { x + sx, y + sy });
                            forbidden.Add(new int[] { x - lx, y - ly });
                        }
                        else
                        {
                            closeAcrossLarge = true;
                            forbidden.Add(new int[] { x + lx, y + ly });
                        }
                    }
                }

                lx = -lx;
                ly = -ly;
            }
            lx = thisLx;
            ly = thisLy;

            // Far rules shouldn't be checked until close rules are checked on both sides, see 305112. Here, close straight is only true on the right side, but left side far rules get checked before.
            // A close rule may be true on one side, but on the other side there can be a far rule, like in 1307639. The close rule has to be large in this case.

            // A large close mid across on one side can have a small far across on the other side.
            // A large close across on one side can have a small far mid across / across on the other side.
            // Only the last case needs to be examined. All the other close rules have two fields disabled.

            if (!closeStraightSmall && !closeMidAcrossSmall && !closeAcrossSmall && !closeStraightLarge && !closeMidAcrossLarge)
            {
                for (int i = 0; i < 2; i++)
                {
                    bool farStraight = false;
                    bool farMidAcross = false;

                    if (InTakenRel(0, 3) && InTakenRel(1, 3) && !InTakenRel(0, 2) && !InTakenRel(0, 1)) // 0, 2: 1225; 0, 1: 1226
                    {
                        T("Far straight");
                        farStraight = true;

                        int middleIndex = InTakenIndexRel(0, 3);
                        int sideIndex = InTakenIndexRel(1, 3);
                        if (sideIndex > middleIndex) // area on left
                        {
                            if (!InTakenRel(1, 2) && !InTakenRel(2, 2)) // 1,2: 1019_4, 2,2: 1019_5
                            {
                                T("Far straight small");
                                if (i == 0) farStraightLeft = true; else farStraightRight = true;

                                bool circleDirectionLeft = i == 0 ? true : false;
                                if (CountAreaRel(1, 1, 1, 2, null, circleDirectionLeft, 1))
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                    forbidden.Add(new int[] { x - lx, y - ly });
                                }
                                else if (InTakenRel(-2, 1) && InTakenRel(-1, 0) && !InTakenRel(-1, 1))
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                }
                            }
                        }
                        else // area on right
                        {
                            if (!InTakenRel(-1, 2) && !InTakenRel(-2, 2)) // -1, 2: 1019_6, -2, 2: 1019_7
                            {
                                T("Far straight large");
                                bool circleDirectionLeft = i == 0 ? false : true;
                                if (CountAreaRel(-1, 1, -1, 2, null, circleDirectionLeft, 1))
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }
                                else if (InTakenRel(2, 1) && InTakenRel(1, 0) && !InTakenRel(1, 1))
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                }
                            }
                        }
                    }

                    if (!farStraight)
                    {
                        if (InTakenRel(1, 3) && InTakenRel(2, 3) && !InTakenRel(0, 2) && !InTakenRel(1, 2) && !InTakenRel(0, 1)) // 0, 2; 1, 2: 1019_3
                        {
                            T("Far mid across");
                            farMidAcross = true;

                            int middleIndex = InTakenIndexRel(1, 3);
                            int sideIndex = InTakenIndexRel(2, 3);
                            if (sideIndex > middleIndex) // area on left
                            {
                                if (!InTakenRel(2, 2)) // 2, 2: 1019
                                {
                                    T("Far mid across small");
                                    if (i == 0) farStraightLeft = true; else farStraightRight = true;
                                    bool circleDirectionLeft = i == 0 ? true : false;
                                    if (CountAreaRel(1, 1, 1, 2, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        forbidden.Add(new int[] { x - lx, y - ly });
                                    }
                                    else if (InTakenRel(-2, 1) && InTakenRel(-1, 0) && !InTakenRel(-1, 1))
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                    }
                                }
                            }
                            else // area on right
                            {
                                if (!InTakenRel(-1, 2)) // -1, 2: 1019_1
                                {
                                    T("Far mid across large");
                                    bool circleDirectionLeft = i == 0 ? false : true;
                                    if (CountAreaRel(0, 1, 0, 2, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                    else if (InTakenRel(2, 1) && InTakenRel(1, 0) && !InTakenRel(1, 1))
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                    }
                                }
                            }
                        }

                        if (!farMidAcross)
                        {
                            if (InTakenRel(2, 3) && InTakenRel(3, 3) && !InTakenRel(0, 2) && !InTakenRel(1, 2) && !InTakenRel(2, 2) && !InTakenRel(0, 1))
                            {
                                T("Far across");
                                int middleIndex = InTakenIndexRel(2, 3);
                                int sideIndex = InTakenIndexRel(3, 3);
                                if (sideIndex > middleIndex) // area on left
                                {
                                    T("Far across small");
                                    if (i == 0) farStraightLeft = true; else farStraightRight = true;
                                    bool circleDirectionLeft = i == 0 ? true : false;
                                    if (CountAreaRel(1, 1, 1, 2, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        forbidden.Add(new int[] { x - lx, y - ly });
                                    }
                                    else
                                    {
                                        if (InTakenRel(-2, 1) && InTakenRel(-1, 0) && !InTakenRel(-1, 1))
                                        {
                                            forbidden.Add(new int[] { x + sx, y + sy });
                                        }
                                        /*if (InTakenRel(1, 4) && !InTakenRel(1, 3)) // end C, there is a separate rule for that now
                                        {
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                        }*/
                                    }
                                }
                                else // area on right
                                {
                                    T("Far across large");
                                    if (!InTakenRel(-1, 2))
                                    {
                                        bool circleDirectionLeft = i == 0 ? false : true;
                                        if (CountAreaRel(0, 1, 1, 2, new List<int[]> { new int[] { 0, 2 } }, circleDirectionLeft, 0))
                                        {
                                            forbidden.Add(new int[] { x + sx, y + sy });
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                        }
                                        else if (InTakenRel(2, 1) && InTakenRel(1, 0) && !InTakenRel(1, 1))
                                        {
                                            forbidden.Add(new int[] { x + sx, y + sy });
                                        }
                                    }
                                }
                            }
                        }
                    }
                    lx = -lx;
                    ly = -ly;
                }
                lx = thisLx;
                ly = thisLy;
            }

            if (farStraightLeft && farStraightRight) // 9:234256
            {
                T("farStraightLeft and farStraightRight true");
                forbidden.Add(new int[] { x + sx, y + sy });
            }

            // left/right side rules
            // When any of the close rules are present, even close across large, examining side rules is not necessary. Example: 1019_8
            if (!closeStraightSmall && !closeMidAcrossSmall && !closeAcrossSmall && !closeStraightLarge && !closeMidAcrossLarge && !closeAcrossLarge)
            {
                for (int i = 0; i < 2; i++)
                {
                    bool closeSideStraight = false;
                    bool closeSideMidAcross = false;
                    bool closeSideAcross = false;
                    farSideUp = false;
                    farSideDown = false;
                    farSideStraightUp = false;
                    farSideStraightDown = false;
                    farSideMidAcrossUp = false;
                    farSideMidAcrossDown = false;
                    bool circleDirectionLeft = i == 0 ? false : true;

                    if (InTakenRel(2, 0) && !InTakenRel(1, 0) && !InTakenRel(1, 1))
                    {
                        closeSideStraight = true;
                        forbidden.Add(new int[] { x + lx, y + ly });
                    }

                    if (!closeSideStraight)
                    {
                        if (!InTakenRel(1, 0) && !InTakenRel(1, 1) && (InTakenRel(2, 1) || InTakenRel(2, -1) && !InTakenRel(1, -1)))
                        {
                            closeSideMidAcross = true;
                            forbidden.Add(new int[] { x + lx, y + ly });
                        }
                    }

                    if (!closeSideStraight && !closeSideMidAcross)
                    {
                        if (InTakenRel(2, 2) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(1, 2))
                        {
                            closeSideAcross = true;
                            // fields forbidden in straight rules
                        }
                    }

                    if (!closeSideStraight && !closeSideMidAcross && !closeSideAcross)
                    {
                        if (InTakenRel(3, 0) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(1, 2))
                        {
                            int middleIndex = InTakenIndexRel(3, 0);
                            if (InTakenRel(3, 1)) // up side taken
                            {
                                T("farSideStraight up");
                                farSideStraightUp = true;

                                int sideIndex = InTakenIndexRel(3, 1);
                                if (sideIndex > middleIndex) // area up
                                {
                                    farSideUp = true;

                                    if (CountAreaRel(1, 1, 2, 1, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                    else if ((InTakenRel(1, -2) || InBorderRel(1, -2)) && !InTakenRel(1, -1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                }
                            }
                            if (InTakenRel(3, -1)) // down side taken, we need to check sepearately from up side, in order to establish farSideStraightDown
                            {
                                T("farSideStraight down");
                                farSideStraightDown = true;

                                int sideIndex = InTakenIndexRel(3, -1);
                                if (sideIndex < middleIndex) // area up
                                {
                                    if (CountAreaRel(1, 1, 2, 1, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                    else if ((InTakenRel(1, -2) || InBorderRel(1, -2)) && !InTakenRel(1, -1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                }
                                else
                                {
                                    farSideDown = true;
                                }
                            }
                        }

                        if (!farSideStraightUp && !farSideStraightDown)
                        {
                            if (InTakenRel(3, 1) && InTakenRel(3, 2) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(1, 2)) // mid across up
                            {
                                T("farSideMidAcross up");
                                farSideMidAcrossUp = true;

                                int middleIndex = InTakenIndexRel(3, 1);
                                int sideIndex = InTakenIndexRel(3, 2);
                                if (sideIndex > middleIndex) // area up
                                {
                                    farSideUp = true;

                                    if (CountAreaRel(1, 1, 2, 1, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                    else if ((InTakenRel(1, -2) || InBorderRel(1, -2)) && !InTakenRel(1, -1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                }
                            }

                            if (InTakenRel(3, -1) && InTakenRel(3, -2) && !InTakenRel(1, -1) && !InTakenRel(1, 0) && !InTakenRel(1, 1)) // mid across down, 1,1: 1021_8
                            {
                                T("farSideMidAcross down");
                                farSideMidAcrossDown = true;

                                int middleIndex = InTakenIndexRel(3, -1);
                                int sideIndex = InTakenIndexRel(3, -2);
                                if (sideIndex < middleIndex) // area up
                                {
                                    if (CountAreaRel(1, 0, 2, 0, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                    else if (InTakenRel(1, -2))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                }
                                else
                                {
                                    farSideDown = true;
                                }
                            }
                        }

                        // there can be a far side across in the opposite direction of a far side straight or mid across situation
                        if (!farSideStraightUp && !farSideMidAcrossUp && InTakenRel(3, 2) && InTakenRel(3, 3) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(1, 2)) // 1,2: 1021
                        {
                            T("farSideAcross up");

                            int middleIndex = InTakenIndexRel(3, 2);
                            int sideIndex = InTakenIndexRel(3, 3);
                            if (sideIndex > middleIndex) // area up
                            {
                                farSideUp = true;

                                if (CountAreaRel(1, 1, 2, 1, null, circleDirectionLeft, 1))
                                {
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }
                                else
                                {
                                    if ((InTakenRel(1, -2) || InBorderRel(1, -2)) && !InTakenRel(1, -1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                    /*if (InTakenRel(4, 1) && !InTakenRel(3, 1)) // end C
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                    }*/
                                }
                            }
                        }

                        if (!farSideStraightDown && !farSideMidAcrossDown && InTakenRel(3, -2) && InTakenRel(3, -3) && !InTakenRel(1, -1) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(2, -2)) // 2,-2: 630259
                        {
                            T("farSideAcross down");

                            int middleIndex = InTakenIndexRel(3, -2);
                            int sideIndex = InTakenIndexRel(3, -3);

                            T(sideIndex + " " + middleIndex);
                            if (sideIndex < middleIndex) // area up
                            {
                                if (CountAreaRel(1, 0, 2, -1, new List<int[]> { new int[] { 2, 0 } }, circleDirectionLeft, 0))
                                {
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }
                                else if (InTakenRel(1, -2))
                                {
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }
                            }
                            else
                            {
                                farSideDown = true;
                            }
                        }
                    }

                    if (farSideUp && farSideDown) // 9:234256
                    {
                        T("farSideUp and farSideDown true");
                        forbidden.Add(new int[] { x + lx, y + ly });
                    }

                    lx = -lx;
                    ly = -ly;
                }
                lx = thisLx;
                ly = thisLy;
            }

            // we cannot have close side across down going down when farSideUp is true, because the area down has only one entrance.
        }

        // 21 x 21

        private void Check1x3()
        {
            //Certain edges are impossible to fill.

            //     x
            //xo   o   
            // xoo?x
            //  xxxx

            //next step has to be left

            //if I give value to int[] thisS = s, it will change when s changes.
            int thisS0 = sx;
            int thisS1 = sy;
            int thisL0 = lx;
            int thisL1 = ly;

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (InTakenRel(1, -1) && InTakenRel(2, -1) && InTakenRel(3, -1)
                    && InTakenRel(4, 0) && InTakenRel(5, 1)
                    && !InTakenRel(1, 0) && !InTakenRel(2, 0) && !InTakenRel(3, 0)
                    && !InTakenRel(4, 1) && InTakenRel(0, 2))
                    {
                        T("1x3 valid at x " + x + " y " + y);
                        forbidden.Add(new int[] { x + sx, y + sy }); //straight field
                        forbidden.Add(new int[] { x - lx, y - ly }); //right field (per the start direction)
                    }

                    //turn right, pattern goes upwards
                    int temps0 = sx;
                    int temps1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = temps0;
                    ly = temps1;
                }

                //mirror directions
                sx = thisS0;
                sy = thisS1;
                lx = -thisL0;
                ly = -thisL1;
            }

            sx = thisS0;
            sy = thisS1;
            lx = thisL0;
            ly = thisL1;
        }

        public void CheckNearFutureStartEnd()
		{
			T(" x " + x + " " + y + " " + lx + " " + ly + " " + sx + " " + sy);
			T("CheckNearFutureStartEnd x + lx + 2 * sx " + (x + lx + 2 * sx) + " y + ly + 2 * sy " + (y + ly + 2 * sy));
			//meeting start and end ahead. Ends are 2 apart from each other at same forward distance.
			if (InFutureStart(x + 2 * sx, y + 2 * sy) && InFutureEnd(x + 2 * rx + 2 * sx, y + 2 * ry + 2 * sy) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField))
			{
				T("CheckNearFutureStartEnd to right");
				forbidden.Add(rightField);
			}
			else if (InFutureStart(x + 2 * sx, y + 2 * sy) && InFutureEnd(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField))
			{
				T("CheckNearFutureStartEnd to left");
				forbidden.Add(leftField);
			}
			else if (InFutureStart(x + lx + 2 * sx, y + ly + 2 * sy) && InFutureEnd(x + rx + 2 * sx, y + ry + 2 * sy) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField))
			{
				T("CheckNearFutureStartEnd to middle, start on left");
				forbidden.Add(rightField);
				forbidden.Add(straightField);
			}
			else if (InFutureStart(x + rx + 2 * sx, y + ry + 2 * sy) && InFutureEnd(x + lx + 2 * sx, y + ly + 2 * sy) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField))
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
            if (InFutureRel(0, 2) && InFutureRel(-1, 2) && InFutureStartRel(-1, 1) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InFutureAbs(leftField) && !InFutureAbs(straightField) && !InFutureAbs(rightField))
            {
                //touching the end at the straight right corner. Can the future line go in other direction?
                T("CheckNearFutureSide mid right");
                forbidden.Add(rightField);
                forbidden.Add(straightField);
            }
            else if (InFutureRel(0,2) && InFutureRel(1,2) && InFutureStartRel(1,1) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InFutureAbs(leftField) && !InFutureAbs(straightField) && !InFutureAbs(rightField))
            {
                T("CheckNearFutureSide mid left");
                forbidden.Add(leftField);
                forbidden.Add(straightField);
            }
            else //check possible across fields. 2 across is in future, 1 across is free, plug 1 forward (0316 can otherwise go wrong). 1 to the side is also free, but we don't need to check it. Both sides can be true simultaneously, example: 0413
                 //Condition has to hold right even in situations like 0425_1
            {
                /*if (InFutureStart(x + 2 * rx + 2 * sx, y + 2 * ry + 2 * sy) && !InFuture(x + rx + sx, y + ry + sy) && !InFuture(x + sx, y + sy) && !InFutureF(rightField)
					&& !InTaken(x + rx + sx, y + ry + sy) && !InTaken(x + sx, y + sy) )
				{
					//2 to right and 1 forward, plus 1 to right and 2 forward is also free
					if (!InFuture(x + 2 * rx + sx, y + 2 * ry + sy) && !InFuture(x + rx + 2 * sx, y + ry + 2 * sy))
					{
						T("CheckNearFutureSide across right");

						if (!InFutureStartIndex(i) && !InFutureEndIndex(i))
						{
							T("Not start of section");
							int[] nextField = path2[i - 1];
							//goes to right further
							if (nextField[0] == x + 3 * rx + 2 * sx &&
								nextField[1] == y + 3 * ry + 2 * sy)
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
				if (InFutureStart(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy) && !InFuture(x + lx + sx, y + ly + sy) && !InFuture(x + sx, y + sy) && !InTaken(x + lx + sx, y + ly + sy) && !InTaken(x + sx, y + sy) && !InFutureF(leftField))
				{
					if (!InFuture(x + 2 * lx + sx, y + 2 * ly + sy) && !InFuture(x + lx + 2 * sx, y + ly + 2 * sy))
					{
						T("CheckNearFutureSide across left");

						if (!InFutureStartIndex(i) && !InFutureEndIndex(i))
						{
							T("Not start of section");
							int[] nextField = path2[i - 1];
							//goes to right further
							if (nextField[0] == x + 3 * lx + 2 * sx &&
								nextField[1] == y + 3 * ly + 2 * sy)
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
			if (InFutureEnd(x + lx + sx, y + ly + sy))
			{
				T("CheckNearFutureEnd to left");
				forbidden.Add(rightField);
				//0804_1 makes this untrue: forbidden.Add(straightField);
			}
			else if (InFutureEnd(x + rx + sx, y + ry + sy))
			{
				T("CheckNearFutureEnd to right");
				forbidden.Add(leftField);
			}
		}

        // Check functions end here

        public bool CountAreaRel(int left1, int straight1, int left2, int straight2, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false)
        {
            T("CountAreaRel " + left1 + " " + straight1 + " " + left2 + " " + straight2);
            int x1 = x + left1 * lx + straight1 * sx;
            int y1 = y + left1 * ly + straight1 * sy;
            int x2 = x + left2 * lx + straight2 * sx;
            int y2 = y + left2 * ly + straight2 * sy;

            List<int[]> absBorderFields = new();
            if (!(borderFields is null))
            {
                foreach (int[] field in borderFields)
                {
                    absBorderFields.Add(new int[] { x + field[0] * lx + field[1] * sx, y + field[0] * ly + field[1] * sy });
                }
            }

            return CountArea(x1, y1, x2, y2, absBorderFields, circleDirectionLeft, circleType, getInfo);
        }

        private bool CountArea(int startX, int startY, int endX, int endY, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false)
        // compareColors is for the starting situation of 1119, where we mark an impair area and know the entry and the exit field. We count the number of white and black cells of a checkered pattern, the color of the entry and exit should be one more than the other color.
        {
            // find coordinates of the top left (circleDirection = right) or top right corner (circleDirection = left)
            int minY = startY;
            int limitX = startX;
            int startIndex = 0;

            int xDiff, yDiff;
            List<int[]> areaLine;

            if (borderFields == null || borderFields.Count == 0)
            {
                if (Math.Abs(endX - startX) == 2 || Math.Abs(endY - startY) == 2)
                {
                    int middleX = (endX + startX) / 2;
                    int middleY = (endY + startY) / 2;
                    xDiff = startX - middleX;
                    yDiff = startY - middleY;
                    areaLine = new List<int[]> { new int[] { middleX, middleY }, new int[] { startX, startY } };
                }
                else
                {
                    xDiff = startX - endX;
                    yDiff = startY - endY;
                    areaLine = new List<int[]> { new int[] { startX, startY } };
                }
            }
            else
            {
                areaLine = new();
                foreach (int[] field in borderFields)
                {
                    areaLine.Add(new int[] { field[0], field[1] });
                }
                xDiff = startX - borderFields[borderFields.Count - 1][0];
                yDiff = startY - borderFields[borderFields.Count - 1][1];
                areaLine.Add(new int[] { startX, startY });
            }
            

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
            foreach (int[] direction in directions)
            {
                currentDirection++;
                if (direction[0] == xDiff && direction[1] == yDiff)
                {
                    break;
                }
            }

            int nextX = startX;
            int nextY = startY;

            // if the field in straight direction is the live end, we need to turn (right if the circle direction is left). Similarly, if the live end is across on the same side the direction is going.
            int turnedDirection = currentDirection == 0 ? 3 : currentDirection - 1;
            if (x == nextX + xDiff && y == nextY + yDiff || x == nextX + xDiff + directions[turnedDirection][0] && y == nextY + yDiff + directions[turnedDirection][1])
            {
                currentDirection = turnedDirection;
            }

            //In case of an area of 2 or 3
            if (InTaken(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]))
            {
                currentDirection = currentDirection == 0 ? 3 : currentDirection - 1;
            }
            nextX += directions[currentDirection][0];
            nextY += directions[currentDirection][1];

            // if the impair area is only 3 fields, we will now encounter the count area border field.
            if (borderFields != null && borderFields.Count == 1 && nextX == borderFields[0][0] && nextY == borderFields[0][1])
            {
                nextX = endX;
                nextY = endY;
            }

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

            while (!(nextX == endX && nextY == endY))
			{
                int startDirection = currentDirection;
				currentDirection = currentDirection == 3 ? 0 : currentDirection + 1;
				int i = currentDirection;
                int possibleNextX = nextX + directions[currentDirection][0];
                int possibleNextY = nextY + directions[currentDirection][1];
                
                while (InBorder(possibleNextX, possibleNextY) || InTaken(possibleNextX, possibleNextY))
                {
                    i = (i == 0) ? 3 : i - 1;
                    possibleNextX = nextX + directions[i][0];
                    possibleNextY = nextY + directions[i][1];
                }

                // not actual with C-shape allowed when checking other rules
                /*if (i != startDirection && (i - startDirection) % 2 == 0) // opposite direction. Can happen in 1006
                {
                    T("Error at " + startX + " " + startY + " " + endX + " " + endY + " " + possibleNextX + " " + possibleNextY);
                    window.errorInWalkthrough = true;
                    T("Single field in arealine.");
                    foreach (int[] field in areaLine)
                    {
                        T(field[0] + " " + field[1]);
                    }
                    window.M("Single field in arealine.", 1);

                    return false;
                }*/
                
				currentDirection = i;

				nextX = possibleNextX;
				nextY = possibleNextY;

                //T(nextX + " " + nextY);
                // when getting info about area
                if (nextX == size && nextY == size)
                {
                    T("Corner is reached.");
                    return false;
                }

                // not actual with C-shape allowed when checking other rules
                // We may go through the same field twice as in 1208 side across down checking, but that field is a count area border field.
                /*foreach (int[] field in areaLine)
                {
                    if (field[0] == nextX && field[1] == nextY)
                    {
                        bool found = false;
                        if (borderFields != null)
                        {
                            foreach (int[] field2 in borderFields)
                            {                                
                                if (field2[0] == nextX && field2[1] == nextY)
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                        
                        if (!found)
                        {
                            T("Error at sx " + startX + " sy " + startY + " ex " + endX + " ey " + endY + " x " + nextX + " y " + nextY);
                            window.errorInWalkthrough = true;
                            T("Field exists in arealine.");
                            window.M("Field exists in arealine.", 1);
                            return false;
                        }
                    }
                }*/

                areaLine.Add(new int[] { nextX, nextY });
                //T("Adding " + nextX + " " + nextY + " count " + areaLine.Count);

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

            /*T("minY " + minY + " limitX " + limitX + " " + startIndex);
            foreach (int[] a in areaLine)
			{
				T(a[0] + " " + a[1]);
			}*/

            //Special cases are not yet programmed in here as in MainWindow.xaml.cs. We take a gradual approach, starting from the cases that can happen on 7 x 7.

            examAreaLines.Add(areaLine);
            examAreaLineTypes.Add(circleType);
            examAreaLineDirections.Add(circleDirectionLeft);

            int area = 0;
            List<int[]> startSquares = new List<int[]>();
            List<int[]> endSquares = new List<int[]>();

            if (areaLine.Count > 3)
            {
                int[] startCandidate = new int[] { limitX, minY };
                int[] endCandidate = new int[] { limitX, minY };
                int currentY = minY;

                bool singleField = false;
                // check if there is a one square row on the top
                if (startIndex > 0)
                {
                    if (areaLine[startIndex][1] != areaLine[startIndex - 1][1])
                    {
                        singleField = true;
                    }
                }
                else
                {
                    if (areaLine[0][1] != areaLine[areaLine.Count - 1][1])
                    {
                        singleField = true;
                    }
                }

                for (int i = 1; i < areaLine.Count; i++)
                {
                    int index = startIndex + i;
                    if (index >= areaLine.Count)
                    {
                        index -= areaLine.Count;
                    }
                    int[] field = areaLine[index];
                    int fieldX = field[0];
                    int fieldY = field[1];

                    //T(" currentY " + currentY + " fieldX " + fieldX + " fieldY " + fieldY + " startCandidate " + startCandidate[0] + " " + startCandidate[1] + " endCandidate " + endCandidate[0] + " " + endCandidate[1]);

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

                                if (y == fieldY - 1 && x <= fieldX)
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
                                else // stair down right or left, any possible start field is higher up
                                {
                                    endSquares.Add(endCandidate);
                                }
                            }
                            else // stair, no start fields exist
                            {
                                if (singleField)
                                {
                                    startSquares.Add(startCandidate);
                                }
                                endSquares.Add(endCandidate);
                            }
                        }
                        else
                        {
                            if (startSquares.Count > 0)
                            {
                                int[] square = startSquares[startSquares.Count - 1];
                                int x = square[0];
                                int y = square[1];

                                if (y == fieldY - 1 && x >= fieldX)
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
                                if (singleField)
                                {
                                    endSquares.Add(endCandidate);
                                }
                                startSquares.Add(startCandidate);
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

                                if (y == fieldY + 1 && x >= fieldX)
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

                                if (y == fieldY + 1 && x <= fieldX)
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
                        }
                        startCandidate = endCandidate = field;
                    }
                    currentY = fieldY;                    
                }

                //add last field
                if (circleDirectionLeft)
                {
                    startSquares.Add(startCandidate);
                }
                else
                {
                    endSquares.Add(endCandidate);
                }

                /*foreach (int[] sfield in startSquares)
                {
                    T("startsquare: " + sfield[0] + " " + sfield[1]);
                }
                foreach (int[] efield in endSquares)
                {
                    T("endsquare: " + efield[0] + " " + efield[1]);
                }*/

                count = endSquares.Count;

                // it should never happen if the above algorithm is bug-free.
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

                for (int i = 0; i < count; i++)
                {
                    area += endSquares[i][0] - startSquares[i][0] + 1;
                }
            }
            else area = areaLine.Count;

            T("Count area: " + area);
            
            switch (circleType)
            {
                case 0:
                    if (area % 2 == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case 1:
                    if (area % 2 == 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                    break;
                case 2:
                case 3:
                    if (!getInfo && area % 2 == 0)
                    {
                        return false;
                    }
                    else
                    {
                        //Check that the number of black cells are one more than the number of white ones in a checkered pattern.The black color is where we enter and exit the area.

                        int pairCount = 0, impairCount = 0;

                        List<int[]> pairFields = new();
                        List<int[]> impairFields = new();

                        foreach (int[] field in startSquares)
                        {
                            int x = field[0];
                            int y = field[1];
                            int minX = size;

                            //without having open peaks, the first start square should match the last end square. Otherwise, we need to find the ending that is closest to the start field in the row.
                            for (int i = endSquares.Count - 1; i >= 0; i--)
                            {
                                if (endSquares[i][1] == y && endSquares[i][0] >= x)
                                {
                                    if (endSquares[i][0] < minX)
                                    {
                                        minX = endSquares[i][0];
                                    }                                    
                                }
                            }

                            int span = minX - x + 1;

                            if (getInfo)
                            {
                                for (int i = x; i <= minX; i++)
                                {    
                                    if ((i + y) % 2 == 0)
                                    {
                                        pairFields.Add(new int[] { i, y });
                                    }
                                    else
                                    {
                                        impairFields.Add(new int[] { i, y });
                                    }
                                    
                                }
                            }

                            if ((x + y) % 2 == 0)
                            {
                                pairCount += (span + span % 2) / 2;
                                impairCount += (span - span % 2) / 2;
                            }
                            else
                            {
                                impairCount += (span + span % 2) / 2;
                                pairCount += (span - span % 2) / 2;
                            }                            
                        }

                        if (getInfo)
                        {
                            if (circleType == 2)
                            {
                                if ((startX + startY) % 2 == 0)
                                {
                                    info = new List<object> { area % 2, pairCount, impairCount, pairFields };
                                }
                                else
                                {
                                    info = new List<object> { area % 2, impairCount, pairCount, impairFields };
                                }
                            }
                            else
                            {
                                if ((startX + startY) % 2 == 1)
                                {
                                    info = new List<object> { area % 2, pairCount, impairCount, pairFields };
                                }
                                else
                                {
                                    info = new List<object> { area % 2, impairCount, pairCount, impairFields };
                                }
                            }
                            
                            return true;
                        }

                        T("pair " + pairCount + ", impair " + impairCount + " circleType " + circleType);
                        if (circleType == 2 && ((startX + startY) % 2 == 0 && pairCount != impairCount + 1 || (startX + startY) % 2 == 1 && impairCount != pairCount + 1) || circleType == 3 && ((startX + startY) % 2 == 0 && pairCount + 1 != impairCount || (startX + startY) % 2 == 1 && impairCount + 1 != pairCount))
                        {
                            // imbalance in colors, forbidden fields in the rule apply
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }

            }
            return false;
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
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
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

        public bool InTakenRel(int left, int straight)
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;

            //T("InTakenRel " + x + " " + y);
            return InTaken(x, y);
        }

        public bool InTaken(int x, int y) //more recent fields are more probable to encounter, so this way processing time is optimized
		{
			if (!isMain)
			{
				if (path2 != null)
				{
					int c2 = path2.Count;
					for (int i = c2 - 1; i >= 0; i--)
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
                    if (x == field[0] && y == field[1]) // In 0919_1 (step right, the near end of the bottom line extends), even if the near end being stepped on is now inactive, it is not an option (Near end cannot connect to near end.) Active checking is unnecessary.
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

        public bool InCornerRel(int left, int straight)
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
            if (x == size && y == size) return true;
            return false;
        }

        public bool InFutureAbs(int[] f)
        {
            return InFuture(f[0], f[1]);
        }

        public bool InFutureRel(int left, int straight)
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
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
                int x = this.x + left * lx + straight * sx;
                int y = this.y + left * ly + straight * sy;
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
                        foundSectionStart = i;
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
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
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

                        foundSectionEnd = i;
                        return true;
                    }
                }
            }

            return false;
		}

        public int InBorderIndexRel(int left, int straight)
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
            return x + y;
        }


        public int InTakenIndexRel(int left, int straight) // relative position
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
            return InTakenIndex(x, y);
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

        private List<int[]> Copy(List<int[]> obj)
        {
            List<int[]> newObj = new();
            foreach (int[] element in obj)
            {
                newObj.Add(element);
            }
            return newObj;
        }
    }
}
