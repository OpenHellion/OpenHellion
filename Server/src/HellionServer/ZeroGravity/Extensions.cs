using System;
using System.Threading.Tasks;
using ZeroGravity.Math;

namespace ZeroGravity;

public static class Extensions
{
	public static double[] ToArray(this Vector3D v)
	{
		return new double[3] { v.X, v.Y, v.Z };
	}

	public static double[] ToArray(this QuaternionD q)
	{
		return new double[4] { q.X, q.Y, q.Z, q.W };
	}

	public static float[] ToFloatArray(this Vector3D v)
	{
		return new float[3]
		{
			(float)v.X,
			(float)v.Y,
			(float)v.Z
		};
	}

	public static float[] ToFloatArray(this QuaternionD q)
	{
		return new float[4]
		{
			(float)q.X,
			(float)q.Y,
			(float)q.Z,
			(float)q.W
		};
	}

	public static Vector3D ToVector3D(this float[] arr)
	{
		if (arr.Length == 3)
		{
			return new Vector3D(arr[0], arr[1], arr[2]);
		}
		return Vector3D.Zero;
	}

	public static Vector3D ToVector3D(this double[] arr)
	{
		if (arr.Length == 3)
		{
			return new Vector3D(arr[0], arr[1], arr[2]);
		}
		return Vector3D.Zero;
	}

	public static QuaternionD ToQuaternionD(this float[] arr)
	{
		if (arr.Length == 4)
		{
			return new QuaternionD(arr[0], arr[1], arr[2], arr[3]);
		}
		return QuaternionD.Identity;
	}

	public static QuaternionD ToQuaternionD(this double[] arr)
	{
		if (arr.Length == 4)
		{
			return new QuaternionD(arr[0], arr[1], arr[2], arr[3]);
		}
		return QuaternionD.Identity;
	}

	public static bool IsNotEpsilonZero(this float val, float epsilon = float.Epsilon)
	{
		return val > epsilon || val < 0f - epsilon;
	}

	public static bool IsNotEpsilonZeroD(this double val, double epsilon = double.Epsilon)
	{
		return val > epsilon || val < 0.0 - epsilon;
	}

	public static bool IsNotEpsilonZero(this Vector3D value, double epsilon = double.Epsilon)
	{
		return System.Math.Abs(value.X) > epsilon || System.Math.Abs(value.Y) > epsilon || System.Math.Abs(value.Z) > epsilon;
	}

	public static bool IsEpsilonZero(this Vector3D value, double epsilon = double.Epsilon)
	{
		return System.Math.Abs(value.X) <= epsilon && System.Math.Abs(value.Y) <= epsilon && System.Math.Abs(value.Z) <= epsilon;
	}

	public static bool IsInfinity(this Vector3D value)
	{
		return double.IsInfinity(value.X) || double.IsInfinity(value.Y) || double.IsInfinity(value.Z);
	}

	public static bool IsNaN(this Vector3D value)
	{
		return double.IsNaN(value.X) || double.IsNaN(value.Y) || double.IsNaN(value.Z);
	}

	public static bool IsEpsilonEqual(this float val, float other, float epsilon = float.Epsilon)
	{
		return !(System.Math.Abs(val - other) > epsilon);
	}

	public static bool IsEpsilonEqualD(this double val, double other, double epsilon = double.Epsilon)
	{
		return !(System.Math.Abs(val - other) > epsilon);
	}

	public static bool IsEpsilonEqual(this Vector3D val, Vector3D other, double epsilon = double.Epsilon)
	{
		return !(System.Math.Abs(val.X - other.X) > epsilon) && !(System.Math.Abs(val.Y - other.Y) > epsilon) && !(System.Math.Abs(val.Z - other.Z) > epsilon);
	}

	public static bool IsEpsilonEqual(this QuaternionD val, QuaternionD other, double epsilon = double.Epsilon)
	{
		return !(System.Math.Abs(val.X - other.X) > epsilon) && !(System.Math.Abs(val.Y - other.Y) > epsilon) && !(System.Math.Abs(val.Z - other.Z) > epsilon) && !(System.Math.Abs(val.W - other.W) > epsilon);
	}

	public static Vector3D FromOther(this Vector3D vec, Vector3D other)
	{
		vec.X = other.X;
		vec.Y = other.Y;
		vec.Z = other.Z;
		return vec;
	}

	public static bool IsValid(this Vector3D v)
	{
		if (double.IsNaN(v.X) || double.IsInfinity(v.X) || double.IsNaN(v.Y) || double.IsInfinity(v.Y) || double.IsNaN(v.Z) || double.IsInfinity(v.Z))
		{
			return false;
		}
		return true;
	}

	public static Vector3D RotateAroundPivot(this Vector3D vector, Vector3D pivot, Vector3D angles)
	{
		return QuaternionD.Euler(angles) * (vector - pivot) + pivot;
	}

	public static double DistanceSquared(this Vector3D a, Vector3D b)
	{
		Vector3D vector = new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		return vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z;
	}

	public static QuaternionD Inverse(this QuaternionD value)
	{
		return QuaternionD.Inverse(value);
	}

	public static void Invoke(Action continuationFunction, double time)
	{
		Task delay = Task.Delay(TimeSpan.FromSeconds(time)).ContinueWith(delegate
		{
			Task.Run(continuationFunction);
		});
	}

	public static bool IsNullOrEmpty(this string val)
	{
		return string.IsNullOrEmpty(val);
	}

	// https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/#a-deterministic-gethashcode-implementation
	public static int GetDeterministicHashCode(this string str)
	{
	    unchecked
	    {
	        int hash1 = (5381 << 16) + 5381;
	        int hash2 = hash1;

	        for (int i = 0; i < str.Length; i += 2)
	        {
	            hash1 = ((hash1 << 5) + hash1) ^ str[i];
	            if (i == str.Length - 1)
	                break;
	            hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
	        }

	        return hash1 + hash2 * 1566083941;
	    }
	}
}
