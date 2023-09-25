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

- When the left or the right field is (n-1, n-1), we cannot step there unless we are on the edge.

<img src="References/0831_2.svg" width="23.8%"/>

There have not been found any case where the future line cannot extend, and the main line has to step back.<br />
This will change on 7 x 7. See these examples:

<img src="References/0821.svg" width="33.3%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/0827.svg" width="33.3%"/>

In the first, the upper right line fails when we step right. In the second, the line on the left. The results are:

<img src="References/0821_fail.svg" width="33.3%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/0827_fail.svg" width="33.3%"/>

Do you see the pattern? To avoid the situation, we need to check if there is a future line that starts 2 to left and ends 2 to left and 2 to straight. (Same with the right side.) And that's not all. The pattern can be rotated as well, so that the future line starts 2 to straight:

<img src="References/0902_1.svg" width="33.3%"/>

In these situations the only possibility is to step towards the start of the future line.

Notice a new future line extension rule in these examples. When a near end is at 2 distance left or right from the actual end, it will fill the field between them if the live end steps elsewhere.

Other rules define the possibilities when approaching or moving along an edge:

<img src="References/0831_3.svg" width="33.3%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/0831_4.svg" width="33.3%"/>

These were not necessary on 5 x 5, because future lines filled the spaces nearby. In a larger area, future lines are not constrained to only one option.

C-shapes on the right and bottom edge also come into play. From here, it is not possible to continue:

<img src="References/0831_1_2.svg" width="33.3%"/>

So, we need to define a rule already at the previous step to prevent stepping here; that is if the current x position is n - 2, and the right field's x position is n - 1, and the field 1 to right and 1 back is free, and the field 1 to right and 2 to back is taken, we cannot step right.

And on 9 x 9, the same rule will apply near the left and upper edge.

<img src="References/0901_1.svg" width="42.86%"/>

The green fields now mark a new rule, but that's for later.<br /> 
There is one more thing to keep in mind on 7 x 7. If the line approaches itself, it needs to behave as on the edge. In the following situation, the left and straight option has to be disabled.

<img src="References/0901.svg" width="33.3%"/>

The program is now equipped with a "Fast run" function, which makes it possible to run through approximately 100 cases per second, depending on you computer speed. This enables us to discover all 7 x 7 walkthroughs, which is 107 704. According to the Online Encyclopedia of Integer Series (Number of simple Hamiltonian paths connecting opposite corners of a 2n+1 X 2n+1 grid) it should be 111 712. If that calculation is correct, then the program has missed some cases.

A rule editor is being created now to provide a better overview about them.

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
