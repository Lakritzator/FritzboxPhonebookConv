namespace FritzboxPhonebookConv.Models
{
    /// <summary>
    /// Represents a single phonebook available on the Fritz.Box.
    /// </summary>
    public class Phonebook
    {
        /// <summary>TR-064 phonebook ID as returned by GetPhonebookList.</summary>
        public int Id { get; set; }

        /// <summary>Human-readable name returned by GetPhonebook.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Download URL returned by GetPhonebook.</summary>
        public string Url { get; set; } = string.Empty;

        public override string ToString() => $"{Name}  (#{Id})";
    }
}
