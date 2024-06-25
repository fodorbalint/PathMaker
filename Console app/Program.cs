using System.Diagnostics;

string loadFile;
int size = 0;
List<int[]> taken;
List<int[]> possibleDirections;
List<int[]> possible = new(); //field coordinates
List<int[]> forbidden = new();
int x = 0, y = 0;
List<int[]> directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } }; //down, right, up, left
int nextDirection = -1;
int lastDirection = -1;
long completedCount = 0;
long fileCompletedCount;
bool lineFinished = false;
long startTimerValue = 0;
long lastTimerValue = 0;
string errorString = "";
string savePath = "";
int saveFrequency;
bool makeStats;
Random rand = new Random();
int numberOfRuns = 0;
long numberOfCompleted = 0;
int statsRuns = 0;
int count = 0;
int sx = 0; //straight, left and right coordinates
int sy = 0;
int lx = 0;
int ly = 0;
int rx = 0;
int ry = 0;
int thisSx = 0; // remain constant in one step, while the above variables change for the InTakenRel calls.
int thisSy = 0;
int thisLx = 0;
int thisLy = 0;
int[] straightField;
int[] leftField;
int[] rightField;
bool CShape = false;
bool closeStraightSmall, closeMidAcrossSmall, closeAcrossSmall, closeStraightLarge, closeMidAcrossLarge, closeAcrossLarge = false;
List<object> info = new();

int Straight3I = -1; // used for checking Down Stair and Double Area first case rotated at the next step.
int Straight3J = -1;

bool DirectionalArea, DoubleArea1, DoubleArea2, DoubleArea3, DoubleArea4, DoubleArea1Rotated, Sequence1, Sequence2, Sequence3, DownStairClose, DownStair = false;
bool DoubleAreaFirstCaseRotatedNext, DownStairNext = false;

int[] newExitField = new int[] { 0, 0 };
bool newDirectionRotated = false; // if rotated, it is CW on left side

List<string> activeRules;
List<List<int[]>> activeRulesForbiddenFields;
List<int[]> activeRuleSizes;

int nextStepEnterLeft = -1;
int nextStepEnterRight = -1;

string baseDir = AppDomain.CurrentDomain.BaseDirectory;

if (File.Exists(baseDir + "settings.txt"))
{
    string[] lines = File.ReadAllLines(baseDir + "settings.txt");
    string[] arr = lines[0].Split(": ");
    size = int.Parse(arr[1]);
    arr = lines[1].Split(": ");
    saveFrequency = int.Parse(arr[1]);
    arr = lines[2].Split(": ");
    makeStats = bool.Parse(arr[1]);
    arr = lines[3].Split(": ");
    statsRuns = int.Parse(arr[1]);
}
else
{
    size = 9;
    saveFrequency = 1000000;
    makeStats = false;
    statsRuns = 100;
    string[] lines = new string[] { "size: " + size, "saveFrequency: " + saveFrequency, "makeStats: " + makeStats, "statsRuns: " + statsRuns };
    File.WriteAllLines(baseDir + "settings.txt", lines);
}

T("Size setting: " + size, "save frequency: " + saveFrequency, "make stats: " + makeStats);

ReadDir();

if (loadFile != "" && !makeStats)
{
    LoadFromFile();
}
else
{
    InitializeList();
}
count = taken.Count;

bool errorInWalkthrough = false;
bool criticalError = false;

if (taken != null && possibleDirections.Count == count) //null checking is only needed for removing warning
{
    if (!lineFinished)
    {
        NextStepPossibilities();

        if (errorInWalkthrough)
        {
            Console.Write("Error: " + errorString);
            Console.Read();
            return;
        }
    }
    else
    {
        possibleDirections.Add(new int[] { });
    }
}
else if (taken != null && possibleDirections.Count != count + 1)
{
    T("Error in file.");
    Console.Read();
    return;
}

bool completedWalkthrough = false;
bool halfwayWalkthrough = false;

if (makeStats)
{
    numberOfRuns = 0;
    numberOfCompleted = 0;
    completedCount = 0;
    File.WriteAllText(baseDir + "log_stats.txt", "");
    File.WriteAllText(baseDir + "log_rules.txt", "");    
}
else
{
    if (fileCompletedCount == 0 || !File.Exists(baseDir + "log_performance.txt"))
    {
        File.WriteAllText(baseDir + "log_performance.txt", "");
        File.WriteAllText(baseDir + "log_rules.txt", "");
        startTimerValue = 0;
    }
    else // continue where we left off
    {
        List<string> arr = File.ReadAllLines(baseDir + "log_performance.txt").ToList();
        List<string> newArr = new();

        foreach (string line in arr)
        {
            string[] parts = line.Split(" ");
            if (long.Parse(parts[0]) <= fileCompletedCount)
            {
                newArr.Add(line);
                startTimerValue = (long)(float.Parse(parts[1]));
            }
            else
            {
                break;
            }
        }

        if (newArr.Count > 0)
        {
            File.WriteAllLines(baseDir + "log_performance.txt", newArr);
        }
    }
    completedCount = fileCompletedCount;
    lastTimerValue = startTimerValue;
}

Stopwatch watch = Stopwatch.StartNew();

DoThread();

void DoThread()
{
    do
    {
        NextClick();
    }
    while (!completedWalkthrough && !halfwayWalkthrough && !errorInWalkthrough);

    if (completedWalkthrough)
    {
        Console.Write("\rThe number of walkthroughs are " + completedCount + ".");
        Console.Read();
    }
    else if (halfwayWalkthrough)
    {
        Console.WriteLine("\r" + completedCount + " walkthroughs are completed halfway.");
        halfwayWalkthrough = false;
        DoThread();
    }
    else
    {
        if (makeStats && !criticalError)
        {
            numberOfRuns++;
            numberOfCompleted += completedCount;
            errorInWalkthrough = false;
            Console.Write("\rIn " + numberOfRuns + " runs, average " + Math.Round((float)numberOfCompleted / numberOfRuns, 1) + " per run.                  ");
            Log("Current run: " + completedCount + ", " + numberOfCompleted + " in " + numberOfRuns + " runs. " + Math.Round((float)numberOfCompleted / numberOfRuns, 1) + " per run. " + errorString);

            if (numberOfRuns < statsRuns)
            {
                InitializeList();
                completedCount = 0;
                DoThread();
            } 
            else
            {
                // Save last error for further study
                SavePath();
                Console.Read();
            }
        }
        else
        {
            Console.Write("\r\nError at " + completedCount + ": " + errorString);
            SavePath(false, true);
            Console.Read();
        }
    }
}

