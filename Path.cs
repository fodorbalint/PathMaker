using Microsoft.Extensions.Logging;
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
using System.Windows.Automation;
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
        public int x, y, x3, y3;
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
        bool CShape = false;
        //bool CShapeLeft = false;
        //bool CShapeRight = false;

        //bool closeStraight = false;
        //bool closeMidAcross = false;
        //bool closeAcross = false;

        // rotation at which if we step straight, an area is created on both sides that we need to enter.
        // 234212, 522267
        int nextStepEnterLeft = -1;
        int nextStepEnterRight = -1;


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
        public List<List<int[]>> examAreaPairFields = new();
        List<int[]> examAreaLine2 = new();
        int examAreaLineType2 = 0;
        bool examAreaLineDirection2 = false;
        List<int[]> examAreaPairField2 = new();

        //used only for displaying area
        public List<List<int[]>> areaLines = new();
        public List<int> areaLineTypes = new();
        public List<bool> areaLineDirections = new();
        public List<List<int[]>> areaPairFields = new();
        public List<bool> areaLineSecondary = new();

        List<object> info;

        int Straight3I = -1; // used for checking Down Stair and Double Area first case rotated at the next step
        int Straight3J = -1;
        bool DirectionalArea, DoubleArea1, DoubleArea2, DoubleArea3, DoubleArea4, DoubleArea1Rotated, Sequence1, Sequence2, Sequence3, DownStairClose, DownStair = false;
        bool DoubleAreaFirstCaseRotatedNext, DownStairNext = false;

        int[] newExitField0 = new int[] { 0, 0 };
        int[] newExitField = new int[] { 0, 0 };
        bool newDirectionRotated = false; // if rotated, it is CW on left side

        // defined in PathRules.cs
        /*List<string> activeRules;
        List<List<int[]>> activeRulesForbiddenFields;
        List<int[]> activeRuleSizes;*/

        // used for double area cases
        int x2 = 0;
        int y2 = 0;
        int sx2 = 0;
        int sy2 = 0;
        int lx2 = 0;
        int ly2 = 0;

        int counterrec = 0;

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
            try
            {
                possible = new List<int[]>();
                forbidden = new List<int[]>();
                List<int[]> newPossible;

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
                                    possible.Add(straightField);
                                }
                                if (!InTakenAbs(rightField) && !InBorderAbs(rightField))
                                {
                                    possible.Add(rightField);
                                }
                                if (!InTakenAbs(leftField) && !InBorderAbs(leftField))
                                {
                                    possible.Add(leftField);
                                }
                            }
                            else
                            {
                                if (!InTakenAbs(straightField) && !InBorderAbs(straightField) && !InFutureAbs(straightField))
                                {
                                    possible.Add(straightField);
                                }
                                if (!InTakenAbs(rightField) && !InBorderAbs(rightField) && !InFutureAbs(rightField))
                                {
                                    possible.Add(rightField);
                                }
                                if (!InTakenAbs(leftField) && !InBorderAbs(leftField) && !InFutureAbs(leftField))
                                {
                                    possible.Add(leftField);
                                }
                            }


                            // A future line may connect to another section as in 0714_2 when we step up, and a 2x2 line is created on the left
                            // For connecting to an older line, see 0730
                            // It cannot connect to the end of the same section
                            if (!isMain)
                            {

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

                            /*if the only possible field is a future field, we don't need to check more. This will prevent unnecessary exits, as in 0804.

                            if (isMain && possible.Count == 1 && InFutureStartAbs(possible[0])) break;*/

                            if (isMain && possible.Count == 1) break;

                            //CShape = false;

                            if (!isMain)
                            {
                                CheckFutureCShape();

                                if (!isNearEnd) // when the far end of the future line extends, it should be checked for border as in 0714. Find out the minimum size for when it is needed.
                                {
                                }
                            }
                            else
                            {
                                // this.isNextStep = isNextStep;

                                // To speed up execution, we check for a C-Shape and close obstacle first. If only one possible field remains, we don't check more. If it is an error, we will see it later.

                                activeRules = new();
                                activeRulesForbiddenFields = new();
                                activeRuleSizes = new();

                                /* ---- uncomment to enable basic rule checking first ---- */

                                /*CShape = false;

                                CheckCShape();

                                if (CShape) break;

                                closeStraightSmall = false;
                                closeMidAcrossSmall = false;
                                closeAcrossSmall = false;
                                closeStraightLarge = false;
                                closeMidAcrossLarge = false;

                                // needed for far left and right case 234320
                                //CheckNearField();

                                if (closeStraightSmall || closeMidAcrossSmall || closeAcrossSmall || closeStraightLarge || closeMidAcrossLarge) break;

                                CheckNearBorder();
                                CheckAreaNearBorder();

                                newPossible = new();
                                foreach (int[] field in possible)
                                {
                                    if (!InForbidden(field))
                                    {
                                        newPossible.Add(field);
                                    }
                                }
                                possible = newPossible;

                                if (possible.Count == 1) break;*/

                                /* ---- uncomment to disable advanced rules ---- */
                                // break;

                                // ----- copy start -----
                                DirectionalArea = DoubleArea1 = DoubleArea2 = DoubleArea3 = DoubleArea4 = DoubleArea1Rotated = Sequence1 = Sequence2 = Sequence3 = DownStairClose = DownStair = false;
                                DoubleAreaFirstCaseRotatedNext = DownStairNext = false;
                                nextStepEnterLeft = -1;
                                nextStepEnterRight = -1;

                                // needs to be checked before AreaUp, it can overwrite it as in 802973
                                T("CheckCShapeNext");
                                CheckCShapeNext();
                                T("CheckStraight " + ShowForbidden());
                                CheckStraight();
                                T("CheckLeftRightAreaUp " + ShowForbidden());
                                CheckLeftRightAreaUp();
                                T("CheckLeftRightAreaUpBig " + ShowForbidden());
                                CheckLeftRightAreaUpBig();  
                                T("CheckLeftRightCorner " + ShowForbidden());
                                CheckLeftRightCorner();
                                T("Forbidden: " + ShowForbidden());

                                T("NextStepEnter " + nextStepEnterLeft + " " + nextStepEnterRight);

                                // 0611_4, 0611_5, 0611_6, 234212, 522267
                                // 0 and 0 or 1 and 3. Beware of 1 and -1.
                                // Overwrite order: 3, 0, 1 (See 802973 and 2020799)
                                // 0618: AreaUp 3 dist, we can only enter now. Can an area be added to the left side? C-Shape is blocked.
                                if (nextStepEnterLeft == 0 && nextStepEnterRight == 0 || nextStepEnterLeft + nextStepEnterRight == 4 && Math.Abs(nextStepEnterLeft - nextStepEnterRight) == 2)
                                {
                                    switch (nextStepEnterLeft)
                                    {
                                        case 0:
                                            T("Next step double area, cannot step straight");
                                            forbidden.Add(new int[] { x + sx, y + sy });
                                            break;
                                        case 1:
                                            T("Next step double area, cannot step right");
                                            forbidden.Add(new int[] { x - lx, y - ly });
                                            break;
                                        case 3:
                                            T("Next step double area, cannot step left");
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                            break;
                                    }
                                }

                                T("CheckLeftRightAreaUpExtended " + ShowForbidden());
                                CheckLeftRightAreaUpExtended(); // #1 close obstacle is at the end of the area, outside.
                                T("CheckAreaUpStartObstacleInside " + ShowForbidden());
                                CheckAreaUpStartObstacleInside(); // #2 close obstacle is at the start of the area, inside.
                                T("CheckStraightSmall " + ShowForbidden());
                                CheckStraightSmall(); // #3 close obstacle is at the start and end of the area, inside. 4 distance only.
                                T("CheckLeftRightAreaUpBigExtended " + ShowForbidden());
                                CheckLeftRightAreaUpBigExtended(); // #4 when entering at the first white field, we have to step down to the first black and then left to enter as in 0624
                                T("CheckStraightBig " + ShowForbidden());
                                CheckStraightBig(); // #7 close obstacle is at the start and end of the area, outside. 4 distance only.
                                T("CheckReverseStair " + ShowForbidden());
                                CheckReverseStair(); // 0718, reverse stair 1/2/ 
                                T("Forbidden: " + ShowForbidden());

                                List<int[]> startForbiddenFields = Copy(forbidden);
                                // If distance is over 3, single area rules seem to disable the needed directions. For 3 distance, we use Sequence first case.

                                /** //T("CheckDirectionalArea");
                                CheckDirectionalArea();
                                T("Check3DoubleArea");
                                Check3DoubleArea(); **/

                                T("CheckSequence " + ShowForbidden());
                                CheckSequence();
                                T("Forbidden: " + ShowForbidden());
                                // ----- copy end -----

                                /*T("DirectionalArea: " + DirectionalArea + "\n" + "DoubleArea1: " + DoubleArea1 + "\n" + "DoubleArea2: " + DoubleArea2 + "\n" + "DoubleArea3: " + DoubleArea3 + "\n" + "DoubleArea4: " + DoubleArea4 + "\n" + "DoubleArea1Rotated: " + DoubleArea1Rotated + "\n" + "Sequence1: " + Sequence1 + "\n" + "Sequence2: " + Sequence2 + "\n" + "Sequence3: " + Sequence3 + "\n" + "DownStairClose: " + DownStairClose + "\n" + "DownStair: " + DownStair + "\n" + "DoubleAreaFirstCaseRotatedNext: " + DoubleAreaFirstCaseRotatedNext + "\n" + "DownStairNext: " + DownStairNext);*/

                                window.ShowActiveRules(activeRules, activeRulesForbiddenFields, startForbiddenFields, activeRuleSizes);

                                // RunRules();

                                /* 5 x 5: CheckNearCorner: 0811_2

                                Used rules for 7 x 7:
                                 * Side back
                                 * Side front
                                 * Side front L
                                 * Future L
                                    821, 0827
                                    the start and end fields have to be in the same section, otherwise they can connect, like in 0913
                                    conditions are true already on 5x5 at 0831_2, but it is handled in CheckNearCorner

                                * Future 2 x 2 Start End
                                * 0909_1, 0909_1_2
                                * On boards larger than 7 x 7, is it possible to apply the rule in straight left/right directions? It means that in the original example, the line is coming downwards instead of heading right.

                                * Future 2 x 3 Start End
                                    0915
                                    Is there a situation where the start and end fields are not part of one future line?
                                    On boards larger than 7 x 7, is it possible to apply the rule in straight left/right directions? It means that in the original example, the line is coming downwards instead of heading right.

                                * Future 3 x 3 Start End
                                   0916

                                * Notes for 9 x 9:

                                * if there is a close across large obstacle leading to a large area, there can be valid rules on the other side, see 2707632
                                * CheckAreaNearBorder() uses countarea, see 0909. A 2x2 area would be created with one way to go in and out
                                * With the exception of closeAcross large area, all near field rules disable two fields, leaving only one option. Running further rules are not necessary. 
                                * Example of interference: 1031_1
                                * CountArea3x3 2,2: 1021_1

                                 Check1x3: 0430_2 */
                            }
                            break;
                        }
                    }
                }

                newPossible = new();

                foreach (int[] field in possible)
                {
                    if (!InForbidden(field))
                    {
                        newPossible.Add(field);
                    }
                }
                possible = newPossible;

                // for debugging
                return;

                if (isNextStep) return;

                // check each possible field if they would result in an impossible situation

                List<int[]> savedPath = Copy(path);
                List<int[]> savedPossible = Copy(possible);

                forbidden = new List<int[]>();

                foreach (int[] field in savedPossible)
                {
                    if (field[0] == size && field[1] == size) return;

                    T("--- check possibility " + field[0] + " " + field[1]);
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
            catch (Exception ex)
            {
                T(ex.Message);
                T(ex.StackTrace);
            }
        }

        // ----- copy start -----
        void CheckCShapeNext() // 0611_5, 0611_6
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (j != 2 && !InTakenRel(1, 1) && (InTakenRel(2, 1) || InBorderRel(2, 1)) && InTakenRel(1, 0))
                    {
                        if (i == 0)
                        {
                            if (nextStepEnterLeft == -1)
                            {
                                nextStepEnterLeft = j;
                            }
                            else if (nextStepEnterLeft == 3 && (j == 0 || j == 1))
                            {
                                nextStepEnterLeft = j;
                            }
                            else if (nextStepEnterLeft == 0 && j == 1)
                            {
                                nextStepEnterLeft = j;
                            }
                        }
                        else
                        {
                            if (nextStepEnterRight == -1)
                            {
                                nextStepEnterRight = j;
                            }
                            else if (nextStepEnterRight == 3 && (j == 0 || j == 1))
                            {
                                nextStepEnterRight = j;
                            }
                            else if (nextStepEnterRight == 0 && j == 1)
                            {
                                nextStepEnterRight = j;
                            }
                        }
                    }

                    // rotate CW
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

        void CheckStraight()
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 3; j++) // j = 1: small area, j = 2: big area
                {
                    bool circleValid = false;
                    int dist = 1;
                    List<int[]> borderFields = new();

                    while (!InTakenRel(0, dist) && !InBorderRel(0, dist))
                    {
                        dist++;
                    }

                    int ex = dist - 1;

                    // if (-1, dist - 1) was border, all the remaining fields will be in the area. 
                    // If that field is taken, a big corner case will be true
                    // But we need circleValid, for 2 distance, in case CheckNearField is not called first.

                    if (dist == 2 || dist > 2 && !InBorderRel(-1, dist - 1))
                    {
                        if (InBorderRel(0, dist))
                        {
                            int i1 = InBorderIndexRel(0, dist);
                            int i2 = InBorderIndexRel(1, dist);

                            if (i1 > i2)
                            {
                                circleValid = true;
                            }
                        }
                        else
                        {
                            int i1 = InTakenIndexRel(0, dist);
                            int i2 = InTakenIndexRel(1, dist);

                            if (i2 != -1)
                            {
                                if (i2 > i1)
                                {
                                    circleValid = true;

                                }
                            }
                            else
                            {
                                i2 = InTakenIndexRel(-1, dist);
                                if (i1 > i2)
                                {
                                    circleValid = true;
                                }
                            }
                        }

                        if (circleValid)
                        {
                            // Not actual with CheckNearField being applied at first.
                            if (ex == 1) // close straight or C-shape up
                            {
                                T("Close straight", i, j);
                                forbidden.Add(new int[] { x - lx, y - ly });
                                // not a C-shape
                                if (!(InTakenRel(1, 1) || InBorderRel(1, 1)))
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                }
                                else
                                {
                                    // C-shape left
                                    if (j == 1)
                                    {
                                        forbidden.Add(new int[] { x - sx, y - sy });
                                    }

                                }

                                // only one option remains
                                sx = thisSx;
                                sy = thisSy;
                                lx = thisLx;
                                ly = thisLy;
                                return;
                            }
                            else
                            {
                                if (ex > 2)
                                {
                                    for (int k = ex - 1; k >= 2; k--)
                                    {
                                        borderFields.Add(new int[] { 0, k });
                                    }
                                }

                                ResetExamAreas();

                                if (CountAreaRel(0, 1, 0, ex, borderFields, circleDirectionLeft, 3, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    int whiteDiff = white - black;
                                    int nowWCount = 0;
                                    int nowWCountLeft = 0;
                                    int nowBCount = 0;
                                    int nowBCountLeft = 0;
                                    int laterWCount = 0;
                                    int laterBCount = 0;

                                    bool ruleTrue = false;

                                    switch (ex % 4)
                                    {
                                        case 0:
                                            nowWCountLeft = nowWCount = ex / 4;
                                            nowBCountLeft = nowBCount = ex / 4 - 1;
                                            laterWCount = ex / 4;
                                            laterBCount = ex / 4;
                                            break;
                                        case 1:
                                            nowWCountLeft = nowWCount = (ex + 3) / 4;
                                            nowBCountLeft = nowBCount = (ex - 5) / 4;
                                            laterWCount = (ex - 1) / 4;
                                            laterBCount = (ex - 5) / 4; // At 5 distance, there are 3 white and 2 black fields on the border. A black to black line is not possible.

                                            if (j == 0 && whiteDiff == nowWCount) // 0715
                                            {
                                                if (CheckNearFieldSmallRel(0, 2, 1, 1, false))
                                                {
                                                    ruleTrue = true;
                                                    T("CheckStraight % 4 = 1 start obstacle: Cannot step straight");
                                                    forbidden.Add(new int[] { x + sx, y + sy });
                                                }
                                            }
                                            break;
                                        case 2:
                                            nowWCount = (ex - 2) / 4; // At 6 distance, if we step straight and exit, the 5 distance situation remain with 3 black and 2 white fields. Another white to white line is not possible. 0610_6
                                            nowWCountLeft = (ex + 2) / 4;
                                            nowBCountLeft = nowBCount = (ex - 2) / 4;
                                            laterWCount = (ex - 2) / 4;
                                            laterBCount = (ex - 2) / 4;
                                            break;
                                        case 3:
                                            nowWCountLeft = nowWCount = (ex + 1) / 4;
                                            nowBCountLeft = (ex - 7) / 4;
                                            nowBCount = (ex - 3) / 4;
                                            laterWCount = (ex + 1) / 4;
                                            laterBCount = (ex - 3) / 4;
                                            break;
                                    }

                                    // T(black, white, nowWCount, nowBCount, nowWCountLeft, nowBCountLeft, laterWCount, laterBCount);

                                    if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount))
                                    {
                                        ruleTrue = true;
                                        T("Straight " + i + " " + j + ": Cannot enter now up");
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                    }
                                    if (!(whiteDiff <= nowWCountLeft && whiteDiff >= -nowBCountLeft) && j != 1)  // for left rotation, lx, ly is the down field
                                    {
                                        ruleTrue = true;
                                        T("Straight " + i + " " + j + ": Cannot enter now left");
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                        if (j == 2)
                                        {
                                            forbidden.Add(new int[] { x - sx, y - sy });
                                        }
                                    }
                                    if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount) && j != 2)
                                    {
                                        ruleTrue = true;
                                        T("Straight " + i + " " + j + ": Cannot enter later");
                                        forbidden.Add(new int[] { x - lx, y - ly });
                                        if (j == 1)
                                        {
                                            forbidden.Add(new int[] { x - sx, y - sy });
                                        }
                                    }

                                    if (ruleTrue)
                                    {
                                        AddExamAreas();
                                    }
                                }
                            }
                        }
                    }

                    if (j == 0) // rotate down (CCW): small area
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW): big area
                    {
                        lx = -lx;
                        ly = -ly;
                        sx = -sx;
                        sy = -sy;
                    }
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

        void CheckLeftRightAreaUp()
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 4; j++) // same rotations as LeftRightCorner
                {
                    if (j != 2)
                    {
                        bool circleValid = false;
                        // As 0618_2 shows, 1,1 can be taken.
                        int dist = 1;
                        List<int[]> borderFields = new();

                        while (!InTakenRel(1, dist) && !InBorderRel(1, dist))
                        {
                            dist++;
                        }

                        int ex = dist - 1;

                        if (ex != 0 && !InTakenRel(0, dist))
                        {
                            int i1 = InTakenIndexRel(1, dist);
                            int i2 = InTakenIndexRel(2, dist);

                            if (i2 > i1)
                            {
                                circleValid = true;
                            }
                        }

                        if (circleValid)
                        {
                            // Not actual with CheckNearField being applied at first.
                            if (ex == 1) // close mid across j = 0 or 2
                            {
                                T("Close mid across", i, j);
                                forbidden.Add(new int[] { x + sx, y + sy });
                                if (j == 0)
                                {
                                    forbidden.Add(new int[] { x - lx, y - ly });
                                }

                                // only one option remains, but we do not return in case of 0623 where the area would close, and at the end, the number of steps are less than size * size.
                                /*sx = thisSx;
                                sy = thisSy;
                                lx = thisLx;
                                ly = thisLy;
                                return;*/
                            }
                            else
                            {
                                if (ex > 2)
                                {
                                    for (int k = ex - 1; k >= 2; k--)
                                    {
                                        borderFields.Add(new int[] { 1, k });
                                    }
                                }

                                ResetExamAreas();

                                if (CountAreaRel(1, 1, 1, ex, borderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    int whiteDiff = white - black;
                                    int nowWCount = 0;
                                    int nowBCount = 0;
                                    int laterWCount = 0;
                                    int laterBCount = 0;

                                    bool ruleTrue = false;

                                    switch (ex % 4)
                                    {
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
                                            nowBCount = (ex - 2) / 4;
                                            laterWCount = (ex - 2) / 4;
                                            laterBCount = (ex - 2) / 4;
                                            break;
                                        case 3:
                                            nowWCount = (ex + 1) / 4;
                                            nowBCount = (ex - 3) / 4;
                                            laterWCount = (ex - 3) / 4;
                                            laterBCount = (ex + 1) / 4;
                                            break;
                                    }

                                    if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount))
                                    {
                                        if (j != 3) // no small small area
                                        {
                                            ruleTrue = true;
                                            T("LeftRightAreaUp: Cannot enter now left ");
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                            if (j == 1)
                                            {
                                                T("LeftRightAreaUp: Cannot enter now down");
                                                forbidden.Add(new int[] { x - sx, y - sy });
                                            }
                                        }
                                    }
                                    if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                    {
                                        ruleTrue = true;
                                        T("LeftRightAreaUp: Cannot enter later");
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        forbidden.Add(new int[] { x - lx, y - ly });
                                    }
                                    else if (j != 2) // We can enter later, check for start C on the opposite side (if the obstacle is up on the left, we check the straight field for next step C, not the right field.) 
                                                     // 466
                                    {
                                        if (ex == 2)
                                        {
                                            if (i == 0)
                                            {
                                                if (nextStepEnterLeft == -1)
                                                {
                                                    nextStepEnterLeft = j;
                                                }
                                                else if (nextStepEnterLeft == 3 && (j == 0 || j == 1))
                                                {
                                                    nextStepEnterLeft = j;
                                                }
                                                else if (nextStepEnterLeft == 0 && j == 1)
                                                {
                                                    nextStepEnterLeft = j;
                                                }
                                            }
                                            else
                                            {
                                                if (nextStepEnterRight == -1)
                                                {
                                                    nextStepEnterRight = j;
                                                }
                                                else if (nextStepEnterRight == 3 && (j == 0 || j == 1))
                                                {
                                                    nextStepEnterRight = j;
                                                }
                                                else if (nextStepEnterRight == 0 && j == 1)
                                                {
                                                    nextStepEnterRight = j;
                                                }
                                            }
                                        }
                                    }

                                    if (ruleTrue)
                                    {
                                        AddExamAreas();
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
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

        void CheckLeftRightAreaUpBig()
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 3; j++) // j = 1: small area, j = 2: big area
                {
                    bool circleValid = false;
                    int dist = 1;
                    List<int[]> borderFields = new();

                    while (!InTakenRel(1, dist) && !InBorderRel(1, dist) && !InTakenRel(0, dist))
                    {
                        dist++;
                    }

                    int ex = dist - 1;
                    // if the obstacle is a border, we will also have the Straight rule.
                    if (ex != 0 && !InBorderRel(1, dist) && !InTakenRel(0, dist))
                    {
                        int i1 = InTakenIndexRel(1, dist);
                        int i2 = InTakenIndexRel(2, dist);

                        if (i1 > i2)
                        {
                            circleValid = true;
                        }
                    }

                    if (circleValid)
                    {
                        // Not actual with CheckNearField being applied at first.
                        if (ex == 1) // close mid across big j = 0 or 1
                        {
                            T("Close mid across big", i, j);
                            forbidden.Add(new int[] { x + sx, y + sy });
                            if (j == 0)
                            {
                                forbidden.Add(new int[] { x + lx, y + ly });
                            }

                            // only one option remains
                            sx = thisSx;
                            sy = thisSy;
                            lx = thisLx;
                            ly = thisLy;
                            return;
                        }
                        else
                        {
                            if (ex > 2)
                            {
                                for (int k = ex - 1; k >= 2; k--)
                                {
                                    borderFields.Add(new int[] { 0, k });
                                }
                            }

                            ResetExamAreas();

                            if (CountAreaRel(0, 1, 0, ex, borderFields, circleDirectionLeft, 3, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

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

                                bool ruleTrue = false;

                                if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount)) // not in range
                                {
                                    ruleTrue = true;
                                    T("LeftRightAreaUpBig: Cannot enter now up");
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                }
                                if (!(whiteDiff <= nowWCountRight && whiteDiff >= -nowBCount)) // not in range
                                {
                                    ruleTrue = true;
                                    T("LeftRightAreaUpBig: Cannot enter now right");
                                    forbidden.Add(new int[] { x - lx, y - ly });
                                }
                                if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                {
                                    ruleTrue = true;
                                    T("LeftRightAreaUpBig: Cannot enter later");
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }

                                if (ruleTrue)
                                {
                                    AddExamAreas();
                                }
                            }
                        }
                    }

                    if (j == 0) // rotate down (CCW): behind obstacle
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW): small area
                    {
                        lx = -lx;
                        ly = -ly;
                        sx = -sx;
                        sy = -sy;
                    }
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

        void CheckLeftRightCorner() // rotations:
                                    // j = 0: small area up left -> CW ->
                                    // j = 1: big area up right -> CW -> 
                                    // j = 2: big area down right -> CW ->
                                    // j = 3: small area down left
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 4; j++)
                {
                    if (!InTakenRel(1, 1) && !InBorderRel(1, 1)) // Can we have an area with a corner if this field is taken? It isn't in the border line.
                    {
                        int horiStart = 2;

                        // checking taken fields from the middle to side is incomplete: 17699719
                        // instead, we check fields in the first row until an obstacle is found, then we walk around the top-left quarter.
                        while (!InTakenRel(horiStart, 1) && !InBorderRel(horiStart, 1))
                        {
                            horiStart++;
                        }

                        // relative directions and coordinates. Since the relative x and y is expanding towards the upper left corner, the current direction is what we use for downwards motion in the absolute coordinate system.
                        int currentDirection = 0;

                        int nextX = horiStart - 1;
                        int nextY = 1;

                        // We will not turn left when we reach a 0 horizontal distance position. Instead, AreaUp will be activated.
                        // When CheckStraight is turned off, we can be at 5,1, going from down, and then nextX will never be 0. In this case, the second condition is needed.

                        int counter = 0;

                        while (!(nextX <= 0 && nextY >= 1) && !InCornerRel(nextX, nextY) && !(counter > 0 && nextX == horiStart - 1 && nextY == 1))
                        // First condition: Includes AreaUp. The closed area might go below and to -1 horizontal position.
                        // Second condition: 0708_1: Finish corner is reached, there cannot be small area from there.
                        // Third condition: 0708_2: We never get to -1 horizontal position, the area is closed. When we get to the first square again, break the cycle.
                        {
                            counter++;
                            if (counter == size * size)
                            {
                                T("Corner discovery error.");

                                window.errorInWalkthrough = true;
                                window.errorString = "Corner discovery error.";
                                window.criticalError = true;
                                return;
                            }

                            // left direction
                            currentDirection = (currentDirection == 3) ? 0 : currentDirection + 1;
                            int l = currentDirection;
                            int possibleNextX = nextX + directions[currentDirection][0];
                            int possibleNextY = nextY + directions[currentDirection][1];

                            // turn right until a field is empty 
                            while (InBorderRel(possibleNextX, possibleNextY) || InTakenRel(possibleNextX, possibleNextY))
                            {
                                l = (l == 0) ? 3 : l - 1;
                                possibleNextX = nextX + directions[l][0];
                                possibleNextY = nextY + directions[l][1];
                            }

                            // if we have turned left from a right direction (to upwards), a corner is found
                            // It has to be left and up. In 0619_2 the walking edge goes below the current position.
                            if (currentDirection == 0 && l == 0 && nextY >= 1)
                            {
                                int hori = nextX + 1;
                                int vert = nextY + 1;

                                //T("Corner found at " + hori + " " + vert + " i " + i + " j " + j);

                                bool circleValid = false;

                                int i1 = InTakenIndexRel(hori, vert);
                                int i2 = InTakenIndexRel(hori + 1, vert);

                                if (i2 > i1)
                                {
                                    circleValid = true;
                                }

                                if (circleValid)
                                {
                                    if (hori == 2 && vert == 2) // close across, small if j = 0, big if j = 1
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        if (j == 0) // close across small
                                        {
                                            T("Close across small", i);
                                            forbidden.Add(new int[] { x - lx, y - ly });

                                            // only one option remains
                                            sx = thisSx;
                                            sy = thisSy;
                                            lx = thisLx;
                                            ly = thisLy;
                                            return;
                                        }
                                        else if (j == 1)
                                        {
                                            T("Close across big", i);
                                        }
                                    }
                                    else
                                    {
                                        bool takenFound = false;
                                        int left1 = 1;
                                        int straight1 = 1;
                                        int left2 = hori - 1;
                                        int straight2 = vert - 1;
                                        List<int[]> borderFields = new();

                                        int nowWCount, nowWCountDown, nowBCount, laterWCount, laterBCount;
                                        int a, n;

                                        //check if all fields on the border line is free
                                        if (vert == hori)
                                        {
                                            a = hori - 1;
                                            nowWCountDown = nowWCount = 0;
                                            nowBCount = a - 1;
                                            laterWCount = -1;// means B = 1
                                            laterBCount = a - 1;

                                            for (int k = 1; k < hori; k++)
                                            {
                                                if (k < hori - 1)
                                                {
                                                    if (InTakenRel(k, k) || InTakenRel(k + 1, k))
                                                    {
                                                        takenFound = true;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    if (InTakenRel(k, k))
                                                    {
                                                        takenFound = true;
                                                        break;
                                                    }
                                                }

                                                if (k == 1)
                                                {
                                                    borderFields.Add(new int[] { 2, 1 });
                                                }
                                                else if (k < hori - 1)
                                                {
                                                    borderFields.Add(new int[] { k, k });
                                                    borderFields.Add(new int[] { k + 1, k });
                                                }
                                            }
                                        }
                                        else if (hori > vert)
                                        {
                                            a = vert - 1;
                                            n = (hori - vert - (hori - vert) % 2) / 2;

                                            if ((hori - vert) % 2 == 0)
                                            {
                                                if (n > 1)
                                                {
                                                    nowWCountDown = nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                                }
                                                else
                                                {
                                                    nowWCount = 0;
                                                    nowWCountDown = 1;
                                                }
                                                nowBCount = a + (n - 1 - (n - 1) % 2) / 2;
                                                laterWCount = (n - n % 2) / 2;
                                                laterBCount = a + (n - n % 2) / 2;
                                            }
                                            else
                                            {
                                                if (n > 0)
                                                {
                                                    nowWCountDown = nowWCount = a + (n - n % 2) / 2;
                                                    laterBCount = (n + 2 - (n + 2) % 2) / 2;
                                                }
                                                else
                                                {
                                                    nowWCount = a - 1;
                                                    nowWCountDown = a;
                                                    laterBCount = 0;
                                                }
                                                nowBCount = (n + 1 - (n + 1) % 2) / 2;
                                                laterWCount = a - 1 + (n + 1 - (n + 1) % 2) / 2;

                                            }

                                            for (int k = 1; k <= hori - vert; k++)
                                            {
                                                if (InTakenRel(k, 1))
                                                {
                                                    takenFound = true;
                                                    break;
                                                }

                                                if (k > 1)
                                                {
                                                    borderFields.Add(new int[] { k, 1 });
                                                }
                                            }

                                            for (int k = 1; k < vert; k++)
                                            {
                                                if (k < vert - 1)
                                                {
                                                    if (InTakenRel(hori - vert + k, k) || InTakenRel(hori - vert + k + 1, k))
                                                    {
                                                        takenFound = true;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    if (InTakenRel(hori - vert + k, k))
                                                    {
                                                        takenFound = true;
                                                        break;
                                                    }
                                                }

                                                if (k < vert - 1)
                                                {
                                                    borderFields.Add(new int[] { hori - vert + k, k });
                                                    borderFields.Add(new int[] { hori - vert + k + 1, k });
                                                }
                                            }
                                        }
                                        else // vert > hori
                                        {
                                            a = hori - 1;
                                            n = (vert - hori - (vert - hori) % 2) / 2;

                                            if ((vert - hori) % 2 == 0)
                                            {
                                                nowWCountDown = nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                                nowBCount = a + (n - 1 - (n - 1) % 2) / 2;
                                                laterWCount = (n - n % 2) / 2;
                                                laterBCount = a + (n - n % 2) / 2;
                                            }
                                            else
                                            {
                                                nowWCountDown = nowWCount = 1 + (n + 1 - (n + 1) % 2) / 2;
                                                nowBCount = a - 1 + (n - n % 2) / 2;
                                                if (n > 0)
                                                {
                                                    laterWCount = 1 + (n - n % 2) / 2;
                                                }
                                                else
                                                {
                                                    laterWCount = 0;
                                                }
                                                laterBCount = a - 1 + (n + 1 - (n + 1) % 2) / 2;
                                            }

                                            for (int k = 1; k < hori; k++)
                                            {
                                                if (k < hori - 1 && hori > 2)
                                                {
                                                    if (InTakenRel(k, k) || InTakenRel(k + 1, k))
                                                    {
                                                        takenFound = true;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    if (InTakenRel(k, k))
                                                    {
                                                        takenFound = true;
                                                        break;
                                                    }
                                                }

                                                if (hori > 2) // there is no stair if corner is at 1 distance, only one field which is the start field.
                                                {
                                                    if (k == 1)
                                                    {
                                                        borderFields.Add(new int[] { 2, 1 });
                                                    }
                                                    else if (k < hori - 1)
                                                    {
                                                        borderFields.Add(new int[] { k, k });
                                                        borderFields.Add(new int[] { k + 1, k });
                                                    }
                                                    else
                                                    {
                                                        borderFields.Add(new int[] { k, k });
                                                    }
                                                }
                                            }

                                            for (int k = 1; k <= vert - hori; k++)
                                            {
                                                if (InTakenRel(hori - 1, hori - 1 + k))
                                                {
                                                    takenFound = true;
                                                    break;
                                                }

                                                if (k < vert - hori)
                                                {
                                                    borderFields.Add(new int[] { hori - 1, hori - 1 + k });
                                                }
                                            }
                                        }

                                        if (!takenFound)
                                        {
                                            // reverse order
                                            List<int[]> newBorderFields = new();
                                            for (int k = borderFields.Count - 1; k >= 0; k--)
                                            {
                                                newBorderFields.Add(borderFields[k]);
                                            }

                                            ResetExamAreas();

                                            // here, true means that count area succeeds, does not run into an error
                                            if (CountAreaRel(left1, straight1, left2, straight2, newBorderFields, circleDirectionLeft, 2, true))
                                            {
                                                int black = (int)info[1];
                                                int white = (int)info[2];

                                                int whiteDiff = white - black;

                                                bool ruleTrue = false;

                                                // need to be generalized for larger than 1 vertical distance
                                                if (hori == 2)
                                                {
                                                    if (vert == 3 && j < 2 && whiteDiff == 0) // 0713
                                                    {
                                                        path.Add(new int[] { x + lx + 2 * sx, y + ly + 2 * sy });
                                                        x2 = x + lx + sx;
                                                        y2 = y + ly + sy;
                                                        path.Add(new int[] { x2, y2 });

                                                        int[] rotatedDir = RotateDir(lx, ly, i);
                                                        lx2 = rotatedDir[0];
                                                        ly2 = rotatedDir[1];
                                                        rotatedDir = RotateDir(sx, sy, i);
                                                        sx2 = rotatedDir[0];
                                                        sy2 = rotatedDir[1];
                                                        lx2 = -lx2;
                                                        ly2 = -ly2;

                                                        if (CheckSequenceRecursive(1 - i)) // recursive main side is the right
                                                        {
                                                            ruleTrue = true;
                                                            T("LeftRightCorner 2, 3 start stair: Cannot step left");
                                                            forbidden.Add(new int[] { x + lx, y + ly });
                                                            if (j == 1) // big area
                                                            {
                                                                T("LeftRightCorner 2, 3 start stair: Cannot step down");
                                                                forbidden.Add(new int[] { x - sx, y - sy });
                                                            }
                                                        }
                                                        path.RemoveAt(path.Count - 1);
                                                        path.RemoveAt(path.Count - 1);
                                                    }

                                                    if (vert % 4 == 3 && j < 2) // 0610, 0610_1, #6 0625_1, 0611_3 (21 cutout)
                                                    {
                                                        if (-whiteDiff == (vert - 3) / 4)
                                                        {
                                                            path.Add(new int[] { x + (hori - 1) * lx + (vert - 1) * sx, y + (hori - 1) * ly + (vert - 1) * sy });
                                                            x2 = x + (hori - 1) * lx + (vert - 1) * sx;
                                                            y2 = y + (hori - 1) * ly + (vert - 1) * sy;

                                                            int[] rotatedDir = RotateDir(lx, ly, i);
                                                            lx2 = rotatedDir[0];
                                                            ly2 = rotatedDir[1];
                                                            rotatedDir = RotateDir(sx, sy, i);
                                                            sx2 = rotatedDir[0];
                                                            sy2 = rotatedDir[1];

                                                            if (CheckCorner2(i, true))
                                                            {
                                                                ruleTrue = true;
                                                                T("LeftRightCorner closed corner 2, 3: Cannot step left");
                                                                forbidden.Add(new int[] { x + lx, y + ly });
                                                                if (j == 1) // big area
                                                                {
                                                                    T("LeftRightCorner closed corner 2, 3: Cannot step down");
                                                                    forbidden.Add(new int[] { x - sx, y - sy });
                                                                }
                                                            }
                                                            path.RemoveAt(path.Count - 1);
                                                        }
                                                    }
                                                    else if (vert % 4 == 0 && j < 2)  // 743059_1, 0610_2, 0610_3, #5 0625
                                                    {
                                                        if (-whiteDiff == vert / 4)
                                                        {
                                                            // Add field so that a second circle can be drawn
                                                            path.Add(new int[] { x + 2 * lx + (vert - 1) * sx, y + 2 * ly + (vert - 1) * sy });
                                                            path.Add(new int[] { x + lx + (vert - 2) * sx, y + ly + (vert - 2) * sy });
                                                            x2 = x + lx + (vert - 2) * sx;
                                                            y2 = y + ly + (vert - 2) * sy;

                                                            int[] rotatedDir = RotateDir(lx, ly, i);
                                                            lx2 = rotatedDir[0];
                                                            ly2 = rotatedDir[1];
                                                            rotatedDir = RotateDir(sx, sy, i);
                                                            sx2 = rotatedDir[0];
                                                            sy2 = rotatedDir[1];

                                                            if (CheckCorner2(i, true))
                                                            {
                                                                ruleTrue = true;
                                                                T("LeftRightCorner open corner 2, 4: Cannot step left");
                                                                forbidden.Add(new int[] { x + lx, y + ly });
                                                                if (j == 1)
                                                                {
                                                                    T("LeftRightCorner open corner 2, 4: Cannot step down");
                                                                    forbidden.Add(new int[] { x - sx, y - sy });
                                                                }
                                                            }

                                                            // 0726, sequence on right side

                                                            ResetExamAreas();

                                                            counterrec = 0;

                                                            lx2 = -lx2;
                                                            ly2 = -ly2;
                                                            if (CheckSequenceRecursive(1 - i))
                                                            {
                                                                AddExamAreas(true);

                                                                T("Corner 1 3 Sequence at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step left");
                                                                forbidden.Add(new int[] { x + lx, y + ly });
                                                                if (j == 1)
                                                                {
                                                                    T("Corner 1 3 Sequence: Cannot step down");
                                                                    forbidden.Add(new int[] { x - sx, y - sy });
                                                                }
                                                            }

                                                            path.RemoveAt(path.Count - 1);
                                                            path.RemoveAt(path.Count - 1);
                                                        }
                                                    }
                                                }
                                                else if (vert == 2)
                                                {
                                                    if (hori % 4 == 0 && j < 2 && -whiteDiff == hori / 4)
                                                    {
                                                        // 0720_3: mid across, 0725_1: across
                                                        // Find example for area
                                                        if (CheckNearFieldSmallRel1(hori - 2, 1, 1, 0, true))
                                                        {
                                                            ruleTrue = true;
                                                            T("LeftRightCorner 4 2 1B: Cannot step left");
                                                            forbidden.Add(new int[] { x + lx, y + ly });
                                                            if (j == 1)
                                                            {
                                                                T("LeftRightCorner 4 2 1B: Cannot step down");
                                                                forbidden.Add(new int[] { x - sx, y - sy });
                                                            }
                                                        }

                                                        // 0711, sequence on left side
                                                        path.Add(new int[] { x + (hori - 1) * lx + sx, y + (hori - 1) * ly + sy });

                                                        x2 = x + (hori - 1) * lx + sx;
                                                        y2 = y + (hori - 1) * ly + sy;

                                                        lx2 = lx;
                                                        ly2 = ly;
                                                        sx2 = sx;
                                                        sy2 = sy;

                                                        ResetExamAreas();

                                                        counterrec = 0;

                                                        if (CheckSequenceRecursive(i))
                                                        {
                                                            AddExamAreas(true);

                                                            T("Corner 3 1 Sequence at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step left");
                                                            forbidden.Add(new int[] { x + lx, y + ly });
                                                            if (j == 1)
                                                            {
                                                                T("Corner 3 1 Sequence: Cannot step down");
                                                                forbidden.Add(new int[] { x - sx, y - sy });
                                                            }
                                                        }

                                                        path.RemoveAt(path.Count - 1);
                                                    }
                                                }

                                                else if (hori == 3 && vert == 4 && -whiteDiff == vert / 4) // 0712
                                                {
                                                    path.Add(new int[] { x + 2 * lx + 3 * sx, y + 2 * ly + 3 * sy });
                                                    path.Add(new int[] { x + lx + 2 * sx, y + ly + 2 * sy });
                                                    x2 = x + lx + 2 * sx;
                                                    y2 = y + ly + 2 * sy;

                                                    int[] rotatedDir = RotateDir(lx, ly, i);
                                                    lx2 = rotatedDir[0];
                                                    ly2 = rotatedDir[1];
                                                    rotatedDir = RotateDir(sx, sy, i);
                                                    sx2 = rotatedDir[0];
                                                    sy2 = rotatedDir[1];

                                                    if (CheckCorner2(i, true))
                                                    {
                                                        ruleTrue = true;
                                                        T("Corner 2 3 Mid Area at " + x + " " + y + ", stop at " + x2 + " " + y2);
                                                        forbidden.Add(new int[] { x + lx, y + ly });
                                                        if (j == 1)
                                                        {
                                                            T("Corner 2 3 Mid Area: Cannot step down");
                                                            forbidden.Add(new int[] { x - sx, y - sy });
                                                        }
                                                    }
                                                    path.RemoveAt(path.Count - 1);
                                                    path.RemoveAt(path.Count - 1);
                                                }

                                                if (vert == hori + 2 && -whiteDiff == hori - 1) // Close mid across: 743059_1, 0610_2, 0610_3; Close across: 0716_1, Area: 0625, 0720_1
                                                    // stair entered from side
                                                    // obstacle at any point of the return step?
                                                {
                                                    path.Add(new int[] { x + hori * lx + (vert - 1) * sx, y + hori * ly + (vert - 1) * sy });

                                                    int m;
                                                    for (m = 1; m <= hori - 1; m++)
                                                    {
                                                        path.Add(new int[] { x + (hori - m) * lx + (vert - 1 - m) * sx, y + (hori - m) * ly + (vert - 1 - m) * sy });
                                                    }
                                                    m--;

                                                    x2 = x + (hori - m) * lx + (vert - 1 - m) * sx;
                                                    y2 = y + (hori - m) * ly + (vert - 1 - m) * sy;

                                                    int[] rotatedDir = RotateDir(lx, ly, i);
                                                    lx2 = rotatedDir[0];
                                                    ly2 = rotatedDir[1];
                                                    rotatedDir = RotateDir(sx, sy, i);
                                                    sx2 = rotatedDir[0];
                                                    sy2 = rotatedDir[1];

                                                    if (CheckCorner2(i, true))
                                                    {
                                                        AddExamAreas();
                                                        T("Corner y = x + 2 return stair close obstacle: Cannot step left");
                                                        forbidden.Add(new int[] { x + lx, y + ly });
                                                        if (j == 1)
                                                        {
                                                            T("Corner y = x + 2 return stair close obstacle: Cannot step down");
                                                            forbidden.Add(new int[] { x - sx, y - sy });
                                                        }
                                                    }

                                                    for (m = 0; m <= hori - 1; m++)
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }
                                                }

                                                if (vert == hori + 3 && -whiteDiff == hori - 1) // 0717, 0717_3 (far obstacle)
                                                    // stair entered from below
                                                {
                                                    path.Add(new int[] { x + (hori - 1) * lx + (vert - 1) * sx, y + (hori - 1) * ly + (vert - 1) * sy });

                                                    int m;
                                                    for (m = 1; m <= hori - 1; m++)
                                                    {
                                                        path.Add(new int[] { x + (hori - 1 - m) * lx + (vert - 1 - m) * sx, y + (hori - 1 - m) * ly + (vert - 1 - m) * sy });
                                                    }
                                                    m--;

                                                    x2 = x + (hori - 1 - m) * lx + (vert - 1 - m) * sx;
                                                    y2 = y + (hori - 1 - m) * ly + (vert - 1 - m) * sy;

                                                    int[] rotatedDir = RotateDir(lx, ly, i);
                                                    lx2 = rotatedDir[0];
                                                    ly2 = rotatedDir[1];
                                                    rotatedDir = RotateDir(sx, sy, i);
                                                    sx2 = rotatedDir[0];
                                                    sy2 = rotatedDir[1];
                                                    
                                                    if (CheckCorner2(i, true))
                                                    {
                                                        ruleTrue = true;
                                                        T("Corner y = x + 3 return stair second obstacle: Cannot step up");
                                                        forbidden.Add(new int[] { x + sx, y + sy });
                                                    }

                                                    for (m = 0; m <= hori - 1; m++)
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }
                                                }

                                                if (hori == vert + 3 && -whiteDiff == 1) // 0725_6, stair up left after first exit
                                                {
                                                    path.Add(new int[] { x + lx, y + ly });

                                                    int m;
                                                    for (m = 1; m <= vert; m++)
                                                    {
                                                        path.Add(new int[] { x + (1 + m) * lx + m * sx, y + (1 + m) * ly + m * sy });
                                                    }
                                                    m--;

                                                    x2 = x + (1 + m) * lx + m * sx;
                                                    y2 = y + (1 + m) * ly + m * sy;
                                                    lx2 = -lx;
                                                    ly2 = -ly;
                                                    sx2 = sx;
                                                    sy2 = sy;

                                                    T("x = y + 3 corner: lx2", lx2, ly2, sx2, sy2, x2, y2);
                                                    if (CheckCorner2(1 - i, true))
                                                    {
                                                        ruleTrue = true;
                                                        T("Corner x = y + 3 up left stair second obstacle: Cannot step left");
                                                        forbidden.Add(new int[] { x + lx, y + ly });
                                                        if (j == 1)
                                                        {
                                                            T("Corner x = y + 3 up left stair second obstacle: Cannot step down");
                                                            forbidden.Add(new int[] { x - sx, y - sy });
                                                        }
                                                    }

                                                    for (m = 0; m <= vert; m++)
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }
                                                }

                                                if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount) && j != 3) // for left rotation, lx, ly is the down field
                                                {
                                                    ruleTrue = true;
                                                    T("LeftRightCorner " + i + " " + j + ": Cannot enter now left");
                                                    forbidden.Add(new int[] { x + lx, y + ly });
                                                }
                                                if (!(whiteDiff <= nowWCountDown && whiteDiff >= -nowBCount) && j != 3)
                                                {
                                                    ruleTrue = true;
                                                    T("LeftRightCorner " + i + " " + j + ": Cannot enter now down");
                                                    forbidden.Add(new int[] { x - sx, y - sy });
                                                }
                                                if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                                {
                                                    ruleTrue = true;
                                                    T("LeftRightCorner " + i + " " + j + ": Cannot enter later");
                                                    forbidden.Add(new int[] { x + sx, y + sy });
                                                    // for small area
                                                    if (j == 0)
                                                    {
                                                        forbidden.Add(new int[] { x - lx, y - ly });
                                                    }
                                                }
                                                else
                                                {
                                                    if (j != 2) // We can enter later, but if we step straight, we have to enter afterwards. Check for pattern on the opposite side (if the obstacle is up on the left, we check the straight field for next step C, not the right field.) 
                                                                // When j = 2, the enter later field is the field behind.

                                                    {
                                                        // 0611_6
                                                        // If we can enter later at the hori 2, vert 3 case, the area must be W = B
                                                        if (
                                                            (hori == 2 && vert == 3) ||
                                                            (hori == 2 && vert == 4 && -whiteDiff == 1) ||
                                                            (hori == 3 && vert == 4 && -whiteDiff == 1)) // 0726_3
                                                        {
                                                            if (i == 0)
                                                            {
                                                                if (nextStepEnterLeft == -1)
                                                                {
                                                                    nextStepEnterLeft = j;
                                                                }
                                                                else if (nextStepEnterLeft == 3 && (j == 0 || j == 1))
                                                                {
                                                                    nextStepEnterLeft = j;
                                                                }
                                                                else if (nextStepEnterLeft == 0 && j == 1)
                                                                {
                                                                    nextStepEnterLeft = j;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (nextStepEnterRight == -1)
                                                                {
                                                                    nextStepEnterRight = j;
                                                                }
                                                                else if (nextStepEnterRight == 3 && (j == 0 || j == 1))
                                                                {
                                                                    nextStepEnterRight = j;
                                                                }
                                                                else if (nextStepEnterRight == 0 && j == 1)
                                                                {
                                                                    nextStepEnterRight = j;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                                if (ruleTrue)
                                                {
                                                    AddExamAreas();
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            currentDirection = l;

                            nextX = possibleNextX;
                            nextY = possibleNextY;
                        }
                    }

                    // rotate CW
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


        void CheckLeftRightAreaUpExtended() // used for double area. As 0618_2 shows, 1,1 can be taken, but we need to check if 0, 1 is free. There can be a previous section of the line going horixontally in front.
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 4; j++) // rotate CW, j = 1: big area, j = 3: small area
                {
                    if (j != 2 && !InTakenRel(0, 1))
                    {
                        bool circleValid = false;

                        int dist = 2;
                        List<int[]> borderFields = new();

                        while (!InTakenRel(1, dist) && !InBorderRel(1, dist))
                        {
                            dist++;
                        }

                        int ex = dist - 1;

                        bool found = false;
                        for (int k = 1; k < dist; k++)
                        {
                            if (InTaken(0, k))
                            {
                                found = true;
                                break;
                            }
                        }

                        // no close mid across checking here, distance needs to be at least 2
                        if (ex > 1 && !found && !InTakenRel(0, dist))
                        {
                            int i1 = InTakenIndexRel(1, dist);
                            int i2 = InTakenIndexRel(2, dist);

                            if (i2 > i1)
                            {
                                circleValid = true;
                            }
                        }

                        if (circleValid)
                        {
                            if (ex > 2)
                            {
                                for (int k = ex - 1; k >= 2; k--)
                                {
                                    borderFields.Add(new int[] { 0, k });
                                }
                            }

                            ResetExamAreas();

                            if (CountAreaRel(0, 1, 0, ex, borderFields, circleDirectionLeft, 3, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                int whiteDiff = white - black;

                                bool ruleTrue = false;

                                switch (ex % 4)
                                {
                                    case 0:
                                        // 0610_4, 0610_5, 121670752, 0627
                                        if (-whiteDiff == ex / 4)
                                        {
                                            // Add field so that a second circle can be drawn
                                            path.Add(new int[] { x + lx + ex * sx, y + ly + ex * sy });
                                            path.Add(new int[] { x + (ex - 1) * sx, y + (ex - 1) * sy });
                                            x2 = x + (ex - 1) * sx;
                                            y2 = y + (ex - 1) * sy;

                                            int[] rotatedDir = RotateDir(lx, ly, i);
                                            lx2 = rotatedDir[0];
                                            ly2 = rotatedDir[1];
                                            rotatedDir = RotateDir(sx, sy, i);
                                            sx2 = rotatedDir[0];
                                            sy2 = rotatedDir[1];

                                            if (CheckCorner2(i, true))
                                            {
                                                ruleTrue = true;
                                                T("LeftRightAreaUpExtended open corner 4: Cannot step straight");
                                                forbidden.Add(new int[] { x + sx, y + sy });
                                            }

                                            path.Add(new int[] { x - lx + (ex - 2) * sx, y - ly + (ex - 2) * sy });

                                            x2 = x - lx + (ex - 2) * sx;
                                            y2 = y - ly + (ex - 2) * sy;

                                            // 0725_5: mid across (up), mid across (down), 0726_1: mid across, across, 0726_2: area, mid across
                                            if (CheckCorner2(i, true) && CheckNearFieldSmallRel1(-1, ex - 2, 1, 1, true))
                                            {
                                                ruleTrue = true;
                                                T("LeftRightAreaUpExtended open corner 4 exit: Cannot step straight");
                                                forbidden.Add(new int[] { x + sx, y + sy });
                                            }

                                            path.RemoveAt(path.Count - 1);
                                            path.RemoveAt(path.Count - 1);
                                            path.RemoveAt(path.Count - 1);
                                        }
                                        // 0618_2: end obstacle, 0725: double obstacle outside 
                                        else if (whiteDiff == ex / 4)
                                        {
                                            if (CheckNearFieldSmallRel1(0, ex, 0, 2, true))
                                            {
                                                ruleTrue = true;
                                                T("LeftRightAreaUpExtended closed corner 4: Cannot step right");
                                                forbidden.Add(new int[] { x - lx, y - ly });

                                                // 0725: double obstacle outside 
                                                if (CheckNearFieldSmallRel0(0, 2, 1, 1, false))
                                                {
                                                    T("LeftRightAreaUpExtended 4 dist double obstacle outside: Cannot step straight");
                                                    forbidden.Add(new int[] { x + sx, y + sy });
                                                }
                                            }
                                        } 
                                        break;
                                    case 1:
                                        // 0626
                                        if (whiteDiff == (ex + 3) / 4 && CheckNearFieldSmallRel(0, ex - 1, 0, 2, true))
                                        {
                                            ruleTrue = true;
                                            T("LeftRightAreaUpExtended open corner 5: Cannot step right");
                                            forbidden.Add(new int[] { x - lx, y - ly });
                                        }
                                        break;
                                    case 2:
                                        // We cannot get to the 2- or 6-distance case if the other rules are applied. 0611_1
                                        /*if (!found && whiteDiff == (ex + 2) / 4 && CheckNearFieldSmallRel(0, ex, 0, 2, true))
                                        {
                                            ruleTrue = true;
                                            T("LeftRightAreaUp closed corner 2: Cannot step left");
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                        }*/
                                        break;
                                    case 3:
                                        if (whiteDiff == (ex + 1) / 4 + 1 && CheckNearFieldSmallRel(0, ex - 1, 0, 2, true))
                                        {
                                            ruleTrue = true;
                                            T("LeftRightAreaUpExtended open corner 3: Cannot step left");
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                        }
                                        // 0611
                                        if (whiteDiff == (ex + 1) / 4 + 1 && CheckNearFieldSmallRel(0, ex - 1, 0, 2, true))
                                        {
                                            ruleTrue = true;
                                            T("LeftRightAreaUpExtended open corner 3: Cannot step left");
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                        }
                                        // 0611, 0710
                                        if (-whiteDiff == (ex + 1) / 4 - 1)
                                        {
                                            path.Add(new int[] { x + ex * sx, y + ex * sy });

                                            x2 = x + ex * sx;
                                            y2 = y + ex * sy;

                                            int[] rotatedDir = RotateDir(lx, ly, i);
                                            lx2 = rotatedDir[0];
                                            ly2 = rotatedDir[1];
                                            rotatedDir = RotateDir(sx, sy, i);
                                            sx2 = rotatedDir[0];
                                            sy2 = rotatedDir[1];

                                            if (CheckCorner2(i, true))
                                            {
                                                ruleTrue = true;
                                                T("LeftRightAreaUpExtended closed corner 3: Cannot step straight");
                                                forbidden.Add(new int[] { x + sx, y + sy });
                                            }

                                            path.Add(new int[] { x - lx + (ex - 1) * sx, y - ly + (ex - 1) * sy });

                                            x2 = x - lx + (ex - 1) * sx;
                                            y2 = y - ly + (ex - 1) * sy;

                                            // 0724, two close obstacles at the exit field
                                            // 0725_2, on left side it is an area
                                            // Find example for across as a down obstacle
                                            if (CheckCorner2(i, true) && CheckNearFieldSmallRel0(-1, ex - 1, 1, 1, true))
                                            {
                                                ruleTrue = true;
                                                T("LeftRightAreaUpExtended closed corner 3 exit: Cannot step straight");
                                                forbidden.Add(new int[] { x + sx, y + sy });
                                            }

                                            path.RemoveAt(path.Count - 1);
                                            path.RemoveAt(path.Count - 1);

                                        }
                                        break;
                                }

                                if (ruleTrue)
                                {
                                    AddExamAreas();
                                }
                            }
                        }
                    }

                    // rotate CW
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

        void CheckAreaUpStartObstacleInside() // 0618, 0619: When we enter the area, we need to step up. There is a close obstacle the other way inside the area.
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 3; j++) // small area, big area, behind right
                {
                    bool circleValid = false;
                    int dist = 1;
                    List<int[]> borderFields = new();

                    while (!InTakenRel(1, dist) && !InBorderRel(1, dist))
                    {
                        dist++;
                    }

                    int ex = dist - 1;

                    if (ex >= 3 && ex % 2 == 1)
                    {
                        if (InBorderRel(1, dist))
                        {
                            int i1 = InBorderIndexRel(1, dist);
                            int i2 = InBorderIndexRel(2, dist);

                            if (i1 > i2)
                            {
                                circleValid = true;
                            }
                        }
                        else
                        {
                            int i1 = InTakenIndexRel(1, dist);
                            int i2 = InTakenIndexRel(2, dist);

                            if (i2 > i1)
                            {
                                circleValid = true;
                            }
                        }
                    }

                    if (circleValid)
                    {
                        for (int k = ex - 1; k >= 2; k--)
                        {
                            borderFields.Add(new int[] { 1, k });
                        }

                        ResetExamAreas();

                        if (CountAreaRel(1, 1, 1, ex, borderFields, circleDirectionLeft, 2, true))
                        {
                            int black = (int)info[1];
                            int white = (int)info[2];

                            int whiteDiff = white - black;

                            bool ruleTrue = false;

                            switch (ex % 4)
                            {
                                case 1:
                                    if (j <= 1 && whiteDiff == (ex - 1) / 4 && CheckNearFieldSmallRel1(1, 2, 0, 1, true)) // Mid across: 0618, Across: 0717_1
                                    {
                                        ruleTrue = true;
                                        T("AreaUpStartObstacleInside % 4 = 1: Cannot step straight and right");
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        if (j != 2) // the right field relative to the area (left of the main line) is now inside the area.
                                        {
                                            forbidden.Add(new int[] { x - lx, y - ly });
                                        }
                                    }
                                    break;
                                case 3:
                                    if (j >= 1 && whiteDiff == (ex + 1) / 4 && CheckNearFieldSmallRel1(1, 0, 0, 1, true)) // Mid across: 0619, Across: 0717_2, area up 3 start obstacle
                                    {
                                        ruleTrue = true;
                                        T("AreaUpStartObstacleInside % 4 = 3: Cannot step left");
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                    break;
                            }

                            if (ruleTrue)
                            {
                                AddExamAreas();
                            }
                        }

                    }

                    // rotate CW
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
        
        void CheckStraightSmall() // 0619_1, 0714, 0716, 0717_4
        // Two columns are checked for being empty, but at the end the straight field must be taken, and the left field must be empty.
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 3; j++) // j = 0: straight area, j = 1: right (big) area, j = 2: behind on right side (0716)
                {
                    bool circleValid = false;
                    int dist = 1;
                    List<int[]> borderFields = new();

                    // stop when both 0 and 1 horizontal distance is empty or when border is encountered
                    while(InTakenRel(0, dist) && !InTakenRel(1, dist))
                    {
                        dist++;
                    }

                    if (!InTakenRel(0, dist) && !InTakenRel(1, dist))
                    {
                        // if border hasn't been encountered, continue and stop when 0 is taken, 1 is empty
                        while (!InTakenRel(0, dist) && !InTakenRel(1, dist) && !InBorderRel(0, dist))
                        {
                            dist++;
                        }

                        int ex = dist - 1;

                        if (ex >= 2 && ex <= 5 && InTakenRel(0, dist) && !InTakenRel(1, dist))
                        {
                            int i1 = InTakenIndexRel(0, dist);
                            int i2 = InTakenIndexRel(-1, dist);

                            if (i1 > i2)
                            {
                                circleValid = true;
                            }
                        }

                        if (circleValid)
                        {
                            for (int k = ex - 1; k >= 2; k--)
                            {
                                borderFields.Add(new int[] { 1, k });
                            }

                            ResetExamAreas();

                            if (CountAreaRel(1, 1, 1, ex, borderFields, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                if (j >= 1 && ex == 2 && white == black + 1) // 0717_4
                                {
                                    if (CheckNearFieldSmallRel(1, 0, 0, 1, true) && CheckNearFieldSmallRel(1, 2, 1, 2, false))
                                    {
                                        T("CheckStraightSmall 2 double close obstacle inside: Cannot step left");
                                        forbidden.Add(new int[] { x + lx, y + ly });

                                        AddExamAreas();
                                    }
                                }

                                if (j >= 1 && ex == 3 && white == black + 1) // 0716
                                {
                                    if (CheckNearFieldSmallRel(1, 0, 0, 1, true) && CheckNearFieldSmallRel(1, 2, 1, 2, false))
                                    {
                                        T("CheckStraightSmall 3 double close obstacle inside: Cannot step left");
                                        forbidden.Add(new int[] { x + lx, y + ly });

                                        AddExamAreas();
                                    }
                                }

                                if (j <= 1 && ex == 4 && white == black + 1) // 0619_1
                                {
                                    if (CheckNearFieldSmallRel(1, 2, 0, 1, true) && CheckNearFieldSmallRel(1, 4, 1, 2, false))
                                    {
                                        T("CheckStraightSmall 4 double close obstacle inside: Cannot step straight and right");
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        forbidden.Add(new int[] { x - lx, y - ly });

                                        AddExamAreas();
                                    }
                                }

                                if (j <= 1 && ex == 5 && white == black + 1) // 0714
                                {
                                    if (CheckNearFieldSmallRel(1, 2, 0, 1, true) && CheckNearFieldSmallRel(1, 4, 1, 2, false))
                                    {
                                        T("CheckStraightSmall 5 double close obstacle inside: Cannot step straight and right");
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        forbidden.Add(new int[] { x - lx, y - ly });

                                        AddExamAreas();
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
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
        
        void CheckLeftRightAreaUpBigExtended() // Area as in the first area case of documentation. That area is taken care of in UpBig and Striaght. This is about a border movement close obstacle: 0624
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 3; j++) // j = 1: small area, j = 2: big area
                {
                    bool circleValid = false;
                    int dist = 1;
                    List<int[]> borderFields = new();

                    while (!InTakenRel(1, dist) && !InBorderRel(1, dist) && !InTakenRel(0, dist))
                    {
                        dist++;
                    }

                    int ex = dist - 1;

                    if (ex > 1 && InTakenRel(1, dist))
                    {
                        int i1 = InTakenIndexRel(1, dist);

                        if (InTakenRel(2, dist))
                        {
                            int i2 = InTakenIndexRel(2, dist);
                            if (i1 > i2)
                            {
                                circleValid = true;
                            }
                        }
                        else
                        {
                            int i2 = InTakenIndexRel(0, dist);
                            if (i2 > i1)
                            {
                                circleValid = true;
                            }
                        }
                    }

                    if (circleValid)
                    {
                        if (ex > 2)
                        {
                            for (int k = ex - 1; k >= 2; k--)
                            {
                                borderFields.Add(new int[] { 1, k });
                            }
                        }

                        ResetExamAreas();

                        if (CountAreaRel(1, 1, 1, ex, borderFields, circleDirectionLeft, 2, true))
                        {
                            int black = (int)info[1];
                            int white = (int)info[2];

                            int whiteDiff = white - black;
                            int nowWCount = 0;
                            int nowWCountRight = 0;
                            int nowBCount = 0;
                            int laterWCount = 0;
                            int laterBCount = 0;

                            bool ruleTrue = false;

                            switch (ex % 4)
                            {
                                case 0:
                                    nowWCountRight = nowWCount = ex / 4;
                                    nowBCount = ex / 4 - 1;
                                    laterWCount = ex / 4;
                                    laterBCount = ex / 4;

                                    if (whiteDiff == laterWCount && CheckNearFieldSmallRel(1, 1, 0, 1, false)) // when entering at the first white field, we have to step down to the first black and then left to enter as in 0624
                                    {
                                        ruleTrue = true;
                                        T("CheckLeftRightAreaUpBigExtended enter obstacle: Cannot step left");
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                        if (j == 2)
                                        {
                                            forbidden.Add(new int[] { x - sx, y - sy });
                                        }
                                    }

                                    break;
                                case 1:
                                    nowWCountRight = nowWCount = (ex - 1) / 4;
                                    nowBCount = (ex - 1) / 4;
                                    laterWCount = (ex - 1) / 4;
                                    laterBCount = (ex + 3) / 4;
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
                                        laterBCount = (ex + 2) / 4;
                                    }
                                    break;
                                case 3:
                                    nowWCountRight = nowWCount = (ex - 3) / 4;
                                    nowBCount = (ex + 1) / 4;
                                    laterWCount = (ex - 3) / 4;
                                    laterBCount = (ex + 1) / 4;
                                    break;
                            }

                            if (ruleTrue)
                            {
                                AddExamAreas();
                            }
                        }
                    }

                    if (j == 0) // rotate down (CCW): behind obstacle
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW): small area
                    {
                        lx = -lx;
                        ly = -ly;
                        sx = -sx;
                        sy = -sy;
                    }
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
        
        void CheckStraightBig() // 18677343, 59434452, 0626_1
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {
                    bool circleValid = false;
                    int dist = 1;
                    List<int[]> borderFields = new();

                    while (!InTakenRel(0, dist) && !InBorderRel(0, dist) && !InTakenRel(-1, dist) && !InBorderRel(-1, dist))
                    {
                        dist++;
                    }

                    int ex = dist - 1;

                    if (InTakenRel(0, dist) && !InTakenRel(-1, dist))
                    {
                        int i1 = InTakenIndexRel(0, dist);
                        int i2 = InTakenIndexRel(1, dist);

                        if (i2 > i1)
                        {
                            circleValid = true;
                        }

                        if (circleValid)
                        {
                            for (int k = ex - 1; k >= 2; k--)
                            {
                                borderFields.Add(new int[] { -1, k });
                            }

                            ResetExamAreas();

                            // 18677343, 59434452
                            if (ex == 3)
                            {
                                if (CountAreaRel(-1, 1, -1, ex, borderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    if (white == black)
                                    {
                                        if (CheckNearFieldSmallRel(-1, 3, 0, 2, true) && CheckNearFieldSmallRel1(-1, 1, 1, 1, false))
                                        {
                                            T("CheckStraightBig double close obstacle outside 3 dist: Cannot step right and down");
                                            forbidden.Add(new int[] { x - sx, y - sy });
                                            forbidden.Add(new int[] { x - lx, y - ly });

                                            AddExamAreas();
                                        }
                                    }
                                }
                            }
                            // 0626_1
                            else if (ex == 4)
                            {
                                if (CountAreaRel(-1, 1, -1, ex, borderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    if (white == black + 1)
                                    {
                                        if (CheckNearFieldSmallRel(-1, 3, 0, 2, true) && CheckNearFieldSmallRel1(-1, 1, 1, 1, false))
                                        {
                                            T("CheckStraightBig double close obstacle outside 4 dist: Cannot step right and down");
                                            forbidden.Add(new int[] { x - sx, y - sy });
                                            forbidden.Add(new int[] { x - lx, y - ly });

                                            AddExamAreas();
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CCW
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
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

        void CheckReverseStair() // 0718, reverse stair 1/2, 0720_2
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: big area
                {
                    int dist = 1;                    

                    while (!InTakenRel(dist, 0) && !InBorderRel(dist, 0))
                    {
                        dist++;
                    }
                    dist--;

                    if (dist >= 3 && !InBorderRel(dist, 0))
                    {
                        int nextX = dist;
                        int nextY = 0;

                        int currentDirection = 0;
                        int counter = 0;

                        while (!(nextY >= nextX && nextY > 0 && nextX > 0)  && !InCornerRel(nextX, nextY))
                        {
                            // Only stop when arealine reaches the 45 degree slope in the first quarter.
                            counter++;
                            if (counter == size * size)
                            {
                                T("ReverseStair corner discovery error.");

                                window.errorInWalkthrough = true;
                                window.errorString = "ReverseStair corner discovery error.";
                                window.criticalError = true;
                                return;
                            }

                            // left direction
                            currentDirection = (currentDirection == 3) ? 0 : currentDirection + 1;
                            int l = currentDirection;
                            int possibleNextX = nextX + directions[currentDirection][0];
                            int possibleNextY = nextY + directions[currentDirection][1];

                            // turn right until a field is empty 
                            while (InBorderRel(possibleNextX, possibleNextY) || InTakenRel(possibleNextX, possibleNextY))
                            {
                                l = (l == 0) ? 3 : l - 1;
                                possibleNextX = nextX + directions[l][0];
                                possibleNextY = nextY + directions[l][1];
                            }

                            if (currentDirection == 1 && l == 1 && nextX >= 3 && nextX == nextY + 2)
                            {
                                T("corner found", nextX, nextY);

                                bool circleValid = false;

                                int i1 = InTakenIndexRel(nextX + 1, nextY - 1);
                                int i2 = InTakenIndexRel(nextX + 2, nextY - 1);

                                if (i2 < i1)
                                {
                                    circleValid = true;
                                }

                                if (circleValid)
                                {
                                    T("circle valid", i, j);
                                    bool takenFound = false;

                                    List<int[]> borderFields = new();
                                    for (int k = 1; k <= nextY; k++)
                                    {
                                        if (InTakenRel(k, k) || InTakenRel(k + 1, k))
                                        {
                                            takenFound = true;
                                            break;
                                        }

                                        if (k == 1)
                                        {
                                            borderFields.Add(new int[] { 2, 1 });
                                        }
                                        else
                                        {
                                            borderFields.Add(new int[] { k, k });
                                            borderFields.Add(new int[] { k + 1, k });
                                        }
                                    }

                                    if (!takenFound)
                                    {
                                        // reverse order
                                        List<int[]> newBorderFields = new();
                                        for (int k = borderFields.Count - 1; k >= 0; k--)
                                        {
                                            newBorderFields.Add(borderFields[k]);
                                        }

                                        ResetExamAreas();

                                        if (CountAreaRel(1, 1, nextX, nextY, newBorderFields, circleDirectionLeft, 2, true))
                                        {
                                            int black = (int)info[1];
                                            int white = (int)info[2];

                                            // 0718: Mid across, across
                                            // 0720_2: Mid across, mid across
                                            // 0727: C-shape, mid across
                                            if (black == white && CheckNearFieldSmallRel(nextX, nextY, 0, 0, true) && CheckNearFieldSmallRel1(nextX - 2, nextY, 1, 0, false))
                                            {
                                                AddExamAreas();
                                                T("Reverse stair at " + nextX + " " + nextY + ": Cannot step straight");
                                                forbidden.Add(new int[] { x + sx, y + sy });

                                                if (j == 0)
                                                {
                                                    T("Reverse stair at " + nextX + " " + nextY + ": Cannot step right");
                                                    forbidden.Add(new int[] { x - lx, y - ly });
                                                }


                                            }
                                        }
                                    }
                                }
                            }

                            currentDirection = l;

                            nextX = possibleNextX;
                            nextY = possibleNextY;
                        }
                    }

                    // rotate CW
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

        void CheckSequence()
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++)
                {
                    // First case

                    // Triple Area
                    // 2024_0516_2
                    // Rotated: 2024_0516_3

                    // See also 665575 for alternative start obstacle placement
                    bool circleValid = false;

                    if (((InBorderRelExact(0, 4) && !InCornerRel(0, 3)) || InTakenRel(0, 4) || InTakenRel(-1, 4)) && !InTakenRel(0, 1) && !InTakenRel(0, 2) && !InTakenRel(0, 3) && !InTakenRel(1, 1) && !InTakenRel(1, 3) && !InTakenRel(-1, 3))
                    {
                        if (InBorderRelExact(0, 4))
                        {
                            int directionFieldIndex = InBorderIndexRel(0, 4);
                            int sideIndex = InBorderIndexRel(1, 4);

                            if (sideIndex < directionFieldIndex)
                            {
                                circleValid = true;
                            }
                        }
                        else if (InTakenRel(0, 4))
                        {
                            int directionFieldIndex = InTakenIndexRel(0, 4);
                            int sideIndex = InTakenIndexRel(1, 4);

                            if (sideIndex != -1)
                            {
                                if (sideIndex > directionFieldIndex)
                                {
                                    circleValid = true;
                                }

                            }
                            else
                            {
                                sideIndex = InTakenIndexRel(-1, 4);
                                if (directionFieldIndex > sideIndex)
                                {
                                    circleValid = true;
                                }
                            }
                        }
                        else
                        {
                            int directionFieldIndex = InTakenIndexRel(-1, 4);
                            int sideIndex = InTakenIndexRel(-2, 4);
                            if (directionFieldIndex > sideIndex)
                            {
                                circleValid = true;
                            }
                        }

                        if (circleValid)
                        {
                            if (CountAreaRel(0, 1, 0, 3, new List<int[]> { new int[] { 0, 2 } }, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                if (black == white)
                                {
                                    path.Add(new int[] { x + sx, y + sy }); // right side area checking needs it
                                    path.Add(new int[] { x + 3 * sx, y + 3 * sy }); // left side area checking needs it
                                    path.Add(new int[] { x - lx + 2 * sx, y - ly + 2 * sy });

                                    // step after exiting area:
                                    x2 = x - lx + 2 * sx;
                                    y2 = y - ly + 2 * sy;

                                    int[] rotatedDir = RotateDir(lx, ly, i);
                                    lx2 = rotatedDir[0];
                                    ly2 = rotatedDir[1];
                                    rotatedDir = RotateDir(sx, sy, i);
                                    sx2 = rotatedDir[0];
                                    sy2 = rotatedDir[1];

                                    ResetExamAreas();

                                    counterrec = 0;

                                    if (CheckSequenceRecursive(i))
                                    {
                                        AddExamAreas(true);

                                        Sequence1 = true;
                                        activeRules.Add("Sequence first case");
                                        activeRuleSizes.Add(new int[] { 5, 5 });
                                        activeRulesForbiddenFields.Add(new List<int[]> { new int[] { x + lx, y + ly }, new int[] { x + sx, y + sy } });

                                        T("CheckSequence case 1 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
                                        forbidden.Add(new int[] { x + sx, y + sy });

                                        if (j == 0)
                                        {
                                            // Due to CheckStraight, stepping left is already disabled when the obstacle is straight ahead. When it is one to the right, we need the left field to be disabled.
                                            T("CheckSequence case 1 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step left");
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                        }

                                        forbidden.Add(new int[] { x + lx, y + ly });
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                    }

                                    path.RemoveAt(path.Count - 1);
                                    path.RemoveAt(path.Count - 1);
                                    path.RemoveAt(path.Count - 1);
                                }
                            }
                        }
                    }

                    // Second case

                    // Square 4 x 2 C-Shape / Square 4 x 2 Area
                    // 2024_0516
                    // Rotated: 2024_0516_1

                    // Double Area Stair
                    // 2024_0516_4
                    // Rotated: 2024_0516_5
                    if (InTakenRel(0, 3) && !InTakenRel(0, 1) && !InTakenRel(0, 2) && !InTakenRel(-1, 3) && !InTakenRel(-2, 2)) // Field in front of exit should also be empty
                    {
                        int directionFieldIndex = InTakenIndexRel(0, 3);
                        int leftIndex = InTakenIndexRel(1, 3);

                        if (leftIndex > directionFieldIndex)
                        {
                            if (CountAreaRel(0, 1, 0, 2, null, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                if (black == white)
                                {
                                    path.Add(new int[] { x + sx, y + sy }); // right side area checking needs it
                                    path.Add(new int[] { x - lx + 2 * sx, y - ly + 2 * sy });

                                    x2 = x - lx + 2 * sx;
                                    y2 = y - ly + 2 * sy;

                                    int[] rotatedDir = RotateDir(lx, ly, i);
                                    lx2 = rotatedDir[0];
                                    ly2 = rotatedDir[1];
                                    rotatedDir = RotateDir(sx, sy, i);
                                    sx2 = rotatedDir[0];
                                    sy2 = rotatedDir[1];

                                    ResetExamAreas();

                                    counterrec = 0;

                                    if (CheckSequenceRecursive(i))
                                    {
                                        AddExamAreas(true);

                                        Sequence2 = true;
                                        activeRules.Add("Sequence second case");
                                        activeRuleSizes.Add(new int[] { 5, 5 });
                                        activeRulesForbiddenFields.Add(new List<int[]> { new int[] { x + lx, y + ly }, new int[] { x + sx, y + sy } });

                                        T("CheckSequence case 2 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
                                        forbidden.Add(new int[] { x + sx, y + sy });

                                        if (j == 0)
                                        {
                                            T("CheckSequence case 2 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step left");
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                        }
                                    }
                                    path.RemoveAt(path.Count - 1);
                                    path.RemoveAt(path.Count - 1);
                                }
                            }
                        }
                    }

                    int l0 = lx; // rotate down (CCW)
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
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

            // Third case, Double Area Stair 2
            // 2024_0516_6
            // Rotated both ways: 2024_0516_7, 2024_0516_8

            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 3; j++)
                {
                    if (InTakenRel(1, 3) && !InTakenRel(1, 2) && !InTakenRel(0, 3) && !InTakenRel(0, 1) && !InTakenRel(-1, 2))
                    {
                        int directionFieldIndex = InTakenIndexRel(1, 3);
                        int leftIndex = InTakenIndexRel(2, 3);

                        if (leftIndex > directionFieldIndex)
                        {
                            if (CountAreaRel(1, 1, 1, 2, null, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                if (black == white)
                                {
                                    path.Add(new int[] { x + sx, y + sy }); // right side area checking needs it
                                    path.Add(new int[] { x + 2 * sx, y + 2 * sy });

                                    x2 = x + 2 * sx;
                                    y2 = y + 2 * sy;

                                    int[] rotatedDir = RotateDir(lx, ly, i);
                                    lx2 = rotatedDir[0];
                                    ly2 = rotatedDir[1];
                                    rotatedDir = RotateDir(sx, sy, i);
                                    sx2 = rotatedDir[0];
                                    sy2 = rotatedDir[1];

                                    ResetExamAreas();

                                    counterrec = 0;

                                    if (CheckSequenceRecursive(i))
                                    {
                                        AddExamAreas(true);

                                        Sequence3 = true;
                                        activeRules.Add("Sequence third case");
                                        activeRuleSizes.Add(new int[] { 4, 5 });
                                        activeRulesForbiddenFields.Add(new List<int[]> { new int[] { x + sx, y + sy } });

                                        T("CheckSequence case 3 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                    }
                                    path.RemoveAt(path.Count - 1);
                                    path.RemoveAt(path.Count - 1);
                                }
                            }
                        }
                    }

                    if (j == 0) // rotate down (CCW)
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW)
                    {
                        lx = -lx;
                        ly = -ly;
                        sx = -sx;
                        sy = -sy;
                    }
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

            // Fourth case, next step C-shape
            // 2024_0630, 2024_0720, 2024_0723
            // Sequence has to begin already at the next step, not at the exit point of the first C-shape: 2024_0725_3
            // Rotated CCW

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (InTakenRel(2, 1) && InTakenRel(1, 0) && !InTakenRel(1, 1))
                    {
                        path.Add(new int[] { x + sx, y + sy });

                        x2 = x + sx;
                        y2 = y + sy;
                        lx2 = lx;
                        ly2 = ly;
                        sx2 = sx;
                        sy2 = sy;

                        ResetExamAreas();

                        counterrec = 0;

                        if (CheckSequenceRecursive(i))
                        {
                            AddExamAreas(true);

                            T("CheckSequence case 4 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
                            forbidden.Add(new int[] { x + sx, y + sy });
                        }
                        path.RemoveAt(path.Count - 1);
                    }

                    // rotate down (CCW)
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
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

            // fifth case, 0724_1: Step right next step C-shape. There is an obstacle 2 distance to the right to start with.

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (InTakenRel(1, 1) && !InTakenRel(0, 1) && InTakenRel(-3, 0) && !InTakenRel(-2, 0) && !InTakenRel(-1, 0) && !InTakenRel(-3, 1))
                    {
                        int directionFieldIndex = InTakenIndexRel(-3, 0);
                        int sideIndex = InTakenIndexRel(-3, -1);

                        if (directionFieldIndex > sideIndex)
                        {
                            path.Add(new int[] { x - lx, y - ly });
                            path.Add(new int[] { x - lx + sx, y - ly + sy });
                            path.Add(new int[] { x + sx, y + sy });
                            path.Add(new int[] { x + 2 * sx, y + 2 * sy });

                            x2 = x + 2 * sx;
                            y2 = y + 2 * sy;
                            lx2 = lx;
                            ly2 = ly;
                            sx2 = sx;
                            sy2 = sy;

                            ResetExamAreas();

                            counterrec = 0;

                            if (CheckSequenceRecursive(i))
                            {
                                AddExamAreas(true);

                                T("CheckSequence case 5 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step right");
                                forbidden.Add(new int[] { x - lx, y - ly });

                                if (j == 1)
                                {
                                    T("CheckSequence case 5 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step down");
                                    forbidden.Add(new int[] { x - sx, y - sy });
                                }
                            }
                            path.RemoveAt(path.Count - 1);
                            path.RemoveAt(path.Count - 1);
                            path.RemoveAt(path.Count - 1);
                            path.RemoveAt(path.Count - 1);
                        }
                    }

                    // rotate down (CCW)
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
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

        bool CheckCorner2(int side, bool smallArea) // #8
        {
            bool circleDirectionLeft = (side == 0) ? true : false;

            // 1, 1 relative field cannot be taken
            int horiStart = 1;
            while (!InTakenRel2(horiStart, 1) && !InBorderRel2(horiStart, 1))
            {
                horiStart++;
            }

            if (horiStart >= 2) // at least left field has to be empty
            {
                int currentDirection = 0;

                int nextX = horiStart - 1;
                int nextY = 1;

                int counter = 0;
                //T("nextX", nextX, nextY, circleDirectionLeft, x2, y2, lx2, ly2);
                while (!(nextX < 0 && nextY >= 1) && !InCornerRel2(nextX, nextY) && !(counter > 0 && nextX == horiStart - 1 && nextY == 1))
                { // First condition: Includes AreaUp. The closed area might go below and to -1 horizontal position.
                  // Second condition: 0708_1: Finish corner is reached, there cannot be small area from there.
                  // Third condition: 0708_2: We never get to -1 horizontal position, the area is closed. When we get to the first square again, break the cycle.

                    //T("nextX", nextX, nextY);
                    counter++;
                    if (counter == size * size)
                    {
                        T("Corner2 discovery error.");

                        window.errorInWalkthrough = true;
                        window.errorString = "Corner2 discovery error.";
                        window.criticalError = true;
                        return false;
                    }

                    // left direction
                    currentDirection = (currentDirection == 3) ? 0 : currentDirection + 1;
                    int l = currentDirection;
                    int possibleNextX = nextX + directions[currentDirection][0];
                    int possibleNextY = nextY + directions[currentDirection][1];

                    // turn right until a field is empty 
                    while (InBorderRel2(possibleNextX, possibleNextY) || InTakenRel2(possibleNextX, possibleNextY))
                    {
                        l = (l == 0) ? 3 : l - 1;
                        possibleNextX = nextX + directions[l][0];
                        possibleNextY = nextY + directions[l][1];
                    }

                    if (currentDirection == 0 && l == 0 && nextY >= 1) // 0708: Corner can be found beneath
                    {
                        int hori = nextX + 1;
                        int vert = nextY + 1;

                        T("Corner at", hori, vert, "x2", x2, "y2", y2, "lx2", lx2, "ly2", ly2, circleDirectionLeft);

                        bool circleValid = false;
                        List<int[]> borderFields = new();

                        int i1, i2;
                        
                        i1 = InTakenIndexRel2(hori, vert);
                        i2 = InTakenIndexRel2(hori + 1, vert);

                        if (smallArea && i2 > i1 || !smallArea && i2 < i1)
                        {
                            circleValid = true;
                        }

                        if (circleValid)
                        {
                            if (hori == 1 && vert == 2) // close mid across
                            {
                                return true;

                            }
                            else if (hori == 2 && vert == 2) // close across
                            {
                                return true;
                            }
                            else if (hori == 1) // AreaUp
                            {
                                /* Example needed
                                int ex = vert - 1;

                                if (ex > 2)
                                {
                                    for (int k = ex - 1; k >= 2; k--)
                                    {
                                        borderFields.Add(new int[] { 1, k });
                                    }
                                }

                                ResetExamAreas();

                                if (CountAreaRel2(1, 1, 1, vert - 1, borderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    int whiteDiff = white - black;
                                    int nowWCount = 0;
                                    int nowBCount = 0;
                                    int laterWCount = 0;
                                    int laterBCount = 0;

                                    switch (ex % 4)
                                    {
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
                                            nowBCount = (ex - 2) / 4;
                                            laterWCount = (ex - 2) / 4;
                                            laterBCount = (ex - 2) / 4;
                                            break;
                                        case 3:
                                            nowWCount = (ex + 1) / 4;
                                            nowBCount = (ex - 3) / 4;
                                            laterWCount = (ex - 3) / 4;
                                            laterBCount = (ex + 1) / 4;
                                            break;
                                    }

                                    if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                    {
                                        T("Corner2: Cannot enter later");
                                        return true;
                                    }
                                }*/
                            }
                            else // Corner 0627, 0627_1
                            {
                                bool takenFound = false;
                                int left1 = 1;
                                int straight1 = 1;
                                int left2 = hori - 1;
                                int straight2 = vert - 1;

                                int nowWCount, nowWCountDown, nowBCount, laterWCount, laterBCount;
                                int a, n;

                                //check if all fields on the border line is free
                                if (vert == hori)
                                {
                                    a = hori - 1;
                                    nowWCountDown = nowWCount = 0;
                                    nowBCount = a - 1;
                                    laterWCount = -1;// means B = 1
                                    laterBCount = a - 1;

                                    for (int k = 1; k < hori; k++)
                                    {
                                        if (k < hori - 1)
                                        {
                                            if (InTakenRel2(k, k) || InTakenRel2(k + 1, k))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (InTakenRel2(k, k))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }

                                        if (k == 1)
                                        {
                                            borderFields.Add(new int[] { 2, 1 });
                                        }
                                        else if (k < hori - 1)
                                        {
                                            borderFields.Add(new int[] { k, k });
                                            borderFields.Add(new int[] { k + 1, k });
                                        }
                                    }
                                }
                                else if (hori > vert)
                                {
                                    a = vert - 1;
                                    n = (hori - vert - (hori - vert) % 2) / 2;

                                    if ((hori - vert) % 2 == 0)
                                    {
                                        if (n > 1)
                                        {
                                            nowWCountDown = nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                        }
                                        else
                                        {
                                            nowWCount = 0;
                                            nowWCountDown = 1;
                                        }
                                        nowBCount = a + (n - 1 - (n - 1) % 2) / 2;
                                        laterWCount = (n - n % 2) / 2;
                                        laterBCount = a + (n - n % 2) / 2;
                                    }
                                    else
                                    {
                                        if (n > 0)
                                        {
                                            nowWCountDown = nowWCount = a + (n - n % 2) / 2;
                                            laterBCount = (n + 2 - (n + 2) % 2) / 2;
                                        }
                                        else
                                        {
                                            nowWCount = a - 1;
                                            nowWCountDown = a;
                                            laterBCount = 0;
                                        }
                                        nowBCount = (n + 1 - (n + 1) % 2) / 2;
                                        laterWCount = a - 1 + (n + 1 - (n + 1) % 2) / 2;

                                    }

                                    for (int k = 1; k <= hori - vert; k++)
                                    {
                                        if (InTakenRel2(k, 1))
                                        {
                                            takenFound = true;
                                            break;
                                        }

                                        if (k > 1)
                                        {
                                            borderFields.Add(new int[] { k, 1 });
                                        }
                                    }

                                    for (int k = 1; k < vert; k++)
                                    {
                                        if (k < vert - 1)
                                        {
                                            if (InTakenRel2(hori - vert + k, k) || InTakenRel2(hori - vert + k + 1, k))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (InTakenRel2(hori - vert + k, k))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }

                                        if (k < vert - 1)
                                        {
                                            borderFields.Add(new int[] { hori - vert + k, k });
                                            borderFields.Add(new int[] { hori - vert + k + 1, k });
                                        }
                                    }
                                }
                                else // vert > hori
                                {
                                    a = hori - 1;
                                    n = (vert - hori - (vert - hori) % 2) / 2;

                                    if ((vert - hori) % 2 == 0)
                                    {
                                        nowWCountDown = nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                        nowBCount = a + (n - 1 - (n - 1) % 2) / 2;
                                        laterWCount = (n - n % 2) / 2;
                                        laterBCount = a + (n - n % 2) / 2;
                                    }
                                    else
                                    {
                                        nowWCountDown = nowWCount = 1 + (n + 1 - (n + 1) % 2) / 2;
                                        nowBCount = a - 1 + (n - n % 2) / 2;
                                        if (n > 0)
                                        {
                                            laterWCount = 1 + (n - n % 2) / 2;
                                        }
                                        else
                                        {
                                            laterWCount = 0;
                                        }
                                        laterBCount = a - 1 + (n + 1 - (n + 1) % 2) / 2;
                                    }

                                    for (int k = 1; k < hori; k++)
                                    {
                                        if (k < hori - 1 && hori > 2)
                                        {
                                            if (InTakenRel2(k, k) || InTakenRel2(k + 1, k))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (InTakenRel2(k, k))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }

                                        if (hori > 2) // there is no stair if corner is at 1 distance, only one field which is the start field.
                                        {
                                            if (k == 1)
                                            {
                                                borderFields.Add(new int[] { 2, 1 });
                                            }
                                            else if (k < hori - 1)
                                            {
                                                borderFields.Add(new int[] { k, k });
                                                borderFields.Add(new int[] { k + 1, k });
                                            }
                                            else
                                            {
                                                borderFields.Add(new int[] { k, k });
                                            }
                                        }
                                    }

                                    for (int k = 1; k <= vert - hori; k++)
                                    {
                                        if (InTakenRel2(hori - 1, hori - 1 + k))
                                        {
                                            takenFound = true;
                                            break;
                                        }

                                        if (k < vert - hori)
                                        {
                                            borderFields.Add(new int[] { hori - 1, hori - 1 + k });
                                        }
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas2();

                                    if (CountAreaRel2(left1, straight1, left2, straight2, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        int whiteDiff = white - black;

                                        if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                        {
                                            T("Corner2: Cannot enter later");
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    currentDirection = l;

                    nextX = possibleNextX;
                    nextY = possibleNextY;
                }
            }

            return false;
        }

        bool CheckSequenceRecursive(int side)
        {
            T("Recursive start side: " + side, "x2 y2 lx2 ly2: " + x2, y2, lx2, ly2);

            counterrec++;
            if (counterrec == size * size)
            {
                window.errorInWalkthrough = true;
                window.errorString = "Recursive overflow.";
                window.criticalError = true;

                return false;
            }

            newExitField0= new int[] { 0, 0 };
            newExitField = new int[] { 0, 0 };

            ResetExamAreas2(); // prevent showing an area from previous cycle

            bool leftSideClose = CheckNearFieldSmall2(); // contains exit points for next call but only works for c-shapes and close obstacles.
            bool leftSideEnterNow = CheckCorner2(side, true);
            lx2 = -lx2;
            ly2 = -ly2;
            bool rightSideClose = CheckNearFieldSmall3(); // 0722
            bool rightSideEnterNow = CheckCorner2(1 - side, true);
            lx2 = -lx2;
            ly2 = -ly2;

            T("Recursive checked", leftSideClose, leftSideEnterNow, rightSideClose, rightSideEnterNow);

            if ((leftSideClose || leftSideEnterNow) && (rightSideClose || rightSideEnterNow))
            {
                return true;
            }
            // right side close can happen with the future line
            // for now, we only take the right side C-shape into account as it happens in 740 293. Other close obstacles we don't check.
            else if (leftSideClose)
            //else if ((leftSideClose || rightSideClose) && newExitField[0] != 0)
            {
                T("CheckSequenceRecursive left side only x2 " + newExitField[0] + " y2 " + newExitField[1] + " direction rotated " + newDirectionRotated);

                // at 0723, it is important that both fields are added, because the sequence relies on the first when determining the direction of the obstacle.
                bool firstAdded = false;
                x2 = newExitField0[0];
                y2 = newExitField0[1];                
                if (x2 != 0 && y2 != 0)
                {
                    firstAdded = true;
                    path.Add(new int[] { x2, y2 });
                }
                x2 = newExitField[0];
                y2 = newExitField[1];
                path.Add(new int[] { x2, y2 });

                if (newDirectionRotated)
                {
                    int[] rotatedDir = RotateDir(lx2, ly2, side);
                    lx2 = rotatedDir[0];
                    ly2 = rotatedDir[1];
                    rotatedDir = RotateDir(sx2, sy2, side);
                    sx2 = rotatedDir[0];
                    sy2 = rotatedDir[1];
                }

                if (InTakenRel2(0, 1)) // 0708 Field in front of exit should be empty
                {
                    if (firstAdded)
                    {
                        path.RemoveAt(path.Count - 1);
                    }
                    path.RemoveAt(path.Count - 1);
                    return false;
                }

                bool ret = CheckSequenceRecursive(side);

                if (firstAdded)
                {
                    path.RemoveAt(path.Count - 1);
                }
                path.RemoveAt(path.Count - 1);

                return ret;

            }
            else if (rightSideClose)
            {
                T("CheckSequenceRecursive right side only x2 " + newExitField[0] + " y2 " + newExitField[1] + " direction rotated " + newDirectionRotated);

                x2 = newExitField[0];
                y2 = newExitField[1];
                path.Add(new int[] { x2, y2 });
                lx2 = -lx2;
                ly2 = -ly2;

                if (newDirectionRotated)
                {
                    int[] rotatedDir = RotateDir(lx2, ly2, 1 - side);
                    lx2 = rotatedDir[0];
                    ly2 = rotatedDir[1];
                    rotatedDir = RotateDir(sx2, sy2, 1 - side);
                    sx2 = rotatedDir[0];
                    sy2 = rotatedDir[1];
                }

                if (InTakenRel2(0, 1)) // 0708: Field in front of exit should be empty
                {
                    path.RemoveAt(path.Count - 1);
                    return false;
                }

                bool ret = CheckSequenceRecursive(1 - side);
                path.RemoveAt(path.Count - 1);

                return ret;
            }
            else
            {
                return false;
            }
        }

        bool CheckNearFieldSmallRel0(int x, int y, int side, int rotation, bool smallArea)
        { // close mid across only
          // obstacle right side of the field in question, area up
          // mid across and across fields
          // used for LeftRightAreaUp and LeftRightCorner
            int lx = 1;
            int ly = 0;
            int sx = 0;
            int sy = 1;

            // Mid across obstacle:
            // left side:
            // 1, 2
            // 2, -1
            // -2, 1

            // right side:
            // -1, 2
            // -2, -1
            // 2, 1

            // direction checkng is not necessary when the close obstacle is inside the area, but it is when the obstacle is at the exit point of the area. 1023055626

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 4; j++) // j = 0: middle, j = 1: small area, j = 2: big area, j = 3: big (right down) area
                {
                    if (i == side && j == rotation)
                    {
                        // close mid across
                        if (InTakenRel(x + lx + 2 * sx, y + ly + 2 * sy) && !InTakenRel(x + 2 * sx, y + 2 * sy) && !InTakenRel(x + lx + sx, y + ly + sy))
                        {

                            int i1 = InTakenIndexRel(x + 1 * lx + 2 * sx, y + 1 * ly + 2 * sy);
                            int i2 = InTakenIndexRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy);

                            if (smallArea && i2 > i1 || !smallArea && i2 < i1) return true;
                        }
                    }

                    if (j == 0) // rotate down (CCW): small area
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW): big area
                    {
                        lx = -lx;
                        ly = -ly;
                        sx = -sx;
                        sy = -sy;
                    }
                    else // rotate CW again
                    {
                        int s0 = sx;
                        int s1 = sy;
                        sx = -lx;
                        sy = -ly;
                        lx = s0;
                        ly = s1;
                    }
                }
                lx = -1;
                ly = 0;
                sx = 0;
                sy = 1;
            }
            return false;
        }

        bool CheckNearFieldSmallRel1(int x, int y, int side, int rotation, bool smallArea)
        { // close mid across and across only
          // obstacle right side of the field in question, area up
          // mid across and across fields
          // used for LeftRightAreaUp and LeftRightCorner
            int lx = 1;
            int ly = 0;
            int sx = 0;
            int sy = 1;

            // Mid across obstacle:
            // left side:
            // 1, 2
            // 2, -1
            // -2, 1

            // right side:
            // -1, 2
            // -2, -1
            // 2, 1

            // direction checkng is not necessary when the close obstacle is inside the area, but it is when the obstacle is at the exit point of the area. 1023055626

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 4; j++) // j = 0: middle, j = 1: small area, j = 2: big area, j = 3: big (right down) area
                {
                    if (i == side && j == rotation)
                    {
                        // close mid across
                        if (InTakenRel(x + lx + 2 * sx, y + ly + 2 * sy) && !InTakenRel(x + 2 * sx, y + 2 * sy) && !InTakenRel(x + lx + sx, y + ly + sy))
                        {
                            
                            int i1 = InTakenIndexRel(x + 1 * lx + 2 * sx, y + 1 * ly + 2 * sy);
                            int i2 = InTakenIndexRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy);

                            if (smallArea && i2 > i1 || !smallArea && i2 < i1) return true;
                        }

                        // close across
                        if (InTakenRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy) && !InTakenRel(x + lx + 2 * sx, y + ly + 2 * sy) && !InTakenRel(x + 2 * lx + sx, y + 2 * ly + sy))
                        {
                            
                            int i1 = InTakenIndexRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy);
                            int i2 = InTakenIndexRel(x + 3 * lx + 2 * sx, y + 3 * ly + 2 * sy);

                            if (smallArea && i2 > i1 || !smallArea && i2 < i1) return true;
                        }
                    }

                    if (j == 0) // rotate down (CCW): small area
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW): big area
                    {
                        lx = -lx;
                        ly = -ly;
                        sx = -sx;
                        sy = -sy;
                    }
                    else // rotate CW again
                    {
                        int s0 = sx;
                        int s1 = sy;
                        sx = -lx;
                        sy = -ly;
                        lx = s0;
                        ly = s1;
                    }
                }
                lx = -1;
                ly = 0;
                sx = 0;
                sy = 1;
            }
            return false;
        }

        bool CheckNearFieldSmallRel(int x, int y, int side, int rotation, bool smallArea)
        { // obstacle right side of the field in question, area up
          // mid across and across fields
          // used for LeftRightAreaUp and LeftRightCorner
            int lx = 1;
            int ly = 0;
            int sx = 0;
            int sy = 1;

            // Mid across obstacle:
            // left side:
            // 1, 2
            // 2, -1
            // -2, 1

            // right side:
            // -1, 2
            // -2, -1
            // 2, 1

            // direction checkng is not necessary when the close obstacle is inside the area, but it is when the obstacle is at the exit point of the area. 1023055626

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 4; j++) // j = 0: middle, j = 1: small area, j = 2: big area, j = 3: big (right down) area
                {
                    if (i == side && j == rotation)
                    {
                        // C-shape left
                        if (smallArea)
                        {
                            if (InTakenRel(x + 2 * lx, y + 2 * ly) && InTakenRel(x + lx - sx, y + ly - sy) && !InTakenRel(x + lx, y + ly))
                            {
                                return true;
                            }
                        }

                        // close mid across
                        if (InTakenRel(x + lx + 2 * sx, y + ly + 2 * sy) && !InTakenRel(x + 2 * sx, y + 2 * sy) && !InTakenRel(x + lx + sx, y + ly + sy))
                        {

                            int i1 = InTakenIndexRel(x + 1 * lx + 2 * sx, y + 1 * ly + 2 * sy);
                            int i2 = InTakenIndexRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy);

                            if (smallArea && i2 > i1 || !smallArea && i2 < i1) return true;
                        }

                        // close across
                        if (InTakenRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy) && !InTakenRel(x + lx + 2 * sx, y + ly + 2 * sy) && !InTakenRel(x + 2 * lx + sx, y + 2 * ly + sy))
                        {

                            int i1 = InTakenIndexRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy);
                            int i2 = InTakenIndexRel(x + 3 * lx + 2 * sx, y + 3 * ly + 2 * sy);

                            if (smallArea && i2 > i1 || !smallArea && i2 < i1) return true;
                        }
                    }

                    if (j == 0) // rotate down (CCW): small area
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW): big area
                    {
                        lx = -lx;
                        ly = -ly;
                        sx = -sx;
                        sy = -sy;
                    }
                    else // rotate CW again
                    {
                        int s0 = sx;
                        int s1 = sy;
                        sx = -lx;
                        sy = -ly;
                        lx = s0;
                        ly = s1;
                    }
                }
                lx = -1;
                ly = 0;
                sx = 0;
                sy = 1;
            }
            return false;
        }

        bool CheckNearFieldSmall2() // for use with Sequence
            // Case 2 and 3, used in recursive function
        {
            bool ret = false;

            // C-Shape, only left side should have it
            // Checking for InTakenRel2(1, -1) is not possible, because in Sequence first case, we are exiting the area at the middle border field.
            // But when it comes to the right side (if it was checked), it is necessary, otherwise we can detect a C-shape with the live end as in 213.
            if ((InTakenRel2(2, 0) || InBorderRelExact2(2, 0)) && !InTakenRel2(1, 0))
            {
                T("CheckNearFieldSmall2 C-Shape, left side");
                ret = true;

                newExitField0 = new int[] { x2 + lx2, y2 + ly2 };
                newExitField = new int[] { x2 + lx2 + sx2, y2 + ly2 + sy2 };
                newDirectionRotated = false;
            }

            //C-Shape up
            if (InTakenRel2(0, 2) && InTakenRel2(1, 1) && !InTakenRel2(0, 1))
            {
                T("CheckNearFieldSmall2 C-Shape up");
                ret = true;

                newExitField = new int[] { x2 - lx2 + sx2, y2 - ly2 + sy2 };
                newDirectionRotated = true;
            }

            // close mid across
            if (InTakenRel2(1, 2) && !InTakenRel2(0, 2) && !InTakenRel2(1, 1))
            {
                int middleIndex = InTakenIndexRel2(1, 2);
                int sideIndex = InTakenIndexRel2(2, 2);

                if (sideIndex > middleIndex)
                {
                    T("CheckNearFieldSmall2 close mid across");
                    ret = true;

                    // mid across overwrites C-shape
                    newExitField = new int[] { x2 + sx2, y2 + sy2 };
                    newDirectionRotated = true;
                }
            }

            // close across. Checking empty fields necessary, see 29558469
            if (InTakenRel2(2, 2) && !InTakenRel2(1, 2) && !InTakenRel2(2, 1))
            {
                int middleIndex = InTakenIndexRel2(2, 2);
                int sideIndex = InTakenIndexRel2(3, 2);

                if (sideIndex > middleIndex)
                {
                    T("CheckNearFieldSmall2 close across");
                    ret = true;

                    newExitField = new int[] { x2 + lx2 + sx2, y2 + ly2 + sy2 };
                    newDirectionRotated = true;
                }
            }

            return ret;
        }

        bool CheckNearFieldSmall3()
        {
            bool ret = false;

            // close mid across
            if (InTakenRel2(1, 2) && !InTakenRel2(0, 2) && !InTakenRel2(1, 1))
            {
                int middleIndex = InTakenIndexRel2(1, 2);
                int sideIndex = InTakenIndexRel2(2, 2);

                if (sideIndex > middleIndex)
                {
                    T("CheckNearFieldSmall3 close mid across");
                    ret = true;

                    // mid across overwrites C-shape
                    newExitField = new int[] { x2 + sx2, y2 + sy2 };
                    newDirectionRotated = true;
                }
            }

            return ret;
        }

        int[] RotateDir(int xDiff, int yDiff, int ccw)
        { // For double area, check if we can only step left
            List<int[]> directions;

            if (ccw == 0) // clockwise
            {
                directions = new List<int[]> { new int[] { 0, 1 }, new int[] { -1, 0 }, new int[] { 0, -1 }, new int[] { 1, 0 } };
            }
            else // counter-clockwise
            {
                directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } };
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

            int turnedDirection = currentDirection == 3 ? 0 : currentDirection + 1;

            return directions[turnedDirection];
        }
        // ----- copy end -----

        public void ResetExamAreas()
        {
            examAreaLines = new();
            examAreaLineTypes = new();
            examAreaLineDirections = new();
            examAreaPairFields = new();

            examAreaLine2 = new();
            examAreaLineType2 = 0;
            examAreaLineDirection2 = false;
            examAreaPairField2 = new();
        }

        public void ResetExamAreas2()
        {
            examAreaLine2 = new();
            examAreaLineType2 = new();
            examAreaLineDirection2 = new();
            examAreaPairField2 = new();
        }

        public void AddExamAreas(bool secondaryArea = false) // if a rule is true, we display all examined circles, but only add the checkerboard from the last one.
        {
            for (int i = 0; i < examAreaLines.Count; i++)
            {
                areaLines.Add(examAreaLines[i]);
                areaLineTypes.Add(examAreaLineTypes[i]);
                areaLineDirections.Add(examAreaLineDirections[i]);
                areaPairFields.Add(examAreaPairFields[i]);
                if (secondaryArea)
                {
                    areaLineSecondary.Add(true);
                }
                else
                {
                    areaLineSecondary.Add(false);
                }
            }

            if (examAreaLine2.Count != 0)
            {
                areaLines.Add(examAreaLine2);
                areaLineTypes.Add(examAreaLineType2);
                areaLineDirections.Add(examAreaLineDirection2);
                areaPairFields.Add(examAreaPairField2);
                areaLineSecondary.Add(true);
            }
        }

        // ----- Unused -----

        void CheckCShape()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if ((InTakenRel(2, 0) || InBorderRel(2, 0)) &&
                        (InTakenRel(1, -1) || InBorderRel(1, -1)) &&
                        !InTakenRel(1, 0) && !InBorderRel(1, 0) && !InCornerRel(1, 0))
                    {
                        CShape = true;
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

        void CheckNearField()
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

                    // needed if C-shape precondition is disabled
                    if (!InTakenRel(1, 1) && !InTakenRel(-1, 1))
                    {
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

                        // needed if C-Shape precondition is disabled
                        if (!InTakenRel(1, -1))
                        {
                            forbidden.Add(new int[] { x + lx, y + ly });
                        }
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

        void CheckFutureCShape() // Even future line can make a straight C-shape, see 0727_1
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
                        forbidden.Add(new int[] { x + sx, y + sy }); //straight				
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

        void CheckNearBorder()
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

        void CheckAreaNearBorder() // 0909. Check both straight approach and side.
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

        void CheckDirectionalArea()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (!(InTakenRel(1, 0) || InBorderRel(1, 0) || InTakenRel(2, 0) || InBorderRel(2, 0)))
                    {
                        bool circleDirectionLeft = (i == 0) ? true : false;
                        int hori = 0;
                        int vert = 1;

                        while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                        {
                            while (hori <= vert + 3 && !InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                            {
                                hori++;
                            }
                            // After stepping to side, we need to step down if the area contains more than the border line
                            // Check if the top row is empty, so we can exit the area.
                            if (hori == vert + 3 && !InTakenRel(hori - 1, vert + 1) && !InBorderRel(hori - 1, vert + 1) && !InTakenRel(hori - 2, vert + 1) && !InTakenRel(hori - 3, vert + 1))
                            {
                                bool circleValid = false;

                                if (InBorderRel(hori, vert))
                                {
                                    int i1 = InBorderIndexRel(hori, vert);
                                    int i2 = InBorderIndexRel(hori, vert + 1);

                                    if (i2 > i1)
                                    {
                                        circleValid = true;
                                    }
                                }
                                else
                                {
                                    int i1 = InTakenIndexRel(hori, vert);
                                    int i2 = InTakenIndexRel(hori, vert + 1);

                                    if (i2 != -1)
                                    {
                                        if (i2 < i1)
                                        {
                                            circleValid = true;

                                        }
                                    }
                                    else
                                    {
                                        i2 = InTakenIndexRel(hori, vert - 1);
                                        if (i2 > i1)
                                        {
                                            circleValid = true;
                                        }
                                    }
                                }

                                if (circleValid)
                                {
                                    bool takenFound = false;
                                    List<int[]> borderFields = new();

                                    for (int k = hori - 2; k >= 2; k--)
                                    {
                                        borderFields.Add(new int[] { k, k - 1 });
                                        borderFields.Add(new int[] { k, k - 2 });
                                    }

                                    // bottom and top row already checked for emptiness
                                    for (int k = hori - 2; k >= 3; k--)
                                    {
                                        if (InTakenRel(k, k - 2) || InTakenRel(k - 1, k - 2))
                                        {
                                            takenFound = true;
                                            break;
                                        }
                                    }

                                    if (!takenFound)
                                    {
                                        if (CountAreaRel(1, 0, hori - 1, hori - 3, borderFields, circleDirectionLeft, 3, true))
                                        {
                                            int black = (int)info[1];
                                            int white = (int)info[2];

                                            if (black == white)
                                            {
                                                int thisX = x;
                                                int thisY = y;
                                                int thisLx = lx;
                                                int thisLy = ly;

                                                // opposite side of the relation of the area to the live end
                                                lx = -lx;
                                                ly = -ly;

                                                // check all the corners if there is a close obstacle that would disable the horizontal far direction
                                                for (int k = 2; k <= hori - 2; k++)
                                                {
                                                    x = thisX + thisLx * k + sx * (k - 1);
                                                    y = thisY + thisLy * k + sy * (k - 1);

                                                    if (CheckNearFieldSmall1())
                                                    {
                                                        T("CheckDirectionalArea at " + thisX + " " + thisY + " obstacle encountered at " + x + " " + y);

                                                        DirectionalArea = true;
                                                        activeRules.Add("Directional Area");
                                                        activeRuleSizes.Add(new int[] { 7, 7 });
                                                        activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + thisLx, thisY + thisLy } });

                                                        forbidden.Add(new int[] { thisX + thisLx, thisY + thisLy });
                                                        // field behind, only relevant when rotated up
                                                        if (j == 1)
                                                        {
                                                            forbidden.Add(new int[] { thisX - sx, thisY - sy });
                                                        }
                                                    }
                                                }

                                                x = thisX;
                                                y = thisY;
                                                lx = thisLx;
                                                ly = thisLy;
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
                            hori = vert - 1;
                        }
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

        bool CheckDownStair(int side = -1, int nLx = 0, int nLy = 0, int nSx = 0, int nSy = 0) // 51015231, 53144883
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (side != -1 && !(side == i && j == 0)) continue; // if it is a next step checking, the pattern will not be rotated.

                    if (side != -1) // if it is right side, cycling through left side would change the values.
                    {
                        lx = nLx;
                        ly = nLy;
                        sx = nSx;
                        sy = nSy;
                    }

                    if (!InTakenRel(1, 0))
                    {
                        int hori = 1;
                        int vert = -1;


                        while (!InTakenRel(hori, vert) && InTakenRel(hori, vert - 1))
                        {
                            hori++;
                            vert--;
                        }

                        if (InTakenRel(hori, vert) && !InTakenRel(hori + 1, vert + 1) && InTakenRel(hori + 2, vert + 1))
                        {
                            hori++;
                            vert += 2;

                            while (!InTakenRel(hori, vert) && vert <= 0)
                            {
                                int thisX = x;
                                int thisY = y;

                                x = thisX + lx * hori + sx * vert;
                                y = thisY + ly * hori + sy * vert;

                                if (CheckNearFieldSmall1())
                                {
                                    if (side != -1)
                                    {
                                        return true; // For next step checking
                                    }

                                    DownStair = true;
                                    activeRules.Add("Down Stair");
                                    activeRuleSizes.Add(new int[] { 7, 8 });
                                    activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + lx, thisY + ly } });

                                    T("CheckDownStair far, cannot step left", i, j);

                                    forbidden.Add(new int[] { thisX + lx, thisY + ly });

                                }

                                x = thisX;
                                y = thisY;

                                hori--;
                                vert++;
                            }
                        }
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

            return false;
        }

        void Check3DistNextStep()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (Straight3I == i && Straight3J == j) // same side and rotation
                    {
                        int thisX = x;
                        int thisY = y;

                        x = thisX + sx;
                        y = thisY + sy;

                        // Check3DoubleAreaRotated will change x, y, lx and ly
                        // CheckDownStair may change sx and sy
                        int tempX = x;
                        int tempY = y;
                        int tempLx = lx;
                        int tempLy = ly;
                        int tempSx = sx;
                        int tempSy = sy;

                        // Both: 18677343

                        // Double Area only: 59434452
                        if (Check3DoubleAreaRotated(i))
                        {
                            T("Check3DoubleAreaRotated true", i, j);

                            DoubleAreaFirstCaseRotatedNext = true;
                            activeRules.Add("Double Area first case rotated next");
                            activeRuleSizes.Add(new int[] { 6, 4 });
                            activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + sx, thisY + sy }, new int[] { thisX - lx, thisY - ly } });

                            forbidden.Add(new int[] { thisX + sx, thisY + sy });
                            forbidden.Add(new int[] { thisX - lx, thisY - ly });
                        }

                        x = tempX;
                        y = tempY;
                        lx = tempLx;
                        ly = tempLy;

                        // Stair only: 2024_0604
                        if (CheckDownStair(i, lx, ly, sx, sy))
                        {
                            T("CheckDownStair true");

                            DownStairNext = true;
                            activeRules.Add("Down Stair next");
                            activeRuleSizes.Add(new int[] { 8, 8 });
                            activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + sx, thisY + sy }, new int[] { thisX - lx, thisY - ly } });

                            forbidden.Add(new int[] { thisX + sx, thisY + sy });
                            forbidden.Add(new int[] { thisX - lx, thisY - ly });
                        }

                        x = thisX;
                        y = thisY;
                        lx = tempLx;
                        ly = tempLy;
                        sx = tempSx;
                        sy = tempSy;
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

        bool Check3DoubleAreaRotated(int side = -1) // Take only the first case and rotate it. Next step checking will need it, otherwise it is built into AreaUp.
        {
            for (int i = 0; i < 2; i++)
            {
                if (side != -1 && side != i) continue;

                bool circleValid = false;
                bool circleDirectionLeft = (i == 0) ? true : false;
                int startX = 0, startY = 0, endX = 0, endY = 0;

                List<int[]> borderFields = new();

                if (InTakenRel(4, -1) && !InTakenRel(2, 0) && !InTakenRel(3, 0) && !InTakenRel(4, 0) && !InTakenRel(1, -1) && !InTakenRel(3, -1))
                {
                    int i1 = InTakenIndexRel(4, -1);
                    int i2 = InTakenIndexRel(4, -2);

                    if (i2 > i1)
                    {
                        circleValid = true;

                        startX = 1;
                        startY = 0;
                        endX = 3;
                        endY = 0;
                        borderFields.Add(new int[] { 2, 0 });
                    }
                }

                if (circleValid && CountAreaRel(startX, startY, endX, endY, borderFields, circleDirectionLeft, 2, true))
                {
                    int black = (int)info[1];
                    int white = (int)info[2];

                    if (black == white)
                    {
                        int thisX = x;
                        int thisY = y;

                        x = x + endX * lx + endY * thisSx;
                        y = y + endX * ly + endY * thisSy;

                        // Checking C-Shape not necessary, side straight will take care of it, because area is 1B.
                        if (CheckNearFieldSmall1())
                        {
                            if (side != -1)
                            {
                                return true; // We are only interested in the side the straight obstacle is going to. Both sides cannot be true at the same time.
                            }

                            T("Check3DoubleAreaRotated at " + thisX + " " + thisY);

                            DoubleArea1Rotated = true;
                            activeRules.Add("Double Area first case rotated");
                            activeRuleSizes.Add(new int[] { 6, 4 });
                            activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + lx, thisY + ly } });

                            forbidden.Add(new int[] { thisX + lx, thisY + ly });
                        }

                        x = thisX;
                        y = thisY;
                    }
                }

                lx = -lx;
                ly = -ly;
            }
            lx = thisLx;
            ly = thisLy;

            return false;
        }

        bool CheckNearFieldSmall1() // for use only with Double Area case 1, 2, 3 and 1 rotated, and Down Stair. Across is needed at 53144883
                                    // Sequence case 1 right side
        {
            // close mid across. In DirectionalArea, the empty fields are already checked.
            if (InTakenRel2(1, 2) && !InTakenRel2(0, 2) && !InTakenRel2(1, 1))
            {
                int middleIndex = InTakenIndexRel2(1, 2);
                int sideIndex = InTakenIndexRel2(2, 2);

                if (sideIndex > middleIndex)
                {

                    return true;
                }
            }

            // close across
            if (InTakenRel2(2, 2) && !InTakenRel2(1, 2) && !InTakenRel2(2, 1))
            {
                int middleIndex = InTakenIndexRel2(2, 2);
                int sideIndex = InTakenIndexRel2(3, 2);

                if (sideIndex > middleIndex)
                {
                    return true;
                }
            }

            return false;
        }

        bool CheckNearFieldSmall1_5() // for use only with Double Area case 1, 2, 3 and 1 rotated
                                      // Sequence case 1 left side
        {
            // C-shape (left)
            if ((InTakenRel2(2, 0) || InBorderRel2(2, 0)) && !InTakenRel2(1, 0))
            {
                return true;
            }

            // close mid across. In DirectionalArea, the empty fields are already checked.
            if (InTakenRel2(1, 2) && !InTakenRel2(0, 2) && !InTakenRel2(1, 1))
            {
                int middleIndex = InTakenIndexRel2(1, 2);
                int sideIndex = InTakenIndexRel2(2, 2);

                if (sideIndex > middleIndex)
                {

                    return true;
                }
            }

            // close across
            if (InTakenRel2(2, 2) && !InTakenRel2(1, 2) && !InTakenRel2(2, 1))
            {
                int middleIndex = InTakenIndexRel2(2, 2);
                int sideIndex = InTakenIndexRel2(3, 2);

                if (sideIndex > middleIndex)
                {
                    return true;
                }
            }

            return false;
        }

        void CheckStairArea() // #9 2024_0630: Stair on one side, and one of the steps creates an area where we can only enter now.
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {
                    int dist = 1;
                    while (InTakenRel(dist, dist - 1) && InTakenRel(dist + 1, dist) && !InTakenRel(dist, dist))
                    {
                        dist++;
                    }
                    dist--;

                    int k;
                    for (k = 1; k <= dist; k++)
                    {
                        path.Add(new int[] { x + (k - 1) * lx + k * sx, y + (k - 1) * ly + k * sy });
                        x2 = x + (k - 1) * lx + k * sx;
                        y2 = y + (k - 1) * ly + k * sy;
                        lx2 = -lx;
                        ly2 = -ly;
                        sx2 = sx;
                        sy2 = sy;

                        ResetExamAreas();

                        if (CheckCorner2(1 - i, true))
                        {
                            AddExamAreas(true);
                            T("StairArea " + dist + " dist: Cannot step straight");
                            forbidden.Add(new int[] { x + sx, y + sy });

                            for (int l = 1; l <= k; l++)
                            {
                                path.RemoveAt(path.Count - 1);
                            }

                            sx = thisSx;
                            sy = thisSy;
                            lx = thisLx;
                            ly = thisLy;
                            return;
                        }
                    }

                    // double area at the exit point of the stair, 0720

                    path.Add(new int[] { x + (k - 1) * lx + k * sx, y + (k - 1) * ly + k * sy });
                    x2 = x + (k - 1) * lx + k * sx;
                    y2 = y + (k - 1) * ly + k * sy;
                    lx2 = -lx;
                    ly2 = -ly;
                    sx2 = sx;
                    sy2 = sy;

                    if (CheckNearFieldSmallRel1(k - 1, k, 0, 0, true) && CheckNearFieldSmallRel0(k - 1, k, 1, 0, true))
                    {
                        T("StairArea end " + dist + " dist: Cannot step straight");
                        forbidden.Add(new int[] { x + sx, y + sy });
                    }

                    for (k = 0; k <= dist; k++)
                    {
                        path.RemoveAt(path.Count - 1);
                    }

                    // rotate CCW
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
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


        // ---- Count Area -----

        public bool CountAreaRel(int left1, int straight1, int left2, int straight2, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false)
        {
            //T("CountAreaRel " + left1 + " " + straight1 + " " + left2 + " " + straight2);
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

            return CountArea(x1, y1, x2, y2, absBorderFields, circleDirectionLeft, circleType, getInfo, false);
        }

        public bool CountAreaRel2(int left1, int straight1, int left2, int straight2, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false)
        {
            //T("CountAreaRel2 " + left1 + " " + straight1 + " " + left2 + " " + straight2, circleDirectionLeft);
            int x_1 = x2 + left1 * lx2 + straight1 * sx2;
            int y_1 = y2 + left1 * ly2 + straight1 * sy2;
            int x_2 = x2 + left2 * lx2 + straight2 * sx2;
            int y_2 = y2 + left2 * ly2 + straight2 * sy2;

            List<int[]> absBorderFields = new();
            if (!(borderFields is null))
            {
                foreach (int[] field in borderFields)
                {
                    absBorderFields.Add(new int[] { x2 + field[0] * lx2 + field[1] * sx2, y2 + field[0] * ly2 + field[1] * sy2 });
                }
            }

            return CountArea(x_1, y_1, x_2, y_2, absBorderFields, circleDirectionLeft, circleType, getInfo, true);
        }

        // Due to 0618_1, new algorithm is not used.
        private bool CountArea(int startX, int startY, int endX, int endY, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false, bool secondaryArea = false)
        // compareColors is for the starting situation of 1119, where we mark an impair area and know the entry and the exit field. We count the number of white and black cells of a checkered pattern, the color of the entry and exit should be one more than the other color.
        {
            bool debug = false;
            bool debug2 = false;

            // find coordinates of the top left (circleDirection = right) or top right corner (circleDirection = left)
            int minY = startY;
            int limitX = startX;
            int startIndex;

            int xDiff, yDiff;
            List<int[]> areaLine = new();

            if (borderFields == null || borderFields.Count == 0)
            {
                if (Math.Abs(endX - startX) == 2 || Math.Abs(endY - startY) == 2)
                {
                    int middleX = (endX + startX) / 2;
                    int middleY = (endY + startY) / 2;
                    xDiff = startX - middleX;
                    yDiff = startY - middleY;
                    areaLine.Add(new int[] { middleX, middleY });
                    if (debug) T("Adding border " + middleX + " " + middleY);

                }
                else
                {
                    xDiff = startX - endX;
                    yDiff = startY - endY;
                }
            }
            else
            {
                areaLine = new();
                foreach (int[] field in borderFields)
                {
                    areaLine.Add(new int[] { field[0], field[1] });
                    if (debug) T("Adding border " + field[0] + " " + field[1]);
                }
                xDiff = startX - borderFields[borderFields.Count - 1][0];
                yDiff = startY - borderFields[borderFields.Count - 1][1];
            }

            areaLine.Add(new int[] { startX, startY });
            if (debug) T("Adding start " + startX + " " + startY);

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

            startIndex = areaLine.Count - 1;

            // if the field in straight direction is the live end, we need to turn (right if the circle direction is left). Similarly, if the live end is across on the same side the direction is going.
            int turnedDirection = currentDirection == 0 ? 3 : currentDirection - 1;

            int x = path[path.Count - 1][0];
            int y = path[path.Count - 1][1];

            // second condition is needed in case of 2024_0411_1 where future possibility creates a 2-field area
            if (x == nextX + xDiff && y == nextY + yDiff || InTaken(nextX + xDiff, nextY + yDiff) || x == nextX + xDiff + directions[turnedDirection][0] && y == nextY + yDiff + directions[turnedDirection][1])
            {
                currentDirection = turnedDirection;
            }

            // T("currentDirection: " + currentDirection + ", " + (nextX + directions[currentDirection][0]) + " " + (nextY + directions[currentDirection][1]) + " taken: " + InTaken(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]));

            // In case of an area of 2, 3 or a longer column
            if (InTaken(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]) || InBorder(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]))
            {
                currentDirection = currentDirection == 0 ? 3 : currentDirection - 1;
            }
            nextX += directions[currentDirection][0];
            nextY += directions[currentDirection][1];

            areaLine.Add(new int[] { nextX, nextY });
            if (debug) T("Adding continued " + nextX + " " + nextY);

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
                // int startDirection = currentDirection;
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

                    window.errorInWalkthrough = true;
                    window.criticalError = true;
                    window.errorString = "Corner is reached.";
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

                if (areaLine.Count == size * size)
                {
                    T("Area walkthrough error.");

                    window.errorInWalkthrough = true;
                    window.criticalError = true;
                    window.errorString = "Area walkthrough error.";
                    return false;
                }

                if (debug) T("Adding " + nextX + " " + nextY + " count " + areaLine.Count);

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

            if (debug)
            {
                T("minY " + minY + " limitX " + limitX + " startIndex " + startIndex);
                foreach (int[] a in areaLine)
                {
                    T(a[0] + " " + a[1]);
                }
            }

            //Special cases are not yet programmed in here as in MainWindow.xaml.cs. We take a gradual approach, starting from the cases that can happen on 7 x 7.

            if (!secondaryArea)
            {
                examAreaLines.Add(areaLine);
                examAreaLineTypes.Add(circleType);
                examAreaLineDirections.Add(circleDirectionLeft);
            }
            else
            {
                examAreaLine2 = Copy(areaLine);
                examAreaLineType2 = circleType;
                examAreaLineDirection2 = circleDirectionLeft;
            }

            int area = 0;
            List<int[]> startSquares = new();
            List<int[]> endSquares = new();

            if (areaLine.Count > 2)
            {
                int[] startCandidate = new int[] { limitX, minY };
                int[] endCandidate = new int[] { limitX, minY };

                if (debug2) T("arealine start " + startCandidate[0] + " " + startCandidate[1]);

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

                // chech if the arealine is one row (column is not a problem for the algorithm)

                int otherX = limitX;
                bool oneRow = true;

                foreach (int[] field in areaLine)
                {
                    x = field[0];
                    y = field[1];

                    if (circleDirectionLeft && x < otherX)
                    {
                        otherX = x;
                    }
                    else if (!circleDirectionLeft && x > otherX)
                    {
                        otherX = x;
                    }

                    if (y != minY)
                    {
                        oneRow = false;
                        break;
                    }
                }

                if (oneRow)
                {
                    if (otherX < limitX)
                    {
                        startSquares.Add(new int[] { otherX, minY });
                        endSquares.Add(new int[] { limitX, minY });
                    }
                    else
                    {
                        startSquares.Add(new int[] { limitX, minY });
                        endSquares.Add(new int[] { otherX, minY });
                    }
                }
                else
                {
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

                        if (debug2) T("field x " + field[0] + " y " + field[1] + " currentY " + currentY + " startCandidate " + startCandidate[0] + " " + startCandidate[1] + " endCandidate " + endCandidate[0] + " " + endCandidate[1]);

                        if (fieldY > currentY)
                        {
                            if (circleDirectionLeft)
                            {
                                //in the case where where the previous row was a closed peak, but an open dip was preceding it: the previous end field should have the same y and lower x
                                if (endSquares.Count > 0)
                                {
                                    int[] square = endSquares[endSquares.Count - 1];
                                    x = square[0];
                                    y = square[1];

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
                                    x = square[0];
                                    y = square[1];

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
                                    x = square[0];
                                    y = square[1];

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
                                    x = square[0];
                                    y = square[1];

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
                                    x = square[0];
                                    y = square[1];

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
                                    x = square[0];
                                    y = square[1];

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
                                    x = square[0];
                                    y = square[1];

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
                                    x = square[0];
                                    y = square[1];

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
                        if (singleField)
                        {
                            // L-shape
                            if (endSquares.Count == 1)
                            {
                                endSquares.Add(endCandidate);
                                startSquares.Add(startCandidate);
                            }
                            // add startCandidate, unless the last row is an open dip
                            else
                            {
                                int[] square = endSquares[endSquares.Count - 1];
                                y = square[1];

                                if (y != currentY - 1)
                                {
                                    startSquares.Add(startCandidate);
                                }
                            }

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
                            // L-shape
                            if (startSquares.Count == 1)
                            {
                                startSquares.Add(startCandidate);
                                endSquares.Add(endCandidate);
                            }
                            // add startCandidate, unless the last row is an open dip
                            else
                            {
                                int[] square = startSquares[startSquares.Count - 1];
                                y = square[1];

                                if (y != currentY - 1)
                                {
                                    endSquares.Add(endCandidate);
                                }
                            }

                        }
                        else
                        {
                            endSquares.Add(endCandidate);
                        }
                    }
                }

                /* T("circleDirectionLeft " + circleDirectionLeft + " singleField " + singleField);
                foreach (int[] sfield in startSquares)
                {
                    T("startsquare: " + sfield[0] + " " + sfield[1]);
                }
                foreach (int[] efield in endSquares)
                {
                    T("endsquare: " + efield[0] + " " + efield[1]);
                } */

                int eCount = endSquares.Count;

                // it should never happen if the above algorithm is bug-free.
                if (startSquares.Count != eCount)
                {
                    foreach (int[] f in startSquares)
                    {
                        T("startSquares " + f[0] + " " + f[1]);
                    }
                    foreach (int[] f in endSquares)
                    {
                        T("endSquares " + f[0] + " " + f[1]);
                    }

                    window.errorInWalkthrough = true;
                    window.criticalError = true;
                    window.errorString = "Count of start and end squares are inequal: " + startSquares.Count + " " + eCount;
                    return false;
                }

                for (int i = 0; i < eCount; i++)
                {
                    area += endSquares[i][0] - startSquares[i][0] + 1;
                }
            }
            else // area is 2. No rule will be applies, but the black and white field counts have to be right.
            {
                area = areaLine.Count;

                if (startY == endY)
                {
                    if (startX < endX)
                    {
                        startSquares.Add(new int[] { startX, startY });
                        endSquares.Add(new int[] { endX, endY });
                    }
                    else
                    {
                        startSquares.Add(new int[] { endX, endY });
                        endSquares.Add(new int[] { startX, startY });
                    }
                }
                else
                {
                    startSquares.Add(new int[] { startX, startY });
                    startSquares.Add(new int[] { endX, endY });
                    endSquares.Add(new int[] { startX, startY });
                    endSquares.Add(new int[] { endX, endY });
                }
            }

            if (debug) T("Count area: " + area);

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
                            x = field[0];
                            y = field[1];
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
                                    info = new List<object> { area % 2, pairCount, impairCount };
                                    if (!secondaryArea)
                                    {
                                        examAreaPairFields.Add(pairFields);
                                    }
                                    else
                                    {
                                        examAreaPairField2 = Copy(pairFields);
                                    }                                    
                                }
                                else
                                {
                                    info = new List<object> { area % 2, impairCount, pairCount };
                                    if (!secondaryArea)
                                    {
                                        examAreaPairFields.Add(impairFields);
                                    }
                                    else
                                    {
                                        examAreaPairField2 = Copy(impairFields);
                                    }
                                }
                            }
                            else
                            {
                                if ((startX + startY) % 2 == 1)
                                {
                                    info = new List<object> { area % 2, pairCount, impairCount };
                                    if (!secondaryArea)
                                    {
                                        examAreaPairFields.Add(pairFields);
                                    }
                                    else
                                    {
                                        examAreaPairField2 = Copy(pairFields);
                                    }
                                }
                                else
                                {
                                    info = new List<object> { area % 2, impairCount, pairCount };
                                    if (!secondaryArea)
                                    {
                                        examAreaPairFields.Add(impairFields);
                                    }
                                    else
                                    {
                                        examAreaPairField2 = Copy(impairFields);
                                    }
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

        private bool CountAreaNew(int startX, int startY, int endX, int endY, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false)
        // compareColors is for the starting situation of 1119, where we mark an impair area and know the entry and the exit field. We count the number of white and black cells of a checkered pattern, the color of the entry and exit should be one more than the other color.
        {
            bool debug = true;
            bool debug2 = true;

            // find coordinates of the top left (circleDirection = right) or top right corner (circleDirection = left)
            int minY = startY;
            int limitX = startX;
            int startIndex;

            int xDiff, yDiff;
            List<int[]> areaLine = new();

            if (borderFields == null || borderFields.Count == 0)
            {
                if (Math.Abs(endX - startX) == 2 || Math.Abs(endY - startY) == 2)
                {
                    int middleX = (endX + startX) / 2;
                    int middleY = (endY + startY) / 2;
                    xDiff = startX - middleX;
                    yDiff = startY - middleY;
                    areaLine.Add(new int[] { middleX, middleY });
                    if (debug) T("Adding border " + middleX + " " + middleY);

                }
                else
                {
                    xDiff = startX - endX;
                    yDiff = startY - endY;
                }
            }
            else
            {
                areaLine = new();
                foreach (int[] field in borderFields)
                {
                    areaLine.Add(new int[] { field[0], field[1] });
                    if (debug) T("Adding border " + field[0] + " " + field[1]);
                }
                xDiff = startX - borderFields[borderFields.Count - 1][0];
                yDiff = startY - borderFields[borderFields.Count - 1][1];
            }

            areaLine.Add(new int[] { startX, startY });
            if (debug) T("Adding start " + startX + " " + startY);

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

            startIndex = areaLine.Count - 1;

            // if the field in straight direction is the live end, we need to turn (right if the circle direction is left). Similarly, if the live end is across on the same side the direction is going.
            int turnedDirection = currentDirection == 0 ? 3 : currentDirection - 1;
            // second condition is needed in case of 2024_0411_1 where future possibility creates a 2-field area
            if (x == nextX + xDiff && y == nextY + yDiff || InTaken(nextX + xDiff, nextY + yDiff) || x == nextX + xDiff + directions[turnedDirection][0] && y == nextY + yDiff + directions[turnedDirection][1])
            {
                currentDirection = turnedDirection;
            }

            // T("currentDirection: " + currentDirection + ", " + (nextX + directions[currentDirection][0]) + " " + (nextY + directions[currentDirection][1]) + " taken: " + InTaken(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]));

            // In case of an area of 2, 3 or a longer column
            if (InTaken(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]) || InBorder(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]))
            {
                currentDirection = currentDirection == 0 ? 3 : currentDirection - 1;
            }
            nextX += directions[currentDirection][0];
            nextY += directions[currentDirection][1];

            areaLine.Add(new int[] { nextX, nextY });
            if (debug) T("Adding continued " + nextX + " " + nextY);

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
                // int startDirection = currentDirection;
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

                    window.errorInWalkthrough = true;
                    window.criticalError = true;
                    window.errorString = "Corner is reached.";
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
                if (debug) T("Adding " + nextX + " " + nextY + " count " + areaLine.Count);

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

            if (debug)
            {
                T("minY " + minY + " limitX " + limitX + " startIndex " + startIndex);
                foreach (int[] a in areaLine)
                {
                    T(a[0] + " " + a[1]);
                }
            }

            //Special cases are not yet programmed in here as in MainWindow.xaml.cs. We take a gradual approach, starting from the cases that can happen on 7 x 7.

            examAreaLines.Add(areaLine);
            examAreaLineTypes.Add(circleType);
            examAreaLineDirections.Add(circleDirectionLeft);

            int area = 0;
            List<int[]> startSquares = new();
            List<int[]> endSquares = new();

            if (areaLine.Count > 2)
            {
                int thisStartX = startX;
                int thisStartY = startY;

                int[] startCandidate = new int[] { limitX, minY };
                int[] endCandidate = new int[] { limitX, minY };

                if (debug2) T("arealine start " + startCandidate[0] + " " + startCandidate[1]);

                int currentY = minY;

                bool singleField = false;
                // check if there is a one square row on the top

                int prevIndex = (startIndex > 0) ? startIndex - 1 : areaLine.Count - 1;
                int nextIndex = (startIndex < areaLine.Count - 1) ? startIndex + 1 : 0;

                if (areaLine[startIndex][1] != areaLine[prevIndex][1] && areaLine[startIndex][1] != areaLine[nextIndex][1])
                {
                    singleField = true;
                    if (debug2) T("Single field on top");
                }

                // check if the arealine is one row (column is not a problem for the algorithm)

                int otherX = limitX;
                bool oneRow = true;
                bool startRepeat = false;
                bool startRepeat1 = false;
                bool startRepeat2 = false;

                int i = 0;
                foreach (int[] field in areaLine)
                {
                    int x = field[0];
                    int y = field[1];

                    if (y == minY)
                    {
                        if (circleDirectionLeft && x < otherX)
                        {
                            otherX = x;
                        }
                        else if (!circleDirectionLeft && x > otherX)
                        {
                            otherX = x;
                        }
                    }

                    if (y != minY)
                    {
                        oneRow = false;
                    }

                    if (x == limitX && y == minY && i != startIndex)
                    {
                        startRepeat = true;
                    }
                    i++;
                }

                // 214111, first row should not be added to start and end squares
                // crossing over the live end is only a problem if we return. An L-shape like 31817 is no problem.
                if (startRepeat && (!circleDirectionLeft && limitX < this.x && otherX > this.x || circleDirectionLeft && limitX > this.x && otherX < this.x) && minY == this.y - 1 && !InTakenAbs(leftField) && !InTakenAbs(rightField))
                {
                    startRepeat1 = true;
                }

                if (oneRow)
                {
                    if (otherX < limitX)
                    {
                        startSquares.Add(new int[] { otherX, minY });
                        endSquares.Add(new int[] { limitX, minY });
                    }
                    else
                    {
                        startSquares.Add(new int[] { limitX, minY });
                        endSquares.Add(new int[] { otherX, minY });
                    }
                }
                else
                {
                    /* Test algorithm with all cases in References/countarea folder:
                     * 1
                     * 6
                     * 198
                     * 348
                     * 22323
                     * 2024_0611_7
                     * 2024_0611_8
                     */

                    int x = 0;
                    int y = 0;

                    for (i = 1; i < areaLine.Count; i++)
                    {
                        int index = startIndex + i;
                        if (index >= areaLine.Count)
                        {
                            index -= areaLine.Count;
                        }
                        int[] field = areaLine[index];
                        x = field[0];
                        y = field[1];
                        int[] square;

                        if (debug2) T("field x " + x + " y " + y + " currentY " + currentY + " startCandidate " + startCandidate[0] + " " + startCandidate[1] + " endCandidate " + endCandidate[0] + " " + endCandidate[1]);

                        if (y > currentY)
                        {
                            if (circleDirectionLeft)
                            {
                                // we descend first
                                if (startSquares.Count == 0 && endSquares.Count == 0)
                                {
                                    // In case of 214111, we add the first field to the start square. It will be removed in the end to prevent a duplicate, but an ascension needs a start field to be present.
                                    // Second condition is to prevent 22495 where the after the start, we step down.
                                    if (!startRepeat1 || startRepeat1 && x == limitX)
                                    {
                                        if (singleField)
                                        {
                                            startSquares.Add(startCandidate);
                                        }
                                        // 348, first row is walked through when we descend. But 198 should not add end square.
                                        else if (startCandidate[0] != endCandidate[0] && x == limitX)
                                        {
                                            startSquares.Add(startCandidate);
                                        }
                                        endSquares.Add(endCandidate);
                                    }
                                    else
                                    {
                                        startRepeat2 = true;
                                        endSquares.Add(endCandidate);
                                    }
                                }
                                else
                                {
                                    // no open peak on the bottom without having startsquares and endsquares -> descending
                                    // no closed peak on the bottom without having endsquares -> ascending
                                    // no open peak on the top without having endsquares -> ascending
                                    // no closed peak on the top without having endsquares -> descending

                                    // we have a startsquare after descending from the top row

                                    if (startSquares.Count > 0)
                                    {
                                        square = startSquares[startSquares.Count - 1];
                                        startX = square[0];
                                        startY = square[1];

                                        if (y == startY)
                                        {
                                            // open peak on the bottom
                                            if (x < startX)
                                            {
                                                startCandidate = endCandidate = field;
                                                currentY = y;
                                                continue;
                                            }
                                            else
                                            {
                                                // closed peak on the top
                                                startSquares.Add(startCandidate);
                                                endSquares.Add(endCandidate);
                                                startCandidate = endCandidate = field;
                                                currentY = y;
                                                continue;
                                            }
                                        }
                                        else if (y == startY + 1)
                                        {
                                            // first row after open peak on the bottom: stair left down or straight down
                                            if (x < startX)
                                            {
                                                endSquares.Add(endCandidate);
                                                startCandidate = endCandidate = field;
                                                currentY = y;
                                                continue;
                                            }
                                        }
                                    }

                                    square = endSquares[endSquares.Count - 1];
                                    endX = square[0];
                                    endY = square[1];

                                    if (y == endY + 1)
                                    {
                                        // closed peak on the top, preceded by and open peak
                                        if (x > endX)
                                        {
                                            startSquares.Add(startCandidate);
                                            endSquares.Add(endCandidate);
                                        }
                                    }
                                    // stair left down or straight down
                                    else
                                    {
                                        endSquares.Add(endCandidate);
                                    }
                                }
                            }
                            else
                            {
                                if (startSquares.Count == 0 && endSquares.Count == 0)
                                {
                                    // In case of 214111, we add the first field to the start square. It will be removed in the end to prevent a duplicate, but an ascension needs a start field to be present.
                                    // Second condition is to prevent 22495 where the after the start, we step down.
                                    if (!startRepeat1 || startRepeat1 && x == limitX)
                                    {
                                        if (singleField)
                                        {
                                            endSquares.Add(endCandidate);
                                        }
                                        // 348, first row is walked through when we descend. But 198 should not add end square.
                                        else if (startCandidate[0] != endCandidate[0] && x == limitX)
                                        {
                                            endSquares.Add(endCandidate);
                                        }
                                        startSquares.Add(startCandidate);
                                    }
                                    else
                                    {
                                        startRepeat2 = true;
                                        startSquares.Add(startCandidate);
                                    }
                                }
                                else
                                {
                                    // no open peak on the bottom without having endsquares and startsquares -> descending
                                    // no closed peak on the bottom without having startsquares -> ascending
                                    // no open peak on the top without having startsquares -> ascending
                                    // no closed peak on the top without having startsquares -> descending

                                    // we have a startsquare after descending from the top row

                                    if (endSquares.Count > 0)
                                    {
                                        square = endSquares[endSquares.Count - 1];
                                        endX = square[0];
                                        endY = square[1];

                                        if (y == endY)
                                        {
                                            // open peak on the bottom
                                            if (x > endX)
                                            {
                                                startCandidate = endCandidate = field;
                                                currentY = y;
                                                continue;
                                            }
                                            else
                                            {
                                                // closed peak on the top
                                                startSquares.Add(startCandidate);
                                                endSquares.Add(endCandidate);
                                                startCandidate = endCandidate = field;
                                                currentY = y;
                                                continue;
                                            }
                                        }
                                        else if (y == endY + 1)
                                        {
                                            // first row after open peak on the bottom: stair right down or straight down
                                            if (x > endX)
                                            {
                                                startSquares.Add(startCandidate);
                                                startCandidate = endCandidate = field;
                                                currentY = y;
                                                continue;
                                            }
                                        }
                                    }

                                    square = startSquares[startSquares.Count - 1];
                                    startX = square[0];
                                    startY = square[1];

                                    if (y == startY + 1)
                                    {
                                        // closed peak on the top, preceded by an open peak
                                        if (x < startX)
                                        {
                                            startSquares.Add(startCandidate);
                                            endSquares.Add(endCandidate);
                                        }
                                    }
                                    // stair right down or straight down
                                    else
                                    {
                                        startSquares.Add(startCandidate);
                                    }
                                }
                            }
                            startCandidate = endCandidate = field;
                        }
                        else if (y == currentY)
                        {
                            if (x < startCandidate[0])
                            {
                                startCandidate = field;
                            }
                            else if (x > endCandidate[0])
                            {
                                endCandidate = field;
                            }
                        }
                        else
                        {
                            if (circleDirectionLeft)
                            {
                                square = endSquares[endSquares.Count - 1];
                                endX = square[0];
                                endY = square[1];

                                if (y == endY)
                                {
                                    // open peak on the top
                                    if (x > endX)
                                    {
                                        startCandidate = endCandidate = field;
                                        currentY = y;
                                        continue;
                                    }
                                    else
                                    {
                                        // closed peak on the bottom
                                        startSquares.Add(startCandidate);
                                        endSquares.Add(endCandidate);
                                        startCandidate = endCandidate = field;
                                        currentY = y;
                                        continue;
                                    }
                                }
                                else if (y == endY - 1)
                                {
                                    // first row after an open peak on the top: stair right up or straight up
                                    if (x > endX)
                                    {
                                        startSquares.Add(startCandidate);
                                        startCandidate = endCandidate = field;
                                        currentY = y;
                                        continue;
                                    }
                                }

                                square = startSquares[startSquares.Count - 1];
                                startX = square[0];
                                startY = square[1];

                                if (y == startY - 1)
                                {
                                    // closed peak on bottom, preceded by an open peak
                                    if (x < startX)
                                    {
                                        startSquares.Add(startCandidate);
                                        endSquares.Add(endCandidate);
                                    }
                                    // first row after an open peak on the top: stair right up or straight up
                                    else
                                    {
                                        startSquares.Add(startCandidate);
                                    }
                                }
                                // stair right up or straight up
                                else
                                {
                                    startSquares.Add(startCandidate);
                                }
                            }
                            else
                            {
                                square = startSquares[startSquares.Count - 1];
                                startX = square[0];
                                startY = square[1];

                                if (y == startY)
                                {
                                    // open peak on the top
                                    if (x < startX)
                                    {
                                        startCandidate = endCandidate = field;
                                        currentY = y;
                                        continue;
                                    }
                                    else
                                    {
                                        // closed peak on the bottom
                                        startSquares.Add(startCandidate);
                                        endSquares.Add(endCandidate);
                                        startCandidate = endCandidate = field;
                                        currentY = y;
                                        continue;
                                    }
                                }
                                else if (y == startY - 1)
                                {
                                    // first row after an open peak on the top: stair left up or straight up
                                    if (x < startX)
                                    {
                                        endSquares.Add(endCandidate);
                                        startCandidate = endCandidate = field;
                                        currentY = y;
                                        continue;
                                    }
                                }

                                square = endSquares[endSquares.Count - 1];
                                endX = square[0];
                                endY = square[1];

                                if (y == endY - 1)
                                {
                                    // closed peak on bottom, preceded by an open peak
                                    if (x > endX)
                                    {
                                        startSquares.Add(startCandidate);
                                        endSquares.Add(endCandidate);
                                    }
                                }
                                // stair left up or straight up
                                else
                                {
                                    endSquares.Add(endCandidate);
                                }
                            }
                            startCandidate = endCandidate = field;
                        }
                        currentY = y;
                    }

                    //add last field
                    if (circleDirectionLeft)
                    {
                        // 0611_8, checkstraight straight: we finish on the left side of the top row. End should not be added as it is the same as the first end field.

                        // 350: checkstraight straight: end row is a bottom row, it acts as a closed peak, so end square should be added.
                        if (x < endCandidate[0] && y == minY + 1)
                        {
                            int[] square = endSquares[endSquares.Count - 1];
                            endX = square[0];
                            endY = square[1];

                            if (y > endY)
                            {
                                endSquares.Add(endCandidate);
                            }
                        }

                        // 1273: L-shape, end row finishes on the right, below minY. We need to add a closed bottom peak. The last end square should be above and at the same x position or right.
                        if (x == endCandidate[0] && y == minY + 1)
                        {
                            int[] square = endSquares[endSquares.Count - 1];
                            endX = square[0];
                            endY = square[1];

                            if (y > endY && x <= endX)
                            {
                                endSquares.Add(endCandidate);
                            }
                        }

                        startSquares.Add(startCandidate);

                        if (startRepeat2)
                        {
                            endSquares.RemoveAt(0);
                        }
                    }
                    else
                    {

                        // 0611_8, checkstraight straight: we finish on the left side of the top row. End should not be added as it is the same as the first end field.

                        // 350: checkstraight straight: end row is a bottom row, it acts as a closed peak, so end square should be added.
                        if (x > startCandidate[0] && y == minY + 1)
                        {
                            int[] square = startSquares[startSquares.Count - 1];
                            startX = square[0];
                            startY = square[1];

                            if (y > startY)
                            {
                                startSquares.Add(startCandidate);
                            }
                        }

                        // 1273: L-shape, end row finishes on the left, below minY. We need to add a closed bottom peak. The last end square should be above and at the same x position or left.
                        if (x == startCandidate[0] && y == minY + 1)
                        {
                            int[] square = startSquares[startSquares.Count - 1];
                            startX = square[0];
                            startY = square[1];

                            if (y > startY && x <= startX)
                            {
                                startSquares.Add(startCandidate);
                            }
                        }

                        endSquares.Add(endCandidate);

                        if (startRepeat2)
                        {
                            startSquares.RemoveAt(0);
                        }
                    }

                    startX = thisStartX;
                    startY = thisStartY;
                }

                if (debug2)
                {
                    T("circleDirectionLeft " + circleDirectionLeft + " singleField " + singleField);
                    foreach (int[] sfield in startSquares)
                    {
                        T("startsquare: " + sfield[0] + " " + sfield[1]);
                    }
                    foreach (int[] efield in endSquares)
                    {
                        T("endsquare: " + efield[0] + " " + efield[1]);
                    }
                }

                int eCount = endSquares.Count;

                // it should never happen if the above algorithm is bug-free.
                if (startSquares.Count != eCount)
                {
                    T("Count of start and end squares are inequal: " + startSquares.Count + " " + eCount);
                    foreach (int[] f in startSquares)
                    {
                        T("startSquares " + f[0] + " " + f[1]);
                    }
                    foreach (int[] f in endSquares)
                    {
                        T("endSquares " + f[0] + " " + f[1]);
                    }

                    window.errorInWalkthrough = true;
                    window.criticalError = true;
                    window.errorString = "Count of start and end squares are inequal: " + startSquares.Count + " " + eCount;
                    return false;
                }

                for (i = 0; i < eCount; i++)
                {
                    area += endSquares[i][0] - startSquares[i][0] + 1;
                }
            }
            else // area is 2. No rule will be applies, but the black and white field counts have to be right.
            {
                area = areaLine.Count;

                if (startY == endY)
                {
                    if (startX < endX)
                    {
                        startSquares.Add(new int[] { startX, startY });
                        endSquares.Add(new int[] { endX, endY });
                    }
                    else
                    {
                        startSquares.Add(new int[] { endX, endY });
                        endSquares.Add(new int[] { startX, startY });
                    }
                }
                else
                {
                    startSquares.Add(new int[] { startX, startY });
                    startSquares.Add(new int[] { endX, endY });
                    endSquares.Add(new int[] { startX, startY });
                    endSquares.Add(new int[] { endX, endY });
                }
            }

            if (debug) T("Count area: " + area);
            
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

        // ----- Field checking -----

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

        public bool InBorderRel2(int left, int straight)
        {
            int x = x2 + left * lx2 + straight * sx2;
            int y = y2 + left * ly2 + straight * sy2;
            return InBorder(x, y);
        }

        public bool InBorder(int x, int y) // allowing negative values could cause an error in AddFutureLines 2x2 checking, but it is necessary in CheckLeftRightCorner due to possibility checking
        {
			if (x <= 0 || x >= size + 1 || y <= 0 || y >= size + 1) return true;
			return false;
		}

        public bool InBorderRelExact(int left, int straight)
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
            return InBorderExact(x, y);
        }

        bool InBorderRelExact2(int left, int straight)
        {
            int x0 = x2 + left * lx2 + straight * sx2;
            int y0 = y2 + left * ly2 + straight * sy2;
            return InBorderExact(x0, y0);
        }

        public bool InBorderExact(int x, int y) // strict mode
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

        public bool InTakenRel2(int left, int straight)
        {
            int x = x2 + left * lx2 + straight * sx2;
            int y = y2 + left * ly2 + straight * sy2;

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
            int x0 = x + left * lx + straight * sx;
            int y0 = y + left * ly + straight * sy;
            if (x0 == size && y0 == size) return true;
            return false;
        }

        public bool InCornerRel2(int left, int straight)
        {
            int x0 = x2 + left * lx2 + straight * sx2;
            int y0 = y2 + left * ly2 + straight * sy2;
            if (x0 == size && y0 == size) return true;
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

        public int InTakenIndexRel2(int left, int straight) // relative position
        {
            int x = x2 + left * lx2 + straight * sx2;
            int y = y2 + left * ly2 + straight * sy2;
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

        // -----  functions end -----

        void T(params object[] o)
		{
            string result = "";
            if (o.Length > 0)
            {
                result += o[0];
            }

            for (int i = 1; i < o.Length; i++)
            {
                result += ", " + o[i];
            }
            Trace.WriteLine(result);
            MainWindow.logger.LogDebug("----------------------------- " + result);
        }

        string ShowForbidden()
        {
            string s = "";
            foreach (int[] field in forbidden)
            {
                s += field[0] + "," + field[1] + "; ";
            }
            if (s.Length > 2)
            {
                return s.Substring(0, s.Length - 2);
            }
            else
            {
                return "";
            }            
        }

        List<int[]> Copy(List<int[]> obj)
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
