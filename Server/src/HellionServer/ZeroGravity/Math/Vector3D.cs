using System;

namespace ZeroGravity.Math;

public struct Vector3D
{
	private const double DirectionEpsilon = 1E-06;

	public double X;

	public double Y;

	public double Z;

	public static Vector3D Back => new Vector3D(0.0, 0.0, -1.0);

	public static Vector3D Down => new Vector3D(0.0, -1.0, 0.0);

	public static Vector3D Forward => new Vector3D(0.0, 0.0, 1.0);

	public static Vector3D Left => new Vector3D(-1.0, 0.0, 0.0);

	public static Vector3D One => new Vector3D(1.0, 1.0, 1.0);

	public static Vector3D Right => new Vector3D(1.0, 0.0, 0.0);

	public static Vector3D Up => new Vector3D(0.0, 1.0, 0.0);

	public static Vector3D Zero => new Vector3D(0.0, 0.0, 0.0);

	public double Magnitude => System.Math.Sqrt(X * X + Y * Y + Z * Z);

	public double SqrMagnitude => X * X + Y * Y + Z * Z;

	public Vector3D Normalized => Normalize(this);

	public double this[int index]
	{
		get
		{
			return index switch
			{
				0 => X,
				1 => Y,
				2 => Z,
				_ => throw new IndexOutOfRangeException("Invalid Vector3 index!"),
			};
		}
		set
		{
			switch (index)
			{
			case 0:
				X = value;
				break;
			case 1:
				Y = value;
				break;
			case 2:
				Z = value;
				break;
			default:
				throw new IndexOutOfRangeException("Invalid Vector3 index!");
			}
		}
	}

