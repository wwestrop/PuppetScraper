using System.Collections.Generic;

namespace PuppetScraper {

	public class Resource {
		public string Name { get; set; }
		public string Description { get; set; }
		public List<Property> Properties { get; set; } = new List<Property>();

		public override string ToString() => this.Name;
	}

}