void NextClick()
{
    if (x == size && y == size)
    {
        if (makeStats)
        {
            InitializeList();
        }
        else
        {
            // step back until there is an option to move right of the step that had been taken.

            bool rightFound = false;
            nextDirection = -1;
            bool oppositeFound;
            bool leftFound;

            do
            {
                PreviousStep();

                int[] prevField;
                if (count > 2)
                {
                    prevField = taken[count - 3];
                }
                else
                {
                    prevField = new int[] { 0, 1 };
                }

                int[] startField = taken[count - 2];
                int[] newField = taken[count - 1];
                int prevX = prevField[0];
                int prevY = prevField[1];
                int startX = startField[0];
                int startY = startField[1];
                int newX = newField[0];
                int newY = newField[1];

                int firstDir = FindDirection(startX - prevX, startY - prevY);
                int secondDir = FindDirection(newX - startX, newY - startY);

                oppositeFound = false;

                foreach (int direction in possibleDirections[count - 1]) // last but one element of possible directions
                {
                    if (direction != secondDir && direction % 2 == secondDir % 2) // two difference
                    {
                        // opposite is found, but we don't know yet if it is on the left or right side. First example 19802714.

                        if (secondDir == firstDir + 1 || firstDir == 3 && secondDir == 0)
                        { // line turned to left. The opposite direction is on the right side
                            oppositeFound = true;
                        }
                        // else line turned to right.
                    }
                    if (direction == secondDir - 1 || secondDir == 0 && direction == 3)
                    {
                        rightFound = true;
                        break;
                    }
                }

                if (rightFound) // right and maybe opposite directions exist
                {
                    nextDirection = secondDir == 0 ? 3 : secondDir - 1;
                    PreviousStep();
                }
                else if (oppositeFound) // only opposite direction
                {
                    nextDirection = secondDir < 2 ? secondDir + 2 : secondDir - 2;
                    rightFound = true;
                    PreviousStep();
                }

            } while (!rightFound && count > 2);

            if (!rightFound)
            {
                PreviousStep(); // c = 2. We reached the end, step back to the start position
                                // Reset nextDirection, so that we can start again
                completedWalkthrough = true;
                nextDirection = -1;
            }
            else if (count == 1)
            {
                halfwayWalkthrough = true;
            }
        }

        return;
    }

    if (NextStep())
    {
        if (x == size && y == size)
        {
            if (count != size * size)
            {
                possibleDirections.Add(new int[] { });

                errorInWalkthrough = true;
                errorString = "The number of steps were only " + count + ".";
                criticalError = true;
                
            }
            else
            {
                possibleDirections.Add(new int[] { });
                lineFinished = true;
                completedCount++;

                if (makeStats)
                {
                    Console.Write("\rIn " + numberOfRuns + " runs, average " + Math.Round((float)numberOfCompleted / (numberOfRuns > 0 ? numberOfRuns : 1), 1) + " per run. Current run: " + completedCount + "      ");
                }
                else
                {
                    if(completedCount % 1000 == 0)
                Console.Write("\r{0} completed.", completedCount);

                    if (completedCount % saveFrequency == 0)
                    {
                        SavePath();

                        long elapsed = watch.ElapsedMilliseconds + startTimerValue;
                        long periodValue = elapsed - lastTimerValue;
                        File.AppendAllText(baseDir + "log_performance.txt", completedCount + " " + (elapsed - elapsed % 1000) / 1000 + "." + elapsed % 1000 + " " + (periodValue - periodValue % 1000) / 1000 + "." + periodValue % 1000 + "\n");

                        lastTimerValue = elapsed;
                    }
                }
            }

            return;
        }

        NextStepPossibilities();
    }
}