	public Vector3D(double x, double y, double z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public Vector3D(double x, double y)
	{
		X = x;
		Y = y;
		Z = 0.0;
	}

	public static Vector3D operator +(Vector3D lhs, Vector3D rhs)
	{
		return new Vector3D(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
	}

	public static Vector3D operator -(Vector3D lhs, Vector3D rhs)
	{
		return new Vector3D(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z);
	}

	public static Vector3D operator -(Vector3D value)
	{
		return new Vector3D(0.0 - value.X, 0.0 - value.Y, 0.0 - value.Z);
	}

	public static Vector3D operator *(double scalar, Vector3D value)
	{
		return new Vector3D(value.X * scalar, value.Y * scalar, value.Z * scalar);
	}

	public static Vector3D operator *(Vector3D value, double scalar)
	{
		return new Vector3D(value.X * scalar, value.Y * scalar, value.Z * scalar);
	}

	public static Vector3D operator /(Vector3D value, double scalar)
	{
		return new Vector3D(value.X / scalar, value.Y / scalar, value.Z / scalar);
	}

	public static bool operator ==(Vector3D lhs, Vector3D rhs)
	{
		return (lhs - rhs).SqrMagnitude < 9.999999E-11;
	}

	public static bool operator !=(Vector3D lhs, Vector3D rhs)
	{
		return (lhs - rhs).SqrMagnitude >= 9.999999E-11;
	}

	public static double Angle(Vector3D from, Vector3D to)
	{
		return System.Math.Acos(MathHelper.Clamp(Dot(from.Normalized, to.Normalized), -1.0, 1.0)) * (180.0 / System.Math.PI);
	}

	public static Vector3D ClampMagnitude(Vector3D value, double maxLength)
	{
		if (value.SqrMagnitude > maxLength * maxLength)
		{
			return value.Normalized * maxLength;
		}

		return value;
	}

	public static Vector3D Cross(Vector3D lhs, Vector3D rhs)
	{
		return new Vector3D(lhs.Y * rhs.Z - lhs.Z * rhs.Y, lhs.Z * rhs.X - lhs.X * rhs.Z, lhs.X * rhs.Y - lhs.Y * rhs.X);
	}

	public static double Distance(Vector3D a, Vector3D b)
	{
		Vector3D delta = new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		return System.Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
	}

	public static double DistanceSquared(Vector3D a, Vector3D b)
	{
		Vector3D delta = new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		return delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
	}

	public static double Dot(Vector3D lhs, Vector3D rhs)
	{
		return lhs.X * rhs.X + lhs.Y * rhs.Y + lhs.Z * rhs.Z;
	}

	public static Vector3D Lerp(Vector3D a, Vector3D b, double t)
	{
		t = MathHelper.Clamp(t, 0.0, 1.0);
		return new Vector3D(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);
	}

	public static Vector3D LerpUnclamped(Vector3D a, Vector3D b, double t)
	{
		return new Vector3D(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);
	}

	public static Vector3D Max(Vector3D lhs, Vector3D rhs)
	{
		return new Vector3D(System.Math.Max(lhs.X, rhs.X), System.Math.Max(lhs.Y, rhs.Y), System.Math.Max(lhs.Z, rhs.Z));
	}

	public static Vector3D Min(Vector3D lhs, Vector3D rhs)
	{
		return new Vector3D(System.Math.Min(lhs.X, rhs.X), System.Math.Min(lhs.Y, rhs.Y), System.Math.Min(lhs.Z, rhs.Z));
	}

	/// <summary>
	/// 	Returns a vector that is made from the absolute value of each of the components of the input vector (makes all values positive).
	/// </summary>
	/// <param name="value">Value to make absolute.</param>
	/// <returns></returns>
	public static Vector3D Abs(Vector3D value)
	{
		return new Vector3D(System.Math.Abs(value.X), System.Math.Abs(value.Y), System.Math.Abs(value.Z));
	}

	public static Vector3D MoveTowards(Vector3D current, Vector3D target, double maxDistanceDelta)
	{
		Vector3D delta = target - current;
		double distance = delta.Magnitude;
		if (distance <= maxDistanceDelta || distance == 0.0)
		{
			return target;
		}

		return current + delta / distance * maxDistanceDelta;
	}

	public static Vector3D Normalize(Vector3D value)
	{
		double magnitude = value.Magnitude;
		if (magnitude > DirectionEpsilon)
		{
			return value / magnitude;
		}

		return Zero;
	}

	/// <summary>
	/// 	Makes the two directions unit length and perpendicular, keeping <paramref name="normal"/> fixed.
	/// </summary>
	public static void OrthoNormalize(ref Vector3D normal, ref Vector3D tangent)
	{
		normal.Normalize();
		double tangentAlongNormal = Dot(normal, tangent);
		tangent -= tangentAlongNormal * normal;
		tangent.Normalize();
	}

	/// <summary>
	/// 	Makes the three directions a unit-length orthogonal basis, keeping <paramref name="normal"/>
	/// 	fixed and moving <paramref name="tangent"/> as little as it can.
	/// </summary>
	public static void OrthoNormalize(ref Vector3D normal, ref Vector3D tangent, ref Vector3D binormal)
	{
		normal.Normalize();
		double tangentAlongNormal = Dot(normal, tangent);
		tangent -= tangentAlongNormal * normal;
		tangent.Normalize();

		double binormalAlongTangent = Dot(tangent, binormal);
		double binormalAlongNormal = Dot(normal, binormal);
		binormal -= binormalAlongNormal * normal + binormalAlongTangent * tangent;
		binormal.Normalize();
	}

	public static Vector3D Project(Vector3D value, Vector3D onNormal)
	{
		double normalLengthSquared = Dot(onNormal, onNormal);
		if (normalLengthSquared < double.Epsilon)
		{
			return Zero;
		}

		return onNormal * Dot(value, onNormal) / normalLengthSquared;
	}

	public static Vector3D ProjectOnPlane(Vector3D value, Vector3D planeNormal)
	{
		return value - Project(value, planeNormal);
	}

	public static Vector3D Reflect(Vector3D inDirection, Vector3D inNormal)
	{
		return -2.0 * Dot(inNormal, inDirection) * inNormal + inDirection;
	}

	public static Vector3D Scale(Vector3D lhs, Vector3D rhs)
	{
		return new Vector3D(lhs.X * rhs.X, lhs.Y * rhs.Y, lhs.Z * rhs.Z);
	}

	public static Vector3D SmoothDamp(Vector3D current, Vector3D target, ref Vector3D currentVelocity, double smoothTime, double deltaTime)
	{
		return SmoothDamp(current, target, ref currentVelocity, smoothTime, double.PositiveInfinity, deltaTime);
	}

	public static Vector3D SmoothDamp(Vector3D current, Vector3D target, ref Vector3D currentVelocity, double smoothTime, double maxSpeed, double deltaTime)
	{
		smoothTime = System.Math.Max(0.0001, smoothTime);
		double frequency = 2.0 / smoothTime;
		double step = frequency * deltaTime;

		// Rational stand-in for exp(-step), cheaper than the real thing and close enough over a frame.
		double decay = 1.0 / (1.0 + step + 0.48 * step * step + 0.235 * step * step * step);

		Vector3D offset = current - target;
		Vector3D originalTarget = target;

		// Capping the offset is what bounds the speed, since the spring pulls in proportion to it.
		offset = ClampMagnitude(offset, maxSpeed * smoothTime);
		target = current - offset;

		Vector3D displacementStep = (currentVelocity + frequency * offset) * deltaTime;
		currentVelocity = (currentVelocity - frequency * displacementStep) * decay;
		Vector3D result = target + (offset + displacementStep) * decay;

		// Overshot the target: stop dead on it instead of springing past.
		if (Dot(originalTarget - current, result - originalTarget) > 0.0)
		{
			result = originalTarget;
			currentVelocity = (result - originalTarget) / deltaTime;
		}

		return result;
	}

	public void Normalize()
	{
		double magnitude = Magnitude;
		if (magnitude > DirectionEpsilon)
		{
			this /= magnitude;
		}
		else
		{
			this = Zero;
		}
	}

	public void Scale(Vector3D scale)
	{
		X *= scale.X;
		Y *= scale.Y;
		Z *= scale.Z;
	}

	public void Set(double x, double y, double z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public override string ToString()
	{
		return $"({X:0.###}, {Y:0.###}, {Z:0.###})";
	}

	public string ToString(string format)
	{
		return $"({X.ToString(format)}, {Y.ToString(format)}, {Z.ToString(format)})";
	}

	public override int GetHashCode()
	{
		return X.GetHashCode() ^ (Y.GetHashCode() << 2) ^ (Z.GetHashCode() >> 2);
	}

	public override bool Equals(object other)
	{
		if (other is not Vector3D vector)
		{
			return false;
		}

		return X.Equals(vector.X) && Y.Equals(vector.Y) && Z.Equals(vector.Z);
	}
}
