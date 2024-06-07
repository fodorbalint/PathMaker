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
int completedCount = 0;
int fileCompletedCount;
bool lineFinished = false;
long startTimerValue = 0, lastTimerValue;
string errorString = "";
string savePath = "";
int saveFrequency;
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

if (File.Exists("settings.txt"))
{
    string[] lines = File.ReadAllLines("settings.txt");
    string[] arr = lines[0].Split(": ");
    size = int.Parse(arr[1]);
    arr = lines[1].Split(": ");
    saveFrequency = int.Parse(arr[1]);
}
else
{
    size = 9;
    saveFrequency = 1000000;
    string[] lines = new string[] { "size: " + size, "saveFrequency: " + saveFrequency };
    File.WriteAllLines("settings.txt", lines);
}

T("Size setting: " + size, "save frequency: " + saveFrequency);

ReadDir();

if (loadFile != "")
{
    LoadFromFile();
}
else
{
    InitializeList();
}
count = taken.Count;

bool errorInWalkthrough = false;

if (taken != null && possibleDirections.Count == count) //null checking is only needed for removing warning
{
    if (!lineFinished)
    {
        NextStepPossibilities();
        if (possible.Count == 0)
        {
            T("No option to move.");
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

if (fileCompletedCount == 0 || !File.Exists("log_performance.txt"))
{
    File.WriteAllText("log_performance.txt", "");
    File.WriteAllText("log_rules.txt", "");
    startTimerValue = 0;
}
else // continue where we left off
{
    List<string> arr = File.ReadAllLines("log_performance.txt").ToList();
    List<string> newArr = new();

    foreach (string line in arr)
    {
        string[] parts = line.Split(" ");
        if (int.Parse(parts[0]) <= fileCompletedCount)
        {
            newArr.Add(line);
            startTimerValue = (long)(float.Parse(parts[1]) * 1000);
        }
        else
        {
            break;
        }
    }

    if (newArr.Count > 0)
    {
        File.WriteAllLines("log_performance.txt", newArr);
    }
}

completedCount = fileCompletedCount;
lastTimerValue = startTimerValue;
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
        Console.Write("\rError at " + completedCount + ": " + errorString);
        SavePath(false, true);
        Console.Read();
    }
}

void NextClick()
{
    if (x == size && y == size)
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
            }
            else
            {
                possibleDirections.Add(new int[] { });
                lineFinished = true;
                completedCount++;
                if (completedCount % 1000 == 0)
                Console.Write("\r{0} completed.", completedCount);

                if (completedCount % saveFrequency == 0)
                {
                    SavePath();

                    long elapsed = watch.ElapsedMilliseconds + startTimerValue;
                    long periodValue = elapsed - lastTimerValue;
                    File.AppendAllText("log_performance.txt", completedCount + " " + (elapsed - elapsed % 1000) / 1000 + "." + elapsed % 1000 + " " + (periodValue - periodValue % 1000) / 1000 + "." + periodValue % 1000 + "\n");

                    lastTimerValue = elapsed;
                }
            }

            return;
        }

        NextStepPossibilities();
    }
}

bool NextStep()
{
    //L("NextStep", x, y);
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

    x = newField[0];
    y = newField[1];
    taken.Add(new int[] { x, y });
    count = taken.Count;

    return true;
}

