namespace OneWayLabyrinth
{
	public partial class Path
	{
		bool CShape = false;
		bool Future2x2StartEnd = false;
		bool Sideback = false;
		bool Sidefront = false;

		public void RunRules()
		{
			if (size >= 5)
			{
				// C-Shape
				if (InTakenRel(1,-1) && (InTakenRel(2,0) || InBorderRel(2,0)) && !InTakenRel(1,0) && !InBorderRel(1,0))
				{
					CShape = true;
					forbidden.Add(new int[] { x - lx, y - ly });
					forbidden.Add(new int[] { x + sx, y + sy });
				}

			}

			if (size >= 7)
			{
				// Future 2 x 2 Start End
				if (InTakenRel(0,3) && InTakenRel(-1,2) && InTakenRel(-1,1) && InFutureStart(1,0) && InFutureEnd(3,0) && (InTakenRel(4,1) || InBorderRel(4,1)))
				{
					Future2x2StartEnd = true;
					forbidden.Add(new int[] { x + lx, y + ly });
				}

				// Side back
				if (!InTakenRel(1,0) && !InBorderRel(1,0) && !InTakenRel(1,-1) && !InBorderRel(1,-1) && InTakenRel(2,-1))
				{
					Sideback = true;
					forbidden.Add(new int[] { x + lx, y + ly });
				}

				// Side front
				if (!InTakenRel(1,0) && !InBorderRel(1,0) && !InTakenRel(1,1) && !InBorderRel(1,1) && InTakenRel(2,1))
				{
					Sidefront = true;
					forbidden.Add(new int[] { x + lx, y + ly });
				}

			}

		}
	}
}