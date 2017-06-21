using System.Collections.Generic;

namespace PuppetScraper {

	public class Property {
		public string Name { get; set; }

		/// <summary>Use one of <c>PuppetType</c></summary>
		public string Type { get; set; }
		public List<string> PossibleValues { get; set; }

		public override string ToString() => $"{this.Name} ({this.Type})";
	}

}