bool NextStep()
{
    // L("NextStep", x, y);
    if (possible.Count == 0)
    {
        errorInWalkthrough = true;
        return false;
    }

    int[] newField = new int[] { };

    if (nextDirection != -1) // found direction after stepping back repeatedly on completion
    {
        newField = new int[] { x + directions[nextDirection][0], y + directions[nextDirection][1] };
        lastDirection = nextDirection;
        nextDirection = -1;
    }
    else
    {
        if (makeStats)
        {
            newField = possible[rand.Next(0, possible.Count)];
        }
        else // Find the most left field. It is possible to have the left and right field but not straight.
        {
            int[] newDirections = possibleDirections[possibleDirections.Count - 1];

            bool foundLeft = false;
            bool foundStraight = false;
            int i = 0;
            for (i = 0; i < newDirections.Length; i++)
            {
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

            int newDirectionTemp = -1;
            if (foundLeft)
            {
                newDirectionTemp = newDirections[i];
                newField = new int[] { x + directions[newDirectionTemp][0], y + directions[newDirectionTemp][1] };
            }
            else if (foundStraight)
            {
                newDirectionTemp = lastDirection;
                newField = new int[] { x + directions[newDirectionTemp][0], y + directions[newDirectionTemp][1] };
            }
            else //only right is possible
            {
                newDirectionTemp = newDirections[0];
                newField = new int[] { x + directions[newDirectionTemp][0], y + directions[newDirectionTemp][1] };
            }
            lastDirection = newDirectionTemp;
        }
    }

    x = newField[0];
    y = newField[1];
    taken.Add(new int[] { x, y });
    count = taken.Count;

    return true;
}

void NextStepPossibilities()
{
    try
    {
        NextStepPossibilities2();

        List<int> possibleFields = new List<int>();
        List<int[]> newPossible = new List<int[]>();

        if (errorInWalkthrough) // countarea errors
        {
            possible = newPossible;
            possibleDirections.Add(possibleFields.ToArray());

            return;
        }

        foreach (int[] field in possible)
        {
            int fx = field[0];
            int fy = field[1];

            newPossible.Add(field);

            for (int i = 0; i < 4; i++)
            {
                //last movement: down, right, up, left
                int dx = directions[i][0];
                int dy = directions[i][1];

                if (fx - x == dx && fy - y == dy)
                {
                    possibleFields.Add(i);
                }
            }
        }

        possible = newPossible;
        possibleDirections.Add(possibleFields.ToArray()); //array containing possible fields for all steps

        if (possible.Count == 0)
        {
            errorInWalkthrough = true;
            errorString = "No option to move.";
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
    catch (Exception ex)
    {
        errorInWalkthrough = true;
        errorString = ex.Message + " " + ex.StackTrace;
        criticalError = true;
    }
}

void NextStepPossibilities2()
{
    try
    {
        //L("NextStepPossibilities2", x, y);
        possible = new List<int[]>();
        forbidden = new List<int[]>();
        List<int[]> newPossible;

        if (count < 2)
        {
            possible.Add(new int[] { 2, 1 });
            possible.Add(new int[] { 1, 2 });
        }
        else
        {
            int x0 = taken[count - 2][0];
            int y0 = taken[count - 2][1];

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

                    if (possible.Count == 1) break;

                    activeRules = new();
                    activeRulesForbiddenFields = new();
                    activeRuleSizes = new();

                    Straight3I = -1;
                    Straight3J = -1;
                    DirectionalArea = DoubleArea1 = DoubleArea2 = DoubleArea3 = DoubleArea4 = DoubleArea1Rotated = Sequence1 = Sequence2 = Sequence3 = DownStairClose = DownStair = false;
                    DoubleAreaFirstCaseRotatedNext = DownStairNext = false;
                    nextStepEnterLeft = -1;
                    nextStepEnterRight = -1;

                    // needs to be checked before AreaUp, it can overwrite it as in 802973
                    // L("CheckCShapeNext " + x + " " + y);
                    CheckCShapeNext();
                    // L("CheckStraight " + ShowForbidden());
                    CheckStraight();
                    // L("CheckLeftRightAreaUp " + ShowForbidden());
                    CheckLeftRightAreaUp();
                    // L("CheckLeftRightAreaUpBig " + ShowForbidden());
                    CheckLeftRightAreaUpBig();
                    // L("CheckLeftRightCorner " + ShowForbidden());
                    CheckLeftRightCorner();

                    // L("NextStepEnter " + nextStepEnterLeft + " " + nextStepEnterRight);

                    // 0611_4, 0611_5, 0611_6, 234212, 522267
                    // 0 and 0 or 1 and 3. Beware of 1 and -1.
                    // Overwrite order: 3, 0, 1 (See 802973 and 2020799)
                    if (nextStepEnterLeft == 0 && nextStepEnterRight == 0 || nextStepEnterLeft + nextStepEnterRight == 4 && Math.Abs(nextStepEnterLeft - nextStepEnterRight) == 2)
                    {
                        switch (nextStepEnterLeft)
                        {
                            case 0:
                                // L("Next step double area, cannot step straight");
                                forbidden.Add(new int[] { x + sx, y + sy });
                                break;
                            case 1:
                                // L("Next step double area, cannot step right");
                                forbidden.Add(new int[] { x - lx, y - ly });
                                break;
                            case 3:
                                // L("Next step double area, cannot step left");
                                forbidden.Add(new int[] { x + lx, y + ly });
                                break;
                        }
                    }

                    // L("CheckLeftRightAreaUpExtended " + ShowForbidden());
                    CheckLeftRightAreaUpExtended(); // #1 close obstacle is at the end of the area, outside.
                    // L("CheckStraightNext " + ShowForbidden());
                    CheckStraightNext(); // #2 close obstacle is at the start of the area, inside.
                    // L("CheckStraightSmall " + ShowForbidden());
                    CheckStraightSmall(); // #3 close obstacle is at the start and end of the area, inside. 4 distance only.
                    // L("CheckLeftRightAreaUpBigExtended " + ShowForbidden());
                    CheckLeftRightAreaUpBigExtended(); // #4 when entering at the first white field, we have to step down to the first black and then left to enter as in 0624

                    List<int[]> startForbiddenFields = Copy(forbidden);

                    // If distance is over 3, single area rules seem to disable the needed directions. For 3 distance, we use Sequence first case.

                    // L("CheckSequence " + ShowForbidden());
                    CheckSequence();
                    // L("CheckDownStair " + ShowForbidden());
                    CheckDownStair();
                    // L("Check3DistNextStep " + ShowForbidden());
                    Check3DistNextStep();

                    ShowActiveRules(activeRules, activeRulesForbiddenFields, startForbiddenFields, activeRuleSizes);

                    break;
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
        }
    }
    catch (Exception ex)
    {
        errorInWalkthrough = true;
        errorString = ex.Message + " " + ex.StackTrace;
        criticalError = true;
    }
}

void PreviousStep()
{
    if (count < 2) return;

    int removeX = taken[count - 1][0];
    int removeY = taken[count - 1][1];

    errorInWalkthrough = false;
    lineFinished = false;

    x = taken[count - 2][0];
    y = taken[count - 2][1];

    taken.RemoveAt(count - 1);
    possibleDirections.RemoveAt(count);

    count = taken.Count;

    possible = new List<int[]>();
    List<int> dirs = possibleDirections[possibleDirections.Count - 1].ToList<int>();

    foreach (int dir in dirs)
    {
        int newX = x + directions[dir][0];
        int newY = y + directions[dir][1];

        possible.Add(new int[] { newX, newY });
    }
}

void SavePath(bool isCompleted = true, bool inRoot = false) // used in fast run mode
{
    int startX = 1;
    int startY = 1;
    string completedPathCode = "";
    int lastDrawnDirection = 0;
    savePath = size + "|1-" + startX + "," + startY + ";";


    for (int i = 1; i < count; i++)
    {
        int[] field = taken[i];
        int newX = field[0];
        int newY = field[1];

        foreach (int direction in possibleDirections[i])
        {
            savePath += direction + ",";
        }
        savePath = savePath.Substring(0, savePath.Length - 1);
        savePath += "-" + newX + "," + newY + ";";

        if (isCompleted)
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

        startX = newX;
        startY = newY;
    }

    if (possibleDirections.Count > count)
    {
        foreach (int direction in possibleDirections[possibleDirections.Count - 1])
        {
            savePath += direction + ",";
        }
    }
    savePath = savePath.Substring(0, savePath.Length - 1);

    ReadDir();

    if (isCompleted)
    {
        File.WriteAllText(baseDir + "completed/" + completedCount + "_" + completedPathCode + ".txt", savePath);
    }
    else
    {
        string path = "";
        if (!inRoot)
        {
            path = "incomplete/";
        }
        if (File.Exists(baseDir + path + completedCount + ".txt"))
        {
            int i = 1;
            while (File.Exists(baseDir + path + completedCount + "_" + i + ".txt"))
            {
                i++;
            }
            File.WriteAllText(baseDir + path + completedCount + "_" + i + ".txt", savePath);
        }
        else
        {
            File.WriteAllText(baseDir + path + completedCount + ".txt", savePath);
        }
    }
}

void ReadDir()
{
    loadFile = "";
    string[] files = Directory.GetFiles(baseDir, "*.txt");
    foreach (string file in files)
    {
        string fileName = System.IO.Path.GetFileName(file);
        if (fileName != "settings.txt" && fileName != "log.txt" && fileName != "log_rules.txt" && fileName != "log_performance.txt" && fileName != "completedPaths.txt" && fileName.IndexOf("_temp") == -1 && fileName.IndexOf("_error") == -1)
        {
            loadFile = fileName;
            return;
        }
    }
}

void LoadFromFile()
{
    string content = File.ReadAllText(baseDir + loadFile);
    string[] loadPath;

    if (content.IndexOf("|") != -1)
    {
        string[] arr = content.Split("|");
        size = int.Parse(arr[0]);
        CheckSize();
        content = arr[1];
    }

    loadPath = content.Split(";");

    taken = new();
    possibleDirections = new();

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
                taken.Add(field);
                x = field[0];
                y = field[1];
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
            taken.Add(field);
            x = field[0];
            y = field[1];

            possibleDirections.Add(new int[] { FindDirection(x - startX, y - startY) });
            startX = x;
            startY = y;
        }
        possibleDirections.Add(new int[] { });
    }

    T("Loading", loadFile, "taken count: " + taken.Count, "possible count: " + possibleDirections.Count);

    nextDirection = -1;

    if (taken.Count > 1)
    {
        int[] prevField = taken[taken.Count - 2];
        int prevX = prevField[0];
        int prevY = prevField[1];
        for (int i = 0; i < 4; i++)
        {
            //last movement: down, right, up, left
            int dx = directions[i][0];
            int dy = directions[i][1];

            if (x - prevX == dx && y - prevY == dy)
            {
                lastDirection = i;
            }
        }
    }
    else lastDirection = 0;

    if (loadFile.IndexOf("_") > 0)
    {
        string[] arr = loadFile.Split("_");
        fileCompletedCount = long.Parse(arr[0]);
    }
    else
    {
        fileCompletedCount = 0;
    }

    if (x == size && y == size)
    {
        lineFinished = true;
    }
    else
    {
        lineFinished = false;

        possible = new();
        foreach (int direction in possibleDirections[possibleDirections.Count - 1])
        {
            possible.Add(new int[] { x + directions[direction][0], y + directions[direction][1] });
        }
    }
}

void InitializeList()
{
    taken = new List<int[]> { new int[] { 1, 1 } };
    x = 1;
    y = 1;
    possibleDirections = new List<int[]> { new int[] { 1 }, new int[] { 0, 1 } };
    nextDirection = -1;
    lastDirection = 0;
    fileCompletedCount = 0;

    possible = new();
    foreach (int direction in possibleDirections[possibleDirections.Count - 1])
    {
        possible.Add(new int[] { x + directions[direction][0], y + directions[direction][1] });
    }

    lineFinished = false;
}

int FindDirection(int xDiff, int yDiff)
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

void CheckSize()
{
    if (size > 99)
    {
        T("Size should be between 3 and 99.");
        size = 99;
    }
    else if (size < 3)
    {
        T("Size should be between 3 and 99.");
        size = 3;
    }
    else if (size % 2 == 0)
    {
        T("Size cannot be pair.");
        size = size - 1;
    }
}

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
    Console.WriteLine(result);
}

