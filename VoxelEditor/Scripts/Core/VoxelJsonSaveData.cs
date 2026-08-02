using Godot;
using System;
using System.Collections.Generic;
using VoxelEditorForGodotDotNet.Core;

namespace VoxelEditorForGodotDotNet.IO
{
	public static class VoxelJsonSaveData
	{
		public const int CurrentVersion = 3;

		#region Overloads accepting Json Resource

		public static void SaveData(Json jsonResource, VoxelData[,,] voxelValues)
		{
			if (jsonResource == null)
			{
				GD.PushError("Json resource is null.");
				return;
			}

			SaveData(jsonResource.ResourcePath, voxelValues);

			// Reload the Json resource in memory so Godot updates its parsed Data state immediately
			using var file = FileAccess.Open(jsonResource.ResourcePath, FileAccess.ModeFlags.Read);
			if (file != null)
			{
				jsonResource.Parse(file.GetAsText());
			}
		}

		public static VoxelData[,,] LoadData(Json jsonResource)
		{
			if (jsonResource == null)
			{
				GD.PushError("Json resource is null.");
				return new VoxelData[0, 0, 0];
			}

			return LoadData(jsonResource.ResourcePath);
		}

		#endregion

		#region Save and Load Implementations (File Path)

		public static void SaveData(string filePath, VoxelData[,,] voxelValues)
		{
			if (string.IsNullOrEmpty(filePath))
			{
				GD.PushError("File path is null or empty.");
				return;
			}

			int resolutionX = voxelValues.GetLength(0);
			int resolutionY = voxelValues.GetLength(1);
			int resolutionZ = voxelValues.GetLength(2);
			int totalVoxels = resolutionX * resolutionY * resolutionZ;

			float[] weightInsideIsPositive = new float[totalVoxels];
			Color[] colors = new Color[totalVoxels];

			int counter = 0;
			for (int x = 0; x < resolutionX; x++)
			{
				for (int y = 0; y < resolutionY; y++)
				{
					for (int z = 0; z < resolutionZ; z++)
					{
						weightInsideIsPositive[counter] = voxelValues[x, y, z].WeightInsideIsPositive;
						colors[counter] = voxelValues[x, y, z].Color;
						counter++;
					}
				}
			}

			byte[] serializedData = SerializeDataV2(weightInsideIsPositive, colors);
			string packedData = Convert.ToBase64String(serializedData);

			var jsonDict = new Godot.Collections.Dictionary
			{
				{ "version", CurrentVersion },
				{ "resolutionX", resolutionX },
				{ "resolutionY", resolutionY },
				{ "resolutionZ", resolutionZ },
				{ "packedData", packedData }
			};

			string jsonString = Json.Stringify(jsonDict, "\t");

			using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
			if (file != null)
			{
				file.StoreString(jsonString);
				file.Flush(); // Force write to physical disk immediately
				GD.Print($"Saved voxel JSON data to: {filePath}");
			}
			else
			{
				GD.PushError($"Failed to open {filePath} for writing. Error: {FileAccess.GetOpenError()}");
			}
		}

		public static VoxelData[,,] LoadData(string filePath)
		{
			if (!FileAccess.FileExists(filePath))
			{
				GD.PushError($"JSON file does not exist at path: {filePath}");
				return new VoxelData[0, 0, 0];
			}

			using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
			if (file == null)
			{
				GD.PushError($"Failed to open {filePath} for reading. Error: {FileAccess.GetOpenError()}");
				return new VoxelData[0, 0, 0];
			}

			string jsonString = file.GetAsText();
			if (string.IsNullOrWhiteSpace(jsonString) || jsonString.Trim() == "{}")
			{
				// Empty file, return empty grid safely
				return new VoxelData[0, 0, 0];
			}

			var json = new Json();
			Error parseResult = json.Parse(jsonString);

			if (parseResult != Error.Ok)
			{
				GD.PushError($"JSON Parse Error: {json.GetErrorMessage()} at line {json.GetErrorLine()}");
				return new VoxelData[0, 0, 0];
			}

			var jsonDict = json.Data.AsGodotDictionary();

			if (!jsonDict.ContainsKey("packedData") || !jsonDict.ContainsKey("resolutionX"))
			{
				return new VoxelData[0, 0, 0];
			}

			int resolutionX = (int)jsonDict["resolutionX"];
			int resolutionY = (int)jsonDict["resolutionY"];
			int resolutionZ = (int)jsonDict["resolutionZ"];
			int version = (int)jsonDict["version"];
			string packedData = (string)jsonDict["packedData"];

			if (string.IsNullOrEmpty(packedData))
			{
				return new VoxelData[resolutionX, resolutionY, resolutionZ];
			}

			int totalVoxels = resolutionX * resolutionY * resolutionZ;
			VoxelData[,,] voxelValues = new VoxelData[resolutionX, resolutionY, resolutionZ];

			byte[] byteData = Convert.FromBase64String(packedData);
			int currentDataVersion = version;

			if (currentDataVersion == 0)
			{
				byteData = ConvertV0ToV3(byteData, totalVoxels);
				currentDataVersion = 2;
			}

			if (currentDataVersion == 1)
			{
				GD.PushWarning("V1 was an intermediate version and cannot be converted. Loading failed.");
				return voxelValues;
			}
			else if (currentDataVersion == 2)
			{
				GD.PushWarning("V2 was an intermediate version and cannot be converted. Loading failed.");
				return voxelValues;
			}
			
			if (currentDataVersion == CurrentVersion)
			{
				var (weightInsideIsPositive, colors) = DeserializeDataV3(byteData, totalVoxels);

				int counter = 0;
				for (int x = 0; x < resolutionX; x++)
				{
					for (int y = 0; y < resolutionY; y++)
					{
						for (int z = 0; z < resolutionZ; z++)
						{
							voxelValues[x, y, z] = new VoxelData(weightInsideIsPositive[counter], colors[counter]);
							counter++;
						}
					}
				}
			}

			return voxelValues;
		}

