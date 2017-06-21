using System.Collections.Generic;

namespace PuppetScraper {

	public class Property {
		public string Name { get; set; }
		public PuppetType Type { get; set; }
		public List<string> PossibleValues { get; set; }

		public override string ToString() => $"{this.Name} ({this.Type})";
	}

}