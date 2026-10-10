using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ZeroGravity;

public static class ClassHasher
{
	public static uint GetClassHashCode(Type type)
	{
		HashSet<Type> classes = new HashSet<Type>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly asm in assemblies)
		{
			Type[] types = asm.GetTypes();
			foreach (Type t in types)
			{
				if ((type.IsClass && t.IsSubclassOf(type)) || (type.IsInterface && t.GetInterfaces().Contains(type)))
				{
					addClass(t, classes, type.Namespace);
				}
			}
		}
		Type[] array = new Type[classes.Count];
		classes.CopyTo(array);
		Array.Sort(array, (Type x, Type y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		string str = "";
		Type[] array2 = array;
		foreach (Type t2 in array2)
		{
			str = str + t2.Name + ":";
			addHashingData(t2, ref str, type.Namespace, new HashSet<Type>());
			str += "\r\n";
		}
		uint hashedValue = 744748791u;
		unchecked
		{
			for (int i = 0; i < str.Length; i++)
			{
				hashedValue += str[i];
				hashedValue *= 3045351289u;
			}
		}
		return hashedValue;
	}

	private static void addClass(Type type, HashSet<Type> classes, string nspace)
	{
		if ((!type.IsClass && !type.IsInterface && !type.IsEnum) || type.IsNested || !(type.Namespace == nspace))
		{
			return;
		}
		if (type.IsArray)
		{
			type = type.GetElementType();
		}
		if (type.IsInterface)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly asm in assemblies)
			{
				Type[] types = asm.GetTypes();
				foreach (Type t in types)
				{
					if (t.GetInterfaces().Contains(type))
					{
						addClass(t, classes, nspace);
					}
				}
			}
		}
		else if (type.IsEnum)
		{
			classes.Add(type);
		}
		else
		{
			if (!classes.Add(type))
			{
				return;
			}
			MemberInfo[] members = type.GetMembers();
			foreach (MemberInfo member in members)
			{
				if (member.MemberType == MemberTypes.Field)
				{
					addClass((member as FieldInfo).FieldType, classes, nspace);
				}
				else if (member.MemberType == MemberTypes.Property)
				{
					addClass((member as PropertyInfo).PropertyType, classes, nspace);
				}
				else if (member.MemberType == MemberTypes.Method)
				{
					ParameterInfo[] parameters = (member as MethodInfo).GetParameters();
					foreach (ParameterInfo par2 in parameters)
					{
						addClass(par2.ParameterType, classes, nspace);
					}
				}
				else if (member.MemberType == MemberTypes.Constructor)
				{
					ParameterInfo[] parameters2 = (member as ConstructorInfo).GetParameters();
					foreach (ParameterInfo par in parameters2)
					{
						addClass(par.ParameterType, classes, nspace);
					}
				}
			}
		}
	}

	private static void addHashingData(Type type, ref string str, string nspace, HashSet<Type> visited)
	{
		if (!visited.Add(type))
		{
			return;
		}
		if (type.IsEnum)
		{
			string[] names = Enum.GetNames(type);
			foreach (string s in names)
			{
				str = str + s + "|";
			}
			return;
		}
		MemberInfo[] members = type.GetMembers();
		Array.Sort(members, (MemberInfo x, MemberInfo y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		bool first = true;
		MemberInfo[] array = members;
		foreach (MemberInfo member in array)
		{
			if (!(member.DeclaringType == type))
			{
				continue;
			}
			string membStr = member.Name;
			if (member.MemberType == MemberTypes.Field)
			{
				addHashingDataMember((member as FieldInfo).FieldType, ref str, nspace, visited);
			}
			else if (member.MemberType == MemberTypes.Property)
			{
				addHashingDataMember((member as PropertyInfo).PropertyType, ref str, nspace, visited);
			}
			else if (member.MemberType == MemberTypes.Method)
			{
				str = str + " " + (member as MethodInfo).ReturnType.ToString();
				ParameterInfo[] parameters = (member as MethodInfo).GetParameters();
				foreach (ParameterInfo par2 in parameters)
				{
					str = str + " " + par2.Name;
					addHashingDataMember(par2.ParameterType, ref str, nspace, visited);
				}
			}
			else if (member.MemberType == MemberTypes.Constructor)
			{
				if ((member as ConstructorInfo).GetParameters().Length == 0)
				{
					continue;
				}
				ParameterInfo[] parameters2 = (member as ConstructorInfo).GetParameters();
				foreach (ParameterInfo par in parameters2)
				{
					str = str + " " + par.Name;
					addHashingDataMember(par.ParameterType, ref str, nspace, visited);
				}
			}
			str = str + (!first ? ", " : " ") + membStr;
			first = false;
		}
	}

	private static void addHashingDataMember(Type t, ref string str, string nspace, HashSet<Type> visited)
	{
		if (t.IsPrimitive)
		{
			str = str + " " + t.Name;
		}
		if (t.IsClass && !t.IsNested && t.Namespace == nspace)
		{
			addHashingData(t, ref str, nspace, visited);
		}
	}
}