		#endregion

		#region Serialization Helper Logic

		private static byte[] SerializeDataV2(float[] weightInsideIsPositive, Color[] colors)
		{
			List<byte> returnValue = new List<byte>();

			short prevValue = ConvertCenterFloatToShort(weightInsideIsPositive[0]);
			returnValue.Add(0);

			byte[] bytes = BitConverter.GetBytes(prevValue);
			returnValue.AddRange(bytes);

			for (int i = 1; i < weightInsideIsPositive.Length; i++)
			{
				float weight = weightInsideIsPositive[i];
				short scaledValue = ConvertCenterFloatToShort(weight);

				if (scaledValue == prevValue && returnValue[returnValue.Count - 3] < 254)
				{
					returnValue[returnValue.Count - 3]++;
				}
				else
				{
					returnValue.Add(0);
					bytes = BitConverter.GetBytes(scaledValue);
					returnValue.AddRange(bytes);
					prevValue = scaledValue;
				}
			}

			Color prevColor = colors[0];
			returnValue.Add(0);

			returnValue.Add((byte)prevColor.R8);
			returnValue.Add((byte)prevColor.G8);
			returnValue.Add((byte)prevColor.B8);
			returnValue.Add((byte)prevColor.A8);

			for (int i = 1; i < colors.Length; i++)
			{
				Color currentColor = colors[i];

				byte currR = (byte)currentColor.R8;
				byte currG = (byte)currentColor.G8;
				byte currB = (byte)currentColor.B8;
				byte currA = (byte)currentColor.A8;

				byte prevR = (byte)prevColor.R8;
				byte prevG = (byte)prevColor.G8;
				byte prevB = (byte)prevColor.B8;
				byte prevA = (byte)prevColor.A8;

				if (currR == prevR &&
					currG == prevG &&
					currB == prevB &&
					currA == prevA &&
					returnValue[returnValue.Count - 5] < 254)
				{
					returnValue[returnValue.Count - 5]++;
				}
				else
				{
					returnValue.Add(0);
					returnValue.Add(currR);
					returnValue.Add(currG);
					returnValue.Add(currB);
					returnValue.Add(currA);

					prevColor = currentColor;
				}
			}

			return returnValue.ToArray();

			static short ConvertCenterFloatToShort(float value)
			{
				return (short)(value * 32767);
			}
		}

		private static (float[] weightInsideIsPositive, Color[] colors) DeserializeDataV3(byte[] data, int totalVoxels)
		{
			List<float> weightList = new List<float>();
			List<Color> colorList = new List<Color>();

			int index = 0;

			while (weightList.Count < totalVoxels)
			{
				int runLength = data[index++] + 1;
				short weightShort = BitConverter.ToInt16(data, index);
				index += 2;

				float weight = ConvertShortToFloat(weightShort);

				for (int i = 0; i < runLength; i++)
				{
					weightList.Add(weight);
				}
			}

			if (weightList.Count != totalVoxels)
			{
				GD.PushWarning($"Mismatch! Weights: {weightList.Count}, Expected: {totalVoxels}");
			}

			while (colorList.Count < totalVoxels)
			{
				int runLength = data[index++] + 1;
				Color color = Color.Color8(data[index], data[index + 1], data[index + 2], data[index + 3]);
				index += 4;

				for (int i = 0; i < runLength; i++)
				{
					colorList.Add(color);
				}
			}

			return (weightList.ToArray(), colorList.ToArray());

			static float ConvertShortToFloat(short value)
			{
				return value / 32767f;
			}
		}

		public static byte[] ConvertV0ToV3(byte[] v0Data, int totalVoxels)
		{
			var (weightInsideIsPositive, colors) = DeserializeDataV0(v0Data, totalVoxels);
			return SerializeDataV2(weightInsideIsPositive, colors);

			static (float[] weights, Color[] colors) DeserializeDataV0(byte[] data, int totalVoxelsCount)
			{
				float[] weights = new float[totalVoxelsCount];
				Color[] colors = new Color[totalVoxelsCount];

				int byteIndex = 0;

				for (int i = 0; i < totalVoxelsCount; i++)
				{
					weights[i] = BitConverter.ToSingle(data, byteIndex);
					colors[i] = Color.Color8(data[byteIndex + 4], data[byteIndex + 5], data[byteIndex + 6], data[byteIndex + 7]);
					byteIndex += 36;
				}

				return (weights, colors);
			}
		}

		#endregion
	}
}