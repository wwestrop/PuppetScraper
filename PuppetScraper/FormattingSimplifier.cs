using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace PuppetScraper {

	/// <summary>
	/// The details window popped up by VS Code supports only plain text. 
	/// The documentation provided by Puppet is HTML. We convert it to pseudo-Markdown. 
	/// It still displays as plain text, but it's easier to read than HTML tags and encoded entities. 
	/// </summary>
	internal static class FormattingSimplifier {

		private static readonly Regex codeTagRegex = new Regex("<code>(.+?)</code>", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex strongTagRegex = new Regex("<strong>(.+?)</strong>", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public static void Simplify(StringBuilder description) {
			TreatTags(description, codeTagRegex, SimplifyCodeTag);
			TreatTags(description, strongTagRegex, SimplifyStrongTag);
		}

		private static string SimplifyCodeTag(string matchedTagContent) {
			if (!matchedTagContent.Contains("\n")) {
				return $"`{matchedTagContent}`";
			}
			else {
				return matchedTagContent;
			}
		}

		private static string SimplifyStrongTag(string matchedTagContent) {
			return matchedTagContent;
		}

		private static void TreatTags(StringBuilder documentBuilder, Regex tagMatcher, Func<string, string> treatMatch) {

			var results = tagMatcher.Matches(documentBuilder.ToString())
				.OfType<Match>()
				.OrderByDescending(m => m.Index)
				.ToList();

			foreach (var r in results) {
				var matchedTag = r.Groups[0];
				var innerHtml = HttpUtility.HtmlDecode(r.Groups[1].Value);

				var treatedHtml = treatMatch(innerHtml);
				documentBuilder.Remove(matchedTag.Index, matchedTag.Length);
				documentBuilder.Insert(matchedTag.Index, treatedHtml);
			}
		}

	}
}
