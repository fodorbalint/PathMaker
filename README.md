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

<!-- change -->

<img src="References/rules/5/C-Shape.svg" width="19.05%" align="top" /><img src="References/spacer.svg" width="4.75%"/><img src="References/C-Shape example.svg" width="19.05%" align="top" />

- A single field next to the live end that is walled from two other sides (either by the border or the line) needs to be filled in the next step. I call it C-shape. The pattern is both mirrored and rotated, so that the empty field is straight ahead. To qualify for this rule, the empty field cannot be the end corner. If there is a C-shape, we don't need to check other rules.

<img src="References/near border.svg" width="23.8%"/>

- Movement near the edge: In the example, we cannot step left (3,5), since the (2,5) field is empty. 

<img src="References/0821_1.svg" width="23.8%"/>

- A 2 x 3 empty area next to the live end that is walled by three sides (2-3-2 long) will have a future line going through along the walls. At the wall next to the main line, its direction is the opposite of the main line, meaning it will go from (3,2) upwards whereas the main line just took a step downwards. How the middle field will be filled is not yet known. Either the near end (the one the main line will go through first) or the far end can fill it.

<img src="References/0821_2.svg" width="23.8%"/>

- A 2 x 2 empty area next to the live end that is walled by three sides (2-2-2 long) will have a future line going through along the walls. In this example, the far end is already extended by one step as it had only one option to move.

<img src="References/0821_3.svg" width="23.8%"/>

- Future line extension: If there was a near end where the main line was in the previous step, it now may have only one choice to move, so it can be extended.

<img src="References/future connection.svg" width="23.8%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/0821_4.svg" width="23.8%"/>

- Future line connection: In this case, the line being stepped on extends until the far end has two options. (When the end corner is one of them, it has to be removed.) Then, the line on the left extends and now has no other option than to connect to the line on the right.<br />

<img src="References/0930.svg" width="23.8%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/0930_0_1.svg" width="23.8%"/>

- When we are two distance away from the edge, we need to check if stepping towards it is possible.
It is because if we do so, an enclosed area is created, with one way to go out of it. If that area has an impair amount of cells, it cannot be filled, so we cannot take that step.<br />
The explanation is simple: Imagine if the table was a chess board. In order to step from white to black, you would need to take an impair amount of steps - the color changes at every step. Here, the entry of the area would be (4,3) and the exit (5,3). An impair amount of steps means pair amount of cells.<br />
In the example, you can also say that we cannot step right, because there is a future line start 2 to straight and an end 2 to straight and 2 to right. On 7 x 7, there will be examples where this is the rule we have to apply, because area counting is not getting triggered: 

<img src="References/1001.svg" width="33.3%"/>

But let's start with the simpler rules:

- Future line extension: When a near end is at 2 distance left or right from the live end, it will fill the field between them if the live end steps elsewhere. That's what happened above before the line failed.

<img src="References/0911.svg" width="33.3%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/0911_0_1.svg" width="33.3%"/>

In other situations, there is a 1-thin future line next to the live end that can be extended if its far end is at the corner. Though disabling this rule does not affect the total amount of walkthroughs on a 7 x 7 grid, I chose to include it in the project on the basis that if a future line can be extended, we should do it. It can make a considerable difference. The left picture is without the rule, the right is with it.

<img src="References/0901.svg" width="33.3%"/>

- Just like moving near the edge, we need to disable some fields if we are approaching an older section of the main line. In order to determine on which side the enclosed area is created, we need to examine the direction of the line at the connection point.

<img src="References/checknearfield/close straight left right.svg" width="9.5%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/checknearfield/close straight left left.svg" width="14.3%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/checknearfield/close straight right right.svg" width="14.3%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/checknearfield/close straight right left.svg" width="9.5%"/>

The gray square means empty field. When the field 2 to straight is taken, its left or right side will be taken too.

<img src="References/checknearfield/close mid across right.svg" width="14.3%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/checknearfield/close mid across left.svg" width="19%"/>

These will only be checked if one of the above 4 situations were not present. (They have to be mirrored, too.)

<img src="References/checknearfield/close across right.svg" width="19%"/><img src="References/spacer.svg" width="4.75%"/><img src="References/checknearfield/close across left.svg" width="23.8%"/>

Likewise, these will be not be checked if the previous rules were true.

And when none of the 1-distance situations are valid, we check for 2-distance.

<img src="References/0929_1.svg" width="33.3%"/>

Impair areas can now happen inside the grid, not just on the edge.

<img align="top" src="References/rules/7/Side back.svg" width="14.3%"/><img src="References/spacer.svg" width="4.75%"/><img align="top" src="References/Side back 2558.svg" width="33.3%"/>

<img align="top" src="References/rules/7/Side front.svg" width="14.3%"/><img src="References/spacer.svg" width="4.75%"/><img align="top" src="References/Side front 266.svg" width="33.3%"/>

<img align="top"  src="References/rules/7/Side front L.svg" width="14.3%"/><img src="References/spacer.svg" width="4.75%"/><img align="top" src="References/Side front L 3627.svg" width="33.3%"/>

- When we are entering or moving in an enclosed area, the above rules define movement near the edge.

<img align="top" src="References/rules/7/Future L.svg" width="19.05%"/><img src="References/spacer.svg" width="4.75%"/><img align="top" src="References/Future L 65.svg" width="33.3%"/>

