using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace FileExplorer.Common
{
	public class MetadataProperties : Dictionary<string, object>
	{
		public override string ToString()
		{
			if (Count == 0)
				return base.ToString();

			return String.Join("_", this.Select(x => $"{x.Key}={x.Value}"));
		}

		public BsonDocument ToDocument()
		{
			BsonDocument document = new BsonDocument();
			foreach (KeyValuePair<string, object> item in this)
			{
				if (item.Value is Enum)
					document[item.Key] = Enum.GetName(item.Value.GetType(), item.Value);
				else
					document[item.Key] = new BsonValue(item.Value);
			}

			return document;
		}
	}
}
