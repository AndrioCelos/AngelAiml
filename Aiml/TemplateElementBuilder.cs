using System.Reflection;
using System.Xml.Linq;
#if !NET6_0_OR_GREATER
using NullabilityInfoContext = Nullability.NullabilityInfoContextEx;
using NullabilityState = Nullability.NullabilityStateEx;
#endif

namespace Aiml;
internal class TemplateElementBuilder {
	private readonly ConstructorInfo constructor;
	private readonly AimlParameterData[] parameterData;
	private readonly int? contentParamIndex;
	private readonly bool allowUnknownAttributes;

	public TemplateElementBuilder(Type type) {
		// If there is a constructor with the appropriate attribute, use that; otherwise use the first constructor, which will be the primary constructor if the type has one.
		var constructors = type.GetConstructors();
		var constructor = constructors.FirstOrDefault(c => c.GetCustomAttribute<AimlLoaderContructorAttribute>() is not null) ?? constructors[0];
		this.constructor = constructor;
		allowUnknownAttributes = type == typeof(Tags.SraiX);

		var nullabilityInfoContext = new NullabilityInfoContext();

		// Analyze the constructor parameters.
		var parameters = constructor.GetParameters();
		parameterData = new AimlParameterData[parameters.Length];
		for (var i = 0; i < parameters.Length; i++) {
			var param = parameters[i];
			if (param.ParameterType == typeof(TemplateElementCollection)) {
				// Either an attribute parameter or the children parameter.
				if (param.Name == "children") {
					parameterData[i] = new(ParameterType.Children, null, false, null);
					contentParamIndex = i;
				} else
					parameterData[i] = new(ParameterType.Attribute, param.Name, nullabilityInfoContext.Create(param).WriteState == NullabilityState.Nullable, null);
			} else if (param.ParameterType == typeof(XElement)) {
				parameterData[i] = new(ParameterType.XmlElement, null, false, null);
			} else if (param.ParameterType.IsArray && param.ParameterType.GetArrayRank() == 1 && param.ParameterType.GetElementType() is Type elementType && typeof(TemplateNode).IsAssignableFrom(elementType)) {
				// A special element parameter (for <li> elements).
				parameterData[i] = new(ParameterType.SpecialElement, elementType.Name, false, elementType);
			} else
				throw new ArgumentException($"Invalid parameter type: {param.ParameterType}");
		}
	}

	public object Parse(XElement el, AimlLoader loader) {
		var values = new object?[parameterData.Length];
		var children = new List<object>[parameterData.Length];
		for (var i = 0; i < children.Length; i++) children[i] = [];
		var content = contentParamIndex is not null ? children[contentParamIndex.Value] : null;

		// Populate attribute parameters from XML attributes.
		foreach (var attr in el.Attributes()) {
			var i = Array.FindIndex(parameterData, p => p.Type == ParameterType.Attribute && p.Name!.Equals(attr.Name.LocalName, StringComparison.OrdinalIgnoreCase));
			if (i >= 0)
				values[i] = new TemplateElementCollection(attr.Value);
			else if (!allowUnknownAttributes)
				throw new AimlException($"Unknown attribute '{attr.Name}'", el);
		}
		// Populate parameters from XML child nodes.
		foreach (var childNode in el.Nodes()) {
			switch (childNode) {
				case XText textNode:
					if (content is not null)
						content.Add(new TemplateText(textNode.Value));
					else if (!string.IsNullOrWhiteSpace(textNode.Value))
						throw new AimlException("Cannot have content.", el);
					break;
				case XElement childElement:
					var i = Array.FindIndex(parameterData, p => p.Name is not null && p.Name.Equals(childElement.Name.LocalName, StringComparison.OrdinalIgnoreCase));
					if (i >= 0) {
						if (parameterData[i].Type == ParameterType.SpecialElement)
							children[i].Add(loader.ParseChildElementInternal(childElement, parameterData[i].ChildType!));
						else
							values[i] = values[i] is null
								? TemplateElementCollection.FromXml(childElement, loader)
								: throw new AimlException($"'{parameterData[i].Name}' attribute provided multiple times.", el);
					} else if (content is null)
						throw new AimlException("Cannot have content.", el);
					else
						content.Add(loader.ParseElement(childElement));
					break;
			}
		}

		for (var i = 0; i < values.Length; i++) {
			var param = parameterData[i];
			switch (param.Type) {
				case ParameterType.Children:
					values[i] = new TemplateElementCollection(children[i].Cast<TemplateNode>());
					break;
				case ParameterType.Attribute:
					if (values[i] is null && !param.IsOptional)
						throw new AimlException($"Missing required attribute '{param.Name}'", el);
					break;
				case ParameterType.SpecialElement:
					var array = Array.CreateInstance(param.ChildType!, children[i].Count);
					for (var j = 0; j < array.Length; j++)
						array.SetValue(children[i][j], j);
					values[i] = array;
					break;
				case ParameterType.XmlElement:
					values[i] = el;
					break;
			}
		}
		try {
			return constructor.Invoke(values);
		} catch (ArgumentException ex) {
			throw new AimlException(ex.Message, el, ex);
		} catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException) {
			throw new AimlException(ex.InnerException.Message, el, ex.InnerException);
		}
	}

	private record struct AimlParameterData(ParameterType Type, string? Name, bool IsOptional, Type? ChildType);
	private enum ParameterType {
		Children,
		Attribute,
		SpecialElement,
		XmlElement
	}
}
