using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace PuppetScraper {

	public static class Program {

		public static void Main(string[] args) {
			using (var http = new HttpClient()) {
				string html = http.GetStringAsync(@"https://docs.puppet.com/puppet/latest/type.html").Result;

				var document = new HtmlDocument();
				document.LoadHtml(html);

				//var typerNames = document.DocumentNode.Descendants()
				//	.Where(n => n.Name == "hr").First();

				var typeNames = document.DocumentNode.Descendants()
					.Where(n => n.Name == "hr")
					.Select(n => n.NextSibling.NextSibling.Id)
					.ToList();

				var typeNameAndAttrs = document.DocumentNode.Descendants()
					.Where(n => n.Name == "hr")
					.Select(n => new {
						Name = n.NextSibling.NextSibling.InnerText,
						Attributes = GetProperties(n.NextSibling.NextSibling.InnerText, document),
					})
					.OrderBy(t => t.Name)
					.ToList();

			}
		}

		private static List<string> GetProperties(string resourceType, HtmlDocument fromDocument) {

			var result = new List<string>();
			
			var attributesTitleNode = fromDocument.DocumentNode.Descendants()
				.SingleOrDefault(n => n.Id == $"{resourceType}-attributes")
				.NextSibling;

			var thisAttr = attributesTitleNode;
			while (thisAttr != null && thisAttr.Name != "hr" && thisAttr.Name != "h3") {
				if (thisAttr.Name == "h4") {
					var enumValues = GetEnumValues(resourceType, thisAttr);
					result.Add(thisAttr.InnerText);
				}

				thisAttr = thisAttr.NextSibling;
			}

			// TODO get providers for type
			// TODO get enum values for type (e.g. ensure we know will always have, what about the rest?????)
			// TODO how do we tell a boolean. Force? Enabled?

			return result.OrderBy(s => s).ToList();
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
			if(parameterTitleElement.InnerText.ToLower() != "provider") {
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

		///// <summary>
		///// Searches for nodes matching a given predicate for the given type
		///// </summary>
		///// <param name="typeTitle">The HTML node corresponding to the title of the resource</param>
		//private void SearchType(HtmlNode typeTitle) {

		//	var attributesTitleNode = fromDocument.DocumentNode.Descendants()
		//		.SingleOrDefault(n => n.Id == $"{forType}-attributes")
		//		.NextSibling;

		//	var thisAttr = attributesTitleNode;
		//	while (thisAttr != null && thisAttr.Name != "hr" && thisAttr.Name != "h3") {
		//		if (thisAttr.Name == "h4") {
		//			result.Add(thisAttr.InnerText);
		//		}

		//		thisAttr = thisAttr.NextSibling;
		//	}
		//}

	}

}
