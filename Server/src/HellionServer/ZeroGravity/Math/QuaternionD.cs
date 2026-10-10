using System;

namespace ZeroGravity.Math;

public struct QuaternionD
{
	private const double DegreesToRadians = System.Math.PI / 180.0;

	private const double RadiansToDegrees = 180.0 / System.Math.PI;

	private const double DirectionEpsilon = 1E-06;

	public double X;

	public double Y;

	public double Z;

	public double W;

	public double this[int index]
	{
		get
		{
			return index switch
			{
				0 => X,
				1 => Y,
				2 => Z,
				3 => W,
				_ => throw new IndexOutOfRangeException("Invalid Quaternion index!"),
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
			case 3:
				W = value;
				break;
			default:
				throw new IndexOutOfRangeException("Invalid Quaternion index!");
			}
		}
	}

	public static QuaternionD Identity => new QuaternionD(0.0, 0.0, 0.0, 1.0);

	public Vector3D EulerAngles
	{
		get
		{
			return ToEulerRadians() * RadiansToDegrees;
		}
		set
		{
			this = FromEulerRadians(value * DegreesToRadians);
		}
	}

	public QuaternionD(double x, double y, double z, double w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public static QuaternionD operator *(QuaternionD lhs, QuaternionD rhs)
	{
		return new QuaternionD(lhs.W * rhs.X + lhs.X * rhs.W + lhs.Y * rhs.Z - lhs.Z * rhs.Y, lhs.W * rhs.Y + lhs.Y * rhs.W + lhs.Z * rhs.X - lhs.X * rhs.Z, lhs.W * rhs.Z + lhs.Z * rhs.W + lhs.X * rhs.Y - lhs.Y * rhs.X, lhs.W * rhs.W - lhs.X * rhs.X - lhs.Y * rhs.Y - lhs.Z * rhs.Z);
	}

	public static Vector3D operator *(QuaternionD rotation, Vector3D point)
	{
		double doubleX = rotation.X * 2.0;
		double doubleY = rotation.Y * 2.0;
		double doubleZ = rotation.Z * 2.0;
		double xx = rotation.X * doubleX;
		double yy = rotation.Y * doubleY;
		double zz = rotation.Z * doubleZ;
		double xy = rotation.X * doubleY;
		double xz = rotation.X * doubleZ;
		double yz = rotation.Y * doubleZ;
		double wx = rotation.W * doubleX;
		double wy = rotation.W * doubleY;
		double wz = rotation.W * doubleZ;
		Vector3D rotated = default(Vector3D);
		rotated.X = (1.0 - (yy + zz)) * point.X + (xy - wz) * point.Y + (xz + wy) * point.Z;
		rotated.Y = (xy + wz) * point.X + (1.0 - (xx + zz)) * point.Y + (yz - wx) * point.Z;
		rotated.Z = (xz - wy) * point.X + (yz + wx) * point.Y + (1.0 - (xx + yy)) * point.Z;
		return rotated;
	}

	public static bool operator ==(QuaternionD lhs, QuaternionD rhs)
	{
		return Dot(lhs, rhs) > 0.999998986721039;
	}

	public static bool operator !=(QuaternionD lhs, QuaternionD rhs)
	{
		return Dot(lhs, rhs) <= 0.999998986721039;
	}

	public static QuaternionD operator -(QuaternionD lhs, QuaternionD rhs)
	{
		return new QuaternionD(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z, lhs.W - rhs.W);
	}

	public static QuaternionD operator +(QuaternionD lhs, QuaternionD rhs)
	{
		return new QuaternionD(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z, lhs.W + rhs.W);
	}

	public void Set(double x, double y, double z, double w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public static double Dot(QuaternionD a, QuaternionD b)
	{
		return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
	}

	public static double Angle(QuaternionD a, QuaternionD b)
	{
		return System.Math.Acos(System.Math.Min(System.Math.Abs(Dot(a, b)), 1.0)) * 2.0 * RadiansToDegrees;
	}

	/// <summary>
	/// 	Rotation of the given angle in degrees about the given axis. A zero-length axis gives identity.
	/// </summary>
	public static QuaternionD AngleAxis(double angle, Vector3D axis)
	{
		axis.Normalize();
		if (axis.SqrMagnitude <= DirectionEpsilon)
		{
			return Identity;
		}

		double sinOfHalf = System.Math.Sin(angle * DegreesToRadians * 0.5);
		double cosOfHalf = System.Math.Cos(angle * DegreesToRadians * 0.5);
		return new QuaternionD(axis.X * sinOfHalf, axis.Y * sinOfHalf, axis.Z * sinOfHalf, cosOfHalf);
	}

	public void ToAngleAxis(out double angle, out Vector3D axis)
	{
		double axisLengthSquared = X * X + Y * Y + Z * Z;
		if (axisLengthSquared > DirectionEpsilon)
		{
			angle = 2.0 * System.Math.Acos(W);
			axis = new Vector3D(X, Y, Z) / System.Math.Sqrt(axisLengthSquared);
		}
		else
		{
			angle = 0.0;
			axis = new Vector3D(1.0, 0.0, 0.0);
		}

		angle *= RadiansToDegrees;
	}

	public static QuaternionD FromToRotation(Vector3D fromDirection, Vector3D toDirection)
	{
		return RotateTowards(LookRotation(fromDirection), LookRotation(toDirection), double.MaxValue);
	}

	public void SetFromToRotation(Vector3D fromDirection, Vector3D toDirection)
	{
		this = FromToRotation(fromDirection, toDirection);
	}

	public static QuaternionD LookRotation(Vector3D forward, Vector3D upwards)
	{
		forward = Vector3D.Normalize(forward);
		Vector3D right = Vector3D.Normalize(Vector3D.Cross(upwards, forward));
		upwards = Vector3D.Cross(forward, right);

		// Convert the basis, read as a rotation matrix, to a quaternion. Which branch is taken depends
		// on which diagonal term is largest, so that the square root never loses precision.
		QuaternionD rotation = default(QuaternionD);
		double trace = right.X + upwards.Y + forward.Z;
		if (trace > 0.0)
		{
			double root = System.Math.Sqrt(trace + 1.0);
			rotation.W = root * 0.5;
			root = 0.5 / root;
			rotation.X = (upwards.Z - forward.Y) * root;
			rotation.Y = (forward.X - right.Z) * root;
			rotation.Z = (right.Y - upwards.X) * root;
		}
		else if (right.X >= upwards.Y && right.X >= forward.Z)
		{
			double root = System.Math.Sqrt(1.0 + right.X - upwards.Y - forward.Z);
			double halfInverseRoot = 0.5 / root;
			rotation.X = 0.5 * root;
			rotation.Y = (right.Y + upwards.X) * halfInverseRoot;
			rotation.Z = (right.Z + forward.X) * halfInverseRoot;
			rotation.W = (upwards.Z - forward.Y) * halfInverseRoot;
		}
		else if (upwards.Y > forward.Z)
		{
			double root = System.Math.Sqrt(1.0 + upwards.Y - right.X - forward.Z);
			double halfInverseRoot = 0.5 / root;
			rotation.X = (upwards.X + right.Y) * halfInverseRoot;
			rotation.Y = 0.5 * root;
			rotation.Z = (forward.Y + upwards.Z) * halfInverseRoot;
			rotation.W = (forward.X - right.Z) * halfInverseRoot;
		}
		else
		{
			double root = System.Math.Sqrt(1.0 + forward.Z - right.X - upwards.Y);
			double halfInverseRoot = 0.5 / root;
			rotation.X = (forward.X + right.Z) * halfInverseRoot;
			rotation.Y = (forward.Y + upwards.Z) * halfInverseRoot;
			rotation.Z = 0.5 * root;
			rotation.W = (right.Y - upwards.X) * halfInverseRoot;
		}

		return rotation;
	}

	public static QuaternionD LookRotation(Vector3D forward)
	{
		return LookRotation(forward, Vector3D.Up);
	}

	public void SetLookRotation(Vector3D view)
	{
		SetLookRotation(view, Vector3D.Up);
	}

	public void SetLookRotation(Vector3D view, Vector3D up)
	{
		this = LookRotation(view, up);
	}

	public static QuaternionD Slerp(QuaternionD a, QuaternionD b, double t)
	{
		return SlerpUnclamped(a, b, MathHelper.Clamp(t, 0.0, 1.0));
	}

	public static QuaternionD SlerpUnclamped(QuaternionD a, QuaternionD b, double t)
	{
		double dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;

		// A negative dot means the rotations are more than half a turn apart; flipping one of them
		// takes the short way round instead of the long one.
		bool flipped = false;
		if (dot < 0.0)
		{
			flipped = true;
			dot = 0.0 - dot;
		}

		double weightA;
		double weightB;
		if (dot > 0.999999)
		{
			// Too close to interpolate as an arc without dividing by a vanishing sine.
			weightA = 1.0 - t;
			weightB = flipped ? 0.0 - t : t;
		}
		else
		{
			double angle = System.Math.Acos(dot);
			double inverseSine = 1.0 / System.Math.Sin(angle);
			weightA = System.Math.Sin((1.0 - t) * angle) * inverseSine;
			weightB = flipped ? (0.0 - System.Math.Sin(t * angle)) * inverseSine : System.Math.Sin(t * angle) * inverseSine;
		}

		QuaternionD result = default(QuaternionD);
		result.X = weightA * a.X + weightB * b.X;
		result.Y = weightA * a.Y + weightB * b.Y;
		result.Z = weightA * a.Z + weightB * b.Z;
		result.W = weightA * a.W + weightB * b.W;
		return result;
	}

	public static QuaternionD Lerp(QuaternionD a, QuaternionD b, double t)
	{
		return LerpUnclamped(a, b, MathHelper.Clamp(t, 0.0, 1.0));
	}

	public static QuaternionD LerpUnclamped(QuaternionD a, QuaternionD b, double t)
	{
		double inverseT = 1.0 - t;
		double dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;

		QuaternionD result = default(QuaternionD);
		if (dot >= 0.0)
		{
			result.X = inverseT * a.X + t * b.X;
			result.Y = inverseT * a.Y + t * b.Y;
			result.Z = inverseT * a.Z + t * b.Z;
			result.W = inverseT * a.W + t * b.W;
		}
		else
		{
			// Blend towards the flipped rotation so the result takes the shorter way round.
			result.X = inverseT * a.X - t * b.X;
			result.Y = inverseT * a.Y - t * b.Y;
			result.Z = inverseT * a.Z - t * b.Z;
			result.W = inverseT * a.W - t * b.W;
		}

		double inverseLength = 1.0 / System.Math.Sqrt(result.X * result.X + result.Y * result.Y + result.Z * result.Z + result.W * result.W);
		result.X *= inverseLength;
		result.Y *= inverseLength;
		result.Z *= inverseLength;
		result.W *= inverseLength;
		return result;
	}

	public static QuaternionD RotateTowards(QuaternionD from, QuaternionD to, double maxDegreesDelta)
	{
		double angle = Angle(from, to);
		if (angle == 0.0)
		{
			return to;
		}

		return SlerpUnclamped(from, to, System.Math.Min(1.0, maxDegreesDelta / angle));
	}

	/// <summary>
	/// 	Rotation that undoes this one. A zero-length rotation is returned unchanged.
	/// </summary>
	public static QuaternionD Inverse(QuaternionD rotation)
	{
		double lengthSquared = rotation.X * rotation.X + rotation.Y * rotation.Y + rotation.Z * rotation.Z + rotation.W * rotation.W;
		if (lengthSquared <= DirectionEpsilon)
		{
			return rotation;
		}

		double inverseLengthSquared = 1.0 / lengthSquared;
		return new QuaternionD(
			(0.0 - rotation.X) * inverseLengthSquared,
			(0.0 - rotation.Y) * inverseLengthSquared,
			(0.0 - rotation.Z) * inverseLengthSquared,
			rotation.W * inverseLengthSquared);
	}

	public static QuaternionD Euler(double x, double y, double z)
	{
		return FromEulerRadians(new Vector3D(x, y, z) * DegreesToRadians);
	}

	public static QuaternionD Euler(Vector3D euler)
	{
		return FromEulerRadians(euler * DegreesToRadians);
	}

	// Expanded form of qY * qX * qZ.
	private static QuaternionD FromEulerRadians(Vector3D euler)
	{
		double halfXAngle = euler.X * 0.5;
		double halfYAngle = euler.Y * 0.5;
		double halfZAngle = euler.Z * 0.5;
		double cx = System.Math.Cos(halfXAngle);
		double sx = System.Math.Sin(halfXAngle);
		double cy = System.Math.Cos(halfYAngle);
		double sy = System.Math.Sin(halfYAngle);
		double cz = System.Math.Cos(halfZAngle);
		double sz = System.Math.Sin(halfZAngle);
		return new QuaternionD(
			w: cx * cy * cz + sx * sy * sz,
			x: sx * cy * cz + cx * sy * sz,
			y: cx * sy * cz - sx * cy * sz,
			z: cx * cy * sz - sx * sy * cz
			);
	}

	/// <summary>
	/// 	Inverse of <see cref="FromEulerRadians"/>, read back off the rotation matrix. Looking straight
	/// 	up or down leaves the first and last angles interchangeable, so roll is pinned to zero there.
	/// </summary>
	private Vector3D ToEulerRadians()
	{
		double m00 = 1.0 - (2.0 * Y * Y + 2.0 * Z * Z);
		double m01 = 2.0 * Y * X - 2.0 * Z * W;
		double m02 = 2.0 * Z * X + 2.0 * Y * W;
		double m10 = 2.0 * Y * X + 2.0 * Z * W;
		double m11 = 1.0 - (2.0 * X * X + 2.0 * Z * Z);
		double m12 = 2.0 * Z * Y - 2.0 * X * W;
		double m22 = 1.0 - (2.0 * X * X + 2.0 * Y * Y);

		Vector3D euler = Vector3D.Zero;
		double pitch = 0.0 - System.Math.Asin(m12);
		if (pitch >= System.Math.PI / 2.0)
		{
			euler.Set(System.Math.PI / 2.0, System.Math.Atan2(m01, m00), 0.0);
		}
		else if (pitch <= -System.Math.PI / 2.0)
		{
			euler.Set(-System.Math.PI / 2.0, System.Math.Atan2(0.0 - m01, m00), 0.0);
		}
		else
		{
			euler.Set(pitch, System.Math.Atan2(m02, m22), System.Math.Atan2(m10, m11));
		}

		return euler;
	}

	public override string ToString()
	{
		return $"({X:0.###}, {Y:0.###}, {Z:0.###}, {W:0.###})";
	}

	public string ToString(string format)
	{
		return $"({X.ToString(format)}, {Y.ToString(format)}, {Z.ToString(format)}, {W.ToString(format)})";
	}

	public override int GetHashCode()
	{
		return X.GetHashCode() ^ (Y.GetHashCode() << 2) ^ (Z.GetHashCode() >> 2) ^ (W.GetHashCode() >> 1);
	}

	public override bool Equals(object other)
	{
		if (other is not QuaternionD quaternion)
		{
			return false;
		}

		return X.Equals(quaternion.X) && Y.Equals(quaternion.Y) && Z.Equals(quaternion.Z) && W.Equals(quaternion.W);
	}
}