void NextStepPossibilities()
{
    NextStepPossibilities2();

    List<int> possibleFields = new List<int>();
    List<int[]> newPossible = new List<int[]>();

    if (errorInWalkthrough) // countarea errors
    {       
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

void NextStepPossibilities2()
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

                /* ---- uncomment to enable basic rule checking first ---- */

                CShape = false;

                CheckCShape();

                if (CShape) break;

                closeStraightSmall = false;
                closeMidAcrossSmall = false;
                closeAcrossSmall = false;
                closeStraightLarge = false;
                closeMidAcrossLarge = false;

                // needed for far left and right case 234320
                CheckNearField();

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

                if (possible.Count == 1) break;

                /* ---- uncomment to disable advanced rules ---- */
                // break;

                Straight3I = -1;
                Straight3J = -1;
                DirectionalArea = DoubleArea1 = DoubleArea2 = DoubleArea3 = DoubleArea4 = DoubleArea1Rotated = Sequence1 = Sequence2 = Sequence3 = DownStairClose = DownStair = false;
                DoubleAreaFirstCaseRotatedNext = DownStairNext = false;

                //L("CheckStraight", forbidden.Count);
                CheckStraight();
                //L("CheckLeftRightAreaUp", forbidden.Count);
                CheckLeftRightAreaUp();
                //L("CheckLeftRightAreaUpBig", forbidden.Count);
                CheckLeftRightAreaUpBig();
                //L("CheckLeftRightCornerBig", forbidden.Count);
                CheckLeftRightCornerBig();

                //L("CheckLeftRightCornerBig end", forbidden.Count);

                List<int[]> startForbiddenFields = Copy(forbidden);

                /* If distance is over 3, single area rules seem to disable the needed directions. For 3 distance, we use Sequence first case.
                
                //T("CheckDirectionalArea");
                CheckDirectionalArea(); */
                //T("Check3DoubleArea");
                Check3DoubleArea();
                //T("Check3DoubleAreaRotated");
                Check3DoubleAreaRotated();
                //T("CheckSequence");
                CheckSequence();
                //T("CheckDownStair");
                CheckDownStair();
                //T("Check3DistNextStep");
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
        File.WriteAllText("completed/" + completedCount + "_" + completedPathCode + ".txt", savePath);
    }
    else
    {
        string path = "";
        if (!inRoot)
        {
            path = "incomplete/";
        }
        if (File.Exists(path + completedCount + ".txt"))
        {
            int i = 1;
            while (File.Exists(path + completedCount + "_" + i + ".txt"))
            {
                i++;
            }
            File.WriteAllText(path + completedCount + "_" + i + ".txt", savePath);
        }
        else
        {
            File.WriteAllText(path + completedCount + ".txt", savePath);
        }
    }
}

void ReadDir()
{
    loadFile = "";
    string[] files = Directory.GetFiles("./", "*.txt");
    foreach (string file in files)
    {
        string fileName = file.Substring(2);
        if (fileName != "settings.txt" && fileName != "log.txt" && fileName != "log_rules.txt" && fileName != "log_performance.txt" && fileName != "completedPaths.txt" && fileName.IndexOf("_temp") == -1 && fileName.IndexOf("_error") == -1)
        {
            loadFile = fileName;
            return;
        }
    }
}

void LoadFromFile()
{
    string content = File.ReadAllText(loadFile);
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
        fileCompletedCount = int.Parse(arr[0]);
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
    Debug.WriteLine(result);
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

    if (File.Exists("log_rules.txt"))
    {
        List<string> arr = File.ReadAllLines("log_rules.txt").ToList();
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
                            arr.Add(completedCount + ": " + rule);
                            // if two different positions of the same path number are saved, there will be appended _1, _2 etc.
                            SavePath(false);
                            break;
                        }
                    }
                }
            }

            ruleNo++;
        }

        if (arr.Count > startCount) File.WriteAllLines("log_rules.txt", arr);
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
                        arr.Add(completedCount + ": " + rule);
                        SavePath(false);
                        break;
                    }
                }
            }

            ruleNo++;
        }
        File.WriteAllLines("log_rules.txt", arr);
    }
}

