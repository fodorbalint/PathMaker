# One-way labyrinth generator

This program aims to solve the following challenge:<br />
"Draw a line that goes through an n x n grid (where n is an odd number), passing through each field once. The line has to start from the field at the upper left corner (1,1) and end at (n,n). At any time it is allowed to move left, right, up or down, and it has to randomly choose between the available fields."

At first sight it may look easy. But look at the following example:

<img src="References/0701_1.svg"/>

The program calculated the blue lines, they are necessary to go through in the future based on how the black line was drawn.<br />
Do you see why the board is impossible to fill from now on?

The question is, is there a single rule or a set of rules that will guarantee you can draw a labyrinth of any size? Or do the rules get infinitely complex?

In the beginning of the project I let the program run on a 21 x 21 field, and whenever I noticed a trouble, I coded the solution into it. While you can discover many patterns this way, a gradual approach may be more effective.

A 3 x 3 area can only be filled in two ways, like this and mirrored:

<img src="References/3x3.svg" width="14.3%"/>

The 5 x 5 requires much more consideration. Whenever it is possible to draw future lines, the program has to be able to do it. The future lines can not only extend at each step but connect too.

<!-- specify extension and connection rules -->

<img src="References/0806.svg" width="23.8%"/>

By August 21, 2023 all 5 x 5 scenarios were discovered. The number of walkthroughs are 104.<br />
Here are the things to consider on a grid of this size:

<img src="References/0821_1.svg" width="23.8%"/>

- A single field next to the live end that is walled from two other sides (either by the border or the line) needs to be filled in the next step. I call it C-shape.

- A 2 x 3 empty area next to the live end that is walled by three sides (2-3-2 long) will have a future line going through along the walls. At the wall next to the main line, its direction is the opposite of the main line, meaning it will go from (3,2) upwards whereas the main line just took a step downwards. How the middle field will be filled is not yet known. Either the near end (the one the main line will go through first) or the far end can fill it.

<img src="References/0821_2.svg" width="23.8%"/>

- A 2 x 2 empty area next to the live end that is walled by three sides (2-2-2 long) will have a future line going through along the walls. In the example above, the far end is already extended by one step as it had only one option to move.

<img src="References/0821_3.svg" width="23.8%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/0821_4.svg" width="23.8%"/>

Taking a step further, another future line is created and extended on the left side. Any step we take now will further extend and connect the two future lines, giving a complete walkthrough. Future lines are first extended when we step on them. Then, if there is another line that started from the position next to where the live end was in the previous step, it gets extended too.<br />
Note that the line being stepped on has its end at (5,4). The nearby empty fields are (4,4) and the corner, (5,5). It cannot choose the corner, because then nothing would fill (4,4). Then, the line on the left gets extended until it connects to the other. As the near end cannot be extended more, the far end gets extended until it reaches the corner.

<!-- change -->
- When we are two distance away from the edge, we need to check if stepping towards it is possible.

<img src="References/0930.svg" width="23.8%"/>

It is because if we do so, an enclosed area is created, with one way to go out of it. If that area has an impair amount of cells, it cannot be filled, so we cannot take that step.<br />
The explanation is simple: Imagine if the table was a chess board. In order to step from white to black, you would need to take an impair amount of steps - the color changes at every step. Here, the entry of the area would be (4,3) and the exit (5,3). An impair amount of steps means pair amount of cells.

There have not been found any case where the future line cannot extend, and the main line has to step back.<br />
This will change on 7 x 7. See these examples:

<img src="References/0821.svg" width="33.3%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/0827.svg" width="33.3%"/>

In the first, the upper right line fails when we step right. In the second, the line on the left. The results are:

<img src="References/0821_0_1.svg" width="33.3%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/0827_0_1.svg" width="33.3%"/>

Do you see the pattern? To avoid the situation, we need to check if there is a future line that starts 2 to left and ends 2 to left and 2 to straight. (Same with the right side.) And that's not all. The pattern can be rotated as well, so that the future line starts 2 to straight:

<img src="References/0902_1.svg" width="33.3%"/>

In these situations the only possibility is to step towards the start of the future line.

Notice a new future line extension rule in these examples. When a near end is at 2 distance left or right from the actual end, it will fill the field between them if the live end steps elsewhere.

<!-- change -->
And in other situations, there is a 1-thin future line next to the live end that can be extended if its far end is at the corner. Though disabling this rule does not affect the total amount of walkthroughs on a 7 x 7 grid, I chose to include it in the project on the basis that if a future line can be extended, we should do it. It can make a considerable difference. The left picture is without the rule, the right is with it:

