using System;
using System.Collections.Generic;

namespace ZeroGravity.Data;

[Serializable]
public class RefinedResourcesData : ISceneData
{
	public ResourceType RawResource;

	public List<CargoResourceData> RefinedResources;
}
