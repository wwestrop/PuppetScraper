using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace PuppetScraper {

	public static class Program {

		public static void Main(string[] args) {
			using (var http = new HttpClient()) {
				string html = http.GetStringAsync(@"https://docs.puppet.com/puppet/latest/type.html").Result;

				var document = new HtmlDocument();
				document.LoadHtml(html);

				var documentedPuppetTypes = document.DocumentNode.Descendants()
					.Where(n => n.Name == "hr")
					.Select(n => new Resource {
						Name = n.NextSibling.NextSibling.InnerText,
						Description = FindResourceDescription(n.NextSibling.NextSibling.InnerText, document),
						Properties = GetProperties(n.NextSibling.NextSibling.InnerText, document),
					})
					.OrderBy(t => t.Name)
					.ToList();

				ExportAsJson(documentedPuppetTypes);
			}
		}

		private static List<Property> GetProperties(string resourceType, HtmlDocument fromDocument) {

			var result = new List<Property>();
			
			var attributesTitleNode = fromDocument.DocumentNode.Descendants()
				.SingleOrDefault(n => n.Id == $"{resourceType}-attributes")
				.NextSibling;

			var thisAttr = attributesTitleNode;
			while (thisAttr != null && thisAttr.Name != "hr" && thisAttr.Name != "h3") {
				if (thisAttr.Name == "h4") {
					string propertyType;
					var enumValues = GetEnumValues(resourceType, thisAttr);
					if (enumValues != null) {
						propertyType = PuppetType.Enum;
					}
					else {
						propertyType = PuppetType.Variant;
					}

					var propertyInfo = new Property {
						Name = thisAttr.InnerText,
						Type = propertyType,
						PossibleValues = enumValues,
					};
					result.Add(propertyInfo);
				}

				thisAttr = thisAttr.NextSibling;
			}

			// TODO get providers for type
			// TODO get enum values for type (e.g. ensure we know will always have, what about the rest?????)
			// TODO how do we tell a boolean. Force? Enabled?

			return result.OrderBy(r => r.Name).ToList();
		}

		private static void ExportAsJson(List<Resource> resources) {

			string autogenWarning = "/* Auto-generated file. Do not edit. */ \r\n\r\n";
			string preamble = "import { PuppetType } from '../types/puppetType';\r\nimport { IResource } from '../types/IResource';\r\n\r\nconst BuiltInResources: IResource[] = ";
			string postamble = ";\r\n\r\nexport default BuiltInResources;";

			var serializer = new JsonSerializer();
			using (var stringWriter = new StringWriter())
			using (var jsonWriter = new JsonTextWriter(stringWriter)) {
				var sb = stringWriter.GetStringBuilder();
				sb.AppendLine(autogenWarning);
				sb.Append(preamble);

				jsonWriter.QuoteName = false;
				jsonWriter.Formatting = Formatting.Indented;
				serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
				serializer.Serialize(jsonWriter, resources);

				sb.Append(postamble);

				File.WriteAllText(@"builtInResources.generated.ts", sb.ToString());
			}
		}

		/// <param name="parameterTitleElement">The element that contains the title of the parameter that the documentation is 
		/// describing. We'll search down from there to try and infer the possible values.</param>
		/// <returns><c>null</c> if this doesn't appear to be an enum type</returns>
		private static List<string> GetEnumValues(string resourceType, HtmlNode parameterTitleElement) {

			List<string> result = null;

			result = ReadEnumValues(parameterTitleElement);
			if (result != null) {
				return result;
			}

			result = FindProviderValues(resourceType, parameterTitleElement);
			if (result != null) {
				return result;
			}

			return null;
		}

		/// <summary>Attempts to read possible values that are written explcitly in the document</summary>
		private static List<string> ReadEnumValues(HtmlNode parameterTitleElement) {

			parameterTitleElement = parameterTitleElement.NextSibling;

			while (parameterTitleElement != null && parameterTitleElement.Name != "hr" && parameterTitleElement.Name != "h3" && parameterTitleElement.Name != "h4") {
				if (parameterTitleElement.InnerText.ToLower().Contains("valid values are")) {
					return parameterTitleElement.Descendants()
						.Where(n => n.Name == "code")
						.Select(n => n.InnerText)
						.ToList();
				}

				parameterTitleElement = parameterTitleElement.NextSibling;
			}

			return null;
		}

		private static List<string> FindProviderValues(string resourceType, HtmlNode parameterTitleElement) {
			if (parameterTitleElement.InnerText.ToLower() != "provider") {
				return null;
			}

			// Locate the provider node for this resource
			var providersTitleNode = parameterTitleElement.OwnerDocument.DocumentNode.Descendants()
				.SingleOrDefault(n => n.Id == $"{resourceType}-providers")
				.NextSibling;

			var result = new List<string>();
			var thisAttr = providersTitleNode;
			while (thisAttr != null && thisAttr.Name != "hr" && thisAttr.Name != "h3") {
				if (thisAttr.Name == "h4") {
					result.Add(thisAttr.InnerText);
				}

				thisAttr = thisAttr.NextSibling;
			}

			return result.OrderBy(s => s).ToList();
		}

		private static string FindResourceDescription(string resourceType, HtmlDocument document) {

			var sb = new StringBuilder();

			var attributesTitleNode = document.DocumentNode.Descendants()
				.SingleOrDefault(n => n.Id == $"{resourceType}-description")
				.NextSibling
				.NextSibling;

			var thisAttr = attributesTitleNode;
			while (thisAttr != null && thisAttr.Name != "hr" && thisAttr.Name != "h3") {
				sb.Append(thisAttr.InnerHtml);

				thisAttr = thisAttr.NextSibling;
			}

			return FormattingSimplifier.Simplify(sb);
		}
	}

}