<img src="References/0911.svg" width="33.3%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/0911_0_1.svg" width="33.3%"/>

There are also rules that define the possibilities when approaching or moving along an edge:

<img src="References/0831_3.svg" width="33.3%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/0831_4.svg" width="33.3%"/>

These were not necessary on 5 x 5, because future lines filled the spaces nearby. In a larger area, future lines are not constrained to only one option.

<!-- change -->
You need to do the same inside the grid as well, and for that it is necessary to determine which direction the line was going at the point you are heading to.

<img src="References/0901.svg" width="33.3%"/>

Impair areas can now happen inside the grid, not just on the edge.

<img src="References/0929_1.svg" width="33.3%"/>

The program is equipped with a "Fast run" function, which makes it possible to run through approximately 100 cases per second, depending on your computer speed. This enables us to discover all 7 x 7 walkthroughs. According to the Online Encyclopedia of Integer Series (Number of simple Hamiltonian paths connecting opposite corners of a 2n+1 x 2n+1 grid) it should be 111 712, but this is not easy to achieve.

Applying all the rules above, there can still be situations where you cannot complete the grid.

I have created a rule editor to provide a better overview about them. Here you can drag and drop the following fields:
- live end
- empty field
- taken field
- taken or border field
- future start field
- future end field
- a field that is not the end corner
- forbidden field

These are the relative rules (where the x or y-position is not determined like on the edge) with examples:

C-shape:

<img src="References/rules/5/C-Shape.svg" width="19.05%"/>

Side back:

<img align="top" src="References/rules/7/Side back.svg" width="14.3%"/><img src="References/spacer.svg" width="4.75%"/><img align="top" src="References/Side back 2558.svg" width="33.3%"/>

Side front:

<img align="top" src="References/rules/7/Side front.svg" width="14.3%"/><img src="References/spacer.svg" width="4.75%"/><img align="top" src="References/Side front 266.svg" width="33.3%"/>

Side front L:

<img align="top"  src="References/rules/7/Side front L.svg" width="14.3%"/><img src="References/spacer.svg" width="4.75%"/><img align="top"  src="References/Side front L 3627.svg" width="33.3%"/>

Future L:

<img align="top" src="References/rules/7/Future L.svg" width="19.05%"/><img src="References/spacer.svg" width="4.75%"/><img align="top"  src="References/Future L 65.svg" width="33.3%"/>

Future 2 x 2 Start End

<img align="top" src="References/rules/7/Future 2 x 2 Start End.svg" width="28.57%"/><img src="References/spacer.svg" width="4.75%"/><img align="top"  src="References/Future 2 x 2 Start End 450.svg" width="33.3%"/>

Future 2 x 3 Start End

<img align="top" src="References/rules/7/Future 2 x 3 Start End.svg" width="14.3%"/><img src="References/spacer.svg" width="4.75%"/><img align="top"  src="References/Future 2 x 3 Start End 465.svg" width="33.3%"/>

Future 3 x 3 Start End

<img align="top" src="References/rules/7/Future 3 x 3 Start End.svg" width="23.81%"/><img src="References/spacer.svg" width="4.75%"/><img align="top"  src="References/Future 3 x 3 Start End 1861.svg" width="33.3%"/>


I can now confirm that the number of walkthroughs in the encyclopedia are correct.

Is it possible to develop an algorythm that works for all sizes? We have seen above rules about approacing the edge or the line itself, which are quite universal. Counting an area too, as well as the side front/back rules. But the "Future ... Start End" patterns look unique.<br />
There will be more cases like this on 9 x 9, and who knows if it gets complicated infinitely.

As the sizes grow, it will be impossible to run through all cases with one computer in a reasonable time. In order to discover the patterns, we need to run the program randomly.

Next, I will make statistics about how many percent of walkthrough attempts are successful on different sizes, using the rules we already have.

---

The project contains the source code for use with Visual Studio. To start the program, run OneWayLabyrinth.exe in the folder "bin/Debug/net6.0-windows".

---

Hotkeys:

Enter: Reload or Close error message<br />
Ctrl + S: Save path<br />
Right arrow: Step forward<br />
Left arrow: Step back<br />
Ctrl/Shift + arrows: step in direction if possible. If CapsLock is on, pressing the Ctrl or Shift keys is not necessary.<br />
Space: Run automatically / Stop automatic running
