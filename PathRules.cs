namespace OneWayLabyrinth
{
	using System.Collections.Generic;

	public partial class Path
	{
		public bool Future2x2StartEnd = false;
		public bool Future2x3StartEnd = false;
		public bool Future3x3StartEnd = false;
		public bool FutureL = false;
		public bool CountAreaAcrossBorderC = false;
		public bool DoubleCShape = false;
		public bool Future2x2StartEnd9 = false;
		public bool Future3x3StartEnd9 = false;
		public bool FutureL9 = false;
		public bool Square4x2 = false;
		public bool Across3impair = false;

		public void RunRules()
		{
			Future2x2StartEnd = false;
			Future2x3StartEnd = false;
			Future3x3StartEnd = false;
			FutureL = false;
			CountAreaAcrossBorderC = false;
			DoubleCShape = false;
			Future2x2StartEnd9 = false;
			Future3x3StartEnd9 = false;
			FutureL9 = false;
			Square4x2 = false;
			Across3impair = false;

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
					if (InTakenRel(0,3) && InTakenRel(-1,2) && InTakenRel(-1,1) && !InTakenRel(0,2) && !InBorderRel(0,2) && InFutureStartRel(1,0) && InFutureEndRel(3,0) && (InTakenRel(4,1) || InBorderRel(4,1)) && foundSectionStart == foundSectionEnd)
					{
						Future2x2StartEnd = true;
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
						if (!InTakenRel(3,3) && !InBorderRel(3,3) && !InTakenRel(3,1) && !InBorderRel(3,1) && (InTakenRel(4,3) || InBorderRel(4,3)) && (InTakenRel(4,2) || InBorderRel(4,2)) && (InTakenRel(4,1) || InBorderRel(4,1)) && (InTakenRel(3,4) || InBorderRel(3,4)) && (InTakenRel(2,4) || InBorderRel(2,4)) && (InTakenRel(1,4) || InBorderRel(1,4)) && InFutureStartRel(0,1) && InFutureEndRel(0,3) && !InCornerRel(3,3) && foundSectionStart == foundSectionEnd)
						{
							Future3x3StartEnd = true;
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
				// Count Area Across Border C
				for (int i = 0; i < 2; i++)
				{
					if ((InTakenRel(-1,-2) || InBorderRel(-1,-2)) && !InTakenRel(-1,-1) && !InBorderRel(-1,-1) && InTakenRel(-3,3) && !InTakenRel(-3,2) && !InBorderRel(-3,2) && !InTakenRel(-2,2) && !InBorderRel(-2,2) && !InTakenRel(-1,1) && !InBorderRel(-1,1))
					{
						int middleIndex = InTakenIndexRel(-3,3);
						if (middleIndex != -1)
						{
							if (InTakenRel(-2,3))
							{
								int sideIndex = InTakenIndexRel(-2,3);
								if (sideIndex > middleIndex)
								{
									circleDirectionLeft = (i == 0) ? true : false;
									List<int[]> countAreaBorderFields = new List<int[]> { new int[] {-2,1}};
									if (!CountAreaRel(-1, 1, -2, 2, countAreaBorderFields))
									{
										CountAreaAcrossBorderC = true;
										forbidden.Add(new int[] { x - lx, y - ly });
									}
								}
							}
							else
							{
								int sideIndex = InTakenIndexRel(-4,3);
								if (sideIndex < middleIndex)
								{
									circleDirectionLeft = (i == 0) ? true : false;
									List<int[]> countAreaBorderFields = new List<int[]> { new int[] {-2,1}};
									if (!CountAreaRel(-1, 1, -2, 2, countAreaBorderFields))
									{
										CountAreaAcrossBorderC = true;
										forbidden.Add(new int[] { x - lx, y - ly });
									}
								}
							}
						}
						else
						{
							middleIndex = InBorderIndexRel(-3,3);
							int farSideIndex = InBorderIndexRel(-4,3);
							if (farSideIndex > middleIndex)
							{
								circleDirectionLeft = (i == 0) ? true : false;
								List<int[]> countAreaBorderFields = new List<int[]> { new int[] {-2,1}};
								if (!CountAreaRel(-1, 1, -2, 2, countAreaBorderFields))
								{
									CountAreaAcrossBorderC = true;
									forbidden.Add(new int[] { x - lx, y - ly });
								}
							}
						}
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;

				// Double C-Shape
				for (int i = 0; i < 2; i++)
				{
					if ((InTakenRel(0,4) || InBorderRel(0,4)) && (InTakenRel(-1,4) || InBorderRel(-1,4)) && !InTakenRel(0,3) && !InBorderRel(0,3) && !InTakenRel(0,2) && !InBorderRel(0,2) && !InTakenRel(-1,3) && !InBorderRel(-1,3) && !InTakenRel(-1,2) && !InBorderRel(-1,2) && !InTakenRel(-1,1) && !InBorderRel(-1,1) && !InTakenRel(-1,0) && !InBorderRel(-1,0) && !InTakenRel(-2,0) && !InBorderRel(-2,0) && !InTakenRel(-2,3) && !InBorderRel(-2,3) && !InTakenRel(-2,2) && !InBorderRel(-2,2) && !InTakenRel(-2,1) && !InBorderRel(-2,1))
					{
						int middleIndex = InTakenIndexRel(-1,4);
						if (middleIndex != -1)
						{
							if (InTakenRel(0,4))
							{
								int sideIndex = InTakenIndexRel(0,4);
								if (sideIndex < middleIndex)
								{
									circleDirectionLeft = (i == 0) ? false : true;
									List<int[]> countAreaBorderFields = new List<int[]> { new int[] {-1,2}, new int[] {-1,1}};
									if (!CountAreaRel(-1, 0, -1, 3, countAreaBorderFields))
									{
										DoubleCShape = true;
										forbidden.Add(new int[] { x - lx, y - ly });
									}
								}
							}
							else
							{
								int sideIndex = InTakenIndexRel(-2,4);
								if (sideIndex > middleIndex)
								{
									circleDirectionLeft = (i == 0) ? false : true;
									List<int[]> countAreaBorderFields = new List<int[]> { new int[] {-1,2}, new int[] {-1,1}};
									if (!CountAreaRel(-1, 0, -1, 3, countAreaBorderFields))
									{
										DoubleCShape = true;
										forbidden.Add(new int[] { x - lx, y - ly });
									}
								}
							}
						}
						else
						{
							middleIndex = InBorderIndexRel(-1,4);
							int farSideIndex = InBorderIndexRel(0,4);
							if (farSideIndex > middleIndex)
							{
								circleDirectionLeft = (i == 0) ? false : true;
								List<int[]> countAreaBorderFields = new List<int[]> { new int[] {-1,2}, new int[] {-1,1}};
								if (!CountAreaRel(-1, 0, -1, 3, countAreaBorderFields))
								{
									DoubleCShape = true;
									forbidden.Add(new int[] { x - lx, y - ly });
								}
							}
						}
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;

				// Future 2 x 2 Start End 9
				for (int i = 0; i < 2; i++)
				{
					if (InTakenRel(0,3) && (InTakenRel(4,1) || InBorderRel(4,1)) && InFutureStartRel(1,0) && InFutureEndRel(3,0) && !InTakenRel(0,2) && !InBorderRel(0,2) && foundSectionStart == foundSectionEnd)
					{
						int middleIndex = InTakenIndexRel(0,3);
						if (middleIndex != -1)
						{
							if (InTakenRel(1,3))
							{
								int sideIndex = InTakenIndexRel(1,3);
								if (sideIndex < middleIndex)
								{
									Future2x2StartEnd9 = true;
									forbidden.Add(new int[] { x + lx, y + ly });
								}
							}
							else
							{
								int sideIndex = InTakenIndexRel(-1,3);
								if (sideIndex > middleIndex)
								{
									Future2x2StartEnd9 = true;
									forbidden.Add(new int[] { x + lx, y + ly });
								}
							}
						}
						else
						{
							middleIndex = InBorderIndexRel(0,3);
							int farSideIndex = InBorderIndexRel(1,3);
							if (farSideIndex > middleIndex)
							{
								Future2x2StartEnd9 = true;
								forbidden.Add(new int[] { x + lx, y + ly });
							}
						}
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;

				// Future 3 x 3 Start End 9
				for (int i = 0; i < 2; i++)
				{
					if ((InTakenRel(1,4) || InBorderRel(1,4)) && (InTakenRel(2,4) || InBorderRel(2,4)) && (InTakenRel(3,4) || InBorderRel(3,4)) && (InTakenRel(4,3) || InBorderRel(4,3)) && (InTakenRel(4,2) || InBorderRel(4,2)) && (InTakenRel(4,1) || InBorderRel(4,1)) && !InTakenRel(3,1) && !InBorderRel(3,1) && !InTakenRel(3,3) && !InBorderRel(3,3) && !InTakenRel(0,3) && !InBorderRel(0,3) && !InTakenRel(0,1) && !InBorderRel(0,1) && !InTakenRel(-1,1) && !InBorderRel(-1,1) && !InTakenRel(-1,3) && !InBorderRel(-1,3) && !InCornerRel(3,3))
					{
						int middleIndex = InTakenIndexRel(1,4);
						if (middleIndex != -1)
						{
							if (InTakenRel(2,4))
							{
								int sideIndex = InTakenIndexRel(2,4);
								if (sideIndex < middleIndex)
								{
									circleDirectionLeft = (i == 0) ? false : true;
									if (CountAreaRel(0, 1, 0, 3))
									{
										Future3x3StartEnd9 = true;
										forbidden.Add(new int[] { x + sx, y + sy });
									}
								}
							}
							else
							{
								int sideIndex = InTakenIndexRel(0,4);
								if (sideIndex > middleIndex)
								{
									circleDirectionLeft = (i == 0) ? false : true;
									if (CountAreaRel(0, 1, 0, 3))
									{
										Future3x3StartEnd9 = true;
										forbidden.Add(new int[] { x + sx, y + sy });
									}
								}
							}
						}
						else
						{
							middleIndex = InBorderIndexRel(1,4);
							int farSideIndex = InBorderIndexRel(2,4);
							if (farSideIndex > middleIndex)
							{
								circleDirectionLeft = (i == 0) ? false : true;
								if (CountAreaRel(0, 1, 0, 3))
								{
									Future3x3StartEnd9 = true;
									forbidden.Add(new int[] { x + sx, y + sy });
								}
							}
						}
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;

				// Future L 9
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if (InFutureStartRel(2,0) && InFutureEndRel(2,2) && foundSectionStart == foundSectionEnd)
						{
							FutureL9 = true;
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

				// Square 4 x 2
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if (InTakenRel(0,3) && InTakenRel(-3,3) && InTakenRel(-3,0) && !InTakenRel(-1,0) && !InBorderRel(-1,0) && !InTakenRel(-2,0) && !InBorderRel(-2,0) && !InTakenRel(-2,2) && !InBorderRel(-2,2) && !InTakenRel(-2,1) && !InBorderRel(-2,1) && !InTakenRel(-2,3) && !InBorderRel(-2,3) && !InTakenRel(-1,3) && !InBorderRel(-1,3) && !InTakenRel(0,2) && !InBorderRel(0,2))
						{
							int middleIndex = InTakenIndexRel(-3,3);
							if (middleIndex != -1)
							{
								if (InTakenRel(-2,3))
								{
									int sideIndex = InTakenIndexRel(-2,3);
									if (sideIndex > middleIndex)
									{
										circleDirectionLeft = (i == 0) ? true : false;
										if (CountAreaLineRel(-2, 1, -2, 2))
										{
											Square4x2 = true;
											forbidden.Add(new int[] { x + sx, y + sy });
										}
									}
								}
								else
								{
									int sideIndex = InTakenIndexRel(-4,3);
									if (sideIndex < middleIndex)
									{
										circleDirectionLeft = (i == 0) ? true : false;
										if (CountAreaLineRel(-2, 1, -2, 2))
										{
											Square4x2 = true;
											forbidden.Add(new int[] { x + sx, y + sy });
										}
									}
								}
							}
							else
							{
								middleIndex = InBorderIndexRel(-3,3);
								int farSideIndex = InBorderIndexRel(-4,3);
								if (farSideIndex > middleIndex)
								{
									circleDirectionLeft = (i == 0) ? true : false;
									if (CountAreaLineRel(-2, 1, -2, 2))
									{
										Square4x2 = true;
										forbidden.Add(new int[] { x + sx, y + sy });
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
			}

			if (size >= 13)
			{
				// Across 3 impair
				for (int i = 0; i < 2; i++)
				{
					if (InTakenRel(4,4) && !InTakenRel(4,3) && !InBorderRel(4,3) && !InTakenRel(3,3) && !InBorderRel(3,3) && !InTakenRel(1,1) && !InBorderRel(1,1) && !InTakenRel(1,0) && !InBorderRel(1,0) && !InTakenRel(3,1) && !InBorderRel(3,1) && !InTakenRel(3,0) && !InBorderRel(3,0) && !InTakenRel(4,1) && !InBorderRel(4,1))
					{
						int middleIndex = InTakenIndexRel(4,4);
						if (middleIndex != -1)
						{
							if (InTakenRel(5,4))
							{
								int sideIndex = InTakenIndexRel(5,4);
								if (sideIndex > middleIndex)
								{
									circleDirectionLeft = (i == 0) ? true : false;
									List<int[]> countAreaBorderFields = new List<int[]> { new int[] {3,2}, new int[] {3,1}, new int[] {2,1}};
									if (!CountAreaRel(1, 1, 3, 3, countAreaBorderFields))
									{
										Across3impair = true;
										forbidden.Add(new int[] { x + sx, y + sy });
										forbidden.Add(new int[] { x - lx, y - ly });
									}
								}
							}
							else
							{
								int sideIndex = InTakenIndexRel(3,4);
								if (sideIndex < middleIndex)
								{
									circleDirectionLeft = (i == 0) ? true : false;
									List<int[]> countAreaBorderFields = new List<int[]> { new int[] {3,2}, new int[] {3,1}, new int[] {2,1}};
									if (!CountAreaRel(1, 1, 3, 3, countAreaBorderFields))
									{
										Across3impair = true;
										forbidden.Add(new int[] { x + sx, y + sy });
										forbidden.Add(new int[] { x - lx, y - ly });
									}
								}
							}
						}
						else
						{
							middleIndex = InBorderIndexRel(4,4);
							int farSideIndex = InBorderIndexRel(3,4);
							if (farSideIndex > middleIndex)
							{
								circleDirectionLeft = (i == 0) ? true : false;
								List<int[]> countAreaBorderFields = new List<int[]> { new int[] {3,2}, new int[] {3,1}, new int[] {2,1}};
								if (!CountAreaRel(1, 1, 3, 3, countAreaBorderFields))
								{
									Across3impair = true;
									forbidden.Add(new int[] { x + sx, y + sy });
									forbidden.Add(new int[] { x - lx, y - ly });
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
			T("Future2x2StartEnd: " + Future2x2StartEnd + "\n" + "Future2x3StartEnd: " + Future2x3StartEnd + "\n" + "Future3x3StartEnd: " + Future3x3StartEnd + "\n" + "FutureL: " + FutureL + "\n" + "CountAreaAcrossBorderC: " + CountAreaAcrossBorderC + "\n" + "DoubleCShape: " + DoubleCShape + "\n" + "Future2x2StartEnd9: " + Future2x2StartEnd9 + "\n" + "Future3x3StartEnd9: " + Future3x3StartEnd9 + "\n" + "FutureL9: " + FutureL9 + "\n" + "Square4x2: " + Square4x2 + "\n" + "Across3impair: " + Across3impair);
		}
	}
}