- This is what I started the 7 x 7 introduction with.

<img align="top" src="References/rules/7/Future 2 x 2 Start End.svg" width="23.81%"/><img src="References/spacer.svg" width="4.75%"/><img align="top" src="References/Future 2 x 2 Start End 450.svg" width="33.3%"/>

<img align="top" src="References/rules/7/Future 2 x 3 Start End.svg" width="14.3%"/><img src="References/spacer.svg" width="19.05%"/><img align="top" src="References/Future 2 x 3 Start End 465.svg" width="33.3%"/>

<img align="top" src="References/rules/7/Future 3 x 3 Start End.svg" width="23.81%"/><img src="References/spacer.svg" width="9.52%"/><img align="top" src="References/Future 3 x 3 Start End 1861.svg" width="33.3%"/>

- And these are the remaining size-specific rules.

The program, in fast mode, can run through approximately 100 cases per second, depending on your computer speed. This enables us to discover all 7 x 7 walkthroughs, which is 111 712.<br />
It is equal to what is described in the Online Encyclopedia of Integer Series (Number of simple Hamiltonian paths connecting opposite corners of a 2n+1 x 2n+1 grid).

As the sizes grow, it will be impossible to run through all cases with one computer in a reasonable time. In order to discover the patterns, we need to run the program randomly.

Is it possible to develop an algorythm that works for all sizes? The edge-related and area-counting rules are universal, but the size-specific rules get more and more complex. Can you define them with one statement?

I have made statistics about how many random walkthroughs you can complete on different grids using the 7 x 7-specific and the universal rules before running into an error. Based on 1000 attempts, here are the results:<br />
9: 16.7<br />
11: 4<br />
13: 1.8<br />
15: 0.9<br />
17: 0.4<br />
19: 0.2<br />
21: 0.1

To discover 9-specific patterns, I run the program keeping it left as long as the time to get to the first error is too big. After that, I will run it randomly. The first 13 826 walkthroughs are completed before we encounter a situation. It is similar to the last one we discovered on 7 x 7:

<img align="top" src="References/1007.svg" width="42.86%"/>

Let's simplify the pattern. Which will be impossible to fill?

<img align="top" src="References/1008.svg" width="42.86%"/><img src="References/spacer.svg" width="4.75%"/><img align="top" src="References/1008_1.svg" width="42.86%"/>

It is the picture on the left. Since the yellow-bordered area is impair, adding the (4,2) (4,3) (4,4) fields will be pair. We enter the area at (4,4), so we will exit at (4,3). Now we enter the 3 x 3 area in the top left corner at its side, (3,3) and will exit at (2,4). The results is two C-shapes on each side:

<img align="top" src="References/1008_2.svg" width="42.86%"/>

We can define a rule by marking the following fields and counting the area from the fields in front of the main line to the right:

<img align="top" src="References/rules/9/Count Area 3 x 3.svg" width="28.57%"/>

Start_1 field is (4,3) and Start_2 field is (4,4) in the actual example. End field is (4,2). Direction of the circle: right (counter-clockwise). If the area is pair, we cannot step straight.

When generating code from the drawing, we have to check on which side the enclosed area was created. Here, we want it to be on the right side, so there are two cases to look at:
- The taken or border field beyond the end field is a taken field. In this case, if the field to its left is taken, its index must be lower. If the field to the right is taken, its index must be higher.
- It is the border. Add together the x- and y-coordinates to get a value. A higher value is closer to the end corner. Here, we compare the border field straight ahead and on its left, and we want the first-mentioned to be the smaller.

I have applied this rule rotated clockwise (besides mirroring it, or course), so that the live end can both come from the bottom and the right. But it can also come from the left in this example:

<img align="top" src="References/1010_2.svg" width="42.86%"/>

This will probably be another rule, because in this case it is not necessary to have an empty 3 x 3 field on the left.

Now let's run the program further up to number 13 992:
<!-- (from stepping back + 142 (with first rule disabled) / 158 = 13 984) why? -->

<img align="top" src="References/1010_4_error.svg" width="42.86%"/><img src="References/spacer.svg" width="4.75%"/><img align="top" src="References/1010_4.svg" width="42.86%"/>

It is also just like the 7 x 7 rule, just with the extension of the area on the opposite side of the future line ends. But we can't simply remove the two taken fields on that side, because the line might continue in that direction, as it is the case here:

<img align="top" src="References/1013.svg" width="33.3%"/>

It would be a mistake to disable the right field.

So we need to check if an enclosed has been created on that side, but counting the area is unnecessary. Nevertheless, we can represent the rule this way, setting the circle direction to right:

<img align="top" src="References/rules/9/Future 2 x 2 Start End 9.svg" width="23.8%"/>

The code generator will examine if the count area start and end fields are 1 or 2 distance apart. In the first case, it will only determine in which direction the taken field straight ahead is going to, and if it is right, the forbidden field will take effect.<br />
You may ask, why that field is "taken", not "taken or border". From what I found through some examples, if that field is border, the enclosed area on the right is impair, so the line cannot step in the other direction anyway. But it needs further examination.

The next error, at 14 004 has something to with how I defined the universal rules of approaching an older section of the line, it needs to be reworked in light of the C-shape the main line can create with the border.

<img align="top" src="References/1013_1.svg" width="42.86%"/>

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
