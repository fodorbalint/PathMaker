namespace OneWayLabyrinth
{
	using System.Collections.Generic;

	public partial class Path
	{
		int directionFieldIndex = 0;
		List<string> activeRules;
		List<List<int[]>> activeRulesForbiddenFields;
		List<int[]> activeRuleSizes;
		List<int[]> startForbiddenFields;
		public bool Future2x2StartEnd = false;
		public bool Future2x3StartEnd = false;
		public bool Future3x3StartEnd = false;
		public bool FutureL = false;
		public bool TripleAreaExitDown = false;

		public void RunRules()
		{
			activeRules = new();
			activeRulesForbiddenFields = new();
			activeRuleSizes = new();
			startForbiddenFields = Copy(forbidden);
			Future2x2StartEnd = false;
			Future2x3StartEnd = false;
			Future3x3StartEnd = false;
			FutureL = false;
			TripleAreaExitDown = false;

			if (size == 5)
			{
				// C-Shape
				// Embedded in Path.cs as the absolute checking functions need it.
			}

			if (size == 7)
			{
				// Future 2 x 2 Start End
				for (int i = 0; i < 2; i++)
				{
					if ((InTakenRel(4,1) || InBorderRel(4,1)) && InFutureStartRel(1,0) && InFutureEndRel(3,0) && InTakenRel(0,3) && InTakenRel(-1,2) && InTakenRel(-1,1) && !InTakenRel(0,2) && !InBorderRel(0,2) && foundSectionStart == foundSectionEnd)
					{
						Future2x2StartEnd = true;
						activeRules.Add("Future 2 x 2 Start End");
						activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + lx, y + ly }});
						activeRuleSizes.Add(new int[] {6,4});
						forbidden.Add(new int[] { x + lx, y + ly });
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;

				// Future 2 x 3 Start End
				for (int i = 0; i < 2; i++)
				{
					if ((InTakenRel(1,-2) || InBorderRel(1,-2)) && !InTakenRel(1,-1) && !InBorderRel(1,-1) && InFutureStartRel(0,1) && InFutureEndRel(2,1) && foundSectionStart == foundSectionEnd)
					{
						Future2x3StartEnd = true;
						activeRules.Add("Future 2 x 3 Start End");
						activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + lx, y + ly }});
						activeRuleSizes.Add(new int[] {3,4});
						forbidden.Add(new int[] { x + lx, y + ly });
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;

				// Future 3 x 3 Start End
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if (!InTakenRel(3,3) && !InBorderRel(3,3) && !InTakenRel(3,1) && !InBorderRel(3,1) && (InTakenRel(3,4) || InBorderRel(3,4)) && (InTakenRel(2,4) || InBorderRel(2,4)) && (InTakenRel(1,4) || InBorderRel(1,4)) && (InTakenRel(4,3) || InBorderRel(4,3)) && (InTakenRel(4,2) || InBorderRel(4,2)) && (InTakenRel(4,1) || InBorderRel(4,1)) && InFutureStartRel(0,1) && InFutureEndRel(0,3) && !InCornerRel(3,3) && foundSectionStart == foundSectionEnd)
						{
							Future3x3StartEnd = true;
							activeRules.Add("Future 3 x 3 Start End");
							activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }});
							activeRuleSizes.Add(new int[] {5,5});
							forbidden.Add(new int[] { x + sx, y + sy });
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

				// Future L
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if (InFutureStartRel(2,0) && InFutureEndRel(2,2) && foundSectionStart == foundSectionEnd)
						{
							FutureL = true;
							activeRules.Add("Future L");
							activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }, new int[] { x - lx, y - ly }});
							activeRuleSizes.Add(new int[] {4,3});
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

			if (size >= 9)
			{
				// Triple Area Exit Down
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(4,0) || InBorderRel(4,0)) && (InTakenRel(4,3) || InBorderRel(4,3)) && (InTakenRel(0,3) || InBorderRel(0,3)) && !InTakenRel(2,0) && !InBorderRel(2,0) && !InTakenRel(3,0) && !InBorderRel(3,0) && !InTakenRel(4,1) && !InBorderRel(4,1) && !InTakenRel(4,2) && !InBorderRel(4,2) && !InTakenRel(3,3) && !InBorderRel(3,3) && !InTakenRel(2,3) && !InBorderRel(2,3) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(1,2) && !InBorderRel(1,2) && !InTakenRel(1,1) && !InBorderRel(1,1))
						{
							bool TripleAreaExitDown_circle1 = false;
							directionFieldIndex = InTakenIndexRel(4,0);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(4,-1))
								{
									int leftIndex = InTakenIndexRel(4,-1);
									if (leftIndex > directionFieldIndex)
									{
										TripleAreaExitDown_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(4,1);
									if (rightIndex < directionFieldIndex)
									{
										TripleAreaExitDown_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(4,0);
								int farSideIndex = InBorderIndexRel(4,1);
								if (farSideIndex > directionFieldIndex)
								{
									TripleAreaExitDown_circle1 = true;
								}
							}
							
							bool TripleAreaExitDown_circle2 = false;
							directionFieldIndex = InTakenIndexRel(4,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(5,3))
								{
									int leftIndex = InTakenIndexRel(5,3);
									if (leftIndex > directionFieldIndex)
									{
										TripleAreaExitDown_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(3,3);
									if (rightIndex < directionFieldIndex)
									{
										TripleAreaExitDown_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(4,3);
								int farSideIndex = InBorderIndexRel(3,3);
								if (farSideIndex > directionFieldIndex)
								{
									TripleAreaExitDown_circle2 = true;
								}
							}
							
							bool TripleAreaExitDown_circle3 = false;
							directionFieldIndex = InTakenIndexRel(0,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(0,4))
								{
									int leftIndex = InTakenIndexRel(0,4);
									if (leftIndex > directionFieldIndex)
									{
										TripleAreaExitDown_circle3 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(0,2);
									if (rightIndex < directionFieldIndex)
									{
										TripleAreaExitDown_circle3 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(0,3);
								int farSideIndex = InBorderIndexRel(0,2);
								if (farSideIndex > directionFieldIndex)
								{
									TripleAreaExitDown_circle3 = true;
								}
							}
							
							ResetExamAreas();
							if (TripleAreaExitDown_circle1 && TripleAreaExitDown_circle2 && TripleAreaExitDown_circle3 && CountAreaRel(1,0,3,0,new List<int[]> {new int[] {2,0}},i==0?true:!true,1) && CountAreaRel(3,3,1,3,new List<int[]> {new int[] {2,3}},i==0?true:!true,1))
							{
								TripleAreaExitDown = true;
								activeRules.Add("Triple Area Exit Down");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x - lx, y - ly }, new int[] { x + sx, y + sy }});
								activeRuleSizes.Add(new int[] {6,4});
								AddExamAreas();
								forbidden.Add(new int[] { x - lx, y - ly });
								forbidden.Add(new int[] { x + sx, y + sy });
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

			if (size >= 13)
			{			}
			T("Future2x2StartEnd: " + Future2x2StartEnd + "\n" + "Future2x3StartEnd: " + Future2x3StartEnd + "\n" + "Future3x3StartEnd: " + Future3x3StartEnd + "\n" + "FutureL: " + FutureL + "\n" + "TripleAreaExitDown: " + TripleAreaExitDown);
			window.ShowActiveRules(activeRules,activeRulesForbiddenFields,startForbiddenFields,activeRuleSizes);
		}
	}
}