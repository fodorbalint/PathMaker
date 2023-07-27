using System;
using System.Diagnostics;

public class Class1
{
	public Class1()
	{
		int size = = int.Parse(Size.Text);
		int steps = int.Parse(Steps.Text);
		int minRadius = int.Parse(MinRadius.Text);
		int maxRadius = int.Parse(MaxRadius.Text);
		int minAngle = int.Parse(MinAngle.Text);
		int maxAngle = int.Parse(MaxAngle.Text);
		int currentAngle = 0;
		var rand = new Random();

		string startPos = (float)size / 2 + " " + (float)size / 2;
		string path = "";

		for (int i = 0; i < steps; i++)
		{
			int radius = rand.Next(minRadius, maxRadius + 1);
			int angle = rand.Next(0, 2);
			angle = angle == 0 ? -90 : 90;
			//int angle = rand.Next(minAngle, maxAngle + 1);

			Trace.WriteLine(angle + " " + currentAngle);

			double dx;
			double dy;

			if (angle >= 0)
			{
				dx = Math.Sin(angle * Math.PI / 180) * radius;
				dy = Math.Cos(angle * Math.PI / 180) * radius - radius;

				double newDx = Math.Cos(currentAngle * Math.PI / 180) * dx + Math.Sin(currentAngle * Math.PI / 180) * dy;
				double newDy = -Math.Sin(currentAngle * Math.PI / 180) * dx + Math.Cos(currentAngle * Math.PI / 180) * dy;

				path += "a " + radius + " " + radius + " 0 0 0 " + Math.Round(newDx, 3) + " " + Math.Round(newDy, 3) + "\r\n";
			}
			else
			{
				dx = Math.Sin(-angle * Math.PI / 180) * radius;
				dy = radius - Math.Cos(-angle * Math.PI / 180) * radius;

				double newDx = Math.Cos(currentAngle * Math.PI / 180) * dx + Math.Sin(currentAngle * Math.PI / 180) * dy;
				double newDy = -Math.Sin(currentAngle * Math.PI / 180) * dx + Math.Cos(currentAngle * Math.PI / 180) * dy;

				path += "a " + radius + " " + radius + " 0 0 1 " + Math.Round(newDx, 3) + " " + Math.Round(newDy, 3) + "\r\n";
			}

			currentAngle += angle;
		}
	}
}
