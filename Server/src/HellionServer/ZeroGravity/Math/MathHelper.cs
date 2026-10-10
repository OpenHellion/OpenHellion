using System;

namespace ZeroGravity.Math;

public static class MathHelper
{
	public const double RadToDeg = 180.0 / System.Math.PI;

	public const double DegToRad = System.Math.PI / 180.0;

	private static Random _randGenerator = new Random();

	public static int Clamp(int value, int min, int max)
	{
		return value < min ? min : value > max ? max : value;
	}

	public static float Clamp(float value, float min, float max)
	{
		return value < min ? min : value > max ? max : value;
	}

	public static double Clamp(double value, double min, double max)
	{
		return value < min ? min : value > max ? max : value;
	}

	public static float Lerp(float value1, float value2, float amount)
	{
		return value1 + (value2 - value1) * Clamp(amount, 0f, 1f);
	}

	public static double Lerp(double value1, double value2, double amount)
	{
		return value1 + (value2 - value1) * Clamp(amount, 0.0, 1.0);
	}

	public static float LerpValue(float fromVelocity, float toVelocity, float lerpAmount, float epsilon = 0.01f)
	{
		if (fromVelocity != toVelocity)
		{
			fromVelocity = !(fromVelocity < toVelocity) ? System.Math.Max(fromVelocity + (toVelocity - fromVelocity) * Clamp(lerpAmount, 0f, 1f), toVelocity) : System.Math.Min(fromVelocity + (toVelocity - fromVelocity) * Clamp(lerpAmount, 0f, 1f), toVelocity);
			if (System.Math.Abs(toVelocity - fromVelocity) < epsilon)
			{
				fromVelocity = toVelocity;
			}
		}
		return fromVelocity;
	}

	public static double SmoothStep(double value1, double value2, double amount)
	{
		return Hermite(value1, 0.0, value2, 0.0, Clamp(amount, 0.0, 1.0));
	}

	public static int Sign(double value)
	{
		if (value < 0.0)
		{
			return -1;
		}
		return 1;
	}

	public static double Hermite(double value1, double tangent1, double value2, double tangent2, double amount)
	{
		double amountCubed = amount * amount * amount;
		double amountSquared = amount * amount;
		if (amount == 0.0)
		{
			return value1;
		}
		if (amount == 1.0)
		{
			return value2;
		}
		return (2.0 * value1 - 2.0 * value2 + tangent2 + tangent1) * amountCubed + (3.0 * value2 - 3.0 * value1 - 2.0 * tangent1 - tangent2) * amountSquared + tangent1 * amount + value1;
	}

	public static float ProportionalValue(float basedOnCurrent, float basedOnMin, float basedOnMax, float resultMin, float resoultMax)
	{
		return resultMin + (resoultMax - resultMin) * ((basedOnCurrent - basedOnMin) / (basedOnMax - basedOnMin));
	}

	public static double ProportionalValueDouble(double basedOnCurrent, double basedOnMin, double basedOnMax, double resultMin, double resoultMax)
	{
		return resultMin + (resoultMax - resultMin) * ((basedOnCurrent - basedOnMin) / (basedOnMax - basedOnMin));
	}

	public static Vector3D ProportionalValue(Vector3D basedOnCurrent, Vector3D basedOnMin, Vector3D basedOnMax, Vector3D resultMin, Vector3D resoultMax)
	{
		return resultMin + (resoultMax - resultMin) * ((basedOnCurrent - basedOnMin).Magnitude / (basedOnMax - basedOnMin).Magnitude);
	}

	public static float SetEpsilonZero(float value, float epsilon = float.Epsilon)
	{
		return System.Math.Abs(value) > epsilon ? value : 0f;
	}

	public static long LongRandom(long min, long max, Random rand)
	{
		byte[] buf = new byte[8];
		rand.NextBytes(buf);
		long longRand = BitConverter.ToInt64(buf, 0);
		return System.Math.Abs(longRand % (max - min)) + min;
	}

	public static double AngleSigned(Vector3D vec1, Vector3D vec2, Vector3D planeNormal)
	{
		return Vector3D.Angle(vec1, vec2) * Sign(Vector3D.Dot(planeNormal, Vector3D.Cross(vec1, vec2)));
	}

	public static Vector3D RotateAroundPivot(Vector3D vector, Vector3D pivot, Vector3D angles)
	{
		return QuaternionD.Euler(angles) * (vector - pivot) + pivot;
	}

	public static float AverageMaxValue(float a, float b, float c, float maxA, float maxB, float maxC)
	{
		return (a + b + c) / (a / maxA + b / maxB + c / maxC);
	}

	public static double Acosh(double x)
	{
		return System.Math.Log(x + System.Math.Sqrt(x * x - 1.0));
	}

	public static float RandomRange(float min, float max)
	{
		return (float)(_randGenerator.NextDouble() * (max - min) + min);
	}

	public static double RandomRange(double min, double max)
	{
		return _randGenerator.NextDouble() * (max - min) + min;
	}

	public static int RandomRange(int min, int max)
	{
		return _randGenerator.Next(min, max);
	}

	public static double RandomNextDouble()
	{
		return _randGenerator.NextDouble();
	}

	public static int RandomNextInt()
	{
		return _randGenerator.Next();
	}

	public static QuaternionD RandomRotation()
	{
		return QuaternionD.Euler(RandomRange(0.0, 359.99), RandomRange(0.0, 359.99), RandomRange(0.0, 359.99));
	}
}
