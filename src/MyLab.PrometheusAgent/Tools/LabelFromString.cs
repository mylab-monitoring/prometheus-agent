using System;
using System.IO;
using System.Text;

namespace MyLab.PrometheusAgent.Tools
{
    class LabelFromString
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public static LabelFromString Read(StringReader reader)
        {
            if (reader.Peek() == -1) return null;

            var nameBuilder = new StringBuilder();
            
            int ch;

            ReadTillSpace(reader);
            while ((ch = reader.Read()) != -1 && ch != '=')
            {
                nameBuilder.Append((char)ch);
            }
            ReadTillSpace(reader);

            var firstQuotes = reader.Read();

            var valueBuilder = new StringBuilder();

            if (firstQuotes != -1 && firstQuotes != '\"')
                valueBuilder.Append((char)firstQuotes);

            while ((ch = reader.Read()) != -1 && ch != '\"')
            {
                valueBuilder.Append((char)ch);
            }

            ReadTillSpace(reader);

            if (reader.Peek() == ',')
                reader.Read();

            return new LabelFromString
            {
                Name = nameBuilder.ToString().Trim(),
                Value = valueBuilder.ToString()
            };
        }

        static void ReadTillSpace(StringReader reader)
        {
            char nextChar;
            while ((nextChar = (char)reader.Peek()) == ' ' || nextChar == '\t')
            {
                reader.Read();
            }
        }

    }
}
