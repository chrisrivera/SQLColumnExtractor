using System.Collections.Generic;

namespace ColumnExtracter.Parser
{
    public class ParseResult
    {
        public List<string> Tablelist { get; set; }
        public List<string> Columnlist { get; set; }
        public List<string> Databaselist { get; set; }
        public List<string> Schemalist { get; set; }
        public List<string> Functionlist { get; set; }
        public List<string> Triggerlist { get; set; }
        public List<string> Sequencelist { get; set; }
        public string Structure { get; set; }
    }
}
