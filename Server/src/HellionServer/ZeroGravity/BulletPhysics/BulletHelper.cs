using BulletSharp.Math;
using ZeroGravity.Math;

namespace ZeroGravity.BulletPhysics;

public static class BulletHelper
{
	public static Vector3[] GetVertices(this float[] v)
	{
		Vector3[] ver = new Vector3[v.Length / 3];
		int i = 0;
		int j = 0;
		for (; i < v.Length / 3; i++)
		{
			ver[i] = new Vector3(v[j++], v[j++], v[j++]);
		}
		return ver;
	}

	public static void AffineTransformation(float scaling, ref Quaternion rotation, ref Vector3 translation, out BulletSharp.Math.Matrix result)
	{
		result = Scaling(scaling) * Matrix.RotationQuaternion(rotation) * Matrix.Translation(translation);
	}

	public static BulletSharp.Math.Matrix AffineTransformation(float scaling, Quaternion rotation, Vector3 translation)
	{
		AffineTransformation(scaling, ref rotation, ref translation, out var result);
		return result;
	}

	public static BulletSharp.Math.Matrix Scaling(float scale)
	{
		Scaling(scale, out var result);
		return result;
	}

	public static void Scaling(float scale, out BulletSharp.Math.Matrix result)
	{
		result = Matrix.Identity;
		result.M11 = result.M22 = result.M33 = scale;
	}

	public static Quaternion LookRotation(Vector3 forward, Vector3 up)
	{
		forward.Normalize();
		Vector3 vector = Vector3.Normalize(forward);
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(up, vector));
		Vector3 vector3 = Vector3.Cross(vector, vector2);
		double m0 = vector2.X;
		double m = vector2.Y;
		double m2 = vector2.Z;
		double m3 = vector3.X;
		double m4 = vector3.Y;
		double m5 = vector3.Z;
		double m6 = vector.X;
		double m7 = vector.Y;
		double m8 = vector.Z;
		double num8 = m0 + m4 + m8;
		Quaternion quaternion = default(Quaternion);
		if (num8 > 0.0)
		{
			double num = System.Math.Sqrt(num8 + 1.0);
			quaternion.W = num * 0.5;
			num = 0.5 / num;
			quaternion.X = (m5 - m7) * num;
			quaternion.Y = (m6 - m2) * num;
			quaternion.Z = (m - m3) * num;
			return quaternion;
		}
		if (m0 >= m4 && m0 >= m8)
		{
			double num7 = System.Math.Sqrt(1.0 + m0 - m4 - m8);
			double num4 = 0.5 / num7;
			quaternion.X = 0.5 * num7;
			quaternion.Y = (m + m3) * num4;
			quaternion.Z = (m2 + m6) * num4;
			quaternion.W = (m5 - m7) * num4;
			return quaternion;
		}
		if (m4 > m8)
		{
			double num6 = System.Math.Sqrt(1.0 + m4 - m0 - m8);
			double num3 = 0.5 / num6;
			quaternion.X = (m3 + m) * num3;
			quaternion.Y = 0.5 * num6;
			quaternion.Z = (m7 + m5) * num3;
			quaternion.W = (m6 - m2) * num3;
			return quaternion;
		}
		double num5 = System.Math.Sqrt(1.0 + m8 - m0 - m4);
		double num2 = 0.5 / num5;
		quaternion.X = (m6 + m2) * num2;
		quaternion.Y = (m7 + m5) * num2;
		quaternion.Z = 0.5 * num5;
		quaternion.W = (m - m3) * num2;
		return quaternion;
	}

	public static Vector3 Up(this BulletSharp.Math.Matrix m)
	{
		return new Vector3(m.M21, m.M22, m.M23);
	}

	public static Vector3 Forward(this BulletSharp.Math.Matrix m)
	{
		return new Vector3(m.M31, m.M32, m.M33);
	}

	public static Vector3D ToVector3D(this Vector3 v)
	{
		return new Vector3D(v.X, v.Y, v.Z);
	}

	public static Vector3 ToVector3(this Vector3D v)
	{
		return new Vector3(v.X, v.Y, v.Z);
	}

	public static QuaternionD ToQuaternionD(this Quaternion q)
	{
		return new QuaternionD(q.X, q.Y, q.Z, q.W);
	}

	public static Quaternion ToQuaternion(this QuaternionD q)
	{
		return new Quaternion(q.X, q.Y, q.Z, q.W);
	}

	public static Vector3 GetLocalVector(this BulletSharp.Math.Matrix m, Vector3 globalVector)
	{
		Vector3 ret = default(Vector3);
		ret.X = m.M11 * ret.X + m.M12 * ret.Y + m.M13 + ret.Z;
		ret.Y = m.M21 * ret.X + m.M22 * ret.Y + m.M23 + ret.Z;
		ret.X = m.M31 * ret.X + m.M32 * ret.Y + m.M33 + ret.Z;
		return ret;
	}
}
