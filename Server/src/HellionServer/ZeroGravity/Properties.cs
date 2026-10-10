using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace ZeroGravity;

public class Properties
{
	private readonly Dictionary<string, string> _properties = new Dictionary<string, string>();

	private DateTime _propertiesChangedTime;

	private readonly string _fileName = "properties.ini";

	public Properties(string fileName)
	{
		_fileName = fileName;
		LoadProperties();
	}

	private void LoadProperties()
	{
		if (!File.Exists(_fileName))
			return;

		_propertiesChangedTime = File.GetLastWriteTime(_fileName);
		_properties.Clear();
		foreach (string row in File.ReadAllLines(_fileName))
		{
			if (row.IsNullOrEmpty() || row.TrimStart().StartsWith('#'))
				continue;

			string[] parts = row.Split("=".ToCharArray(), 2);
			if (parts.Length == 2)
				_properties[parts[0].Trim().ToLower()] = parts[1];
		}
	}

	private void ReloadIfChanged()
	{
		if (File.Exists(_fileName) && File.GetLastWriteTime(_fileName) != _propertiesChangedTime)
			LoadProperties();
	}

	/// <summary>
	/// 	Gets a property with value <c>value</c>.
	/// </summary>
	/// <typeparam name="T">The value type</typeparam>
	/// <param name="propertyName">Name of the property</param>
	/// <param name="value">A nullable value</param>
	/// <returns>True if property was found, false if not</returns>
	public bool TryGetProperty<T>(string propertyName, out T value)
	{
		ReloadIfChanged();
		value = default;

		if (!_properties.TryGetValue(propertyName.ToLower(), out string rawValue))
			return false;

		TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
		if (!converter.IsValid(rawValue))
			return false;

		value = (T)converter.ConvertFrom(rawValue);
		return true;
	}

	/// <summary>
	/// 	Gets a property, but only sets value when it succeeds.
	/// </summary>
	/// <typeparam name="T">The value type</typeparam>
	/// <param name="propertyName">Name of the property</param>
	/// <param name="value">The variable you want to mutate</param>
	/// <returns>True if property was found, false if not</returns>
	public bool TryGetPropertySafe<T>(string propertyName, ref T value)
	{
		ReloadIfChanged();

		if (!_properties.TryGetValue(propertyName.ToLower(), out string rawValue))
			return false;

		TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
		if (!converter.IsValid(rawValue))
			return false;

		value = (T)converter.ConvertFrom(rawValue);
		return true;
	}

	public T GetProperty<T>(string propertyName)
	{
		TryGetProperty(propertyName, out T value);
		return value;
	}

	public void SetProperty(string name, string value)
	{
		_properties[name.Trim().ToLower()] = value;
	}
}
