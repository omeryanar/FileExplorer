using System.ComponentModel.DataAnnotations;
using FileExplorer.Persistence;
using LiteDB;

namespace FileExplorer.Core
{
	public class UniqueAttribute : ValidationAttribute
	{
		public string CollectionName { get; set; }

		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			if (validationContext.ObjectInstance is PersistentItem persistentItem)
			{
				string command = $"SELECT COUNT(*) FROM {CollectionName} WHERE {validationContext.MemberName} = \"{value}\" AND _id != {persistentItem.Id}";
				var duplicate = App.Repository.Database.Execute(command).ToList();

				if (duplicate?.Count > 0)
				{
					int count = duplicate[0]["expr"].AsInt32;
					if (count > 0)
						return new ValidationResult(ErrorMessageString);
				}
			}

			return ValidationResult.Success;
		}
	}
}