/* ----- Rules ----- */

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
                farStraight = true;

                int middleIndex = InTakenIndexRel(0, 3);
                int sideIndex = InTakenIndexRel(1, 3);
                if (sideIndex > middleIndex) // area on left
                {
                    if (!InTakenRel(1, 2) && !InTakenRel(2, 2)) // 1,2: 1019_4, 2,2: 1019_5
                    {
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
                    farMidAcross = true;

                    int middleIndex = InTakenIndexRel(1, 3);
                    int sideIndex = InTakenIndexRel(2, 3);
                    if (sideIndex > middleIndex) // area on left
                    {
                        if (!InTakenRel(2, 2)) // 2, 2: 1019
                        {
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
                        int middleIndex = InTakenIndexRel(2, 3);
                        int sideIndex = InTakenIndexRel(3, 3);
                        if (sideIndex > middleIndex) // area on left
                        {
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

void CheckAreaNearBorder() // 0909. Check both straight approach and side.
{
    if (x == 3 && straightField[0] == 2 && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InTaken(1, y))
    {
        if (CountArea(2, y - 1, 1, y - 1, null, false, 1))
        {
            forbidden.Add(straightField);
            forbidden.Add(leftField);
        }
    }
    else if (y == 3 && straightField[1] == 2 && !InTakenAbs(straightField) && !InTakenAbs(leftField) && !InTaken(x, 1))
    {
        if (CountArea(x - 1, 2, x - 1, 1, null, true, 1))
        {
            forbidden.Add(straightField);
            forbidden.Add(rightField);
        }
    }
    else if (x == size - 2 && y >= 2 && straightField[0] == size - 1 && !InTakenAbs(straightField) && !InTakenAbs(leftField) && !InTaken(size, y))
    {
        if (CountArea(size - 1, y - 1, size, y - 1, null, true, 1))
        {
            forbidden.Add(straightField);
            forbidden.Add(rightField);
        }
    }
    else if (y == size - 2 && x >= 2 && straightField[1] == size - 1 && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InTaken(x, size))
    {
        if (CountArea(x - 1, size - 1, x - 1, size, null, false, 1))
        {
            forbidden.Add(straightField);
            forbidden.Add(leftField);
        }
    }
    else if (x == 3 && y >= 4 && leftField[0] == 2 && !InTaken(3, y - 1) && !InTaken(1, y) && !InTaken(2, y - 2)) //straight and left field cannot be taken, but it is enough we check the most left field on border. Also, 1 left and 2 up, 2 left and 2 up cannot be taken in order to draw an arealine. Checking 1 left and 2 up is enough.
    {
        if (CountArea(2, y - 1, 1, y - 1, null, false, 1))
        {
            forbidden.Add(leftField);
        }
    }
    else if (y == 3 && x >= 4 && rightField[1] == 2 && !InTaken(x - 1, 3) && !InTaken(x, 1) && !InTaken(x - 2, 2))
    {
        if (CountArea(x - 1, 2, x - 1, 1, null, true, 1))
        {
            forbidden.Add(rightField);
        }
    }
    else if (x == size - 2 && y >= 3 && rightField[0] == size - 1 && !InTaken(size - 2, y - 1) && !InTaken(size, y) && !InTaken(size - 1, y - 2))
    {
        if (CountArea(size - 1, y - 1, size, y - 1, null, true, 1))
        {
            forbidden.Add(rightField);
        }
    }
    else if (y == size - 2 && x >= 3 && leftField[1] == size - 1 && !InTaken(x - 1, size - 2) && !InTaken(x, size) && !InTaken(x - 2, size - 1))
    {
        if (CountArea(x - 1, size - 1, x - 1, size, null, false, 1))
        {
            forbidden.Add(leftField);
        }
    }
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
            // But we need circleValid, for 2 distance, in case CheckNearField is not called first..

            if (dist == 2 || dist > 2 && !InBorderRel(-1, dist - 1) && !InTakenRel(-1, dist - 1))
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
                        if (!(InTakenRel(1, 1) || InBorderRel(1, 1)))
                        {
                            forbidden.Add(new int[] { x + sx, y + sy });
                        }
                        else // C-shape
                        {
                            forbidden.Add(new int[] { x - sx, y - sy });
                        }
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
                                    if (ex == 2)
                                    {
                                        nowWCount = 0;
                                        nowWCountLeft = 1;
                                    }
                                    else
                                    {
                                        nowWCountLeft = nowWCount = (ex + 2) / 4;
                                        nowBCountLeft = nowBCount = (ex - 2) / 4;
                                        laterWCount = (ex - 2) / 4;
                                        laterBCount = (ex - 2) / 4;
                                    }
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
                                forbidden.Add(new int[] { x + lx, y + ly });
                                if (j == 2)
                                {
                                    forbidden.Add(new int[] { x - sx, y - sy });
                                }
                            }
                            if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                            {
                                forbidden.Add(new int[] { x - lx, y - ly });
                            }
                        }
                    }
                }
            }
            else if (dist > 2 && (InBorderRel(-1, dist - 1) || InTakenRel(-1, dist - 1)))                   
            {
                bool foundTaken = false;
                for(int k = 1; k < dist; k++)
                {
                    if (InTakenRel(1, k))
                    {
                        foundTaken = true;
                        break;
                    }
                }

                if (!foundTaken) // area same as in LeftRightAreaUp
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
                        // the right side should be taken, so we don't need to check more
                        int i1 = InTakenIndexRel(0, dist);
                        int i2 = InTakenIndexRel(-1, dist);

                        if (i1 > i2)
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

        for (int j = 0; j < 3; j++) // j = 1: small area, j = 2: big area
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
                if (ex == 1) // close mid across
                {
                    forbidden.Add(new int[] { x + sx, y + sy });
                    forbidden.Add(new int[] { x - lx, y - ly });
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
                        else // We can enter later, check for start C on the opposite side (if the obstacle is up on the left, we check the straight field for next step C, not the right field.) 
                             // 466
                        {
                            if (ex == 2 && !InTakenRel(-1, 1) && (InTakenRel(-2, 1) || InBorderRel(-2, 1)) && InTakenRel(-1, 0))
                            {
                                forbidden.Add(new int[] { x + sx, y + sy });
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

            while (!InTakenRel(1, dist) && !InBorderRel(1, dist))
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
                if (ex == 1) // close mid across big
                {
                    forbidden.Add(new int[] { x + sx, y + sy });
                    forbidden.Add(new int[] { x + lx, y + ly });
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

void CheckLeftRightCornerBig() // rotate down (CCW): 59438645 for behind and up for small area 
{
    for (int i = 0; i < 2; i++)
    {
        bool circleDirectionLeft = (i == 0) ? false : true;

        for (int j = 0; j < 3; j++)
        {

            int hori = 1;
            int vert = 2;

            while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
            {
                bool circleValid = false;
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

                // check field below to make sure we are at a corner, not a side wall
                if (!InTakenRel(hori, vert - 1))
                {
                    int i1 = InTakenIndexRel(hori, vert);
                    int i2 = InTakenIndexRel(hori, vert + 1);

                    if (i2 > i1)
                    {
                        circleValid = true;
                    }
                }

                if (circleValid)
                {
                    if (hori == 2 && vert == 2) // close across, big if j = 0
                    {
                        forbidden.Add(new int[] { x + lx, y + ly });
                        if (j == 2) // close across small
                        {
                            forbidden.Add(new int[] { x - sx, y - sy });
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

                        int nowWCount, nowWCountRight, nowBCount, laterWCount, laterBCount;
                        int a, n;

                        //check if all fields on the border line is free
                        if (vert == hori)
                        {
                            a = hori - 1;
                            nowWCountRight = nowWCount = 0;
                            nowBCount = a - 1;
                            laterWCount = -1;// means B = 1
                            laterBCount = a - 1;

                            for (int k = 1; k < hori; k++)
                            {
                                if (k < hori - 1)
                                {
                                    if (InTakenRel(k, k) || InTakenRel(k, k + 1))
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
                                    borderFields.Add(new int[] { 1, 2 });
                                }
                                else if (k < hori - 1)
                                {
                                    borderFields.Add(new int[] { k, k });
                                    borderFields.Add(new int[] { k, k + 1 });
                                }
                            }
                        }
                        else if (hori > vert)
                        {
                            a = vert - 1;
                            n = (hori - vert - (hori - vert) % 2) / 2;

                            if ((hori - vert) % 2 == 0)
                            {
                                nowWCountRight = nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                nowBCount = a + (n - 1 - (n - 1) % 2) / 2;
                                laterWCount = (n - n % 2) / 2;
                                laterBCount = a + (n - n % 2) / 2;
                            }
                            else
                            {
                                nowWCountRight = nowWCount = 1 + (n + 1 - (n + 1) % 2) / 2;
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

                            for (int k = 1; k < vert; k++)
                            {
                                if (k < vert - 1 && vert > 2)
                                {
                                    if (InTakenRel(k, k) || InTakenRel(k, k + 1))
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

                                if (vert > 2) // there is no stair if corner is at 1 distance, only one field which is the start field.
                                {
                                    if (k == 1)
                                    {
                                        borderFields.Add(new int[] { 1, 2 });
                                    }
                                    else if (k < vert - 1)
                                    {
                                        borderFields.Add(new int[] { k, k });
                                        borderFields.Add(new int[] { k, k + 1 });
                                    }
                                    else
                                    {
                                        borderFields.Add(new int[] { k, k });
                                    }
                                }
                            }

                            for (int k = 1; k <= hori - vert; k++)
                            {
                                if (InTakenRel(vert - 1 + k, vert - 1))
                                {
                                    takenFound = true;
                                    break;
                                }

                                if (k < hori - vert)
                                {
                                    borderFields.Add(new int[] { vert - 1 + k, vert - 1 });
                                }
                            }
                        }
                        else // vert > hori
                        {
                            a = hori - 1;
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
                                nowBCount = a + (n - 1 - (n - 1) % 2) / 2;
                                laterWCount = (n - n % 2) / 2;
                                laterBCount = a + (n - n % 2) / 2;
                            }
                            else
                            {
                                if (n > 0)
                                {
                                    nowWCountRight = nowWCount = a + (n - n % 2) / 2;
                                    laterBCount = (n + 2 - (n + 2) % 2) / 2;
                                }
                                else
                                {
                                    nowWCount = a - 1;
                                    nowWCountRight = a;
                                    laterBCount = 0;
                                }
                                nowBCount = (n + 1 - (n + 1) % 2) / 2;
                                laterWCount = a - 1 + (n + 1 - (n + 1) % 2) / 2;

                            }

                            for (int k = 1; k <= vert - hori; k++)
                            {
                                if (InTakenRel(1, k))
                                {
                                    takenFound = true;
                                    break;
                                }

                                if (k > 1)
                                {
                                    borderFields.Add(new int[] { 1, k });
                                }
                            }

                            for (int k = 1; k < hori; k++)
                            {
                                if (k < hori - 1)
                                {
                                    if (InTakenRel(k, vert - hori + k) || InTakenRel(k, vert - hori + k + 1))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    if (InTakenRel(k, vert - hori + k))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }

                                if (k < hori - 1)
                                {
                                    borderFields.Add(new int[] { k, vert - hori + k });
                                    borderFields.Add(new int[] { k, vert - hori + k + 1 });
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
                                    // for small area
                                    if (j == 2)
                                    {
                                        forbidden.Add(new int[] { x - sx, y - sy });
                                    }
                                }
                            }
                        }
                    }
                }

                vert++;
                hori = 1;
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

void Check3DoubleArea() // the distance to the obstacle is maximum 3. Line cannot finish at the far corner, but at the field below. There is a second area created sith an obstacle on the right side.

// has to be rotated ccw in first area case
{
    for (int i = 0; i < 2; i++)
    {
        for (int j = 0; j < 2; j++)
        {
            bool circleValid = false;
            bool circleDirectionLeft = (i == 0) ? true : false;
            int startX = 0, startY = 0, endX = 0, endY = 0;
            int forbidX = 0, forbidY = 0;
            int circleParity = 0;
            int caseNumber = 0;

            List<int[]> borderFields = new();

            // First two cases can be simultaneously true, as in 2024_0505. If the bigger area is pair, so is the smaller, which would cause a C-shape with the obstacle in (1, 4). So we can just examine the smaller area. borderFields needs to be reset in that case.

            if (InTakenRel(1, 4) && !InTakenRel(0, 2) && !InTakenRel(0, 3) && !InTakenRel(0, 4) && !InTakenRel(1, 1) && !InTakenRel(1, 3))
            {
                int i1 = InTakenIndexRel(1, 4);
                int i2 = InTakenIndexRel(2, 4);

                if (i2 > i1)
                {
                    circleValid = true;

                    startX = 0;
                    startY = 1;
                    endX = 0;
                    endY = 3;
                    forbidX = 0;
                    forbidY = 1;
                    borderFields.Add(new int[] { 0, 2 });
                    circleParity = 0;
                    caseNumber = 1;
                }
            }

            if (InTakenRel(2, 3) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(1, 2) && !InTakenRel(1, 3) && !InTakenRel(0, 2) && !InTakenRel(2, 2))
            {
                int i1 = InTakenIndexRel(2, 3);
                int i2 = InTakenIndexRel(3, 3);

                if (i2 > i1)
                {
                    circleValid = true;

                    startX = 1;
                    startY = 1;
                    endX = 1;
                    endY = 2;
                    forbidX = 1;
                    forbidY = 0;
                    borderFields = new();
                    circleParity = 0;
                    caseNumber = 2;
                }
            }

            if (InTakenRel(2, 4) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(1, 2) && !InTakenRel(1, 3) && !InTakenRel(1, 4) && !InTakenRel(2, 1) && !InTakenRel(2, 3))
            {
                int i1 = InTakenIndexRel(2, 4);
                int i2 = InTakenIndexRel(3, 4);

                if (i2 > i1)
                {
                    circleValid = true;

                    startX = 1;
                    startY = 1;
                    endX = 1;
                    endY = 3;
                    forbidX = 1;
                    forbidY = 0;
                    borderFields.Add(new int[] { 1, 2 });
                    circleParity = 1;
                    caseNumber = 3;
                }
            }

            if (circleValid && CountAreaRel(startX, startY, endX, endY, borderFields, circleDirectionLeft, 2, true))
            {
                int black = (int)info[1];
                int white = (int)info[2];

                if (circleParity == 0 && black == white || circleParity == 1 && black == white + 1)
                {
                    int thisX = x;
                    int thisY = y;
                    int thisSx = sx;
                    int thisSy = sy;
                    int thisLx = lx;
                    int thisLy = ly;

                    int[] rotatedDir = RotateDir(lx, ly, i);
                    lx = rotatedDir[0];
                    ly = rotatedDir[1];
                    rotatedDir = RotateDir(sx, sy, i);
                    sx = rotatedDir[0];
                    sy = rotatedDir[1];

                    if (caseNumber < 3)
                    {
                        x = x + endX * thisLx + endY * thisSx;
                        y = y + endX * thisLy + endY * thisSy;

                        if (CheckNearFieldSmall1()) // check only mid across
                        {
                            switch (caseNumber)
                            {
                                case 1: // 0601
                                    DoubleArea1 = true;
                                    activeRules.Add("Double Area first case");
                                    activeRuleSizes.Add(new int[] { 4, 6 });
                                    break;
                                case 2:
                                    DoubleArea2 = true;
                                    activeRules.Add("Double Area second case");
                                    activeRuleSizes.Add(new int[] { 4, 5 });
                                    break;
                            }

                            activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + forbidX * thisLx + forbidY * thisSx, thisY + forbidX * thisLy + forbidY * thisSy } });

                            forbidden.Add(new int[] { thisX + forbidX * thisLx + forbidY * thisSx, thisY + forbidX * thisLy + forbidY * thisSy });
                        }
                    }
                    else
                    {
                        x = x + endX * thisLx + (endY - 1) * thisSx;
                        y = y + endX * thisLy + (endY - 1) * thisSy;

                        if (CheckNearFieldSmall1()) // check mid across or across
                        {
                            DoubleArea3 = true;
                            activeRules.Add("Double Area third case");
                            activeRuleSizes.Add(new int[] { 4, 5 });

                            activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + forbidX * thisLx + forbidY * thisSx, thisY + forbidX * thisLy + forbidY * thisSy } });

                            forbidden.Add(new int[] { thisX + forbidX * thisLx + forbidY * thisSx, thisY + forbidX * thisLy + forbidY * thisSy });
                        }
                    }

                    x = thisX;
                    y = thisY;
                    lx = thisLx;
                    ly = thisLy;
                    sx = thisSx;
                    sy = thisSy;
                }
            }
            // rotate up. Clockwise on left side and counter-clockwise on right side.
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

bool CheckNearFieldSmall() // for use only with Directional Area
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

    return false;
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
            return false;
        }

        areaLine.Add(new int[] { nextX, nextY });
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
            errorString = "Count of start and end squares are inequal: " + startSquares.Count + " " + count;
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