void L(params object[] o)
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
    Console.WriteLine(result);
    //Debug.WriteLine(result);
}

void Log(string line)
{
    File.AppendAllText(baseDir + "log_stats.txt", line + "\n");
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

void ShowActiveRules(List<string> rules, List<List<int[]>> forbiddenFields, List<int[]> startForbiddenFields, List<int[]> sizes)
{
    if (rules.Count == 0) return;

    int i;

    // only record new rule when its forbidden fields were not created by other rules. More complicated rules can be true together with simpler rules that already added the necessary forbidden fields, like in 349170
    int ruleNo = 0;

    if (File.Exists(baseDir + "log_rules.txt"))
    {
        List<string> arr = File.ReadAllLines(baseDir + "log_rules.txt").ToList();
        int startCount = arr.Count;

        foreach (string rule in rules)
        {
            bool found = false;

            foreach (string line in arr)
            {
                string[] split = line.Split(": ");
                if (split[1] == rule) found = true;
            }

            if (!found)
            {
                List<int[]> newRuleForbiddenFields = forbiddenFields[ruleNo];

                // only remove the same fields as what is contained in startforbiddenFields.
                // other rules can be true simultaneously.

                foreach (int[] field in startForbiddenFields)
                {
                    for (int j = newRuleForbiddenFields.Count - 1; j >= 0; j--)
                    {
                        int[] newField = newRuleForbiddenFields[j];
                        if (field[0] == newField[0] && field[1] == newField[1])
                        {
                            newRuleForbiddenFields.RemoveAt(j);
                        }
                    }
                }

                // the unique forbidden field(s) has to be empty
                if (newRuleForbiddenFields.Count != 0)
                {
                    foreach (int[] field in newRuleForbiddenFields)
                    {
                        if (!InTaken(field[0], field[1]))
                        {
                            arr.Add((numberOfCompleted + completedCount) + ": " + rule);
                            // if two different positions of the same path number are saved, there will be appended _1, _2 etc.
                            SavePath(false);
                            break;
                        }
                    }
                }
            }

            ruleNo++;
        }

        if (arr.Count > startCount) File.WriteAllLines(baseDir + "log_rules.txt", arr);
    }
    else
    {
        List<string> arr = new();
        foreach (string rule in rules)
        {
            List<int[]> newRuleForbiddenFields = forbiddenFields[ruleNo]; // does not copy, it is a reference assignment

            foreach (int[] field in startForbiddenFields)
            {
                for (int j = newRuleForbiddenFields.Count - 1; j >= 0; j--)
                {
                    int[] newField = newRuleForbiddenFields[j];
                    if (field[0] == newField[0] && field[1] == newField[1])
                    {
                        newRuleForbiddenFields.RemoveAt(j);
                    }
                }
            }

            if (newRuleForbiddenFields.Count != 0)
            {
                foreach (int[] field in newRuleForbiddenFields)
                {
                    if (!InTaken(field[0], field[1]))
                    {
                        arr.Add((numberOfCompleted + completedCount) + ": " + rule);
                        SavePath(false);
                        break;
                    }
                }
            }

            ruleNo++;
        }
        File.WriteAllLines(baseDir + "log_rules.txt", arr);
    }
}

/* ----- Rules ----- */

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
                        else
                        {
                            Straight3I = i;
                            if (j == 0)
                            {
                                Straight3J = 0;
                            }
                            else if (j == 2)
                            {
                                Straight3J = 1;
                            }
                        }

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

                            if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount))
                            {
                                forbidden.Add(new int[] { x + sx, y + sy });
                            }
                            if (!(whiteDiff <= nowWCountLeft && whiteDiff >= -nowBCountLeft))
                            {
                                if (j != 1) // for left rotation, lx, ly is the down field
                                {
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }
                                if (j == 2)
                                {
                                    forbidden.Add(new int[] { x - sx, y - sy });
                                }
                            }
                            if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                            {
                                forbidden.Add(new int[] { x - lx, y - ly });
                                if (j == 1)
                                {
                                    forbidden.Add(new int[] { x - sx, y - sy });
                                }
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

// 0619_1
// Two columns are checked for being empty, but at the end the straight field must be taken, and the left field must be empty.
void CheckStraightSmall()
{
    for (int i = 0; i < 2; i++)
    {
        bool circleDirectionLeft = (i == 0) ? true : false;

        for (int j = 0; j < 2; j++) // j = 1: small area, j = 2: big area
        {
            bool circleValid = false;
            int dist = 1;
            List<int[]> borderFields = new();

            while (!InTakenRel(0, dist) && !InBorderRel(0, dist) && !InTakenRel(1, dist) && !InBorderRel(1, dist))
            {
                dist++;
            }

            int ex = dist - 1;

            if (ex == 4 && InTakenRel(0, dist) && !InTakenRel(1, dist))
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

                if (CountAreaRel(1, 1, 1, ex, borderFields, circleDirectionLeft, 2, true))
                {
                    int black = (int)info[1];
                    int white = (int)info[2];

                    if (white == black + 1)
                    {
                        if (CheckNearFieldSmallRel(1, 2, 0, 1, false) && CheckNearFieldSmallRel(1, 4, 1, 2, false))
                        {
                            forbidden.Add(new int[] { x + sx, y + sy });
                            forbidden.Add(new int[] { x - lx, y - ly });
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

// 0618, 0619: When we enter the area, we need to step up. There is a close obstacle the other way inside the area.
void CheckStraightNext()
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

                if (CountAreaRel(1, 1, 1, ex, borderFields, circleDirectionLeft, 2, true))
                {
                    int black = (int)info[1];
                    int white = (int)info[2];

                    int whiteDiff = white - black;

                    switch (ex % 4)
                    {
                        case 1:
                            if (whiteDiff == (ex - 1) / 4 && CheckNearFieldSmallRel(1, 2, 0, 1, false))
                            {
                                forbidden.Add(new int[] { x + sx, y + sy });
                                if (j != 2) // the right field relative to the area (left of the main line) is now inside the area.
                                {
                                    forbidden.Add(new int[] { x - lx, y - ly });
                                }
                            }
                            break;
                        case 3:
                            if (whiteDiff == (ex + 1) / 4 && CheckNearFieldSmallRel(1, 0, 0, 1, false))
                            {
                                forbidden.Add(new int[] { x + lx, y + ly });
                            }
                            break;
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

                        if (CountAreaRel(1, 1, 1, ex, borderFields, circleDirectionLeft, 2, true))
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

                            if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount))
                            {
                                forbidden.Add(new int[] { x + lx, y + ly });
                            }
                            if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                            {
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

void CheckLeftRightAreaUpExtended() // used for double area. As 0618_2 shows, 1,1 can be taken.
{
    for (int i = 0; i < 2; i++)
    {
        bool circleDirectionLeft = (i == 0) ? true : false;

        for (int j = 0; j < 4; j++) // j = 1: small area, j = 2: big area
        {
            if (j != 2)
            {
                bool circleValid = false;

                int dist = 1;
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

                    if (CountAreaRel(0, 1, 0, ex, borderFields, circleDirectionLeft, 3, true))
                    {
                        int black = (int)info[1];
                        int white = (int)info[2];

                        int whiteDiff = white - black;

                        switch (ex % 4)
                        {
                            case 0:
                                // 0610_4, 0610_5, 121670752
                                if (-whiteDiff == ex / 4 && CheckNearFieldSmallRel(0, ex - 1, 0, 2, true))
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                }
                                // 0618_2
                                else if (whiteDiff == ex / 4 && CheckNearFieldSmallRel(0, ex, 0, 2, true))
                                {
                                    forbidden.Add(new int[] { x - lx, y - ly });
                                }
                                break;
                            case 1:
                                break;
                            case 2:
                                // We cannot get to the 2- or 6-distance case if the other rules are applied. 0611_1
                                /*if (!found && whiteDiff == (ex + 2) / 4 && CheckNearFieldSmallRel(0, ex, 0, 2, true))
                                {
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }*/
                                break;
                            case 3:
                                if (whiteDiff == (ex + 1) / 4 + 1 && CheckNearFieldSmallRel(0, ex - 1, 0, 2, true))
                                {
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }
                                // 0611
                                if (-whiteDiff == (ex + 1) / 4 - 1 && CheckNearFieldSmallRel(0, ex, 0, 2, true))
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                }
                                break;
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

                        if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount)) // not in range
                        {
                            forbidden.Add(new int[] { x + sx, y + sy });
                        }
                        if (!(whiteDiff <= nowWCountRight && whiteDiff >= -nowBCount)) // not in range
                        {
                            forbidden.Add(new int[] { x - lx, y - ly });
                        }
                        if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                        {
                            forbidden.Add(new int[] { x + lx, y + ly });
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

// Area as in the first area case of documentation. That area is taken care of in UpBig and Striaght. This is about a border movement close obstacle: 0624
void CheckLeftRightAreaUpBigExtended()
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

                    switch (ex % 4)
                    {
                        case 0:
                            nowWCountRight = nowWCount = ex / 4;
                            nowBCount = ex / 4 - 1;
                            laterWCount = ex / 4;
                            laterBCount = ex / 4;

                            if (whiteDiff == laterWCount && CheckNearFieldSmallRel(1, 1, 0, 1, false)) // when entering at the first white field, we have to step down to the first black and then left to enter as in 0624
                            {
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

                int xDiff = x - taken[taken.Count - 2][0];
                int yDiff = y - taken[taken.Count - 2][1];

                // relative directions and coordinates. Since the relative x and y is expanding towards the upper left corner, the current direction is what we use for downwards motion in the absolute coordinate system.
                int currentDirection = 0;

                int nextX = horiStart - 1;
                int nextY = 1;

                // We will not turn left when we reach a 0 horizontal distance position. Instead, AreaUp will be activated.
                // When CheckStraight is turned off, we can be at 5,1, going from down, and then nextX will never be 0. In this case, the second condition is needed.

                int counter = 0;
                while (nextX > 0 && !(counter > 0 && nextX == horiStart - 1 && nextY == 1))
                {
                    counter++;
                    if (counter == size * size)
                    {
                        errorInWalkthrough = true;
                        errorString = "Corner discovery error.";
                        criticalError = true;                        
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
                                    forbidden.Add(new int[] { x - lx, y - ly });

                                    // only one option remains
                                    sx = thisSx;
                                    sy = thisSy;
                                    lx = thisLx;
                                    ly = thisLy;
                                    return;
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
                                        if (InTakenRel(hori - 1 + k, hori - 1))
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

                                    // here, true means that count area succeeds, does not run into an error
                                    if (CountAreaRel(left1, straight1, left2, straight2, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        int whiteDiff = white - black;

                                        // need to be generalized for larger than 1 vertical distance
                                        if (hori == 2)
                                        {
                                            if (vert % 4 == 3) //0610, 0610_1
                                            {
                                                if (-whiteDiff == (vert - 3) / 4 && CheckNearFieldSmallRel(1, 2, 0, 2, true))
                                                {

                                                    if (j != 3) // no small small area, left field is taken
                                                    {
                                                        forbidden.Add(new int[] { x + lx, y + ly });
                                                        if (j == 1) // big area
                                                        {
                                                            forbidden.Add(new int[] { x - sx, y - sy });
                                                        }
                                                    }
                                                }
                                            }
                                            if (vert % 4 == 0)  //0610_2, 0610_3
                                            {
                                                if (-whiteDiff == vert / 4 && CheckNearFieldSmallRel(1, 2, 0, 2, true))
                                                {
                                                    if (j != 3) // no small small area, left field is taken
                                                    {
                                                        forbidden.Add(new int[] { x + lx, y + ly });
                                                        if (j == 0)
                                                        {
                                                            forbidden.Add(new int[] { x - sx, y - sy });
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount)) // not in range
                                        {
                                            if (j != 3)
                                            {
                                                forbidden.Add(new int[] { x + lx, y + ly });
                                            }
                                        }
                                        if (!(whiteDiff <= nowWCountDown && whiteDiff >= -nowBCount)) // not in range
                                        {
                                            if (j != 3)
                                            {
                                                forbidden.Add(new int[] { x - sx, y - sy });
                                            }
                                        }
                                        if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                        {
                                            forbidden.Add(new int[] { x + sx, y + sy });
                                            // for small area
                                            if (j == 0)
                                            {
                                                forbidden.Add(new int[] { x - lx, y - ly });
                                            }
                                        }
                                        else if (j != 2) // We can enter later, but if we step straight, we have to enter afterwards. Check for pattern on the opposite side (if the obstacle is up on the left, we check the straight field for next step C, not the right field.) 
                                                         // When j = 2, the enter later field is the field behind.

                                        {
                                            // 0611_6
                                            // If we can enter later at the hori 2, vert 3 case, the area must be W = B
                                            if ((hori == 2 && vert == 3) ||
                                                (hori == 2 && vert == 4 && -whiteDiff == 1))
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

bool Check3DoubleAreaRotated(int side = -1) // Take only the first case and rotate it.
{
    for (int i = 0; i < 2; i++)
    {
        if (side != -1 && side != i) continue;

        bool circleValid = false;
        bool circleDirectionLeft = (i == 0) ? true : false;
        int sx = 0, sy = 0, ex = 0, ey = 0;

        List<int[]> borderFields = new();

        if (InTakenRel(4, -1) && !InTakenRel(2, 0) && !InTakenRel(3, 0) && !InTakenRel(4, 0) && !InTakenRel(1, -1) && !InTakenRel(3, -1))
        {
            int i1 = InTakenIndexRel(4, -1);
            int i2 = InTakenIndexRel(4, -2);

            if (i2 > i1)
            {
                circleValid = true;

                sx = 1;
                sy = 0;
                ex = 3;
                ey = 0;
                borderFields.Add(new int[] { 2, 0 });
            }
        }

        if (circleValid && CountAreaRel(sx, sy, ex, ey, borderFields, circleDirectionLeft, 2, true))
        {
            int black = (int)info[1];
            int white = (int)info[2];

            if (black == white)
            {
                int thisX = x;
                int thisY = y;

                x = x + ex * lx + ey * thisSx;
                y = y + ex * ly + ey * thisSy;

                // Checking C-Shape not necessary, side straight will take care of it, because area is 1B.
                if (CheckNearFieldSmall1())
                {
                    if (side != -1)
                    {
                        return true; // We are only interested in the side the straight obstacle is going to. Both sides cannot be true at the same time.
                    }

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
                            int thisX = x;
                            int thisY = y;
                            int thisSx = sx;
                            int thisSy = sy;
                            int thisLx = lx;
                            int thisLy = ly;

                            //List<int[]> thisPath = Copy(path);
                            // necessary for checking C-shape on the left side
                            //path.Add(new int[] { x + 3 * sx, y + 3 * sy });

                            // step after exiting area:
                            x = x - lx + 2 * sx;
                            y = y - ly + 2 * sy;

                            //only necessary if recursive function will be used: path.Add(new int[] { x, y });

                            int[] rotatedDir = RotateDir(lx, ly, i);
                            lx = rotatedDir[0];
                            ly = rotatedDir[1];
                            rotatedDir = RotateDir(sx, sy, i);
                            sx = rotatedDir[0];
                            sy = rotatedDir[1];

                            // does not use C-shape up, only left
                            bool leftSideClose = CheckNearFieldSmall1_5();

                            lx = -lx;
                            ly = -ly;

                            bool rightSideClose = CheckNearFieldSmall1();

                            lx = -lx;
                            ly = -ly;

                            if (leftSideClose && rightSideClose)
                            {
                                Sequence1 = true;
                                activeRules.Add("Sequence first case");
                                activeRuleSizes.Add(new int[] { 5, 5 });
                                activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + thisSx, thisY + thisSy } });

                                // Due to CheckStraight, stepping left is already disabled when the obstacle is straight ahead. When it is one to the right, we need the left field to be disabled.
                                forbidden.Add(new int[] { thisX + thisLx, thisY + thisLy });
                                forbidden.Add(new int[] { thisX + thisSx, thisY + thisSy });
                            }

                            x = thisX;
                            y = thisY;
                            lx = thisLx;
                            ly = thisLy;
                            sx = thisSx;
                            sy = thisSy;
                            //path = thisPath;
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
            if (InTakenRel(0, 3) && !InTakenRel(0, 1) && !InTakenRel(0, 2) && !InTakenRel(-1, 3))
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
                            int thisX = x;
                            int thisY = y;
                            int thisSx = sx;
                            int thisSy = sy;
                            int thisLx = lx;
                            int thisLy = ly;

                            //List<int[]> thisPath = Copy(path);
                            //path.Add(new int[] { x + sx, y + sy });
                            // step after exiting area:
                            x = x - lx + 2 * sx;
                            y = y - ly + 2 * sy;

                            //path.Add(new int[] { x, y });

                            int[] rotatedDir = RotateDir(lx, ly, i);
                            lx = rotatedDir[0];
                            ly = rotatedDir[1];
                            rotatedDir = RotateDir(sx, sy, i);
                            sx = rotatedDir[0];
                            sy = rotatedDir[1];

                            if (CheckSequenceRecursive(i))
                            {
                                Sequence2 = true;
                                activeRules.Add("Sequence second case");
                                activeRuleSizes.Add(new int[] { 1, 1 });
                                activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + thisLx, thisY + thisLy }, new int[] { thisX + thisSx, thisY + thisSy } });

                                forbidden.Add(new int[] { thisX + thisLx, thisY + thisLy });
                                forbidden.Add(new int[] { thisX + thisSx, thisY + thisSy });
                            }

                            x = thisX;
                            y = thisY;
                            lx = thisLx;
                            ly = thisLy;
                            sx = thisSx;
                            sy = thisSy;
                            //path = thisPath;
                        }
                    }
                }
            }

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

    // Third case, Double Area Stair 2
    // 2024_0516_6
    // Rotated both ways: 2024_0516_7, 2024_0516_8

    for (int i = 0; i < 2; i++)
    {
        bool circleDirectionLeft = (i == 0) ? true : false;

        for (int j = 0; j < 3; j++)
        {
            if (InTakenRel(1, 3) && !InTakenRel(1, 2) && !InTakenRel(0, 3))
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
                            // first circle true

                            int thisX = x;
                            int thisY = y;
                            int thisSx = sx;
                            int thisSy = sy;
                            int thisLx = lx;
                            int thisLy = ly;

                            //List<int[]> thisPath = Copy(path);
                            //path.Add(new int[] { x + sx, y + sy });
                            // step after exiting area:
                            x = x + 2 * sx;
                            y = y + 2 * sy;

                            //path.Add(new int[] { x, y });

                            int[] rotatedDir = RotateDir(lx, ly, i);
                            lx = rotatedDir[0];
                            ly = rotatedDir[1];
                            rotatedDir = RotateDir(sx, sy, i);
                            sx = rotatedDir[0];
                            sy = rotatedDir[1];

                            if (CheckSequenceRecursive(i))
                            {
                                Sequence3 = true;
                                activeRules.Add("Sequence third case");
                                activeRuleSizes.Add(new int[] { 1, 1 });
                                activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + thisSx, thisY + thisSy } });

                                forbidden.Add(new int[] { thisX + thisSx, thisY + thisSy });
                            }

                            x = thisX;
                            y = thisY;
                            lx = thisLx;
                            ly = thisLy;
                            sx = thisSx;
                            sy = thisSy;
                            //path = thisPath;
                        }
                    }
                }
            }

            if (j == 0)
            {
                int l0 = lx;
                int l1 = ly;
                lx = -sx;
                ly = -sy;
                sx = l0;
                sy = l1;
            }
            else if (j == 1)
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

bool CheckSequenceRecursive(int j)
{
    newExitField = new int[] { 0, 0 };

    bool leftSideClose = CheckNearFieldSmall2(true);
    bool rightSideClose = CheckNearFieldSmall2(false);

    if (leftSideClose && rightSideClose)
    {
        return true;
    }
    // right side close can happen with the future line
    // for now, we only take the right side C-shape into account as it happens in 740 293. Other close obstacles we don't check.
    else if (leftSideClose)
    //else if ((leftSideClose || rightSideClose) && newExitField[0] != 0)
    {
        x = newExitField[0];
        y = newExitField[1];
        //path.Add(new int[] { x, y });

        if (newDirectionRotated)
        {
            int[] rotatedDir = RotateDir(lx, ly, j);
            lx = rotatedDir[0];
            ly = rotatedDir[1];
            rotatedDir = RotateDir(sx, sy, j);
            sx = rotatedDir[0];
            sy = rotatedDir[1];
        }

        return CheckSequenceRecursive(j);
    }
    else
    {
        return false;
    }
}

bool CheckDownStair(int side = -1, int nLx = 0, int nLy = 0, int nSx = 0, int nSy = 0)
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

bool CheckNearFieldSmall1() // for use only with Double Area case 1, 2, 3 and 1 rotated, and Down Stair. Across is needed at 53144883
{
    // close mid across. In DirectionalArea, the empty fields are already checked.
    if (InTakenRel(1, 2) && !InTakenRel(0, 2) && !InTakenRel(1, 1))
    {
        int middleIndex = InTakenIndexRel(1, 2);
        int sideIndex = InTakenIndexRel(2, 2);

        if (sideIndex > middleIndex)
        {

            return true;
        }
    }

    // close across
    if (InTakenRel(2, 2) && !InTakenRel(1, 2) && !InTakenRel(2, 1))
    {
        int middleIndex = InTakenIndexRel(2, 2);
        int sideIndex = InTakenIndexRel(3, 2);

        if (sideIndex > middleIndex)
        {
            return true;
        }
    }

    return false;
}

bool CheckNearFieldSmall1_5() // for use only with Double Area case 1, 2, 3 and 1 rotated
{
    // C-shape (left)
    if ((InTakenRel(2, 0) || InBorderRel(2, 0)) && !InTakenRel(1, 0))
    {
        return true;
    }

    // close mid across. In DirectionalArea, the empty fields are already checked.
    if (InTakenRel(1, 2) && !InTakenRel(0, 2) && !InTakenRel(1, 1))
    {
        int middleIndex = InTakenIndexRel(1, 2);
        int sideIndex = InTakenIndexRel(2, 2);

        if (sideIndex > middleIndex)
        {

            return true;
        }
    }

    // close across
    if (InTakenRel(2, 2) && !InTakenRel(1, 2) && !InTakenRel(2, 1))
    {
        int middleIndex = InTakenIndexRel(2, 2);
        int sideIndex = InTakenIndexRel(3, 2);

        if (sideIndex > middleIndex)
        {
            return true;
        }
    }

    return false;
}

bool CheckNearFieldSmall2(bool leftSide = true) // for use with Sequence
{
    bool ret = false;

    if (!leftSide)
    {
        lx = -lx;
        ly = -ly;
    }
    else
    {
        // C-Shape, only left side should have it
        // Checking for InTakenRel(1, -1) is not possible, because in Sequence first case, we are exiting the area at the middle border field.
        // But when it comes to the right side (if it was checked), it is necessary, otherwise we can detect a C-shape with the live end as in 213.
        if (InTakenRel(2, 0) && !InTakenRel(1, 0))
        {
            ret = true;

            newExitField = new int[] { x + lx + sx, y + ly + sy };
            newDirectionRotated = false;
        }

        //C-Shape up
        if (InTakenRel(0, 2) && InTakenRel(1, 1) && !InTakenRel(0, 1))
        {
            ret = true;

            newExitField = new int[] { x - lx + sx, y - ly + sy };
            newDirectionRotated = true;
        }
    }

    // close mid across
    if (InTakenRel(1, 2) && !InTakenRel(0, 2) && !InTakenRel(1, 1))
    {
        int middleIndex = InTakenIndexRel(1, 2);
        int sideIndex = InTakenIndexRel(2, 2);

        if (sideIndex > middleIndex)
        {
            ret = true;

            if (leftSide)
            {
                // mid across overwrites C-shape
                newExitField = new int[] { x + sx, y + sy };
                newDirectionRotated = true;
            }
        }
    }

    // close across. Checking empty fields necessary, see 29558469
    if (InTakenRel(2, 2) && !InTakenRel(1, 2) && !InTakenRel(2, 1))
    {
        int middleIndex = InTakenIndexRel(2, 2);
        int sideIndex = InTakenIndexRel(3, 2);

        if (sideIndex > middleIndex)
        {
            ret = true;

            if (leftSide)
            {
                newExitField = new int[] { x + lx + sx, y + ly + sy };
                newDirectionRotated = true;
            }
        }
    }

    if (!leftSide)
    {
        lx = -lx;
        ly = -ly;
    }

    return ret;
}

void CheckCShapeNext() // 0611_5, 0611_6
{
    for (int i = 0; i < 2; i++)
    {
        bool circleDirectionLeft = (i == 0) ? false : true;

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

// obstacle right side of the field in question, area up
// mid across and across fields
// used for LeftRightAreaUp and LeftRightCorner
bool CheckNearFieldSmallRel(int x, int y, int side, int rotation, bool strictSmall)
{
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
        for (int j = 0; j < 3; j++) // j = 0: middle, j = 1: small area, j = 2: big area
        {
            if (i == side && j == rotation)
            {
                if (InTakenRel(x + lx + 2 * sx, y + ly + 2 * sy) && !InTakenRel(x + 2 * sx, y + 2 * sy) && !InTakenRel(x + lx + sx, y + ly + sy))
                {
                    if (strictSmall)
                    {
                        int i1 = InTakenIndexRel(x + 1 * lx + 2 * sx, y + 1 * ly + 2 * sy);
                        int i2 = InTakenIndexRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy);

                        if (i2 > i1) return true;
                    }
                    else return true;
                }
                if (InTakenRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy) && !InTakenRel(x + lx + 2 * sx, y + ly + 2 * sy) && !InTakenRel(x + 2 * lx + sx, y + 2 * ly + sy))
                {
                    if (strictSmall)
                    {
                        int i1 = InTakenIndexRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy);
                        int i2 = InTakenIndexRel(x + 3 * lx + 2 * sx, y + 3 * ly + 2 * sy);

                        if (i2 > i1) return true;
                    }
                    else return true;
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
        lx = -1;
        ly = 0;
        sx = 0;
        sy = 1;
    }
    return false;
}

int[] RotateDir(int xDiff, int yDiff, int ccw)
{
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

/* ----- Count Area ----- */

bool CountAreaRel(int left1, int straight1, int left2, int straight2, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false)
{
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

bool CountArea(int startX, int startY, int endX, int endY, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false)
// compareColors is for the starting situation of 1119, where we mark an impair area and know the entry and the exit field. We count the number of white and black cells of a checkered pattern, the color of the entry and exit should be one more tchan the other color.
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
            if (debug) L("Adding border " + middleX + " " + middleY);

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
            if (debug) L("Adding border " + field[0] + " " + field[1]);
        }
        xDiff = startX - borderFields[borderFields.Count - 1][0];
        yDiff = startY - borderFields[borderFields.Count - 1][1];
    }

    areaLine.Add(new int[] { startX, startY });
    if (debug) L("Adding start " + startX + " " + startY);

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
    if (debug) L("Adding continued " + nextX + " " + nextY);

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

        currentDirection = i;

        nextX = possibleNextX;
        nextY = possibleNextY;

        // when getting info about area
        if (nextX == size && nextY == size)
        {
            errorInWalkthrough = true;
            errorString = "Corner is reached."; 
            criticalError = true;            
            return false;
        }

        areaLine.Add(new int[] { nextX, nextY });

        if (areaLine.Count == size * size)
        {
            errorInWalkthrough = true;
            errorString = "Area walkthrough error.";
            criticalError = true;            
            return false;
        }

        if (debug) L("Adding " + nextX + " " + nextY + " count " + areaLine.Count);

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

    int area = 0;
    List<int[]> startSquares = new List<int[]>();
    List<int[]> endSquares = new List<int[]>();

    if (areaLine.Count > 2)
    {
        int[] startCandidate = new int[] { limitX, minY };
        int[] endCandidate = new int[] { limitX, minY };

        if (debug2) L("arealine start " + startCandidate[0] + " " + startCandidate[1]);

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
            int x = field[0];
            int y = field[1];

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

                if (debug2) L("field x " + field[0] + " y " + field[1] + " currentY " + currentY + " startCandidate " + startCandidate[0] + " " + startCandidate[1] + " endCandidate " + endCandidate[0] + " " + endCandidate[1]);

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
                        int y = square[1];

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
                        int y = square[1];

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

            errorInWalkthrough = true;
            errorString = "Count of start and end squares are inequal: " + startSquares.Count + " " + eCount;
            criticalError = true;
            
            return false;
        }

        for (int i = 0; i < eCount; i++)
        {
            area += endSquares[i][0] - startSquares[i][0] + 1;
        }
    }
    else area = areaLine.Count;

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

/* ----- Field checking ----- */

bool InBorderAbs(int[] field)
{
    int x = field[0];
    int y = field[1];
    return InBorder(x, y);
}

bool InBorderRel(int left, int straight)
{
    int x0 = x + left * lx + straight * sx;
    int y0 = y + left * ly + straight * sy;
    return InBorder(x0, y0);
}

bool InBorder(int x, int y) // allowing negative values could cause an error in AddFutureLines 2x2 checking, but it is necessary in CheckLeftRightCorner due to possibility checking
{
    if (x <= 0 || x >= size + 1 || y <= 0 || y >= size + 1) return true;
    return false;
}

bool InBorderRelExact(int left, int straight)
{
    int x0 = x + left * lx + straight * sx;
    int y0 = y + left * ly + straight * sy;
    return InBorderExact(x0, y0);
}

bool InBorderExact(int x, int y) // strict mode
{
    if (x == 0 || x == size + 1 || y == 0 || y == size + 1) return true;
    return false;
}

bool InTakenAbs(int[] field0)
{
    int x0 = field0[0];
    int y0 = field0[1];

    return InTaken(x0, y0);
}

bool InTakenRel(int left, int straight)
{
    int x0 = x + left * lx + straight * sx;
    int y0 = y + left * ly + straight * sy;

    return InTaken(x0, y0);
}

bool InTaken(int x, int y) //more recent fields are more probable to encounter, so this way processing time is optimized
{
    for (int i = count - 1; i >= 0; i--)
    {
        int[] field = taken[i];
        if (x == field[0] && y == field[1])
        {
            return true;
        }
    }

    return false;
}

bool InCornerRel(int left, int straight)
{
    int x0 = x + left * lx + straight * sx;
    int y0 = y + left * ly + straight * sy;
    if (x0 == size && y0 == size) return true;
    return false;
}

int InBorderIndexRel(int left, int straight)
{
    int x0 = x + left * lx + straight * sx;
    int y0 = y + left * ly + straight * sy;
    return x0 + y0;
}


int InTakenIndexRel(int left, int straight) // relative position
{
    int x0 = x + left * lx + straight * sx;
    int y0 = y + left * ly + straight * sy;
    return InTakenIndex(x0, y0);
}

int InTakenIndex(int x, int y)
{
    for (int i = 0; i < count; i++)
    {
        int[] field = taken[i];
        if (x == field[0] && y == field[1])
        {
            return i;
        }
    }
    return -1;
}

bool InForbidden(int[] value)
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